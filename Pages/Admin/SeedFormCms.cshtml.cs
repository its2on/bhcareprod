using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Services;
using Microsoft.AspNetCore.Authorization;

namespace Barangay.Pages.Admin
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class SeedFormCmsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SeedFormCmsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool HasExistingForms { get; set; }
        public int ExistingFormsCount { get; set; }

        public async Task OnGetAsync()
        {
            ExistingFormsCount = await _context.FormTemplates.CountAsync();
            HasExistingForms = ExistingFormsCount > 0;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var seeder = new FormCmsSeeder(_context);
                await seeder.SeedCommonFormsAsync();

                var newCount = await _context.FormTemplates.CountAsync();

                TempData["SuccessMessage"] = $"Form CMS seeded successfully! {newCount} form template(s) are now available.";
                
                return RedirectToPage("./FormManagement");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error seeding forms: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}
