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
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Barangay.Pages.Nurse
{
    [Authorize(Roles = "Nurse,Head Nurse")]
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
        public string SelectedDoctorId { get; set; } = string.Empty;
        public string SelectedDoctorName { get; set; } = string.Empty;
        public List<ApplicationUser> Doctors { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string doctorId = null)
        {
            // Get all doctors for the dropdown
            var doctorsList = await _userManager.GetUsersInRoleAsync("Doctor");
            Doctors = doctorsList.ToList();

            // If no doctor is selected, default to the first doctor
            if (string.IsNullOrEmpty(doctorId) && Doctors.Any())
            {
                doctorId = Doctors.First().Id;
            }

            if (string.IsNullOrEmpty(doctorId))
            {
                Message = "No doctors found in the system.";
                IsSuccess = false;
                return Page();
            }

            SelectedDoctorId = doctorId;
            var selectedDoctor = Doctors.FirstOrDefault(d => d.Id == doctorId);
            SelectedDoctorName = selectedDoctor?.FullName ?? "Unknown Doctor";

            var availability = await _context.DoctorAvailabilities
                .FirstOrDefaultAsync(da => da.DoctorId == doctorId);

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
                    MaxAppointmentsPerDay = 50,
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

        public async Task<IActionResult> OnPostAsync(string doctorId)
        {
            if (!ModelState.IsValid)
            {
                IsSuccess = false;
                Message = "Please correct the errors and try again.";
                
                // Reload doctors list
                var doctorsList = await _userManager.GetUsersInRoleAsync("Doctor");
                Doctors = doctorsList.ToList();
                SelectedDoctorId = doctorId;
                var selectedDoctor = Doctors.FirstOrDefault(d => d.Id == doctorId);
                SelectedDoctorName = selectedDoctor?.FullName ?? "Unknown Doctor";
                
                return Page();
            }

            try
            {
                if (string.IsNullOrEmpty(doctorId))
                {
                    ModelState.AddModelError("", "Doctor selection is required");
                    IsSuccess = false;
                    Message = "Please select a doctor.";
                    
                    var doctorsList = await _userManager.GetUsersInRoleAsync("Doctor");
                    Doctors = doctorsList.ToList();
                    return Page();
                }

                // Parse times
                if (!TimeSpan.TryParse(Settings.StartTime, out TimeSpan startTime))
                {
                    ModelState.AddModelError("Settings.StartTime", "Invalid start time format");
                    
                    var doctorsList = await _userManager.GetUsersInRoleAsync("Doctor");
                    Doctors = doctorsList.ToList();
                    SelectedDoctorId = doctorId;
                    var selectedDoctor = Doctors.FirstOrDefault(d => d.Id == doctorId);
                    SelectedDoctorName = selectedDoctor?.FullName ?? "Unknown Doctor";
                    
                    return Page();
                }

                if (!TimeSpan.TryParse(Settings.EndTime, out TimeSpan endTime))
                {
                    ModelState.AddModelError("Settings.EndTime", "Invalid end time format");
                    
                    var doctorsList = await _userManager.GetUsersInRoleAsync("Doctor");
                    Doctors = doctorsList.ToList();
                    SelectedDoctorId = doctorId;
                    var selectedDoctor2 = Doctors.FirstOrDefault(d => d.Id == doctorId);
                    SelectedDoctorName = selectedDoctor2?.FullName ?? "Unknown Doctor";
                    
                    return Page();
                }

                if (endTime <= startTime)
                {
                    ModelState.AddModelError("Settings.EndTime", "End time must be after start time");
                    
                    var doctorsList = await _userManager.GetUsersInRoleAsync("Doctor");
                    Doctors = doctorsList.ToList();
                    SelectedDoctorId = doctorId;
                    var selectedDoctor3 = Doctors.FirstOrDefault(d => d.Id == doctorId);
                    SelectedDoctorName = selectedDoctor3?.FullName ?? "Unknown Doctor";
                    
                    return Page();
                }

                var availability = await _context.DoctorAvailabilities
                    .FirstOrDefaultAsync(da => da.DoctorId == doctorId);

                if (availability == null)
                {
                    // Create new availability
                    availability = new DoctorAvailability
                    {
                        DoctorId = doctorId,
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
                    _logger.LogInformation($"Created new availability settings for doctor {doctorId} by nurse {User.Identity?.Name}");
                }
                else
                {
                    // Update existing availability
                    _logger.LogInformation($"[Nurse] Updating availability for doctor {doctorId} - Old MaxSlots: {availability.MaxAppointmentsPerDay}, New MaxSlots: {Settings.MaxAppointmentsPerDay}");
                    
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

                    // Mark as modified to ensure EF Core tracks the change
                    _context.Entry(availability).State = EntityState.Modified;

                    _logger.LogInformation($"[Nurse] Updated availability settings for doctor {doctorId} by nurse {User.Identity?.Name} - SlotDuration: {availability.SlotDurationMinutes} minutes");
                }

                var savedChanges = await _context.SaveChangesAsync();
                _logger.LogInformation($"[Nurse] SaveChangesAsync completed - {savedChanges} entities saved");

                IsSuccess = true;
                Message = $"Settings saved successfully! Each appointment slot will be approximately {availability.SlotDurationMinutes} minutes. (Max: {availability.MaxAppointmentsPerDay} slots/day)";

                // Reload page data
                return RedirectToPage(new { doctorId = doctorId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving appointment settings");
                IsSuccess = false;
                Message = "An error occurred while saving settings. Please try again.";
                
                var doctorsList = await _userManager.GetUsersInRoleAsync("Doctor");
                Doctors = doctorsList.ToList();
                SelectedDoctorId = doctorId;
                var selectedDoctor4 = Doctors.FirstOrDefault(d => d.Id == doctorId);
                SelectedDoctorName = selectedDoctor4?.FullName ?? "Unknown Doctor";
                
                return Page();
            }
        }
    }

    public class DoctorAvailabilitySettings
    {
        [Required]
        [Range(1, 200, ErrorMessage = "Max appointments per day must be between 1 and 200")]
        public int MaxAppointmentsPerDay { get; set; } = 50;

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
