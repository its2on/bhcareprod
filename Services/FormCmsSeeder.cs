using Barangay.Data;
using Barangay.Models;
using Microsoft.EntityFrameworkCore;

namespace Barangay.Services
{
    /// <summary>
    /// Seeds common form templates into the Form CMS
    /// </summary>
    public class FormCmsSeeder
    {
        private readonly ApplicationDbContext _context;

        public FormCmsSeeder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedCommonFormsAsync()
        {
            // Check if forms already exist
            if (await _context.FormTemplates.AnyAsync())
            {
                return; // Forms already seeded
            }

            await SeedPatientRegistrationFormAsync();
            await SeedHealthSurveyFormAsync();
            await SeedAppointmentFeedbackFormAsync();
            await SeedContactTracingFormAsync();

            await _context.SaveChangesAsync();
        }

        private async Task SeedPatientRegistrationFormAsync()
        {
            var form = new FormTemplate
            {
                FormName = "Patient Registration",
                FormKey = "patient-registration",
                Description = "New patient registration form for barangay health center",
                Category = "Registration",
                IconClass = "fa-solid fa-user-plus",
                IsActive = true,
                DisplayOrder = 1,
                SuccessMessage = "Your registration has been submitted successfully. Please wait for approval.",
                CreatedAt = DateTime.UtcNow,
                Version = 1,
                FormFields = new List<FormField>
                {
                    new FormField
                    {
                        FieldName = "first_name",
                        FieldLabel = "First Name",
                        FieldType = "text",
                        IsRequired = true,
                        DisplayOrder = 1,
                        FieldWidth = "col-md-4",
                        Placeholder = "Enter first name"
                    },
                    new FormField
                    {
                        FieldName = "middle_name",
                        FieldLabel = "Middle Name",
                        FieldType = "text",
                        IsRequired = false,
                        DisplayOrder = 2,
                        FieldWidth = "col-md-4",
                        Placeholder = "Enter middle name"
                    },
                    new FormField
                    {
                        FieldName = "last_name",
                        FieldLabel = "Last Name",
                        FieldType = "text",
                        IsRequired = true,
                        DisplayOrder = 3,
                        FieldWidth = "col-md-4",
                        Placeholder = "Enter last name"
                    },
                    new FormField
                    {
                        FieldName = "date_of_birth",
                        FieldLabel = "Date of Birth",
                        FieldType = "date",
                        IsRequired = true,
                        DisplayOrder = 4,
                        FieldWidth = "col-md-6",
                        HelpText = "Must be 18 years or older"
                    },
                    new FormField
                    {
                        FieldName = "gender",
                        FieldLabel = "Gender",
                        FieldType = "select",
                        IsRequired = true,
                        DisplayOrder = 5,
                        FieldWidth = "col-md-6",
                        FormFieldOptions = new List<FormFieldOption>
                        {
                            new FormFieldOption { OptionLabel = "Male", OptionValue = "Male", DisplayOrder = 1, IsActive = true },
                            new FormFieldOption { OptionLabel = "Female", OptionValue = "Female", DisplayOrder = 2, IsActive = true },
                            new FormFieldOption { OptionLabel = "Other", OptionValue = "Other", DisplayOrder = 3, IsActive = true }
                        }
                    },
                    new FormField
                    {
                        FieldName = "civil_status",
                        FieldLabel = "Civil Status",
                        FieldType = "select",
                        IsRequired = true,
                        DisplayOrder = 6,
                        FieldWidth = "col-md-6",
                        FormFieldOptions = new List<FormFieldOption>
                        {
                            new FormFieldOption { OptionLabel = "Single", OptionValue = "Single", DisplayOrder = 1, IsActive = true },
                            new FormFieldOption { OptionLabel = "Married", OptionValue = "Married", DisplayOrder = 2, IsActive = true },
                            new FormFieldOption { OptionLabel = "Widowed", OptionValue = "Widowed", DisplayOrder = 3, IsActive = true },
                            new FormFieldOption { OptionLabel = "Separated", OptionValue = "Separated", DisplayOrder = 4, IsActive = true }
                        }
                    },
                    new FormField
                    {
                        FieldName = "religion",
                        FieldLabel = "Religion",
                        FieldType = "select",
                        IsRequired = false,
                        DisplayOrder = 7,
                        FieldWidth = "col-md-6",
                        FormFieldOptions = new List<FormFieldOption>
                        {
                            new FormFieldOption { OptionLabel = "Roman Catholic", OptionValue = "Roman Catholic", DisplayOrder = 1, IsActive = true },
                            new FormFieldOption { OptionLabel = "Islam", OptionValue = "Islam", DisplayOrder = 2, IsActive = true },
                            new FormFieldOption { OptionLabel = "Iglesia ni Cristo", OptionValue = "Iglesia ni Cristo", DisplayOrder = 3, IsActive = true },
                            new FormFieldOption { OptionLabel = "Protestant", OptionValue = "Protestant", DisplayOrder = 4, IsActive = true },
                            new FormFieldOption { OptionLabel = "Others", OptionValue = "Others", DisplayOrder = 5, IsActive = true }
                        }
                    },
                    new FormField
                    {
                        FieldName = "contact_number",
                        FieldLabel = "Contact Number",
                        FieldType = "tel",
                        IsRequired = true,
                        DisplayOrder = 8,
                        FieldWidth = "col-md-6",
                        Placeholder = "+63 XXX XXX XXXX"
                    },
                    new FormField
                    {
                        FieldName = "email",
                        FieldLabel = "Email Address",
                        FieldType = "email",
                        IsRequired = true,
                        DisplayOrder = 9,
                        FieldWidth = "col-md-6",
                        Placeholder = "email@example.com"
                    },
                    new FormField
                    {
                        FieldName = "address",
                        FieldLabel = "Complete Address",
                        FieldType = "textarea",
                        IsRequired = true,
                        DisplayOrder = 10,
                        FieldWidth = "col-12",
                        Placeholder = "House No., Street, Barangay, City"
                    }
                }
            };

            _context.FormTemplates.Add(form);
        }

