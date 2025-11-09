using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using Barangay.Extensions;
using System.Text.Json;
using System.Security.Claims;

namespace Barangay.Pages.Forms
{
    public class SubmitFormModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubmitFormModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDataEncryptionService _encryptionService;

        public SubmitFormModel(
            ApplicationDbContext context, 
            ILogger<SubmitFormModel> logger,
            UserManager<ApplicationUser> userManager,
            IDataEncryptionService encryptionService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _encryptionService = encryptionService;
        }

        public FormTemplate FormTemplate { get; set; } = null!;
        public bool IsSubmitted { get; set; } = false;
        public string? ErrorMessage { get; set; }
        public int? AppointmentId { get; set; }
        public Appointment? Appointment { get; set; }
        public ApplicationUser? CurrentUser { get; set; }
        public Patient? PatientData { get; set; }
        
        // Dictionary to hold prefilled values for form fields
        public Dictionary<string, string> PrefilledValues { get; set; } = new Dictionary<string, string>();
        
        // Fields that should be readonly (not editable)
        public HashSet<string> ReadonlyFields { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public async Task<IActionResult> OnGetAsync(string formKey, int? appointmentId = null)
        {
            FormTemplate = await _context.FormTemplates
                .Include(f => f.FormFields)
                .ThenInclude(ff => ff.FormFieldOptions)
                .FirstOrDefaultAsync(f => f.FormKey == formKey && f.IsActive);

            if (FormTemplate == null)
            {
                return NotFound("Form not found or is not active.");
            }

            // Load appointment context if provided
            if (appointmentId.HasValue)
            {
                AppointmentId = appointmentId.Value;
                Appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId.Value);
                
                if (Appointment == null)
                {
                    return NotFound("Appointment not found.");
                }

                // Log appointment context for debugging
                _logger.LogInformation("=== SUBMIT FORM - APPOINTMENT CONTEXT ===");
                _logger.LogInformation("Appointment ID: {AppointmentId}", Appointment.Id);
                _logger.LogInformation("BookingForOther: {BookingForOther}", Appointment.BookingForOther);
                _logger.LogInformation("PatientName (Booker): {PatientName}", Appointment.PatientName);
                _logger.LogInformation("DependentFullName: {DependentFullName}", Appointment.DependentFullName ?? "NULL");
                _logger.LogInformation("DependentAge: {DependentAge}", Appointment.DependentAge?.ToString() ?? "NULL");
                _logger.LogInformation("AgeValue: {AgeValue}", Appointment.AgeValue);
                _logger.LogInformation("FamilyNumber: {FamilyNumber}", Appointment.FamilyNumber ?? "NULL");
                _logger.LogInformation("Relationship: {Relationship}", Appointment.Relationship ?? "NULL");
                _logger.LogInformation("=========================================");

                // Verify age restrictions if appointment-based form
                // Use the correct age: DependentAge if booking for someone else, otherwise use Patient.Age or AgeValue
                if (FormTemplate.MinAge.HasValue || FormTemplate.MaxAge.HasValue)
                {
                    var age = Appointment.DependentAge ?? Appointment.AgeValue;
                    
                    if (FormTemplate.MinAge.HasValue && age < FormTemplate.MinAge.Value)
                    {
                        ErrorMessage = $"This form requires a minimum age of {FormTemplate.MinAge.Value}. Patient is {age} years old.";
                        return Page();
                    }
                    
                    if (FormTemplate.MaxAge.HasValue && age > FormTemplate.MaxAge.Value)
                    {
                        ErrorMessage = $"This form requires a maximum age of {FormTemplate.MaxAge.Value}. Patient is {age} years old.";
                        return Page();
                    }
                }
            }

            // Load user and patient data for prefilling
            await LoadPrefillDataAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string formKey, int? appointmentId = null)
        {
            _logger.LogInformation("=== FORM SUBMISSION START ===");
            _logger.LogInformation("FormKey received: {FormKey}", formKey);
            _logger.LogInformation("AppointmentId received: {AppointmentId}", appointmentId);
            _logger.LogInformation("Request.Form keys count: {Count}", Request.Form.Keys.Count);
            _logger.LogInformation("Request.Form keys: {Keys}", string.Join(", ", Request.Form.Keys));
            
            FormTemplate = await _context.FormTemplates
                .Include(f => f.FormFields)
                .ThenInclude(ff => ff.FormFieldOptions)
                .FirstOrDefaultAsync(f => f.FormKey == formKey && f.IsActive);

            if (FormTemplate == null)
            {
                _logger.LogError("FormTemplate not found for key: {FormKey}", formKey);
                return NotFound("Form not found or is not active.");
            }
            
            _logger.LogInformation("FormTemplate found: {FormName} (ID: {FormTemplateId})", FormTemplate.FormName, FormTemplate.FormTemplateId);

            // Load appointment context if provided
            if (appointmentId.HasValue)
            {
                AppointmentId = appointmentId.Value;
                Appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId.Value);
                
                if (Appointment != null)
                {
                    // Log appointment context for debugging
                    _logger.LogInformation("=== SUBMIT FORM POST - APPOINTMENT CONTEXT ===");
                    _logger.LogInformation("Appointment ID: {AppointmentId}", Appointment.Id);
                    _logger.LogInformation("BookingForOther: {BookingForOther}", Appointment.BookingForOther);
                    _logger.LogInformation("PatientName (Booker): {PatientName}", Appointment.PatientName);
                    _logger.LogInformation("DependentFullName: {DependentFullName}", Appointment.DependentFullName ?? "NULL");
                    _logger.LogInformation("DependentAge: {DependentAge}", Appointment.DependentAge?.ToString() ?? "NULL");
                    _logger.LogInformation("==============================================");
                }
            }

