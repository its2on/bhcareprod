using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Barangay.Models;
using Barangay.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;

namespace Barangay.Pages.User
{
    [Authorize]
    public class NotificationsModel : PageModel
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public NotificationsModel(
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _notificationService = notificationService;
            _userManager = userManager;
            _context = context;
        }

        public List<Notification> Notifications { get; set; } = new List<Notification>();
        public int UnreadCount { get; set; }
        public ApplicationUser CurrentUser { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            CurrentUser = await _userManager.GetUserAsync(User);
            if (CurrentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Get all notifications for the current user (both read and unread)
            Notifications = await _context.Notifications
                .Where(n => n.UserId == CurrentUser.Id || n.RecipientId == CurrentUser.Id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            UnreadCount = Notifications.Count(n => !n.IsRead);

            ViewData["Title"] = "Notifications";
            ViewData["ShowDashboardNav"] = true;
            
            return Page();
        }

        public async Task<IActionResult> OnPostMarkAsReadAsync(int id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostMarkAllAsReadAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _notificationService.MarkAllAsReadAsync(user.Id);
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                var user = await _userManager.GetUserAsync(User);
                // Only allow users to delete their own notifications
                if (user != null && (notification.UserId == user.Id || notification.RecipientId == user.Id))
                {
                    _context.Notifications.Remove(notification);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAllReadAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                var readNotifications = await _context.Notifications
                    .Where(n => (n.UserId == user.Id || n.RecipientId == user.Id) && n.IsRead)
                    .ToListAsync();
                    
                _context.Notifications.RemoveRange(readNotifications);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}
