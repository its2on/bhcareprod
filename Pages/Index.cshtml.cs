using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Barangay.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Barangay.Services;
using Barangay.Extensions;
using System.Linq;

namespace Barangay.Pages
{
    public class IndexModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<IndexModel> _logger;
        private readonly IOTPService _otpService;
        private readonly IEmailService _emailService;
        private readonly IDataEncryptionService _encryptionService;

        public IndexModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<IndexModel> logger,
            IOTPService otpService,
            IEmailService emailService,
            IDataEncryptionService encryptionService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _otpService = otpService;
            _emailService = emailService;
            _encryptionService = encryptionService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me")]
            public bool RememberMe { get; set; }

            [Display(Name = "OTP Code")]
            public string? OTPCode { get; set; }
        }

        [BindProperty]
        public bool ShowOTPField { get; set; } = false;

        [BindProperty]
        public bool OTPRequired { get; set; } = false;

        [BindProperty]
        public string? UserEmail { get; set; }

        public void OnGet()
        {
            // This method is called when the page is initially loaded
        }

        private async Task<ApplicationUser?> FindUserAsync(string emailOrUsername)
        {
            // First try the standard methods
            var user = await _userManager.FindByEmailAsync(emailOrUsername);
            if (user == null)
            {
                _logger.LogInformation($"User not found by email {emailOrUsername}, trying username lookup");
                user = await _userManager.FindByNameAsync(emailOrUsername);
            }
            
            // If still not found and it looks like an email, try searching through all users for encrypted emails
            if (user == null && emailOrUsername.Contains("@"))
            {
                _logger.LogInformation($"User not found by standard methods, searching through encrypted emails for: {emailOrUsername}");
                
                // Normalize the email for comparison
                var normalizedEmail = emailOrUsername.ToUpperInvariant();
                
                // Get all users and check their encrypted emails
                var allUsers = _userManager.Users.ToList();
                foreach (var candidateUser in allUsers)
                {
                    try
                    {
                        // Check both Email and NormalizedEmail fields
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
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Error processing user {candidateUser.Id}");
                        // Continue to next user
                    }
                }
            }
            
            return user;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Ensure Email and Password are not null before attempting sign-in
                    if (string.IsNullOrEmpty(Input.Email) || string.IsNullOrEmpty(Input.Password))
                    {
                        ErrorMessage = "Email or password cannot be empty.";
                        return Page();
                    }

                    var user = await FindUserAsync(Input.Email);
                    
                    if (user == null)
                    {
                        // Log the issue but show generic error to user
                        _logger.LogWarning($"Login attempt failed: User with email/username {Input.Email} not found.");
                        ErrorMessage = "Invalid email or password.";
                        return Page();
                    }

                    // Check if user is trying to use admin login on home page - CHECK IMMEDIATELY
                    var userRoles = await _userManager.GetRolesAsync(user);
                    if (userRoles.Contains("Admin") || userRoles.Contains("Admin Staff"))
                    {
                        _logger.LogWarning($"Admin user {user.Email} attempted to use home page login - ACCESS DENIED");
                        ErrorMessage = "ACCESS DENIED: Admin users must use the Admin Login page. Please go to the Login page and use the 'Admin Login Only' button.";
                        return Page();
                    }

                    // Enhanced logging to diagnose user account state
                    _logger.LogInformation(
                        $"Regular user found: ID={user.Id}, Email={user.Email}, UserName={user.UserName}, " +
                        $"NormalizedEmail={user.NormalizedEmail}, NormalizedUserName={user.NormalizedUserName}, " +
                        $"Status={user.Status}, EncryptedStatus={user.EncryptedStatus}, " +
                        $"EmailConfirmed={user.EmailConfirmed}, LockoutEnabled={user.LockoutEnabled}");

                    // Check if user account is approved - check both Status and EncryptedStatus
                    if (user.Status == "Pending" || user.EncryptedStatus == "Pending")
                    {
                        ErrorMessage = "Your account is pending approval by an administrator. Please check back later.";
                        return Page();
                    }

                    // First try direct password verification
                    var passwordCheck = await _userManager.CheckPasswordAsync(user, Input.Password);
                    _logger.LogInformation($"Direct password check result: {passwordCheck}");

                    if (!passwordCheck)
                    {
                        _logger.LogWarning($"Password verification failed for user {user.Email}");
                        ErrorMessage = "Invalid email or password.";
                        return Page();
                    }

                    // Check if OTP is required for this user
                    // Use Input.Email (the decrypted email from login form) for OTP check
                    var userEmail = Input.Email;
                    var isOTPRequired = await _otpService.IsOTPRequiredAsync(userEmail);
                    
                    if (isOTPRequired)
                    {
                        _logger.LogInformation($"OTP required for Gmail user: {userEmail}");
                        
                        // Generate and send OTP
                        var otp = await _otpService.GenerateOTPAsync(userEmail);
                        var emailSent = await _emailService.SendOTPEmailAsync(userEmail, otp);
                        
                        if (emailSent)
                        {
                            // Redirect to OTP verification page
                            return RedirectToPage("/Account/OTPVerification", new { 
                                email = userEmail, 
                                password = Input.Password, 
                                rememberMe = Input.RememberMe 
                            });
                        }
                        else
                        {
                            ErrorMessage = "Failed to send OTP. Please try again later.";
                            return Page();
                        }
                    }

                    // Since password is correct, ensure the user can log in
                    if (user.LockoutEnd != null && user.LockoutEnd > System.DateTimeOffset.Now)
                    {
                        _logger.LogWarning($"Removing lockout for user {user.Email}");
                        await _userManager.SetLockoutEndDateAsync(user, null);
                        await _userManager.ResetAccessFailedCountAsync(user);
                    }

                    // Try to sign in with username first
                    var signInIdentifier = user.UserName;
                    
                    _logger.LogInformation($"Attempting sign in with identifier: {signInIdentifier}");
                    var result = await _signInManager.PasswordSignInAsync(signInIdentifier, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                    
                    if (!result.Succeeded && !string.IsNullOrEmpty(user.Email))
                    {
                        _logger.LogInformation($"Username sign-in failed, trying email: {user.Email}");
                        result = await _signInManager.PasswordSignInAsync(user.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                    }

                    _logger.LogInformation($"Final sign-in result: {result.Succeeded}");

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User logged in successfully.");
                        
                        // Redirect based on role
                        if (await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            return RedirectToPage("/Admin/AdminDashboard");
                        }
                        else if (await _userManager.IsInRoleAsync(user, "Admin Staff"))
                        {
                            _logger.LogInformation("Redirecting Admin Staff user to dashboard");
                            return RedirectToPage("/AdminStaff/Dashboard");
                        }
                        else if (await _userManager.IsInRoleAsync(user, "Doctor"))
                        {
                            return RedirectToPage("/Doctor/DoctorDashboard");
                        }
                        else if (await _userManager.IsInRoleAsync(user, "Nurse") || await _userManager.IsInRoleAsync(user, "Head Nurse"))
                        {
                            _logger.LogInformation("Redirecting Nurse to Dashboard");
                            return RedirectToPage("/Nurse/NurseDashboard");
                        }
                        else 
                        {
                            return RedirectToPage("/User/UserDashboard");
                        }
                    }
                    
                    if (result.RequiresTwoFactor)
                    {
                        return RedirectToPage("/Account/LoginWith2fa", new { RememberMe = Input.RememberMe });
                    }
                    
                    if (result.IsLockedOut)
                    {
                        _logger.LogWarning("User account locked out.");
                        return RedirectToPage("/Account/Lockout");
                    }

                    // If we get here, something went wrong with the sign in process
                    _logger.LogWarning($"Login failed for {user.Email} with correct password but sign-in failed");
                    ErrorMessage = "Login failed. Please try again.";
                    return Page();
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Error during login process");
                    ErrorMessage = "An error occurred during login. Please try again later.";
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}

