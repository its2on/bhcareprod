using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using Barangay.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Collections.Generic;

namespace Barangay.Pages.Nurse
{
    [Authorize(Roles = "Nurse,Head Nurse")]
    public class ImmunizationRecordModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ImmunizationRecordModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDataEncryptionService _encryptionService;
        private readonly IAuthorizationService _authorizationService;
        private readonly IFamilyNumberService _familyNumberService;

        public ImmunizationRecordModel(
            ApplicationDbContext context, 
            ILogger<ImmunizationRecordModel> logger,
            UserManager<ApplicationUser> userManager,
            IDataEncryptionService encryptionService,
            IAuthorizationService authorizationService,
            IFamilyNumberService familyNumberService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _authorizationService = authorizationService;
            _familyNumberService = familyNumberService;
        }

        [BindProperty(SupportsGet = true)]
        public int AppointmentId { get; set; }

        // Child Information
        [BindProperty]
        [Required(ErrorMessage = "Child's first name is required")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Only letters and spaces are allowed")]
        public string ChildFirstName { get; set; }
        
        [BindProperty]
        [RegularExpression(@"^[a-zA-Z\s]*$", ErrorMessage = "Only letters and spaces are allowed")]
        public string ChildMiddleName { get; set; }
        
        [BindProperty]
        [Required(ErrorMessage = "Child's last name is required")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Only letters and spaces are allowed")]
        public string ChildLastName { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Date of birth is required")]
        public string DateOfBirth { get; set; }

        [BindProperty]
        [RegularExpression(@"^[a-zA-Z\s,.-]*$", ErrorMessage = "Only letters, spaces, commas, periods, and hyphens are allowed")]
        public string PlaceOfBirth { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Sex is required")]
        public string Sex { get; set; }

        [BindProperty]
        [RegularExpression(@"^[0-9.]*$", ErrorMessage = "Only numbers and decimal points are allowed")]
        public string BirthWeight { get; set; }

        [BindProperty]
        [RegularExpression(@"^[0-9.]*$", ErrorMessage = "Only numbers and decimal points are allowed")]
        public string BirthHeight { get; set; }

        [BindProperty]
        public string Address { get; set; }

        // Mother Information
        [BindProperty]
        [RegularExpression(@"^[a-zA-Z\s]*$", ErrorMessage = "Only letters and spaces are allowed")]
        public string MotherFirstName { get; set; }
        
        [BindProperty]
        [RegularExpression(@"^[a-zA-Z\s]*$", ErrorMessage = "Only letters and spaces are allowed")]
        public string MotherMiddleName { get; set; }
        
        [BindProperty]
        [RegularExpression(@"^[a-zA-Z\s]*$", ErrorMessage = "Only letters and spaces are allowed")]
        public string MotherLastName { get; set; }

        // Father Information
        [BindProperty]
        [RegularExpression(@"^[a-zA-Z\s]*$", ErrorMessage = "Only letters and spaces are allowed")]
        public string FatherFirstName { get; set; }
        
        [BindProperty]
        [RegularExpression(@"^[a-zA-Z\s]*$", ErrorMessage = "Only letters and spaces are allowed")]
        public string FatherMiddleName { get; set; }
        
        [BindProperty]
        [RegularExpression(@"^[a-zA-Z\s]*$", ErrorMessage = "Only letters and spaces are allowed")]
        public string FatherLastName { get; set; }

        [BindProperty]
        [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "Only numbers, +, -, spaces, and parentheses are allowed")]
        public string ContactNumber { get; set; }

        [BindProperty]
        public string HealthCenter { get; set; } = "Barangay Health Care Center";

        [BindProperty]
        [Required(ErrorMessage = "Barangay is required")]
        public string Barangay { get; set; }

        [BindProperty]
        public string FamilyNumber { get; set; }

        // Complete vaccine properties
        [BindProperty] public string BCGVaccineDate { get; set; }
        [BindProperty] public string BCGVaccineRemarks { get; set; }
        
        [BindProperty] public string HepBBirthDate { get; set; }
        [BindProperty] public string HepBBirthRemarks { get; set; }
        
        [BindProperty] public string HepB1Date { get; set; }
        [BindProperty] public string HepB1Remarks { get; set; }
        
        [BindProperty] public string HepB2Date { get; set; }
        [BindProperty] public string HepB2Remarks { get; set; }
        
        [BindProperty] public string Pentavalent1Date { get; set; }
        [BindProperty] public string Pentavalent1Remarks { get; set; }
        
        [BindProperty] public string Pentavalent2Date { get; set; }
        [BindProperty] public string Pentavalent2Remarks { get; set; }
        
        [BindProperty] public string Pentavalent3Date { get; set; }
        [BindProperty] public string Pentavalent3Remarks { get; set; }
        
        [BindProperty] public string OPV1Date { get; set; }
        [BindProperty] public string OPV1Remarks { get; set; }
        
        [BindProperty] public string OPV2Date { get; set; }
        [BindProperty] public string OPV2Remarks { get; set; }
        
        [BindProperty] public string OPV3Date { get; set; }
        [BindProperty] public string OPV3Remarks { get; set; }
        
        [BindProperty] public string IPV1Date { get; set; }
        [BindProperty] public string IPV1Remarks { get; set; }
        
        [BindProperty] public string IPV2Date { get; set; }
        [BindProperty] public string IPV2Remarks { get; set; }
        
        [BindProperty] public string PCV1Date { get; set; }
        [BindProperty] public string PCV1Remarks { get; set; }
        
        [BindProperty] public string PCV2Date { get; set; }
        [BindProperty] public string PCV2Remarks { get; set; }
        
        [BindProperty] public string PCV3Date { get; set; }
        [BindProperty] public string PCV3Remarks { get; set; }
        
        [BindProperty] public string MMR1Date { get; set; }
        [BindProperty] public string MMR1Remarks { get; set; }
        
        [BindProperty] public string MMR2Date { get; set; }
        [BindProperty] public string MMR2Remarks { get; set; }

        public Appointment Appointment { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                _logger.LogInformation("Loading immunization record page for appointment {AppointmentId}", AppointmentId);

                // Load appointment details
                Appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.Id == AppointmentId);

                if (Appointment == null)
                {
                    StatusMessage = "Error: Appointment not found.";
                    return RedirectToPage("/Nurse/Appointments");
                }

                // TEMPORARY BYPASS: Skip decryption if user is not authenticated
                // Decrypt appointment data only if user is authenticated
                if (User.Identity?.IsAuthenticated == true)
                {
                    if (!string.IsNullOrEmpty(Appointment.PatientName) && _encryptionService.IsEncrypted(Appointment.PatientName))
                    {
                        Appointment.PatientName = _encryptionService.DecryptForUser(Appointment.PatientName, User);
                    }
                    
                    if (!string.IsNullOrEmpty(Appointment.DependentFullName) && _encryptionService.IsEncrypted(Appointment.DependentFullName))
                    {
                        Appointment.DependentFullName = _encryptionService.DecryptForUser(Appointment.DependentFullName, User);
                    }

                    if (!string.IsNullOrEmpty(Appointment.Address) && _encryptionService.IsEncrypted(Appointment.Address))
                    {
                        Appointment.Address = _encryptionService.DecryptForUser(Appointment.Address, User);
                    }

                    if (!string.IsNullOrEmpty(Appointment.ContactNumber) && _encryptionService.IsEncrypted(Appointment.ContactNumber))
                    {
                        Appointment.ContactNumber = _encryptionService.DecryptForUser(Appointment.ContactNumber, User);
                    }
                }
                else
                {
                    _logger.LogWarning("BYPASS MODE: Skipping decryption due to unauthenticated user");
                }

                // Pre-populate child information from appointment
                if (!string.IsNullOrEmpty(Appointment.DependentFullName))
                {
                    // Split dependent name (assuming format: FirstName MiddleName LastName)
                    var nameParts = Appointment.DependentFullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (nameParts.Length == 1)
                    {
                        // Only first name
                        ChildFirstName = nameParts[0];
                    }
                    else if (nameParts.Length == 2)
                    {
                        // First and last name
                        ChildFirstName = nameParts[0];
                        ChildLastName = nameParts[1];
                    }
                    else if (nameParts.Length >= 3)
                    {
                        // First, middle, and last name
                        ChildFirstName = nameParts[0];
                        ChildLastName = nameParts[nameParts.Length - 1];
                        ChildMiddleName = string.Join(" ", nameParts.Skip(1).Take(nameParts.Length - 2));
                    }
                }
                else if (!string.IsNullOrEmpty(Appointment.PatientName))
                {
                    // Fallback: if DependentFullName is empty, try PatientName (might be the child)
                    var nameParts = Appointment.PatientName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (nameParts.Length == 1)
                    {
                        ChildFirstName = nameParts[0];
                    }
                    else if (nameParts.Length == 2)
                    {
                        ChildFirstName = nameParts[0];
                        ChildLastName = nameParts[1];
                    }
                    else if (nameParts.Length >= 3)
                    {
                        ChildFirstName = nameParts[0];
                        ChildLastName = nameParts[nameParts.Length - 1];
                        ChildMiddleName = string.Join(" ", nameParts.Skip(1).Take(nameParts.Length - 2));
                    }
                }

                // Pre-fill child's date of birth
                if (Appointment.DateOfBirth.HasValue)
                {
                    DateOfBirth = Appointment.DateOfBirth.Value.ToString("yyyy-MM-dd");
                }

                // Pre-fill child's sex/gender
                if (!string.IsNullOrEmpty(Appointment.Gender))
                {
                    Sex = Appointment.Gender;
                }

                // Pre-fill address
                if (!string.IsNullOrEmpty(Appointment.Address))
                {
                    Address = Appointment.Address;
                }

                if (Appointment.Patient != null)
                {
                    // TEMPORARY BYPASS: Decrypt patient data only if user is authenticated
                    if (User.Identity?.IsAuthenticated == true)
                    {
                        Appointment.Patient.DecryptSensitiveData(_encryptionService, User);
                    }
                    else
                    {
                        _logger.LogWarning("BYPASS MODE: Skipping patient data decryption due to unauthenticated user");
                    }
                    
                    // Get patient's barangay
                    var patientUser = await _context.Users.FindAsync(Appointment.PatientId);
                    if (patientUser != null)
                    {
                        Barangay = patientUser.Barangay ?? "158"; // Default to 158 if not set
                    }
                    
                    // Pre-populate parent information (split patient name)
                    if (!string.IsNullOrEmpty(Appointment.PatientName))
                    {
                        var parentNameParts = Appointment.PatientName.Trim().Split(' ');
                        // Assuming the booking parent is the mother
                        if (parentNameParts.Length >= 1) MotherFirstName = parentNameParts[0];
                        if (parentNameParts.Length >= 2) MotherMiddleName = string.Join(" ", parentNameParts.Skip(1).Take(parentNameParts.Length - 2));
                        if (parentNameParts.Length >= 2) MotherLastName = parentNameParts[parentNameParts.Length - 1];
                    }
                    
                    // Check if patient already has a family number
                    if (patientUser != null && !string.IsNullOrWhiteSpace(patientUser.FamilyNumber))
                    {
                        FamilyNumber = patientUser.FamilyNumber;
                        _logger.LogInformation("Using existing family number {FamilyNumber} from patient profile", FamilyNumber);
                    }
                    else
                    {
                        _logger.LogInformation("No existing family number found for patient {PatientId}", Appointment.PatientId);
                    }
                    
                    // Use appointment contact number if available, otherwise use patient's
                    if (string.IsNullOrEmpty(ContactNumber))
                    {
                        ContactNumber = !string.IsNullOrEmpty(Appointment.ContactNumber) 
                            ? Appointment.ContactNumber 
                            : Appointment.Patient.ContactNumber;
                    }

                    // Use appointment address if available, otherwise use patient's
                    if (string.IsNullOrEmpty(Address))
                    {
                        Address = !string.IsNullOrEmpty(Appointment.Address) 
                            ? Appointment.Address 
                            : Appointment.Patient.Address;
                    }
                }
                else
                {
                    // No patient record found, use appointment data only
                    if (string.IsNullOrEmpty(ContactNumber) && !string.IsNullOrEmpty(Appointment.ContactNumber))
                    {
                        ContactNumber = Appointment.ContactNumber;
                    }
                    if (string.IsNullOrEmpty(Address) && !string.IsNullOrEmpty(Appointment.Address))
                    {
                        Address = Appointment.Address;
                    }
                }

                _logger.LogInformation("Successfully loaded appointment data for immunization record");
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading immunization record page");
                StatusMessage = "Error loading immunization record page. Please try again.";
                return RedirectToPage("/Nurse/Appointments");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                _logger.LogInformation("Saving immunization record for appointment {AppointmentId}", AppointmentId);

                // Simple - just get user ID
                string userId = User.Identity?.Name ?? "Unknown";
                _logger.LogInformation("User: {UserId}", userId);

                // Construct full child name
                var childFullName = $"{ChildFirstName} {ChildMiddleName} {ChildLastName}".Replace("  ", " ").Trim();
                
                // Construct mother's full name
                var motherFullName = string.Empty;
                if (!string.IsNullOrWhiteSpace(MotherFirstName) || !string.IsNullOrWhiteSpace(MotherLastName))
                {
                    motherFullName = $"{MotherFirstName} {MotherMiddleName} {MotherLastName}".Replace("  ", " ").Trim();
                }
                
                // Construct father's full name
                var fatherFullName = string.Empty;
                if (!string.IsNullOrWhiteSpace(FatherFirstName) || !string.IsNullOrWhiteSpace(FatherLastName))
                {
                    fatherFullName = $"{FatherFirstName} {FatherMiddleName} {FatherLastName}".Replace("  ", " ").Trim();
                }

                // Create immunization record
                var immunizationRecord = new ImmunizationRecord
                {
                    ChildName = childFullName,
                    DateOfBirth = DateOfBirth,
                    PlaceOfBirth = PlaceOfBirth ?? string.Empty,
                    Address = Address ?? string.Empty,
                    MotherName = motherFullName,
                    FatherName = fatherFullName,
                    Sex = Sex,
                    BirthHeight = BirthHeight ?? string.Empty,
                    BirthWeight = BirthWeight ?? string.Empty,
                    HealthCenter = HealthCenter ?? "Barangay Health Care Center",
                    Barangay = Barangay,
                    FamilyNumber = FamilyNumber ?? string.Empty,
                    Email = string.Empty, // Not collected in this form
                    ContactNumber = ContactNumber ?? string.Empty,
                    
                    // Vaccine dates and remarks
                    BCGVaccineDate = BCGVaccineDate,
                    BCGVaccineRemarks = BCGVaccineRemarks,
                    HepatitisBVaccineDate = HepBBirthDate,
                    HepatitisBVaccineRemarks = HepBBirthRemarks,
                    Pentavalent1Date = Pentavalent1Date,
                    Pentavalent1Remarks = Pentavalent1Remarks,
                    Pentavalent2Date = Pentavalent2Date,
                    Pentavalent2Remarks = Pentavalent2Remarks,
                    Pentavalent3Date = Pentavalent3Date,
                    Pentavalent3Remarks = Pentavalent3Remarks,
                    OPV1Date = OPV1Date,
                    OPV1Remarks = OPV1Remarks,
                    OPV2Date = OPV2Date,
                    OPV2Remarks = OPV2Remarks,
                    OPV3Date = OPV3Date,
                    OPV3Remarks = OPV3Remarks,
                    IPV1Date = IPV1Date,
                    IPV1Remarks = IPV1Remarks,
                    IPV2Date = IPV2Date,
                    IPV2Remarks = IPV2Remarks,
                    PCV1Date = PCV1Date,
                    PCV1Remarks = PCV1Remarks,
                    PCV2Date = PCV2Date,
                    PCV2Remarks = PCV2Remarks,
                    PCV3Date = PCV3Date,
                    PCV3Remarks = PCV3Remarks,
                    MMR1Date = MMR1Date,
                    MMR1Remarks = MMR1Remarks,
                    MMR2Date = MMR2Date,
                    MMR2Remarks = MMR2Remarks,
                    
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    CreatedBy = userId, // Use fallback ID if user is null
                    UpdatedBy = userId, // Use fallback ID if user is null
                    Status = "Active"
                };

                // Encrypt sensitive data
                immunizationRecord.EncryptSensitiveData(_encryptionService);

                // Save to database
                _context.ImmunizationRecords.Add(immunizationRecord);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Immunization record saved successfully with ID {RecordId}", immunizationRecord.Id);

                // Save family number to patient profile if they don't have one
                if (!string.IsNullOrWhiteSpace(FamilyNumber))
                {
                    var appointment = await _context.Appointments.FindAsync(AppointmentId);
                    
                    if (appointment != null)
                    {
                        var patientUser = await _context.Users.FindAsync(appointment.PatientId);
                        
                        if (patientUser != null && string.IsNullOrWhiteSpace(patientUser.FamilyNumber))
                        {
                            patientUser.FamilyNumber = FamilyNumber;
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Family number {FamilyNumber} saved to patient {PatientId} profile", FamilyNumber, appointment.PatientId);
                        }
                    }
                }

                // Update appointment status to Completed
                var completedAppointment = await _context.Appointments.FindAsync(AppointmentId);
                if (completedAppointment != null)
                {
                    completedAppointment.Status = AppointmentStatus.Completed;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Appointment {AppointmentId} marked as completed", AppointmentId);
                }

                TempData["StatusMessage"] = "Immunization record saved successfully!";
                
                // Redirect to ImmunizationRecords page to show the saved record
                _logger.LogInformation("Redirecting to ImmunizationRecords page");
                return RedirectToPage("/Nurse/ImmunizationRecords");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving immunization record");
                TempData["StatusMessage"] = $"Error: {ex.Message}";
                return RedirectToPage("/Nurse/ImmunizationRecords");
            }
        }
        
        // Handler for generating family numbers
        public async Task<JsonResult> OnPostGenerateFamilyNumberAsync([FromBody] GenerateFamilyNumberRequest request)
        {
            try
            {
                _logger.LogInformation("=== GENERATE FAMILY NUMBER REQUEST (Immunization) ===");
                _logger.LogInformation("LastName: {LastName}", request.LastName);
                
                if (string.IsNullOrWhiteSpace(request.LastName))
                {
                    return new JsonResult(new { success = false, error = "Last name is required" });
                }
                
                // Generate family number using the service
                var response = await _familyNumberService.GenerateFamilyNumberAsync(request.LastName);
                
                if (!response.Success)
                {
                    _logger.LogError("Failed to generate family number: {Error}", response.Error);
                    return new JsonResult(new { success = false, error = response.Error });
                }
                
                _logger.LogInformation("Family number {FamilyNumber} generated for immunization record", response.FamilyNumber);
                
                return new JsonResult(new { success = true, familyNumber = response.FamilyNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating family number");
                return new JsonResult(new { success = false, error = "An error occurred while generating the family number" });
            }
        }
    }
}
