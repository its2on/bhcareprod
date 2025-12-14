using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;

namespace Barangay.Pages.Admin
{
    public class FixWeekendAppointmentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FixWeekendAppointmentsModel> _logger;

        public FixWeekendAppointmentsModel(ApplicationDbContext context, ILogger<FixWeekendAppointmentsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<DoctorAvailabilityViewModel> DoctorAvailabilities { get; set; } = new();
        public string StatusMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public class DoctorAvailabilityViewModel
        {
            public string DoctorId { get; set; } = string.Empty;
            public string DoctorName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public bool Monday { get; set; }
            public bool Tuesday { get; set; }
            public bool Wednesday { get; set; }
            public bool Thursday { get; set; }
            public bool Friday { get; set; }
            public bool Saturday { get; set; }
            public bool Sunday { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public bool IsAvailable { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDoctorAvailabilities();
            return Page();
        }

        public async Task<IActionResult> OnPostEnableWeekendsAsync()
        {
            try
            {
                var updatedCount = 0;
                
                // Update all existing DoctorAvailability records
                var existingRecords = await _context.DoctorAvailabilities.ToListAsync();
                foreach (var record in existingRecords)
                {
                    record.Saturday = true;
                    record.Sunday = true;
                    record.IsAvailable = true;
                    record.StartTime = new TimeSpan(8, 0, 0);
                    record.EndTime = new TimeSpan(17, 0, 0);
                    record.LastUpdated = DateTime.Now;
                    updatedCount++;
                }

                // Create records for doctors who don't have them
                var doctors = await _context.Users
                    .Where(u => _context.UserRoles
                        .Any(ur => ur.UserId == u.Id && 
                                   _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Doctor")))
                    .ToListAsync();

                var createdCount = 0;
                foreach (var doctor in doctors)
                {
                    var existingAvailability = await _context.DoctorAvailabilities
                        .FirstOrDefaultAsync(da => da.DoctorId == doctor.Id);
                    
                    if (existingAvailability == null)
                    {
                        var newAvailability = new DoctorAvailability
                        {
                            DoctorId = doctor.Id,
                            IsAvailable = true,
                            Monday = true,
                            Tuesday = true,
                            Wednesday = true,
                            Thursday = true,
                            Friday = true,
                            Saturday = true,
                            Sunday = true,
                            StartTime = new TimeSpan(8, 0, 0),
                            EndTime = new TimeSpan(17, 0, 0),
                            LastUpdated = DateTime.Now
                        };
                        
                        _context.DoctorAvailabilities.Add(newAvailability);
                        createdCount++;
                    }
                }

                await _context.SaveChangesAsync();
                
                StatusMessage = $" Successfully enabled weekend appointments! Updated {updatedCount} existing records and created {createdCount} new records.";
                _logger.LogInformation($"Enabled weekend appointments: {updatedCount} updated, {createdCount} created");
            }
            catch (Exception ex)
            {
                ErrorMessage = $" Error enabling weekend appointments: {ex.Message}";
                _logger.LogError(ex, "Error enabling weekend appointments");
            }

            await LoadDoctorAvailabilities();
            return Page();
        }

        public async Task<IActionResult> OnPostCreateMissingAsync()
        {
            try
            {
                var doctors = await _context.Users
                    .Where(u => _context.UserRoles
                        .Any(ur => ur.UserId == u.Id && 
                                   _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Doctor")))
                    .ToListAsync();

                var createdCount = 0;
                foreach (var doctor in doctors)
                {
                    var existingAvailability = await _context.DoctorAvailabilities
                        .FirstOrDefaultAsync(da => da.DoctorId == doctor.Id);
                    
                    if (existingAvailability == null)
                    {
                        var newAvailability = new DoctorAvailability
                        {
                            DoctorId = doctor.Id,
                            IsAvailable = true,
                            Monday = true,
                            Tuesday = true,
                            Wednesday = true,
                            Thursday = true,
                            Friday = true,
                            Saturday = true,
                            Sunday = true,
                            StartTime = new TimeSpan(8, 0, 0),
                            EndTime = new TimeSpan(17, 0, 0),
                            LastUpdated = DateTime.Now
                        };
                        
                        _context.DoctorAvailabilities.Add(newAvailability);
                        createdCount++;
                    }
                }

                await _context.SaveChangesAsync();
                
                StatusMessage = $" Created {createdCount} missing DoctorAvailability records with weekend support enabled.";
                _logger.LogInformation($"Created {createdCount} missing DoctorAvailability records");
            }
            catch (Exception ex)
            {
                ErrorMessage = $" Error creating missing records: {ex.Message}";
                _logger.LogError(ex, "Error creating missing DoctorAvailability records");
            }

            await LoadDoctorAvailabilities();
            return Page();
        }

        public async Task<IActionResult> OnPostResetAllAsync()
        {
            try
            {
                // Delete all existing records
                var existingRecords = await _context.DoctorAvailabilities.ToListAsync();
                _context.DoctorAvailabilities.RemoveRange(existingRecords);

                // Create new records for all doctors
                var doctors = await _context.Users
                    .Where(u => _context.UserRoles
                        .Any(ur => ur.UserId == u.Id && 
                                   _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Doctor")))
                    .ToListAsync();

                foreach (var doctor in doctors)
                {
                    var newAvailability = new DoctorAvailability
                    {
                        DoctorId = doctor.Id,
                        IsAvailable = true,
                        Monday = true,
                        Tuesday = true,
                        Wednesday = true,
                        Thursday = true,
                        Friday = true,
                        Saturday = true,
                        Sunday = true,
                        StartTime = new TimeSpan(8, 0, 0),
                        EndTime = new TimeSpan(17, 0, 0),
                        LastUpdated = DateTime.Now
                    };
                    
                    _context.DoctorAvailabilities.Add(newAvailability);
                }

                await _context.SaveChangesAsync();
                
                StatusMessage = $" Reset all DoctorAvailability records for {doctors.Count} doctors with weekend support enabled.";
                _logger.LogInformation($"Reset all DoctorAvailability records for {doctors.Count} doctors");
            }
            catch (Exception ex)
            {
                ErrorMessage = $" Error resetting records: {ex.Message}";
                _logger.LogError(ex, "Error resetting DoctorAvailability records");
            }

            await LoadDoctorAvailabilities();
            return Page();
        }

        private async Task LoadDoctorAvailabilities()
        {
            try
            {
                var availabilities = await _context.DoctorAvailabilities
                    .Include(da => da.Doctor)
                    .ToListAsync();

                DoctorAvailabilities = availabilities.Select(da => new DoctorAvailabilityViewModel
                {
                    DoctorId = da.DoctorId,
                    DoctorName = da.Doctor?.FullName ?? "Unknown",
                    Email = da.Doctor?.Email ?? "Unknown",
                    Monday = da.Monday,
                    Tuesday = da.Tuesday,
                    Wednesday = da.Wednesday,
                    Thursday = da.Thursday,
                    Friday = da.Friday,
                    Saturday = da.Saturday,
                    Sunday = da.Sunday,
                    StartTime = da.StartTime,
                    EndTime = da.EndTime,
                    IsAvailable = da.IsAvailable
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading doctor availabilities");
                ErrorMessage = $"Error loading data: {ex.Message}";
            }
        }
    }
}
