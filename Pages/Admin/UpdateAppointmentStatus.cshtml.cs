using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Barangay.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UpdateAppointmentStatusModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public UpdateAppointmentStatusModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string Message { get; set; }
        public bool Success { get; set; }
        public List<Appointment> UpdatedAppointments { get; set; } = new List<Appointment>();

        public void OnGet()
        {
            // Just display the page
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Consultation types that don't require assessments
                var noAssessmentTypes = new[] { "immunization", "prenatal & family planning", "prenatal and family planning", "dots consult", "dental" };

                // Find all Draft appointments with these consultation types
                var draftAppointments = await _context.Appointments
                    .Where(a => a.Status == AppointmentStatus.Draft &&
                                noAssessmentTypes.Contains(a.Type.ToLower()))
                    .ToListAsync();

                if (draftAppointments.Any())
                {
                    // Update status to Pending
                    foreach (var appointment in draftAppointments)
                    {
                        appointment.Status = AppointmentStatus.Pending;
                        appointment.UpdatedAt = DateTime.Now;
                    }

                    await _context.SaveChangesAsync();

                    UpdatedAppointments = draftAppointments;
                    Success = true;
                    Message = $"Successfully updated {draftAppointments.Count} appointment(s) from Draft to Pending status.";
                }
                else
                {
                    Success = true;
                    Message = "No Draft appointments found for the specified consultation types.";
                }
            }
            catch (Exception ex)
            {
                Success = false;
                Message = $"Error updating appointments: {ex.Message}";
            }

            return Page();
        }
    }
}
