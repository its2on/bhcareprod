using Barangay.Models;
using Barangay.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Barangay.Controllers
{
    /// <summary>
    /// API Controller for atomic family number generation
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FamilyNumberController : ControllerBase
    {
        private readonly IFamilyNumberService _familyNumberService;
        private readonly ILogger<FamilyNumberController> _logger;

        public FamilyNumberController(
            IFamilyNumberService familyNumberService,
            ILogger<FamilyNumberController> logger)
        {
            _familyNumberService = familyNumberService;
            _logger = logger;
        }

        /// <summary>
        /// Generates a new family number atomically
        /// </summary>
        /// <param name="request">Family number generation request</param>
        /// <returns>Generated family number response</returns>
        [HttpPost("generate")]
        public async Task<ActionResult<GenerateFamilyNumberResponse>> GenerateFamilyNumber(
            [FromBody] GenerateFamilyNumberRequest request)
        {
            try
            {
                _logger.LogInformation("=== API FAMILY NUMBER GENERATION STARTED ===");
                _logger.LogInformation("Request: LastName={LastName}, HealthFacility={HealthFacility}, PatientCategory={PatientCategory}",
                    request.LastName, request.HealthFacility, request.PatientCategory);

                if (string.IsNullOrWhiteSpace(request.LastName))
                {
                    return BadRequest(new GenerateFamilyNumberResponse
                    {
                        Success = false,
                        Error = "Last name is required"
                    });
                }

                var result = await _familyNumberService.GenerateFamilyNumberAsync(
                    request.LastName,
                    request.HealthFacility,
                    request.PatientCategory);

                if (result.Success)
                {
                    _logger.LogInformation("=== API FAMILY NUMBER GENERATION COMPLETED SUCCESSFULLY ===");
                    _logger.LogInformation("Generated: {FamilyNumber}", result.FamilyNumber);
                    return Ok(result);
                }
                else
                {
                    _logger.LogError("Family number generation failed: {Error}", result.Error);
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in family number generation API");
                return StatusCode(500, new GenerateFamilyNumberResponse
                {
                    Success = false,
                    Error = "Internal server error occurred while generating family number"
                });
            }
        }

        /// <summary>
        /// Gets the next family number for a specific prefix
        /// </summary>
        /// <param name="prefix">The prefix to get the next number for</param>
        /// <returns>Next family number</returns>
        [HttpGet("next/{prefix}")]
        public async Task<ActionResult<string>> GetNextFamilyNumber(string prefix)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prefix))
                {
                    return BadRequest("Prefix is required");
                }

                var familyNumber = await _familyNumberService.GetNextFamilyNumberAsync(prefix);
                return Ok(familyNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next family number for prefix: {Prefix}", prefix);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Validates a family number format and existence
        /// </summary>
        /// <param name="familyNumber">Family number to validate</param>
        /// <returns>Validation result</returns>
        [HttpGet("validate/{familyNumber}")]
        public async Task<ActionResult<bool>> ValidateFamilyNumber(string familyNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(familyNumber))
                {
                    return BadRequest("Family number is required");
                }

                var isValid = await _familyNumberService.ValidateFamilyNumberAsync(familyNumber);
                return Ok(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating family number: {FamilyNumber}", familyNumber);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}

