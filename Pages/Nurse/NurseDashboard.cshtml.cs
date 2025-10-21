using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Barangay.Models;
using Barangay.Helpers;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Barangay.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace Barangay.Pages.Nurse
{
    [Authorize(Roles = "Nurse,Head Nurse")]
    [Authorize(Policy = "NurseDashboard")]
    public class NurseDashboardModel : PageModel
    {
        private readonly Data.ApplicationDbContext _context;
        private readonly IPermissionService _permissionService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NurseDashboardModel(Data.ApplicationDbContext context, IPermissionService permissionService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _permissionService = permissionService;
            _userManager = userManager;

            // Initialize properties
            ErrorMessage = string.Empty;
            
            // Initialize collections
            Alerts = new List<AlertViewModel>();
            Queue = new List<QueueViewModel>();
            PatientRecords = new List<PatientRecordViewModel>();
        }
        
        public string ErrorMessage { get; set; }
        public int TodaysTotalPatients { get; set; }
        public int InProgressCount { get; set; }
        public int WaitingCount { get; set; }
        public int CompletedTodayCount { get; set; }
        public List<AlertViewModel> Alerts { get; set; }
        public List<QueueViewModel> Queue { get; set; }
        public List<PatientRecordViewModel> PatientRecords { get; set; }
        public List<NotificationItem> RecentNotifications { get; set; } = new();
        
        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Get current user
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToPage("/Account/Login");
                }
                
                // Check if user has never changed their password - show notification
                ViewData["ShowFirstLoginNotification"] = !user.HasChangedPassword;
                
                // Load actual dashboard data from database
                await LoadDashboardDataAsync();

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading dashboard: {ex.Message}";
                return Page();
            }
        }
        
        private async Task LoadDashboardDataAsync()
        {
            var today = DateTimeHelper.Today;

            // Exclude system administrator from query
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "admin@example.com");
            var adminId = adminUser?.Id ?? "";

            var todaysAppointments = _context.Appointments
                .Where(a => a.AppointmentDate.Date == today && a.PatientId != adminId);

            TodaysTotalPatients = await todaysAppointments.Select(a => a.PatientId).Distinct().CountAsync();
            InProgressCount = await todaysAppointments.CountAsync(a => a.Status == Models.AppointmentStatus.InProgress);
            WaitingCount = await todaysAppointments.CountAsync(a => a.Status == Models.AppointmentStatus.Pending || a.Status == Models.AppointmentStatus.Confirmed);
            CompletedTodayCount = await todaysAppointments.CountAsync(a => a.Status == Models.AppointmentStatus.Completed);
            
            // Fetch recent notifications from the database
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var notifications = await _context.Notifications
                                                  .Where(n => n.RecipientId == userId && !n.IsRead)
                                                  .OrderByDescending(n => n.CreatedAt)
                                                  .Take(5)
                                                  .ToListAsync();

                RecentNotifications = notifications.Select(n => new NotificationItem
                {
                    Title = n.Title,
                    Message = n.Message,
                    CreatedAt = n.CreatedAt
                }).ToList();
            }
        }

        public class AlertViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
            public string PatientName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }
        
        public class QueueViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string WaitingTime { get; set; } = string.Empty;
            public string Priority { get; set; } = string.Empty;
            public string PatientName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        public class PatientRecordViewModel
        {
            public int Id { get; set; }
            public string PatientId { get; set; } = string.Empty;
            public string RecordNo { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
            public string Gender { get; set; } = string.Empty;
            public DateTime LastVisit { get; set; }
            public string Diagnosis { get; set; } = string.Empty;
        }
        
        public class NotificationItem
        {
            public string Title { get; set; }
            public string Message { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}
