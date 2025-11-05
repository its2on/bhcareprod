using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Barangay.Data;
using Barangay.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Barangay.Services;
using Barangay.Extensions;
using System.Collections.Generic;
using Barangay.Helpers;

namespace Barangay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Nurse")]
    public class NurseApiController : ControllerBase
    {
        private readonly EncryptedDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NurseApiController> _logger;
        private readonly IDataEncryptionService _encryptionService;
        private readonly IAuditTrailService _auditTrail;

        public NurseApiController(
            EncryptedDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<NurseApiController> logger,
            IDataEncryptionService encryptionService,
            IAuditTrailService auditTrail)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _encryptionService = encryptionService;
            _auditTrail = auditTrail;
        }

        [HttpGet("appointments")]
        public async Task<IActionResult> GetAppointments()
        {
            try
            {
                var appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToListAsync();

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching appointments");
                return StatusCode(500, "Error fetching appointments");
            }
        }

        [HttpPost("appointment/{id}/update-status")]
        public async Task<IActionResult> UpdateAppointmentStatus(int id, [FromBody] AppointmentStatus status)
        {
            try
            {
                var appointment = await _context.Appointments.FindAsync(id);
                if (appointment == null) return NotFound("Appointment not found");

                appointment.Status = status;
                appointment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating appointment status");
                return StatusCode(500, "Error updating appointment status");
            }
        }

        [HttpGet("dashboard-data")]
        public async Task<IActionResult> GetDashboardData()
        {
            try
            {
                var today = DateTime.Today;
                var appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.AppointmentDate.Date == today.Date)
                    .ToListAsync();

                var metrics = new
                {
                    TotalPatients = appointments.Count(),
                    InProgressPatients = appointments.Count(a => a.Status == AppointmentStatus.InProgress),
                    WaitingPatients = appointments.Count(a => a.Status == AppointmentStatus.Pending),
                    CompletedPatients = appointments.Count(a => a.Status == AppointmentStatus.Completed),
                    CriticalAlerts = appointments.Count(a => a.Status == AppointmentStatus.Urgent)
                };

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dashboard data");
                return StatusCode(500, "Error fetching dashboard data");
            }
        }

        [HttpPost("send-message")]
        public async Task<IActionResult> SendMessage([FromBody] Message messageData)
        {
            try
            {
                var message = new Message
                {
                    SenderName = User.Identity?.Name ?? "System",
                    RecipientGroup = messageData.RecipientGroup,
                    Content = messageData.Content,
                    Timestamp = DateTime.Now
                };
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return StatusCode(500, "Error sending message");
            }
        }

        [HttpGet("messages")]
        public async Task<IActionResult> GetMessages()
        {
            try
            {
                var messages = await _context.Messages
                    .OrderByDescending(m => m.Timestamp)
                    .Take(50)
                    .ToListAsync();
                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching messages");
                return StatusCode(500, "Error fetching messages");
            }
        }

        [HttpPost("vital-signs")]
        public async Task<IActionResult> SaveVitalSigns([FromForm] VitalSignsUpdateModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.PatientId))
                {
                    return BadRequest(new { success = false, message = "Patient ID is required" });
                }

                // Create a new vital sign record
                var vitalSign = new VitalSign
                {
                    PatientId = model.PatientId,
                    // Store encrypted data in encrypted columns
                    EncryptedBloodPressure = !string.IsNullOrEmpty(model.BloodPressure) ? _encryptionService.Encrypt(model.BloodPressure) : null,
                    EncryptedHeartRate = !string.IsNullOrEmpty(model.HeartRate) ? _encryptionService.Encrypt(model.HeartRate) : null,
                    EncryptedTemperature = !string.IsNullOrEmpty(model.Temperature) ? _encryptionService.Encrypt(model.Temperature) : null,
                    EncryptedRespiratoryRate = !string.IsNullOrEmpty(model.RespiratoryRate) ? _encryptionService.Encrypt(model.RespiratoryRate) : null,
                    EncryptedSpO2 = !string.IsNullOrEmpty(model.SpO2) ? _encryptionService.Encrypt(model.SpO2) : null,
                    EncryptedWeight = !string.IsNullOrEmpty(model.Weight) ? _encryptionService.Encrypt(model.Weight) : null,
                    EncryptedHeight = !string.IsNullOrEmpty(model.Height) ? _encryptionService.Encrypt(model.Height) : null,
                    Notes = model.Notes,
                    RecordedAt = DateTime.Now
                };

                // EncryptedDbContext will handle encryption automatically
                _context.VitalSigns.Add(vitalSign);
                await _context.SaveChangesAsync();

                // AUDIT: Log vital signs creation
                await _auditTrail.LogAsync(
                    "Create",
                    "Vital signs recorded",
                    "VitalSign",
                    vitalSign.Id.ToString(),
                    null,
                    Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        PatientId = model.PatientId,
                        BloodPressure = model.BloodPressure,
                        HeartRate = model.HeartRate,
                        Temperature = model.Temperature,
                        RespiratoryRate = model.RespiratoryRate,
                        SpO2 = model.SpO2,
                        Weight = model.Weight,
                        Height = model.Height
                    }),
                    $"Nurse recorded vital signs for patient {model.PatientId}"
                );

                // If appointment ID is provided, update the appointment's vital signs
                if (!string.IsNullOrEmpty(model.AppointmentId))
                {
                    int appointmentId = int.Parse(model.AppointmentId);
                    var appointment = await _context.Appointments.FindAsync(appointmentId);
                    
                    if (appointment != null)
                    {
                        // Update the appointment's status to indicate vital signs were taken
                        // Note: We're not updating fields that don't exist in the model
                        appointment.UpdatedAt = DateTime.Now;
                        
                        // Optional: Add notes about vital signs to the appointment
                        string vitalSignsInfo = $"VS recorded: BP: {model.BloodPressure ?? "N/A"}, HR: {model.HeartRate ?? "N/A"}, Temp: {model.Temperature ?? "N/A"}";
                        
                        // If Description field exists, append to it
                        if (string.IsNullOrEmpty(appointment.Description))
                            appointment.Description = vitalSignsInfo;
                        else
                            appointment.Description += Environment.NewLine + vitalSignsInfo;
                        
                        await _context.SaveChangesAsync();
                    }
                }

                _logger.LogInformation("Vital signs recorded for patient {PatientId}", model.PatientId);
                return Ok(new { success = true, message = "Vital signs recorded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving vital signs");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("search-patient")]
        public async Task<IActionResult> SearchPatient([FromQuery] string query)
        {
            try
            {
                _logger.LogInformation($"Searching for patients with query: {query}");

                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Search query cannot be empty");
                }

                var patients = await _context.Patients
                    .Where(p => p.Name.Contains(query) || 
                               p.Email.Contains(query) ||
                               p.ContactNumber.Contains(query))
                    .Select(p => new
                    {
                        p.UserId,
                        p.Name,
                        p.Email,
                        p.Gender,
                        p.ContactNumber,
                        Age = p.Age,
                        Status = p.Status
                    })
                    .Take(10)
                    .ToListAsync();

                _logger.LogInformation($"Found {patients.Count} patients matching the query");
                return Ok(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching patients");
                return StatusCode(500, "Error searching patients");
            }
        }

        [HttpGet("patient/{userId}/vitals-info")]
        public async Task<IActionResult> GetPatientVitalsInfo(string userId)
        {
            try
            {
                _logger.LogInformation($"Fetching patient vitals info for userId: {userId}");

                // Get patient from Patients table
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (patient == null)
                {
                    _logger.LogWarning($"Patient not found for userId: {userId}");
                    return NotFound("Patient not found");
                }

                // Get appointment patient name (the name used when booking)
                var appointmentPatientName = await _context.Appointments
                    .Where(a => a.PatientId == userId)
                    .OrderByDescending(a => a.AppointmentDate)
                    .Select(a => a.PatientName)
                    .FirstOrDefaultAsync() ?? patient.Name;

                // Get user information for barangay
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                
                // Decrypt patient data
                patient.DecryptSensitiveData(_encryptionService, User);
                if (user != null)
                {
                    user.DecryptSensitiveData(_encryptionService, User);
                }

                var patientInfo = new
                {
                    PatientId = userId,
                    AppointmentPatientName = appointmentPatientName,
                    AccountPatientName = patient.Name,
                    Age = patient.Age,
                    Gender = patient.Gender,
                    DateOfBirth = patient.BirthDate.ToString("MMMM dd, yyyy"),
                    Address = patient.Address,
                    ContactNumber = patient.ContactNumber,
                    Barangay = user?.Barangay ?? "-",
                    BloodType = patient.BloodType,
                    Allergies = patient.Allergies,
                    // Assessment form info
                    AssessmentFormType = patient.Age >= 20 ? "NCD Risk Assessment" : "HEEADSSS Assessment",
                    IsAssessmentCompleted = patient.Age >= 20 ? 
                        await _context.NCDRiskAssessments.AnyAsync(n => n.UserId == userId) :
                        await _context.HEEADSSSAssessments.AnyAsync(h => h.UserId == userId)
                };

                _logger.LogInformation($"Returning vitals info for: {appointmentPatientName}");
                return Ok(patientInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching patient vitals info");
                return StatusCode(500, "Error fetching patient information");
            }
        }

        [HttpGet("patient/{userId}")]
        public async Task<IActionResult> GetPatientInformation(string userId)
        {
            try
            {
                _logger.LogInformation($"Fetching patient information for userId: {userId}");

                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (patient == null)
                {
                    _logger.LogWarning($"Patient not found for userId: {userId}");
                    return NotFound("Patient not found");
                }

                _logger.LogInformation($"Found patient: {patient.Name}");

                var patientInfo = new
                {
                    FullName = patient.Name,
                    Email = patient.Email,
                    Gender = patient.Gender,
                    DateOfBirth = patient.BirthDate,
                    Address = patient.Address,
                    ContactNumber = patient.ContactNumber,
                    EmergencyContact = patient.EmergencyContact,
                    EmergencyContactNumber = patient.EmergencyContactNumber,
                    Allergies = patient.Allergies,
                    CurrentMedications = patient.CurrentMedications,
                    MedicalHistory = patient.MedicalHistory,
                    Status = patient.Status,
                    Room = patient.Room,
                    Diagnosis = patient.Diagnosis,
                    Alert = patient.Alert,
                    Time = patient.Time?.ToString(@"hh\:mm")
                };

                _logger.LogInformation($"Returning patient info for: {patientInfo.FullName}");
                return Ok(patientInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching patient information");
                return StatusCode(500, "Error fetching patient information");
            }
        }
    }

    public class VitalSignsUpdateModel
    {
        public string PatientId { get; set; } = string.Empty;
        public string? AppointmentId { get; set; }
        public string? BloodPressure { get; set; }
        public string? HeartRate { get; set; }
        public string? Temperature { get; set; }
        public string? RespiratoryRate { get; set; }
        public string? SpO2 { get; set; }
        public string? Weight { get; set; }
        public string? Height { get; set; }
        public string? Notes { get; set; }
    }
}
