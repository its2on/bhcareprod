using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Microsoft.AspNetCore.Authorization;

namespace Barangay.Pages.Admin
{
    [Authorize(Roles = "Admin,SuperAdmin,Nurse")]
    public class FormManagementModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public FormManagementModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IEnumerable<FormTemplate> FormTemplates { get; set; } = new List<FormTemplate>();
        public string StatusFilter { get; set; } = "";
        public string CategoryFilter { get; set; } = "";
        public string SearchQuery { get; set; } = "";
        public List<string> Categories { get; set; } = new List<string>();

        public async Task OnGetAsync(string? status, string? category, string? search)
        {
            StatusFilter = status ?? "";
            CategoryFilter = category ?? "";
            SearchQuery = search ?? "";

            // Build query
            var query = _context.FormTemplates
                .Include(f => f.FormFields)
                .Include(f => f.FormSubmissions)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(StatusFilter))
            {
                bool isActive = StatusFilter.ToLower() == "active";
                query = query.Where(f => f.IsActive == isActive);
            }

            if (!string.IsNullOrEmpty(CategoryFilter))
            {
                query = query.Where(f => f.Category == CategoryFilter);
            }

            if (!string.IsNullOrEmpty(SearchQuery))
            {
                query = query.Where(f => 
                    f.FormName.Contains(SearchQuery) || 
                    (f.Description != null && f.Description.Contains(SearchQuery)));
            }

            FormTemplates = await query
                .OrderBy(f => f.DisplayOrder)
                .ThenByDescending(f => f.CreatedAt)
                .ToListAsync();

            // Get unique categories
            Categories = await _context.FormTemplates
                .Where(f => f.Category != null && f.Category != "")
                .Select(f => f.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var formTemplate = await _context.FormTemplates
                .Include(f => f.FormFields)
                .Include(f => f.FormSubmissions)
                .FirstOrDefaultAsync(f => f.FormTemplateId == id);

            if (formTemplate == null)
            {
                TempData["ErrorMessage"] = "Form not found.";
                return RedirectToPage();
            }

            try
            {
                _context.FormTemplates.Remove(formTemplate);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Form '{formTemplate.FormName}' has been deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting form: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            var formTemplate = await _context.FormTemplates.FindAsync(id);

            if (formTemplate == null)
            {
                return NotFound();
            }

            formTemplate.IsActive = !formTemplate.IsActive;
            formTemplate.UpdatedAt = DateTime.UtcNow;
            formTemplate.UpdatedBy = User.Identity?.Name;

            await _context.SaveChangesAsync();

            return new OkResult();
        }

        public async Task<IActionResult> OnPostDuplicateAsync(int id)
        {
            var originalForm = await _context.FormTemplates
                .Include(f => f.FormFields)
                .ThenInclude(ff => ff.FormFieldOptions)
                .FirstOrDefaultAsync(f => f.FormTemplateId == id);

            if (originalForm == null)
            {
                return NotFound();
            }

            try
            {
                // Create duplicate form
                var duplicateForm = new FormTemplate
                {
                    FormName = originalForm.FormName + " (Copy)",
                    Description = originalForm.Description,
                    FormKey = originalForm.FormKey + "-copy-" + Guid.NewGuid().ToString().Substring(0, 8),
                    Category = originalForm.Category,
                    IsActive = false, // Set inactive by default
                    DisplayOrder = originalForm.DisplayOrder,
                    IconClass = originalForm.IconClass,
                    SuccessMessage = originalForm.SuccessMessage,
                    RedirectUrl = originalForm.RedirectUrl,
                    CssClasses = originalForm.CssClasses,
                    JsonConfiguration = originalForm.JsonConfiguration,
                    Version = 1,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name
                };

                _context.FormTemplates.Add(duplicateForm);
                await _context.SaveChangesAsync();

                // Duplicate fields
                foreach (var originalField in originalForm.FormFields)
                {
                    var duplicateField = new FormField
                    {
                        FormTemplateId = duplicateForm.FormTemplateId,
                        FieldName = originalField.FieldName,
                        FieldLabel = originalField.FieldLabel,
                        FieldType = originalField.FieldType,
                        Placeholder = originalField.Placeholder,
                        DefaultValue = originalField.DefaultValue,
                        HelpText = originalField.HelpText,
                        IsRequired = originalField.IsRequired,
                        IsReadOnly = originalField.IsReadOnly,
                        IsDisabled = originalField.IsDisabled,
                        DisplayOrder = originalField.DisplayOrder,
                        ValidationRules = originalField.ValidationRules,
                        CssClasses = originalField.CssClasses,
                        FieldWidth = originalField.FieldWidth,
                        ConditionalLogic = originalField.ConditionalLogic,
                        CustomAttributes = originalField.CustomAttributes,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.FormFields.Add(duplicateField);
                    await _context.SaveChangesAsync();

                    // Duplicate field options
                    foreach (var originalOption in originalField.FormFieldOptions)
                    {
                        var duplicateOption = new FormFieldOption
                        {
                            FormFieldId = duplicateField.FormFieldId,
                            OptionLabel = originalOption.OptionLabel,
                            OptionValue = originalOption.OptionValue,
                            DisplayOrder = originalOption.DisplayOrder,
                            IsDefault = originalOption.IsDefault,
                            IsActive = originalOption.IsActive,
                            IconClass = originalOption.IconClass,
                            GroupName = originalOption.GroupName,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.FormFieldOptions.Add(duplicateOption);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Form '{originalForm.FormName}' has been duplicated successfully.";
                return new OkResult();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error duplicating form: {ex.Message}");
            }
        }
    }
}
