using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Barangay.Pages.Account
{
    public class RegisterConfirmationModel : PageModel
    {
        public bool AutoApproved { get; set; }
        public string? VerifiedBarangay { get; set; }

        public void OnGet(string? auto)
        {
            // Check if this was an auto-approved registration
            AutoApproved = auto == "true" || (TempData["AutoApproved"] != null && (bool)TempData["AutoApproved"]);
            
            // Get verified barangay from TempData if available
            if (TempData["VerifiedBarangay"] != null)
            {
                VerifiedBarangay = TempData["VerifiedBarangay"].ToString();
            }
        }
    }
}
