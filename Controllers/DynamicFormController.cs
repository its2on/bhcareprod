using Microsoft.AspNetCore.Mvc;
using Barangay.Services;
using System.Text.Json;

namespace Barangay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DynamicFormController : ControllerBase
    {
        private readonly IDynamicFormService _formService;

        public DynamicFormController(IDynamicFormService formService)
        {
            _formService = formService;
        }

        [HttpPost("Submit")]
        public async Task<IActionResult> Submit([FromBody] DynamicFormSubmissionRequest request)
        {
            try
            {
                if (request.FormTemplateId <= 0 || request.FormData == null)
                {
                    return BadRequest(new { success = false, message = "Invalid form data" });
                }

                var userId = User.Identity?.IsAuthenticated == true ? User.Identity.Name : null;
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                var submission = await _formService.SaveSubmissionAsync(
                    request.FormTemplateId,
                    userId,
                    request.FormData,
                    ipAddress,
                    userAgent
                );

                return Ok(new 
                { 
                    success = true, 
                    message = "Form submitted successfully",
                    submissionId = submission.FormSubmissionId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = $"Error submitting form: {ex.Message}" 
                });
            }
        }

        [HttpGet("GetForm/{formKey}")]
        public async Task<IActionResult> GetForm(string formKey)
        {
            try
            {
                var form = await _formService.GetFormByKeyAsync(formKey);

                if (form == null)
                {
                    return NotFound(new { success = false, message = "Form not found" });
                }

                return Ok(new { success = true, form });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = $"Error retrieving form: {ex.Message}" 
                });
            }
        }
    }

    public class DynamicFormSubmissionRequest
    {
        public int FormTemplateId { get; set; }
        public Dictionary<string, string> FormData { get; set; } = new Dictionary<string, string>();
    }
}
