using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Barangay.Extensions;
using System.ComponentModel.DataAnnotations;

namespace Barangay.Pages.VitalSigns
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly EncryptedDbContext _context;
        private readonly ILogger<IndexModel> _logger;
        private readonly IPermissionService _permissionService;
        private readonly IDataEncryptionService _encryptionService;

        public IndexModel(
            EncryptedDbContext context, 
            ILogger<IndexModel> logger,
            IPermissionService permissionService,
            IDataEncryptionService encryptionService)
        {
            _context = context;
            _logger = logger;
            _permissionService = permissionService;
            _encryptionService = encryptionService;
            VitalSignRecords = new List<VitalSignViewModel>();
            PatientSelectList = new List<SelectListItem>();
        }

        public class VitalSignViewModel
        {
            public int Id { get; set; }
            
            // Allow empty PatientId for auto-generation
            public string PatientId { get; set; } = string.Empty;
            
            public string PatientName { get; set; } = string.Empty;
            public DateTime RecordedAt { get; set; }
            public string? BloodPressure { get; set; }
            public string? HeartRate { get; set; }
            
            [Required(ErrorMessage = "Temperature is required")]
            public string? Temperature { get; set; }
            
            public string? RespiratoryRate { get; set; }
            public string? SpO2 { get; set; }
            public string? Weight { get; set; }
            public string? Height { get; set; }
            public string? Notes { get; set; }
        }

        [BindProperty]
        public VitalSignViewModel NewVitalSign { get; set; } = new();
        public List<VitalSignViewModel> VitalSignRecords { get; set; }
        public List<SelectListItem> PatientSelectList { get; set; }
        public bool CanRecordVitalSigns { get; set; }
        public bool CanViewVitalSigns { get; set; }
        public bool CanDeleteVitalSigns { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Check if user has access to this page (single permission)
                var hasAccessVitalSigns = await _permissionService.UserHasPermissionAsync(User, "Access Vital Signs");

                if (!hasAccessVitalSigns)
                {
                    _logger.LogWarning("User attempted to access VitalSigns page without permission");
                    return Forbid();
                }

                // Set permission flags (simplified: Access Vital Signs allows view/record on this page)
                CanRecordVitalSigns = hasAccessVitalSigns;
                CanViewVitalSigns = hasAccessVitalSigns;
                CanDeleteVitalSigns = await _permissionService.UserHasPermissionAsync(User, "Delete Vital Signs Data");

                // Load patients for the dropdown directly from the Patients table
                var patientsQuery = _context.Patients
                    .Where(p => p.FullName != "System Administrator" && !string.IsNullOrWhiteSpace(p.FullName))
                    .OrderBy(p => p.FullName)
                    .Select(p => new SelectListItem
                    {
                        Value = p.UserId,
                        Text = p.FullName
                    });

                PatientSelectList = await patientsQuery.ToListAsync();
                _logger.LogInformation($"Loaded {PatientSelectList.Count} patients for dropdown");

                // Load vital signs only if user has permission to view them
                if (CanViewVitalSigns)
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

                    // For vital signs where Patient is null (might be from appointments),
                    // use the same patient ID to name lookup
                    VitalSignRecords = vitalSigns.Select(v => new VitalSignViewModel
                    {
                        Id = v.Id,
                        PatientId = v.PatientId,
                        PatientName = v.Patient?.FullName ?? GetPatientName(v.PatientId),
                        RecordedAt = v.RecordedAt,
                        BloodPressure = v.BloodPressure,
                        HeartRate = v.HeartRate?.ToString(),
                        Temperature = v.Temperature?.ToString(),
                        RespiratoryRate = v.RespiratoryRate?.ToString(),
                        SpO2 = v.SpO2?.ToString(),
                        Weight = v.Weight?.ToString(),
                        Height = v.Height?.ToString(),
                        Notes = v.Notes
                    }).ToList();

                    _logger.LogInformation($"Loaded {VitalSignRecords.Count} vital sign records");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading vital signs");
                ModelState.AddModelError(string.Empty, "Error loading vital signs. Please try again.");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Check if user has permission to record vital signs (single permission)
                if (!await _permissionService.UserHasPermissionAsync(User, "Access Vital Signs"))
                {
                    _logger.LogWarning("User attempted to record vital signs without permission");
                    return Forbid();
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning($"Validation error: {error.ErrorMessage}");
                    }
                    
                    TempData["ErrorMessage"] = "Please correct the errors below.";
                    await OnGetAsync();  // Reload the page data
                    return Page();
                }

                // Generate a random PatientId if not selected
                if (string.IsNullOrEmpty(NewVitalSign.PatientId))
                {
                    // Generate a random PatientId using a GUID substring
                    NewVitalSign.PatientId = Guid.NewGuid().ToString().Substring(0, 8);
                    _logger.LogInformation($"Generated random PatientId: {NewVitalSign.PatientId}");
                }
                else
                {
                    // Verify patient exists
                    var patientExists = await _context.Patients.AnyAsync(p => p.UserId == NewVitalSign.PatientId) ||
                                       await _context.Appointments.AnyAsync(a => a.PatientId == NewVitalSign.PatientId);
                                       
                    if (!patientExists)
                    {
                        ModelState.AddModelError("NewVitalSign.PatientId", "Selected patient does not exist.");
                        TempData["ErrorMessage"] = "The selected patient could not be found.";
                        await OnGetAsync();  // Reload the page data
                        return Page();
                    }
                }

                // Check if temperature is provided (required field)
                if (string.IsNullOrEmpty(NewVitalSign.Temperature))
                {
                    ModelState.AddModelError("NewVitalSign.Temperature", "Temperature is required.");
                    TempData["ErrorMessage"] = "Please enter a temperature value.";
                    await OnGetAsync();  // Reload the page data
                    return Page();
                }

                // Get patient name from the database
                string patientName = "";
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == NewVitalSign.PatientId);
                if (patient != null)
                {
                    patientName = patient.FullName;
                }
                else
                {
                    patientName = $"Patient {NewVitalSign.PatientId}";
                }

                var vitalSign = new VitalSign
                {
                    PatientId = NewVitalSign.PatientId,
                    // Store encrypted data in encrypted columns
                    EncryptedTemperature = !string.IsNullOrEmpty(NewVitalSign.Temperature) ? _encryptionService.Encrypt(NewVitalSign.Temperature) : null,
                    EncryptedBloodPressure = !string.IsNullOrEmpty(NewVitalSign.BloodPressure) ? _encryptionService.Encrypt(NewVitalSign.BloodPressure) : null,
                    EncryptedHeartRate = !string.IsNullOrEmpty(NewVitalSign.HeartRate) ? _encryptionService.Encrypt(NewVitalSign.HeartRate) : null,
                    EncryptedRespiratoryRate = !string.IsNullOrEmpty(NewVitalSign.RespiratoryRate) ? _encryptionService.Encrypt(NewVitalSign.RespiratoryRate) : null,
                    EncryptedSpO2 = !string.IsNullOrEmpty(NewVitalSign.SpO2) ? _encryptionService.Encrypt(NewVitalSign.SpO2) : null,
                    EncryptedWeight = !string.IsNullOrEmpty(NewVitalSign.Weight) ? _encryptionService.Encrypt(NewVitalSign.Weight) : null,
                    EncryptedHeight = !string.IsNullOrEmpty(NewVitalSign.Height) ? _encryptionService.Encrypt(NewVitalSign.Height) : null,
                    RecordedAt = DateTime.Now,
                    Notes = NewVitalSign.Notes
                };

                try
                {
                    _context.VitalSigns.Add(vitalSign);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Successfully saved vital signs for patient {NewVitalSign.PatientId} (Name: {patientName})");
                    TempData["SuccessMessage"] = "Vital signs recorded successfully!";
                    return RedirectToPage();
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Database error saving vital signs for patient {PatientId}", NewVitalSign.PatientId);
                    TempData["ErrorMessage"] = $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}";
                    await OnGetAsync();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving vital signs");
                ModelState.AddModelError(string.Empty, "Error saving vital signs. Please try again.");
                TempData["ErrorMessage"] = $"Error saving vital signs: {ex.Message}";
                await OnGetAsync();  // Reload the page data
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                // Check if user has permission to delete vital signs
                if (!await _permissionService.UserHasPermissionAsync(User, "Delete Vital Signs Data"))
                {
                    _logger.LogWarning("User attempted to delete vital signs without permission");
                    return Forbid();
                }

                var vitalSign = await _context.VitalSigns.FindAsync(id);
                if (vitalSign == null)
                {
                    return NotFound();
                }

                _context.VitalSigns.Remove(vitalSign);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully deleted vital sign record {id}");
                TempData["SuccessMessage"] = "Vital sign record deleted successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting vital sign record");
                TempData["ErrorMessage"] = "Error deleting vital sign record. Please try again.";
                return RedirectToPage();
            }
        }

        
        private string GetPatientName(string patientId)
        {
            try
            {
                if (string.IsNullOrEmpty(patientId))
                    return "Unknown Patient";
                    
                var patient = _context.Patients
                    .FirstOrDefault(p => p.UserId == patientId);
                    
                return patient?.FullName ?? "Unknown Patient";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving patient name for ID {patientId}");
                return "Unknown Patient";
            }
        }
    }
}
