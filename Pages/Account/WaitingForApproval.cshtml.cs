using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Barangay.Models;
using System.Threading.Tasks;

namespace Barangay.Pages.Account
{
    public class WaitingForApprovalModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<WaitingForApprovalModel> _logger;

        public WaitingForApprovalModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<WaitingForApprovalModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // If user is already verified OR auto-approved, redirect to appropriate dashboard
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    // Check if user is verified OR auto-approved (bypass approval page)
                    // Accept both "Verified" and "Active" status for auto-approved users
                    bool isVerified = (user.Status == "Verified" && user.IsActive) || 
                                     (user.IsApproved && user.VerificationStatus == "Auto Verified" && user.IsActive) ||
                                     (user.Status == "Active" && user.IsApproved && user.VerificationStatus == "Auto Verified" && user.IsActive);
                    
                    if (isVerified)
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        
                        // Redirect based on role
                        if (roles.Contains("Admin"))
                        {
                            return RedirectToPage("/Admin/AdminDashboard");
                        }
                        if (roles.Contains("Admin Staff"))
                        {
                            return RedirectToPage("/AdminStaff/Dashboard");
                        }
                        if (roles.Contains("Doctor"))
                        {
                            return RedirectToPage("/Doctor/DoctorDashboard");
                        }
                        if (roles.Contains("Nurse") || roles.Contains("Head Nurse"))
                        {
                            return RedirectToPage("/Nurse/NurseDashboard");
                        }
                        if (roles.Contains("User") || roles.Contains("Patient"))
                        {
                            return RedirectToPage("/User/UserDashboard");
                        }
                        
                        // Default fallback
                        return RedirectToPage("/Index");
                    }
                }
            }
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Sign out the user
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            
            return RedirectToPage("/Index");
        }
    }
} 