using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Barangay.Models;
using Microsoft.AspNetCore.Authentication;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Barangay.Services;
using Barangay.Extensions;
using System;

namespace Barangay.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly IOTPService _otpService;
        private readonly IEmailService _emailService;
        private readonly IDataEncryptionService _encryptionService;
        private readonly IAuditTrailService _auditTrail;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginModel> logger,
            IOTPService otpService,
            IEmailService emailService,
            IDataEncryptionService encryptionService,
            IAuditTrailService auditTrail)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _otpService = otpService;
            _emailService = emailService;
            _encryptionService = encryptionService;
            _auditTrail = auditTrail;
        }

        [BindProperty]
        [Required(ErrorMessage = "Email or Username is required")]
        [Display(Name = "Email or Username")]
        public string EmailOrUsername { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public bool RememberMe { get; set; }

        [BindProperty]
        [Display(Name = "OTP Code")]
        public string? OTPCode { get; set; }

        [BindProperty]
        public bool ShowOTPField { get; set; } = false;

        [BindProperty]
        public bool OTPRequired { get; set; } = false;

        [BindProperty]
        public string? UserEmail { get; set; }

        private async Task<ApplicationUser?> FindUserAsync(string emailOrUsername)
        {
            // First try the standard methods
            var user = await _userManager.FindByEmailAsync(emailOrUsername);
            if (user == null)
            {
                _logger.LogInformation($"User not found by email {emailOrUsername}, trying username lookup");
                user = await _userManager.FindByNameAsync(emailOrUsername);
            }
            
            // If still not found, try searching through all users for encrypted emails or direct username match
            if (user == null)
            {
                _logger.LogInformation($"User not found by standard methods, searching through all users for: {emailOrUsername}");
                
                // Normalize the email for comparison
                var normalizedEmail = emailOrUsername.ToUpperInvariant();
                
                // Get all users and check their encrypted emails
                var allUsers = _userManager.Users.ToList();
                foreach (var candidateUser in allUsers)
                {
                    try
                    {
                        // First try direct username match
                        if (string.Equals(candidateUser.UserName, emailOrUsername, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation($"Found user by direct username match: {candidateUser.Id}");
                            user = candidateUser;
                            break;
                        }
                        
                        // If it looks like an email, check encrypted emails
                        if (emailOrUsername.Contains("@"))
                        {
                            bool emailMatch = false;
                            
                            // Check Email field
                            if (!string.IsNullOrEmpty(candidateUser.Email))
                            {
                                if (_encryptionService.IsEncrypted(candidateUser.Email))
                                {
                                    try
                                    {
                                        var decryptedEmail = _encryptionService.Decrypt(candidateUser.Email);
                                        emailMatch = string.Equals(decryptedEmail, emailOrUsername, StringComparison.OrdinalIgnoreCase);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, $"Error decrypting Email for user {candidateUser.Id}");
                                    }
                                }
                                else
                                {
                                    // Email is not encrypted, compare directly
                                    emailMatch = string.Equals(candidateUser.Email, emailOrUsername, StringComparison.OrdinalIgnoreCase);
                                }
                            }
                            
                            // Check NormalizedEmail field if Email didn't match
                            if (!emailMatch && !string.IsNullOrEmpty(candidateUser.NormalizedEmail))
                            {
                                if (_encryptionService.IsEncrypted(candidateUser.NormalizedEmail))
                                {
                                    try
                                    {
                                        var decryptedNormalizedEmail = _encryptionService.Decrypt(candidateUser.NormalizedEmail);
                                        emailMatch = string.Equals(decryptedNormalizedEmail, normalizedEmail, StringComparison.OrdinalIgnoreCase);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, $"Error decrypting NormalizedEmail for user {candidateUser.Id}");
                                    }
                                }
                                else
                                {
                                    // NormalizedEmail is not encrypted, compare directly
                                    emailMatch = string.Equals(candidateUser.NormalizedEmail, normalizedEmail, StringComparison.OrdinalIgnoreCase);
                                }
                            }
                            
                            if (emailMatch)
                            {
                                _logger.LogInformation($"Found user by encrypted email match: {candidateUser.Id}");
                                user = candidateUser;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Error processing user {candidateUser.Id}");
                        // Continue to next user
                    }
                }
            }
            
            return user;
        }

        private IActionResult? GetDashboardRedirect(ApplicationUser user, IList<string> roles)
        {
            _logger.LogInformation("GetDashboardRedirect called for user {Email} with IsFirstLogin={IsFirstLogin}, Roles={Roles}", 
                user.Email, user.IsFirstLogin, string.Join(",", roles));
            
            // Note: First-login notifications will be handled in dashboard layouts
            // No forced redirect - better UX with dashboard notifications
            
            if (roles.Contains("Admin"))
            {
                return RedirectToPage("/Admin/AdminDashboard");
            }
            if (roles.Contains("Admin Staff"))
            {
                _logger.LogInformation("Redirecting Admin Staff user to dashboard");
                return RedirectToPage("/AdminStaff/Dashboard");
            }
            if (roles.Contains("Doctor"))
            {
                return RedirectToPage("/Doctor/DoctorDashboard");
            }
            if (roles.Contains("Nurse") || roles.Contains("Head Nurse"))
            {
                _logger.LogInformation("Redirecting Nurse to Dashboard");
                return RedirectToPage("/Nurse/NurseDashboard");
            }
            if (roles.Contains("User") || roles.Contains("Patient"))
            {
                if (user.Status == "Verified" && user.IsActive)
                {
                    return RedirectToPage("/User/UserDashboard");
                }
                return RedirectToPage("/Account/WaitingForApproval");
            }
            return null; // No role matched
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Ensure Email and Password are not null before attempting sign-in
                if (string.IsNullOrEmpty(EmailOrUsername) || string.IsNullOrEmpty(Password))
                {
                    ModelState.AddModelError(string.Empty, "Email or password cannot be empty.");
                    return Page();
                }

                var user = await FindUserAsync(EmailOrUsername);
                
                if (user == null)
                {
                    // Log the issue but show generic error to user
                    _logger.LogWarning($"Login attempt failed: User with email/username {EmailOrUsername} not found.");
                    
                    // AUDIT: Log failed login attempt - user not found
                    await _auditTrail.LogAsync(
                        "LoginFailed",
                        $"Failed login attempt: User not found",
                        "Authentication",
                        null,
                        null,
                        null,
                        $"Login attempt for non-existent user: {EmailOrUsername}"
                    );
                    
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                    return Page();
                }

                // Check if user is trying to use admin login on regular login page - CHECK IMMEDIATELY
                var userRoles = await _userManager.GetRolesAsync(user);
                if (userRoles.Contains("Admin") || userRoles.Contains("Admin Staff"))
                {
                    _logger.LogWarning($"Admin user {user.Email} attempted to use regular login page - ACCESS DENIED");
                    
                    // AUDIT: Log unauthorized login attempt on wrong portal
                    await _auditTrail.LogAsync(
                        "LoginFailed",
                        "Admin user attempted regular login portal",
                        "Authentication",
                        user.Id,
                        null,
                        null,
                        $"Admin user {user.Email} tried to access regular login portal - ACCESS DENIED"
                    );
                    
                    ModelState.AddModelError(string.Empty, "❌ ACCESS DENIED: Admin users must use the Admin Login page. Please use the 'Admin Login Only' button below.");
                    return Page();
                }

                // Enhanced logging to diagnose user account state
                _logger.LogInformation(
                    $"Regular user found: ID={user.Id}, Email={user.Email}, UserName={user.UserName}, " +
                    $"NormalizedEmail={user.NormalizedEmail}, NormalizedUserName={user.NormalizedUserName}, " +
                    $"Status={user.Status}, EncryptedStatus={user.EncryptedStatus}, " +
                    $"EmailConfirmed={user.EmailConfirmed}, LockoutEnabled={user.LockoutEnabled}, " +
                    $"LockoutEnd={user.LockoutEnd}, AccessFailedCount={user.AccessFailedCount}");

                // Check if user account is approved - check both Status and EncryptedStatus
                if (user.Status == "Pending" || user.EncryptedStatus == "Pending")
                {
                    ModelState.AddModelError(string.Empty, "Your account is pending approval by an administrator. Please check back later.");
                    return Page();
                }

                // First try direct password verification
                var passwordCheck = await _userManager.CheckPasswordAsync(user, Password);
                _logger.LogInformation($"Direct password check result: {passwordCheck}");

                if (!passwordCheck)
                {
                    _logger.LogWarning($"Password verification failed for user {user.Email}");
                    
                    // AUDIT: Log failed login attempt - invalid password
                    await _auditTrail.LogAsync(
                        "LoginFailed",
                        "Failed login attempt: Invalid password",
                        "Authentication",
                        user.Id,
                        null,
                        null,
                        $"User {user.Email} entered incorrect password"
                    );
                    
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                    return Page();
                }

                // Check if OTP is required for this user
                // Use EmailOrUsername (the decrypted email from login form) for OTP check
                var userEmail = EmailOrUsername;
                var isOTPRequired = await _otpService.IsOTPRequiredAsync(userEmail);
                
                if (isOTPRequired)
                {
                    // If OTP is required but not provided, redirect to OTPVerification page (classic flow)
                    if (string.IsNullOrEmpty(OTPCode))
                    {
                        _logger.LogInformation($"OTP required for user: {userEmail}");
                        
                        // Generate and send OTP
                        var otp = await _otpService.GenerateOTPAsync(userEmail);
                        var emailSent = await _emailService.SendOTPEmailAsync(userEmail, otp);
                        
                        if (emailSent)
                        {
                            return RedirectToPage("/Account/OTPVerification", new {
                                email = userEmail,
                                password = Password,
                                rememberMe = RememberMe
                            });
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Failed to send OTP. Please try again later.");
                            return Page();
                        }
                    }
                    else
                    {
                        // Validate the provided OTP before proceeding
                        var valid = await _otpService.ValidateOTPAsync(userEmail, OTPCode);
                        if (!valid)
                        {
                            _logger.LogWarning($"Invalid or expired OTP for user: {userEmail}");
                            ModelState.AddModelError(string.Empty, "Invalid or expired OTP. Please try again.");
                            OTPRequired = true;
                            UserEmail = userEmail;
                            return Page();
                        }
                        _logger.LogInformation("OTP validated successfully; proceeding with login");
                    }
                }

                // Since password is correct, ensure the user can log in
                if (user.LockoutEnd != null && user.LockoutEnd > System.DateTimeOffset.Now)
                {
                    _logger.LogWarning($"Removing lockout for user {user.Email}");
                    await _userManager.SetLockoutEndDateAsync(user, null);
                    await _userManager.ResetAccessFailedCountAsync(user);
                }

                if (!user.EmailConfirmed && (user.Status == "Approved" || user.EncryptedStatus == "Approved"))
                {
                    _logger.LogInformation($"Auto-confirming email for approved user {user.Email}");
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                }

                // Try to sign in with email if username doesn't match email
                var signInIdentifier = user.UserName;
                
                _logger.LogInformation($"Attempting sign in with identifier: {signInIdentifier}");
                var result = await _signInManager.PasswordSignInAsync(signInIdentifier, Password, RememberMe, lockoutOnFailure: false);
                
                if (!result.Succeeded && !string.IsNullOrEmpty(user.Email))
                {
                    _logger.LogInformation($"Username sign-in failed, trying email: {user.Email}");
                    result = await _signInManager.PasswordSignInAsync(user.Email, Password, RememberMe, lockoutOnFailure: false);
                }

                _logger.LogInformation($"Final sign-in result: {result.Succeeded}");

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in successfully.");
                    
                    var roles = await _userManager.GetRolesAsync(user);
                    _logger.LogInformation($"User roles: {string.Join(", ", roles)}");
                    _logger.LogInformation($"User IsFirstLogin: {user.IsFirstLogin}");
                    _logger.LogInformation($"User Email: {user.Email}, UserName: {user.UserName}");

                    // Log successful login to audit trail
                    await _auditTrail.LogAsync(
                        "Login",
                        "User logged in successfully",
                        "Authentication",
                        user.Id,
                        null,
                        null,
                        $"User {user.Email} logged into the system"
                    );

                    var claims = new List<Claim>
                    {
                        new Claim("UserId", user.Id),
                        new Claim("Status", user.Status),
                        new Claim("IsActive", user.IsActive.ToString())
                    };

                    var existingClaims = await _userManager.GetClaimsAsync(user);
                    await _userManager.RemoveClaimsAsync(user, existingClaims);
                    await _userManager.AddClaimsAsync(user, claims);

                    var redirectResult = GetDashboardRedirect(user, roles);
                    if (redirectResult != null)
                    {
                        return redirectResult;
                    }

                    await _signInManager.SignOutAsync();
                    ModelState.AddModelError(string.Empty, "The account does not have any assigned roles.");
                    return Page();
                }
                
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { RememberMe = RememberMe });
                }
                
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    
                    // AUDIT: Log account lockout event
                    await _auditTrail.LogAsync(
                        "LoginFailed",
                        "Login attempt blocked: Account locked out",
                        "Authentication",
                        user.Id,
                        null,
                        null,
                        $"User {user.Email} attempted login while account is locked out"
                    );
                    
                    return RedirectToPage("./Lockout");
                }

                // If we get here, something went wrong with the sign in process
                _logger.LogWarning($"Login failed for {user.Email} with correct password but sign-in failed");
                
                // AUDIT: Log unexpected login failure
                await _auditTrail.LogAsync(
                    "LoginFailed",
                    "Login failed: Sign-in process error",
                    "Authentication",
                    user.Id,
                    null,
                    null,
                    $"User {user.Email} login failed despite correct password - sign-in process error"
                );
                
                ModelState.AddModelError(string.Empty, "Login failed. Please try again.");
                return Page();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error during login process");
                ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again later.");
                return Page();
            }
        }

        // AJAX handler to (re)send OTP from the login page modal
        public async Task<IActionResult> OnPostRequestOtpAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return new JsonResult(new { success = false, message = "Email is required" });
                }

                var otp = await _otpService.GenerateOTPAsync(email);
                var sent = await _emailService.SendOTPEmailAsync(email, otp);
                if (sent)
                {
                    return new JsonResult(new { success = true });
                }
                return new JsonResult(new { success = false, message = "Failed to send OTP. Please try again later." });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP in RequestOtp handler for {Email}", email);
                return new JsonResult(new { success = false, message = "Server error while sending OTP" });
            }
        }
    }
}


