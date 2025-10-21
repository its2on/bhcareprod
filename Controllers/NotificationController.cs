using System.Threading.Tasks;
using Barangay.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Barangay.Models;
using Barangay.Data;
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Barangay.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationController> _logger;
        
        public NotificationController(
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            try
            {
                _logger.LogInformation("GetNotifications endpoint called");
                
                // Get the count of pending users with additional validation
                var pendingUsers = await _userManager.Users
                    .Where(u => u.Status == "Pending")
                    .Select(u => new { u.Id, u.UserName, u.Email })
                    .ToListAsync();
                    
                var pendingUsersCount = pendingUsers.Count;
                    
                _logger.LogInformation($"Found {pendingUsersCount} pending users with IDs: {string.Join(", ", pendingUsers.Select(u => u.Id))}");

                // Double-check with UserDocuments table to ensure data consistency
                var pendingDocuments = await _context.UserDocuments
                    .Where(d => d.Status == "Pending")
                    .CountAsync();
                    
                _logger.LogInformation($"Found {pendingDocuments} pending documents in UserDocuments table");
                
                // If counts don't match, log warning and fix the discrepancy
                if (pendingUsersCount != pendingDocuments)
                {
                    _logger.LogWarning($"Mismatch between pending users ({pendingUsersCount}) and pending documents ({pendingDocuments})");
                    
                    // Try to auto-fix the discrepancy
                    await SynchronizePendingUsersAndDocuments();
                    
                    // Re-query after fix attempt
                    pendingUsersCount = await _userManager.Users
                        .CountAsync(u => u.Status == "Pending");
                }

                // Get the notifications
                var notifications = await _notificationService.GetUnreadNotificationsAsync();
                _logger.LogInformation($"Found {notifications.Count()} unread notifications");
                
                // Format the notifications for display
                var formattedNotifications = notifications
                    .Select(n => new 
                    {
                        id = n.Id,
                        message = n.Message,
                        link = n.Link,
                        type = n.Type,
                        createdAt = n.CreatedAt
                    })
                    .ToList();
                    
                _logger.LogInformation("Returning notification data");

                return Ok(new 
                { 
                    count = pendingUsersCount,
                    notifications = formattedNotifications
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications");
                return StatusCode(500, new { error = "Error retrieving notifications", details = ex.Message });
            }
        }
        
        // Method to synchronize pending users and documents
        private async Task SynchronizePendingUsersAndDocuments()
        {
            try
            {
                _logger.LogInformation("Starting synchronization of pending users and documents");
                
                // Find pending users without documents
                var pendingUsersWithoutDocs = await _userManager.Users
                    .Where(u => u.Status == "Pending")
                    .Where(u => !_context.UserDocuments.Any(d => d.UserId == u.Id))
                    .ToListAsync();
                    
                _logger.LogInformation($"Found {pendingUsersWithoutDocs.Count} pending users without documents");
                
                // Find documents without matching pending users
                var orphanedDocs = await _context.UserDocuments
                    .Where(d => d.Status == "Pending")
                    .Where(d => !_userManager.Users.Any(u => u.Id == d.UserId && u.Status == "Pending"))
                    .ToListAsync();
                    
                _logger.LogInformation($"Found {orphanedDocs.Count} orphaned documents");
                
                // Fix orphaned documents - mark as processed
                foreach (var doc in orphanedDocs)
                {
                    doc.Status = "Processed";
                    _logger.LogInformation($"Marked orphaned document {doc.Id} as Processed");
                }
                
                // Save changes
                await _context.SaveChangesAsync();
                _logger.LogInformation("Synchronization completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during synchronization");
                // Don't rethrow - this is a background task
            }
        }
        
        [HttpPost("markAsRead/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                await _notificationService.MarkAsReadAsync(id);
                _logger.LogInformation($"Notification {id} marked as read");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking notification {id} as read");
                return StatusCode(500, new { error = "Error marking notification as read" });
            }
        }
        
        [HttpPost("markAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                await _notificationService.MarkAllAsReadAsync();
                _logger.LogInformation("All notifications marked as read");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, new { error = "Error marking all notifications as read" });
            }
        }

        // User-facing endpoints (accessible by all authenticated users)
        [AllowAnonymous]
        [Authorize]
        [HttpPost("{id}/mark-read")]
        public async Task<IActionResult> UserMarkAsRead(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized();

                await _notificationService.MarkAsReadAsync(id);
                _logger.LogInformation($"User {user.Id} marked notification {id} as read");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking notification {id} as read for user");
                return StatusCode(500, new { error = "Error marking notification as read" });
            }
        }

        [AllowAnonymous]
        [Authorize]
        [HttpPost("mark-all-read")]
        public async Task<IActionResult> UserMarkAllAsRead()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Unauthorized();

                await _notificationService.MarkAllAsReadAsync(user.Id);
                _logger.LogInformation($"User {user.Id} marked all notifications as read");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user");
                return StatusCode(500, new { error = "Error marking all notifications as read" });
            }
        }
        
        [HttpGet("debug")]
        public async Task<IActionResult> GetNotificationsDebug()
        {
            try
            {
                _logger.LogInformation("GetNotificationsDebug endpoint called - performing detailed diagnostics");
                
                // Test database connection
                bool canConnect = await _context.Database.CanConnectAsync();
                _logger.LogInformation($"Database connection test: {(canConnect ? "SUCCESS" : "FAILED")}");
                
                // Get connection info
                var connection = _context.Database.GetDbConnection();
                string dbName = connection.Database;
                string dataSource = connection.DataSource;
                _logger.LogInformation($"Connected to database: {dbName} on server: {dataSource}");
                
                // Check for users regardless of status
                var allUserCount = await _userManager.Users.CountAsync();
                _logger.LogInformation($"Total users in database: {allUserCount}");
                
                // Detailed query for pending users to ensure we're finding them correctly
                var pendingUsers = await _userManager.Users
                    .Where(u => u.Status == "Pending")
                    .Select(u => new { u.Id, u.UserName, u.Email, u.Status, u.CreatedAt })
                    .ToListAsync();
                    
                var pendingUsersCount = pendingUsers.Count;
                _logger.LogInformation($"Found {pendingUsersCount} pending users with details:");
                foreach (var user in pendingUsers)
                {
                    _logger.LogInformation($"  - User ID: {user.Id}, Username: {user.UserName}, Email: {user.Email}, Status: {user.Status}, Created: {user.CreatedAt}");
                }
                
                // Get users by status for diagnostic purposes
                var verifiedCount = await _userManager.Users.CountAsync(u => u.Status == "Verified");
                var rejectedCount = await _userManager.Users.CountAsync(u => u.Status == "Rejected");
                var otherStatusCount = await _userManager.Users.CountAsync(u => 
                    u.Status != "Pending" && 
                    u.Status != "Verified" && 
                    u.Status != "Rejected");
                
                _logger.LogInformation($"User status counts - Pending: {pendingUsersCount}, Verified: {verifiedCount}, " +
                                      $"Rejected: {rejectedCount}, Other: {otherStatusCount}");
                
                // Check for pending user documents
                var pendingDocuments = await _context.UserDocuments
                    .Where(d => d.Status == "Pending")
                    .Select(d => new { d.Id, d.UserId, d.FileName })
                    .ToListAsync();
                    
                _logger.LogInformation($"Found {pendingDocuments.Count} pending documents");
                foreach (var doc in pendingDocuments)
                {
                    _logger.LogInformation($"  - Doc ID: {doc.Id}, User ID: {doc.UserId}, File: {doc.FileName}");
                }
                
                // Get all notifications for diagnostics
                var allNotifications = await _context.Notifications.CountAsync();
                _logger.LogInformation($"Total notifications in database: {allNotifications}");
                
                // Get unread notifications for the regular endpoint
                var unreadNotifications = await _notificationService.GetUnreadNotificationsAsync();
                _logger.LogInformation($"Unread notifications: {unreadNotifications.Count()}");
                
                // Get recent users for diagnostics
                var recentUsers = await _userManager.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .Select(u => new { 
                        u.Id, 
                        u.UserName, 
                        u.Email, 
                        u.Status, 
                        u.IsActive, 
                        u.CreatedAt 
                    })
                    .ToListAsync();
                
                // Get recent notifications for diagnostics
                var recentNotifications = await _context.Notifications
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .ToListAsync();
                
                // Return comprehensive diagnostic data
                return Ok(new { 
                    diagnostics = new {
                        databaseConnection = canConnect,
                        databaseName = dbName,
                        dataSource = dataSource,
                        userCounts = new {
                            total = allUserCount,
                            pending = pendingUsersCount,
                            verified = verifiedCount,
                            rejected = rejectedCount,
                            other = otherStatusCount
                        },
                        pendingUsers = pendingUsers,
                        pendingDocuments = pendingDocuments,
                        notificationCounts = new {
                            total = allNotifications,
                            unread = unreadNotifications.Count()
                        },
                        recentUsers,
                        recentNotifications
                    },
                    count = pendingUsersCount,
                    notifications = unreadNotifications.Select(n => new {
                        id = n.Id,
                        message = n.Message,
                        link = n.Link,
                        type = n.Type,
                        createdAt = n.CreatedAt
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetNotificationsDebug");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
} 