            try
            {
                // Collect form data
                var formData = new Dictionary<string, string>();

                _logger.LogInformation("=== FIELD-BY-FIELD DEBUGGING ===");
                _logger.LogInformation("Total fields in template: {Count}", FormTemplate.FormFields.Count);

                foreach (var field in FormTemplate.FormFields)
                {
                    string value = string.Empty;
                    bool fieldExistsInForm = Request.Form.ContainsKey(field.FieldName);

                    // Debug: Log field information
                    _logger.LogInformation("--- Processing Field: {FieldName} ({FieldLabel}) ---", field.FieldName, field.FieldLabel);
                    _logger.LogInformation("Field Type: {FieldType}, IsRequired: {IsRequired}, DisplayOrder: {DisplayOrder}", 
                        field.FieldType, field.IsRequired, field.DisplayOrder);
                    _logger.LogInformation("Field exists in Request.Form: {Exists}", fieldExistsInForm);

                    // Handle different field types
                    if (field.FieldType == "checkbox")
                    {
                        // For checkbox fields, combine multiple values
                        var values = Request.Form[field.FieldName];
                        if (values.Count > 0)
                        {
                            value = string.Join(", ", values);
                            _logger.LogInformation("CHECKBOX - Selected values: {Values}", value);
                        }
                        else
                        {
                            value = string.Empty; // No checkboxes selected
                            _logger.LogInformation("CHECKBOX - No values selected (empty)");
                        }
                    }
                    else if (field.FieldType == "radio")
                    {
                        // For radio buttons, get the selected value
                        var radioValue = Request.Form[field.FieldName].ToString();
                        if (!string.IsNullOrWhiteSpace(radioValue))
                        {
                            value = radioValue;
                            _logger.LogInformation("RADIO - Selected value: {Value}", value);
                        }
                        else
                        {
                            value = string.Empty; // No radio button selected
                            _logger.LogInformation("RADIO - No option selected (empty)");
                        }
                    }
                    else if (field.FieldType == "button" || field.FieldType == "submit")
                    {
                        // Buttons don't typically submit values, but check if clicked
                        var buttonValue = Request.Form[field.FieldName].ToString();
                        value = buttonValue;
                        _logger.LogInformation("BUTTON - Value: {Value} (exists: {Exists})", value, fieldExistsInForm);
                    }
                    else
                    {
                        // For text, textarea, select, date, number, etc.
                        value = Request.Form[field.FieldName].ToString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            _logger.LogInformation("TEXTBOX/SELECT - Value: {Value}", value);
                        }
                        else
                        {
                            _logger.LogInformation("TEXTBOX/SELECT - Empty value");
                        }
                    }

                    // NOTE: Required validation removed - all fields are now optional
                    // Save ALL fields to database (even if empty/optional)
                    // Empty string for optional fields that weren't filled
                    formData[field.FieldName] = value ?? string.Empty;
                    _logger.LogInformation("Saved to formData: {FieldName} = '{Value}' (Required: {IsRequired})", 
                        field.FieldName, value ?? "(null)", field.IsRequired);
                }

