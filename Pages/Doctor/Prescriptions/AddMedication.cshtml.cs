using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Barangay.Pages.Doctor.Prescriptions
{
    [Authorize(Roles = "Doctor")]
    public class AddMedicationModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AddMedicationModel> _logger;
        private readonly IAuditTrailService _auditTrail;

        public AddMedicationModel(
            ApplicationDbContext context,
            ILogger<AddMedicationModel> logger,
            IAuditTrailService auditTrail)
        {
            _context = context;
            _logger = logger;
            _auditTrail = auditTrail;
        }

        [BindProperty]
        public int PrescriptionId { get; set; }

        [BindProperty]
        public PrescriptionMedicationViewModel Medication { get; set; } = new();

        public class PrescriptionMedicationViewModel
        {
            public string MedicationName { get; set; }
            public string Dosage { get; set; }
            public string Unit { get; set; }
            public string Frequency { get; set; }
            public string Duration { get; set; }
            public string Instructions { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            PrescriptionId = id;

            var prescription = await _context.Prescriptions
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prescription == null)
            {
                TempData["ErrorMessage"] = "Prescription not found.";
                return RedirectToPage("/Doctor/Prescriptions/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var prescription = await _context.Prescriptions
                .FirstOrDefaultAsync(p => p.Id == PrescriptionId);

            if (prescription == null)
            {
                TempData["ErrorMessage"] = "Prescription not found.";
                return RedirectToPage("/Doctor/Prescriptions/Index");
            }

            try
            {
                // Find or create medication
                var medication = await _context.Medications
                    .FirstOrDefaultAsync(m => m.Name.ToLower() == Medication.MedicationName.ToLower());

                if (medication == null)
                {
                    medication = new Medication
                    {
                        Name = Medication.MedicationName,
                        Description = "Added from prescription"
                    };
                    _context.Medications.Add(medication);
                    await _context.SaveChangesAsync();
                }

                // Create prescription medication
                var prescriptionMedication = new PrescriptionMedication
                {
                    PrescriptionId = PrescriptionId,
                    MedicationId = medication.Id,
                    MedicationName = medication.Name,
                    Dosage = Medication.Dosage,
                    Unit = Medication.Unit,
                    Frequency = Medication.Frequency,
                    Duration = Medication.Duration ?? "As prescribed",
                    Instructions = Medication.Instructions ?? "Take as directed"
                };

                // Find the first medical record associated with this prescription (if any)
                var medicalRecord = await _context.MedicalRecords
                    .FirstOrDefaultAsync(mr => mr.AppointmentId == prescription.Id);

                // If there's no medical record, create a minimal one
                if (medicalRecord == null)
                {
                    medicalRecord = new MedicalRecord
                    {
                        PatientId = prescription.PatientId,
                        DoctorId = prescription.DoctorId,
                        Date = DateTime.UtcNow,
                        Type = "Prescription",
                        ChiefComplaint = "Medication Added",
                        Status = "Active"
                    };
                    _context.MedicalRecords.Add(medicalRecord);
                    await _context.SaveChangesAsync();
                }

                prescriptionMedication.MedicalRecordId = medicalRecord.Id;
                _context.PrescriptionMedications.Add(prescriptionMedication);
                await _context.SaveChangesAsync();

                // Log audit trail
                await _auditTrail.LogAsync(
                    "Create",
                    $"Added prescription medication: {medication.Name}",
                    "PrescriptionMedication",
                    prescriptionMedication.Id.ToString(),
                    null,
                    JsonConvert.SerializeObject(new {
                        MedicationName = medication.Name,
                        Dosage = Medication.Dosage,
                        Frequency = Medication.Frequency,
                        Duration = Medication.Duration,
                        PatientId = prescription.PatientId
                    }),
                    $"Doctor prescribed {medication.Name} for patient"
                );

                TempData["SuccessMessage"] = "Medication added successfully to the prescription.";
                return RedirectToPage("/Doctor/Prescriptions/Details", new { id = PrescriptionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding medication to prescription {PrescriptionId}", PrescriptionId);
                ModelState.AddModelError("", "An error occurred while adding the medication. Please try again.");
                return Page();
            }
        }
    }
} 