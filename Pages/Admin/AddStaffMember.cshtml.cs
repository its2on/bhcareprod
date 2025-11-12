using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using Barangay.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Barangay.Pages.Admin
{
    [Authorize(Roles = "Admin,Nurse,Doctor")]
    public class AddStaffMemberModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AddStaffMemberModel> _logger;
        private readonly IDataEncryptionService _encryptionService;
        private readonly IAuditTrailService _auditTrail;

        public AddStaffMemberModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AddStaffMemberModel> logger,
            IDataEncryptionService encryptionService,
            IAuditTrailService auditTrail)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _encryptionService = encryptionService;
            _auditTrail = auditTrail;
        }

        [BindProperty]
        public StaffMember StaffMember { get; set; } = new StaffMember();

        [BindProperty]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public List<int> SelectedPermissions { get; set; } = new List<int>();

        [TempData]
        public string ErrorMessage { get; set; } = string.Empty;

        [TempData]
        public string SuccessMessage { get; set; } = string.Empty;
        
        public List<string> DaysOfWeek { get; } = new List<string> 
        { 
            "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" 
        };
        
        public List<string> TimeSlots { get; } = new List<string>();

        public Dictionary<string, List<Permission>> CategorizedPermissions { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                // Initialize default values
                StaffMember = new StaffMember
                {
                    IsActive = true,
                    JoinDate = DateTimeHelper.Now,
                    CreatedAt = DateTimeHelper.Now
                };
                
                // Generate time slots for working hours
                GenerateTimeSlots();

                // Ensure essential simplified permissions exist (align with StaffPermissions page)
                await EnsureEssentialPermissionsAsync();

                // Load and categorize permissions
                var permissions = await _context.Permissions
                    .OrderBy(p => p.Category)
                    .ThenBy(p => p.Name)
                    .ToListAsync();

                if (!permissions.Any())
                {
                    await CreateDefaultPermissionsAsync();
                    permissions = await _context.Permissions
                        .OrderBy(p => p.Category)
                        .ThenBy(p => p.Name)
                        .ToListAsync();
                }

                // Group permissions by category, excluding "Doctor Pages" and "Nurse Pages" to avoid duplicates
                var grouped = permissions
                    .Where(p => p.Category != "Doctor Pages" && p.Category != "Nurse Pages") // Exclude these categories
                    .GroupBy(p => string.IsNullOrEmpty(p.Category) ? "General" : p.Category)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var ordered = new Dictionary<string, List<Permission>>();
                
                // Build simplified role categories by permission NAME so it works regardless of DB categories
                var byName = permissions
                    .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                // Add Nurse simplified group (combines Nurse Pages permissions)
                var nurseNames = new [] { "Appointments", "NurseDashboard", "PatientList", "PatientQueue", "VitalSigns" };
                var nurseList = new List<Permission>();
                foreach (var n in nurseNames)
                {
                    if (byName.TryGetValue(n, out var perm)) nurseList.Add(perm);
                }
                if (nurseList.Count > 0)
                {
                    ordered["Nurse"] = nurseList;
                }

                // Add Doctor simplified group (combines Doctor Pages permissions)
                var doctorNames = new [] { "Consultation", "DoctorDashboard", "Reports", "PatientList", "PatientRecords" };
                var doctorList = new List<Permission>();
                foreach (var d in doctorNames)
                {
                    if (byName.TryGetValue(d, out var perm)) doctorList.Add(perm);
                }
                if (doctorList.Count > 0)
                {
                    ordered["Doctor"] = doctorList;
                }
                
                // Add remaining categories (excluding the ones we've already handled)
                foreach (var kv in grouped)
                {
                    if (!ordered.ContainsKey(kv.Key))
                    {
                        ordered[kv.Key] = kv.Value;
                    }
                }

                CategorizedPermissions = ordered;

                // Filter permissions based on selected role
                var selectedRole = (StaffMember.Role ?? string.Empty).Trim();
                
                if (!string.IsNullOrEmpty(selectedRole))
                {
                    var roleToCategories = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
                    {
                        // Doctor positions
                        ["Head Doctor"] = new HashSet<string>(new [] { "Doctor", "Dashboard Access" }, StringComparer.OrdinalIgnoreCase),
                        
                        // Nurse positions  
                        ["Head Nurse"] = new HashSet<string>(new [] { "Nurse", "Dashboard Access" }, StringComparer.OrdinalIgnoreCase),
                        
                        // Admin positions
                        ["Admin Staff"] = new HashSet<string>(new [] { "User Management", "Reports", "Reporting", "Dashboard Access" }, StringComparer.OrdinalIgnoreCase)
                    };

                    if (roleToCategories.TryGetValue(selectedRole, out var allowed))
                    {
                        var keep = new Dictionary<string, List<Permission>>(StringComparer.OrdinalIgnoreCase);
                        foreach (var kv in CategorizedPermissions)
                        {
                            if (allowed.Contains(kv.Key) || kv.Key.Equals("Dashboard Access", StringComparison.OrdinalIgnoreCase))
                                keep[kv.Key] = kv.Value;
                        }
                        CategorizedPermissions = keep;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnGetAsync");
                ErrorMessage = "An error occurred while loading the page. Please try again.";
            }
        }

        // Mirror essential permission seeding used by StaffPermissions to guarantee simplified categories
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

        private async Task CreateDefaultPermissionsAsync()
        {
            var defaultPermissions = new List<Permission>
            {
                // Dashboard permission (consolidated)
                new Permission {
                    Name = "Access Dashboard",
                    Description = "Can access all system dashboards",
                    Category = "Dashboard Access"
                },
                new Permission {
                    Name = "Manage Permissions",
                    Description = "Can manage user permissions",
                    Category = "Administration"
                },

                // Appointments permissions
                new Permission {
                    Name = "Create Appointments",
                    Description = "Can create new appointments",
                    Category = "Appointments"
                },
                new Permission {
                    Name = "View Appointments",
                    Description = "Can view appointment details",
                    Category = "Appointments"
                },

                // Doctor permissions
                new Permission {
                    Name = "Write Prescriptions",
                    Description = "Can write and update prescriptions",
                    Category = "Medical Records"
                },
                new Permission {
                    Name = "Manage Consultations",
                    Description = "Can create and manage patient consultations",
                    Category = "Medical Records"
                },
                new Permission {
                    Name = "View Patient Details",
                    Description = "Can view detailed patient information",
                    Category = "Medical Records"
                },
                new Permission {
                    Name = "Print Medical Records",
                    Description = "Can print patient medical records",
                    Category = "Medical Records"
                },

                // Nurse permissions
                new Permission {
                    Name = "Record Vital Signs",
                    Description = "Can record patient vital signs",
                    Category = "Medical Records"
                },
                new Permission {
                    Name = "Manage Patient Queue",
                    Description = "Can manage the patient queue",
                    Category = "Patient Management"
                },
                new Permission {
                    Name = "View Patient History",
                    Description = "Can view patient medical history",
                    Category = "Medical Records"
                },
                new Permission {
                    Name = "Create Medical Records",
                    Description = "Can create new medical records",
                    Category = "Medical Records"
                },
                new Permission {
                    Name = "Edit Medical Records",
                    Description = "Can edit existing medical records",
                    Category = "Medical Records"
                },
                new Permission {
                    Name = "Manage Diagnoses",
                    Description = "Can create and manage diagnoses",
                    Category = "Medical Records"
                },

                // VitalSigns permissions (simplified: only Access Vital Signs)
                new Permission {
                    Name = "Access Vital Signs",
                    Description = "Can access the vital signs page",
                    Category = "Vital Signs"
                },
                new Permission {
                    Name = "Delete Vital Signs Data",
                    Description = "Can delete patient vital signs records",
                    Category = "Vital Signs"
                },

                // Prescriptions permissions
                new Permission {
                    Name = "Create Prescriptions",
                    Description = "Can create new prescriptions",
                    Category = "Prescriptions"
                },
                new Permission {
                    Name = "View Prescriptions",
                    Description = "Can view patient prescriptions",
                    Category = "Prescriptions"
                },
                new Permission {
                    Name = "Edit Prescriptions",
                    Description = "Can edit existing prescriptions",
                    Category = "Prescriptions"
                },
                new Permission {
                    Name = "Delete Prescriptions",
                    Description = "Can delete prescriptions",
                    Category = "Prescriptions"
                },

                // Records permissions
                new Permission {
                    Name = "Manage Medical Records",
                    Description = "Can create and edit medical records",
                    Category = "Medical Records"
                },
                new Permission {
                    Name = "View Medical Records",
                    Description = "Can view medical records",
                    Category = "Medical Records"
                },
                new Permission {
                    Name = "Delete Medical Records",
                    Description = "Can delete medical records",
                    Category = "Medical Records"
                },

                // User Management permissions
                new Permission {
                    Name = "Manage Users",
                    Description = "Can manage user accounts",
                    Category = "User Management"
                },
                new Permission {
                    Name = "View Users",
                    Description = "Can view user details",
                    Category = "User Management"
                },
                new Permission {
                    Name = "Approve Users",
                    Description = "Can approve user registrations",
                    Category = "User Management"
                },
                new Permission {
                    Name = "Delete Users",
                    Description = "Can delete users from the system",
                    Category = "User Management"
                }
            };

            await _context.Permissions.AddRangeAsync(defaultPermissions);
            await _context.SaveChangesAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                _logger.LogInformation("Starting OnPostAsync for staff member creation with email: {Email}", StaffMember.Email);

                // Working days and hours are now optional - no validation needed

                // Additional server-side validation for name fields and phone format
                var firstName = (StaffMember.FirstName ?? string.Empty).Trim();
                var middleName = (StaffMember.MiddleName ?? string.Empty).Trim();
                var lastName = (StaffMember.LastName ?? string.Empty).Trim();
                var phone = (StaffMember.ContactNumber ?? string.Empty).Trim();

                // Allow letters (including diacritics), spaces, hyphen, apostrophe. No digits/symbols.
                var nameAllowedPattern = new Regex("^[A-Za-zÀ-ÖØ-öø-ÿ'\\-\\s]+$");
                
                // Validate FirstName
                if (!nameAllowedPattern.IsMatch(firstName))
                {
                    ModelState.AddModelError("StaffMember.FirstName", "First name may only contain letters, spaces, hyphen (-), and apostrophe (').");
                }
                if (Regex.IsMatch(firstName, "([A-Za-z])\\1{2,}"))
                {
                    ModelState.AddModelError("StaffMember.FirstName", "First name cannot contain 3 or more repeated letters in a row.");
                }
                
                // Validate MiddleName (if provided)
                if (!string.IsNullOrEmpty(middleName))
                {
                    if (!nameAllowedPattern.IsMatch(middleName))
                    {
                        ModelState.AddModelError("StaffMember.MiddleName", "Middle name may only contain letters, spaces, hyphen (-), and apostrophe (').");
                    }
                    if (Regex.IsMatch(middleName, "([A-Za-z])\\1{2,}"))
                    {
                        ModelState.AddModelError("StaffMember.MiddleName", "Middle name cannot contain 3 or more repeated letters in a row.");
                    }
                }
                
                // Validate LastName
                if (!nameAllowedPattern.IsMatch(lastName))
                {
                    ModelState.AddModelError("StaffMember.LastName", "Last name may only contain letters, spaces, hyphen (-), and apostrophe (').");
                }
                if (Regex.IsMatch(lastName, "([A-Za-z])\\1{2,}"))
                {
                    ModelState.AddModelError("StaffMember.LastName", "Last name cannot contain 3 or more repeated letters in a row.");
                }

                // Accept both +639XXXXXXXXX and 09XXXXXXXXX formats
                if (!Regex.IsMatch(phone, "^(\\+639\\d{9}|09\\d{9})$"))
                {
                    ModelState.AddModelError("StaffMember.ContactNumber", "Contact number must be in the format +639XXXXXXXXX or 09XXXXXXXXX.");
                }

                // Set Position from Role BEFORE ModelState validation (Position is required but auto-set from Role)
                var selectedRole = StaffMember.Role ?? string.Empty;
                StaffMember.Position = selectedRole;
                _logger.LogInformation("Set Position to: {Position} from Role: {Role}", StaffMember.Position, StaffMember.Role);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState is invalid: {Errors}", 
                        string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    await OnGetAsync();
                    return Page();
                }

                // Check if email already exists (only if email is provided)
                if (!string.IsNullOrEmpty(StaffMember.Email))
                {
                    var existingUser = await _userManager.FindByEmailAsync(StaffMember.Email);
                    if (existingUser != null)
                    {
                        _logger.LogWarning("Email already exists: {Email}", StaffMember.Email);
                        ModelState.AddModelError("StaffMember.Email", "This email is already registered.");
                        await OnGetAsync();
                        return Page();
                    }
                }

                // Normalize and enforce permission selection when a role/position is chosen
                // If no permissions were selected, auto-grant essential permissions for the chosen role
                // Also ensure "Access Dashboard" is included
                
                if (!string.IsNullOrWhiteSpace(selectedRole))
                {
                    // Normalize role to canonical casing to match [Authorize(Roles=...)] attributes
                    StaffMember.Role = NormalizeRoleName(selectedRole);

                    SelectedPermissions ??= new List<int>();

                    if (!SelectedPermissions.Any())
                    {
                        var essential = await GetEssentialPermissionsForRoleAsync(selectedRole);
                        if (essential != null && essential.Any())
                        {
                            SelectedPermissions = essential.Distinct().ToList();
                            _logger.LogInformation(
                                "No permissions selected; auto-granted {Count} essential permissions for role/position {Role}.",
                                SelectedPermissions.Count, selectedRole);
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, "Please select at least one permission or use 'Grant Essential Permissions'.");
                            await OnGetAsync();
                            return Page();
                        }
                    }

                    // Ensure Access Dashboard is always granted if it exists in the DB
                    var accessDashboardId = await _context.Permissions
                        .Where(p => p.Name == "Access Dashboard")
                        .Select(p => p.Id)
                        .FirstOrDefaultAsync();
                    if (accessDashboardId != 0 && !SelectedPermissions.Contains(accessDashboardId))
                    {
                        SelectedPermissions.Add(accessDashboardId);
                        _logger.LogInformation("Added missing 'Access Dashboard' permission for role/position {Role}", selectedRole);
                    }

                    // Deduplicate
                    SelectedPermissions = SelectedPermissions.Distinct().ToList();
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create the user account
                    var userEmail = !string.IsNullOrEmpty(StaffMember.Email) ? StaffMember.Email : $"staff{DateTime.Now.Ticks}@temp.com";
                    var user = new ApplicationUser
                    {
                        UserName = userEmail,
                        Email = _encryptionService.Encrypt(userEmail), // Encrypt email
                        EmailConfirmed = true,
                        PhoneNumber = !string.IsNullOrEmpty(StaffMember.ContactNumber) ? _encryptionService.Encrypt(StaffMember.ContactNumber) : null, // Encrypt phone number
                        // Name fields will be populated below via FullName setter
                        IsActive = StaffMember.IsActive,
                        JoinDate = DateTimeHelper.Now,
                        Status = "Verified", // Set as verified since added by admin
                        BirthDate = DateTime.Now.AddYears(-25), // Set a default birth date (25 years ago)
                        IsFirstLogin = true // Require password change on first login
                    };

                    // Populate FirstName, MiddleName, LastName from the StaffMember model
                    // Build full name from the separate name fields
                    var fullName = string.IsNullOrEmpty(middleName) 
                        ? $"{firstName} {lastName}" 
                        : $"{firstName} {middleName} {lastName}";
                    user.FullName = fullName;

                    // Generate default password if none provided
                    var userPassword = !string.IsNullOrEmpty(Password) ? Password : "TempPassword123!";
                    
                    _logger.LogInformation("Creating user account for: {Email}", userEmail);
                    var result = await _userManager.CreateAsync(user, userPassword);
            
                    if (!result.Succeeded)
                    {
                        _logger.LogError("Failed to create user account. Errors: {Errors}", 
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        await OnGetAsync();
                        return Page();
                    }

                    // Map role to appropriate Identity role
                    string roleToAssign = "Admin Staff"; // Default
                    if (StaffMember.Role == "Head Doctor")
                    {
                        roleToAssign = "Doctor";
                    }
                    else if (StaffMember.Role == "Head Nurse")
                    {
                        roleToAssign = "Nurse";
                    }
                    else if (StaffMember.Role == "Admin Staff")
                    {
                        roleToAssign = "Admin Staff";
                    }

                    // Create or get the role
                    if (!await _roleManager.RoleExistsAsync(roleToAssign))
                    {
                        _logger.LogInformation("Creating new role: {Role}", roleToAssign);
                        var roleResult = await _roleManager.CreateAsync(new IdentityRole(roleToAssign));
                        if (!roleResult.Succeeded)
                        {
                            _logger.LogError("Failed to create role. Errors: {Errors}", 
                                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                            throw new Exception($"Failed to create role: {roleToAssign}");
                        }
                    }

                    // Assign role to user
                    _logger.LogInformation("Assigning role {Role} to user {Email} (mapped from role {OriginalRole})", roleToAssign, StaffMember.Email, StaffMember.Role);
                    var roleAssignResult = await _userManager.AddToRoleAsync(user, roleToAssign);
                    if (!roleAssignResult.Succeeded)
                    {
                        _logger.LogError("Failed to assign role. Errors: {Errors}", 
                            string.Join(", ", roleAssignResult.Errors.Select(e => e.Description)));
                        throw new Exception($"Failed to assign role to user");
                    }

                    // Save staff member details
                    StaffMember.UserId = user.Id;
                    StaffMember.CreatedAt = DateTimeHelper.Now;
                    StaffMember.IsActive = true;
                    StaffMember.Role = roleToAssign; // Update the role to the mapped role
                    
                    // Set default department value if not provided
                    if (string.IsNullOrEmpty(StaffMember.Department))
                    {
                        StaffMember.Department = "General";
                    }
                    
                    // Set default values for WorkingDays and WorkingHours if not provided (database doesn't allow NULL)
                    if (string.IsNullOrEmpty(StaffMember.WorkingDays))
                    {
                        StaffMember.WorkingDays = "Monday,Tuesday,Wednesday,Thursday,Friday";
                        _logger.LogInformation("Setting default WorkingDays for staff member");
                    }
                    
                    if (string.IsNullOrEmpty(StaffMember.WorkingHours))
                    {
                        StaffMember.WorkingHours = "8:00 AM-5:00 PM";
                        _logger.LogInformation("Setting default WorkingHours for staff member");
                    }

                    _logger.LogInformation("Saving staff member details to database");
                    await _context.StaffMembers.AddAsync(StaffMember);
                    await _context.SaveChangesAsync();

                    // Save staff permissions
                    if (SelectedPermissions != null && SelectedPermissions.Any())
                    {
                        _logger.LogInformation("Saving {Count} permissions for staff member", SelectedPermissions.Count);
                        
                        // First, get the permission details to create claims
                        var selectedPermissionDetails = await _context.Permissions
                            .Where(p => SelectedPermissions.Contains(p.Id))
                            .ToListAsync();

                        // Create UserPermissions entries
                        var userPermissions = SelectedPermissions.Select(permissionId => new UserPermission
                        {
                            UserId = user.Id,
                            PermissionId = permissionId
                        }).ToList();

                        await _context.UserPermissions.AddRangeAsync(userPermissions);
                        
                        // Also create StaffPermissions entries
                        var staffPermissions = SelectedPermissions.Select(permissionId => new StaffPermission
                        {
                            StaffMemberId = StaffMember.Id,
                            PermissionId = permissionId,
                            GrantedAt = DateTimeHelper.ToUtc(DateTimeHelper.Now)
                        }).ToList();
                        
                        await _context.StaffPermissions.AddRangeAsync(staffPermissions);

                        // Add permission claims
                        var claims = selectedPermissionDetails.Select(p => 
                            new System.Security.Claims.Claim("Permission", $"{p.Category}:{p.Name}")
                        ).ToList();

                        // Add role claim
                        claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, StaffMember.Role));

                        var claimsResult = await _userManager.AddClaimsAsync(user, claims);
                        if (!claimsResult.Succeeded)
                        {
                            _logger.LogError("Failed to add permission claims. Errors: {Errors}", 
                                string.Join(", ", claimsResult.Errors.Select(e => e.Description)));
                            throw new Exception("Failed to add permission claims");
                        }

                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Successfully saved {Count} permissions and claims", SelectedPermissions.Count);
                    }
                    else
                    {
                        _logger.LogWarning("No permissions selected for staff member");
                    }

                    // Sync with DoctorAvailability if this is a doctor
                    if (StaffMember.Role == "Doctor" || roleToAssign == "Doctor")
                    {
                        _logger.LogInformation($"Syncing DoctorAvailability for new doctor {user.Id} with working days: {StaffMember.WorkingDays}");
                        await SyncDoctorAvailabilityAsync(StaffMember, user.Id);
                    }

                    await transaction.CommitAsync();

                    // Log audit trail
                    await _auditTrail.LogAsync(
                        "Create",
                        $"Created staff member: {user.Email}",
                        "ApplicationUser",
                        user.Id,
                        null,
                        JsonConvert.SerializeObject(new {
                            Email = user.Email,
                            FullName = user.FullName,
                            Role = StaffMember.Role,
                            Position = StaffMember.Position,
                            Department = StaffMember.Department
                        }),
                        $"Admin created new {StaffMember.Role} account"
                    );

                    _logger.LogInformation("Successfully created staff member with ID {StaffMemberId}", StaffMember.Id);
                    TempData["SuccessMessage"] = "Staff member created successfully.";
                    return RedirectToPage("/Admin/AdminDashboard");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during staff member creation transaction");
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating staff member");
                
                // Check if it's a database connection issue
                if (ex.Message.Contains("transport-level error") || ex.Message.Contains("connection attempt failed") || ex.Message.Contains("timeout"))
                {
                    ModelState.AddModelError(string.Empty, "Unable to connect to the database. Please check your internet connection and try again. If the problem persists, contact your system administrator.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "An error occurred while creating the staff member. Please try again.");
                }
                
                await OnGetAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostGrantEssentialPermissionsAsync()
        {
            try
            {
                var role = NormalizeRoleName(StaffMember.Role);
                if (string.IsNullOrEmpty(role))
                {
                    return new JsonResult(new { success = false, message = "Please select a role first." });
                }

                var essentialPermissions = await GetEssentialPermissionsForRoleAsync(role);
                return new JsonResult(new { success = true, permissions = essentialPermissions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting essential permissions");
                return new JsonResult(new { success = false, message = "Error getting essential permissions." });
            }
        }

        private async Task<List<int>> GetEssentialPermissionsForRoleAsync(string role)
        {
            var permissions = await _context.Permissions.ToListAsync();
            var essentialPermissions = new List<int>();

            switch (role.ToLower())
            {
                case "admin":
                case "admin staff":
                    essentialPermissions.AddRange(permissions
                        .Where(p => new[] { 
                            "Access Dashboard",
                            "Manage Permissions",
                            "Manage Users",
                            "Manage Medical Records",
                            "View Medical Records",
                            "Approve Users",
                            "Delete Users",
                            "Create Appointments",
                            "View Appointments"
                        }.Contains(p.Name))
                        .Select(p => p.Id));
                    break;

                case "doctor":
                case "head doctor":
                    essentialPermissions.AddRange(permissions
                        .Where(p => new[] {
                            "DoctorDashboard", "Consultation", "PatientRecords", "PatientList", "Reports"
                        }.Contains(p.Name))
                        .Select(p => p.Id));
                    break;

                case "nurse":
                case "head nurse":
                    essentialPermissions.AddRange(permissions
                        .Where(p => new[] {
                            "NurseDashboard", "PatientList", "Appointments", "VitalSigns", "PatientQueue"
                        }.Contains(p.Name))
                        .Select(p => p.Id));
                    break;

                case "receptionist":
                    essentialPermissions.AddRange(permissions
                        .Where(p => new[] {
                            "Access Dashboard",
                            "Appointments"
                        }.Contains(p.Name))
                        .Select(p => p.Id));
                    break;

                case "it":
                    essentialPermissions.AddRange(permissions
                        .Where(p => new[] {
                            "Access Dashboard",
                            "Manage Users"
                        }.Contains(p.Name))
                        .Select(p => p.Id));
                    break;

                default:
                    essentialPermissions.AddRange(permissions
                        .Where(p => new[] {
                            "Access Dashboard"
                        }.Contains(p.Name))
                        .Select(p => p.Id));
                    break;
            }

            return essentialPermissions;
        }

        private static string NormalizeRoleName(string role)
        {
            if (string.IsNullOrWhiteSpace(role)) return role;
            var r = role.Trim();
            if (string.Equals(r, "nurse", StringComparison.OrdinalIgnoreCase)) return "Nurse";
            if (string.Equals(r, "doctor", StringComparison.OrdinalIgnoreCase)) return "Doctor";
            if (string.Equals(r, "admin", StringComparison.OrdinalIgnoreCase)) return "Admin";
            if (string.Equals(r, "admin staff", StringComparison.OrdinalIgnoreCase) || string.Equals(r, "adminstaff", StringComparison.OrdinalIgnoreCase) || string.Equals(r, "staff", StringComparison.OrdinalIgnoreCase)) return "Admin Staff";
            // Fallback: capitalize first letter
            return char.ToUpper(r[0]) + r.Substring(1);
        }

        private async Task SyncDoctorAvailabilityAsync(StaffMember staffMember, string userId)
        {
            try
            {
                // Find or create DoctorAvailability record
                var doctorAvailability = await _context.DoctorAvailabilities
                    .FirstOrDefaultAsync(da => da.DoctorId == userId);

                if (doctorAvailability == null)
                {
                    // Create new DoctorAvailability record
                    doctorAvailability = new DoctorAvailability
                    {
                        DoctorId = userId,
                        IsAvailable = staffMember.IsActive,
                        LastUpdated = DateTime.Now
                    };
                    _context.DoctorAvailabilities.Add(doctorAvailability);
                }
                else
                {
                    // Update existing record
                    doctorAvailability.IsAvailable = staffMember.IsActive;
                    doctorAvailability.LastUpdated = DateTime.Now;
                }

                // Parse working days from StaffMember
                var workingDays = staffMember.WorkingDays?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => d.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();

                // Update day availability
                doctorAvailability.Monday = workingDays.Contains("Monday");
                doctorAvailability.Tuesday = workingDays.Contains("Tuesday");
                doctorAvailability.Wednesday = workingDays.Contains("Wednesday");
                doctorAvailability.Thursday = workingDays.Contains("Thursday");
                doctorAvailability.Friday = workingDays.Contains("Friday");
                doctorAvailability.Saturday = workingDays.Contains("Saturday");
                doctorAvailability.Sunday = workingDays.Contains("Sunday");

                // Parse working hours from StaffMember
                if (!string.IsNullOrEmpty(staffMember.WorkingHours))
                {
                    var timeMatch = Regex.Match(
                        staffMember.WorkingHours, 
                        @"(\d{1,2}):(\d{2})\s*(AM|PM)?\s*-\s*(\d{1,2}):(\d{2})\s*(AM|PM)?", 
                        RegexOptions.IgnoreCase);

                    if (timeMatch.Success)
                    {
                        var startHour = int.Parse(timeMatch.Groups[1].Value);
                        var startMinute = int.Parse(timeMatch.Groups[2].Value);
                        var startPeriod = timeMatch.Groups[3].Value.ToUpper();
                        var endHour = int.Parse(timeMatch.Groups[4].Value);
                        var endMinute = int.Parse(timeMatch.Groups[5].Value);
                        var endPeriod = timeMatch.Groups[6].Value.ToUpper();

                        // Convert to 24-hour format
                        if (startPeriod == "PM" && startHour != 12) startHour += 12;
                        else if (startPeriod == "AM" && startHour == 12) startHour = 0;

                        if (endPeriod == "PM" && endHour != 12) endHour += 12;
                        else if (endPeriod == "AM" && endHour == 12) endHour = 0;

                        doctorAvailability.StartTime = new TimeSpan(startHour, startMinute, 0);
                        doctorAvailability.EndTime = new TimeSpan(endHour, endMinute, 0);
                    }
                }
                
                // Set max daily patients if provided
                if (staffMember.MaxDailyPatients > 0)
                {
                    doctorAvailability.MaxAppointmentsPerDay = staffMember.MaxDailyPatients;
                }

                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Synced DoctorAvailability for doctor {userId}: " +
                    $"Mon={doctorAvailability.Monday}, Tue={doctorAvailability.Tuesday}, " +
                    $"Wed={doctorAvailability.Wednesday}, Thu={doctorAvailability.Thursday}, " +
                    $"Fri={doctorAvailability.Friday}, Sat={doctorAvailability.Saturday}, " +
                    $"Sun={doctorAvailability.Sunday}, Hours={doctorAvailability.StartTime}-{doctorAvailability.EndTime}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error syncing DoctorAvailability for staff member {staffMember.Id}");
            }
        }

        private void GenerateTimeSlots()
        {
            TimeSlots.Clear();
            
            // Generate time slots from 8:00 AM to 5:00 PM in 30-minute intervals
            for (int hour = 8; hour <= 17; hour++)
            {
                for (int minute = 0; minute < 60; minute += 30)
                {
                    if (hour == 17 && minute > 0) break; // Don't go past 5:00 PM
                    
                    string period = hour >= 12 ? "PM" : "AM";
                    int displayHour = hour > 12 ? hour - 12 : (hour == 0 ? 12 : hour);
                    string timeString = $"{displayHour}:{minute:D2} {period}";
                    TimeSlots.Add(timeString);
                }
            }
        }
    }
}