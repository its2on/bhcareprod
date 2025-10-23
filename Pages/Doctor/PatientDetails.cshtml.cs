using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using Barangay.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Barangay.Pages.Doctor
{
    [Authorize(Roles = "Doctor")]
    public class PatientDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PatientDetailsModel> _logger;
        private readonly IDataEncryptionService _encryptionService;
        private readonly IAuditTrailService _auditTrail;

        public PatientDetailsModel(ApplicationDbContext context, ILogger<PatientDetailsModel> logger, IDataEncryptionService encryptionService, IAuditTrailService auditTrail)
        {
            _context = context;
            _logger = logger;
            _encryptionService = encryptionService;
            _auditTrail = auditTrail;
        }

        public Patient? Patient { get; set; }
        public ApplicationUser? CurrentUser { get; set; }
        public List<MedicalRecordViewModel> MedicalRecords { get; set; } = new();
        public List<PrescriptionMedicationViewModel> Medications { get; set; } = new();
        public GuardianInformation? Guardian { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Patient ID is required");
                TempData["ErrorMessage"] = "Patient ID is required.";
                return RedirectToPage("/Doctor/PatientRecords");
            }

            _logger.LogInformation("Attempting to load patient details with ID: {PatientId}", id);

            var doctorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(doctorId))
            {
                _logger.LogWarning("Doctor ID not found");
                return Unauthorized();
            }

            // Load current user (doctor) information
            CurrentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == doctorId);
            if (CurrentUser != null)
            {
                CurrentUser.DecryptSensitiveData(_encryptionService, User);
            }

            try
            {
                // Get full patient details with related data
                Patient = await _context.Patients
                    .Include(p => p.User)
                    .Include(p => p.VitalSigns)
                    .Include(p => p.Appointments)
                        .ThenInclude(a => a.Doctor)
                    .FirstOrDefaultAsync(p => p.UserId == id);

                if (Patient == null)
                {
                    _logger.LogWarning("Patient not found with ID: {PatientId}", id);
                    TempData["ErrorMessage"] = "Patient not found.";
                    return Page();
                }

                // Decrypt patient data for display
                Patient.DecryptSensitiveData(_encryptionService, User);
                
                // Decrypt user data if available
                if (Patient.User != null)
                {
                    Patient.User.DecryptSensitiveData(_encryptionService, User);
                }

                // Load Guardian Information separately
                Guardian = await _context.GuardianInformation
                    .FirstOrDefaultAsync(g => g.UserId == id);
                
                // Decrypt guardian data if available
                if (Guardian != null)
                {
                    Guardian.DecryptSensitiveData(_encryptionService, User);
                    _logger.LogInformation("Found guardian information for patient: {PatientId}", id);
                }
                else
                {
                    _logger.LogInformation("No guardian information found for patient: {PatientId}", id);
                }

                _logger.LogInformation("Successfully loaded patient: {PatientName} with ID: {PatientId}", 
                    Patient.FullName, id);

                // Try to load medical records if available
                try
                {
                    // Get all medical records for this patient
                    var medicalRecords = await _context.MedicalRecords
                        .Include(r => r.Doctor)
                        .Where(r => r.PatientId == id)
                        .OrderByDescending(r => r.Date)
                        .ToListAsync();

                    // Decrypt medical records before creating view model
                    foreach (var record in medicalRecords)
                    {
                        record.DecryptSensitiveData(_encryptionService, User);
                    }

                    MedicalRecords = medicalRecords.Select(r => new MedicalRecordViewModel
                    {
                        Id = r.Id,
                        Date = r.Date,
                        Type = r.Type ?? "Consultation",
                        ChiefComplaint = r.ChiefComplaint,
                        Diagnosis = r.Diagnosis,
                        Treatment = r.Treatment,
                        Notes = r.Notes,
                        Status = r.Status,
                        DoctorName = r.Doctor?.FullName ?? "Unknown"
                    }).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error loading medical records for patient {PatientId}", id);
                    // Continue even if we can't load records
                }

                // Try to load medications/prescriptions if available 
                try
                {
                    // Get all medications/prescriptions for this patient
                    var medications = await _context.PrescriptionMedications
                        .Include(m => m.Medication)
                        .Include(m => m.MedicalRecord)
                            .ThenInclude(mr => mr.Doctor)
                        .Where(m => m.MedicalRecord.PatientId == id)
                        .OrderByDescending(m => m.MedicalRecord.Date)
                        .ToListAsync();

                    Medications = medications.Select(m => new PrescriptionMedicationViewModel
                    {
                        Id = m.Id,
                        MedicationName = m.Medication?.Name ?? "Unknown",
                        Dosage = m.Dosage,
                        Instructions = m.Instructions,
                        CreatedAt = m.MedicalRecord?.Date,
                        DoctorName = m.MedicalRecord?.Doctor?.FullName ?? "Unknown"
                    }).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error loading medications for patient {PatientId}", id);
                    // Continue even if we can't load medications
                }

                // AUDIT: Log PHI access - CRITICAL for HIPAA compliance
                await _auditTrail.LogAsync(
                    "View",
                    $"Viewed patient medical records",
                    "Patient",
                    id,
                    null,
                    JsonConvert.SerializeObject(new {
                        PatientName = Patient.FullName,
                        MedicalRecordsCount = MedicalRecords.Count,
                        MedicationsCount = Medications.Count,
                        HasGuardian = Guardian != null
                    }),
                    $"Doctor accessed confidential medical information for patient {Patient.User?.Email}"
                );

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading patient details for {PatientId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading patient details.";
                return Page();
            }
        }

        public class MedicalRecordViewModel
        {
            public int Id { get; set; }
            public DateTime Date { get; set; }
            public string Type { get; set; } = string.Empty;
            public string? ChiefComplaint { get; set; }
            public string? Diagnosis { get; set; }
            public string? Treatment { get; set; }
            public string? Notes { get; set; }
            public string? Status { get; set; }
            public string DoctorName { get; set; } = string.Empty;
        }

        public class PrescriptionMedicationViewModel
        {
            public int Id { get; set; }
            public string MedicationName { get; set; } = string.Empty;
            public string Dosage { get; set; } = string.Empty;
            public string Instructions { get; set; } = string.Empty;
            public DateTime? CreatedAt { get; set; }
            public string DoctorName { get; set; } = string.Empty;
        }
    }
} 