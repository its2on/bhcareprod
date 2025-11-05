using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Barangay.Data;
using Barangay.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Barangay.Extensions;
using Barangay.Services;
using Barangay.Helpers;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Hosting;
using Barangay.Models.Appointments;

namespace Barangay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AppointmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AppointmentController> _logger;
        private readonly IAppointmentService _appointmentService;
        private readonly IWebHostEnvironment _env;
        private readonly IDataEncryptionService _encryptionService;

        public AppointmentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AppointmentController> logger,
            IAppointmentService appointmentService,
            IWebHostEnvironment env,
            IDataEncryptionService encryptionService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _appointmentService = appointmentService;
            _env = env;
            _encryptionService = encryptionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAppointments()
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

            return Ok(appointments);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AppointmentModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var appointmentDate = DateTimeHelper.ParseDate(model.AppointmentDate);
                if (appointmentDate == DateTime.MinValue)
                {
                    return BadRequest("Invalid appointment date format");
                }

                var appointmentTime = DateTimeHelper.ParseTime(model.AppointmentTime);
                if (appointmentTime == TimeSpan.Zero)
                {
                    return BadRequest("Invalid appointment time format");
                }

                // Get the current user ID correctly
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                var appointment = new Appointment
                {
                    DoctorId = model.DoctorId,
                    PatientId = userId,  // Use the actual user ID, not the name
                    PatientName = user.FullName,
                    AppointmentDate = appointmentDate,
                    AppointmentTime = appointmentTime,
                    Description = model.Description,
                    ReasonForVisit = model.Description ?? string.Empty,
                    Status = AppointmentStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Load doctor information to establish the relationship
                var doctor = await _userManager.FindByIdAsync(model.DoctorId);
                if (doctor == null)
                {
                    return BadRequest(new { error = "Doctor not found" });
                }

                // Get the Patient entity for the current user
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
                if (patient == null)
                {
                    // If patient doesn't exist, create a new Patient record
                    _logger.LogWarning($"Patient record not found for user {userId}. Creating a new patient record.");
                    
                    patient = new Patient
                    {
                        UserId = userId,
                        FullName = _encryptionService.Encrypt(user.FullName ?? user.UserName ?? "Unknown"),
                        Email = _encryptionService.Encrypt(user.Email ?? string.Empty),
                        User = user
                    };
                    
                    _context.Patients.Add(patient);
                }

                // Set navigation properties
                appointment.Patient = patient;
                appointment.Doctor = doctor;

                // Add to context and save
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Appointment created successfully: ID={appointment.Id}, Doctor={doctor.Id}, Patient={user.Id}");

                return Ok(new
                {
                    id = appointment.Id,
                    date = appointment.GetFormattedDate(),
                    time = appointment.GetFormattedTime(),
                    status = appointment.Status.ToString(),
                    success = true,
                    message = "Appointment created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating appointment");
                return StatusCode(500, "An error occurred while creating the appointment");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
                if (appointment == null)
                {
                    return NotFound();
                }

                return Ok(new
                {
                    id = appointment.Id,
                    doctorId = appointment.DoctorId,
                    patientId = appointment.PatientId,
                    date = appointment.GetFormattedDate(),
                    time = appointment.GetFormattedTime(),
                    description = appointment.Description,
                    status = appointment.Status.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointment");
                return StatusCode(500, "An error occurred while retrieving the appointment");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAppointment(int id, [FromBody] AppointmentUpdateModel model)
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

            try
            {
                if (!string.IsNullOrEmpty(model.AppointmentDate))
                {
                    appointment.AppointmentDate = DateTimeHelper.ParseDate(model.AppointmentDate);
                }

                if (!string.IsNullOrEmpty(model.AppointmentTime) && 
                    TimeSpan.TryParseExact(model.AppointmentTime, "HH:mm", CultureInfo.InvariantCulture, 
                    TimeSpanStyles.None, out TimeSpan time))
                {
                    appointment.AppointmentTime = time;
                }

                if (!string.IsNullOrEmpty(model.Description))
                {
                    appointment.Description = model.Description;
                }

                if (!string.IsNullOrEmpty(model.Status))
                {
                    if (Enum.TryParse<AppointmentStatus>(model.Status, out AppointmentStatus status))
                    {
                        appointment.Status = status;
                    }
                }

                appointment.UpdatedAt = DateTime.UtcNow;
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

        [HttpPost("book")]
        [Authorize]
        public async Task<IActionResult> BookAppointment([FromForm] AppointmentBookingViewModel model, IFormFile attachment = null)
        {
            try
            {
                // Check if this is a GET request and render the form
                if (HttpContext.Request.Method == "GET")
                {
                    // Load available doctors for the form
                    var doctors = await _userManager.GetUsersInRoleAsync("Doctor");
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var currentUser = await _userManager.FindByIdAsync(currentUserId);

                    // Return a custom response with doctor and user information
                    return Ok(new { 
                        success = true, 
                        doctors = doctors.Select(d => new { d.Id, Name = $"Dr. {d.FirstName} {d.LastName}" }), 
                        user = new { 
                            FullName = currentUser?.FullName ?? "Unknown", 
                            Email = currentUser?.Email, 
                            PhoneNumber = currentUser?.PhoneNumber 
                        } 
                    });
                }

                // Get the logged in user
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                // Get the doctor entity to establish proper relationship
                var doctor = await _userManager.FindByIdAsync(model.DoctorId);
                if (doctor == null)
                {
                    _logger.LogWarning($"Doctor not found with ID: {model.DoctorId}");
                    return BadRequest(new { error = "Doctor not found" });
                }

                // Parse date and time
                if (!DateTime.TryParse(model.Date, out DateTime appointmentDate))
                {
                    _logger.LogWarning($"Invalid date format: {model.Date}");
                    return BadRequest(new { error = "Invalid date format" });
                }

                // Handle different time formats (24-hour or 12-hour with AM/PM)
                TimeSpan appointmentTime;
                if (!TimeSpan.TryParse(model.Time, out appointmentTime))
                {
                    // The time might contain commas if multiple formats were provided
                    string timeToUse = model.Time.Contains(",") ? model.Time.Split(',')[0].Trim() : model.Time;
                    
                    // Try 12-hour format with AM/PM
                    string[] formats = { "h:mm tt", "hh:mm tt", "H:mm", "HH:mm" };
                    if (DateTime.TryParseExact(
                        timeToUse,
                        formats,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime parsedTime))
                    {
                        appointmentTime = parsedTime.TimeOfDay;
                        _logger.LogInformation($"Successfully parsed time '{timeToUse}' to {appointmentTime}");
                    }
                    else
                    {
                        _logger.LogWarning($"Invalid time format: {model.Time}");
                        return BadRequest(new { error = $"Invalid time format: {model.Time}. Please use format like '2:00 PM' or '14:00'" });
                    }
                }

                // Check for conflicts
                var hasConflict = await _context.Appointments
                    .AnyAsync(a => a.DoctorId == model.DoctorId &&
                                 a.AppointmentDate.Date == appointmentDate.Date &&
                                 a.AppointmentTime == appointmentTime &&
                                 a.Status != AppointmentStatus.Cancelled);

                if (hasConflict)
                {
                    _logger.LogWarning($"Time slot is no longer available: Doctor={model.DoctorId}, Date={appointmentDate}, Time={appointmentTime}");
                    return BadRequest(new { error = "Time slot is no longer available" });
                }

                // Create the appointment
                var appointment = new Appointment
                {
                    PatientId = userId,
                    PatientName = user.FullName ?? user.UserName ?? "Unknown", // Use username as fallback
                    DoctorId = model.DoctorId,
                    AppointmentDate = appointmentDate,
                    AppointmentTime = appointmentTime,
                    Description = model.Reason ?? string.Empty,
                    ReasonForVisit = model.Reason ?? string.Empty,
                    AgeValue = model.Age,
                    Status = AppointmentStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Handle file upload if present
                if (attachment != null && attachment.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "appointments");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(attachment.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await attachment.CopyToAsync(stream);
                    }

                    appointment.AttachmentPath = $"/uploads/appointments/{uniqueFileName}";
                }

                // Get the Patient entity for the current user
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
                if (patient == null)
                {
                    // If patient doesn't exist, create a new Patient record
                    _logger.LogWarning($"Patient record not found for user {userId}. Creating a new patient record.");
                    
                    patient = new Patient
                    {
                        UserId = userId,
                        FullName = _encryptionService.Encrypt(user.FullName ?? user.UserName ?? "Unknown"),
                        Email = _encryptionService.Encrypt(user.Email ?? string.Empty),
                        User = user
                    };
                    
                    _context.Patients.Add(patient);
                }

                // Set navigation properties
                appointment.Patient = patient;
                appointment.Doctor = doctor;

                // Save to database with explicit entity tracking
                _context.Entry(user).State = EntityState.Unchanged;
                _context.Entry(doctor).State = EntityState.Unchanged;
                _context.Appointments.Add(appointment);
                
                // Log action before saving
                _logger.LogInformation($"Attempting to save appointment for patient: {userId}, doctor: {model.DoctorId}");
                
                try {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Save succeeded: Appointment ID: {appointment.Id}");
                }
                catch (DbUpdateException dbEx) {
                    _logger.LogError(dbEx, $"Database error saving appointment: {dbEx.InnerException?.Message ?? dbEx.Message}");
                    return StatusCode(500, new { error = "Database error occurred while saving the appointment." });
                }
                catch (Exception ex) {
                    _logger.LogError(ex, $"Error saving appointment to database: {ex.Message}");
                    throw; // Re-throw to be caught by outer try/catch
                }

                // Log success details
                _logger.LogInformation($"Appointment booked successfully: ID={appointment.Id}, " +
                    $"PatientId={appointment.PatientId}, " +
                    $"PatientName={appointment.PatientName}, " +
                    $"DoctorId={appointment.DoctorId}, " +
                    $"DateTime={appointment.AppointmentDate:yyyy-MM-dd} {appointment.GetFormattedTime()}");

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
                return StatusCode(500, new { error = $"An error occurred while booking your appointment: {ex.Message}" });
            }
        }

        [HttpGet("GetAvailableTimeSlots")]
        [Authorize]
        public async Task<IActionResult> GetAvailableTimeSlots(string doctorId, string date)
        {
            try
            {
                if (string.IsNullOrEmpty(doctorId) || string.IsNullOrEmpty(date))
                {
                    return BadRequest(new { error = "Doctor ID and date are required" });
                }

                // Parse the date
                if (!DateTime.TryParse(date, out DateTime appointmentDate))
                {
                    _logger.LogWarning($"Invalid date format: {date}");
                    return BadRequest(new { error = "Invalid date format" });
                }

                // Get the doctor to check their working hours
                var doctor = await _userManager.FindByIdAsync(doctorId);
                if (doctor == null)
                {
                    _logger.LogWarning($"Doctor not found with ID: {doctorId}");
                    return NotFound(new { error = "Doctor not found" });
                }

                // Get the doctor's working days
                var workingDays = doctor.WorkingDays?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => d.Trim())
                    .ToArray();

                // Check if the doctor works on the selected day
                if (workingDays != null && workingDays.Length > 0 && 
                    !workingDays.Contains(appointmentDate.DayOfWeek.ToString()))
                {
                    _logger.LogInformation($"Doctor {doctorId} does not work on {appointmentDate.DayOfWeek}");
                    return Ok(new string[] { }); // Return empty slots
                }

                // Get existing appointments for the doctor on the selected date
                var existingAppointments = await _context.Appointments
                    .Where(a => a.DoctorId == doctorId && 
                           a.AppointmentDate.Date == appointmentDate.Date &&
                           a.Status != AppointmentStatus.Cancelled)
                    .Select(a => a.AppointmentTime)
                    .ToListAsync();

                // Generate time slots based on doctor's schedule (9 AM - 5 PM as default)
                var startTime = new TimeSpan(9, 0, 0); // 9 AM
                var endTime = new TimeSpan(17, 0, 0);  // 5 PM
                var interval = new TimeSpan(0, 30, 0); // 30-minute slots

                // If doctor has custom work hours, try to parse them
                if (!string.IsNullOrEmpty(doctor.WorkingHours))
                {
                    var hoursString = doctor.WorkingHours;
                    string[] hoursParts = hoursString.Split('-');
                    if (hoursParts.Length == 2)
                    {
                        if (TimeSpan.TryParse(hoursParts[0].Trim(), out TimeSpan customStart))
                        {
                            startTime = customStart;
                        }
                        if (TimeSpan.TryParse(hoursParts[1].Trim(), out TimeSpan customEnd))
                        {
                            endTime = customEnd;
                        }
                    }
                }

                // Generate all possible time slots
                var availableSlots = new List<string>();
                for (var time = startTime; time < endTime; time = time.Add(interval))
                {
                    // Skip the slot if it's already booked
                    if (!existingAppointments.Any(a => a == time))
                    {
                        // Format as "1:30 PM" or "9:00 AM"
                        var hour = time.Hours > 12 ? time.Hours - 12 : (time.Hours == 0 ? 12 : time.Hours);
                        var amPm = time.Hours < 12 ? "AM" : "PM";
                        availableSlots.Add($"{hour}:{time.Minutes:00} {amPm}");
                    }
                }

                return Ok(availableSlots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available time slots");
                return StatusCode(500, new { error = "An error occurred while getting available time slots" });
            }
        }

        [HttpGet("GetDoctors")]
        [Authorize]
        public async Task<IActionResult> GetDoctors()
        {
            try
            {
                // Get users in Doctor role
                var doctors = await _userManager.GetUsersInRoleAsync("Doctor");
                
                // Format data for response
                var formattedDoctors = doctors.Select(d => new
                {
                    id = d.Id,
                    firstName = d.FirstName,
                    lastName = d.LastName,
                    specialization = d.Specialization,
                    workingDays = d.WorkingDays,
                    workingHours = d.WorkingHours,
                    maxDailyPatients = d.MaxDailyPatients
                });
                
                return Ok(formattedDoctors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available doctors");
                return StatusCode(500, new { error = "An error occurred while getting available doctors" });
            }
        }

        public class AppointmentBookingViewModel
        {
            public string DoctorId { get; set; }
            public string Date { get; set; }
            public string Time { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
            public string Reason { get; set; }
        }
    }
}
