using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Barangay.Authorization
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PermissionHandler> _logger;

        public PermissionHandler(ApplicationDbContext context, ILogger<PermissionHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            if (context.User == null)
            {
                _logger.LogWarning("Authorization failed: User is null");
                return;
            }

            // Special case - admin and system administrator roles have all permissions
            if (context.User.IsInRole("Admin") || context.User.IsInRole("System Administrator"))
            {
                _logger.LogInformation("User is in Admin or System Administrator role, granting {Permission} permission", requirement.Permission);
                context.Succeed(requirement);
                return;
            }

            // Get user ID from claims
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Authorization failed: User ID not found in claims");
                return;
            }

            // Allow users with User or Patient role to access the User Dashboard
            if (requirement.Permission == "Access User Dashboard" && 
                (context.User.IsInRole("User") || context.User.IsInRole("Patient")))
            {
                _logger.LogInformation("User is in User or Patient role, granting access to User Dashboard");
                context.Succeed(requirement);
                return;
            }

            // Special case for verified or auto-approved users to access User Dashboard
            if (requirement.Permission == "Access User Dashboard" || requirement.Permission == "Access Dashboard")
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null)
                {
                    // Check if user is verified OR auto-approved (bypass approval page)
                    // Accept both "Verified" and "Active" status for auto-approved users
                    bool isVerified = (user.Status == "Verified" && user.IsActive) || 
                                     (user.IsApproved && user.VerificationStatus == "Auto Verified" && user.IsActive) ||
                                     (user.Status == "Active" && user.IsApproved && user.VerificationStatus == "Auto Verified" && user.IsActive);
                    
                    if (isVerified)
                    {
                        _logger.LogInformation("User {UserId} is verified/auto-approved, granting access to Dashboard", userId);
                        context.Succeed(requirement);
                        return;
                    }
                }
            }

            // For Nurse role/dashboard
            if ((requirement.Permission == "Access Nurse Dashboard" || requirement.Permission == "Access Dashboard") 
                && (context.User.IsInRole("Nurse") || context.User.IsInRole("Head Nurse")))
            {
                _logger.LogInformation("User is in Nurse role, granting access to Nurse Dashboard");
                context.Succeed(requirement);
                return;
            }

            // For Doctor role/dashboard
            if ((requirement.Permission == "Access Doctor Dashboard" || requirement.Permission == "Access Dashboard") 
                && (context.User.IsInRole("Doctor") || context.User.IsInRole("Head Doctor")))
            {
                _logger.LogInformation("User is in Doctor role, granting access to Doctor Dashboard");
                context.Succeed(requirement);
                return;
            }

            _logger.LogInformation("Checking if user {UserId} has permission {Permission}", userId, requirement.Permission);

            // Normalize the permission name to check both formats (with and without category)
            string permissionToCheck = requirement.Permission;
            string permissionNameOnly = requirement.Permission;
            string Normalize(string s) => (s ?? string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
            var normalizedRequired = Normalize(requirement.Permission);
            
            // If permission contains a category prefix (Category:Name format)
            if (requirement.Permission.Contains(':'))
            {
                permissionNameOnly = requirement.Permission.Split(':')[1];
            }

            // Staff override: if staff has any configured permissions, they are authoritative
            var staffMember = await _context.StaffMembers
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (staffMember != null)
            {
                var staffPermissions = await _context.StaffPermissions
                    .Include(sp => sp.Permission)
                    .Where(sp => sp.StaffMemberId == staffMember.Id)
                    .ToListAsync();

                var hasStaffPermission = staffPermissions.Any(sp =>
                    sp.Permission.Name == permissionToCheck || 
                    sp.Permission.Name == permissionNameOnly ||
                    Normalize(sp.Permission.Name) == normalizedRequired ||
                    Normalize($"{sp.Permission.Category}:{sp.Permission.Name}") == normalizedRequired);

                // Staff is authoritative: allow only if StaffPermissions explicitly grants
                if (hasStaffPermission)
                {
                    _logger.LogInformation("Staff {StaffId} granted {Permission} via StaffPermissions allow-list", staffMember.Id, requirement.Permission);
                    context.Succeed(requirement);
                }
                else
                {
                    _logger.LogWarning("Staff {StaffId} missing {Permission} in StaffPermissions. Denying (no role/user fallback for staff).", staffMember.Id, requirement.Permission);
                }
                return; // Always stop here for staff users (deny by default if not granted)
            }

            // Check in UserPermissions table first - with both formats
            var userPermissions = await _context.UserPermissions
                .Include(up => up.Permission)
                .Where(up => up.UserId == userId)
                .ToListAsync();

            var hasUserPermission = userPermissions.Any(up => 
                up.Permission.Name == permissionToCheck ||
                up.Permission.Name == permissionNameOnly ||
                Normalize(up.Permission.Name) == normalizedRequired ||
                Normalize($"{up.Permission.Category}:{up.Permission.Name}") == normalizedRequired);

            if (hasUserPermission)
            {
                _logger.LogInformation("User {UserId} has permission {Permission} in UserPermissions table", 
                    userId, requirement.Permission);
                context.Succeed(requirement);
                return;
            }

            // If not found in UserPermissions, check for role-based permissions
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();
                
            if (userRoles.Any())
            {
                // Check if any of the user's roles have this permission
                var rolePermissions = await _context.RolePermissions
                    .Include(rp => rp.Permission)
                    .Where(rp => userRoles.Contains(rp.RoleId))
                    .ToListAsync();
                    
                var hasRolePermission = rolePermissions.Any(rp => 
                    rp.Permission.Name == permissionToCheck || 
                    rp.Permission.Name == permissionNameOnly ||
                    Normalize(rp.Permission.Name) == normalizedRequired ||
                    Normalize($"{rp.Permission.Category}:{rp.Permission.Name}") == normalizedRequired);
                         
                if (hasRolePermission)
                {
                    _logger.LogInformation("User {UserId} has role-based permission {Permission}", 
                        userId, requirement.Permission);
                    context.Succeed(requirement);
                    return;
                }
            }

            _logger.LogWarning("Authorization failed: User {UserId} does not have permission {Permission}", 
                userId, requirement.Permission);
        }
    }
} 