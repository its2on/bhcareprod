using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;

namespace Barangay.Pages
{
    public class QuickFixModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuickFixModel> _logger;

        public QuickFixModel(ApplicationDbContext context, ILogger<QuickFixModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<DoctorStatusViewModel> Doctors { get; set; } = new();
        public string StatusMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public class DoctorStatusViewModel
        {
            public string DoctorId { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public bool Saturday { get; set; }
            public bool Sunday { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public bool IsAvailable { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDoctorStatus();
            return Page();
        }

        public async Task<IActionResult> OnPostFixWeekendsAsync()
        {
            try
            {
                // Get all doctors
                var doctors = await _context.Users
                    .Where(u => _context.UserRoles
                        .Any(ur => ur.UserId == u.Id && 
                                   _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Doctor")))
                    .ToListAsync();

                var updatedCount = 0;
                var createdCount = 0;

                foreach (var doctor in doctors)
                {
                    // Check if DoctorAvailability exists
                    var availability = await _context.DoctorAvailabilities
                        .FirstOrDefaultAsync(da => da.DoctorId == doctor.Id);

                    if (availability == null)
                    {
                        // Create new availability with weekend support
                        availability = new DoctorAvailability
                        {
                            DoctorId = doctor.Id,
                            IsAvailable = true,
                            Monday = true,
                            Tuesday = true,
                            Wednesday = true,
                            Thursday = true,
                            Friday = true,
                            Saturday = true,  // ENABLE WEEKENDS
                            Sunday = true,    // ENABLE WEEKENDS
                            StartTime = new TimeSpan(8, 0, 0), // 8:00 AM
                            EndTime = new TimeSpan(17, 0, 0),  // 5:00 PM
                            LastUpdated = DateTime.Now
                        };

                        _context.DoctorAvailabilities.Add(availability);
                        createdCount++;
                    }
                    else
                    {
                        // Update existing availability
                        availability.Saturday = true;  // ENABLE WEEKENDS
                        availability.Sunday = true;    // ENABLE WEEKENDS
                        availability.IsAvailable = true;
                        availability.StartTime = new TimeSpan(8, 0, 0);
                        availability.EndTime = new TimeSpan(17, 0, 0);
                        availability.LastUpdated = DateTime.Now;
                        updatedCount++;
                    }
                }

                await _context.SaveChangesAsync();

                StatusMessage = $" SUCCESS! Fixed weekend appointments for {doctors.Count} doctors! " +
                              $"Updated {updatedCount} existing records and created {createdCount} new records. " +
                              $"Now try booking a weekend 'Libreng Tuli' appointment - it should work! 🏥";

                _logger.LogInformation($"Fixed weekend appointments: {updatedCount} updated, {createdCount} created");
            }
            catch (Exception ex)
            {
                ErrorMessage = $" Error fixing weekend appointments: {ex.Message}";
                _logger.LogError(ex, "Error fixing weekend appointments");
            }

            await LoadDoctorStatus();
            return Page();
        }

        private async Task LoadDoctorStatus()
        {
            try
            {
                // Get all doctors with their availability
                var doctors = await _context.Users
                    .Where(u => _context.UserRoles
                        .Any(ur => ur.UserId == u.Id && 
                                   _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Doctor")))
                    .ToListAsync();

                Doctors = new List<DoctorStatusViewModel>();

                foreach (var doctor in doctors)
                {
                    var availability = await _context.DoctorAvailabilities
                        .FirstOrDefaultAsync(da => da.DoctorId == doctor.Id);

                    Doctors.Add(new DoctorStatusViewModel
                    {
                        DoctorId = doctor.Id,
                        Name = doctor.UserName ?? "Unknown",
                        Email = doctor.Email ?? "Unknown",
                        Saturday = availability?.Saturday ?? false,
                        Sunday = availability?.Sunday ?? false,
                        StartTime = availability?.StartTime ?? new TimeSpan(9, 0, 0),
                        EndTime = availability?.EndTime ?? new TimeSpan(17, 0, 0),
                        IsAvailable = availability?.IsAvailable ?? false
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading doctor status");
                ErrorMessage = $"Error loading data: {ex.Message}";
            }
        }
    }
}
