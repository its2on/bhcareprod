using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Microsoft.AspNetCore.Authorization;
using Barangay.Helpers;

namespace Barangay.Pages.Nurse
{
    [Authorize(Roles = "Nurse,Head Nurse")]
    public class ControlPanelModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ControlPanelModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IEnumerable<FormTemplate> FormTemplates { get; set; } = new List<FormTemplate>();
        public List<ConsultationService> Services { get; set; } = new List<ConsultationService>();
        public string StatusFilter { get; set; } = "";
        public string CategoryFilter { get; set; } = "";
        public string SearchQuery { get; set; } = "";
        public List<string> Categories { get; set; } = new List<string>();
        
        // Daily appointment slot tracking properties
        public DateTime SelectedDate { get; set; } = DateTimeHelper.Today;
        public int TotalAppointments { get; set; } = 50; // Default to 50 slots
        public int MaxAppointmentsPerDay { get; set; } = 50;
        public List<Barangay.Models.Appointment> TodayAppointments { get; set; } = new List<Barangay.Models.Appointment>();

        public async Task OnGetAsync(string? status, string? category, string? search, string? filterDate = null)
        {
            StatusFilter = status ?? "";
            CategoryFilter = category ?? "";
            SearchQuery = search ?? "";

            // Parse filter date or use today as default
            if (!string.IsNullOrEmpty(filterDate) && DateTime.TryParse(filterDate, out var parsedDate))
            {
                SelectedDate = parsedDate.Date;
            }
            else
            {
                SelectedDate = DateTimeHelper.Today;
            }

            // Load today's appointments for slot tracking
            var validStatuses = new[] { AppointmentStatus.Pending, AppointmentStatus.Confirmed, AppointmentStatus.InProgress };
            TodayAppointments = await _context.Appointments
                .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Where(a => a.AppointmentDate.Date == SelectedDate
                            && validStatuses.Contains(a.Status))
                .OrderBy(a => a.AppointmentTime)
                .ToListAsync();

            // Set default max appointments per day (can be configured later)
            MaxAppointmentsPerDay = 50;
            TotalAppointments = 50;

            // Load Services
            Services = await _context.ConsultationServices
                .Include(s => s.AssociatedForms)
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.ServiceName)
                .ToListAsync();

            // Build query - Show all forms (active and inactive)
            var query = _context.FormTemplates
                .Include(f => f.FormFields)
                .Include(f => f.FormSubmissions)
                .AsQueryable();

            // Apply status filter
            if (!string.IsNullOrEmpty(StatusFilter))
            {
                bool isActive = StatusFilter.ToLower() == "active";
                query = query.Where(f => f.IsActive == isActive);
            }

            // Apply filters
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

            // Get unique categories from all forms
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

        // ===== SERVICE MANAGEMENT HANDLERS =====

        public async Task<IActionResult> OnPostAddServiceAsync(
            string ServiceName,
            string ServiceKey,
            string? Description,
            string? Category,
            int DisplayOrder,
            string? IconClass,
            string? ColorTheme,
            int? MinAge,
            int? MaxAge,
            bool IsActive = true)
        {
            try
            {
                // Validate
                if (string.IsNullOrWhiteSpace(ServiceName) || string.IsNullOrWhiteSpace(ServiceKey))
                {
                    TempData["ErrorMessage"] = "Service Name and Service Key are required.";
                    return RedirectToPage();
                }

                // Check if key exists
                var existingService = await _context.ConsultationServices
                    .FirstOrDefaultAsync(s => s.ServiceKey == ServiceKey.ToLower().Trim());

                if (existingService != null)
                {
                    TempData["ErrorMessage"] = $"A service with key '{ServiceKey}' already exists.";
                    return RedirectToPage();
                }

                // Create service
                var service = new ConsultationService
                {
                    ServiceName = ServiceName.Trim(),
                    ServiceKey = ServiceKey.Trim().ToLower().Replace(" ", "-"),
                    Description = Description?.Trim(),
                    Category = Category?.Trim(),
                    DisplayOrder = DisplayOrder,
                    IconClass = IconClass?.Trim(),
                    ColorTheme = ColorTheme?.Trim(),
                    MinAge = MinAge,
                    MaxAge = MaxAge,
                    IsActive = IsActive,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name
                };

                _context.ConsultationServices.Add(service);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Service '{ServiceName}' created successfully. It will now appear in the Form Builder.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating service: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditServiceAsync(
            int ServiceId,
            string ServiceName,
            string ServiceKey,
            string? Description,
            string? Category,
            int DisplayOrder,
            string? IconClass,
            string? ColorTheme,
            int? MinAge,
            int? MaxAge,
            bool IsActive = false)
        {
            try
            {
                var service = await _context.ConsultationServices
                    .FirstOrDefaultAsync(s => s.ServiceId == ServiceId);

                if (service == null)
                {
                    TempData["ErrorMessage"] = "Service not found.";
                    return RedirectToPage();
                }

                // Update service
                service.ServiceName = ServiceName.Trim();
                service.Description = Description?.Trim();
                service.Category = Category?.Trim();
                service.DisplayOrder = DisplayOrder;
                service.IconClass = IconClass?.Trim();
                service.ColorTheme = ColorTheme?.Trim();
                service.MinAge = MinAge;
                service.MaxAge = MaxAge;
                service.IsActive = IsActive;
                service.UpdatedAt = DateTime.UtcNow;
                service.UpdatedBy = User.Identity?.Name;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Service '{ServiceName}' updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating service: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteServiceAsync(int ServiceId)
        {
            try
            {
                var service = await _context.ConsultationServices
                    .Include(s => s.AssociatedForms)
                    .FirstOrDefaultAsync(s => s.ServiceId == ServiceId);

                if (service == null)
                {
                    TempData["ErrorMessage"] = "Service not found.";
                    return RedirectToPage();
                }

                // Check if service has associated forms
                if (service.AssociatedForms.Any())
                {
                    TempData["ErrorMessage"] = $"Cannot delete service '{service.ServiceName}' because it has {service.AssociatedForms.Count} associated form(s). Please remove or reassign the forms first.";
                    return RedirectToPage();
                }

                _context.ConsultationServices.Remove(service);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Service '{service.ServiceName}' deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting service: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}
