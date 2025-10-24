using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using Barangay.Extensions;
using Barangay.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;

namespace Barangay.Pages.Nurse
{
    [Authorize(Roles = "Nurse,Head Nurse")]
    [Authorize(Policy = "Appointments")]
    public class AppointmentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AppointmentsModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDataEncryptionService _encryptionService;

        public AppointmentsModel(
            ApplicationDbContext context, 
            ILogger<AppointmentsModel> logger,
            UserManager<ApplicationUser> userManager,
            IDataEncryptionService encryptionService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _encryptionService = encryptionService;
        }

        public class AppointmentViewModel
        {
            public int Id { get; set; }
            public string PatientId { get; set; }
            public string PatientName { get; set; }
            public DateTime AppointmentDate { get; set; }
            public TimeSpan AppointmentTime { get; set; }
            public AppointmentStatus Status { get; set; }
            public string Type { get; set; }
            public string Description { get; set; }
        }

        public List<AppointmentViewModel> Appointments { get; set; } = new List<AppointmentViewModel>();
        public List<AppointmentViewModel> TodayAppointments { get; set; } = new List<AppointmentViewModel>();

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                _logger.LogInformation("Loading appointments for nurse dashboard");
                
                // Get all appointments with eager loading of Patient (Doctor loading removed)
                var appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.PatientName != "System Administrator" && a.PatientId != "0e03f06e-ba88-46ed-b047-4974d8b8252a")
                    .OrderByDescending(a => a.AppointmentDate)
                    .ThenBy(a => a.AppointmentTime)
                    .ToListAsync();

                _logger.LogInformation("Found {0} appointments in the database", appointments.Count);
                
                // Decrypt patient data for display
                foreach (var appointment in appointments)
                {
                    if (appointment.Patient != null)
                    {
                        try
                        {
                            appointment.Patient.DecryptSensitiveData(_encryptionService, User);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to decrypt patient data for appointment {Id}", appointment.Id);
                        }
                    }
                }
                
                // Get today's date using Philippine timezone
                var today = DateTimeHelper.Today;
                var endOfToday = today.AddDays(1).AddTicks(-1);
                
                // Decrypt patient names and doctor names for all appointments
                foreach (var appointment in appointments)
                {
                    // Decrypt patient name
                    if (!string.IsNullOrEmpty(appointment.PatientName) && _encryptionService.IsEncrypted(appointment.PatientName))
                    {
                        appointment.PatientName = _encryptionService.DecryptForUser(appointment.PatientName, User);
                    }
                    
                    // Decrypt dependent name if applicable
                    if (!string.IsNullOrEmpty(appointment.DependentFullName) && _encryptionService.IsEncrypted(appointment.DependentFullName))
                    {
                        appointment.DependentFullName = _encryptionService.DecryptForUser(appointment.DependentFullName, User);
                    }
                    
                    // Doctor data decryption removed - not needed for nurse view
                }

                // Convert to view models - EXCLUDE Draft and Cancelled appointments from nurse view
                // Nurses should only see: Pending, InProgress, Confirmed, and Completed
                Appointments = appointments
                    .Where(a => a.Status == AppointmentStatus.Pending || 
                                a.Status == AppointmentStatus.InProgress || 
                                a.Status == AppointmentStatus.Confirmed ||
                                a.Status == AppointmentStatus.Completed)
                    .Select(a => new AppointmentViewModel
                    {
                        Id = a.Id,
                        PatientId = a.PatientId,
                        PatientName = !string.IsNullOrEmpty(a.DependentFullName) ? a.DependentFullName : a.PatientName,
                        AppointmentDate = a.AppointmentDate,
                        AppointmentTime = a.AppointmentTime,
                        Status = a.Status,
                        Type = a.Type ?? "General",
                        Description = a.Description
                    }).ToList();
                
                _logger.LogInformation("After filtering: {0} appointments for nurse view (excluding Draft and Cancelled)", Appointments.Count);
                
                // Filter today's appointments (include Pending, InProgress, Confirmed)
                TodayAppointments = Appointments
                    .Where(a => a.AppointmentDate >= today && a.AppointmentDate <= endOfToday)
                    .OrderBy(a => a.AppointmentTime)
                    .ToList();
                
                _logger.LogInformation("Found {0} appointments for today", TodayAppointments.Count);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading appointments");
                StatusMessage = "Error loading appointments. Please try again later.";
                return Page();
            }
        }

    }
} 