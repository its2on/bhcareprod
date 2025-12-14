using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Barangay.Helpers;
using System.Globalization;

namespace Barangay.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class DoctorsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DoctorsController> _logger;
        private readonly IEncryptionService _encryptionService;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DoctorsController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ILogger<DoctorsController> logger,
            IEncryptionService encryptionService,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _encryptionService = encryptionService;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetDoctors()
        {
            try
            {
                var doctors = await _context.StaffMembers
                    .Where(s => s.Role == "Doctor" && s.IsActive)
                    .Select(s => new DoctorViewModel
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Email = s.Email ?? string.Empty,
                        Specialization = s.Specialization ?? "General Practice",
                        ContactNumber = s.ContactNumber ?? string.Empty,
                        LicenseNumber = s.LicenseNumber ?? string.Empty,
                        WorkingDays = s.WorkingDays ?? "Monday-Friday",
                        WorkingHours = s.WorkingHours ?? "9:00 AM - 5:00 PM"
                    })
                    .ToListAsync();

                return Ok(new { doctors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting doctors list");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableDoctors([FromQuery] string date)
        {
            _logger.LogInformation($"Getting available doctors for date: {date}");
            
            var parsedDate = DateTimeHelper.ParseDate(date);
            if (parsedDate == DateTime.MinValue)
            {
                return BadRequest(new { error = "Invalid date format" });
            }

            try
            {
                // Use ToList() to evaluate the query first, then apply filtering in memory
                var allDoctors = await _context.StaffMembers
                    .Where(d => d.Role == "Doctor" && d.IsActive)
                    .ToListAsync();

                // Filter and map in memory after getting all doctors
                var doctors = allDoctors.Select(d => {
                    // Count appointments for this doctor on the selected date using in-memory filtering
                    var appointmentCount = _context.Appointments
                        .AsEnumerable() // Switch to client evaluation
                        .Count(a => a.DoctorId == d.UserId && 
                               a.AppointmentDate.Date == parsedDate.Date &&
                               a.Status != AppointmentStatus.Cancelled);
                    
                    // Calculate available slots
                    var maxSlots = d.MaxDailyPatients > 0 ? d.MaxDailyPatients : 10;
                    var availableSlots = maxSlots - appointmentCount;
                    
                    return new {
                        id = d.UserId,
                        name = d.Name,
                        specialization = d.Specialization ?? "General Practice",
                        workingHours = d.WorkingHours ?? "09:00-17:00",
                        workingDays = d.WorkingDays ?? "Monday-Friday",
                        availableSlots = availableSlots
                    };
                })
                .Where(d => d.availableSlots > 0)
                .ToList();

                _logger.LogInformation($"Found {doctors.Count} available doctors for {date}");
                
                return Ok(new { doctors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available doctors for date: {Date}", date);
                return StatusCode(500, new { error = "Error retrieving available doctors", details = ex.Message });
            }
        }

        [HttpGet("{doctorId}/timeslots")]
        public async Task<IActionResult> GetAvailableTimeSlots(string doctorId, [FromQuery] string date)
        {
            _logger.LogInformation($"Getting time slots for doctor {doctorId} on date {date}");
            
            var parsedDate = DateTimeHelper.ParseDate(date);
            if (parsedDate == DateTime.MinValue)
            {
                return BadRequest(new { error = "Invalid date format" });
            }

            try
            {
                var doctor = await _context.StaffMembers
                    .FirstOrDefaultAsync(d => d.UserId == doctorId && d.IsActive);

                if (doctor == null)
                {
                    _logger.LogWarning($"Doctor not found with ID: {doctorId}");
                    return NotFound(new { error = "Doctor not found" });
                }

                // Parse working hours
                var workingHours = doctor.WorkingHours ?? "09:00-17:00";
                var hours = workingHours.Split('-');
                
                if (hours.Length != 2)
                {
                    _logger.LogWarning($"Invalid working hours format for doctor {doctorId}: {workingHours}");
                    workingHours = "09:00-17:00";
                    hours = workingHours.Split('-');
                }

                // Get booked appointments for the selected date
                var bookedAppointments = await _context.Appointments
                    .Where(a => a.DoctorId == doctorId)
                    .AsNoTracking() // Improves query performance
                    .ToListAsync();
                
                // Filter in memory
                bookedAppointments = bookedAppointments
                    .Where(a => a.AppointmentDate.Date == parsedDate.Date && 
                           a.Status != AppointmentStatus.Cancelled)
                    .ToList();
                
                var bookedTimes = bookedAppointments.Select(a => a.AppointmentTime).ToList();

                // Generate available time slots
                var availableSlots = new List<string>();
                
                // Try multiple methods to parse time
                TimeSpan startTime;
                TimeSpan endTime;
                
                if (!TryParseTimeWithFormats(hours[0].Trim(), out startTime))
                {
                    _logger.LogWarning($"Could not parse start time: {hours[0]}. Using default 9:00 AM");
                    startTime = new TimeSpan(9, 0, 0); // Default to 9:00 AM
                }
                
                if (!TryParseTimeWithFormats(hours[1].Trim(), out endTime))
                {
                    _logger.LogWarning($"Could not parse end time: {hours[1]}. Using default 5:00 PM");
                    endTime = new TimeSpan(17, 0, 0); // Default to 5:00 PM
                }
                
                var currentSlot = startTime;
                var slotInterval = TimeSpan.FromMinutes(30); // 30-minute slots
                
                while (currentSlot < endTime)
                {
                    // Format as 12-hour time with AM/PM
                    var timeString = DateTime.Today.Add(currentSlot).ToString("hh:mm tt");
                    
                    if (!bookedTimes.Contains(currentSlot))
                    {
                        availableSlots.Add(timeString);
                    }
                    
                    currentSlot = currentSlot.Add(slotInterval);
                }

                _logger.LogInformation($"Found {availableSlots.Count} available time slots for doctor {doctorId} on {date}");
                
                return Ok(new { timeSlots = availableSlots });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting time slots for doctor {DoctorId} on date {Date}", doctorId, date);
                return StatusCode(500, new { error = "Error retrieving time slots", details = ex.Message });
            }
        }

        [HttpGet("{doctorId}/appointments")]
        public async Task<IActionResult> GetDoctorAppointments(string doctorId, [FromQuery] string date)
        {
            if (!DateTime.TryParse(date, out DateTime parsedDate))
                return BadRequest(new { error = "Invalid or missing date parameter" });

            try
            {
                var appointments = await _context.Appointments
                    .Where(a => a.DoctorId == doctorId && a.AppointmentDate.Date == parsedDate.Date)
                    .ToListAsync();

                return Ok(new { appointments });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving doctor appointments");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        
        private bool TryParseTimeWithFormats(string timeString, out TimeSpan result)
        {
            // Clean up the input string
            timeString = timeString?.Trim() ?? string.Empty;
            
            // If the string contains comma, take only the first part
            if (timeString.Contains(","))
                timeString = timeString.Split(',')[0].Trim();
            
            // Try regular TimeSpan parsing first
            if (TimeSpan.TryParse(timeString, out result))
            {
                _logger.LogInformation($"Successfully parsed time '{timeString}' to {result} using TimeSpan.TryParse");
                return true;
            }
            
            // Try various time formats
            string[] formats = { "HH:mm", "H:mm", "h:mm tt", "hh:mm tt" };
            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(timeString, format, 
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                {
                    result = dt.TimeOfDay;
                    _logger.LogInformation($"Successfully parsed time '{timeString}' to {result} using format {format}");
                    return true;
                }
            }
            
            // As a last resort, try generic DateTime parsing
            if (DateTime.TryParse(timeString, out DateTime parsedDateTime))
            {
                result = parsedDateTime.TimeOfDay;
                _logger.LogInformation($"Successfully parsed time '{timeString}' to {result} using DateTime.TryParse");
                return true;
            }
            
            // If all parsing methods fail
            _logger.LogWarning($"Failed to parse time string: '{timeString}'");
            result = TimeSpan.Zero;
            return false;
        }
    }

    public class DoctorViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string WorkingDays { get; set; } = string.Empty;
        public string WorkingHours { get; set; } = string.Empty;
    }
}
