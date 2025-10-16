using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using System.Globalization;
using Barangay.Helpers;

namespace Barangay.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly ILogger<AppointmentsController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEncryptionService _encryptionService;

        public AppointmentsController(
            ILogger<AppointmentsController> logger,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEncryptionService encryptionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        }

        [HttpPost("book")]
        public async Task<IActionResult> BookAppointment([FromBody] AppointmentModel model)
        {
            if (model == null) return BadRequest(new { success = false, message = "Invalid request: No data received" });

            try
            {
                _logger.LogInformation($"Received booking request: Doctor={model.DoctorId}, Date={model.AppointmentDate}, Time={model.AppointmentTime}");
                
                // Fix date and time parsing
                var appointmentDate = DateTimeHelper.ParseDate(model.AppointmentDate);
                if (appointmentDate == DateTime.MinValue)
                {
                    _logger.LogWarning($"Invalid date format: {model.AppointmentDate}");
                    return BadRequest(new { success = false, message = "Invalid date format. Use yyyy-MM-dd" });
                }

                var parsedTime = DateTimeHelper.ParseTime(model.AppointmentTime);
                if (parsedTime == TimeSpan.Zero)
                {
                    _logger.LogWarning($"Invalid time format: {model.AppointmentTime}");
                    return BadRequest(new { success = false, message = "Invalid time format. Use HH:mm or hh:mm tt" });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User not authenticated");
                    return Unauthorized(new { success = false, message = "You must be logged in to book an appointment" });
                }

                var doctor = await _userManager.FindByIdAsync(model.DoctorId?.ToString() ?? string.Empty);
                if (doctor == null)
                {
                    _logger.LogWarning($"Doctor not found: {model.DoctorId}");
                    return BadRequest(new { success = false, message = "Doctor not found" });
                }

                var workingDays = doctor.WorkingDays?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => d.Trim())
                    .ToArray();

                if (workingDays != null && workingDays.Length > 0 && !workingDays.Contains(appointmentDate.DayOfWeek.ToString()))
                {
                    _logger.LogWarning($"Doctor {doctor.Id} is not available on {appointmentDate.DayOfWeek}");
                    return BadRequest(new { success = false, message = $"Doctor is not available on {appointmentDate.DayOfWeek}" });
                }

                // Check if slot is already booked
                var isSlotBooked = await _context.Appointments
                    .AnyAsync(a => a.DoctorId == model.DoctorId && 
                          DateTimeHelper.AreDatesEqual(a.AppointmentDate, appointmentDate) &&
                          a.AppointmentTime == parsedTime &&
                          a.Status != AppointmentStatus.Cancelled);

                if (isSlotBooked)
                {
                    _logger.LogWarning($"Time slot already booked: {appointmentDate.ToShortDateString()} at {parsedTime}");
                    return BadRequest(new { success = false, message = "This time slot is already booked. Please select another time." });
                }

                var existingAppointments = await _context.Appointments
                    .CountAsync(a => a.DoctorId == model.DoctorId && 
                          DateTimeHelper.AreDatesEqual(a.AppointmentDate, appointmentDate) &&
                          a.Status != AppointmentStatus.Cancelled);

                if (existingAppointments >= doctor.MaxDailyPatients)
                {
                    _logger.LogWarning($"Doctor reached max appointments: {existingAppointments}/{doctor.MaxDailyPatients}");
                    return BadRequest(new { success = false, message = "Doctor has reached maximum appointments for this day" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User not found: {userId}");
                    return BadRequest(new { success = false, message = "User not found" });
                }

                // Create the appointment
                var appointment = new Appointment
                {
                    DoctorId = model.DoctorId!,
                    PatientId = userId,
                    PatientName = user.FullName ?? user.UserName ?? "Unknown",
                    AppointmentDate = appointmentDate,
                    AppointmentTime = parsedTime,
                    Status = AppointmentStatus.Pending,
                    Description = model.Description ?? string.Empty,
                    ReasonForVisit = model.Description ?? string.Empty,
                    AgeValue = 0, // Default value
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Add entry with explicit relationship tracking
                _context.Entry(appointment).State = EntityState.Added;
                _context.Entry(appointment).Reference(a => a.Doctor).EntityEntry.State = EntityState.Unchanged;
                _context.Entry(appointment).Reference(a => a.Patient).EntityEntry.State = EntityState.Unchanged;
                
                await _context.SaveChangesAsync();
                
                // Reload the complete appointment with navigation properties to ensure proper references
                appointment = await _context.Appointments
                    .Include(a => a.Doctor)
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.Id == appointment.Id);

                if (appointment == null)
                {
                    _logger.LogError("Failed to retrieve saved appointment");
                    return StatusCode(500, new { success = false, message = "Appointment was created but could not be retrieved" });
                }

                _logger.LogInformation($"Appointment created successfully: ID={appointment.Id}, Doctor={doctor.Id}, Patient={user.Id}, References: Doctor={appointment.Doctor != null}, Patient={appointment.Patient != null}");

                // Send confirmation email
                var subject = "Appointment Confirmation";
                var body = $"Your appointment with Dr. {doctor.FullName} has been confirmed for {appointment.AppointmentDate:d} at {appointment.GetFormattedTime()}.";

                // Commented out email sending since we removed the EmailSender dependency
                // await _emailSender.SendEmailAsync(patient.Email, subject, body);
                _logger.LogInformation($"Email notification would be sent to {user.Email}: {subject}");

                return Ok(new { 
                    success = true, 
                    message = "Appointment booked successfully",
                    appointmentId = appointment.Id,
                    doctorName = doctor.FullName,
                    patientName = user.FullName,
                    date = appointment.GetFormattedDate(),
                    time = appointment.GetFormattedTime()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error booking appointment");
                return StatusCode(500, new { success = false, message = "Internal server error: " + ex.Message });
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> Book(AppointmentModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid request" });
            }

            // Add date/time parsing here
            var parsedDate = DateTimeHelper.ParseDate(model.AppointmentDate);
            if (parsedDate == DateTime.MinValue)
            {
                return BadRequest(new { success = false, message = "Invalid date format. Use yyyy-MM-dd" });
            }
            
            var parsedTime = DateTimeHelper.ParseTime(model.AppointmentTime);
            if (parsedTime == TimeSpan.Zero)
            {
                return BadRequest(new { success = false, message = "Invalid time format. Use HH:mm or hh:mm tt" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Check if patient exists
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
            if (patient == null)
            {
                // Add null checks for user retrieval
                var user = await _userManager.FindByIdAsync(userId) ?? throw new InvalidOperationException("User not found");
                patient = new Patient
                {
                    UserId = userId,
                    Name = user?.UserName ?? "Unknown",
                    FullName = user?.UserName ?? "Unknown",
                    CreatedAt = DateTime.Now
                };
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();
            }

            var patientName = patient.FullName ?? patient.Name ?? userId;

            var appointment = new Appointment
            {
                DoctorId = model.DoctorId ?? throw new ArgumentNullException(nameof(model.DoctorId)),
                PatientId = userId,
                PatientName = patientName,
                AppointmentDate = parsedDate,
                AppointmentTime = parsedTime,
                Status = AppointmentStatus.Pending,
                Description = model.Description ?? string.Empty,
                ReasonForVisit = model.Description ?? string.Empty,
                AgeValue = 0, // Default value
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Appointment booked successfully" });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Appointment>>> GetAppointments()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var isDoctor = await _userManager.IsInRoleAsync(user, "Doctor");
            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Where(a => isDoctor ? a.DoctorId == user.Id : a.PatientId == user.Id)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            _logger.LogInformation($"Retrieved {appointments.Count} appointments for user {user.Id}. IsDoctor: {isDoctor}");
            
            return Ok(appointments);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Appointment>> GetAppointment(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                _logger.LogWarning($"Appointment not found: {id}");
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var isDoctor = await _userManager.IsInRoleAsync(user, "Doctor");
            if (appointment.PatientId != user.Id && appointment.DoctorId != user.Id && !isDoctor)
            {
                _logger.LogWarning($"Unauthorized access to appointment {id} by user {user.Id}");
                return Unauthorized();
            }

            _logger.LogInformation($"Retrieved appointment {id} for user {user.Id}");
            return Ok(appointment);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAppointment(int id, [FromBody] Appointment appointment)
        {
            if (id != appointment.Id)
            {
                return BadRequest();
            }

            var existingAppointment = await _context.Appointments.FindAsync(id);
            if (existingAppointment == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var isDoctor = await _userManager.IsInRoleAsync(user, "Doctor");
            if (appointment.PatientId != user.Id && !isDoctor)
            {
                return Unauthorized();
            }

            try
            {
                if (appointment.AppointmentDate != default)
                {
                    existingAppointment.AppointmentDate = appointment.AppointmentDate;
                }

                if (appointment.AppointmentTime != default)
                {
                    existingAppointment.AppointmentTime = appointment.AppointmentTime;
                }

                if (!string.IsNullOrEmpty(appointment.Description))
                {
                    existingAppointment.Description = appointment.Description;
                }

                if (appointment.Status != existingAppointment.Status)
                {
                    existingAppointment.Status = appointment.Status;
                }

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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var isDoctor = await _userManager.IsInRoleAsync(user, "Doctor");
            if (appointment.PatientId != user.Id && !isDoctor)
            {
                return Unauthorized();
            }

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Appointment deleted successfully" });
        }

        [HttpPost("{id}/start")]
        public async Task<IActionResult> StartConsultation(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            // Only allow doctor or nurse or the assigned doctor to start
            var isDoctor = await _userManager.IsInRoleAsync(user, "Doctor");
            var isNurse = await _userManager.IsInRoleAsync(user, "Nurse");
            if (!isDoctor && !isNurse && appointment.DoctorId != user.Id)
            {
                return Unauthorized();
            }

            if (appointment.Status != AppointmentStatus.Pending)
            {
                return BadRequest(new { success = false, message = "Only pending appointments can be started." });
            }

            appointment.Status = AppointmentStatus.InProgress;
            appointment.UpdatedAt = DateTime.UtcNow;
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();

            // Optionally: Add notification logic for the patient here

            return Ok(new { success = true, message = "Consultation started." });
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            _logger.LogInformation("=== API CANCEL APPOINTMENT DEBUG START ===");
            _logger.LogInformation("Appointment ID: {AppointmentId}", id);
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null) 
            {
                _logger.LogWarning("User not authenticated for API cancellation");
                return Unauthorized();
            }
            
            _logger.LogInformation("User ID: {UserId}, User Name: {UserName}", user.Id, user.UserName);

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) 
            {
                _logger.LogWarning("Appointment not found for API cancellation: {AppointmentId}", id);
                return NotFound();
            }
            
            _logger.LogInformation("Found appointment: ID={AppointmentId}, PatientId={PatientId}, Date={AppointmentDate}, Time={AppointmentTime}, Status={Status}", 
                appointment.Id, appointment.PatientId, appointment.AppointmentDate, appointment.AppointmentTime, appointment.Status);

            var isDoctor = await _userManager.IsInRoleAsync(user, "Doctor");
            if (appointment.PatientId != user.Id && !isDoctor)
            {
                _logger.LogWarning("Unauthorized cancellation attempt: User {UserId} trying to cancel appointment {AppointmentId} owned by {PatientId}", 
                    user.Id, id, appointment.PatientId);
                return Unauthorized();
            }

            // Only allow cancellation for future appointments
            if (appointment.AppointmentDate < DateTime.Now.Date || 
                (appointment.AppointmentDate == DateTime.Now.Date && appointment.AppointmentTime < DateTime.Now.TimeOfDay))
            {
                _logger.LogWarning("Attempted to cancel past appointment via API: {AppointmentId}", id);
                return BadRequest(new { success = false, message = "Cannot cancel past appointments." });
            }

            try
            {
                _logger.LogInformation("Updating appointment status to Cancelled via API");
                appointment.Status = AppointmentStatus.Cancelled;
                appointment.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Appointment cancelled successfully via API: {AppointmentId}", id);
                return Ok(new { success = true, message = "Appointment cancelled successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling appointment via API {AppointmentId}", id);
                return StatusCode(500, new { success = false, message = "Failed to cancel appointment." });
            }
            finally
            {
                _logger.LogInformation("=== API CANCEL APPOINTMENT DEBUG END ===");
            }
        }

        [HttpPost("{appointmentId}/save-consultation")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> SaveConsultation(int appointmentId, [FromBody] ConsultationViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
                
            if (appointment == null)
                return NotFound("Appointment not found.");

            // Get the patient entity to ensure we have a valid reference
            var patient = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == appointment.PatientId.ToString());
            if (patient == null)
                return NotFound("Patient not found.");
                
            // Get doctor entity to ensure we have a valid reference  
            string doctorIdString = appointment.DoctorId?.ToString() ?? string.Empty;
            var doctor = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == doctorIdString);
            if (doctor == null)
                return NotFound("Doctor not found.");

            // Update appointment with consultation notes and set status to Completed
            appointment.Description = model.Notes;
            appointment.Status = AppointmentStatus.Completed;
            appointment.UpdatedAt = DateTime.UtcNow;
            _context.Appointments.Update(appointment);

            // Create medical record for reporting
            var medicalRecord = new MedicalRecord
            {
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                Date = DateTime.Now,
                Type = "Consultation",
                Diagnosis = model.Diagnosis,
                Notes = model.Notes,
                ChiefComplaint = model.ChiefComplaint ?? "", 
                Treatment = model.Treatment ?? "",
                Status = "Completed",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.MedicalRecords.Add(medicalRecord);

            // Create prescription
            var prescription = new Prescription
            {
                PatientId = appointment.PatientId,
                DoctorId = appointment.DoctorId,
                PrescriptionDate = DateTime.Now,
                Status = PrescriptionStatus.Filled,
                Notes = model.Prescription,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Prescriptions.Add(prescription);

            // Add medications
            if (model.Medications != null && model.Medications.Any())
            {
                var medicationNames = model.Medications.Select(m => m.MedicationName.ToLower()).Distinct().ToList();
                var existingMedications = await _context.Medications
                    .Where(m => medicationNames.Contains(m.Name.ToLower()))
                    .ToDictionaryAsync(m => m.Name.ToLower());

                foreach (var med in model.Medications)
                {
                    if (string.IsNullOrWhiteSpace(med.MedicationName)) continue;

                    if (!existingMedications.TryGetValue(med.MedicationName.ToLower(), out var medication))
                    {
                        medication = new Medication { Name = med.MedicationName };
                        _context.Medications.Add(medication);
                    }

                    var prescriptionMedication = new PrescriptionMedication
                    {
                        Prescription = prescription,
                        MedicalRecord = medicalRecord,
                        Medication = medication,
                        Dosage = med.Dosage ?? string.Empty,
                        Instructions = med.Instructions ?? string.Empty,
                        Frequency = med.Frequency ?? string.Empty,
                        Duration = med.Duration ?? string.Empty
                    };
                    _context.PrescriptionMedications.Add(prescriptionMedication);
                }
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation($"Consultation saved for appointment {appointmentId}. MedicalRecord ID: {medicalRecord.Id}, Prescription ID: {prescription.Id}");

            // Prescription email sending removed - prescriptions are no longer sent via email

            return Ok(new { success = true, message = "Consultation and prescription saved!" });
        }
    } // This closing brace for the controller class
} // This closing brace for the namespace