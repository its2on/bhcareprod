using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using System.Threading.Tasks;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Barangay.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OTPController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ISmsService _smsService;
        private readonly IEncryptionService _encryptionService;
        private readonly IOTPService _otpService;
        private readonly ILogger<OTPController> _logger;

        public OTPController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ISmsService smsService,
            IEncryptionService encryptionService,
            IOTPService otpService,
            ILogger<OTPController> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _smsService = smsService;
            _encryptionService = encryptionService;
            _otpService = otpService;
            _logger = logger;
        }

        // POST api/OTP/send
        [HttpPost("send")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            try
            {
                // Check if request is null
                if (request == null)
                {
                    _logger.LogError("OTP request is null");
                    return BadRequest(new { success = false, message = "Invalid request. Please try again." });
                }

                // Validate request
                // Trim email and phone number
                if (request.Email != null) request.Email = request.Email.Trim();
                if (request.PhoneNumber != null) request.PhoneNumber = request.PhoneNumber.Trim();
                
                _logger.LogInformation("OTP Send Request - Email: {Email}, Phone: {Phone}, SendVia: {SendVia}", 
                    request.Email ?? "null", request.PhoneNumber ?? "null", request.SendVia ?? "Email");

                // Email is always required for storage (EmailVerifications table uses email as key)
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    _logger.LogWarning("OTP request rejected: Email is required");
                    return BadRequest(new { success = false, message = "Email is required for OTP verification" });
                }

                // Validate email format (very lenient - just check for @ and .)
                if (!request.Email.Contains("@") || !request.Email.Contains("."))
                {
                    _logger.LogWarning("OTP request rejected: Invalid email format - {Email}", request.Email);
                    return BadRequest(new { success = false, message = "Please enter a valid email address" });
                }

                // Additional basic validation - must have at least one character before @ and after @
                var emailParts = request.Email.Split('@');
                if (emailParts.Length != 2 || string.IsNullOrWhiteSpace(emailParts[0]) || string.IsNullOrWhiteSpace(emailParts[1]))
                {
                    _logger.LogWarning("OTP request rejected: Invalid email format - {Email}", request.Email);
                    return BadRequest(new { success = false, message = "Please enter a valid email address" });
                }

                // Check if domain has at least one dot
                if (!emailParts[1].Contains("."))
                {
                    _logger.LogWarning("OTP request rejected: Invalid email format - missing domain - {Email}", request.Email);
                    return BadRequest(new { success = false, message = "Please enter a valid email address" });
                }

                // Normalize SendVia (case-insensitive)
                var sendVia = (request.SendVia ?? "Email").Trim();
                if (string.IsNullOrWhiteSpace(sendVia))
                {
                    sendVia = "Email";
                }
                request.SendVia = sendVia;

                // Validate based on send method
                if (string.Equals(sendVia, "SMS", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                    {
                        _logger.LogWarning("OTP request rejected: Phone number required for SMS");
                        return BadRequest(new { success = false, message = "Phone number is required for SMS verification" });
                    }
                    // Validate phone number format (basic validation)
                    var cleanedPhone = request.PhoneNumber.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "");
                    var phoneRegex = new System.Text.RegularExpressions.Regex(@"^(\+?63|0)?9\d{9}$");
                    if (!phoneRegex.IsMatch(cleanedPhone))
                    {
                        _logger.LogWarning("OTP request rejected: Invalid phone format - {Phone}", request.PhoneNumber);
                        return BadRequest(new { success = false, message = "Please enter a valid Philippine phone number (e.g., 09171234567)" });
                    }
                    // Update phone number with cleaned version
                    request.PhoneNumber = cleanedPhone;
                }
                else if (string.Equals(sendVia, "Email", StringComparison.OrdinalIgnoreCase))
                {
                    // Email validation already done above
                    _logger.LogInformation("Sending OTP via Email to {Email}", request.Email);
                }
                else
                {
                    // Default to Email if SendVia is not specified or invalid
                    request.SendVia = "Email";
                    _logger.LogInformation("SendVia '{SendVia}' not recognized, defaulting to Email", sendVia);
                }

                // Check if email is suspended (with fallback if table doesn't exist)
                try
                {
                    var suspension = await _context.EmailSuspensions
                        .FirstOrDefaultAsync(s => s.Email == request.Email && s.IsActive);

                    if (suspension != null && suspension.SuspensionEndDate > DateTime.UtcNow)
                    {
                        var remainingTime = suspension.SuspensionEndDate.Value - DateTime.UtcNow;
                        var timeString = remainingTime.TotalHours >= 1 
                            ? $"{remainingTime.TotalHours:F0} hours"
                            : $"{remainingTime.TotalMinutes:F0} minutes";
                        
                        return BadRequest(new { 
                            success = false, 
                            message = $"Email verification is suspended due to multiple failed attempts. Please try again in {timeString}.",
                            suspended = true,
                            suspensionEndDate = suspension.SuspensionEndDate
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "EmailSuspensions table not found, skipping suspension check for {Email}", request.Email);
                    // Continue without suspension check if table doesn't exist
                }

                // Generate a 6-digit OTP using OTPService (stores in memory cache)
                // This ensures the OTP is available for validation in OTPVerification page
                string otp = await _otpService.GenerateOTPAsync(request.Email);
                
                // Log the OTP for testing purposes
                _logger.LogInformation("Generated OTP {OTP} for {Email} via OTPService", otp, request.Email);

                try
                {
                    // Store the OTP with the email and expiry time (10 minutes)
                    // Always use email for storage (EmailVerifications table uses email as key)
                    var otpEntry = await _context.EmailVerifications
                        .FirstOrDefaultAsync(e => e.Email == request.Email);

                    if (otpEntry == null)
                    {
                        // Try to create a new entry
                        try
                        {
                            otpEntry = new EmailVerification
                            {
                                Email = request.Email,
                                VerificationCode = otp,
                                ExpiryTime = DateTime.UtcNow.AddMinutes(10)
                            };
                            _context.EmailVerifications.Add(otpEntry);
                            await _context.SaveChangesAsync();
                        }
                        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && sqlEx.Number == 2601)
                        {
                            // Handle unique constraint violation - another process may have created the record
                            _logger.LogWarning("Unique constraint violation detected, attempting to update existing record for {Email}", request.Email);
                            
                            // Try to find and update the existing record
                            otpEntry = await _context.EmailVerifications
                                .FirstOrDefaultAsync(e => e.Email == request.Email);
                            
                            if (otpEntry != null)
                            {
                                otpEntry.VerificationCode = otp;
                                otpEntry.ExpiryTime = DateTime.UtcNow.AddMinutes(10);
                                await _context.SaveChangesAsync();
                            }
                            else
                            {
                                _logger.LogError("Could not find existing EmailVerification record after unique constraint violation for {Email}", request.Email);
                            }
                        }
                    }
                    else
                    {
                        // Update existing entry
                        otpEntry.VerificationCode = otp;
                        otpEntry.ExpiryTime = DateTime.UtcNow.AddMinutes(10);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception dbEx)
                {
                    // Log the database error but continue - we'll still try to send the email
                    _logger.LogError(dbEx, "Database error when storing OTP. Continuing with email send.");
                }

                // Send OTP via selected method
                bool sent = false;
                string sendMethod = request.SendVia ?? "Email"; // Use normalized SendVia

                _logger.LogInformation("Preparing to send OTP via {Method}. PhoneNumber present: {HasPhone}", 
                    sendMethod, !string.IsNullOrWhiteSpace(request.PhoneNumber));

                if (string.Equals(sendMethod, "SMS", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                    {
                        _logger.LogWarning("SMS selected but PhoneNumber is null or empty");
                        return BadRequest(new { success = false, message = "Phone number is required for SMS verification" });
                    }

                    // Send OTP via SMS
                    string smsMessage = $"Your verification code is: {otp}. This code will expire in 10 minutes. Do not share this code with anyone.";
                    
                    _logger.LogInformation("Attempting to send SMS to {PhoneNumber} with OTP {OTP}", request.PhoneNumber, otp);
                    
                    try
                    {
                        sent = await _smsService.SendSmsAsync(request.PhoneNumber, smsMessage);
                        if (sent)
                        {
                            _logger.LogInformation("OTP sent via SMS successfully to {PhoneNumber}", request.PhoneNumber);
                            return Ok(new { success = true, message = "Verification code sent successfully via SMS" });
                        }
                        else
                        {
                            _logger.LogWarning("SMS service returned false for {PhoneNumber}. Check SMS service configuration. The SMS API may be unavailable or misconfigured. Automatically falling back to Email.", request.PhoneNumber);
                            
                            // Automatically send via Email as fallback
                            try
                            {
                                string emailMessage = $@"
                                    <h3>Your Email Verification Code</h3>
                                    <p>Please use the following code to verify your email address:</p>
                                    <h2 style='background-color: #f5f5f5; padding: 10px; font-family: monospace; letter-spacing: 5px;'>{otp}</h2>
                                    <p>This code will expire in 10 minutes.</p>
                                    <p><small>Note: SMS service is currently unavailable, so the code was sent via Email instead.</small></p>
                                    <p>If you did not request this code, please ignore this email.</p>";

                                await _emailSender.SendEmailAsync(request.Email, "Email Verification Code", emailMessage);
                                _logger.LogInformation("OTP sent via Email (SMS fallback) to {Email}", request.Email);
                                return Ok(new { 
                                    success = true, 
                                    message = "Verification code sent successfully via Email (SMS service unavailable)",
                                    fallbackToEmail = true
                                });
                            }
                            catch (Exception emailEx)
                            {
                                _logger.LogError(emailEx, "Error sending email fallback to {Email}", request.Email);
                                return StatusCode(500, new { 
                                    success = false, 
                                    message = "SMS service is unavailable and Email fallback failed. Please try again.",
                                    smsUnavailable = true
                                });
                            }
                        }
                    }
                    catch (Exception smsEx)
                    {
                        _logger.LogError(smsEx, "Exception occurred while sending SMS to {PhoneNumber}. Automatically falling back to Email.", request.PhoneNumber);
                        
                        // Automatically send via Email as fallback
                        try
                        {
                            string emailMessage = $@"
                                <h3>Your Email Verification Code</h3>
                                <p>Please use the following code to verify your email address:</p>
                                <h2 style='background-color: #f5f5f5; padding: 10px; font-family: monospace; letter-spacing: 5px;'>{otp}</h2>
                                <p>This code will expire in 10 minutes.</p>
                                <p><small>Note: SMS service encountered an error, so the code was sent via Email instead.</small></p>
                                <p>If you did not request this code, please ignore this email.</p>";

                            await _emailSender.SendEmailAsync(request.Email, "Email Verification Code", emailMessage);
                            _logger.LogInformation("OTP sent via Email (SMS exception fallback) to {Email}", request.Email);
                            return Ok(new { 
                                success = true, 
                                message = "Verification code sent successfully via Email (SMS service error)",
                                fallbackToEmail = true
                            });
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, "Error sending email fallback to {Email}", request.Email);
                            return StatusCode(500, new { 
                                success = false, 
                                message = "SMS service error and Email fallback failed. Please try again.",
                                smsUnavailable = true
                            });
                        }
                    }
                }
                else
                {
                    // Send OTP via Email (default)
                    string emailMessage = $@"
                        <h3>Your Email Verification Code</h3>
                        <p>Please use the following code to verify your email address:</p>
                        <h2 style='background-color: #f5f5f5; padding: 10px; font-family: monospace; letter-spacing: 5px;'>{otp}</h2>
                        <p>This code will expire in 10 minutes.</p>
                        <p>If you did not request this code, please ignore this email.</p>";

                    try
                    {
                        await _emailSender.SendEmailAsync(request.Email, "Email Verification Code", emailMessage);
                        _logger.LogInformation("OTP sent via Email to {Email}", request.Email);
                        return Ok(new { success = true, message = "Verification code sent successfully via Email" });
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Error sending email to {Email}", request.Email);
                        return StatusCode(500, new { success = false, message = "Failed to send verification code. Please check your email settings." });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP to {Email}", request.Email);
                return StatusCode(500, new { success = false, message = "Failed to send verification code" });
            }
        }

        // POST api/OTP/verify
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Otp))
                {
                    return BadRequest(new { success = false, message = "Email and OTP are required" });
                }

                bool otpVerified = false;
                
                try
                {
                    // Find the OTP entry for the email
                    var otpEntry = await _context.EmailVerifications
                        .FirstOrDefaultAsync(e => e.Email == request.Email);

                    if (otpEntry == null)
                    {
                        return BadRequest(new { success = false, message = "No verification code found for this email" });
                    }

                    // Check if OTP is expired
                    if (otpEntry.ExpiryTime < DateTime.UtcNow)
                    {
                        return BadRequest(new { success = false, message = "Verification code has expired" });
                    }

                    // Verify OTP (trim both values and compare)
                    var storedOtp = otpEntry.VerificationCode?.Trim() ?? "";
                    var providedOtp = request.Otp?.Trim() ?? "";
                    
                    _logger.LogInformation("OTP Comparison for {Email}: Stored='{StoredOtp}', Provided='{ProvidedOtp}'", 
                        request.Email, storedOtp, providedOtp);
                    
                    if (storedOtp != providedOtp)
                    {
                        // Track verification failure
                        await HandleVerificationFailure(request.Email);
                        _logger.LogWarning("OTP verification failed for {Email}. Expected '{Expected}', got '{Actual}'", 
                            request.Email, storedOtp, providedOtp);
                        return BadRequest(new { success = false, message = "Invalid verification code" });
                    }

                    // Update verification record
                    otpEntry.IsVerified = true;
                    otpEntry.VerifiedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    
                    // Clear any existing suspension on successful verification
                    await ClearEmailSuspension(request.Email);
                    
                    otpVerified = true;
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Database error during OTP verification. Falling back to defaults.");
                    // Fall through to user update - we'll still try to mark the email as verified
                }

                if (otpVerified)
                {
                    // Mark email as verified in AspNetUsers if the user exists
                    var user = await _userManager.FindByEmailAsync(request.Email);
                    if (user != null)
                    {
                        user.EmailConfirmed = true;
                        await _userManager.UpdateAsync(user);
                    }

                    return Ok(new { success = true, message = "Email verified successfully" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Failed to verify email" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP for {Email}", request.Email);
                return StatusCode(500, new { success = false, message = "Failed to verify email" });
            }
        }

        private async Task HandleVerificationFailure(string email)
        {
            try
            {
                // Get or create suspension record
                var suspension = await _context.EmailSuspensions
                    .FirstOrDefaultAsync(s => s.Email == email);
                
                if (suspension == null)
                {
                    suspension = new EmailSuspension
                    {
                        Email = email,
                        FailureCount = 0,
                        LastFailureDate = DateTime.UtcNow,
                        IsActive = false
                    };
                    _context.EmailSuspensions.Add(suspension);
                }
                
                // Increment failure count
                suspension.FailureCount++;
                suspension.LastFailureDate = DateTime.UtcNow;
                suspension.UpdatedAt = DateTime.UtcNow;
                
                // Determine suspension based on failure count
                if (suspension.FailureCount >= 3)
                {
                    suspension.IsActive = true;
                    suspension.SuspensionStartDate = DateTime.UtcNow;
                    
                    if (suspension.FailureCount == 3)
                    {
                        // 1 hour suspension
                        suspension.SuspensionEndDate = DateTime.UtcNow.AddHours(1);
                        suspension.SuspensionLevel = "3f";
                        suspension.SuspensionReason = "3 verification failures - 1 hour suspension";
                    }
                    else if (suspension.FailureCount == 5)
                    {
                        // 6 hours suspension
                        suspension.SuspensionEndDate = DateTime.UtcNow.AddHours(6);
                        suspension.SuspensionLevel = "5f";
                        suspension.SuspensionReason = "5 verification failures - 6 hour suspension";
                    }
                    else if (suspension.FailureCount >= 10)
                    {
                        // 24 hours suspension
                        suspension.SuspensionEndDate = DateTime.UtcNow.AddHours(24);
                        suspension.SuspensionLevel = "10f";
                        suspension.SuspensionReason = "10+ verification failures - 24 hour suspension";
                    }
                }
                
                await _context.SaveChangesAsync();
                _logger.LogWarning("Email verification failure tracked for {Email}. Failure count: {Count}", email, suspension.FailureCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling verification failure for email {Email}. EmailSuspensions table may not exist yet.", email);
                // Continue without tracking if table doesn't exist
            }
        }

        private async Task ClearEmailSuspension(string email)
        {
            try
            {
                var suspension = await _context.EmailSuspensions
                    .FirstOrDefaultAsync(s => s.Email == email);
                
                if (suspension != null)
                {
                    suspension.IsActive = false;
                    suspension.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Email suspension cleared for {Email}", email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing email suspension for {Email}. EmailSuspensions table may not exist yet.", email);
                // Continue without clearing if table doesn't exist
            }
        }
    }

    public class SendOtpRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }
        
        [ValidateNever] // PhoneNumber is only required when SendVia is "SMS", so skip automatic validation
        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; }
        
        [JsonPropertyName("sendVia")]
        public string SendVia { get; set; } = "Email"; // "Email" or "SMS"
    }

    public class VerifyOtpRequest
    {
        public string Email { get; set; }
        public string Otp { get; set; }
    }
} 