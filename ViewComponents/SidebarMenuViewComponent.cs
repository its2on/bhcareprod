using Barangay.Models;
using Barangay.Services;
using Barangay.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Barangay.ViewComponents
{
    public class SidebarMenuViewComponent : ViewComponent
    {
        private readonly IPermissionService _permissionService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SidebarMenuViewComponent> _logger;
        private readonly ApplicationDbContext _context;

        public SidebarMenuViewComponent(
            IPermissionService permissionService,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor httpContextAccessor,
            ILogger<SidebarMenuViewComponent> logger,
            ApplicationDbContext context)
        {
            _permissionService = permissionService;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(string role)
        {
            if (!User.Identity.IsAuthenticated)
                return View("Default", new List<SidebarMenuItem>());

            var claimsPrincipal = User as ClaimsPrincipal;
            if (claimsPrincipal == null)
                return View("Default", new List<SidebarMenuItem>());
                
            var userId = _userManager.GetUserId(claimsPrincipal);
            if (string.IsNullOrEmpty(userId))
                return View("Default", new List<SidebarMenuItem>());

            // Get all permissions for the user
            var userPermissions = await _permissionService.GetUserPermissionsAsync(userId);
            
            // Create a function to check if user has permission (handles both formats)
            bool HasPermission(params string[] permissions)
            {
                foreach (var permissionName in permissions)
                {
                    if (userPermissions.Contains(permissionName, StringComparer.OrdinalIgnoreCase))
                        return true;

                    // Also check for Category:Name format
                    foreach (var p in userPermissions)
                    {
                        if (p.EndsWith($":{permissionName}", StringComparison.OrdinalIgnoreCase) || 
                            (p.Contains(":") && p.Split(':')[1].Equals(permissionName, StringComparison.OrdinalIgnoreCase)))
                            return true;
                    }
                }
                return false;
            }
            
            // Get current path to determine active item
            var currentPath = _httpContextAccessor.HttpContext?.Request.Path.Value?.ToLower() ?? "";

            // Define navigation items based on role and check against permissions
            var navItems = new List<SidebarMenuItem>();

            switch (role?.ToLower())
            {
                case "nurse":
                    // Only show dashboard if user has NurseDashboard permission
                    if (HasPermission("NurseDashboard"))
                    {
                        navItems.Add(new SidebarMenuItem { 
                            Text = "Dashboard", 
                            Icon = "tachometer-alt", 
                            Url = "/Nurse/NurseDashboard", 
                            RequiredPermissions = new List<string> { "NurseDashboard" },
                            IsActive = currentPath.Contains("/nurse/nursedashboard")
                        });
                    }
                    
                    // View Immunization Records
                    var canSeeImmunization = HasPermission("PatientList");
                    _logger.LogInformation($"Nurse {userId}: Immunization access = {canSeeImmunization}");
                    if (canSeeImmunization)
                    {
                        navItems.Add(new SidebarMenuItem { 
                            Text = "Immunization Records", 
                            Icon = "database", 
                            Url = "/Nurse/ImmunizationRecords", 
                            RequiredPermissions = new List<string> { "PatientList" },
                            IsActive = currentPath.Contains("/nurse/immunizationrecords")
                        });
                    }
                    
                    var canSeeAppointments = HasPermission("Appointments");
                    _logger.LogInformation($"Nurse {userId}: Appointments visible = {canSeeAppointments}");
                    if (canSeeAppointments)
                    {
                        // Get today's pending/confirmed appointments count
                        var today = DateTime.Today;
                        var todaysAppointmentsCount = await _context.Appointments
                            .CountAsync(a => a.AppointmentDate.Date == today && 
                                           (a.Status == Models.AppointmentStatus.Pending || 
                                            a.Status == Models.AppointmentStatus.Confirmed));
                        
                        navItems.Add(new SidebarMenuItem { 
                            Text = "Appointments", 
                            Icon = "calendar-check", 
                            Url = "/Nurse/Appointments", 
                            RequiredPermissions = new List<string> { "Appointments" },
                            IsActive = currentPath.Contains("/nurse/appointments"),
                            BadgeCount = todaysAppointmentsCount
                        });
                    }
                    
                    // Removed 'Vitals' menu item from nurse sidebar per requirement
                    
                    // Removed separate 'Record Vitals' menu item; recording is handled within Vitals page

                    // Removed 'Patient History' from nurse sidebar per requirement
                    
                    // Removed 'Patient Queue' from nurse sidebar per requirement

                    // Only show Reports if user has View Reports permission
                    if (HasPermission("View Reports", "Generate Reports"))
                    {
                        navItems.Add(new SidebarMenuItem { 
                            Text = "Reports", 
                            Icon = "chart-bar", 
                            Url = "/Nurse/Reports", 
                            RequiredPermissions = new List<string> { "View Reports", "Generate Reports" },
                            IsActive = currentPath.Contains("/nurse/reports")
                        });
                    }
                    
                    // Only show Notifications if user has View Notifications permission
                    if (HasPermission("View Notifications"))
                    {
                        navItems.Add(new SidebarMenuItem { 
                            Text = "Notifications", 
                            Icon = "bell", 
                            Url = "/Nurse/Notifications", 
                            RequiredPermissions = new List<string> { "View Notifications" },
                            IsActive = currentPath.Contains("/nurse/notifications")
                        });
                    }
                    
// Settings - Allow nurses to access their settings
navItems.Add(new SidebarMenuItem { 
    Text = "Settings", 
    Icon = "cog", 
    Url = "/Nurse/Settings", 
    RequiredPermissions = new List<string>(),
    IsActive = currentPath.Contains("/nurse/settings")
});

break;

                case "doctor":
                    // Doctor simplified permissions - Check multiple permission variations
                    if (HasPermission("DoctorDashboard", "Access Doctor Dashboard", "Dashboard Access"))
                    {
                        navItems.Add(new SidebarMenuItem {
                            Text = "Dashboard",
                            Icon = "tachometer-alt",
                            Url = "/Doctor/DoctorDashboard",
                            RequiredPermissions = new List<string> { "DoctorDashboard", "Access Doctor Dashboard" },
                            IsActive = currentPath.Contains("/doctor/doctordashboard")
                        });
                    }

                    if (HasPermission("Consultation", "Manage Consultations"))
                    {
                        navItems.Add(new SidebarMenuItem {
                            Text = "Consultation",
                            Icon = "stethoscope",
                            Url = "/Doctor/Consultation",
                            RequiredPermissions = new List<string> { "Consultation", "Manage Consultations" },
                            IsActive = currentPath.Contains("/doctor/consultation")
                        });
                    }

                    if (HasPermission("PatientList", "View Patient Details", "View Patients"))
                    {
                        navItems.Add(new SidebarMenuItem {
                            Text = "Patient List",
                            Icon = "user-injured",
                            Url = "/Doctor/PatientList",
                            RequiredPermissions = new List<string> { "PatientList", "View Patient Details", "View Patients" },
                            IsActive = currentPath.Contains("/doctor/patientlist")
                        });
                    }

                    if (HasPermission("Reports", "View Reports", "Reporting"))
                    {
                        navItems.Add(new SidebarMenuItem {
                            Text = "Reports",
                            Icon = "chart-bar",
                            Url = "/Doctor/Reports",
                            RequiredPermissions = new List<string> { "Reports", "View Reports", "Reporting" },
                            IsActive = currentPath.Contains("/doctor/reports")
                        });
                    }

                    // Settings - Allow doctors to access their settings
                    navItems.Add(new SidebarMenuItem { 
                        Text = "Settings", 
                        Icon = "cog", 
                        Url = "/Doctor/Settings", 
                        RequiredPermissions = new List<string>(),
                        IsActive = currentPath.Contains("/doctor/settings")
                    });
                    
                    break;

                case "admin":
                    // Admin dashboard
                    navItems.Add(new SidebarMenuItem {
                        Text = "Dashboard",
                        Icon = "tachometer-alt",
                        Url = "/Admin/AdminDashboard",
                        RequiredPermissions = new List<string> { "AdminDashboard" },
                        IsActive = currentPath.Contains("/admin/admindashboard")
                    });

                    // User Management
                    navItems.Add(new SidebarMenuItem {
                        Text = "User Management",
                        Icon = "users",
                        Url = "/Admin/UserManagement",
                        RequiredPermissions = new List<string> { "UserManagement" },
                        IsActive = currentPath.Contains("/admin/usermanagement")
                    });

                    // Immunization Archive (Admin only)
                    navItems.Add(new SidebarMenuItem {
                        Text = "Immunization Archive",
                        Icon = "archive",
                        Url = "/Admin/ImmunizationArchive",
                        RequiredPermissions = new List<string> { "AdminAccess" },
                        IsActive = currentPath.Contains("/admin/immunizationarchive")
                    });

                    // System Tools
                    navItems.Add(new SidebarMenuItem {
                        Text = "System Tools",
                        Icon = "cogs",
                        Url = "/Admin/SystemTools",
                        RequiredPermissions = new List<string> { "SystemTools" },
                        IsActive = currentPath.Contains("/admin/systemtools")
                    });
                    break;
            }

            // Add cache busting to ensure fresh data
            ViewBag.CacheBuster = DateTime.UtcNow.Ticks;
            return View("Default", navItems);
        }
    }

    public class SidebarMenuItem
    {
        public string Text { get; set; }
        public string Icon { get; set; }
        public string Url { get; set; }
        public List<string> RequiredPermissions { get; set; } = new List<string>();
        public bool IsActive { get; set; }
        public int BadgeCount { get; set; } = 0;
    }
} 