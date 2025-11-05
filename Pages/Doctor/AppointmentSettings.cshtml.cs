using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Barangay.Data;
using Barangay.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Barangay.Pages.Doctor
{
    [Authorize(Roles = "Doctor")]
    public class AppointmentSettingsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AppointmentSettingsModel> _logger;

        public AppointmentSettingsModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AppointmentSettingsModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public DoctorAvailabilitySettings Settings { get; set; } = new();

        public string Message { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var availability = await _context.DoctorAvailabilities
                .FirstOrDefaultAsync(da => da.DoctorId == user.Id);

            if (availability != null)
            {
                Settings = new DoctorAvailabilitySettings
                {
                    MaxAppointmentsPerDay = availability.MaxAppointmentsPerDay,
                    StartTime = availability.StartTime.ToString(@"hh\:mm"),
                    EndTime = availability.EndTime.ToString(@"hh\:mm"),
                    Monday = availability.Monday,
                    Tuesday = availability.Tuesday,
                    Wednesday = availability.Wednesday,
                    Thursday = availability.Thursday,
                    Friday = availability.Friday,
                    Saturday = availability.Saturday,
                    Sunday = availability.Sunday,
                    IsAvailable = availability.IsAvailable
                };
            }
            else
            {
                // Default settings
                Settings = new DoctorAvailabilitySettings
                {
                    MaxAppointmentsPerDay = 30,
                    StartTime = "08:00",
                    EndTime = "17:00",
                    Monday = true,
                    Tuesday = true,
                    Wednesday = true,
                    Thursday = true,
                    Friday = true,
                    Saturday = false,
                    Sunday = false,
                    IsAvailable = true
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                IsSuccess = false;
                Message = "Please correct the errors and try again.";
                return Page();
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                // Parse times
                if (!TimeSpan.TryParse(Settings.StartTime, out TimeSpan startTime))
                {
                    ModelState.AddModelError("Settings.StartTime", "Invalid start time format");
                    return Page();
                }

                if (!TimeSpan.TryParse(Settings.EndTime, out TimeSpan endTime))
                {
                    ModelState.AddModelError("Settings.EndTime", "Invalid end time format");
                    return Page();
                }

                if (endTime <= startTime)
                {
                    ModelState.AddModelError("Settings.EndTime", "End time must be after start time");
                    return Page();
                }

                var availability = await _context.DoctorAvailabilities
                    .FirstOrDefaultAsync(da => da.DoctorId == user.Id);

                if (availability == null)
                {
                    // Create new availability
                    availability = new DoctorAvailability
                    {
                        DoctorId = user.Id,
                        IsAvailable = Settings.IsAvailable,
                        MaxAppointmentsPerDay = Settings.MaxAppointmentsPerDay,
                        StartTime = startTime,
                        EndTime = endTime,
                        Monday = Settings.Monday,
                        Tuesday = Settings.Tuesday,
                        Wednesday = Settings.Wednesday,
                        Thursday = Settings.Thursday,
                        Friday = Settings.Friday,
                        Saturday = Settings.Saturday,
                        Sunday = Settings.Sunday,
                        LastUpdated = DateTime.UtcNow
                    };

                    // Calculate slot duration
                    availability.CalculateSlotDuration();

                    _context.DoctorAvailabilities.Add(availability);
                    _logger.LogInformation($"Created new availability settings for doctor {user.Id}");
                }
                else
                {
                    // Update existing availability
                    availability.IsAvailable = Settings.IsAvailable;
                    availability.MaxAppointmentsPerDay = Settings.MaxAppointmentsPerDay;
                    availability.StartTime = startTime;
                    availability.EndTime = endTime;
                    availability.Monday = Settings.Monday;
                    availability.Tuesday = Settings.Tuesday;
                    availability.Wednesday = Settings.Wednesday;
                    availability.Thursday = Settings.Thursday;
                    availability.Friday = Settings.Friday;
                    availability.Saturday = Settings.Saturday;
                    availability.Sunday = Settings.Sunday;
                    availability.LastUpdated = DateTime.UtcNow;

                    // Recalculate slot duration
                    availability.CalculateSlotDuration();

                    _logger.LogInformation($"Updated availability settings for doctor {user.Id}");
                }

                await _context.SaveChangesAsync();

                IsSuccess = true;
                Message = $"Settings saved successfully! Each appointment slot will be approximately {availability.SlotDurationMinutes} minutes.";

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving appointment settings");
                IsSuccess = false;
                Message = "An error occurred while saving settings. Please try again.";
                return Page();
            }
        }
    }

    public class DoctorAvailabilitySettings
    {
        [Required]
        [Range(1, 100, ErrorMessage = "Max appointments per day must be between 1 and 100")]
        public int MaxAppointmentsPerDay { get; set; } = 30;

        [Required]
        public string StartTime { get; set; } = "08:00";

        [Required]
        public string EndTime { get; set; } = "17:00";

        public bool Monday { get; set; } = true;
        public bool Tuesday { get; set; } = true;
        public bool Wednesday { get; set; } = true;
        public bool Thursday { get; set; } = true;
        public bool Friday { get; set; } = true;
        public bool Saturday { get; set; } = false;
        public bool Sunday { get; set; } = false;

        public bool IsAvailable { get; set; } = true;

        public int CalculatedSlotDuration
        {
            get
            {
                if (TimeSpan.TryParse(StartTime, out TimeSpan start) && TimeSpan.TryParse(EndTime, out TimeSpan end))
                {
                    var workingMinutes = (int)(end - start).TotalMinutes;
                    return MaxAppointmentsPerDay > 0 ? workingMinutes / MaxAppointmentsPerDay : 0;
                }
                return 0;
            }
        }
    }
}

