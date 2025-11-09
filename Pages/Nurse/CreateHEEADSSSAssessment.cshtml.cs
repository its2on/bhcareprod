using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Barangay.Pages.Nurse
{
    /// <summary>
    /// Create HEEADSSS Assessment page for Nurses/Doctors/Admin
    /// This page redirects to SubmitForm.cshtml with the HEEADSSS form key.
    /// SubmitForm.cshtml will automatically load role-specific forms:
    /// - Nurses: heeadsss-assessment-nurse (if exists) or heeadsss-assessment
    /// - Doctors/Admin: heeadsss-assessment-doctor (if exists) or heeadsss-assessment
    /// - Patients: heeadsss-assessment
    /// </summary>
    [Authorize(Roles = "Nurse,Head Nurse,Doctor,Head Doctor,Admin")]
    public class CreateHEEADSSSAssessmentModel : PageModel
    {
        private readonly ILogger<CreateHEEADSSSAssessmentModel> _logger;

        public CreateHEEADSSSAssessmentModel(ILogger<CreateHEEADSSSAssessmentModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet(int? appointmentId)
            {
                if (appointmentId == null)
            {
                _logger.LogWarning("Appointment ID not provided to CreateHEEADSSSAssessment");
                TempData["StatusMessage"] = "Error: Appointment ID must be provided.";
                return RedirectToPage("/Nurse/Appointments");
            }

            // Redirect to SubmitForm.cshtml - it will handle role-specific form loading
            // Form key: heeadsss-assessment (or heeadsss-assessment-form)
            // SubmitForm will automatically check for:
            // - heeadsss-assessment-nurse (for nurses)
            // - heeadsss-assessment-doctor (for doctors/admin)
            // - heeadsss-assessment (fallback for all)
            _logger.LogInformation("Redirecting to SubmitForm for HEEADSSS Assessment. AppointmentId: {AppointmentId}", appointmentId);
            
            // Try common form key variations
            return RedirectToPage("/Forms/SubmitForm", new { formKey = "heeadsss-assessment", appointmentId = appointmentId });
        }
    }
}
