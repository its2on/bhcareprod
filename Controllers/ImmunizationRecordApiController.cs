using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using Barangay.Extensions;
using System;
using System.Threading.Tasks;

namespace Barangay.Controllers
{
    [Route("api/immunization")]
    [ApiController]
    [AllowAnonymous] // TEMPORARY BYPASS: No authentication required
    public class ImmunizationRecordApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ImmunizationRecordApiController> _logger;
        private readonly IDataEncryptionService _encryptionService;

        public ImmunizationRecordApiController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ImmunizationRecordApiController> logger,
            IDataEncryptionService encryptionService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _encryptionService = encryptionService;
        }

        [HttpPost("save")]
        public async Task<IActionResult> SaveImmunizationRecord([FromBody] ImmunizationRecordDto dto)
        {
            try
            {
                _logger.LogInformation("API BYPASS: Saving immunization record for appointment {AppointmentId}", dto.AppointmentId);

                // Get user ID with fallback
                string userId = "system-bypass";
                try
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser != null)
                    {
                        userId = currentUser.Id;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "API BYPASS: Could not get user, using system-bypass");
                }

                // Log received data for debugging
                _logger.LogInformation("API BYPASS: Received data - ChildFirstName: {First}, ChildLastName: {Last}, DOB: {DOB}, Sex: {Sex}, Barangay: {Barangay}", 
                    dto.ChildFirstName, dto.ChildLastName, dto.DateOfBirth, dto.Sex, dto.Barangay);

                // Validate required fields with detailed error messages
                var errors = new List<string>();
                
                if (string.IsNullOrWhiteSpace(dto.ChildFirstName))
                    errors.Add("Child's first name is required");
                    
                if (string.IsNullOrWhiteSpace(dto.ChildLastName))
                    errors.Add("Child's last name is required");
                    
                if (string.IsNullOrWhiteSpace(dto.DateOfBirth))
                    errors.Add("Date of birth is required");
                    
                if (string.IsNullOrWhiteSpace(dto.Sex))
                    errors.Add("Sex is required");
                    
                if (string.IsNullOrWhiteSpace(dto.Barangay))
                    errors.Add("Barangay is required");
                
                if (errors.Any())
                {
                    var errorMessage = string.Join(", ", errors);
                    _logger.LogWarning("API BYPASS: Validation failed - {Errors}", errorMessage);
                    return BadRequest(new { success = false, message = errorMessage, errors = errors });
                }

                // Construct names
                var childFullName = $"{dto.ChildFirstName} {dto.ChildMiddleName} {dto.ChildLastName}".Replace("  ", " ").Trim();
                var motherFullName = $"{dto.MotherFirstName} {dto.MotherMiddleName} {dto.MotherLastName}".Replace("  ", " ").Trim();
                var fatherFullName = $"{dto.FatherFirstName} {dto.FatherMiddleName} {dto.FatherLastName}".Replace("  ", " ").Trim();

                // Create immunization record
                var immunizationRecord = new ImmunizationRecord
                {
                    ChildName = childFullName,
                    DateOfBirth = dto.DateOfBirth,
                    PlaceOfBirth = dto.PlaceOfBirth ?? string.Empty,
                    Address = dto.Address ?? string.Empty,
                    MotherName = motherFullName,
                    FatherName = fatherFullName,
                    Sex = dto.Sex,
                    BirthHeight = dto.BirthHeight ?? string.Empty,
                    BirthWeight = dto.BirthWeight ?? string.Empty,
                    HealthCenter = dto.HealthCenter ?? "Barangay Health Care Center",
                    Barangay = dto.Barangay,
                    FamilyNumber = dto.FamilyNumber ?? string.Empty,
                    Email = string.Empty,
                    ContactNumber = dto.ContactNumber ?? string.Empty,
                    
                    // Vaccine dates and remarks
                    BCGVaccineDate = dto.BCGVaccineDate,
                    BCGVaccineRemarks = dto.BCGVaccineRemarks,
                    HepatitisBVaccineDate = dto.HepBBirthDate,
                    HepatitisBVaccineRemarks = dto.HepBBirthRemarks,
                    Pentavalent1Date = dto.Pentavalent1Date,
                    Pentavalent1Remarks = dto.Pentavalent1Remarks,
                    Pentavalent2Date = dto.Pentavalent2Date,
                    Pentavalent2Remarks = dto.Pentavalent2Remarks,
                    Pentavalent3Date = dto.Pentavalent3Date,
                    Pentavalent3Remarks = dto.Pentavalent3Remarks,
                    OPV1Date = dto.OPV1Date,
                    OPV1Remarks = dto.OPV1Remarks,
                    OPV2Date = dto.OPV2Date,
                    OPV2Remarks = dto.OPV2Remarks,
                    OPV3Date = dto.OPV3Date,
                    OPV3Remarks = dto.OPV3Remarks,
                    IPV1Date = dto.IPV1Date,
                    IPV1Remarks = dto.IPV1Remarks,
                    IPV2Date = dto.IPV2Date,
                    IPV2Remarks = dto.IPV2Remarks,
                    PCV1Date = dto.PCV1Date,
                    PCV1Remarks = dto.PCV1Remarks,
                    PCV2Date = dto.PCV2Date,
                    PCV2Remarks = dto.PCV2Remarks,
                    PCV3Date = dto.PCV3Date,
                    PCV3Remarks = dto.PCV3Remarks,
                    MMR1Date = dto.MMR1Date,
                    MMR1Remarks = dto.MMR1Remarks,
                    MMR2Date = dto.MMR2Date,
                    MMR2Remarks = dto.MMR2Remarks,
                    
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    CreatedBy = userId,
                    UpdatedBy = userId,
                    Status = "Active"
                };

                // Encrypt sensitive data
                immunizationRecord.EncryptSensitiveData(_encryptionService);

                // Save to database
                _context.ImmunizationRecords.Add(immunizationRecord);
                await _context.SaveChangesAsync();

                _logger.LogInformation("API BYPASS: Immunization record saved successfully with ID {RecordId}", immunizationRecord.Id);

                // Update appointment status
                if (dto.AppointmentId > 0)
                {
                    var appointment = await _context.Appointments.FindAsync(dto.AppointmentId);
                    if (appointment != null)
                    {
                        appointment.Status = AppointmentStatus.Completed;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("API BYPASS: Appointment {AppointmentId} marked as completed", dto.AppointmentId);
                    }
                }

                return Ok(new { success = true, message = "Immunization record saved successfully!", recordId = immunizationRecord.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API BYPASS: Error saving immunization record");
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }

    // DTO for the API
    public class ImmunizationRecordDto
    {
        public int AppointmentId { get; set; }
        public string ChildFirstName { get; set; }
        public string ChildMiddleName { get; set; }
        public string ChildLastName { get; set; }
        public string DateOfBirth { get; set; }
        public string PlaceOfBirth { get; set; }
        public string Sex { get; set; }
        public string BirthWeight { get; set; }
        public string BirthHeight { get; set; }
        public string Address { get; set; }
        public string MotherFirstName { get; set; }
        public string MotherMiddleName { get; set; }
        public string MotherLastName { get; set; }
        public string FatherFirstName { get; set; }
        public string FatherMiddleName { get; set; }
        public string FatherLastName { get; set; }
        public string ContactNumber { get; set; }
        public string HealthCenter { get; set; }
        public string Barangay { get; set; }
        public string FamilyNumber { get; set; }
        
        // Vaccines
        public string BCGVaccineDate { get; set; }
        public string BCGVaccineRemarks { get; set; }
        public string HepBBirthDate { get; set; }
        public string HepBBirthRemarks { get; set; }
        public string Pentavalent1Date { get; set; }
        public string Pentavalent1Remarks { get; set; }
        public string Pentavalent2Date { get; set; }
        public string Pentavalent2Remarks { get; set; }
        public string Pentavalent3Date { get; set; }
        public string Pentavalent3Remarks { get; set; }
        public string OPV1Date { get; set; }
        public string OPV1Remarks { get; set; }
        public string OPV2Date { get; set; }
        public string OPV2Remarks { get; set; }
        public string OPV3Date { get; set; }
        public string OPV3Remarks { get; set; }
        public string IPV1Date { get; set; }
        public string IPV1Remarks { get; set; }
        public string IPV2Date { get; set; }
        public string IPV2Remarks { get; set; }
        public string PCV1Date { get; set; }
        public string PCV1Remarks { get; set; }
        public string PCV2Date { get; set; }
        public string PCV2Remarks { get; set; }
        public string PCV3Date { get; set; }
        public string PCV3Remarks { get; set; }
        public string MMR1Date { get; set; }
        public string MMR1Remarks { get; set; }
        public string MMR2Date { get; set; }
        public string MMR2Remarks { get; set; }
    }
}