        private async Task SeedHealthSurveyFormAsync()
        {
            var form = new FormTemplate
            {
                FormName = "Health Screening Survey",
                FormKey = "health-screening",
                Description = "Quick health screening questionnaire for community health monitoring",
                Category = "Survey",
                IconClass = "fa-solid fa-clipboard-check",
                IsActive = true,
                DisplayOrder = 2,
                SuccessMessage = "Thank you for completing the health screening survey.",
                CreatedAt = DateTime.UtcNow,
                Version = 1,
                FormFields = new List<FormField>
                {
                    new FormField
                    {
                        FieldName = "temperature",
                        FieldLabel = "Body Temperature (°C)",
                        FieldType = "number",
                        IsRequired = true,
                        DisplayOrder = 1,
                        FieldWidth = "col-md-6",
                        HelpText = "Normal range: 36.5 - 37.5°C"
                    },
                    new FormField
                    {
                        FieldName = "has_symptoms",
                        FieldLabel = "Are you experiencing any symptoms?",
                        FieldType = "radio",
                        IsRequired = true,
                        DisplayOrder = 2,
                        FieldWidth = "col-12",
                        FormFieldOptions = new List<FormFieldOption>
                        {
                            new FormFieldOption { OptionLabel = "Yes", OptionValue = "Yes", DisplayOrder = 1, IsActive = true },
                            new FormFieldOption { OptionLabel = "No", OptionValue = "No", DisplayOrder = 2, IsActive = true, IsDefault = true }
                        }
                    },
                    new FormField
                    {
                        FieldName = "symptoms",
                        FieldLabel = "Select your symptoms (if any)",
                        FieldType = "checkbox",
                        IsRequired = false,
                        DisplayOrder = 3,
                        FieldWidth = "col-12",
                        FormFieldOptions = new List<FormFieldOption>
                        {
                            new FormFieldOption { OptionLabel = "Fever", OptionValue = "Fever", DisplayOrder = 1, IsActive = true },
                            new FormFieldOption { OptionLabel = "Cough", OptionValue = "Cough", DisplayOrder = 2, IsActive = true },
                            new FormFieldOption { OptionLabel = "Sore Throat", OptionValue = "Sore Throat", DisplayOrder = 3, IsActive = true },
                            new FormFieldOption { OptionLabel = "Difficulty Breathing", OptionValue = "Difficulty Breathing", DisplayOrder = 4, IsActive = true },
                            new FormFieldOption { OptionLabel = "Body Aches", OptionValue = "Body Aches", DisplayOrder = 5, IsActive = true },
                            new FormFieldOption { OptionLabel = "Headache", OptionValue = "Headache", DisplayOrder = 6, IsActive = true }
                        }
                    },
                    new FormField
                    {
                        FieldName = "additional_notes",
                        FieldLabel = "Additional Notes",
                        FieldType = "textarea",
                        IsRequired = false,
                        DisplayOrder = 4,
                        FieldWidth = "col-12",
                        Placeholder = "Any additional health concerns or information"
                    }
                }
            };

            _context.FormTemplates.Add(form);
        }

