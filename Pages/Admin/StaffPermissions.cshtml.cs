using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Barangay.Data;
using Barangay.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Barangay.Services;
using Newtonsoft.Json;

namespace Barangay.Pages.Admin
{
    [Authorize(Policy = "AccessDashboard")]
    public class StaffPermissionsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StaffPermissionsModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPermissionService _permissionService;
        private readonly IAuditTrailService _auditTrail;

        public StaffPermissionsModel(
            ApplicationDbContext context,
            ILogger<StaffPermissionsModel> logger,
            UserManager<ApplicationUser> userManager,
            IPermissionService permissionService,
            IAuditTrailService auditTrail)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _permissionService = permissionService;
            _auditTrail = auditTrail;
        }

        [BindProperty(SupportsGet = true)]
        public int? StaffId { get; set; }

        [TempData]
        public string SuccessMessage { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public List<StaffMember> StaffMembers { get; set; } = new();
        public StaffMember? SelectedStaff { get; set; }
        public List<Permission> Permissions { get; set; } = new();
        public List<StaffPermission> StaffPermissions { get; set; } = new();
        public Dictionary<string, List<Permission>> PermissionsByCategory { get; set; } = new();

        [BindProperty]
        public List<int> SelectedPermissions { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                _logger.LogInformation("Loading staff permissions page");

                // Load all staff members
                StaffMembers = await _context.StaffMembers
                    .Include(s => s.User)
                    .OrderBy(s => s.FirstName)
                    .ThenBy(s => s.LastName)
                    .ToListAsync();

                // Ensure essential permission rows exist so expected categories appear
                await EnsureEssentialPermissionsAsync();

                // Load all permissions (optionally exclude legacy ones)
                var allPermissions = await _context.Permissions
                    .OrderBy(p => p.Category)
                    .ThenBy(p => p.Name)
                    .ToListAsync();

                // Remove deprecated Vital Signs entries from the UI list
                allPermissions = allPermissions
                    .Where(p => !(p.Category == "Vital Signs" &&
                                  (p.Name == "Record Vital Signs Data" || p.Name == "View Vital Signs Data")))
                    .ToList();

                // If a staff is selected, filter permissions by role so only relevant
                // categories are displayed. Fallback to show all when unknown.
                IEnumerable<Permission> filteredPermissions = allPermissions;

                if (StaffId.HasValue)
                {
                    var staff = await _context.StaffMembers.FirstOrDefaultAsync(s => s.Id == StaffId.Value);
                    var role = staff?.Role?.Trim().ToLower() ?? string.Empty;

                    // Define which permission categories are relevant per role - show actual pages
                    var roleToCategories = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["doctor"] = new HashSet<string>(new []
                        {
                            "Doctor Pages"
                        }, StringComparer.OrdinalIgnoreCase),
                        ["head doctor"] = new HashSet<string>(new []
                        {
                            "Doctor Pages"
                        }, StringComparer.OrdinalIgnoreCase),
                        ["nurse"] = new HashSet<string>(new []
                        {
                            "Nurse Pages"
                        }, StringComparer.OrdinalIgnoreCase),
                        ["head nurse"] = new HashSet<string>(new []
                        {
                            "Nurse Pages"
                        }, StringComparer.OrdinalIgnoreCase),
                        ["admin"] = new HashSet<string>(new []
                        {
                            "Doctor Pages", "Nurse Pages"
                        }, StringComparer.OrdinalIgnoreCase),
                        ["admin staff"] = new HashSet<string>(new []
                        {
                            "Doctor Pages", "Nurse Pages"
                        }, StringComparer.OrdinalIgnoreCase)
                    };

                    if (!string.IsNullOrEmpty(role))
                    {
                        if (roleToCategories.TryGetValue(role, out var allowedCategories))
                        {
                            filteredPermissions = allPermissions.Where(p =>
                                string.IsNullOrEmpty(p.Category) || allowedCategories.Contains(p.Category));

                            // Ensure simplified permissions are present even if category differs
                            if (role.Equals("doctor", StringComparison.OrdinalIgnoreCase) || role.Equals("head doctor", StringComparison.OrdinalIgnoreCase))
                            {
                                var doctorNames = new HashSet<string>(new [] { "Doctor" }, StringComparer.OrdinalIgnoreCase);
                                var extras = allPermissions.Where(p => doctorNames.Contains(p.Name));
                                filteredPermissions = filteredPermissions
                                    .Union(extras)
                                    .Distinct();
                            }
                            else if (role.Equals("nurse", StringComparison.OrdinalIgnoreCase) || role.Equals("head nurse", StringComparison.OrdinalIgnoreCase))
                            {
                                var nurseNames = new HashSet<string>(new [] { "Nurse" }, StringComparer.OrdinalIgnoreCase);
                                var extras = allPermissions.Where(p => nurseNames.Contains(p.Name));
                                filteredPermissions = filteredPermissions
                                    .Union(extras)
                                    .Distinct();
                            }
                        }
                    }
                }

                Permissions = filteredPermissions.ToList();

                // Define page categories - show actual pages available
                var standardCategories = new List<string> 
                {
                    "Doctor Pages",
                    "Nurse Pages"
                };

                // Group permissions by category
                var groupedPermissions = Permissions
                    .GroupBy(p => !string.IsNullOrEmpty(p.Category) ? p.Category : "General")
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Reorder categories according to standard order
                PermissionsByCategory = new Dictionary<string, List<Permission>>();
                
                // First add standard categories in order
                foreach (var category in standardCategories)
                {
                    if (groupedPermissions.ContainsKey(category))
                    {
                        PermissionsByCategory[category] = groupedPermissions[category];
                    }
                }
                
                // Then add any other categories
                foreach (var category in groupedPermissions.Keys)
                {
                    if (!PermissionsByCategory.ContainsKey(category))
                    {
                        PermissionsByCategory[category] = groupedPermissions[category];
                    }
                }

                // If a staff ID is provided, load that staff member and their permissions
                if (StaffId.HasValue)
                {
                    _logger.LogInformation("Loading permissions for staff ID: {StaffId}", StaffId);

                    SelectedStaff = await _context.StaffMembers
                        .Include(s => s.User)
                        .FirstOrDefaultAsync(s => s.Id == StaffId);

                    if (SelectedStaff != null)
                    {
                        // Load current permissions for this staff member
                        StaffPermissions = await _context.StaffPermissions
                            .Where(sp => sp.StaffMemberId == StaffId)
                            .ToListAsync();

                        _logger.LogInformation("Found {Count} permissions for staff member {Name}", 
                            StaffPermissions.Count, SelectedStaff.Name);
                    }
                    else
                    {
                        _logger.LogWarning("Staff member not found for ID: {StaffId}", StaffId);
                        ErrorMessage = "Staff member not found.";
                        return RedirectToPage("/Admin/AdminDashboard");
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading staff permissions page");
                ErrorMessage = "Error loading permissions. Please try again later.";
                return RedirectToPage("/Admin/AdminDashboard");
            }
        }

        private async Task EnsureEssentialPermissionsAsync()
        {
            var mustHave = new List<Permission>
            {
                // Doctor Pages - Show actual pages available to doctors
                new Permission { Name = "DoctorDashboard", Description = "Access to Doctor Dashboard page", Category = "Doctor Pages" },
                new Permission { Name = "Consultation", Description = "Access to Consultation page", Category = "Doctor Pages" },
                new Permission { Name = "PatientRecords", Description = "Access to Patient Records page", Category = "Doctor Pages" },
                new Permission { Name = "PatientList", Description = "Access to Patient List page", Category = "Doctor Pages" },
                new Permission { Name = "Reports", Description = "Access to Reports page", Category = "Doctor Pages" },

                // Nurse Pages - Show actual pages available to nurses
                new Permission { Name = "NurseDashboard", Description = "Access to Nurse Dashboard page", Category = "Nurse Pages" },
                new Permission { Name = "PatientList", Description = "Access to Patient List page", Category = "Nurse Pages" },
                new Permission { Name = "Appointments", Description = "Access to Appointments page", Category = "Nurse Pages" },
                new Permission { Name = "VitalSigns", Description = "Access to Vital Signs page", Category = "Nurse Pages" },
                new Permission { Name = "PatientQueue", Description = "Access to Patient Queue page", Category = "Nurse Pages" }
            };

            var existingNames = await _context.Permissions.Select(p => p.Name).ToListAsync();
            var toInsert = mustHave.Where(p => !existingNames.Contains(p.Name)).ToList();
            if (toInsert.Count > 0)
            {
                await _context.Permissions.AddRangeAsync(toInsert);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!StaffId.HasValue)
                {
                    ErrorMessage = "No staff member selected.";
                    return RedirectToPage();
                }

                // Find the staff member
                var staffMember = await _context.StaffMembers
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == StaffId);

                if (staffMember == null)
                {
                    ErrorMessage = "Staff member not found.";
                    return RedirectToPage();
                }

                // Load all current permissions
                var currentPermissions = await _context.StaffPermissions
                    .Where(sp => sp.StaffMemberId == StaffId)
                    .Include(sp => sp.Permission)
                    .ToListAsync();
                
                // Get old permission names for audit log
                var oldPermissionNames = currentPermissions.Select(cp => cp.Permission.Name).ToList();

                // Remove all current permissions
                _context.StaffPermissions.RemoveRange(currentPermissions);

                // Add new permissions and get names
                var newPermissionNames = new List<string>();
                if (SelectedPermissions != null && SelectedPermissions.Any())
                {
                    var permissionsList = await _context.Permissions
                        .Where(p => SelectedPermissions.Contains(p.Id))
                        .ToListAsync();
                    
                    foreach (var permissionId in SelectedPermissions)
                    {
                        _context.StaffPermissions.Add(new StaffPermission
                        {
                            StaffMemberId = StaffId.Value,
                            PermissionId = permissionId
                        });
                        
                        var permName = permissionsList.FirstOrDefault(p => p.Id == permissionId)?.Name;
                        if (!string.IsNullOrEmpty(permName))
                            newPermissionNames.Add(permName);
                    }
                }

                await _context.SaveChangesAsync();
                
                // Log audit trail
                var changedPermissions = new List<string>();
                var removedPerms = oldPermissionNames.Except(newPermissionNames).ToList();
                var addedPerms = newPermissionNames.Except(oldPermissionNames).ToList();
                
                if (removedPerms.Any())
                    changedPermissions.Add($"Removed: {string.Join(", ", removedPerms)}");
                if (addedPerms.Any())
                    changedPermissions.Add($"Added: {string.Join(", ", addedPerms)}");
                
                await _auditTrail.LogAsync(
                    "Update",
                    $"Updated permissions for staff member {staffMember.Name} ({staffMember.User?.Email})",
                    "StaffPermissions",
                    StaffId.Value.ToString(),
                    JsonConvert.SerializeObject(oldPermissionNames),
                    JsonConvert.SerializeObject(newPermissionNames),
                    changedPermissions.Any() ? string.Join(" | ", changedPermissions) : "No changes"
                );

                // Clear cached permissions for this user
                if (staffMember.User != null)
                {
                    await _permissionService.ClearCachedPermissionsForStaffMemberAsync(staffMember.Id);
                    // Also refresh all permission caches to ensure consistency
                    await _permissionService.RefreshAllPermissionCachesAsync();
                    
                    _logger.LogInformation($"Permissions updated for user {staffMember.UserId}. Cache cleared and permissions refreshed.");
                }

                SuccessMessage = $"Permissions updated for {staffMember.Name}.";
                return RedirectToPage(new { StaffId = StaffId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating staff permissions");
                ErrorMessage = "Error updating permissions. Please try again later.";
                return RedirectToPage();
            }
        }
    }
} 