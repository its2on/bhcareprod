using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Barangay.Models;
using Barangay.Services;
using System.Security.Claims;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Barangay.Extensions;

namespace Barangay.Pages.Account
{
    public class OTPVerificationModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<OTPVerificationModel> _logger;
        private readonly IOTPService _otpService;
        private readonly IEmailService _emailService;
        private readonly IDataEncryptionService _encryptionService;

        public OTPVerificationModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<OTPVerificationModel> logger,
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
        [Required(ErrorMessage = "OTP Code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP Code must be exactly 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP Code must contain only numbers")]
        public string OTPCode { get; set; } = string.Empty;

        [BindProperty]
        public string UserEmail { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public bool RememberMe { get; set; }

        [BindProperty]
        public bool IsAdmin { get; set; } = false;

        public string? UserPhoneNumber { get; set; }

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

        public async Task<IActionResult> OnGetAsync(string email, string password, bool rememberMe = false, bool isAdmin = false)
        {
            try
            {
                // Validate required parameters
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    TempData["ErrorMessage"] = "Invalid login session. Please login again.";
                    return RedirectToPage("/Account/Login");
                }

                // Check if user exists
                var user = await FindUserAsync(email);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found. Please login again.";
                    return RedirectToPage("/Account/Login");
                }

                // Check if OTP is required for this user
                var isOTPRequired = await _otpService.IsOTPRequiredAsync(email);
                if (!isOTPRequired)
                {
                    TempData["ErrorMessage"] = "OTP verification is not required for this account.";
                    return RedirectToPage("/Account/Login");
                }

                // Store login credentials in TempData for verification
                TempData["LoginEmail"] = email;
                TempData["LoginPassword"] = password;
                TempData["LoginRememberMe"] = rememberMe.ToString();

                UserEmail = email;
                Password = password;
                RememberMe = rememberMe;
                IsAdmin = isAdmin;

                // Get user's phone number for SMS option
                if (user != null)
                {
                    try
                    {
                        var phoneNumber = user.PhoneNumber;
                        if (!string.IsNullOrEmpty(phoneNumber) && _encryptionService.IsEncrypted(phoneNumber))
                        {
                            phoneNumber = _encryptionService.Decrypt(phoneNumber);
                        }
                        UserPhoneNumber = phoneNumber;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error retrieving phone number for user {Email}", email);
                        UserPhoneNumber = null;
                    }
                }

                _logger.LogInformation($"OTP verification page loaded for user: {email}");
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading OTP verification page");
                TempData["ErrorMessage"] = "An error occurred. Please try again.";
                return RedirectToPage("/Account/Login");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                _logger.LogInformation($"OTP verification attempt for user: {UserEmail}, OTP: {OTPCode}");
                
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning($"Model state is invalid. Errors: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                    return Page();
                }

                // Get login credentials from bound properties
                var loginEmail = UserEmail;
                var loginPassword = Password;
                var loginRememberMe = RememberMe;

                if (string.IsNullOrEmpty(loginEmail) || string.IsNullOrEmpty(loginPassword))
                {
                    TempData["ErrorMessage"] = "Login session expired. Please login again.";
                    return RedirectToPage("/Account/Login");
                }

                // Find user
                var user = await FindUserAsync(loginEmail);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found. Please login again.";
                    return RedirectToPage("/Account/Login");
                }

                // Verify OTP
                _logger.LogInformation($"Validating OTP for {loginEmail}: {OTPCode}");
                var isOTPValid = await _otpService.ValidateOTPAsync(loginEmail, OTPCode);
                _logger.LogInformation($"OTP validation result: {isOTPValid}");
                
                if (!isOTPValid)
                {
                    _logger.LogWarning($"OTP validation failed for {loginEmail} with code: {OTPCode}");
                    ModelState.AddModelError(nameof(OTPCode), "Invalid OTP code. Please try again.");
                    UserEmail = loginEmail;
                    Password = loginPassword;
                    RememberMe = loginRememberMe;
                    return Page();
                }

                _logger.LogInformation($"OTP verified successfully for user: {loginEmail}");

                // Verify password again (security check)
                var passwordCheck = await _userManager.CheckPasswordAsync(user, loginPassword);
                if (!passwordCheck)
                {
                    TempData["ErrorMessage"] = "Invalid credentials. Please login again.";
                    return RedirectToPage("/Account/Login");
                }

