using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Barangay.Data;
using Barangay.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Barangay.Services;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Barangay.Extensions;
using System.Globalization;
using Barangay.Helpers;
using Barangay.Models.Appointments;

// Ensure this is the first line (no statements before namespace)
namespace Barangay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserApiController> _logger;
        private readonly IEncryptionService _encryptionService;
        private readonly IAppointmentService _appointmentService;

        public UserApiController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ILogger<UserApiController> logger,
            IEncryptionService encryptionService,
            IAppointmentService appointmentService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _encryptionService = encryptionService;
            _appointmentService = appointmentService;
        }

        [HttpGet("availableTimeSlots")]
        public async Task<IActionResult> GetAvailableTimeSlots(DateTime date, string doctorId)
        {
            try
            {
                var doctor = await _userManager.FindByIdAsync(doctorId);
                if (doctor == null)
                {
                    return BadRequest(new { error = "Doctor not found" });
                }

                var staffMember = await _context.StaffMembers
                    .FirstOrDefaultAsync(s => s.UserId == doctorId);

                if (staffMember == null || string.IsNullOrEmpty(staffMember.WorkingHours))
                {
                    return BadRequest(new { error = "Doctor's working hours not configured" });
                }

                var workingHours = staffMember.WorkingHours.Split('-').Select(h => h.Trim()).ToArray();
                if (workingHours.Length != 2)
                {
                    return BadRequest(new { error = "Invalid working hours format" });
                }

                if (!TimeSpan.TryParse(workingHours[0], out TimeSpan startTime) ||
                    !TimeSpan.TryParse(workingHours[1], out TimeSpan endTime))
                {
                    return BadRequest(new { error = "Invalid time format in working hours" });
                }

                var dateString = DateTimeHelper.ToDateString(date);
                var existingAppointments = await _context.Appointments
                    .Where(a => DateTimeHelper.AreDatesEqual(a.AppointmentDate, date) && 
                           a.DoctorId == doctorId &&
                           a.Status != AppointmentStatus.Cancelled)
                    .Select(a => a.AppointmentTime)
                    .ToListAsync();

                var allTimeSlots = new List<string>();
                for (var time = startTime; time < endTime; time = time.Add(TimeSpan.FromMinutes(30)))
                {
                    if (!existingAppointments.Contains(time))
                    {
                        allTimeSlots.Add(time.ToString(@"hh\:mm"));
                    }
                }

                return Ok(new { timeSlots = allTimeSlots });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available time slots");
                return StatusCode(500, new { error = "Error fetching available time slots" });
            }
        }

        [HttpGet("patients")]
        public async Task<IActionResult> GetPatients(string? searchTerm = null)
        {
            try
            {
                var query = _context.Patients.AsQueryable();
                
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(p => 
                        p.Name.ToLower().Contains(searchTerm) || 
                        p.UserId.ToString().Contains(searchTerm));
                }

                var patients = await query
                    .Select(p => new {
                        Id = p.UserId,
                        p.Name,
                        p.BirthDate,
                        p.Gender,
                        p.ContactNumber,
                        p.Address
                    })
                    .ToListAsync();

                return Ok(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching patients");
                return StatusCode(500, new { error = "Error fetching patients" });
            }
        }

        // Fix the route conflict and type mismatch in the first method
        [HttpGet("patient/id/{userId}/medical-history")]
        public async Task<IActionResult> GetPatientMedicalHistoryById(string userId)
        {
            try
            {
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserId == userId);
        
                if (patient == null)
                {
                    return NotFound(new { error = "Patient not found" });
                }
        
                // Get medical records separately - fix the type mismatch here
                var medicalRecords = await _context.MedicalRecords
                    .Where(mr => mr.PatientId == patient.UserId) // Change from id (int) to patient.UserId (string)
                    .OrderByDescending(mr => mr.Date)
                    .ToListAsync();
        
                return Ok(new { 
                    patient = new {
                        Id = patient.UserId,
                        patient.Name,
                        patient.BirthDate,
                        patient.Gender,
                        patient.ContactNumber,
                        patient.Address
                    },
                    medicalRecords = medicalRecords.Select(mr => new {
                        mr.Id,
                        mr.Date,
                        mr.Diagnosis,
                        mr.Treatment,
                        mr.Notes
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching patient medical history: {Message}", ex.Message);
                return StatusCode(500, new { error = "Error fetching patient medical history" });
            }
        }

        [HttpPost("appointments")]
        public async Task<IActionResult> CreateAppointment([FromBody] AppointmentCreateModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                string todayString = DateTimeHelper.Today.ToString("yyyy-MM-dd");
                
                if (string.Compare(model.AppointmentDate, todayString) < 0)
                {
                    return BadRequest("Cannot schedule appointments in the past.");
                }

                if (!TimeSpan.TryParse(model.AppointmentTime, out TimeSpan appointmentTime))
                {
                    return BadRequest("Invalid time format. Please use HH:mm format.");
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var appointment = new Appointment
                {
                    DoctorId = model.StaffId,
                    PatientId = user.Id,
                    PatientName = user.UserName,
                    ReasonForVisit = model.Description,
                    Status = AppointmentStatus.Pending,
                    AppointmentDate = DateTimeHelper.ParseDate(model.AppointmentDate),
                    AppointmentTime = appointmentTime,
                    AgeValue = 0,
                    CreatedAt = DateTimeHelper.ToUtc(DateTimeHelper.Now),
                    UpdatedAt = DateTimeHelper.ToUtc(DateTimeHelper.Now)
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Appointment created successfully" });
            }
            catch (FormatException)
            {
                return BadRequest("Invalid date or time format.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating appointment on {Date}", DateTimeHelper.Now.ToString("yyyy-MM-dd"));
                return StatusCode(500, "An error occurred while creating the appointment.");
            }
        }
        
        // Add any other methods for the UserApiController here
        
        // Method to get patient medical history with string PatientId
        [HttpGet("patient/{patientId}/medical-history")]
        public async Task<IActionResult> GetPatientMedicalHistory(string patientId)
        {
            try
            {
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .Where(p => p.UserId == patientId)
                    .Select(p => new
                    {
                        p.UserId,
                        p.FullName,
                        p.Gender,
                        BirthDate = p.BirthDate,
                        Age = p.Age,
                        p.Status,
                        p.ContactNumber,
                        p.Address,
                        p.EmergencyContact,
                        p.EmergencyContactNumber,
                        p.MedicalHistory,
                        p.Allergies,
                        p.CurrentMedications
                    })
                    .FirstOrDefaultAsync();

                if (patient == null)
                {
                    return NotFound(new { error = "Patient not found" });
                }

                var appointments = await _context.Appointments
                    .Where(a => a.PatientId == patientId)
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => new
                    {
                        a.Id,
                        a.DoctorId,
                        a.AppointmentDate,
                        a.AppointmentTime,
                        a.Status,
                        a.Description,
                        a.CreatedAt
                    })
                    .ToListAsync();

                var medicalRecords = await _context.MedicalRecords
                    .Where(m => m.PatientId == patientId)
                    .OrderByDescending(m => m.Date)
                    .Select(m => new
                    {
                        m.Id,
                        m.Date,
                        m.Diagnosis,
                        m.Treatment,
                        m.Prescription,
                        m.Instructions,
                        m.DoctorId
                    })
                    .ToListAsync();

                var prescriptions = await _context.Prescriptions
                    .Where(p => p.PatientId == patientId)
                    .OrderByDescending(p => p.PrescriptionDate)
                    .ToListAsync();

                return Ok(new
                {
                    patient,
                    appointments,
                    medicalRecords,
                    prescriptions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patient medical history: {Message}", ex.Message);
                return StatusCode(500, new { error = "Error getting patient medical history" });
            }
        }

        [HttpGet("appointments")]
        public async Task<IActionResult> GetAppointments()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Where(a => a.PatientId == user.Id)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return Ok(appointments);
        }

        [HttpGet("appointments/{id}")]
        public async Task<IActionResult> GetAppointment(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (appointment.PatientId != user.Id)
            {
                return Unauthorized();
            }

            // Convert TimeSpan to string for comparison
            var timeString = appointment.AppointmentTime.ToString(@"hh\:mm");

            return Ok(appointment);
        }

        [HttpPut("appointments/{id}")]
        public async Task<IActionResult> UpdateAppointment(int id, [FromBody] AppointmentUpdateModel model)
        {
            var existingAppointment = await _context.Appointments.FindAsync(id);
            if (existingAppointment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (existingAppointment.PatientId != user.Id)
            {
                return Unauthorized();
            }

            try
            {
                // Compare dates and times after parsing
                var newAppointmentDate = DateTime.Parse(model.AppointmentDate);
                var newAppointmentTime = TimeSpan.Parse(model.AppointmentTime);

                if (newAppointmentDate != existingAppointment.AppointmentDate ||
                    newAppointmentTime != existingAppointment.AppointmentTime)
                {
                    // Check if new slot is available
                    var isSlotAvailable = await _appointmentService.CheckTimeSlotAvailability(
                        model.StaffId,
                        newAppointmentDate,
                        newAppointmentTime);

                    if (!isSlotAvailable)
                    {
                        return BadRequest("Selected time slot is no longer available");
                    }
                }

                // Update appointment
                existingAppointment.DoctorId = model.StaffId;
                existingAppointment.AppointmentDate = newAppointmentDate;
                existingAppointment.AppointmentTime = newAppointmentTime;
                existingAppointment.Description = model.Description;
                existingAppointment.Status = Enum.Parse<AppointmentStatus>(model.Status);
                existingAppointment.PatientName = model.IsForDependent ? model.DependentName : existingAppointment.PatientName;
                                existingAppointment.AgeValue = model.IsForDependent ? (model.DependentAge ?? existingAppointment.AgeValue) : existingAppointment.AgeValue;
                existingAppointment.RelationshipToDependent = model.IsForDependent ? model.RelationshipToDependent : null;
                existingAppointment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Appointment updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating appointment");
                return StatusCode(500, "An error occurred while updating the appointment.");
            }
        }

        [HttpPost("cancelAppointment/{appointmentId}")]
        public async Task<IActionResult> CancelAppointment(int appointmentId)
        {
            _logger.LogInformation("=== USER API CANCEL APPOINTMENT DEBUG START ===");
            _logger.LogInformation("Appointment ID: {AppointmentId}", appointmentId);
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null) 
            {
                _logger.LogWarning("User not authenticated for User API cancellation");
                return Unauthorized();
            }
            
            _logger.LogInformation("User ID: {UserId}, User Name: {UserName}", user.Id, user.UserName);

            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null) 
            {
                _logger.LogWarning("Appointment not found for User API cancellation: {AppointmentId}", appointmentId);
                return NotFound();
            }
            
            _logger.LogInformation("Found appointment: ID={AppointmentId}, PatientId={PatientId}, Date={AppointmentDate}, Time={AppointmentTime}, Status={Status}", 
                appointment.Id, appointment.PatientId, appointment.AppointmentDate, appointment.AppointmentTime, appointment.Status);

            if (appointment.PatientId != user.Id)
            {
                _logger.LogWarning("Unauthorized User API cancellation attempt: User {UserId} trying to cancel appointment {AppointmentId} owned by {PatientId}", 
                    user.Id, appointmentId, appointment.PatientId);
                return Unauthorized();
            }

            // Only allow cancellation for future appointments
            if (appointment.AppointmentDate < DateTime.Now.Date || 
                (appointment.AppointmentDate == DateTime.Now.Date && appointment.AppointmentTime < DateTime.Now.TimeOfDay))
            {
                _logger.LogWarning("Attempted to cancel past appointment via User API: {AppointmentId}", appointmentId);
                return BadRequest(new { success = false, message = "Cannot cancel past appointments." });
            }

            try
            {
                _logger.LogInformation("Updating appointment status to Cancelled via User API");
                appointment.Status = AppointmentStatus.Cancelled;
                appointment.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Appointment cancelled successfully via User API: {AppointmentId}", appointmentId);
                return Ok(new { success = true, message = "Appointment cancelled successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling appointment via User API {AppointmentId}", appointmentId);
                return StatusCode(500, new { success = false, message = "Failed to cancel appointment." });
            }
            finally
            {
                _logger.LogInformation("=== USER API CANCEL APPOINTMENT DEBUG END ===");
            }
        }

        [HttpDelete("appointments/{id}")]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (appointment.PatientId != user.Id)
            {
                return Unauthorized();
            }

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Appointment deleted successfully" });
        }
    }
}
