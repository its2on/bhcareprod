using System;
using System.Threading.Tasks;
using System.Linq;
using Barangay.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Barangay.Data;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Barangay.Services;
using Barangay.Extensions;

namespace Barangay.Pages.Nurse
{
    [Authorize(Roles = "Nurse,Head Nurse")]
    public class CreateNCDAssessmentModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateNCDAssessmentModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDataEncryptionService _encryptionService;

        public CreateNCDAssessmentModel(
            ApplicationDbContext context,
            ILogger<CreateNCDAssessmentModel> logger,
            UserManager<ApplicationUser> userManager,
            IDataEncryptionService encryptionService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _encryptionService = encryptionService;
            Assessment = new NCDRiskAssessment();
        }

        [BindProperty]
        public NCDRiskAssessment Assessment { get; set; }
        
        public string PatientName { get; set; }
        public string PatientAddress { get; set; }
        public string PatientBarangay { get; set; }
        public string PatientPhone { get; set; }
        public int PatientAge { get; set; }
        public string HealthFacility { get; set; } = "Barangay Health Center 161";
        public string FamilyNo { get; set; } = "C-001";

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? appointmentId)
        {
            try
            {
                if (appointmentId == null)
                {
                    _logger.LogWarning("Appointment ID not provided");
                    return NotFound("Appointment ID must be provided");
                }

                _logger.LogInformation("Loading data for appointment: {AppointmentId}", appointmentId);
                
                // Find the appointment
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);

                if (appointment == null)
                {
                    _logger.LogWarning("Appointment with ID {Id} not found", appointmentId);
                    return NotFound("Appointment not found");
                }

                // Check if assessment already exists
                var existingAssessment = await _context.NCDRiskAssessments
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

                if (existingAssessment != null)
                {
                    _logger.LogWarning("Assessment already exists for appointment ID {AppointmentId}", appointmentId);
                    StatusMessage = "An assessment already exists for this appointment.";
                    return RedirectToPage("/Nurse/AppointmentDetails", new { id = appointmentId });
                }

                // Initialize assessment with appointment data
                Assessment.AppointmentId = appointmentId.Value;
                
                if (appointment.Patient != null)
                {
                    PatientName = appointment.Patient.FullName;
                    Assessment.UserId = appointment.Patient.UserId;
                    Assessment.Birthday = appointment.Patient.BirthDate != DateTime.MinValue ? appointment.Patient.BirthDate.ToString("yyyy-MM-dd") : null;
                    PatientAddress = appointment.Patient.Address;
                    PatientPhone = appointment.Patient.ContactNumber;
                    
                    // Extract barangay from address if possible
                    if (!string.IsNullOrEmpty(appointment.Patient.Address))
                    {
                        var addressParts = appointment.Patient.Address.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        if (addressParts.Length > 1)
                        {
                            PatientBarangay = addressParts[1].Trim();
                        }
                    }
                    
                    // Calculate age
                    PatientAge = appointment.Patient.Age;
                    Assessment.Edad = PatientAge.ToString();

                    // Get family number from associated user if available
                    if (appointment.Patient.User != null)
                    {
                        FamilyNo = await GetOrGenerateFamilyNumber(appointment.Patient.User);
                    }
                }
                else
                {
                    PatientName = appointment.PatientName ?? "Unknown";
                    Assessment.UserId = appointment.PatientId;
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading appointment data for ID: {Id}", appointmentId);
                StatusMessage = "Error loading appointment data. Please try again later.";
                return RedirectToPage("/Nurse/Appointments");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state in CreateNCDAssessment OnPost");
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    _logger.LogWarning("Validation errors: {Errors}", errors);
                    return Page();
                }

                // Set timestamps
                Assessment.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                Assessment.UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Get appointment
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.Id == Assessment.AppointmentId);

                if (appointment == null)
                {
                    _logger.LogWarning("Appointment not found when saving assessment");
                    StatusMessage = "Appointment not found.";
                    return RedirectToPage("/Nurse/Appointments");
                }

                // Make sure cancer type is only set when cancer is checked
                if (Assessment.HasCancer != "true")
                {
                    Assessment.CancerType = null;
                }

                // Encrypt sensitive data before saving
                Assessment.EncryptSensitiveData(_encryptionService);

                // Save assessment
                _context.NCDRiskAssessments.Add(Assessment);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Successfully saved NCD Risk Assessment for appointment ID: {Id}", Assessment.AppointmentId);
                StatusMessage = "NCD Risk Assessment saved successfully.";
                
                return RedirectToPage("/Nurse/AppointmentDetails", new { id = Assessment.AppointmentId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving NCD Risk Assessment");
                StatusMessage = "Error saving assessment. Please try again later.";
                return Page();
            }
        }

        private int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            
            if (birthDate.Date > today.AddYears(-age))
            {
                age--;
            }
            
            return age;
        }

        private async Task<string> GetOrGenerateFamilyNumber(ApplicationUser user)
        {
            // Check if user already has a family number in previous assessments
            var existingAssessment = await _context.NCDRiskAssessments
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (existingAssessment != null && !string.IsNullOrEmpty(existingAssessment.FamilyNo))
            {
                return existingAssessment.FamilyNo;
            }

            // Generate a new family number based on last name initial
            var lastName = user.LastName ?? user.FullName?.Split(' ').LastOrDefault() ?? "X";
            var firstLetter = lastName.Substring(0, 1).ToUpper();

            // Get the highest sequence number for this letter
            var lastNumber = await _context.NCDRiskAssessments
                .Where(a => a.FamilyNo != null && a.FamilyNo.StartsWith(firstLetter + "-"))
                .Select(a => a.FamilyNo.Substring(2))
                .Where(n => n.All(char.IsDigit))
                .Select(n => int.Parse(n))
                .DefaultIfEmpty(0)
                .MaxAsync();
            
            // Generate new family number
            var newSequence = lastNumber + 1;
            return $"{firstLetter}-{newSequence:D3}"; // Format: X-001, X-002, etc.
        }
    }
} 