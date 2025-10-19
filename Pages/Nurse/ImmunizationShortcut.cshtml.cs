using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using System.ComponentModel.DataAnnotations;

namespace Barangay.Pages.Nurse
{
    [Authorize(Roles = "Nurse,Head Nurse")]
    [Authorize(Policy = "PatientList")]
    public class ImmunizationShortcutModel : PageModel
    {
        private readonly EncryptedDbContext _context;
        private readonly ILogger<ImmunizationShortcutModel> _logger;
        private readonly IImmunizationReminderService _immunizationReminderService;

        public ImmunizationShortcutModel(
            EncryptedDbContext context,
            ILogger<ImmunizationShortcutModel> logger,
            IImmunizationReminderService immunizationReminderService)
        {
            _context = context;
            _logger = logger;
            _immunizationReminderService = immunizationReminderService;
        }

        [BindProperty]
        public ImmunizationShortcutForm Form { get; set; } = new();

        public Task<IActionResult> OnGetAsync()
        {
            // Set default values
            Form.CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            Form.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            Form.CreatedBy = User.Identity?.Name ?? "Unknown";
            Form.Status = "Pending";

            return Task.FromResult<IActionResult>(Page());
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Validate that the preferred date is a Wednesday (only if provided)
                if (!string.IsNullOrEmpty(Form.PreferredDate) && DateTime.TryParse(Form.PreferredDate, out DateTime preferredDateTime))
                {
                    if (preferredDateTime.DayOfWeek != DayOfWeek.Wednesday)
                    {
                        ModelState.AddModelError("Form.PreferredDate", "Immunization is only available on Wednesdays. Please select a Wednesday.");
                        return Page();
                    }

                    // Validate that the preferred date is not in the past
                    if (preferredDateTime.Date < DateTime.Today)
                    {
                        ModelState.AddModelError("Form.PreferredDate", "Please select a future date.");
                        return Page();
                    }
                }

                // Set audit fields
                Form.CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                Form.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                Form.CreatedBy = User.Identity?.Name ?? "Unknown";

                _context.ImmunizationShortcutForms.Add(Form);
                await _context.SaveChangesAsync();

                // Send email notification with schedule
                await SendScheduleNotificationEmailAsync();

                TempData["SuccessMessage"] = $"Immunization schedule request for {Form.ChildName} has been submitted successfully. An email confirmation has been sent to {Form.Email}.";
                _logger.LogInformation("Immunization schedule request created for child {ChildName} by user {User}", 
                    Form.ChildName, User.Identity?.Name);

                return RedirectToPage("/Nurse/Immunization");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating immunization schedule request for child {ChildName}", Form.ChildName);
                ModelState.AddModelError("", "An error occurred while submitting the schedule request. Please try again.");
                return Page();
            }
        }

        private async Task SendScheduleNotificationEmailAsync()
        {
            try
            {
                var scheduleMessage = $@"Dear {Form.MotherName},

Thank you for requesting an immunization schedule for {Form.ChildName}.

SCHEDULE DETAILS:
- Date: {(string.IsNullOrEmpty(Form.PreferredDate) ? "To be scheduled" : DateTime.Parse(Form.PreferredDate).ToString("dddd, MMMM dd, yyyy"))}
- Time: {Form.PreferredTime ?? "8:00 AM - 12:00 PM"}
- Location: Baesa Health Center
- Address: {Form.Address}

IMPORTANT REMINDERS:
- Please bring your child's birth certificate
- Bring any previous immunization records
- Arrive 15 minutes before your scheduled time
- If you need to reschedule, please contact us in advance

WHAT TO EXPECT:
Our nurses will complete a full immunization record during your visit, tracking all vaccines according to the official schedule.

For any questions, please contact Baesa Health Center.

Best regards,
Baesa Health Center Team";

                await _immunizationReminderService.SendImmunizationReminderAsync(
                    Form.Email, 
                    Form.MotherName, 
                    scheduleMessage);

                _logger.LogInformation("Schedule notification email sent to {Email} for child {ChildName}", 
                    Form.Email, Form.ChildName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send schedule notification email to {Email}", Form.Email);
                // Don't fail the form submission if email fails
            }
        }
    }
}
