using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Barangay.Models;
using System.Threading.Tasks;
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Services;

namespace Barangay.Pages.Nurse
{
    [Authorize(Roles = "Nurse")]
    public class SettingsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<SettingsModel> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public SettingsModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<SettingsModel> logger,
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
            _emailService = emailService;
        }

        [BindProperty]
        public StaffProfileViewModel StaffProfile { get; set; } = new StaffProfileViewModel();

        [BindProperty]
        public ChangePasswordViewModel PasswordModel { get; set; } = new ChangePasswordViewModel();

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Get staff member data
            var staffMember = await _context.StaffMembers
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (staffMember == null)
            {
                _logger.LogWarning($"Staff member not found for user {user.Id}");
                return NotFound("Staff member record not found.");
            }

            // Parse the full name into separate components
            var nameParts = staffMember.Name?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
            var firstName = nameParts.Length > 0 ? nameParts[0] : "";
            var middleName = nameParts.Length > 2 ? string.Join(" ", nameParts.Skip(1).Take(nameParts.Length - 2)) : "";
            var lastName = nameParts.Length > 1 ? nameParts[nameParts.Length - 1] : "";

            StaffProfile = new StaffProfileViewModel
            {
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                Email = staffMember.Email,
                ContactNumber = staffMember.ContactNumber
            };

            // Check if this is first login - show notification
            ViewData["ShowFirstLoginNotification"] = user.IsFirstLogin;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Remove password model validation for profile update
            string[] pwdKeys = new[] {
                "PasswordModel.OldPassword", "PasswordModel.NewPassword", "PasswordModel.ConfirmPassword",
                "OldPassword", "NewPassword", "ConfirmPassword", nameof(PasswordModel)
            };
            foreach (var k in pwdKeys)
            {
                if (ModelState.ContainsKey(k)) ModelState.Remove(k);
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Get staff member data
            var staffMember = await _context.StaffMembers
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (staffMember == null)
            {
                _logger.LogWarning($"Staff member not found for user {user.Id}");
                StatusMessage = "Error: Staff member record not found.";
                return Page();
            }

            // Update staff member record
            staffMember.Name = StaffProfile.FullName;
            staffMember.Email = StaffProfile.Email;
            staffMember.ContactNumber = StaffProfile.ContactNumber;

            // Update user record to keep them in sync
            user.FullName = StaffProfile.FullName;
            user.Name = StaffProfile.FullName;
            user.Email = StaffProfile.Email;
            user.UserName = StaffProfile.Email;
            user.PhoneNumber = StaffProfile.ContactNumber;
            user.UpdatedAt = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
                var userResult = await _userManager.UpdateAsync(user);
                
                if (userResult.Succeeded)
                {
                    await _signInManager.RefreshSignInAsync(user);
                    StatusMessage = "Your profile has been updated successfully.";
                    _logger.LogInformation($"Nurse profile updated for user {user.Id}");
                }
                else
                {
                    foreach (var error in userResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    StatusMessage = "Error: Could not update profile.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating nurse profile for user {user.Id}");
                StatusMessage = "Error: Could not update profile. Please try again.";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            // Only validate the PasswordModel for this handler
            ModelState.Clear();
            TryValidateModel(PasswordModel, nameof(PasswordModel));
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, 
                PasswordModel.OldPassword, PasswordModel.NewPassword);
            
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            // Clear first login flag if this was a first-time password change
            if (user.IsFirstLogin)
            {
                user.IsFirstLogin = false;
                await _userManager.UpdateAsync(user);
                _logger.LogInformation($"IsFirstLogin flag cleared for user {user.Id}");
            }
            
            // Mark that user has changed their password
            if (!user.HasChangedPassword)
            {
                user.HasChangedPassword = true;
                user.LastPasswordChangeDate = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                _logger.LogInformation($"HasChangedPassword flag set for user {user.Id}");
            }

            await _signInManager.RefreshSignInAsync(user);
            
            // Send email verification/notification
            try
            {
                await SendPasswordChangeNotificationAsync(user);
                StatusMessage = "Your password has been changed successfully. A confirmation email has been sent.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send password change notification to {user.Email}");
                StatusMessage = "Your password has been changed successfully.";
            }

            return RedirectToPage();
        }

        private async Task SendPasswordChangeNotificationAsync(ApplicationUser user)
        {
            try
            {
                // Use UserName (which is the email) for sending email
                var userEmail = !string.IsNullOrEmpty(user.UserName) ? user.UserName : user.Email;
                var displayName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Name;

                var subject = "BH Care - Password Changed Successfully";
                var htmlContent = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }}
                        .container {{ max-width: 600px; margin: 0 auto; background: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
                        .header {{ text-align: center; margin-bottom: 30px; }}
                        .logo {{ color: #e27e38; font-size: 24px; font-weight: bold; margin-bottom: 10px; }}
                        .content {{ line-height: 1.6; color: #333; }}
                        .highlight {{ background-color: #f8f9fa; padding: 15px; border-left: 4px solid #e27e38; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; color: #666; font-size: 14px; }}
                        .icon {{ color: #e27e38; margin-right: 8px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <div class='logo'>🏥 BH Care</div>
                            <h2 style='color: #333; margin: 0;'>Password Changed Successfully</h2>
                        </div>
                        
                        <div class='content'>
                            <p>Hello <strong>{displayName}</strong>,</p>
                            
                            <p>Your password has been changed successfully for your BH Care account.</p>
                            
                            <div class='highlight'>
                                <p><span class='icon'>📧</span><strong>Account:</strong> {userEmail}</p>
                                <p><span class='icon'>🕒</span><strong>Changed on:</strong> {DateTime.Now:MMMM dd, yyyy 'at' HH:mm}</p>
                                <p><span class='icon'></span><strong>Status:</strong> Password updated and secured</p>
                            </div>
                            
                            <p>You can now access your dashboard with your new password. If you didn't make this change, please contact your administrator immediately.</p>
                            
                            <p>Thank you for using BH Care!</p>
                        </div>
                        
                        <div class='footer'>
                            <p>This is an automated message from BH Care System</p>
                            <p>© {DateTime.Now.Year} Barangay Health Care. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

                await _emailService.SendEmailAsync(userEmail, subject, htmlContent);
                _logger.LogInformation("Password change notification email sent to user {Email}", userEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password change notification email");
                throw; // Re-throw to let caller handle
            }
        }

        // OTP Verification Handlers
        public async Task<IActionResult> OnPostRequestOTPAsync()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return new JsonResult(new { success = false, message = "User not found" });
                }

                // Generate 6-digit OTP
                var otp = new Random().Next(100000, 999999).ToString();
                
                // Store OTP in session with expiry (5 minutes)
                HttpContext.Session.SetString("OTP_Code", otp);
                HttpContext.Session.SetString("OTP_Expiry", DateTime.UtcNow.AddMinutes(5).ToString("O"));
                
                // Send OTP via email
                var userEmail = !string.IsNullOrEmpty(user.UserName) ? user.UserName : user.Email;
                var displayName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Name;
                
                var subject = "BH Care - Password Change Verification Code";
                var htmlContent = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; margin: 0; padding: 20px; background-color: #f4f4f4; }}
                        .container {{ max-width: 600px; margin: 0 auto; background: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
                        .header {{ text-align: center; margin-bottom: 30px; }}
                        .logo {{ color: #e27e38; font-size: 24px; font-weight: bold; margin-bottom: 10px; }}
                        .otp-box {{ background: linear-gradient(135deg, #e27e38 0%, #ff9248 100%); color: white; padding: 20px; border-radius: 10px; text-align: center; margin: 30px 0; }}
                        .otp-code {{ font-size: 36px; font-weight: bold; letter-spacing: 8px; margin: 10px 0; }}
                        .content {{ line-height: 1.6; color: #333; }}
                        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; color: #666; font-size: 14px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <div class='logo'>🏥 BH Care</div>
                            <h2 style='color: #333; margin: 0;'>Password Change Verification</h2>
                        </div>
                        
                        <div class='content'>
                            <p>Hello <strong>{displayName}</strong>,</p>
                            
                            <p>You have requested to change your password. Please use the verification code below to complete the process:</p>
                            
                            <div class='otp-box'>
                                <p style='margin: 0; font-size: 14px;'>Your Verification Code</p>
                                <div class='otp-code'>{otp}</div>
                                <p style='margin: 0; font-size: 12px;'>Valid for 5 minutes</p>
                            </div>
                            
                            <div class='warning'>
                                <p style='margin: 0;'><strong> Security Notice:</strong> If you didn't request this code, please ignore this email and contact your administrator immediately.</p>
                            </div>
                            
                            <p>This code will expire in 5 minutes for your security.</p>
                        </div>
                        
                        <div class='footer'>
                            <p>This is an automated message from BH Care System</p>
                            <p>© {DateTime.Now.Year} Barangay Health Care. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

                await _emailService.SendEmailAsync(userEmail, subject, htmlContent);
                _logger.LogInformation($"OTP sent to user {user.Id}: {otp}"); // Log for debugging (remove in production)
                
                return new JsonResult(new { success = true, message = "Verification code sent to your email" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP");
                return new JsonResult(new { success = false, message = "Failed to send verification code" });
            }
        }

        public async Task<IActionResult> OnPostVerifyOTPAndChangePasswordAsync(string otpCode, string oldPassword, string newPassword, string confirmPassword)
        {
            try
            {
                // Verify OTP
                var storedOtp = HttpContext.Session.GetString("OTP_Code");
                var otpExpiry = HttpContext.Session.GetString("OTP_Expiry");
                
                if (string.IsNullOrEmpty(storedOtp) || string.IsNullOrEmpty(otpExpiry))
                {
                    return new JsonResult(new { success = false, message = "No verification code found. Please request a new code." });
                }
                
                if (DateTime.Parse(otpExpiry) < DateTime.UtcNow)
                {
                    HttpContext.Session.Remove("OTP_Code");
                    HttpContext.Session.Remove("OTP_Expiry");
                    return new JsonResult(new { success = false, message = "Verification code has expired. Please request a new code." });
                }
                
                if (storedOtp != otpCode)
                {
                    return new JsonResult(new { success = false, message = "Invalid verification code. Please try again." });
                }
                
                // OTP verified, now change password
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return new JsonResult(new { success = false, message = "User not found" });
                }
                
                if (newPassword != confirmPassword)
                {
                    return new JsonResult(new { success = false, message = "New password and confirmation do not match." });
                }
                
                var changePasswordResult = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
                
                if (!changePasswordResult.Succeeded)
                {
                    var errors = string.Join(", ", changePasswordResult.Errors.Select(e => e.Description));
                    return new JsonResult(new { success = false, message = errors });
                }
                
                // Clear OTP from session
                HttpContext.Session.Remove("OTP_Code");
                HttpContext.Session.Remove("OTP_Expiry");
                
                // Clear first login flag and mark password as changed
                if (user.IsFirstLogin)
                {
                    user.IsFirstLogin = false;
                    await _userManager.UpdateAsync(user);
                    _logger.LogInformation($"IsFirstLogin flag cleared for user {user.Id}");
                }
                
                if (!user.HasChangedPassword)
                {
                    user.HasChangedPassword = true;
                    user.LastPasswordChangeDate = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);
                    _logger.LogInformation($"HasChangedPassword flag set for user {user.Id}");
                }
                
                await _signInManager.RefreshSignInAsync(user);
                
                // Send confirmation email
                try
                {
                    await SendPasswordChangeNotificationAsync(user);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password change notification");
                }
                
                return new JsonResult(new { success = true, message = "Password updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP and changing password");
                return new JsonResult(new { success = false, message = "An error occurred. Please try again." });
            }
        }
    }

    public class StaffProfileViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Display(Name = "Middle Name")]
        public string MiddleName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; }

        // Computed property for full name
        public string FullName => $"{FirstName} {MiddleName} {LastName}".Trim();
    }

    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
