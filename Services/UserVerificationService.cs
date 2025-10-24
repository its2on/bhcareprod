using Barangay.Data;
using Barangay.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Barangay.Services
{
    public class UserVerificationService : IUserVerificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UserVerificationService> _logger;
        private readonly IAuditTrailService _auditTrail;
        
        public UserVerificationService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<UserVerificationService> logger,
            IAuditTrailService auditTrail)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _auditTrail = auditTrail;
        }
        
        public async Task<List<ApplicationUser>> GetPendingUsersAsync()
        {
            try
            {
                // Join AspNetUsers and UserDocuments tables to find pending users
                var pendingUsers = await _userManager.Users
                    .Where(u => u.Status == "Pending" || u.IsActive == false)
                    .Include(u => u.UserDocuments)
                    .ToListAsync();
                
                return pendingUsers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending users");
                return new List<ApplicationUser>();
            }
        }
        
        public async Task<bool> ApproveUserAsync(string userId, string adminId)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User not found with ID: {userId}");
                    return false;
                }

                // Update user status
                user.Status = "Verified";
                user.IsActive = true;
                user.EmailConfirmed = true; // Ensure email is confirmed
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    _logger.LogError($"Failed to update user status: {string.Join(", ", result.Errors)}");
                    await transaction.RollbackAsync();
                    return false;
                }

                // Preserve existing staff roles; only assign 'User' role to non-staff users without roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                var staffRoles = new[] { "Doctor", "Nurse", "Admin", "Admin Staff", "System Administrator" };

                if (!currentRoles.Any(r => staffRoles.Contains(r)))
                {
                    // Ensure 'User' role exists
                    if (!await _roleManager.RoleExistsAsync("User"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("User"));
                        _logger.LogInformation("Created 'User' role");
                    }

                    // Assign 'User' role if not already present
                    if (!currentRoles.Contains("User"))
                    {
                        var roleAddResult = await _userManager.AddToRoleAsync(user, "User");
                        if (!roleAddResult.Succeeded)
                        {
                            _logger.LogError($"Failed to assign User role: {string.Join(", ", roleAddResult.Errors)}");
                            await transaction.RollbackAsync();
                            return false;
                        }
                        _logger.LogInformation($"Assigned 'User' role to {user.Email}");
                    }
                }
                else
                {
                    _logger.LogInformation($"Preserved existing staff roles for {user.Email}: {string.Join(", ", currentRoles)}");
                }

                // Update any associated documents
                var documents = await _context.UserDocuments
                    .Where(d => d.UserId == userId)
                    .ToListAsync();

                foreach (var doc in documents)
                {
                    doc.Status = "Verified";
                    doc.ApprovedBy = adminId;
                    doc.ApprovedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Successfully approved user {user.Email} with ID {userId}");
                
                // Log audit trail
                await _auditTrail.LogAsync(
                    "Approve",
                    $"User {user.Email} approved by admin",
                    "UserVerification",
                    userId,
                    JsonConvert.SerializeObject(new { Status = "Pending", IsActive = false }),
                    JsonConvert.SerializeObject(new { Status = "Verified", IsActive = true }),
                    $"User approved and assigned role(s): {string.Join(", ", await _userManager.GetRolesAsync(user))}"
                );
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving user: {userId}");
                return false;
            }
        }
        
        public async Task<bool> RejectUserAsync(string userId, string adminId, string reason = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Get the user
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return false;
                }
                
                // Update user status
                user.Status = "Rejected";
                user.IsActive = false;
                
                // Update user
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    _logger.LogError("Failed to update user: {Errors}", string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                    return false;
                }
                
                // Update user documents
                var documents = await _context.UserDocuments
                    .Where(d => d.UserId == userId)
                    .ToListAsync();
                
                foreach (var document in documents)
                {
                    document.Status = "Rejected";
                    document.ApprovedAt = DateTime.UtcNow;
                    document.ApprovedBy = adminId;
                }
                
                await _context.SaveChangesAsync();
                
                // Commit transaction
                await transaction.CommitAsync();
                
                // Log audit trail
                await _auditTrail.LogAsync(
                    "Reject",
                    $"User {user.Email} rejected by admin",
                    "UserVerification",
                    userId,
                    JsonConvert.SerializeObject(new { Status = "Pending" }),
                    JsonConvert.SerializeObject(new { Status = "Rejected" }),
                    string.IsNullOrEmpty(reason) ? "User registration rejected" : $"Rejection reason: {reason}"
                );
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting user: {UserId}", userId);
                await transaction.RollbackAsync();
                return false;
            }
        }
        
        public async Task<bool> IsUserVerifiedAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }
                
                return user.Status == "Verified" && user.IsActive;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user is verified: {UserId}", userId);
                return false;
            }
        }
        
        public async Task<string> GetUserStatusAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                return user?.Status ?? "Unknown";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user status: {UserId}", userId);
                return "Error";
            }
        }

        public async Task EnsureUserRoleAssignedAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || user.Status != "Verified")
                {
                    return;
                }

                // Ensure user has the 'User' role if verified
                if (!await _userManager.IsInRoleAsync(user, "User"))
                {
                    if (!await _roleManager.RoleExistsAsync("User"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("User"));
                    }
                    await _userManager.AddToRoleAsync(user, "User");
                    _logger.LogInformation($"Assigned 'User' role to verified user {user.Email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring user role assignment");
            }
        }
    }
}