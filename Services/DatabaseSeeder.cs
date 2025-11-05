using Barangay.Data;
using Barangay.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Barangay.Services;

namespace Barangay.Services
{
    public class DatabaseSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<DatabaseSeeder> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDataEncryptionService _encryptionService;

        public DatabaseSeeder(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<DatabaseSeeder> logger,
            IConfiguration configuration,
            IDataEncryptionService encryptionService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _configuration = configuration;
            _encryptionService = encryptionService;
        }


        public async Task SeedDoctorAvailabilityAsync()
        {
            try
            {
                // Check if we already have doctor availability records
                var existingRecords = await _context.DoctorAvailabilities.CountAsync();
                if (existingRecords > 0)
                {
                    _logger.LogInformation($"Doctor availability records already exist ({existingRecords} records). Skipping seeding.");
                    return;
                }

                _logger.LogInformation("No doctor availability records found. Creating default availability...");

                // Get all doctors in the system
                var doctors = await _userManager.GetUsersInRoleAsync("Doctor");
                
                if (doctors.Any())
                {
                    // Create availability records for each doctor
                    foreach (var doctor in doctors)
                    {
                        var availability = new DoctorAvailability
                        {
                            DoctorId = doctor.Id,
                            Monday = true,
                            Tuesday = true,
                            Wednesday = true,
                            Thursday = true,
                            Friday = true,
                            Saturday = false,
                            Sunday = false,
                            StartTime = TimeSpan.FromHours(8),  // 8:00 AM
                            EndTime = TimeSpan.FromHours(17),   // 5:00 PM
                            MaxAppointmentsPerDay = 100,        // 100 slots per day
                            SlotDurationMinutes = 5,            // 5 minutes per slot
                            IsAvailable = true,
                            LastUpdated = DateTime.UtcNow
                        };

                        _context.DoctorAvailabilities.Add(availability);
                        _logger.LogInformation($"Created availability record for doctor: {doctor.FirstName} {doctor.LastName}");
                    }
                }
                else
                {
                    // No doctors found. Since DoctorId is required, skip creating a generic record
                    // to avoid inserting a null DoctorId which violates the NOT NULL constraint.
                    _logger.LogWarning("No doctors found in the system. Skipping creation of generic availability to avoid null DoctorId.");
                    return;
                }

                await _context.SaveChangesAsync();
                
                var createdRecords = await _context.DoctorAvailabilities.CountAsync();
                _logger.LogInformation($"Successfully created {createdRecords} doctor availability records.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding doctor availability records.");
                throw;
            }
        }

        private async Task SeedRolesAsync()
        {
            try
            {
                var roles = new[] { "Admin", "Doctor", "Nurse", "Patient", "System Administrator", "Admin Staff" };
                
                foreach (var role in roles)
                {
                    if (!await _roleManager.RoleExistsAsync(role))
                    {
                        var roleResult = await _roleManager.CreateAsync(new IdentityRole(role));
                        if (roleResult.Succeeded)
                        {
                            _logger.LogInformation("Created role: {Role}", role);
                        }
                        else
                        {
                            _logger.LogError("Failed to create role {Role}: {Errors}", role, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Role already exists: {Role}", role);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding roles.");
                throw;
            }
        }



        private async Task SeedAdminUserAsync()
        {
            try
            {
                var adminEmail = _configuration["AdminUser:Email"];
                var adminPassword = _configuration["AdminUser:Password"];
                var adminFullName = _configuration["AdminUser:FullName"];

                if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
                {
                    _logger.LogWarning("Admin user configuration not found in appsettings.json");
                    return;
                }

                // Check if admin user already exists (by email and username)
                var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
                if (existingAdmin == null)
                {
                    // Also check by username in case email lookup fails
                    existingAdmin = await _userManager.FindByNameAsync(adminEmail);
                }
                
                if (existingAdmin != null)
                {
                    _logger.LogInformation("Admin user already exists: {Email}", adminEmail);
                    
                    // Ensure admin user has Admin role
                    if (!await _userManager.IsInRoleAsync(existingAdmin, "Admin"))
                    {
                        await _userManager.AddToRoleAsync(existingAdmin, "Admin");
                        _logger.LogInformation("Added Admin role to existing admin user");
                    }
                    
                    // Ensure admin user is verified and active
                    if (existingAdmin.Status != "Verified" || !existingAdmin.IsActive)
                    {
                        existingAdmin.Status = "Verified";
                        existingAdmin.IsActive = true;
                        await _userManager.UpdateAsync(existingAdmin);
                        _logger.LogInformation("Updated admin user status to Verified and Active");
                    }
                    
                    return;
                }

                // Create new admin user
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = adminFullName ?? "System Administrator",
                    Status = "Verified",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                    _logger.LogInformation("Admin user created successfully: {Email}", adminEmail);
                }
                else
                {
                    // Check if the error is due to duplicate key (user already exists)
                    var duplicateError = result.Errors.FirstOrDefault(e => e.Code == "DuplicateUserName" || e.Description.Contains("duplicate"));
                    if (duplicateError != null)
                    {
                        _logger.LogInformation("Admin user already exists (duplicate key detected): {Email}", adminEmail);
                        
                        // Try to find and update the existing user
                        var existingUser = await _userManager.FindByEmailAsync(adminEmail);
                        if (existingUser != null)
                        {
                            // Ensure admin user has Admin role
                            if (!await _userManager.IsInRoleAsync(existingUser, "Admin"))
                            {
                                await _userManager.AddToRoleAsync(existingUser, "Admin");
                                _logger.LogInformation("Added Admin role to existing admin user");
                            }
                            
                            // Ensure admin user is verified and active
                            if (existingUser.Status != "Verified" || !existingUser.IsActive)
                            {
                                existingUser.Status = "Verified";
                                existingUser.IsActive = true;
                                await _userManager.UpdateAsync(existingUser);
                                _logger.LogInformation("Updated admin user status to Verified and Active");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding admin user.");
                throw;
            }
        }

        public async Task SeedAllAsync()
        {
            await SeedRolesAsync();
            await SeedAdminUserAsync();
            await SeedDoctorAvailabilityAsync();
            // Add other seeding methods here as needed
        }
    }
}