using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using Barangay.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Barangay.Pages.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    public class AdminDashboardModel : PageModel
    {
        private readonly ILogger<AdminDashboardModel> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDataEncryptionService _encryptionService;

        public AdminDashboardModel(
            ILogger<AdminDashboardModel> logger,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IDataEncryptionService encryptionService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _encryptionService = encryptionService;
        }

        public int TotalDoctors { get; set; }
        public int TotalNurses { get; set; }
        public int PendingAccountsCount { get; set; }
        public int ActiveStaffCount { get; set; }
        public List<StaffOverviewModel> RecentStaff { get; set; }
        public DateTime CurrentPhilippineTime { get; set; }

        private DateTime GetPhilippineTime()
        {
            try
            {
                // Get the current time in the system's local timezone
                var currentDateTime = DateTime.Now;
                
                // For PST timezone (Pacific Standard Time)
                // You can use either a TimeZoneInfo or just return the current DateTime
                // since this is primarily for display purposes
                
                // Uncomment below if you need specific timezone conversion
                /*
                TimeZoneInfo pstZone = null;
                try
                {
                    // Try Windows timezone ID first
                    pstZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
                }
                catch
                {
                    try
                    {
                        // Try IANA timezone ID for Linux systems
                        pstZone = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
                    }
                    catch
                    {
                        // Fallback - just return current time if timezone not found
                        _logger.LogWarning("Could not find PST timezone, using system local time");
                    }
                }
                
                if (pstZone != null)
                {
                    return TimeZoneInfo.ConvertTime(DateTime.Now, pstZone);
                }
                */
                
                return currentDateTime;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current time");
                // Return current time as fallback
                return DateTime.Now;
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Set current time - this will update on each page load
                CurrentPhilippineTime = GetPhilippineTime();

                // Initialize collections
                RecentStaff = new List<StaffOverviewModel>();
                
                try
                {
                    // Get total counts - wrap in try/catch as SqlNullValueException is occurring here
                    var doctors = await _userManager.GetUsersInRoleAsync("Doctor");
                    var nurses = await _userManager.GetUsersInRoleAsync("Nurse");
                    var headNurses = await _userManager.GetUsersInRoleAsync("Head Nurse");
                    
                    TotalDoctors = doctors.Count;
                    TotalNurses = nurses.Count + headNurses.Count;
                    
                    // Calculate active staff count
                    ActiveStaffCount = doctors.Count(d => d.IsActive) + nurses.Count(n => n.IsActive) + headNurses.Count(hn => hn.IsActive);

                    // Get all staff members (doctors, nurses, admins)
                    var staffRoles = new[] { "Doctor", "Nurse", "Head Nurse", "Admin", "Staff", "System Administrator" };
                    
                    // Get users with staff roles
                    foreach (var role in staffRoles)
                    {
                        var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                        foreach (var user in usersInRole)
                        {
                            // Skip if already added (user might have multiple roles)
                            if (RecentStaff.Any(s => s.Id == user.Id))
                                continue;

                            // Decrypt user data for authorized users
                            var decryptedUser = user.DecryptSensitiveData(_encryptionService, User);
                            
                            // Manually decrypt PhoneNumber since it's not marked with [Encrypted] attribute
                            if (!string.IsNullOrEmpty(decryptedUser.PhoneNumber) && _encryptionService.IsEncrypted(decryptedUser.PhoneNumber))
                            {
                                decryptedUser.PhoneNumber = decryptedUser.PhoneNumber.DecryptForUser(_encryptionService, User);
                            }
                                
                            RecentStaff.Add(new StaffOverviewModel
                            {
                                Id = decryptedUser.Id,
                                Name = !string.IsNullOrEmpty(decryptedUser.FirstName) 
                                    ? (!string.IsNullOrEmpty(decryptedUser.LastName) 
                                        ? $"{decryptedUser.FirstName} {decryptedUser.LastName}" 
                                        : decryptedUser.FirstName)
                                    : decryptedUser.UserName ?? "Unknown",
                                Role = role,
                                // Department field removed
                                IsActive = decryptedUser.IsActive,
                                LastActive = decryptedUser.LastActive
                            });
                        }
                    }
                    
                    // Sort by last active time
                    RecentStaff = RecentStaff
                        .OrderByDescending(s => s.LastActive)
                        .Take(10)
                        .ToList();
                }
                catch (System.Data.SqlTypes.SqlNullValueException ex)
                {
                    _logger.LogError(ex, "SQL null value exception when retrieving users from roles");
                    TempData["ErrorMessage"] = "Some staff data could not be loaded due to database issues.";
                    // Continue with whatever data we have
                    TotalDoctors = 0;
                    TotalNurses = 0;
                    ActiveStaffCount = 0;
                }
                
                // Get pending accounts count - place outside the inner try/catch so it still runs
                try 
                {
                    // Get the IDs of users with special roles to exclude (Admin, Doctor, Nurse, etc.)
                    var excludedRoleNames = new[] { "Admin", "System Administrator", "Admin Staff", "System Admin", "Staff Admin", "Doctor", "Nurse", "Head Nurse", "Head Doctor" };
                    var excludedRoles = await _context.Roles
                        .Where(r => excludedRoleNames.Contains(r.Name))
                        .ToListAsync();
                        
                    var excludedUserIds = await _context.UserRoles
                        .Where(ur => excludedRoles.Select(r => r.Id).Contains(ur.RoleId))
                        .Select(ur => ur.UserId)
                        .ToListAsync();

                    // Count pending users (patients only, exclude staff)
                    PendingAccountsCount = await _userManager.Users
                        .Where(u => u.Status.ToLower() == "pending" && !excludedUserIds.Contains(u.Id))
                        .CountAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading pending accounts count");
                    PendingAccountsCount = 0;
                }

                _logger.LogInformation($"Admin dashboard loaded successfully by user {User.Identity?.Name}");
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                TempData["ErrorMessage"] = "Error loading dashboard data. Please try again later.";
                return Page();
            }
        }
    }

    public class StaffOverviewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        // Department field removed
        public bool IsActive { get; set; }
        public DateTime LastActive { get; set; }
    }
}
        
