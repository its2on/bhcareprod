using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace Barangay.Pages.Admin
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ManageFormFieldsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ManageFormFieldsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public FormTemplate? FormTemplate { get; set; }
        public List<FormField> FormFields { get; set; } = new List<FormField>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            FormTemplate = await _context.FormTemplates
                .Include(f => f.FormFields)
                    .ThenInclude(ff => ff.FormFieldOptions)
                .FirstOrDefaultAsync(f => f.FormTemplateId == id);

            if (FormTemplate == null)
            {
                return NotFound();
            }

            FormFields = FormTemplate.FormFields.OrderBy(f => f.DisplayOrder).ToList();

            return Page();
        }

        public async Task<IActionResult> OnGetGetFieldAsync(int fieldId)
        {
            var field = await _context.FormFields.FindAsync(fieldId);

            if (field == null)
            {
                return NotFound();
            }

            return new JsonResult(field);
        }

        public async Task<IActionResult> OnPostSaveFieldAsync(
            int FormFieldId,
            int FormTemplateId,
            string FieldLabel,
            string FieldName,
            string FieldType,
            string? FieldWidth,
            string? Placeholder,
            string? HelpText,
            string? DefaultValue,
            string? CssClasses,
            bool IsRequired,
            bool IsReadOnly)
        {
            FormField? field;

            if (FormFieldId > 0)
            {
                // Update existing field
                field = await _context.FormFields.FindAsync(FormFieldId);
                if (field == null)
                {
                    TempData["ErrorMessage"] = "Field not found.";
                    return RedirectToPage(new { id = FormTemplateId });
                }
                field.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new field
                field = new FormField
                {
                    FormTemplateId = FormTemplateId,
                    CreatedAt = DateTime.UtcNow
                };

                // Set display order to be last
                var maxOrder = await _context.FormFields
                    .Where(f => f.FormTemplateId == FormTemplateId)
                    .MaxAsync(f => (int?)f.DisplayOrder) ?? 0;
                field.DisplayOrder = maxOrder + 1;

                _context.FormFields.Add(field);
            }

            // Update field properties
            field.FieldLabel = FieldLabel;
            field.FieldName = FieldName;
            field.FieldType = FieldType;
            field.FieldWidth = FieldWidth ?? "col-12";
            field.Placeholder = Placeholder;
            field.HelpText = HelpText;
            field.DefaultValue = DefaultValue;
            field.CssClasses = CssClasses;
            field.IsRequired = IsRequired;
            field.IsReadOnly = IsReadOnly;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Field '{FieldLabel}' has been saved successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error saving field: {ex.Message}";
            }

            return RedirectToPage(new { id = FormTemplateId });
        }

        public async Task<IActionResult> OnPostDeleteFieldAsync(int fieldId)
        {
            var field = await _context.FormFields
                .Include(f => f.FormFieldOptions)
                .FirstOrDefaultAsync(f => f.FormFieldId == fieldId);

            if (field == null)
            {
                TempData["ErrorMessage"] = "Field not found.";
                return RedirectToPage();
            }

            var formTemplateId = field.FormTemplateId;

            try
            {
                _context.FormFields.Remove(field);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Field '{field.FieldLabel}' has been deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting field: {ex.Message}";
            }

            return RedirectToPage(new { id = formTemplateId });
        }
    }
}
