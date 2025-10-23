using System.Threading.Tasks;
using System.Security.Claims;
using Barangay.Models;
using Barangay.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Barangay.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;
        private readonly IAuditTrailService _auditTrail;

        public LogoutModel(SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel> logger, IAuditTrailService auditTrail)
        {
            _signInManager = signInManager;
            _logger = logger;
            _auditTrail = auditTrail;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // For GET requests, perform the logout operation as well
            return await LogoutAndRedirect();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            return await LogoutAndRedirect();
        }

        private async Task<IActionResult> LogoutAndRedirect()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                // Capture user info BEFORE logout for audit trail
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName = User.Identity.Name;
                
                // AUDIT: Log logout event
                await _auditTrail.LogAsync(
                    "Logout",
                    "User logged out",
                    "Authentication",
                    userId,
                    null,
                    null,
                    $"User {userName} ended session"
                );
                
                // Sign out the user
                await _signInManager.SignOutAsync();
                
                try 
                {
                    // Try to clear session if it's available
                    if (HttpContext.Session != null)
                    {
                        HttpContext.Session.Clear();
                    }
                }
                catch (InvalidOperationException ex)
                {
                    // Log the session error but continue with logout
                    _logger.LogWarning(ex, "Session not available during logout");
                }
                
                // Add cache control headers to prevent browser back button from showing sensitive pages
                Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, post-check=0, pre-check=0");
                Response.Headers.Append("Pragma", "no-cache");
                Response.Headers.Append("Expires", "0");
                
                // Revoke any authentication cookies
                await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
                
                // Clear browser storage via JavaScript (will be executed on the client side)
                TempData["ClearBrowserStorage"] = true;
                
                // Add a success message
                TempData["SuccessMessage"] = "You have been successfully logged out.";
                
                _logger.LogInformation("User logged out.");
            }
            
            // Always redirect to the homepage after logout
            return RedirectToPage("/Index");
        }
    }
} 