        private async Task SeedAppointmentFeedbackFormAsync()
        {
            var form = new FormTemplate
            {
                FormName = "Appointment Feedback",
                FormKey = "appointment-feedback",
                Description = "Share your experience with our healthcare services",
                Category = "Feedback",
                IconClass = "fa-solid fa-star",
                IsActive = true,
                DisplayOrder = 3,
                SuccessMessage = "Thank you for your valuable feedback!",
                CreatedAt = DateTime.UtcNow,
                Version = 1,
                FormFields = new List<FormField>
                {
                    new FormField
                    {
                        FieldName = "overall_rating",
                        FieldLabel = "Overall Rating",
                        FieldType = "select",
                        IsRequired = true,
                        DisplayOrder = 1,
                        FieldWidth = "col-md-6",
                        FormFieldOptions = new List<FormFieldOption>
                        {
                            new FormFieldOption { OptionLabel = " Excellent", OptionValue = "5", DisplayOrder = 1, IsActive = true },
                            new FormFieldOption { OptionLabel = " Good", OptionValue = "4", DisplayOrder = 2, IsActive = true },
                            new FormFieldOption { OptionLabel = " Average", OptionValue = "3", DisplayOrder = 3, IsActive = true },
                            new FormFieldOption { OptionLabel = " Poor", OptionValue = "2", DisplayOrder = 4, IsActive = true },
                            new FormFieldOption { OptionLabel = " Very Poor", OptionValue = "1", DisplayOrder = 5, IsActive = true }
                        }
                    },
                    new FormField
                    {
                        FieldName = "service_quality",
                        FieldLabel = "Service Quality",
                        FieldType = "radio",
                        IsRequired = true,
                        DisplayOrder = 2,
                        FieldWidth = "col-12",
                        FormFieldOptions = new List<FormFieldOption>
                        {
                            new FormFieldOption { OptionLabel = "Excellent", OptionValue = "Excellent", DisplayOrder = 1, IsActive = true },
                            new FormFieldOption { OptionLabel = "Good", OptionValue = "Good", DisplayOrder = 2, IsActive = true },
                            new FormFieldOption { OptionLabel = "Fair", OptionValue = "Fair", DisplayOrder = 3, IsActive = true },
                            new FormFieldOption { OptionLabel = "Poor", OptionValue = "Poor", DisplayOrder = 4, IsActive = true }
                        }
                    },
                    new FormField
                    {
                        FieldName = "would_recommend",
                        FieldLabel = "Would you recommend our services to others?",
                        FieldType = "radio",
                        IsRequired = true,
                        DisplayOrder = 3,
                        FieldWidth = "col-12",
                        FormFieldOptions = new List<FormFieldOption>
                        {
                            new FormFieldOption { OptionLabel = "Yes", OptionValue = "Yes", DisplayOrder = 1, IsActive = true },
                            new FormFieldOption { OptionLabel = "No", OptionValue = "No", DisplayOrder = 2, IsActive = true }
                        }
                    },
                    new FormField
                    {
                        FieldName = "comments",
                        FieldLabel = "Comments or Suggestions",
                        FieldType = "textarea",
                        IsRequired = false,
                        DisplayOrder = 4,
                        FieldWidth = "col-12",
                        Placeholder = "Please share your thoughts or suggestions for improvement"
                    }
                }
            };

            _context.FormTemplates.Add(form);
        }

        private async Task SeedContactTracingFormAsync()
        {
            var form = new FormTemplate
            {
                FormName = "Contact Tracing Form",
                FormKey = "contact-tracing",
                Description = "COVID-19 contact tracing and visitor log",
                Category = "Medical",
                IconClass = "fa-solid fa-virus",
                IsActive = false, // Inactive by default
                DisplayOrder = 4,
                SuccessMessage = "Contact tracing information recorded successfully.",
                CreatedAt = DateTime.UtcNow,
                Version = 1,
                FormFields = new List<FormField>
                {
                    new FormField
                    {
                        FieldName = "full_name",
                        FieldLabel = "Full Name",
                        FieldType = "text",
                        IsRequired = true,
                        DisplayOrder = 1,
                        FieldWidth = "col-md-6"
                    },
                    new FormField
                    {
                        FieldName = "contact_number",
                        FieldLabel = "Contact Number",
                        FieldType = "tel",
                        IsRequired = true,
                        DisplayOrder = 2,
                        FieldWidth = "col-md-6"
                    },
                    new FormField
                    {
                        FieldName = "visit_date",
                        FieldLabel = "Date of Visit",
                        FieldType = "date",
                        IsRequired = true,
                        DisplayOrder = 3,
                        FieldWidth = "col-md-6"
                    },
                    new FormField
                    {
                        FieldName = "visit_time",
                        FieldLabel = "Time of Visit",
                        FieldType = "time",
                        IsRequired = true,
                        DisplayOrder = 4,
                        FieldWidth = "col-md-6"
                    },
                    new FormField
                    {
                        FieldName = "purpose",
                        FieldLabel = "Purpose of Visit",
                        FieldType = "textarea",
                        IsRequired = true,
                        DisplayOrder = 5,
                        FieldWidth = "col-12"
                    }
                }
            };

            _context.FormTemplates.Add(form);
        }
    }
}
