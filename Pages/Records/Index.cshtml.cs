using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Barangay.Models;
using Barangay.Data;
using Barangay.Services;
using Barangay.Extensions;

namespace Barangay.Pages.Records
{
    [Authorize(Roles = "Admin,Doctor,Nurse,User")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EncryptedDbContext _context;
        private readonly ILogger<IndexModel> _logger;
        private readonly IDataEncryptionService _encryptionService;
        private readonly IAuditTrailService _auditTrail;

        public IndexModel(UserManager<ApplicationUser> userManager, EncryptedDbContext context, ILogger<IndexModel> logger, IDataEncryptionService encryptionService, IAuditTrailService auditTrail)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
            _encryptionService = encryptionService;
            _auditTrail = auditTrail;
        }

        public string BloodType { get; set; } = "O+";
        public double Height { get; set; } = 170;
        public double Weight { get; set; } = 70;
        public double BMI { get; set; } = 24.2;
        public DateTime LastCheckup { get; set; } = DateTime.Now.AddDays(-30);
        public bool IsNurse { get; set; }
        public bool IsDoctor { get; set; }
        public bool IsPatient { get; set; }
        public string CurrentUserId { get; set; }
        public string PatientName { get; set; }
        public List<Patient> Patients { get; set; } = new List<Patient>();

        public class VitalSign
        {
            public int Id { get; set; }
            public string PatientId { get; set; }
            public string PatientName { get; set; }
            public DateTime Date { get; set; }
            public string BloodPressure { get; set; }
            public int HeartRate { get; set; }
            public double Temperature { get; set; }
            public int RespiratoryRate { get; set; }
            public double? Weight { get; set; }
            public double? Height { get; set; }
        }

        public class MedicalHistoryRecord
        {
            public int Id { get; set; }
            public string PatientId { get; set; }
            public string PatientName { get; set; }
            public DateTime Date { get; set; }
            public string Doctor { get; set; }
            public string Diagnosis { get; set; }
            public string Treatment { get; set; }
        }

        public class LabResult
        {
            public int Id { get; set; }
            public string PatientId { get; set; }
            public string PatientName { get; set; }
            public DateTime Date { get; set; }
            public string TestName { get; set; }
            public string Result { get; set; }
            public string ReferenceRange { get; set; }
            public string Status { get; set; }
        }

        public List<VitalSign> VitalSigns { get; set; } = new List<VitalSign>();
        public List<MedicalHistoryRecord> MedicalHistory { get; set; } = new List<MedicalHistoryRecord>();
        public List<LabResult> LabResults { get; set; } = new List<LabResult>();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                // Determine user role
                IsNurse = await _userManager.IsInRoleAsync(user, "Nurse");
                IsDoctor = await _userManager.IsInRoleAsync(user, "Doctor");
                IsPatient = await _userManager.IsInRoleAsync(user, "Patient");
                CurrentUserId = user.Id;

