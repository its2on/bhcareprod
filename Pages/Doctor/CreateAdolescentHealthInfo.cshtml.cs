using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using Barangay.Extensions;
using System.Security.Claims;

namespace Barangay.Pages.Doctor
{
    [Authorize(Roles = "Doctor,Medical Officer")]
    public class CreateAdolescentHealthInfoModel : PageModel
    {
        private readonly EncryptedDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CreateAdolescentHealthInfoModel> _logger;
        private readonly IDataEncryptionService _encryptionService;

        public CreateAdolescentHealthInfoModel(
            EncryptedDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<CreateAdolescentHealthInfoModel> logger,
            IDataEncryptionService encryptionService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _encryptionService = encryptionService;
        }

        [BindProperty]
        public AdolescentHealthInfo HealthInfo { get; set; } = new AdolescentHealthInfo();

        public string? PatientName { get; set; }
        public string? PatientAddress { get; set; }
        public string? PatientGender { get; set; }
        public string? PatientPhone { get; set; }
        public int PatientAge { get; set; }
        public int? AppointmentId { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? appointmentId)
        {
            try
            {
                if (appointmentId == null)
                {
                    TempData["ErrorMessage"] = "Appointment ID is required.";
                    return RedirectToPage("/Doctor/Appointments");
                }

                AppointmentId = appointmentId;

                // Get appointment details
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);

                if (appointment?.Patient == null)
                {
                    TempData["ErrorMessage"] = "Appointment or patient not found.";
                    return RedirectToPage("/Doctor/Appointments");
                }

                // Decrypt patient data
                appointment.Patient.DecryptSensitiveData(_encryptionService, User);

                PatientName = appointment.Patient.FullName;
                PatientAddress = appointment.Patient.Address;
                PatientGender = appointment.Patient.Gender;
                PatientPhone = appointment.Patient.ContactNumber;
                
                // Calculate age
                PatientAge = DateTime.Today.Year - appointment.Patient.BirthDate.Year;
                if (appointment.Patient.BirthDate.Date > DateTime.Today.AddYears(-PatientAge))
                    PatientAge--;
                else
                {
                    PatientAge = 15; // Default adolescent age
                }

                // Initialize HealthInfo with patient data
                HealthInfo.UserId = appointment.Patient.UserId;
                HealthInfo.AppointmentId = appointmentId.ToString();
                HealthInfo.PatientName = PatientName;
                HealthInfo.PatientAge = PatientAge.ToString();
                HealthInfo.PatientGender = PatientGender;
                HealthInfo.PatientAddress = PatientAddress;
                HealthInfo.PatientContact = PatientPhone;
                HealthInfo.RecordedBy = User.Identity?.Name ?? "Doctor";

                // Check if health info already exists
                AdolescentHealthInfo? existingHealthInfo = null;
                try
                {
                    existingHealthInfo = await _context.AdolescentHealthInfo
                        .Where(a => a.UserId == appointment.Patient.UserId)
                        .OrderByDescending(a => a.CreatedAt)
                        .FirstOrDefaultAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AdolescentHealthInfo table may not exist. Will create new record.");
                }

                if (existingHealthInfo != null)
                {
                    StatusMessage = "Health information already exists for this patient. You can view it in the consultation.";
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient information for Adolescent Health Info");
                TempData["ErrorMessage"] = "An error occurred while retrieving patient information.";
                return RedirectToPage("/Doctor/Appointments");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Page();
                }

                HealthInfo.CreatedAt = DateTime.UtcNow;
                HealthInfo.UpdatedAt = DateTime.UtcNow;
                HealthInfo.RecordedBy = User.Identity?.Name ?? "Doctor";

                // Encrypt sensitive data
                HealthInfo.EncryptSensitiveData(_encryptionService);

                try
                {
                    _context.AdolescentHealthInfo.Add(HealthInfo);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Adolescent Health Information recorded successfully.";
                    return RedirectToPage("/Doctor/Consultation", new { appointmentId = HealthInfo.AppointmentId });
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Failed to save AdolescentHealthInfo. Table may not exist in database.");
                    TempData["ErrorMessage"] = "Unable to save Adolescent Health Information. The database table may not exist. Please contact your system administrator.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Adolescent Health Information");
                TempData["ErrorMessage"] = "An error occurred while saving the information.";
                return Page();
            }
        }

        
        public string CalculateBMI(decimal weight, decimal height)
        {
            if (weight > 0 && height > 0)
            {
                var heightInMeters = height / 100;
                var bmi = weight / (heightInMeters * heightInMeters);
                return Math.Round(bmi, 2).ToString();
            }
            return string.Empty;
        }

        
        public string GetBMICategory(decimal bmi)
        {
            if (bmi < 18.5m) return "Underweight";
            if (bmi >= 18.5m && bmi < 25m) return "Normal";
            if (bmi >= 25m && bmi < 30m) return "Overweight";
            return "Obese";
        }
    }
}
