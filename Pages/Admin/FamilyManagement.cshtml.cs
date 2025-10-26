using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Barangay.Data;
using Microsoft.EntityFrameworkCore;

namespace Barangay.Pages.Admin
{
    [Authorize(Roles = "Admin,System Administrator")]
    public class FamilyManagementModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FamilyManagementModel> _logger;

        public FamilyManagementModel(ApplicationDbContext context, ILogger<FamilyManagementModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public void OnGet()
        {
            _logger.LogInformation("Admin accessed Family Management page");
        }
    }
}
