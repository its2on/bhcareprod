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
    [Authorize] // Basic authorization for all endpoints
    public class FamiliesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FamiliesController> _logger;

        public FamiliesController(ApplicationDbContext context, ILogger<FamiliesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get family details by family number
        /// </summary>
        /// <param name="familyNumber">Family number to look up</param>
        [HttpGet("details/{familyNumber}")]
        public async Task<IActionResult> GetFamilyDetails(string familyNumber)
        {
            if (string.IsNullOrEmpty(familyNumber))
            {
                return BadRequest("Family number is required");
            }

            try
            {
                // Find all patients with this family number
                var patients = await _context.Patients
                    .Include(p => p.User)
                    .Where(p => p.FamilyNumber == familyNumber)
                    .ToListAsync();
                
                if (!patients.Any())
                {
                    // Check ApplicationUser records as fallback
                    var users = await _context.Users
                        .Where(u => u.FamilyNumber == familyNumber)
                        .ToListAsync();
                    
                    if (!users.Any())
                    {
                        return NotFound($"No family found with number {familyNumber}");
                    }
                    
                    // Return basic info from users
                    return Ok(new
                    {
                        familyNumber = familyNumber,
                        memberCount = users.Count,
                        barangay = users.FirstOrDefault()?.Barangay ?? "Unknown",
                        contactNumber = users.FirstOrDefault()?.PhoneNumber ?? "None",
                        members = users.Select(u => new 
                        { 
                            name = u.FullName,
                            age = u.BirthDate.HasValue ? CalculateAge(u.BirthDate.Value) : 0,
                            status = u.Status
                        }).ToList()
                    });
                }
                
                // Get all members with the same family number
                var familyMembers = await _context.FamilyMembers
                    .Where(fm => fm.FamilyNumber == familyNumber)
                    .ToListAsync();
                
                // Get primary patient
                var primaryPatient = patients.FirstOrDefault();
                
                return Ok(new
                {
                    familyNumber = familyNumber,
                    memberCount = patients.Count + familyMembers.Count,
                    barangay = primaryPatient?.User?.Barangay ?? "Unknown",
                    contactNumber = primaryPatient?.User?.PhoneNumber ?? "None",
                    members = patients.Select(p => new 
                    { 
                        name = p.FullName,
                        age = p.Age.ToString(),
                        status = p.User?.Status ?? "Unknown"
                    }).Concat(familyMembers.Select(fm => new 
                    {
                        name = fm.Name,
                        age = fm.Age.ToString(),
                        status = "Family Member"
                    })).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving family details for {FamilyNumber}", familyNumber);
                return StatusCode(500, new { message = $"Error retrieving family details: {ex.Message}" });
            }
        }
        
        
        private static int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}
