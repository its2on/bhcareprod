using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Barangay.Pages.Doctor
{
    /// <summary>
    /// Doctor edit page for HEEADSSS Assessment
    /// This page redirects to the Nurse edit page which handles both Nurse and Doctor roles.
    /// The Nurse edit page uses IsDoctorRole() to determine layout and navigation.
    /// 
    /// Permission Summary:
    /// - Admin: Can edit all forms for all roles (patient, doctor, nurse)
    /// - Doctor: Can edit forms for their assigned appointments
    /// - Nurse: Can edit forms for their assigned appointments
    /// - Patient: Cannot access edit pages (read-only via view pages)
    /// </summary>
    [Authorize(Roles = "Doctor,Head Doctor,Admin")]
    public class EditHEEADSSSAssessmentModel : PageModel
    {
        public IActionResult OnGet(int appointmentId)
        {
            // Redirect to the nurse edit page with proper layout handling
            // The Nurse edit page handles both Nurse and Doctor roles with IsDoctorRole() check
            return RedirectToPage("/Nurse/EditHEEADSSSAssessment", new { appointmentId = appointmentId });
        }
    }
}
