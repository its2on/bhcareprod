using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Barangay.Models;
using Barangay.Data;
using Barangay.Services;
using System.ComponentModel.DataAnnotations;

namespace Barangay.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ResetPasswordModel> _logger;
        private readonly IAuditTrailService _auditTrail;

        public ResetPasswordModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<ResetPasswordModel> logger,
            IAuditTrailService auditTrail)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
            _auditTrail = auditTrail;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string Email { get; set; } = string.Empty;

        public class InputModel
        {
            [Required(ErrorMessage = "New password is required")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please confirm your new password")]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm New Password")]
            [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Identity verification is required")]
            [Display(Name = "Identity Verification")]
            public string IdentityVerification { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Invalid request. Please start the password reset process again.";
                return RedirectToPage("./ForgotPassword");
            }

            // Verify the token exists and is valid
            var otpRecord = await _context.PasswordResetOTPs
                .Where(o => o.Email == email && o.Id.ToString() == token && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            if (otpRecord == null)
            {
                TempData["ErrorMessage"] = "Invalid or expired reset link. Please start the password reset process again.";
                return RedirectToPage("./ForgotPassword");
            }

            Email = email;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Get email and token from query parameters
                var email = Request.Query["email"].FirstOrDefault();
                var token = Request.Query["token"].FirstOrDefault();

                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
                {
                    TempData["ErrorMessage"] = "Invalid request. Please start the password reset process again.";
                    return RedirectToPage("./ForgotPassword");
                }

                Email = email;

                // Verify the token exists and is valid
                var otpRecord = await _context.PasswordResetOTPs
                    .Where(o => o.Email == email && o.Id.ToString() == token && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
                    .FirstOrDefaultAsync();

                if (otpRecord == null)
                {
                    TempData["ErrorMessage"] = "Invalid or expired reset link. Please start the password reset process again.";
                    return RedirectToPage("./ForgotPassword");
                }

                // Find the user
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found. Please contact support.";
                    return RedirectToPage("./ForgotPassword");
                }

                // Verify identity by checking if the entered name matches the user's full name
                var userFullName = $"{user.FirstName} {user.LastName}".Trim();
                if (!string.Equals(Input.IdentityVerification, userFullName, StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(string.Empty, "Identity verification failed. Please enter your full name exactly as registered in the system.");
                    return Page();
                }

                // Reset the password
                var result = await _userManager.ResetPasswordAsync(user, await _userManager.GeneratePasswordResetTokenAsync(user), Input.NewPassword);

                if (result.Succeeded)
                {
                    // Mark OTP as used
                    otpRecord.IsUsed = true;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Password reset successfully for user {email} after identity verification");

                    // AUDIT: Log password change event
                    await _auditTrail.LogAsync(
                        "Update",
                        "Password changed via reset",
                        "ApplicationUser",
                        user.Id,
                        null,
                        null,
                        $"User {user.Email} successfully reset password after identity verification"
                    );

                    TempData["SuccessMessage"] = "Password reset successfully! Your identity has been verified and your password has been changed. You can now log in with your new password.";
                    return RedirectToPage("./Login");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for {Email}", Email);
                TempData["ErrorMessage"] = "An error occurred while resetting your password. Please try again later.";
                return Page();
            }
        }
    }
}