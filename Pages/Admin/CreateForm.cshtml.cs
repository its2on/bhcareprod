using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Microsoft.AspNetCore.Authorization;

namespace Barangay.Pages.Admin
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class CreateFormModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateFormModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public FormTemplate FormTemplate { get; set; } = new FormTemplate();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Check if form key already exists
            var existingForm = await _context.FormTemplates
                .FirstOrDefaultAsync(f => f.FormKey == FormTemplate.FormKey);

            if (existingForm != null)
            {
                ModelState.AddModelError("FormTemplate.FormKey", "A form with this key already exists.");
                return Page();
            }

            // Set metadata
            FormTemplate.CreatedAt = DateTime.UtcNow;
            FormTemplate.CreatedBy = User.Identity?.Name;
            FormTemplate.Version = 1;

            _context.FormTemplates.Add(FormTemplate);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Form '{FormTemplate.FormName}' has been created successfully. Now add fields to your form.";

            return RedirectToPage("./ManageFormFields", new { id = FormTemplate.FormTemplateId });
        }
    }
}
