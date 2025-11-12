using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace Barangay.Pages.Nurse
{
    [Authorize(Roles = "Nurse,Head Nurse")]
    public class FormBuilderModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FormBuilderModel> _logger;

        public FormBuilderModel(ApplicationDbContext context, ILogger<FormBuilderModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public FormTemplate FormTemplate { get; set; } = new FormTemplate();
        public bool IsEdit { get; set; } = false;
        public string FormFieldsJson { get; set; } = "[]";
        public List<ConsultationService> AvailableServices { get; set; } = new List<ConsultationService>();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id.HasValue)
            {
                // Edit mode
                FormTemplate = await _context.FormTemplates
                    .Include(f => f.FormFields)
                    .ThenInclude(ff => ff.FormFieldOptions)
                    .FirstOrDefaultAsync(f => f.FormTemplateId == id.Value);

                if (FormTemplate == null)
                {
                    return NotFound();
                }

                IsEdit = true;

                // Serialize fields to JSON for JavaScript
                var fields = FormTemplate.FormFields.OrderBy(f => f.DisplayOrder).Select(f => new
                {
                    formFieldId = f.FormFieldId,
                    fieldLabel = f.FieldLabel,
                    fieldName = f.FieldName,
                    fieldType = f.FieldType,
                    isRequired = f.IsRequired,
                    displayOrder = f.DisplayOrder,
                    title = f.Title,
                    description = f.HelpText,
                    validationPattern = f.ValidationPattern,
                    options = f.FormFieldOptions.OrderBy(o => o.DisplayOrder).Select(o => new
                    {
                        formFieldOptionId = o.FormFieldOptionId,
                        optionLabel = o.OptionLabel,
                        optionValue = o.OptionValue,
                        displayOrder = o.DisplayOrder
                    }).ToList()
                });

                FormFieldsJson = JsonSerializer.Serialize(fields);
            }

            // Load available consultation services
            AvailableServices = await _context.ConsultationServices
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostSaveFormAsync([FromBody] FormBuilderData formData)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(formData.FormName) || string.IsNullOrWhiteSpace(formData.FormKey))
                {
                    return BadRequest("Form name and form key are required");
                }

                FormTemplate? form;

                if (formData.FormTemplateId > 0)
                {
                    // Update existing form
                    form = await _context.FormTemplates
                        .Include(f => f.FormFields)
                        .ThenInclude(ff => ff.FormFieldOptions)
                        .FirstOrDefaultAsync(f => f.FormTemplateId == formData.FormTemplateId);

                    if (form == null)
                    {
                        return NotFound("Form not found");
                    }

                    form.FormName = formData.FormName;
                    form.Description = formData.FormDescription;
                    form.ServiceId = formData.ServiceId;
                    form.IsActive = formData.IsActive;
                    form.DisplayOrder = formData.DisplayOrder;
                    form.ShowInAppointmentFlow = formData.ShowInAppointmentFlow;
                    form.MinAge = formData.MinAge;
                    form.MaxAge = formData.MaxAge;
                    form.IconClass = formData.IconClass;
                    form.SuccessMessage = formData.SuccessMessage;
                    form.RedirectUrl = formData.RedirectUrl;
                    form.UpdatedAt = DateTime.UtcNow;
                    form.UpdatedBy = User.Identity?.Name;
                    form.Version++;

                    // Remove existing fields
                    _context.FormFields.RemoveRange(form.FormFields);
                }
                else
                {
                    // Check if form key already exists
                    var existingForm = await _context.FormTemplates
                        .FirstOrDefaultAsync(f => f.FormKey == formData.FormKey);

                    if (existingForm != null)
                    {
                        return BadRequest("A form with this key already exists");
                    }

                    // Create new form
                    form = new FormTemplate
                    {
                        FormName = formData.FormName,
                        Description = formData.FormDescription,
                        FormKey = formData.FormKey,
                        ServiceId = formData.ServiceId,
                        IsActive = formData.IsActive,
                        DisplayOrder = formData.DisplayOrder,
                        ShowInAppointmentFlow = formData.ShowInAppointmentFlow,
                        MinAge = formData.MinAge,
                        MaxAge = formData.MaxAge,
                        IconClass = formData.IconClass,
                        SuccessMessage = formData.SuccessMessage,
                        RedirectUrl = formData.RedirectUrl,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = User.Identity?.Name,
                        Version = 1
                    };

                    _context.FormTemplates.Add(form);
                }

                await _context.SaveChangesAsync();

                // Add fields
                foreach (var fieldData in formData.Fields)
                {
                    var field = new FormField
                    {
                        FormTemplateId = form.FormTemplateId,
                        FieldLabel = fieldData.FieldLabel,
                        FieldName = fieldData.FieldName,
                        FieldType = fieldData.FieldType,
                        IsRequired = fieldData.IsRequired,
                        DisplayOrder = fieldData.DisplayOrder,
                        Title = fieldData.Title,
                        HelpText = fieldData.Description,
                        ValidationPattern = fieldData.ValidationPattern,
                        FieldWidth = "col-12",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.FormFields.Add(field);
                    await _context.SaveChangesAsync(); // Save to get the FieldId

                    // Add options for choice fields
                    if (fieldData.Options != null && fieldData.Options.Any())
                    {
                        foreach (var optionData in fieldData.Options)
                        {
                            var option = new FormFieldOption
                            {
                                FormFieldId = field.FormFieldId,
                                OptionLabel = optionData.OptionLabel,
                                OptionValue = optionData.OptionValue,
                                DisplayOrder = optionData.DisplayOrder,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            };

                            _context.FormFieldOptions.Add(option);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Form '{form.FormName}' saved successfully by {User.Identity?.Name}");

                return new JsonResult(new { success = true, formId = form.FormTemplateId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving form");
                return BadRequest($"Error saving form: {ex.Message}");
            }
        }
    }

    // DTOs for form data
    public class FormBuilderData
    {
        public int FormTemplateId { get; set; }
        public string FormName { get; set; } = string.Empty;
        public string? FormDescription { get; set; }
        public string FormKey { get; set; } = string.Empty;
        public int? ServiceId { get; set; }
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; }
        public bool ShowInAppointmentFlow { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public string? IconClass { get; set; }
        public string? SuccessMessage { get; set; }
        public string? RedirectUrl { get; set; }
        public List<FormFieldData> Fields { get; set; } = new List<FormFieldData>();
    }

    public class FormFieldData
    {
        public int FormFieldId { get; set; }
        public string FieldLabel { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = "text";
        public bool IsRequired { get; set; }
        public int DisplayOrder { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? ValidationPattern { get; set; }
        public List<FormFieldOptionData> Options { get; set; } = new List<FormFieldOptionData>();
    }

    public class FormFieldOptionData
    {
        public string OptionLabel { get; set; } = string.Empty;
        public string OptionValue { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}
