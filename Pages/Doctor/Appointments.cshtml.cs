using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Barangay.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Barangay.Services;
using Microsoft.Extensions.Logging;

namespace Barangay.Pages.Doctor
{
    [Authorize(Roles = "Doctor")]
    public class AppointmentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AppointmentsModel> _logger;
        private readonly IPermissionService _permissionService;
        private readonly IDataEncryptionService _encryptionService;

        public AppointmentsModel(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ILogger<AppointmentsModel> logger,
            IPermissionService permissionService,
            IDataEncryptionService encryptionService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _permissionService = permissionService;
            _encryptionService = encryptionService;
        }

        public List<Barangay.Models.Appointment> TodayAppointments { get; set; } = new();
        public List<Barangay.Models.Appointment> UpcomingAppointments { get; set; } = new();
        public List<Barangay.Models.Appointment> AllAppointments { get; set; } = new();
        public string ErrorMessage { get; set; }
        public bool CanAccessReports { get; set; }
        public DateTime SelectedDate { get; set; } = DateTimeHelper.Today;

        public async Task OnGetAsync(string? filterDate = null)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    ErrorMessage = "Unable to load user information.";
                    _logger.LogWarning("User could not be found in Doctor/Appointments.");
                    return;
                }

                // Evaluate access to Doctor Reports (simplified permission model)
                CanAccessReports = await _permissionService.UserHasPermissionAsync(User, "Reports")
                                     || User.IsInRole("Admin") || User.IsInRole("Doctor") || User.IsInRole("Head Doctor");

                // Parse filter date or use today as default
                if (!string.IsNullOrEmpty(filterDate) && DateTime.TryParse(filterDate, out var parsedDate))
                {
                    SelectedDate = parsedDate.Date;
                }
                else
                {
                    SelectedDate = DateTimeHelper.Today;
                }

                var today = SelectedDate;
                var endOfToday = today.AddDays(1).AddTicks(-1);

                // Exclude Immunization appointments - those are only for nurses
                var appointmentsQuery = _context.Appointments
                                                  .Where(a => a.DoctorId == user.Id && 
                                                         (a.Type == null || a.Type.ToLower() != "immunization"))
                                                  .Include(a => a.Patient)
                                                  .AsNoTracking();

                TodayAppointments = await appointmentsQuery
                                            .Where(a => a.AppointmentDate >= today && a.AppointmentDate <= endOfToday)
                                            .OrderBy(a => a.AppointmentTime)
                                            .ToListAsync();

                UpcomingAppointments = await appointmentsQuery
                                               .Where(a => a.AppointmentDate > endOfToday)
                                               .OrderBy(a => a.AppointmentDate)
                                               .ThenBy(a => a.AppointmentTime)
                                               .ToListAsync();

                AllAppointments = await appointmentsQuery
                                        .OrderByDescending(a => a.AppointmentDate)
                                        .ThenBy(a => a.AppointmentTime)
                                        .ToListAsync();
                
                // Decrypt patient data for all appointment lists
                DecryptAppointmentData(TodayAppointments);
                DecryptAppointmentData(UpcomingAppointments);
                DecryptAppointmentData(AllAppointments);
            }
            catch (Exception ex)
            {
                ErrorMessage = "An unexpected error occurred while fetching appointments.";
                _logger.LogError(ex, "Error loading appointments for doctor {DoctorId}", (await _userManager.GetUserAsync(User))?.Id);
            }
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int appointmentId, AppointmentStatus status)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null)
            {
                return NotFound();
            }

            appointment.Status = status;
            appointment.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
        
        private void DecryptAppointmentData(List<Barangay.Models.Appointment> appointments)
        {
            foreach (var appointment in appointments)
            {
                if (appointment.Patient != null)
                {
                    appointment.Patient = appointment.Patient.DecryptSensitiveData(_encryptionService, User);
                }
                
                // Also decrypt the PatientName field if it's encrypted
                if (!string.IsNullOrEmpty(appointment.PatientName) && _encryptionService.IsEncrypted(appointment.PatientName))
                {
                    appointment.PatientName = appointment.PatientName.DecryptForUser(_encryptionService, User);
                }
            }
        }
    }
} 