using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using Barangay.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Barangay.Pages.Admin
{
    [Authorize(Policy = "AccessAdminDashboard")]
    public class UserDetailsModel : AdminPageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserDetailsModel> _logger;
        private readonly IDataEncryptionService _encryptionService;
        private readonly IEmailService _emailService;
        
        public UserDetailsModel(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            ILogger<UserDetailsModel> logger,
            IDataEncryptionService encryptionService,
            IEmailService emailService) 
            : base(notificationService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _encryptionService = encryptionService;
            _emailService = emailService;
        }
        
        [BindProperty(SupportsGet = true)]
        public string Id { get; set; }
        
        public ApplicationUser UserDetails { get; set; }
        public List<UserDocument> UserDocuments { get; set; } = new();
        public GuardianInformation Guardian { get; set; }
        public List<string> UserRoles { get; set; } = new();
        public bool IsMinor { get; set; }
        public string ErrorMessage { get; set; }
        
        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Id))
            {
                ErrorMessage = "No user ID provided";
                return Page();
            }
            
            // Load user with includes
            UserDetails = await _context.Users
                .Include(u => u.UserDocuments)
                .FirstOrDefaultAsync(u => u.Id == Id);
                
            if (UserDetails == null)
            {
                ErrorMessage = "User not found";
                return Page();
            }

            // Decrypt user data for authorized users
            UserDetails = UserDetails.DecryptSensitiveData(_encryptionService, User);
            
            // Manually decrypt Email since it's not marked with [Encrypted] attribute
            if (!string.IsNullOrEmpty(UserDetails.Email) && _encryptionService.IsEncrypted(UserDetails.Email))
            {
                UserDetails.Email = UserDetails.Email.DecryptForUser(_encryptionService, User);
            }
            
            // Manually decrypt PhoneNumber since it's not marked with [Encrypted] attribute
            if (!string.IsNullOrEmpty(UserDetails.PhoneNumber) && _encryptionService.IsEncrypted(UserDetails.PhoneNumber))
            {
                UserDetails.PhoneNumber = UserDetails.PhoneNumber.DecryptForUser(_encryptionService, User);
            }
            
            // Load user documents
            UserDocuments = UserDetails.UserDocuments?.ToList() ?? new List<UserDocument>();
            
            // Sync document status with user status if needed
            if (UserDetails.Status == "Verified" && UserDocuments.Any(d => d.Status != "Verified"))
            {
                foreach (var doc in UserDocuments.Where(d => d.Status != "Verified"))
                {
                    doc.Status = "Verified";
                    doc.ApprovedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
            }
            
            // Load user roles
            UserRoles = (await _userManager.GetRolesAsync(UserDetails)).ToList();
            
            // Check if user is a minor
            IsMinor = CalculateAge() < 18;
            
            // If minor, try to get guardian information
            if (IsMinor)
            {
                try
                {
                    Guardian = await _context.GuardianInformation
                        .AsNoTracking()
                        .FirstOrDefaultAsync(g => g.UserId == Id);
                    
                    // Decrypt guardian names if they exist and are encrypted
                    if (Guardian != null)
                    {
                        if (!string.IsNullOrEmpty(Guardian.GuardianFirstName) && _encryptionService.IsEncrypted(Guardian.GuardianFirstName))
                        {
                            Guardian.GuardianFirstName = _encryptionService.Decrypt(Guardian.GuardianFirstName);
                        }
                        if (!string.IsNullOrEmpty(Guardian.GuardianLastName) && _encryptionService.IsEncrypted(Guardian.GuardianLastName))
                        {
                            Guardian.GuardianLastName = _encryptionService.Decrypt(Guardian.GuardianLastName);
                        }
                    }
                    
                    _logger.LogInformation($"Loaded guardian information for user {Id}: {Guardian != null}");
                }
                catch (Exception ex)
                {
                    // Handle exception but continue loading the page
                    _logger.LogError(ex, $"Error loading guardian information for user {Id}");
                    Guardian = null;
                }
            }
            
            return Page();
        }
        
        public async Task<JsonResult> OnPostApproveAsync(string id)
        {
            try
            {
                _logger.LogInformation($"Attempting to approve user with ID: {id}");
                
                // Find the user to approve
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning($"User not found with ID: {id}");
                    return new JsonResult(new { success = false, message = "User not found." });
                }
                
                // Get current admin
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return new JsonResult(new { success = false, message = "Admin user not found." });
                }
                
                // Update user status
                user.Status = "Verified";
                user.IsActive = true;
                
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    string errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    return new JsonResult(new { success = false, message = $"Failed to update user: {errors}" });
                }
                
                // Update document status if exists
                var documents = await _context.UserDocuments
                    .Where(d => d.UserId == id)
                    .ToListAsync();
                    
                foreach (var document in documents)
                {
                    document.Status = "Verified";
                    document.ApprovedAt = DateTime.UtcNow;
                    document.ApprovedBy = currentUser.Id;
                }
                
                // Update guardian consent status if user is a minor
                var guardianInfo = await _context.GuardianInformation
                    .FirstOrDefaultAsync(g => g.UserId == id);
                    
                if (guardianInfo != null)
                {
                    guardianInfo.ConsentStatus = "Approved";
                    _context.GuardianInformation.Update(guardianInfo);
                    _logger.LogInformation($"Guardian consent approved for user {id}");
                }
                
                // Assign patient role if needed
                if (!await _userManager.IsInRoleAsync(user, "PATIENT"))
                {
                    await _userManager.AddToRoleAsync(user, "PATIENT");
                }
                
                // Save changes
                await _context.SaveChangesAsync();
                
                // Create notification for user
                await _notificationService.CreateNotificationForUserAsync(
                    userId: id,
                    title: "Account Approved",
                    message: "Your account has been approved.",
                    type: "Success",
                    link: "https://bhcare.software"
                );
                
                // Send approval email notification
                try
                {
                    // Decrypt user name components
                    var decryptedFirstName = !string.IsNullOrEmpty(user.FirstName) && _encryptionService.IsEncrypted(user.FirstName) 
                        ? _encryptionService.Decrypt(user.FirstName) 
                        : user.FirstName;
                    var decryptedLastName = !string.IsNullOrEmpty(user.LastName) && _encryptionService.IsEncrypted(user.LastName) 
                        ? _encryptionService.Decrypt(user.LastName) 
                        : user.LastName;
                    
                    var userName = $"{decryptedFirstName} {decryptedLastName}".Trim();
                    var userEmail = user.Email;
                    
                    // Decrypt email if it's encrypted
                    if (!string.IsNullOrEmpty(userEmail) && _encryptionService.IsEncrypted(userEmail))
                    {
                        userEmail = _encryptionService.Decrypt(userEmail);
                    }
                    
                    if (!string.IsNullOrEmpty(userEmail) && userEmail.Contains("@"))
                    {
                        var emailSubject = "Account Approved - Baesa Health Care";
                        var emailBody = GenerateApprovalEmailBody(userName, userEmail);
                        
                        await _emailService.SendEmailAsync(userEmail, emailSubject, emailBody);
                        _logger.LogInformation($"Approval email sent successfully to {userEmail}");
                    }
                    else
                    {
                        _logger.LogWarning($"Cannot send approval email - user email is null, empty, or invalid for user ID: {id}. Email: {userEmail}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send approval email to user {user.Email}. Approval process continues.");
                    // Don't fail the approval process if email fails
                }
                
                return new JsonResult(new { success = true, message = "User approved successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving user: {UserId}", id);
                return new JsonResult(new { success = false, message = "An error occurred while approving the user." });
            }
        }
        
        public async Task<JsonResult> OnPostApproveGuardianConsentAsync(int guardianId)
        {
            try
            {
                var guardianInfo = await _context.GuardianInformation
                    .FirstOrDefaultAsync(g => g.GuardianId == guardianId);
                    
                if (guardianInfo == null)
                {
                    return new JsonResult(new { success = false, message = "Guardian information not found." });
                }
                
                guardianInfo.ConsentStatus = "Approved";
                _context.GuardianInformation.Update(guardianInfo);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Guardian consent {guardianId} approved manually");
                
                return new JsonResult(new { success = true, message = "Guardian consent approved successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving guardian consent {guardianId}");
                return new JsonResult(new { success = false, message = "An error occurred while approving guardian consent." });
            }
        }
        
        public async Task<JsonResult> OnPostRejectAsync(string id)
        {
            try
            {
                // Find the user to reject
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return new JsonResult(new { success = false, message = "User not found." });
                }
                
                // Handle suspension logic
                var suspensionResult = await HandleUserSuspension(id, user);
                
                // Update user status
                user.Status = "Rejected";
                var updateResult = await _userManager.UpdateAsync(user);
                
                if (!updateResult.Succeeded)
                {
                    string errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    return new JsonResult(new { success = false, message = $"Failed to update user: {errors}" });
                }
                
                // Update document status if exists
                var documents = await _context.UserDocuments
                    .Where(d => d.UserId == id)
                    .ToListAsync();
                    
                foreach (var document in documents)
                {
                    document.Status = "Rejected";
                }
                
                await _context.SaveChangesAsync();
                
                // Create notification for user
                await _notificationService.CreateNotificationForUserAsync(
                    userId: id,
                    title: "Account Rejected",
                    message: suspensionResult.IsSuspended 
                        ? $"Your account has been rejected and suspended for {suspensionResult.SuspensionPeriod}. You cannot reapply until {suspensionResult.SuspensionEndDate:MMM dd, yyyy}."
                        : "Your account verification was not approved.",
                    type: "Danger",
                    link: "/Index"
                );
                
                // Send rejection email notification with suspension info
                try
                {
                    var userName = $"{user.FirstName} {user.LastName}".Trim();
                    var userEmail = user.Email;
                    
                    // Decrypt email if it's encrypted
                    if (!string.IsNullOrEmpty(userEmail) && _encryptionService.IsEncrypted(userEmail))
                    {
                        userEmail = _encryptionService.Decrypt(userEmail);
                    }
                    
                    if (!string.IsNullOrEmpty(userEmail) && userEmail.Contains("@"))
                    {
                        var emailSubject = "Account Application Status - Baesa Health Care";
                        var emailBody = suspensionResult.IsSuspended 
                            ? GenerateSuspensionEmailBody(userName, userEmail, suspensionResult.DenialCount, suspensionResult.SuspensionPeriod, suspensionResult.SuspensionEndDate)
                            : GenerateRejectionEmailBody(userName, userEmail);
                        
                        await _emailService.SendEmailAsync(userEmail, emailSubject, emailBody);
                        _logger.LogInformation($"Rejection email sent successfully to {userEmail}");
                    }
                    else
                    {
                        _logger.LogWarning($"Cannot send rejection email - user email is null, empty, or invalid for user ID: {id}. Email: {userEmail}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send rejection email to user {user.Email}. Rejection process continues.");
                    // Don't fail the rejection process if email fails
                }
                
                var message = suspensionResult.IsSuspended 
                    ? $"User rejected and suspended for {suspensionResult.SuspensionPeriod}. Denial count: {suspensionResult.DenialCount}"
                    : "User rejected successfully.";
                
                return new JsonResult(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting user: {UserId}", id);
                return new JsonResult(new { success = false, message = "An error occurred while rejecting the user." });
            }
        }
        
        public int CalculateAge()
        {
            // Check if UserDetails is null or has no birth date
            if (UserDetails == null) 
                return 0;
            
            DateTime? birthDateNullable = UserDetails.BirthDate;
            if (birthDateNullable == null) 
                return 0;
                
            DateTime birthDate = birthDateNullable.Value;
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            
            // Adjust age if birthday hasn't occurred yet this year
            if (birthDate.Date > today.AddYears(-age))
                age--;
                
            return age;
        }
        
        public string GetStatusBadgeClass()
        {
            if (UserDetails == null)
                return "secondary";
                
            string status = UserDetails.Status;
            if (string.IsNullOrEmpty(status))
                return "secondary";
                
            return status.ToLower() switch
            {
                "verified" => "success",
                "pending" => "warning",
                "rejected" => "danger",
                "inactive" => "secondary",
                _ => "secondary"
            };
        }
        
        public string GetBirthDateAsString()
        {
            if (UserDetails == null)
                return "";
                
            DateTime? birthDateNullable = UserDetails.BirthDate;
            if (birthDateNullable == null)
                return "";
                
            return birthDateNullable.Value.ToString("MM/dd/yyyy");
        }
        
        public string GetLastActiveAsString()
        {
            if (UserDetails == null)
                return "";
                
            DateTime? lastActiveNullable = UserDetails.LastActive;
            if (lastActiveNullable == null)
                return "";
                
            return lastActiveNullable.Value.ToString("MM/dd/yyyy");
        }
        
        public string GetUserTypeDisplay()
        {
            if (UserDetails == null)
                return "Standard";
                
            // Use ToString() directly since UserType is not nullable
            string userTypeString = UserDetails.UserType.ToString();
            return string.IsNullOrEmpty(userTypeString) ? "Standard" : userTypeString;
        }

        private string GenerateApprovalEmailBody(string userName, string userEmail)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #ffffff; padding: 30px; border: 1px solid #dee2e6; }}
                        .success-badge {{ background-color: #d4edda; color: #155724; padding: 10px; border-radius: 5px; text-align: center; margin: 20px 0; }}
                        .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; border-radius: 0 0 5px 5px; font-size: 14px; color: #6c757d; }}
                        .cta-button {{ display: inline-block; background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>✅ Account Approved - Baesa Health Care</h2>
                        </div>
                        
                        <div class='content'>
                            <h3>Congratulations, {userName}!</h3>
                            
                            <div class='success-badge'>
                                <strong>🎉 Your account has been approved and verified!</strong>
                            </div>
                            
                            <p>We are pleased to inform you that your Baesa Health Care account has been successfully approved. You can now access all the features and services available in our system.</p>
                            
                            <p><strong>What you can do now:</strong></p>
                            <ul>
                                <li>✅ Access your personal health dashboard</li>
                                <li>✅ Schedule medical appointments</li>
                                <li>✅ View your medical records</li>
                                <li>✅ Request health services</li>
                                <li>✅ Receive health notifications</li>
                            </ul>
                            
                            <p>To get started, please log in to your account using your registered email: <strong>{userEmail}</strong></p>
                            
                            <div style='text-align: center;'>
                                <a href='https://bhcare.software/' class='cta-button'>Login to Your Account</a>
                            </div>
                            
                            <p>If you have any questions or need assistance, please don't hesitate to contact our support team.</p>
                            
                            <p>Thank you for choosing Baesa Health Care for your health needs!</p>
                        </div>
                        
                        <div class='footer'>
                            <p>This is an automated message from Baesa Health Care System</p>
                            <p>Please do not reply to this email</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string GenerateRejectionEmailBody(string userName, string userEmail, string reason = "")
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #ffffff; padding: 30px; border: 1px solid #dee2e6; }}
                        .warning-badge {{ background-color: #f8d7da; color: #721c24; padding: 10px; border-radius: 5px; text-align: center; margin: 20px 0; }}
                        .guide-section {{ background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #007bff; }}
                        .step {{ margin: 15px 0; padding: 10px; background-color: white; border-radius: 5px; border-left: 3px solid #28a745; }}
                        .step-number {{ background-color: #007bff; color: white; border-radius: 50%; width: 25px; height: 25px; display: inline-flex; align-items: center; justify-content: center; font-weight: bold; margin-right: 10px; }}
                        .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; border-radius: 0 0 5px 5px; font-size: 14px; color: #6c757d; }}
                        .cta-button {{ display: inline-block; background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 10px 5px; }}
                        .cta-button.secondary {{ background-color: #6c757d; }}
                        .requirements {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 15px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>❌ Account Application Status - Baesa Health Care</h2>
                        </div>
                        
                        <div class='content'>
                            <h3>Dear {userName},</h3>
                            
                            <div class='warning-badge'>
                                <strong>⚠️ Your account application requires attention</strong>
                            </div>
                            
                            <p>We regret to inform you that your Baesa Health Care account application has been reviewed and requires additional information or documentation.</p>
                            
                            {(string.IsNullOrEmpty(reason) ? "" : $"<p><strong>Reason:</strong> {reason}</p>")}
                            
                            <div class='guide-section'>
                                <h4 style='color: #007bff; margin-top: 0;'>📋 Step-by-Step Guide to Get Approved</h4>
                                
                                <div class='step'>
                                    <span class='step-number'>1</span>
                                    <strong>Review Your Personal Information</strong>
                                    <ul style='margin: 10px 0 0 35px;'>
                                        <li>Ensure your full name matches your government ID</li>
                                        <li>Verify your birth date is correct</li>
                                        <li>Check that your contact information is accurate</li>
                                        <li>Confirm your address is complete and valid</li>
                                    </ul>
                                </div>
                                
                                <div class='step'>
                                    <span class='step-number'>2</span>
                                    <strong>Upload Clear Identity Documents</strong>
                                    <ul style='margin: 10px 0 0 35px;'>
                                        <li>Use a valid government-issued ID (Driver's License, Passport, National ID, etc.)</li>
                                        <li>Ensure the document is not expired</li>
                                        <li>Take clear, well-lit photos of your ID</li>
                                        <li>Make sure all text is readable and not blurry</li>
                                        <li>Avoid shadows, glare, or reflections</li>
                                    </ul>
                                </div>
                                
                                <div class='step'>
                                    <span class='step-number'>3</span>
                                    <strong>Provide Residency Proof</strong>
                                    <ul style='margin: 10px 0 0 35px;'>
                                        <li>Upload a recent utility bill (electricity, water, internet)</li>
                                        <li>Bank statement with your current address</li>
                                        <li>Barangay certificate of residency</li>
                                        <li>Lease agreement or property title</li>
                                        <li>Document must be dated within the last 3 months</li>
                                    </ul>
                                </div>
                                
                                <div class='step'>
                                    <span class='step-number'>4</span>
                                    <strong>Complete All Required Fields</strong>
                                    <ul style='margin: 10px 0 0 35px;'>
                                        <li>Fill out all mandatory information</li>
                                        <li>Provide accurate emergency contact details</li>
                                        <li>Answer all health-related questions honestly</li>
                                        <li>Agree to terms and conditions</li>
                                    </ul>
                                </div>
                                
                                <div class='step'>
                                    <span class='step-number'>5</span>
                                    <strong>Submit for Review</strong>
                                    <ul style='margin: 10px 0 0 35px;'>
                                        <li>Double-check all information before submitting</li>
                                        <li>Ensure all documents are properly uploaded</li>
                                        <li>Wait for our team to review your application</li>
                                        <li>You will receive an email notification once reviewed</li>
                                    </ul>
                                </div>
                            </div>
                            
                            <div class='requirements'>
                                <h5 style='color: #856404; margin-top: 0;'>📄 Document Requirements Checklist</h5>
                                <ul style='margin: 10px 0;'>
                                    <li>✅ Valid government-issued ID (not expired)</li>
                                    <li>✅ Clear, readable photo of your ID</li>
                                    <li>✅ Recent residency proof (within 3 months)</li>
                                    <li>✅ Complete personal information</li>
                                    <li>✅ Accurate contact details</li>
                                    <li>✅ Emergency contact information</li>
                                </ul>
                            </div>
                            
                            <p><strong>Common Reasons for Rejection:</strong></p>
                            <ul>
                                <li>📷 Blurry or unclear document photos</li>
                                <li>📅 Expired identification documents</li>
                                <li>🏠 Missing or outdated residency proof</li>
                                <li>✏️ Incomplete or inaccurate personal information</li>
                                <li>📞 Invalid or missing contact information</li>
                            </ul>
                            
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='https://your-domain.com/Account/Register' class='cta-button'>🔄 Reapply Now</a>
                                <a href='https://your-domain.com/Contact' class='cta-button secondary'>📞 Contact Support</a>
                            </div>
                            
                            <p><strong>Need Help?</strong> Our support team is here to assist you. Contact us at:</p>
                            <ul>
                                <li>📧 Email: bhcare@barangay161.ph</li>
                                <li>📞 Phone: (02) 8123-4567</li>
                                <li>🏢 Visit: Barangay 161 Health Center</li>
                                <li>⏰ Hours: Monday-Friday, 8:00 AM - 5:00 PM</li>
                            </ul>
                            
                            <p>We appreciate your interest in using our health care services and look forward to assisting you once your application is complete.</p>
                        </div>
                        
                        <div class='footer'>
                            <p>This is an automated message from Baesa Health Care System</p>
                            <p>Please do not reply to this email</p>
                        </div>
                    </div>
                </body>
                </html>";
        }
        
        private async Task<SuspensionResult> HandleUserSuspension(string userId, ApplicationUser user)
        {
            try
            {
                // Get or create suspension record
                var suspension = await _context.UserSuspensions
                    .FirstOrDefaultAsync(s => s.UserId == userId);
                
                if (suspension == null)
                {
                    suspension = new UserSuspension
                    {
                        UserId = userId,
                        DenialCount = 0,
                        LastDenialDate = DateTime.UtcNow,
                        IsActive = false
                    };
                    _context.UserSuspensions.Add(suspension);
                }
                
                // Increment denial count
                suspension.DenialCount++;
                suspension.LastDenialDate = DateTime.UtcNow;
                suspension.UpdatedAt = DateTime.UtcNow;
                
                // Determine suspension based on denial count
                var result = new SuspensionResult
                {
                    DenialCount = suspension.DenialCount,
                    IsSuspended = false
                };
                
                if (suspension.DenialCount >= 3)
                {
                    suspension.IsActive = true;
                    suspension.SuspensionStartDate = DateTime.UtcNow;
                    
                    if (suspension.DenialCount == 3)
                    {
                        // 24 hours suspension
                        suspension.SuspensionEndDate = DateTime.UtcNow.AddHours(24);
                        suspension.SuspensionLevel = "24h";
                        suspension.SuspensionReason = "3 denials - 24 hour suspension";
                        result.SuspensionPeriod = "24 hours";
                    }
                    else if (suspension.DenialCount == 5)
                    {
                        // 3 days suspension
                        suspension.SuspensionEndDate = DateTime.UtcNow.AddDays(3);
                        suspension.SuspensionLevel = "3d";
                        suspension.SuspensionReason = "5 denials - 3 day suspension";
                        result.SuspensionPeriod = "3 days";
                    }
                    else if (suspension.DenialCount >= 10)
                    {
                        // 1 month suspension
                        suspension.SuspensionEndDate = DateTime.UtcNow.AddMonths(1);
                        suspension.SuspensionLevel = "1m";
                        suspension.SuspensionReason = "10+ denials - 1 month suspension";
                        result.SuspensionPeriod = "1 month";
                    }
                    
                    result.IsSuspended = true;
                    result.SuspensionEndDate = suspension.SuspensionEndDate ?? DateTime.UtcNow;
                }
                
                await _context.SaveChangesAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling user suspension for user {UserId}", userId);
                return new SuspensionResult
                {
                    DenialCount = 1,
                    IsSuspended = false
                };
            }
        }
        
        private string GenerateSuspensionEmailBody(string userName, string userEmail, int denialCount, string suspensionPeriod, DateTime suspensionEndDate)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Account Suspended - Barangay Health Care</title>
                <style>
                    body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }}
                    .container {{ max-width: 600px; margin: 0 auto; background: white; padding: 20px; border-radius: 10px; box-shadow: 0 0 20px rgba(0,0,0,0.1); }}
                    .header {{ background: linear-gradient(135deg, #dc3545, #c82333); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; margin: -20px -20px 30px -20px; }}
                    .header h1 {{ margin: 0; font-size: 28px; font-weight: 300; }}
                    .content {{ padding: 0 20px; }}
                    .alert {{ background: #fff3cd; border: 1px solid #ffeaa7; color: #856404; padding: 20px; border-radius: 8px; margin: 20px 0; }}
                    .suspension-info {{ background: #f8d7da; border: 1px solid #f5c6cb; color: #721c24; padding: 20px; border-radius: 8px; margin: 20px 0; }}
                    .suspension-info h3 {{ margin-top: 0; color: #721c24; }}
                    .count {{ font-size: 24px; font-weight: bold; color: #dc3545; }}
                    .period {{ font-size: 20px; font-weight: bold; color: #dc3545; }}
                    .end-date {{ font-size: 18px; font-weight: bold; color: #6c757d; }}
                    .guide-section {{ background: #e7f3ff; border-left: 4px solid #007bff; padding: 20px; margin: 20px 0; }}
                    .step {{ margin: 15px 0; padding: 15px; background: white; border-radius: 8px; border-left: 3px solid #007bff; }}
                    .step-number {{ background: #007bff; color: white; width: 25px; height: 25px; border-radius: 50%; display: inline-block; text-align: center; line-height: 25px; margin-right: 10px; font-weight: bold; }}
                    .requirements {{ background: #d4edda; border: 1px solid #c3e6cb; color: #155724; padding: 20px; border-radius: 8px; margin: 20px 0; }}
                    .cta-button {{ display: inline-block; background: #007bff; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; margin: 10px 5px; font-weight: bold; }}
                    .cta-button.secondary {{ background: #6c757d; }}
                    .footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; color: #666; font-size: 14px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>🚫 Account Suspended</h1>
                        <p>Barangay Health Care System</p>
                    </div>
                    
                    <div class='content'>
                        <p>Dear <strong>{userName}</strong>,</p>
                        
                        <div class='suspension-info'>
                            <h3>⚠️ Account Suspension Notice</h3>
                            <p>Your account has been <strong>suspended</strong> due to multiple application rejections.</p>
                            
                            <div style='text-align: center; margin: 20px 0;'>
                                <div class='count'>Denial Count: {denialCount}</div>
                                <div class='period'>Suspension Period: {suspensionPeriod}</div>
                                <div class='end-date'>Suspension Ends: {suspensionEndDate:MMMM dd, yyyy 'at' h:mm tt}</div>
                            </div>
                            
                            <p><strong>You cannot submit new applications until your suspension period ends.</strong></p>
                        </div>
                        
                        <div class='alert'>
                            <h4>📋 Why Was My Account Suspended?</h4>
                            <p>Your account was suspended because your application has been rejected <strong>{denialCount} times</strong>. This is to ensure the quality and integrity of our health care system.</p>
                            
                            <p><strong>Suspension Schedule:</strong></p>
                            <ul>
                                <li>3 rejections = 24-hour suspension</li>
                                <li>5 rejections = 3-day suspension</li>
                                <li>10+ rejections = 1-month suspension</li>
                            </ul>
                        </div>
                        
                        <div class='guide-section'>
                            <h4 style='color: #007bff; margin-top: 0;'>📋 How to Get Approved After Suspension</h4>
                            
                            <div class='step'>
                                <span class='step-number'>1</span>
                                <strong>Wait for Suspension to End</strong>
                                <p>Your suspension will automatically end on <strong>{suspensionEndDate:MMMM dd, yyyy}</strong>. You will receive an email notification when you can reapply.</p>
                            </div>
                            
                            <div class='step'>
                                <span class='step-number'>2</span>
                                <strong>Review Previous Rejections</strong>
                                <ul style='margin: 10px 0 0 35px;'>
                                    <li>Check your email for previous rejection reasons</li>
                                    <li>Identify common issues in your applications</li>
                                    <li>Prepare better documentation</li>
                                </ul>
                            </div>
                            
                            <div class='step'>
                                <span class='step-number'>3</span>
                                <strong>Prepare High-Quality Documents</strong>
                                <ul style='margin: 10px 0 0 35px;'>
                                    <li>Use a valid, non-expired government ID</li>
                                    <li>Take clear, well-lit photos of your documents</li>
                                    <li>Ensure all text is readable and not blurry</li>
                                    <li>Upload recent residency proof (within 3 months)</li>
                                </ul>
                            </div>
                            
                            <div class='step'>
                                <span class='step-number'>4</span>
                                <strong>Complete Application Carefully</strong>
                                <ul style='margin: 10px 0 0 35px;'>
                                    <li>Double-check all personal information</li>
                                    <li>Ensure contact details are accurate</li>
                                    <li>Provide complete emergency contact information</li>
                                    <li>Answer all questions honestly and completely</li>
                                </ul>
                            </div>
                            
                            <div class='step'>
                                <span class='step-number'>5</span>
                                <strong>Submit After Suspension Ends</strong>
                                <p>Once your suspension period ends, you can submit a new application. Make sure to follow all guidelines carefully to avoid further rejections.</p>
                            </div>
                        </div>
                        
                        <div class='requirements'>
                            <h5 style='color: #155724; margin-top: 0;'>📄 Required Documents Checklist</h5>
                            <ul style='margin: 10px 0;'>
                                <li>✅ Valid government-issued ID (not expired)</li>
                                <li>✅ Clear, readable photo of your ID</li>
                                <li>✅ Recent residency proof (within 3 months)</li>
                                <li>✅ Complete personal information</li>
                                <li>✅ Accurate contact details</li>
                                <li>✅ Emergency contact information</li>
                            </ul>
                        </div>
                        
                        <p><strong>Common Rejection Reasons:</strong></p>
                        <ul>
                            <li>📷 Blurry or unclear document photos</li>
                            <li>📅 Expired identification documents</li>
                            <li>🏠 Missing or outdated residency proof</li>
                            <li>✏️ Incomplete or inaccurate personal information</li>
                            <li>📞 Invalid or missing contact information</li>
                        </ul>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <p><strong>Suspension End Date:</strong> {suspensionEndDate:MMMM dd, yyyy 'at' h:mm tt}</p>
                            <p style='color: #6c757d; font-size: 14px;'>You can reapply after this date</p>
                        </div>
                        
                        <p><strong>Need Help?</strong> Our support team is here to assist you. Contact us at:</p>
                        <ul>
                            <li>📧 Email: bhcare@barangay161.ph</li>
                            <li>📞 Phone: (02) 8123-4567</li>
                            <li>🏢 Visit: Barangay 161 Health Center</li>
                            <li>⏰ Hours: Monday-Friday, 8:00 AM - 5:00 PM</li>
                        </ul>
                    </div>
                    
                    <div class='footer'>
                        <p>© 2025 - Barangay Health System | Barangay 161, Manila</p>
                        <p>This is an automated message. Please do not reply to this email.</p>
                    </div>
                </div>
            </body>
            </html>";
        }
    }
    
    public class SuspensionResult
    {
        public int DenialCount { get; set; }
        public bool IsSuspended { get; set; }
        public string SuspensionPeriod { get; set; } = string.Empty;
        public DateTime SuspensionEndDate { get; set; }
    }
} 