                // Check if user account is approved
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    _logger.LogInformation($"Admin user {user.Email} bypassing approval check");
                }
                else if (user.Status == "Pending" || user.EncryptedStatus == "Pending")
                {
                    TempData["ErrorMessage"] = "Your account is pending approval by an administrator. Please check back later.";
                    return RedirectToPage("/Account/Login");
                }

                // Remove lockout if present
                if (user.LockoutEnd != null && user.LockoutEnd > System.DateTimeOffset.Now)
                {
                    _logger.LogWarning($"Removing lockout for user {user.Email}");
                    await _userManager.SetLockoutEndDateAsync(user, null);
                    await _userManager.ResetAccessFailedCountAsync(user);
                }

                // Auto-confirm email if approved
                if (!user.EmailConfirmed && (user.Status == "Approved" || user.EncryptedStatus == "Approved"))
                {
                    _logger.LogInformation($"Auto-confirming email for approved user {user.Email}");
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                }

                // Sign in user
                var signInIdentifier = user.UserName;
                var result = await _signInManager.PasswordSignInAsync(signInIdentifier, loginPassword, loginRememberMe, lockoutOnFailure: false);
                
                if (!result.Succeeded && !string.IsNullOrEmpty(user.Email))
                {
                    _logger.LogInformation($"Username sign-in failed, trying email: {user.Email}");
                    result = await _signInManager.PasswordSignInAsync(user.Email, loginPassword, loginRememberMe, lockoutOnFailure: false);
                }

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in successfully after OTP verification.");
                    
                    var roles = await _userManager.GetRolesAsync(user);
                    _logger.LogInformation($"User roles: {string.Join(", ", roles)}");

                    // Add user claims
                    var claims = new List<Claim>
                    {
                        new Claim("UserId", user.Id),
                        new Claim("Status", user.Status),
                        new Claim("IsActive", user.IsActive.ToString())
                    };

                    var existingClaims = await _userManager.GetClaimsAsync(user);
                    await _userManager.RemoveClaimsAsync(user, existingClaims);
                    await _userManager.AddClaimsAsync(user, claims);

                    // Redirect based on role
                    var redirectResult = GetDashboardRedirect(user, roles);
                    if (redirectResult != null)
                    {
                        return redirectResult;
                    }

                    return RedirectToPage("/Index");
                }
                else
                {
                    _logger.LogWarning($"Sign-in failed for user {user.Email} after OTP verification");
                    TempData["ErrorMessage"] = "Login failed. Please try again.";
                    return RedirectToPage("/Account/Login");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OTP verification");
                TempData["ErrorMessage"] = "An error occurred during verification. Please try again.";
                return RedirectToPage("/Account/Login");
            }
        }

        private IActionResult? GetDashboardRedirect(ApplicationUser user, IList<string> roles)
        {
            // If this is an admin login, only allow admin roles
            if (IsAdmin)
            {
                if (roles.Contains("Admin"))
                {
                    _logger.LogInformation($"Admin user {user.Email} completing OTP verification");
                    return RedirectToPage("/Admin/AdminDashboard");
                }
                if (roles.Contains("Admin Staff"))
                {
                    _logger.LogInformation($"Admin Staff user {user.Email} completing OTP verification");
                    return RedirectToPage("/AdminStaff/Dashboard");
                }
                
                // If admin login but not admin role, deny access
                _logger.LogWarning($"Non-admin user {user.Email} attempted admin OTP verification");
                return RedirectToPage("/Account/AdminLogin");
            }
            
            // Regular login - restrict admin users
            if (roles.Contains("Admin") || roles.Contains("Admin Staff"))
            {
                _logger.LogWarning($"Admin user {user.Email} attempted regular OTP verification");
                return RedirectToPage("/Account/AdminLogin");
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
                // Allow access if user is verified OR auto-approved (bypass approval page)
                // Accept both "Verified" and "Active" status for auto-approved users
                bool isVerified = (user.Status == "Verified" && user.IsActive) || 
                                 (user.IsApproved && user.VerificationStatus == "Auto Verified" && user.IsActive) ||
                                 (user.Status == "Active" && user.IsApproved && user.VerificationStatus == "Auto Verified" && user.IsActive);
                
                if (isVerified)
                {
                    return RedirectToPage("/User/UserDashboard");
                }
                return RedirectToPage("/Account/WaitingForApproval");
            }
            return null; // No role matched
        }
    }
}
