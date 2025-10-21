using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Barangay.Models;
using System.Threading.Tasks;
using System.Linq;

namespace Barangay.ViewComponents
{
    public class PendingUsersCountViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public PendingUsersCountViewComponent(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Count users with "Pending" status, excluding special roles
            var excludedRoleNames = new[] { "Admin", "System Administrator", "Admin Staff", "System Admin", "Staff Admin", "Doctor", "Nurse", "Head Nurse", "Head Doctor" };
            
            // Get all users with Pending status
            var pendingCount = await _userManager.Users
                .Where(u => u.Status.ToLower() == "pending")
                .CountAsync();

            return View(pendingCount);
        }
    }
}
