using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Barangay.Models;
using Barangay.Data;
using Barangay.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Barangay.Services;
using Barangay.Extensions;

namespace Barangay.Pages.User
{
    [Authorize]
    public class AppointmentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDataEncryptionService _encryptionService;

        public AppointmentsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IDataEncryptionService encryptionService)
        {
            _context = context;
            _userManager = userManager;
            _encryptionService = encryptionService;
        }

        public List<Appointment> UpcomingAppointments { get; set; } = new List<Appointment>();
        public List<Appointment> PastAppointments { get; set; } = new List<Appointment>();
        public List<Appointment> DraftAppointments { get; set; } = new List<Appointment>();
        public List<Appointment> OngoingAppointments { get; set; } = new List<Appointment>();
        public List<Appointment> ConfirmedAppointments { get; set; } = new List<Appointment>();
        public Dictionary<string, ApplicationUser> Doctors { get; set; } = new Dictionary<string, ApplicationUser>();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Decrypt user data for authorized users
            user = user.DecryptSensitiveData(_encryptionService, User);
            
            // Manually decrypt Email and PhoneNumber since they're not marked with [Encrypted] attribute
            if (!string.IsNullOrEmpty(user.Email) && _encryptionService.IsEncrypted(user.Email))
            {
                user.Email = user.Email.DecryptForUser(_encryptionService, User);
            }
            if (!string.IsNullOrEmpty(user.PhoneNumber) && _encryptionService.IsEncrypted(user.PhoneNumber))
            {
                user.PhoneNumber = user.PhoneNumber.DecryptForUser(_encryptionService, User);
            }

            // Get the current date using Philippine timezone
            var today = DateTimeHelper.Today;

            try
            {
                // Get all appointments for the current user
                var appointments = await _context.Appointments
                    .Where(a => a.PatientId == user.Id)
                    .OrderBy(a => a.AppointmentDate)
                    .ThenBy(a => a.AppointmentTime)
                    .ToListAsync();

                // Ensure all appointments have valid times
                foreach (var appointment in appointments)
                {
                    // Ensure ReasonForVisit is not null
                    if (appointment.ReasonForVisit == null)
                    {
                        appointment.ReasonForVisit = "General Checkup";
                    }
                }

                // Get all doctor IDs from appointments
                var doctorIds = appointments
                    .Select(a => a.DoctorId)
                    .Distinct()
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToList();

                // Load all doctors in one query
                var doctors = await _userManager.Users
                    .Where(u => doctorIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id);

                // Decrypt doctor data
                Doctors = doctors?.ToDictionary(
                    kvp => kvp.Key, 
                    kvp => kvp.Value.DecryptSensitiveData(_encryptionService, User)
                ) ?? new Dictionary<string, ApplicationUser>();

                // Manually decrypt Email for each doctor
                foreach (var doctor in Doctors.Values)
                {
                    if (!string.IsNullOrEmpty(doctor.Email) && _encryptionService.IsEncrypted(doctor.Email))
                    {
                        doctor.Email = doctor.Email.DecryptForUser(_encryptionService, User);
                    }
                }

                // Split into upcoming and past appointments using proper datetime comparison
                var now = DateTimeHelper.Now;
                var todayEnd = today.AddDays(1).AddTicks(-1);
                
                UpcomingAppointments = appointments
                    .Where(a => a.AppointmentDate > todayEnd || 
                        (a.AppointmentDate >= today && a.AppointmentDate <= todayEnd && 
                            a.AppointmentTime >= now.TimeOfDay))
                    .ToList();

                PastAppointments = appointments
                    .Where(a => a.AppointmentDate < today || 
                        (a.AppointmentDate >= today && a.AppointmentDate <= todayEnd && 
                            a.AppointmentTime < now.TimeOfDay))
                    .ToList();

                // Separate appointments by status for upcoming appointments
                DraftAppointments = UpcomingAppointments
                    .Where(a => a.Status == AppointmentStatus.Draft)
                    .ToList();

                OngoingAppointments = UpcomingAppointments
                    .Where(a => a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.InProgress)
                    .ToList();

                ConfirmedAppointments = UpcomingAppointments
                    .Where(a => a.Status == AppointmentStatus.Confirmed)
                    .ToList();
            }
            catch (InvalidCastException ex)
            {
                // Log the error
                Console.WriteLine($"Type conversion error: {ex.Message}");
                
                // Initialize empty lists to avoid null reference exceptions in the view
                UpcomingAppointments = new List<Appointment>();
                PastAppointments = new List<Appointment>();
                DraftAppointments = new List<Appointment>();
                OngoingAppointments = new List<Appointment>();
                ConfirmedAppointments = new List<Appointment>();
            }
            catch (Exception ex)
            {
                // Log the general error
                Console.WriteLine($"Error loading appointments: {ex.Message}");
                
                // Initialize empty lists to avoid null reference exceptions in the view
                UpcomingAppointments = new List<Appointment>();
                PastAppointments = new List<Appointment>();
                DraftAppointments = new List<Appointment>();
                OngoingAppointments = new List<Appointment>();
                ConfirmedAppointments = new List<Appointment>();
            }

            return Page();
        }

        public string GetDoctorName(string doctorId)
        {
            if (string.IsNullOrEmpty(doctorId))
                return "Unknown Doctor";

            if (Doctors.TryGetValue(doctorId, out ApplicationUser? doctor))
            {
                string fullName = "";
                if (!string.IsNullOrEmpty(doctor.FirstName) && !string.IsNullOrEmpty(doctor.LastName))
                {
                    fullName = $"Dr. {doctor.FirstName} {doctor.LastName}";
                }
                else
                {
                    fullName = doctor.UserName ?? doctor.Email ?? "Unknown Doctor";
                }
                return fullName;
            }

            return "Unknown Doctor";
        }

        public IActionResult OnGetBookNewAppointment()
        {
            return RedirectToPage("/BookAppointment");
        }

        public string GetFullConsultationType(string? consultationType)
        {
            if (string.IsNullOrEmpty(consultationType))
            {
                return "N/A";
            }

            return consultationType.ToLower() switch
            {
                "general consult" => "General Consult",
                "dental" => "Dental",
                "immunization" => "Immunization",
                "prenatal & family planning" => "Prenatal & Family Planning",
                "prenatal and family planning" => "Prenatal & Family Planning",
                "dots consult" => "DOTS Consult",
                _ => consultationType
            };
        }

        public async Task<IActionResult> OnPostCancelAppointmentAsync(int appointmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.PatientId == user.Id);

            if (appointment == null)
            {
                TempData["Error"] = "Appointment not found.";
                return RedirectToPage();
            }

            // Only allow cancellation for future appointments
            if (appointment.AppointmentDate < DateTime.Now.Date || 
                (appointment.AppointmentDate == DateTime.Now.Date && appointment.AppointmentTime < DateTime.Now.TimeOfDay))
            {
                TempData["Error"] = "Cannot cancel past appointments.";
                return RedirectToPage();
            }

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.UpdatedAt = DateTime.Now;
            
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Appointment cancelled successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetFixWeekendsAsync()
        {
            try
            {
                // Get all doctors
                var doctors = await _context.Users
                    .Where(u => _context.UserRoles
                        .Any(ur => ur.UserId == u.Id && 
                                   _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Doctor")))
                    .ToListAsync();

                var updatedCount = 0;
                var createdCount = 0;

                foreach (var doctor in doctors)
                {
                    // Check if DoctorAvailability exists
                    var availability = await _context.DoctorAvailabilities
                        .FirstOrDefaultAsync(da => da.DoctorId == doctor.Id);

                    if (availability == null)
                    {
                        // Create new availability with weekend support
                        availability = new DoctorAvailability
                        {
                            DoctorId = doctor.Id,
                            IsAvailable = true,
                            Monday = true,
                            Tuesday = true,
                            Wednesday = true,
                            Thursday = true,
                            Friday = true,
                            Saturday = true,  // ENABLE WEEKENDS
                            Sunday = true,    // ENABLE WEEKENDS
                            StartTime = new TimeSpan(8, 0, 0), // 8:00 AM
                            EndTime = new TimeSpan(17, 0, 0),  // 5:00 PM
                            LastUpdated = DateTime.Now
                        };

                        _context.DoctorAvailabilities.Add(availability);
                        createdCount++;
                    }
                    else
                    {
                        // Update existing availability
                        availability.Saturday = true;  // ENABLE WEEKENDS
                        availability.Sunday = true;    // ENABLE WEEKENDS
                        availability.IsAvailable = true;
                        availability.StartTime = new TimeSpan(8, 0, 0);
                        availability.EndTime = new TimeSpan(17, 0, 0);
                        availability.LastUpdated = DateTime.Now;
                        updatedCount++;
                    }
                }

                await _context.SaveChangesAsync();

                return new JsonResult(new { 
                    success = true, 
                    message = $"Fixed weekend appointments for {doctors.Count} doctors! Updated {updatedCount} existing records and created {createdCount} new records.",
                    updatedCount = updatedCount,
                    createdCount = createdCount,
                    totalDoctors = doctors.Count
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { 
                    success = false, 
                    error = ex.Message 
                });
            }
        }
    }
} 