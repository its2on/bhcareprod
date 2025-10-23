using Barangay.Data;
using Barangay.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Barangay.Services
{
    public interface IAuditTrailService
    {
        Task LogAsync(string actionType, string action, string entityName, string entityId, 
                      string oldValues = null, string newValues = null, string description = null);
    }

    public class AuditTrailService : IAuditTrailService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AuditTrailService> _logger;

        public AuditTrailService(
            ApplicationDbContext context, 
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager,
            ILogger<AuditTrailService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task LogAsync(string actionType, string action, string entityName, string entityId, 
                                   string oldValues = null, string newValues = null, string description = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null) return;

                var user = httpContext.User;
                var userName = user?.Identity?.Name ?? "System";
                var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
                
                // Get user role - check multiple claim types
                var role = user?.FindFirstValue(ClaimTypes.Role) 
                          ?? user?.FindFirstValue("role")
                          ?? await GetUserRoleAsync(userId)
                          ?? "Unknown";

                // Get IP address - handle proxy scenarios
                var ipAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (string.IsNullOrEmpty(ipAddress))
                {
                    var remoteIp = httpContext.Connection?.RemoteIpAddress;
                    ipAddress = remoteIp?.IsIPv4MappedToIPv6 == true 
                        ? remoteIp.MapToIPv4().ToString() 
                        : remoteIp?.ToString();
                }

                // Capture request details
                var requestMethod = httpContext.Request.Method;
                var requestUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.Path}{httpContext.Request.QueryString}";
                var deviceInfo = httpContext.Request.Headers["User-Agent"].ToString();
                var sessionId = httpContext.Session?.Id;

                // Determine outcome based on action type
                var outcome = actionType.Contains("Failed") || actionType.Contains("LoginFailed") ? "Failed" : "Success";

                var auditLog = new AuditTrail
                {
                    PerformedBy = userName,
                    UserId = userId,
                    Role = role,
                    ActionType = actionType,
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    Description = description,
                    IPAddress = ipAddress,
                    OldValues = oldValues,
                    NewValues = newValues,
                    Timestamp = DateTime.UtcNow,
                    RequestMethod = requestMethod,
                    RequestUrl = requestUrl,
                    DeviceInfo = deviceInfo,
                    SessionId = sessionId,
                    Outcome = outcome,
                    Location = null // Can be enhanced with IP geolocation service
                };

                _logger.LogInformation("=== AUDIT LOG ATTEMPT ===");
                _logger.LogInformation("User: {User}, Role: {Role}, Action: {Action}", userName, role, actionType);
                
                _context.AuditTrails.Add(auditLog);
                
                var savedCount = await _context.SaveChangesAsync();
                
                _logger.LogInformation("✅ Audit log saved successfully. ID: {Id}, Rows affected: {Count}", auditLog.Id, savedCount);
            }
            catch (Exception ex)
            {
                // Log error but don't throw - audit logging should not break the application
                _logger.LogError(ex, "❌ AUDIT LOGGING FAILED: {Message}\nStack: {Stack}", ex.Message, ex.StackTrace);
                Console.WriteLine($"Audit logging error: {ex.Message}");
            }
        }

        private async Task<string> GetUserRoleAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return "Unknown";

            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return "Unknown";

                var roles = await _userManager.GetRolesAsync(user);
                return roles.FirstOrDefault() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }
}
