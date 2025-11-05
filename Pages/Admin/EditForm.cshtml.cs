using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Microsoft.AspNetCore.Authorization;

namespace Barangay.Pages.Admin
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class EditFormModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditFormModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public FormTemplate FormTemplate { get; set; } = new FormTemplate();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var form = await _context.FormTemplates.FindAsync(id);

            if (form == null)
            {
                return NotFound();
            }

            FormTemplate = form;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var formToUpdate = await _context.FormTemplates.FindAsync(FormTemplate.FormTemplateId);

            if (formToUpdate == null)
            {
                return NotFound();
            }

            // Update properties (don't allow FormKey to change)
            formToUpdate.FormName = FormTemplate.FormName;
            formToUpdate.Description = FormTemplate.Description;
            formToUpdate.Category = FormTemplate.Category;
            formToUpdate.IconClass = FormTemplate.IconClass;
            formToUpdate.DisplayOrder = FormTemplate.DisplayOrder;
            formToUpdate.SuccessMessage = FormTemplate.SuccessMessage;
            formToUpdate.RedirectUrl = FormTemplate.RedirectUrl;
            formToUpdate.CssClasses = FormTemplate.CssClasses;
            formToUpdate.IsActive = FormTemplate.IsActive;
            formToUpdate.UpdatedAt = DateTime.UtcNow;
            formToUpdate.UpdatedBy = User.Identity?.Name;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Form '{FormTemplate.FormName}' has been updated successfully.";

            return RedirectToPage("./ManageFormFields", new { id = FormTemplate.FormTemplateId });
        }
    }
}
