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

namespace Barangay.Pages.Nurse
{
    [Authorize(Roles = "Nurse,Head Nurse")]
    [Authorize(Policy = "PatientList")]
    public class ImmunizationRecordModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ImmunizationRecordModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDataEncryptionService _encryptionService;

        public ImmunizationRecordModel(
            ApplicationDbContext context, 
            ILogger<ImmunizationRecordModel> logger,
            UserManager<ApplicationUser> userManager,
            IDataEncryptionService encryptionService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _encryptionService = encryptionService;
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
        [Phone(ErrorMessage = "Please enter a valid phone number")]
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

                // Decrypt appointment data
                if (!string.IsNullOrEmpty(Appointment.PatientName) && _encryptionService.IsEncrypted(Appointment.PatientName))
                {
                    Appointment.PatientName = _encryptionService.DecryptForUser(Appointment.PatientName, User);
                }
                
                if (!string.IsNullOrEmpty(Appointment.DependentFullName) && _encryptionService.IsEncrypted(Appointment.DependentFullName))
                {
                    Appointment.DependentFullName = _encryptionService.DecryptForUser(Appointment.DependentFullName, User);
                }

                // Pre-populate form with appointment data
                if (!string.IsNullOrEmpty(Appointment.DependentFullName))
                {
                    // Split dependent name (assuming format: FirstName MiddleName LastName)
                    var nameParts = Appointment.DependentFullName.Trim().Split(' ');
                    if (nameParts.Length >= 1) ChildFirstName = nameParts[0];
                    if (nameParts.Length >= 2) ChildMiddleName = string.Join(" ", nameParts.Skip(1).Take(nameParts.Length - 2));
                    if (nameParts.Length >= 2) ChildLastName = nameParts[nameParts.Length - 1];
                }

                if (Appointment.Patient != null)
                {
                    // Decrypt patient data
                    Appointment.Patient.DecryptSensitiveData(_encryptionService, User);
                    
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
                        
                        // Generate family number based on first letter of last name
                        if (!string.IsNullOrEmpty(MotherLastName))
                        {
                            var firstLetter = MotherLastName.ToUpper().Substring(0, 1);
                            var existingFamilyCount = await _context.ImmunizationRecords
                                .Where(r => !string.IsNullOrEmpty(r.FamilyNumber) && r.FamilyNumber.StartsWith(firstLetter))
                                .CountAsync();
                            FamilyNumber = $"{firstLetter}-{(existingFamilyCount + 1):D3}";
                        }
                    }
                    
                    ContactNumber = Appointment.Patient.ContactNumber;
                    Address = Appointment.Patient.Address;
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
                if (!ModelState.IsValid)
                {
                    await OnGetAsync(); // Reload appointment data
                    return Page();
                }

                _logger.LogInformation("Saving immunization record for appointment {AppointmentId}", AppointmentId);

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    StatusMessage = "Error: User not found.";
                    return RedirectToPage("/Account/Login");
                }

                // Combine name fields
                var childFullName = $"{ChildFirstName?.Trim()} {ChildMiddleName?.Trim()} {ChildLastName?.Trim()}".Trim();
                var motherFullName = $"{MotherFirstName?.Trim()} {MotherMiddleName?.Trim()} {MotherLastName?.Trim()}".Trim();
                var fatherFullName = $"{FatherFirstName?.Trim()} {FatherMiddleName?.Trim()} {FatherLastName?.Trim()}".Trim();

                // Create immunization record
                var immunizationRecord = new ImmunizationRecord
                {
                    ChildName = _encryptionService.Encrypt(childFullName),
                    DateOfBirth = _encryptionService.Encrypt(DateOfBirth ?? ""),
                    PlaceOfBirth = _encryptionService.Encrypt(PlaceOfBirth ?? ""),
                    Sex = _encryptionService.Encrypt(Sex ?? ""),
                    BirthWeight = _encryptionService.Encrypt(BirthWeight ?? ""),
                    BirthHeight = _encryptionService.Encrypt(BirthHeight ?? ""),
                    Address = _encryptionService.Encrypt(Address ?? ""),
                    MotherName = _encryptionService.Encrypt(motherFullName),
                    FatherName = _encryptionService.Encrypt(fatherFullName),
                    ContactNumber = _encryptionService.Encrypt(ContactNumber ?? ""),
                    HealthCenter = _encryptionService.Encrypt(HealthCenter ?? ""),
                    Barangay = _encryptionService.Encrypt(Barangay ?? ""),
                    FamilyNumber = _encryptionService.Encrypt(FamilyNumber ?? ""),
                    
                    // Vaccine records - Complete list
                    BCGVaccineDate = !string.IsNullOrEmpty(BCGVaccineDate) ? _encryptionService.Encrypt(BCGVaccineDate) : null,
                    BCGVaccineRemarks = !string.IsNullOrEmpty(BCGVaccineRemarks) ? _encryptionService.Encrypt(BCGVaccineRemarks) : null,
                    
                    // Store Hepatitis B vaccines in existing fields for now
                    HepatitisBVaccineDate = !string.IsNullOrEmpty(HepBBirthDate) ? _encryptionService.Encrypt(HepBBirthDate) : null,
                    HepatitisBVaccineRemarks = !string.IsNullOrEmpty(HepBBirthRemarks) ? _encryptionService.Encrypt(HepBBirthRemarks) : null,
                    
                    Pentavalent1Date = !string.IsNullOrEmpty(Pentavalent1Date) ? _encryptionService.Encrypt(Pentavalent1Date) : null,
                    Pentavalent1Remarks = !string.IsNullOrEmpty(Pentavalent1Remarks) ? _encryptionService.Encrypt(Pentavalent1Remarks) : null,
                    Pentavalent2Date = !string.IsNullOrEmpty(Pentavalent2Date) ? _encryptionService.Encrypt(Pentavalent2Date) : null,
                    Pentavalent2Remarks = !string.IsNullOrEmpty(Pentavalent2Remarks) ? _encryptionService.Encrypt(Pentavalent2Remarks) : null,
                    Pentavalent3Date = !string.IsNullOrEmpty(Pentavalent3Date) ? _encryptionService.Encrypt(Pentavalent3Date) : null,
                    Pentavalent3Remarks = !string.IsNullOrEmpty(Pentavalent3Remarks) ? _encryptionService.Encrypt(Pentavalent3Remarks) : null,
                    
                    CreatedBy = _encryptionService.Encrypt(currentUser.Id),
                    CreatedAt = _encryptionService.Encrypt(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")),
                    UpdatedAt = _encryptionService.Encrypt(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")),
                    Status = _encryptionService.Encrypt("Active")
                };

                _context.ImmunizationRecords.Add(immunizationRecord);
                await _context.SaveChangesAsync();

                // Update appointment status to completed
                var appointment = await _context.Appointments.FindAsync(AppointmentId);
                if (appointment != null)
                {
                    appointment.Status = AppointmentStatus.Completed;
                    appointment.UpdatedAt = DateTime.UtcNow;
                    _context.Appointments.Update(appointment);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Successfully saved immunization record with ID {RecordId}", immunizationRecord.Id);
                StatusMessage = "Immunization record saved successfully!";
                
                return RedirectToPage("/Nurse/Appointments");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving immunization record");
                StatusMessage = "Error saving immunization record. Please try again.";
                
                await OnGetAsync(); // Reload appointment data
                return Page();
            }
        }
    }
}