                _logger.LogInformation("=== END FIELD-BY-FIELD DEBUGGING ===");
                _logger.LogInformation("Total fields saved to formData: {Count}", formData.Count);
                
                // Log summary of all saved fields
                _logger.LogInformation("=== FORM DATA SUMMARY ===");
                foreach (var kvp in formData.OrderBy(f => f.Key))
                {
                    var field = FormTemplate.FormFields.FirstOrDefault(ff => ff.FieldName == kvp.Key);
                    var fieldType = field?.FieldType ?? "unknown";
                    var isRequired = field?.IsRequired ?? false;
                    var isEmpty = string.IsNullOrWhiteSpace(kvp.Value);
                    _logger.LogInformation("Field: {FieldName} | Type: {FieldType} | Required: {IsRequired} | Empty: {IsEmpty} | Value: '{Value}'", 
                        kvp.Key, fieldType, isRequired, isEmpty, isEmpty ? "(empty)" : kvp.Value);
                }
                _logger.LogInformation("=== END FORM DATA SUMMARY ===");

                // Get the current user's ID if authenticated
                string? userId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    userId = currentUser?.Id;
                }

                // Create submission record
                var submission = new FormSubmission
                {
                    FormTemplateId = FormTemplate.FormTemplateId,
                    UserId = userId, // Use actual User ID (GUID), not username
                    AppointmentId = AppointmentId, // Link to appointment if provided
                    FormData = JsonSerializer.Serialize(formData),
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    Status = "Submitted",
                    SubmittedAt = DateTime.UtcNow
                };

                _logger.LogInformation($"Creating form submission: FormTemplateId={FormTemplate.FormTemplateId}, FormName='{FormTemplate.FormName}', UserId={userId}, AppointmentId={AppointmentId}");

                _context.FormSubmissions.Add(submission);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Form submission {submission.FormSubmissionId} for form '{FormTemplate.FormName}' saved successfully with AppointmentId={submission.AppointmentId}");

                // Update appointment status to Pending after successful form submission
                // Pending = waiting for nurse/doctor to start consultation
                // InProgress = nurse/doctor has started the consultation (set by nurse/doctor, not by form)
                if (AppointmentId.HasValue)
                {
                    _logger.LogInformation("Updating appointment status to Pending for AppointmentId: {AppointmentId}", AppointmentId.Value);
                    var appointment = await _context.Appointments.FindAsync(AppointmentId.Value);
                    if (appointment != null)
                    {
                        var oldStatus = appointment.Status;
                        appointment.Status = AppointmentStatus.Pending; // Ready for nurse/doctor review
                        appointment.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Appointment {AppointmentId} status updated from {OldStatus} to Pending (form completed)", 
                            AppointmentId.Value, oldStatus);
                    }
                    else
                    {
                        _logger.LogWarning("Appointment not found for ID: {AppointmentId}", AppointmentId.Value);
                    }
                }

                IsSubmitted = true;
                TempData["FormSubmitted"] = true;
                
                _logger.LogInformation("=== FORM SUBMISSION SUCCESS ===");
                _logger.LogInformation("SubmissionId: {SubmissionId}", submission.FormSubmissionId);
                _logger.LogInformation("FormName: {FormName}", FormTemplate.FormName);
                _logger.LogInformation("AppointmentId: {AppointmentId}", submission.AppointmentId);
                _logger.LogInformation("IsSubmitted: {IsSubmitted}", IsSubmitted);
                _logger.LogInformation("TempData[FormSubmitted]: {TempData}", TempData["FormSubmitted"]);
                
                // Reload prefill data so page can render properly
                await LoadPrefillDataAsync();
                
                _logger.LogInformation("=== RETURNING PAGE WITH SUCCESS MODAL ===");
                
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error submitting form '{FormTemplate.FormName}'");
                _logger.LogError("=== FORM SUBMISSION FAILED ===");
                _logger.LogError("Exception: {Exception}", ex.Message);
                _logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);
                
                ErrorMessage = "An error occurred while submitting the form. Please try again.";
                await LoadPrefillDataAsync(); // Reload data for page display
                return Page();
            }
        }

        // API endpoint to search for family members by family number
        public async Task<JsonResult> OnGetSearchFamilyByNumberAsync(string familyNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(familyNumber))
                {
                    return new JsonResult(new { success = false, error = "Family number is required" });
                }

                _logger.LogInformation("Searching for family members with family number: {FamilyNumber}", familyNumber);

                // Search in ApplicationUsers (AspNetUsers table)
                var usersList = await _context.Users
                    .Where(u => u.FamilyNumber == familyNumber)
                    .ToListAsync();

                // Project to anonymous type with consistent types
                var users = usersList.Select(u => new
                {
                    fullName = (u.FirstName ?? "") + " " + (u.LastName ?? ""),
                    firstName = u.FirstName ?? "",
                    lastName = u.LastName ?? "",
                    middleName = u.MiddleName ?? "",
                    address = u.Address ?? "",
                    barangay = u.Barangay ?? "",
                    phoneNumber = u.PhoneNumber ?? "",
                    birthDate = u.BirthDate,
                    age = int.TryParse(u.Age ?? "", out var parsedAge) ? (int?)parsedAge : null,
                    gender = u.Gender ?? "",
                    familyNumber = u.FamilyNumber
                }).ToList();

                // Also search in Patients table
                var patientsList = await _context.Patients
                    .Where(p => p.FamilyNumber == familyNumber)
                    .ToListAsync();

                // Project to anonymous type after materialization to access computed Age property
                var patients = patientsList.Select(p => new
                {
                    fullName = p.FullName ?? "",
                    firstName = ExtractFirstName(p.FullName ?? ""),
                    lastName = ExtractLastName(p.FullName ?? ""),
                    middleName = "",
                    address = p.Address ?? "",
                    barangay = "",
                    phoneNumber = p.ContactNumber ?? "",
                    birthDate = (DateTime?)p.BirthDate,
                    age = (int?)p.Age,
                    gender = p.Gender ?? "",
                    familyNumber = p.FamilyNumber
                }).ToList();

                // Combine and deduplicate by full name
                var allMembers = users
                    .Concat(patients)
                    .GroupBy(m => m.fullName)
                    .Select(g => g.First())
                    .OrderBy(m => m.fullName)
                    .ToList();

                _logger.LogInformation($"Found {allMembers.Count} family members with family number {familyNumber}");

                return new JsonResult(new { success = true, members = allMembers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching family members by family number: {FamilyNumber}", familyNumber);
                return new JsonResult(new { success = false, error = "An error occurred while searching" });
            }
        }

        private string ExtractFirstName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "";
            var parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : "";
        }

        private string ExtractLastName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "";
            var parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? parts[parts.Length - 1] : parts.Length > 0 ? parts[0] : "";
        }

        private int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age;
        }

        /// <summary>
        /// Normalizes a field name by removing spaces, parentheses, and special characters
        /// </summary>
        private string NormalizeFieldName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName)) return "";
            return System.Text.RegularExpressions.Regex.Replace(fieldName.ToLower(), "[^a-z0-9]", "");
        }

        /// <summary>
        /// Loads user and patient data, then builds prefilled values for form fields
        /// </summary>
        private async Task LoadPrefillDataAsync()
        {
            try
            {
                // Get current logged-in user
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("No authenticated user found for prefilling");
                    return;
                }

                // Decrypt user data for authorized users
                user = user.DecryptSensitiveData(_encryptionService, User);
                
                // Manually decrypt Email and PhoneNumber since they're not marked with [Encrypted] attribute
                if (!string.IsNullOrEmpty(user.Email) && _encryptionService.IsEncrypted(user.Email))
                {
                    user.Email = user.Email.DecryptForUser(_encryptionService, User);
                }
                if (!string.IsNullOrEmpty(user.PhoneNumber) && _encryptionService.IsEncrypted(user.PhoneNumber))
                {
                    user.PhoneNumber = user.PhoneNumber.DecryptForUser(_encryptionService, User);
                }

                CurrentUser = user;

                // Get patient data if it exists
                PatientData = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);
                
                // Decrypt patient data if exists
                if (PatientData != null)
                {
                    PatientData = PatientData.DecryptSensitiveData(_encryptionService, User);
                }

                _logger.LogInformation("=== PREFILL DATA LOADED ===");
                _logger.LogInformation("User: {UserName}, Patient Data: {HasPatient}", user.UserName, PatientData != null);
                
                // Determine if this is for a dependent (booking for someone else)
                bool isForDependent = Appointment?.BookingForOther == true;
                string? dependentName = isForDependent ? Appointment?.DependentFullName : null;
                int? dependentAge = isForDependent ? Appointment?.DependentAge : null;
                DateTime? dependentBirthday = isForDependent ? Appointment?.DateOfBirth : null;
                string? dependentGender = isForDependent ? Appointment?.Gender : null;
                
                _logger.LogInformation("Is For Dependent: {IsForDependent}, Dependent Name: {DependentName}", 
                    isForDependent, dependentName ?? "N/A");

                // Build prefilled values dictionary
                BuildPrefilledValues(user, PatientData, isForDependent, dependentName, dependentAge, dependentBirthday, dependentGender);
                
                _logger.LogInformation("Prefilled {Count} fields", PrefilledValues.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading prefill data");
            }
        }

        /// <summary>
        /// Builds the prefilled values dictionary based on user and patient data
        /// </summary>
        private void BuildPrefilledValues(ApplicationUser user, Patient? patient, bool isForDependent, 
            string? dependentName, int? dependentAge, DateTime? dependentBirthday, string? dependentGender)
        {
            PrefilledValues.Clear();
            ReadonlyFields.Clear();

            // Health Facility - prefilled + readonly
            PrefilledValues["health_facility"] = "Baesa Health Center";
            PrefilledValues["healthfacility"] = "Baesa Health Center";
            PrefilledValues["facility"] = "Baesa Health Center";
            ReadonlyFields.Add("health_facility");
            ReadonlyFields.Add("healthfacility");
            ReadonlyFields.Add("facility");

            // Family Number - prefilled + readonly
            var familyNumber = Appointment?.FamilyNumber ?? user.FamilyNumber ?? patient?.FamilyNumber ?? "";
            if (!string.IsNullOrEmpty(familyNumber))
            {
                PrefilledValues["family_no"] = familyNumber;
                PrefilledValues["familyno"] = familyNumber;
                PrefilledValues["family_number"] = familyNumber;
                PrefilledValues["familynumber"] = familyNumber;
                ReadonlyFields.Add("family_no");
                ReadonlyFields.Add("familyno");
                ReadonlyFields.Add("family_number");
                ReadonlyFields.Add("familynumber");
            }

            // Determine which person's data to use (dependent vs. user)
            string firstName, middleName, lastName, fullName, address, barangay, contactNumber, gender;
            DateTime? birthDate;
            int age;

            if (isForDependent && !string.IsNullOrEmpty(dependentName))
            {
                // Use dependent's data
                fullName = dependentName;
                var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                firstName = nameParts.Length > 0 ? nameParts[0] : "";
                middleName = nameParts.Length > 2 ? nameParts[1] : "";
                lastName = nameParts.Length > 1 ? nameParts[nameParts.Length - 1] : "";
                
                address = Appointment?.Address ?? user.Address ?? "";
                barangay = user.Barangay ?? "";
                contactNumber = Appointment?.ContactNumber ?? user.PhoneNumber ?? "";
                gender = dependentGender ?? "";
                birthDate = dependentBirthday;
                age = dependentAge ?? (birthDate.HasValue ? CalculateAge(birthDate.Value) : 0);
            }
            else
            {
                // Use user's data
                firstName = user.FirstName ?? "";
                middleName = user.MiddleName ?? "";
                lastName = user.LastName ?? "";
                fullName = patient?.FullName ?? user.FullName ?? "";
                address = patient?.Address ?? user.Address ?? "";
                barangay = user.Barangay ?? "";
                contactNumber = patient?.ContactNumber ?? user.PhoneNumber ?? "";
                gender = patient?.Gender ?? user.Gender ?? "";
                birthDate = patient?.BirthDate ?? user.BirthDate;
                age = patient?.Age ?? (birthDate.HasValue ? CalculateAge(birthDate.Value) : 
                    (int.TryParse(user.Age, out int userAge) ? userAge : 0));
            }

            // Last Name (Apelyido) - prefilled + readonly
            // Add multiple variations to handle different field naming conventions
            PrefilledValues["last_name"] = lastName;
            PrefilledValues["lastname"] = lastName;
            PrefilledValues["apelyido"] = lastName;
            PrefilledValues["apelyido(lastname)"] = lastName; // Handle "Apelyido (Last Name)"
            PrefilledValues["apelyidolastname"] = lastName; // No spaces/parentheses
            PrefilledValues["surname"] = lastName;
            ReadonlyFields.Add("last_name");
            ReadonlyFields.Add("lastname");
            ReadonlyFields.Add("apelyido");
            ReadonlyFields.Add("apelyido(lastname)");
            ReadonlyFields.Add("apelyidolastname");
            ReadonlyFields.Add("surname");

            // First Name (Unang Pangalan) - prefilled + readonly
            PrefilledValues["first_name"] = firstName;
            PrefilledValues["firstname"] = firstName;
            PrefilledValues["unang_pangalan"] = firstName;
            PrefilledValues["unangpangalan"] = firstName; // No spaces
            PrefilledValues["unangpangalan(firstname)"] = firstName; // Handle "Unang Pangalan (First Name)"
            PrefilledValues["unangpangalanfirstname"] = firstName; // Fully normalized
            PrefilledValues["given_name"] = firstName;
            PrefilledValues["givenname"] = firstName;
            ReadonlyFields.Add("first_name");
            ReadonlyFields.Add("firstname");
            ReadonlyFields.Add("unang_pangalan");
            ReadonlyFields.Add("unangpangalan");
            ReadonlyFields.Add("unangpangalan(firstname)");
            ReadonlyFields.Add("unangpangalanfirstname");
            ReadonlyFields.Add("given_name");
            ReadonlyFields.Add("givenname");

            // Middle Name (Gitnang Pangalan) - prefilled + readonly
            PrefilledValues["middle_name"] = middleName;
            PrefilledValues["middlename"] = middleName;
            PrefilledValues["gitnang_pangalan"] = middleName;
            PrefilledValues["gitnangpangalan"] = middleName; // No spaces
            PrefilledValues["gitnangpangalan(middlename)"] = middleName; // Handle "Gitnang Pangalan (Middle Name)"
            PrefilledValues["gitnangpangalanmiddlename"] = middleName; // Fully normalized
            ReadonlyFields.Add("middle_name");
            ReadonlyFields.Add("middlename");
            ReadonlyFields.Add("gitnang_pangalan");
            ReadonlyFields.Add("gitnangpangalan");
            ReadonlyFields.Add("gitnangpangalan(middlename)");
            ReadonlyFields.Add("gitnangpangalanmiddlename");

            // Full Name - prefilled + readonly
            PrefilledValues["full_name"] = fullName;
            PrefilledValues["fullname"] = fullName;
            PrefilledValues["name"] = fullName;
            PrefilledValues["pangalan"] = fullName;
            PrefilledValues["buongpangalan"] = fullName;
            ReadonlyFields.Add("full_name");
            ReadonlyFields.Add("fullname");
            ReadonlyFields.Add("name");
            ReadonlyFields.Add("pangalan");
            ReadonlyFields.Add("buongpangalan");

            // Address - prefilled + readonly
            PrefilledValues["address"] = address;
            PrefilledValues["tirahan"] = address;
            ReadonlyFields.Add("address");
            ReadonlyFields.Add("tirahan");

            // Barangay - prefilled + readonly
            PrefilledValues["barangay"] = barangay;
            ReadonlyFields.Add("barangay");

            // Contact Number (Telepono) - prefilled + readonly
            PrefilledValues["contact_number"] = contactNumber;
            PrefilledValues["contactnumber"] = contactNumber;
            PrefilledValues["phone"] = contactNumber;
            PrefilledValues["phone_number"] = contactNumber;
            PrefilledValues["telepono"] = contactNumber;
            ReadonlyFields.Add("contact_number");
            ReadonlyFields.Add("contactnumber");
            ReadonlyFields.Add("phone");
            ReadonlyFields.Add("phone_number");
            ReadonlyFields.Add("telepono");

            // Birthday - prefilled + readonly
            if (birthDate.HasValue)
            {
                PrefilledValues["birthday"] = birthDate.Value.ToString("yyyy-MM-dd");
                PrefilledValues["birthdate"] = birthDate.Value.ToString("yyyy-MM-dd");
                PrefilledValues["birth_date"] = birthDate.Value.ToString("yyyy-MM-dd");
                PrefilledValues["date_of_birth"] = birthDate.Value.ToString("yyyy-MM-dd");
                PrefilledValues["kaarawan"] = birthDate.Value.ToString("yyyy-MM-dd");
                ReadonlyFields.Add("birthday");
                ReadonlyFields.Add("birthdate");
                ReadonlyFields.Add("birth_date");
                ReadonlyFields.Add("date_of_birth");
                ReadonlyFields.Add("kaarawan");
            }

            // Age (Edad) - prefilled + readonly
            PrefilledValues["age"] = age.ToString();
            PrefilledValues["edad"] = age.ToString();
            PrefilledValues["edad(age)"] = age.ToString(); // Handle "Edad (Age)"
            PrefilledValues["edadage"] = age.ToString(); // No spaces/parentheses
            ReadonlyFields.Add("age");
            ReadonlyFields.Add("edad");
            ReadonlyFields.Add("edad(age)");
            ReadonlyFields.Add("edadage");

            // Gender (Kasarian) - prefilled + readonly
            // Use the EXACT same values as stored in database from signup: "Male" or "Female"
            PrefilledValues["gender"] = gender;
            PrefilledValues["sex"] = gender;
            PrefilledValues["kasarian"] = gender;
            PrefilledValues["kasarian(sex)"] = gender;
            PrefilledValues["kasariansex"] = gender;
            
            ReadonlyFields.Add("gender");
            ReadonlyFields.Add("sex");
            ReadonlyFields.Add("kasarian");
            ReadonlyFields.Add("kasarian(sex)");
            ReadonlyFields.Add("kasariansex");

            // Date of Assessment - prefilled + readonly (current date)
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            PrefilledValues["date_of_assessment"] = today;
            PrefilledValues["dateofassessment"] = today;
            PrefilledValues["assessment_date"] = today;
            PrefilledValues["assessmentdate"] = today;
            PrefilledValues["petsa"] = today;
            ReadonlyFields.Add("date_of_assessment");
            ReadonlyFields.Add("dateofassessment");
            ReadonlyFields.Add("assessment_date");
            ReadonlyFields.Add("assessmentdate");
            ReadonlyFields.Add("petsa");

            // NOTE: Religion and Civil Status are intentionally NOT prefilled
            // These remain editable for the user to fill in
            
            _logger.LogInformation("Prefilled Name: {FirstName} {LastName}, Age: {Age}, Gender: '{Gender}'", 
                firstName, lastName, age, gender);
            
            // Log gender/kasarian mappings for debugging
            if (PrefilledValues.ContainsKey("kasarian"))
            {
                _logger.LogInformation("Kasarian prefilled: '{Value}'", PrefilledValues["kasarian"]);
            }
        }
    }
}

