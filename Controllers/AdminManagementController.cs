using Barangay.Data;
using Barangay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Barangay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,System Administrator")] // Only admins can access this controller
    public class AdminManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminManagementController> _logger;

        public AdminManagementController(ApplicationDbContext context, ILogger<AdminManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Force removes a family record and all its associations
        /// </summary>
        /// <param name="familyNumber">The family number to remove</param>
        [HttpDelete("family/{familyNumber}")]
        [Authorize(Roles = "Admin,System Administrator")] // Double check authorization
        public async Task<IActionResult> ForceRemoveFamily(string familyNumber)
        {
            if (string.IsNullOrEmpty(familyNumber))
            {
                return BadRequest("Family number is required");
            }

            _logger.LogWarning("ADMIN ACTION: Force removing family {FamilyNumber} by user {UserId}", 
                familyNumber, User.Identity?.Name ?? "Unknown");

            try
            {
                // Begin transaction to ensure all or nothing operations
                using var transaction = await _context.Database.BeginTransactionAsync();

                // 1. Find all users with this family number
                var users = await _context.Users
                    .Where(u => u.FamilyNumber == familyNumber)
                    .ToListAsync();
                
                _logger.LogInformation("Found {Count} users with family number {FamilyNumber}", 
                    users.Count, familyNumber);

                // 2. Update ApplicationUser records
                foreach (var user in users)
                {
                    user.FamilyNumber = null;
                    // Make sure UpdatedAt is assigned with the correct type
                    if (typeof(ApplicationUser).GetProperty("UpdatedAt")?.PropertyType == typeof(string))
                    {
                        user.GetType().GetProperty("UpdatedAt")?.SetValue(user, DateTime.UtcNow.ToString());
                    }
                    else
                    {
                        user.GetType().GetProperty("UpdatedAt")?.SetValue(user, DateTime.UtcNow);
                    }
                    _logger.LogInformation("Removed family number from user {UserId}", user.Id);
                }
                await _context.SaveChangesAsync();

                // 3. Update Patient records
                var patients = await _context.Patients
                    .Where(p => p.FamilyNumber == familyNumber)
                    .ToListAsync();
                
                foreach (var patient in patients)
                {
                    patient.FamilyNumber = null;
                    
                    // Handle UpdatedAt with reflection to manage type safely
                    if (typeof(Patient).GetProperty("UpdatedAt")?.PropertyType == typeof(string))
                    {
                        patient.GetType().GetProperty("UpdatedAt")?.SetValue(patient, DateTime.UtcNow.ToString());
                    }
                    else
                    {
                        patient.GetType().GetProperty("UpdatedAt")?.SetValue(patient, DateTime.UtcNow);
                    }
                    
                    _logger.LogInformation("Removed family number from patient {PatientId}", patient.UserId);
                }
                await _context.SaveChangesAsync();

                // 4. Delete FamilyMember records
                var familyMembers = await _context.FamilyMembers
                    .Where(fm => fm.FamilyNumber == familyNumber)
                    .ToListAsync();
                
                _context.FamilyMembers.RemoveRange(familyMembers);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Removed {Count} family member records", familyMembers.Count);

                // 5. Update NCDRiskAssessments
                var ncdAssessments = await _context.NCDRiskAssessments
                    .Where(n => n.FamilyNo == familyNumber)
                    .ToListAsync();
                
                foreach (var assessment in ncdAssessments)
                {
                    assessment.FamilyNo = null;
                    
                    // Handle UpdatedAt with reflection to manage type safely
                    if (assessment.GetType().GetProperty("UpdatedAt")?.PropertyType == typeof(string))
                    {
                        assessment.GetType().GetProperty("UpdatedAt")?.SetValue(assessment, DateTime.UtcNow.ToString());
                    }
                    else
                    {
                        assessment.GetType().GetProperty("UpdatedAt")?.SetValue(assessment, DateTime.UtcNow);
                    }
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated {Count} NCD risk assessments", ncdAssessments.Count);

                // 6. Update HEEADSSSAssessments
                var heeadsssAssessments = await _context.HEEADSSSAssessments
                    .Where(h => h.FamilyNo == familyNumber)
                    .ToListAsync();
                
                foreach (var assessment in heeadsssAssessments)
                {
                    assessment.FamilyNo = null;
                    
                    // Handle UpdatedAt with reflection to manage type safely
                    if (assessment.GetType().GetProperty("UpdatedAt")?.PropertyType == typeof(string))
                    {
                        assessment.GetType().GetProperty("UpdatedAt")?.SetValue(assessment, DateTime.UtcNow.ToString());
                    }
                    else
                    {
                        assessment.GetType().GetProperty("UpdatedAt")?.SetValue(assessment, DateTime.UtcNow);
                    }
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated {Count} HEEADSSS assessments", heeadsssAssessments.Count);

                // 7. Update Appointments
                var appointments = await _context.Appointments
                    .Where(a => a.FamilyNumber == familyNumber)
                    .ToListAsync();
                
                foreach (var appointment in appointments)
                {
                    appointment.FamilyNumber = null;
                    
                    // Handle UpdatedAt with reflection to manage type safely
                    if (appointment.GetType().GetProperty("UpdatedAt")?.PropertyType == typeof(string))
                    {
                        appointment.GetType().GetProperty("UpdatedAt")?.SetValue(appointment, DateTime.UtcNow.ToString());
                    }
                    else
                    {
                        appointment.GetType().GetProperty("UpdatedAt")?.SetValue(appointment, DateTime.UtcNow);
                    }
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated {Count} appointments", appointments.Count);

                // Commit all changes
                await transaction.CommitAsync();
                
                _logger.LogWarning("ADMIN ACTION COMPLETED: Successfully removed family {FamilyNumber}", familyNumber);

                return Ok(new { 
                    message = $"Successfully removed family {familyNumber}",
                    usersUpdated = users.Count,
                    patientsUpdated = patients.Count,
                    familyMembersRemoved = familyMembers.Count,
                    assessmentsUpdated = ncdAssessments.Count + heeadsssAssessments.Count,
                    appointmentsUpdated = appointments.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing family {FamilyNumber}", familyNumber);
                return StatusCode(500, new { message = $"Error removing family: {ex.Message}" });
            }
        }
    }
}
