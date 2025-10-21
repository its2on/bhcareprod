using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Barangay.Data;
using Barangay.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Barangay.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string title, string message, string type, string link = null, string recipientId = null);
        Task CreateNotificationForUserAsync(string userId, string title, string message, string type, string link = null);
        Task<List<Notification>> GetUnreadNotificationsAsync(string userId = null);
        Task<int> GetUnreadNotificationCountAsync(string userId = null);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(string userId = null);
    }
    
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificationService> _logger;
        
        public NotificationService(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ILogger<NotificationService> logger = null)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }
        
        public async Task CreateNotificationAsync(string title, string message, string type, string link = null, string recipientId = null)
        {
            try
            {
                var notification = new Notification
                {
                    Title = title,
                    Message = message,
                    Type = type,
                    Link = link ?? "/Index", // Use default value if link is null
                    UserId = recipientId, // Set UserId to recipientId to satisfy NOT NULL constraint
                    RecipientId = recipientId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating notification");
            }
        }
        
        public async Task CreateNotificationForUserAsync(string userId, string title, string message, string type, string link = null)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    _logger?.LogWarning("Cannot create notification: userId is null or empty");
                    return;
                }
                
                // Verify if the user exists
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger?.LogWarning($"Cannot create notification: user with ID {userId} not found");
                    return;
                }
                
                _logger?.LogInformation($"Creating notification for user {user.Email}: {title}");
                
                // This method is specifically for creating notifications for a specific user
                await CreateNotificationAsync(title, message, type, link, userId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error creating notification for user {userId}: {ex.Message}");
            }
        }
        
        public async Task<List<Notification>> GetUnreadNotificationsAsync(string userId = null)
        {
            try
            {
                // Use defensive SQL query approach to avoid issues with missing columns
                var query = _context.Notifications
                    .AsNoTracking() // Use AsNoTracking for better performance on read-only data
                    .Select(n => new Notification
                    {
                        Id = n.Id,
                        Title = n.Title,
                        Message = n.Message,
                        Type = n.Type,
                        Link = n.Link,
                        UserId = n.UserId,
                        RecipientId = n.RecipientId,
                        CreatedAt = n.CreatedAt,
                        IsRead = n.IsRead
                    })
                    .AsQueryable();
                
                // If userId is provided, get notifications for that user or all admins
                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(n => n.RecipientId == userId || n.RecipientId == null);
                }
                else
                {
                    // For admin users, get all notifications
                    query = query.Where(n => n.RecipientId == null);
                }
                
                // Get only unread notifications - use IsRead property instead of ReadAt
                query = query.Where(n => n.IsRead == false);
                
                // Order by creation date (newest first)
                query = query.OrderByDescending(n => n.CreatedAt);
                
                try
                {
                    return await query.ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error executing query for notifications, attempting fallback query");
                    
                    // Fallback: Just get IDs and create empty notifications if query fails
                    var notificationIds = await _context.Notifications
                        .AsNoTracking()
                        .Where(n => !n.IsRead)
                        .Select(n => n.Id)
                        .Take(10)
                        .ToListAsync();
                        
                    return notificationIds.Select(id => new Notification 
                    { 
                        Id = id,
                        Title = "Notification", 
                        Message = "Please check admin panel for details", 
                        Type = "Info",
                        CreatedAt = DateTime.Now,
                        IsRead = false
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving notifications");
                return new List<Notification>();
            }
        }
        
        public async Task<int> GetUnreadNotificationCountAsync(string userId = null)
        {
            try
            {
                var query = _context.Notifications.AsQueryable();
                
                // If userId is provided, get notifications for that user or all admins
                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(n => n.RecipientId == userId || n.RecipientId == null);
                }
                else
                {
                    // For admin users, get all notifications
                    query = query.Where(n => n.RecipientId == null);
                }
                
                // Count only unread notifications - use IsRead property instead of ReadAt
                return await query.CountAsync(n => n.IsRead == false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error counting unread notifications");
                return 0;
            }
        }
        
        public async Task MarkAsReadAsync(int notificationId)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(notificationId);
                
                if (notification != null)
                {
                    // Don't use ReadAt, just update IsRead
                    notification.IsRead = true;
                    _context.Notifications.Update(notification);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error marking notification as read");
            }
        }
        
        public async Task MarkAllAsReadAsync(string userId = null)
        {
            try
            {
                var query = _context.Notifications.AsQueryable();
                
                // If userId is provided, mark notifications for that user or all admins
                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(n => (n.RecipientId == userId || n.RecipientId == null) && n.IsRead == false);
                }
                else
                {
                    // For admin users, mark all notifications
                    query = query.Where(n => n.RecipientId == null && n.IsRead == false);
                }
                
                var notifications = await query.ToListAsync();
                
                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                }
                
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error marking all notifications as read");
            }
        }
    }
} 