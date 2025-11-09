using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Barangay.Pages.Doctor
{
    /// <summary>
    /// Create NCD Assessment page for Doctors/Admin
    /// This page redirects to SubmitForm.cshtml with the NCD form key.
    /// SubmitForm.cshtml will automatically load role-specific forms:
    /// - Doctors/Admin: ncd-risk-assessment-form-doctor (if exists) or ncd-risk-assessment-form
    /// - Nurses: ncd-risk-assessment-form-nurse (if exists) or ncd-risk-assessment-form
    /// - Patients: ncd-risk-assessment-form
    /// </summary>
    [Authorize(Roles = "Doctor,Head Doctor,Admin")]
    public class CreateNCDAssessmentModel : PageModel
    {
        private readonly ILogger<CreateNCDAssessmentModel> _logger;

        public CreateNCDAssessmentModel(ILogger<CreateNCDAssessmentModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet(int? appointmentId)
            {
                if (appointmentId == null)
            {
                _logger.LogWarning("Appointment ID not provided to CreateNCDAssessment");
                TempData["StatusMessage"] = "Error: Appointment ID must be provided.";
                return RedirectToPage("/Doctor/Consultations");
            }

            // Redirect to SubmitForm.cshtml - it will handle role-specific form loading
            // Form key: ncd-risk-assessment-form
            // SubmitForm will automatically check for:
            // - ncd-risk-assessment-form-doctor (for doctors/admin)
            // - ncd-risk-assessment-form-nurse (for nurses)
            // - ncd-risk-assessment-form (fallback for all)
            _logger.LogInformation("Redirecting to SubmitForm for NCD Assessment. AppointmentId: {AppointmentId}", appointmentId);
            
            return RedirectToPage("/Forms/SubmitForm", new { formKey = "ncd-risk-assessment-form", appointmentId = appointmentId });
        }
    }
} 