                // If the user is a Patient, only show their own records
                if (IsPatient)
                {
                    // Get patient information
                    var patient = await _context.Patients
                        .FirstOrDefaultAsync(p => p.UserId == CurrentUserId);

                    if (patient == null)
                    {
                        _logger.LogWarning($"Patient record not found for user ID {CurrentUserId}");
                        return Page();
                    }

                    PatientName = patient.FullName;

                    // Get vital signs for this patient
                    await LoadPatientVitalSigns(patient.UserId);

                    // Get medical history for this patient
                    await LoadPatientMedicalHistory(patient.UserId);

                    // Get lab results for this patient
                    LoadPatientLabResults(patient.UserId);
                }
                // If the user is a Nurse or Doctor, show all patient records
                else if (IsNurse || IsDoctor)
                {
                    // Get all patients for dropdown
                    Patients = await _context.Patients
                        .OrderBy(p => p.FullName)
                        .ToListAsync();

                    // Load all vital signs
                    await LoadAllVitalSigns();

                    // Load all medical history
                    await LoadAllMedicalHistory();

                    // Load all lab results
                    LoadAllLabResults();
                }
                else
                {
                    // Redirect users without proper roles
                    return RedirectToPage("/Account/Login");
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading medical records");
                return Page();
            }
        }

        private async Task LoadPatientVitalSigns(string patientId)
        {
            var vitalSigns = await _context.VitalSigns
                .Where(v => v.PatientId == patientId)
                .OrderByDescending(v => v.RecordedAt)
                .ToListAsync();

            // Manually decrypt vital signs data
            foreach (var vitalSign in vitalSigns)
            {
                vitalSign.DecryptVitalSignData(_encryptionService, User);
            }

            VitalSigns = vitalSigns.Select(v => new VitalSign
            {
                Id = v.Id,
                PatientId = v.PatientId,
                PatientName = PatientName,
                Date = v.RecordedAt,
                BloodPressure = v.BloodPressure,
                HeartRate = !string.IsNullOrEmpty(v.HeartRate) && int.TryParse(v.HeartRate, out var hr) ? hr : 0,
                Temperature = !string.IsNullOrEmpty(v.Temperature) && double.TryParse(v.Temperature, out var temp) ? temp : 0,
                RespiratoryRate = !string.IsNullOrEmpty(v.RespiratoryRate) && int.TryParse(v.RespiratoryRate, out var rr) ? rr : 0,
                Weight = !string.IsNullOrEmpty(v.Weight) && double.TryParse(v.Weight, out var weight) ? weight : null,
                Height = !string.IsNullOrEmpty(v.Height) && double.TryParse(v.Height, out var height) ? height : null
            }).ToList();
        }

        private async Task LoadAllVitalSigns()
        {
            var vitalSigns = await _context.VitalSigns
                .Include(v => v.Patient)
                .OrderByDescending(v => v.RecordedAt)
                .ToListAsync();

            // Manually decrypt vital signs data
            foreach (var vitalSign in vitalSigns)
            {
                vitalSign.DecryptVitalSignData(_encryptionService, User);
            }

            VitalSigns = vitalSigns.Select(v => new VitalSign
            {
                Id = v.Id,
                PatientId = v.PatientId,
                PatientName = v.Patient?.FullName ?? "Unknown",
                Date = v.RecordedAt,
                BloodPressure = v.BloodPressure,
                HeartRate = !string.IsNullOrEmpty(v.HeartRate) && int.TryParse(v.HeartRate, out var hr) ? hr : 0,
                Temperature = !string.IsNullOrEmpty(v.Temperature) && double.TryParse(v.Temperature, out var temp) ? temp : 0,
                RespiratoryRate = !string.IsNullOrEmpty(v.RespiratoryRate) && int.TryParse(v.RespiratoryRate, out var rr) ? rr : 0,
                Weight = !string.IsNullOrEmpty(v.Weight) && double.TryParse(v.Weight, out var weight) ? weight : null,
                Height = !string.IsNullOrEmpty(v.Height) && double.TryParse(v.Height, out var height) ? height : null
            }).ToList();
        }

        private async Task LoadPatientMedicalHistory(string patientId)
        {
            var medicalRecords = await _context.MedicalRecords
                .Where(m => m.PatientId == patientId)
                .Include(m => m.Doctor)
                .OrderByDescending(m => m.Date)
                .ToListAsync();

            MedicalHistory = medicalRecords.Select(m => new MedicalHistoryRecord
            {
                Id = m.Id,
                PatientId = m.PatientId,
                PatientName = PatientName,
                Date = m.Date,
                Doctor = m.Doctor?.FullName ?? "Unknown",
                Diagnosis = m.Diagnosis,
                Treatment = m.Treatment
            }).ToList();
        }

        private async Task LoadAllMedicalHistory()
        {
            var medicalRecords = await _context.MedicalRecords
                .Include(m => m.Patient)
                .Include(m => m.Doctor)
                .OrderByDescending(m => m.Date)
                .ToListAsync();

            MedicalHistory = medicalRecords.Select(m => new MedicalHistoryRecord
            {
                Id = m.Id,
                PatientId = m.PatientId,
                PatientName = m.Patient?.FullName ?? "Unknown",
                Date = m.Date,
                Doctor = m.Doctor?.FullName ?? "Unknown",
                Diagnosis = m.Diagnosis,
                Treatment = m.Treatment
            }).ToList();
        }

        private void LoadPatientLabResults(string patientId)
        {
            // Return empty list instead of sample data
            LabResults = new List<LabResult>();
            
            // Log that we're not generating sample data anymore
            _logger.LogInformation("Lab results are now empty by default - no sample data generated");
        }

        private void LoadAllLabResults()
        {
            // Return empty list instead of sample data for all patients
            LabResults = new List<LabResult>();
            
            // Log that we're not generating sample data anymore
            _logger.LogInformation("Lab results are now empty by default - no sample data generated");
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var record = await _context.MedicalRecords.FindAsync(id);
                if (record == null)
                {
                    return NotFound();
                }

                _context.MedicalRecords.Remove(record);
                await _context.SaveChangesAsync();
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting medical record");
                return RedirectToPage("./Error");
            }
        }

        public async Task<IActionResult> OnPostExportAsync()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var records = await _context.MedicalRecords
                    .Include(r => r.Patient)
                    .Include(r => r.Doctor)
                    .ToListAsync();

                // AUDIT: Log medical records export
                try
                {
                    await _auditTrail.LogAsync(
                        "Export",
                        "Exported medical records",
                        "MedicalRecords",
                        "bulk",
                        null,
                        Newtonsoft.Json.JsonConvert.SerializeObject(new { 
                            RecordCount = records.Count,
                            ExportFormat = "Excel",
                            ExportedBy = user?.Email
                        }),
                        $"User {user?.Email} exported {records.Count} medical records"
                    );
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to log medical records export audit trail");
                }

                // Export logic here
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting medical records");
                return RedirectToPage("./Error");
            }
        }
    }
} 