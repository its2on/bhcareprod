using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace Barangay.Pages.Forms
{
    [Authorize(Roles = "Nurse,Head Nurse,Doctor,Admin,SuperAdmin")]
    public class ViewSubmissionModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ViewSubmissionModel> _logger;

        public ViewSubmissionModel(ApplicationDbContext context, ILogger<ViewSubmissionModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public FormSubmission Submission { get; set; } = null!;
        public FormTemplate FormTemplate { get; set; } = null!;
        public Appointment? Appointment { get; set; }
        public Dictionary<string, string> SubmissionData { get; set; } = new Dictionary<string, string>();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound("Submission ID is required.");
            }

            Submission = await _context.FormSubmissions
                .Include(s => s.FormTemplate)
                .FirstOrDefaultAsync(s => s.FormSubmissionId == id.Value);

            if (Submission == null)
            {
                return NotFound("Form submission not found.");
            }

            FormTemplate = Submission.FormTemplate;

            // Load appointment if linked
            if (Submission.AppointmentId.HasValue)
            {
                Appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.Id == Submission.AppointmentId.Value);
            }

            // Parse JSON form data
            try
            {
                SubmissionData = JsonSerializer.Deserialize<Dictionary<string, string>>(Submission.FormData) 
                                 ?? new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse form submission data for submission ID {Id}", id);
                SubmissionData = new Dictionary<string, string> { { "Error", "Failed to parse submission data" } };
            }

            return Page();
        }
    }
}

