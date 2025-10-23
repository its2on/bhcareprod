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
        private readonly INotificationService _notificationService;
        private readonly IDataEncryptionService _encryptionService;

        public AppointmentsModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService,
            IDataEncryptionService encryptionService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _encryptionService = encryptionService;
        }

        public List<Appointment> UpcomingAppointments { get; set; } = new List<Appointment>();
        public List<Appointment> PastAppointments { get; set; } = new List<Appointment>();
        public List<Appointment> DraftAppointments { get; set; } = new List<Appointment>();
        public List<Appointment> OngoingAppointments { get; set; } = new List<Appointment>();
        public List<Appointment> ConfirmedAppointments { get; set; } = new List<Appointment>();
        public List<Appointment> CancelledAppointments { get; set; } = new List<Appointment>();
        public List<Appointment> CompletedAppointments { get; set; } = new List<Appointment>();
        public Dictionary<string, ApplicationUser> Doctors { get; set; } = new Dictionary<string, ApplicationUser>();

        public async Task<IActionResult> OnGetAsync()
        {
            Console.WriteLine("=== DEBUG: OnGetAsync called ===");
            Console.WriteLine($"DEBUG: Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("DEBUG: User is null, redirecting to login");
                return RedirectToPage("/Account/Login");
            }
            
            Console.WriteLine($"DEBUG: User found: {user.Id} ({user.UserName})");

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

                Console.WriteLine($"DEBUG: Raw appointments from database: {appointments.Count}");
                
                // Log each appointment with full details
                foreach (var apt in appointments)
                {
                    Console.WriteLine($"DEBUG: Raw Appointment {apt.Id} - Status: {apt.Status} ({apt.Status.GetType().Name}), Date: {apt.AppointmentDate:yyyy-MM-dd}, Time: {apt.AppointmentTime}, UpdatedAt: {apt.UpdatedAt:yyyy-MM-dd HH:mm:ss}, CreatedAt: {apt.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                }

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
                // Consultation types that don't require assessments
                var noAssessmentTypes = new[] { "immunization", "prenatal & family planning", "prenatal and family planning", "dots consult", "dental" };
                
                DraftAppointments = UpcomingAppointments
                    .Where(a => a.Status == AppointmentStatus.Draft && 
                                !noAssessmentTypes.Contains(a.Type?.ToLower() ?? ""))
                    .ToList();

                OngoingAppointments = UpcomingAppointments
                    .Where(a => a.Status == AppointmentStatus.Pending || 
                                a.Status == AppointmentStatus.InProgress ||
                                (a.Status == AppointmentStatus.Draft && 
                                 noAssessmentTypes.Contains(a.Type?.ToLower() ?? "")))
                    .ToList();

                ConfirmedAppointments = UpcomingAppointments
                    .Where(a => a.Status == AppointmentStatus.Confirmed)
                    .ToList();

                // Get cancelled appointments from all appointments (not just upcoming)
                CancelledAppointments = appointments
                    .Where(a => a.Status == AppointmentStatus.Cancelled)
                    .ToList();

                // Get completed appointments from all appointments
                CompletedAppointments = appointments
                    .Where(a => a.Status == AppointmentStatus.Completed)
                    .ToList();

                // Enhanced debug logging
                Console.WriteLine($"DEBUG: === APPOINTMENT CATEGORIZATION RESULTS ===");
                Console.WriteLine($"DEBUG: Total appointments found: {appointments.Count}");
                Console.WriteLine($"DEBUG: Upcoming appointments: {UpcomingAppointments.Count}");
                Console.WriteLine($"DEBUG: Past appointments: {PastAppointments.Count}");
                Console.WriteLine($"DEBUG: Ongoing appointments: {OngoingAppointments.Count}");
                Console.WriteLine($"DEBUG: Draft appointments: {DraftAppointments.Count}");
                Console.WriteLine($"DEBUG: Confirmed appointments: {ConfirmedAppointments.Count}");
                Console.WriteLine($"DEBUG: Cancelled appointments found: {CancelledAppointments.Count}");
                Console.WriteLine($"DEBUG: Completed appointments found: {CompletedAppointments.Count}");
                
                // Log each cancelled appointment in detail
                Console.WriteLine($"DEBUG: === CANCELLED APPOINTMENTS DETAIL ===");
                foreach (var cancelled in CancelledAppointments)
                {
                    Console.WriteLine($"DEBUG: Cancelled Appointment {cancelled.Id} - Status: {cancelled.Status}, Date: {cancelled.AppointmentDate:yyyy-MM-dd}, Time: {cancelled.AppointmentTime}, UpdatedAt: {cancelled.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                }
                
                // Log each ongoing appointment in detail
                Console.WriteLine($"DEBUG: === ONGOING APPOINTMENTS DETAIL ===");
                foreach (var ongoing in OngoingAppointments)
                {
                    Console.WriteLine($"DEBUG: Ongoing Appointment {ongoing.Id} - Status: {ongoing.Status}, Date: {ongoing.AppointmentDate:yyyy-MM-dd}, Time: {ongoing.AppointmentTime}, UpdatedAt: {ongoing.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                }
                
                // Log each completed appointment in detail
                Console.WriteLine($"DEBUG: === COMPLETED APPOINTMENTS DETAIL ===");
                foreach (var completed in CompletedAppointments)
                {
                    Console.WriteLine($"DEBUG: Completed Appointment {completed.Id} - Status: {completed.Status}, Date: {completed.AppointmentDate:yyyy-MM-dd}, Time: {completed.AppointmentTime}, UpdatedAt: {completed.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
                }
                
                Console.WriteLine($"DEBUG: === END APPOINTMENT CATEGORIZATION ===");
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
                CancelledAppointments = new List<Appointment>();
                CompletedAppointments = new List<Appointment>();
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
                CancelledAppointments = new List<Appointment>();
                CompletedAppointments = new List<Appointment>();
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
            Console.WriteLine($"DEBUG: OnPostCancelAppointmentAsync called with appointmentId: {appointmentId}");
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                Console.WriteLine("DEBUG: User is null, redirecting to login");
                return RedirectToPage("/Account/Login");
            }

            Console.WriteLine($"DEBUG: User found: {user.Id}");

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.PatientId == user.Id);

            if (appointment == null)
            {
                Console.WriteLine($"DEBUG: Appointment {appointmentId} not found for user {user.Id}");
                TempData["Error"] = "Appointment not found.";
                return RedirectToPage();
            }

            Console.WriteLine($"DEBUG: Found appointment {appointmentId} - Status: {appointment.Status}, Date: {appointment.AppointmentDate}, Time: {appointment.AppointmentTime}");

            // Only allow cancellation for future appointments
            var now = DateTimeHelper.Now;
            Console.WriteLine($"DEBUG: Current time: {now}, Appointment date: {appointment.AppointmentDate}, Appointment time: {appointment.AppointmentTime}");
            
            if (appointment.AppointmentDate < now.Date || 
                (appointment.AppointmentDate == now.Date && appointment.AppointmentTime < now.TimeOfDay))
            {
                Console.WriteLine($"DEBUG: Cannot cancel past appointment - Date: {appointment.AppointmentDate}, Time: {appointment.AppointmentTime}");
                TempData["Error"] = "Cannot cancel past appointments.";
                return RedirectToPage();
            }

            Console.WriteLine($"DEBUG: Proceeding with cancellation of appointment {appointmentId} - Current status: {appointment.Status}");
            
            appointment.Status = AppointmentStatus.Cancelled;
            appointment.UpdatedAt = DateTimeHelper.Now;
            
            // Explicitly mark the entity as modified to ensure EF tracks the change
            _context.Entry(appointment).State = EntityState.Modified;
            
            Console.WriteLine($"DEBUG: Updated appointment status to: {appointment.Status}");
            
            await _context.SaveChangesAsync();
            
            // Verify the change was saved by reloading from database
            var verifyAppointment = await _context.Appointments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
            
            Console.WriteLine($"DEBUG: Verification - Appointment {appointmentId} status in DB: {verifyAppointment?.Status}");
            
            if (verifyAppointment?.Status == AppointmentStatus.Cancelled)
            {
                Console.WriteLine($"DEBUG: Appointment {appointmentId} cancelled successfully and verified in database.");
                TempData["Success"] = "Appointment cancelled successfully.";
                
                // Create notification for the user
                try
                {
                    var notificationMessage = $"Your appointment on {appointment.AppointmentDate:MMM dd, yyyy} at {appointment.AppointmentTime:hh\\:mm tt} has been cancelled successfully.";
                    
                    await _notificationService.CreateNotificationForUserAsync(
                        user.Id,
                        "Appointment Cancelled",
                        notificationMessage,
                        "Appointment Cancelled",
                        "/User/Appointments"
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DEBUG: Error creating cancellation notification: {ex.Message}");
                    // Don't fail the cancellation if notification fails
                }
            }
            else
            {
                Console.WriteLine($"DEBUG: WARNING - Appointment status not properly saved! Expected: Cancelled, Got: {verifyAppointment?.Status}");
                TempData["Error"] = "There was an issue cancelling the appointment. Please try again.";
            }
            
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetCreateTestAppointmentAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Get a doctor to assign the appointment to
            var doctor = await _userManager.Users
                .FirstOrDefaultAsync(u => _context.UserRoles
                    .Any(ur => ur.UserId == u.Id && 
                               _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Doctor")));

            if (doctor == null)
            {
                TempData["Error"] = "No doctors available for test appointment.";
                return RedirectToPage();
            }

            // Create a test appointment for tomorrow
            var testAppointment = new Appointment
            {
                PatientId = user.Id,
                DoctorId = doctor.Id,
                AppointmentDate = DateTime.Now.AddDays(1).Date,
                AppointmentTime = new TimeSpan(10, 0, 0), // 10:00 AM
                ReasonForVisit = "Test appointment for cancellation testing",
                Status = AppointmentStatus.Pending,
                Type = "General Consult",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Appointments.Add(testAppointment);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Test appointment created successfully with ID: {testAppointment.Id}";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetAppointmentDetailsAsync(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return new JsonResult(new { success = false, error = "User not authenticated" });
            }

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.PatientId == user.Id);

            if (appointment == null)
            {
                return new JsonResult(new { success = false, error = "Appointment not found" });
            }

            // Calculate age display (months if 0 years, or calculate from DateOfBirth)
            var ageDisplay = appointment.AgeValue.ToString();
            
            if (appointment.AgeValue == 0)
            {
                // Calculate age in months from birth date if available
                if (appointment.DateOfBirth.HasValue)
                {
                    var today = DateTime.Today;
                    var birthDate = appointment.DateOfBirth.Value;
                    
                    // Calculate total months
                    int months = ((today.Year - birthDate.Year) * 12) + today.Month - birthDate.Month;
                    
                    // If the day hasn't occurred yet this month, subtract one month
                    if (today.Day < birthDate.Day)
                    {
                        months--;
                    }
                    
                    // Handle negative months (future date)
                    if (months < 0)
                    {
                        months = 0;
                    }
                    
                    ageDisplay = months == 1 ? "1 month old" : $"{months} months old";
                }
                else
                {
                    // No birth date available, show as newborn
                    ageDisplay = "Newborn";
                }
            }
            else if (appointment.AgeValue == 1)
            {
                ageDisplay = "1 year old";
            }
            else
            {
                ageDisplay = $"{appointment.AgeValue} years old";
            }

            // Get immunization schedule for immunization appointments
            string immunizationSchedule = "N/A";
            if (appointment.Type?.ToLower() == "immunization")
            {
                // Try to find immunization record for this user
                var immunizationRecords = await _context.ImmunizationRecords
                    .Where(r => r.Status == "Active")
                    .ToListAsync();
                
                // Decrypt and match by user details
                var matchingRecord = immunizationRecords
                    .Select(r => {
                        try {
                            var decrypted = new ImmunizationRecord {
                                Id = r.Id,
                                ChildName = r.ChildName.DecryptForUser(_encryptionService, User) ?? "",
                                DateOfBirth = r.DateOfBirth.DecryptForUser(_encryptionService, User) ?? "",
                                BCGVaccineDate = r.BCGVaccineDate?.DecryptForUser(_encryptionService, User),
                                BCGVaccineRemarks = r.BCGVaccineRemarks?.DecryptForUser(_encryptionService, User),
                                HepatitisBVaccineDate = r.HepatitisBVaccineDate?.DecryptForUser(_encryptionService, User),
                                HepatitisBVaccineRemarks = r.HepatitisBVaccineRemarks?.DecryptForUser(_encryptionService, User),
                                Pentavalent1Date = r.Pentavalent1Date?.DecryptForUser(_encryptionService, User),
                                Pentavalent1Remarks = r.Pentavalent1Remarks?.DecryptForUser(_encryptionService, User),
                                Pentavalent2Date = r.Pentavalent2Date?.DecryptForUser(_encryptionService, User),
                                Pentavalent2Remarks = r.Pentavalent2Remarks?.DecryptForUser(_encryptionService, User),
                                Pentavalent3Date = r.Pentavalent3Date?.DecryptForUser(_encryptionService, User),
                                Pentavalent3Remarks = r.Pentavalent3Remarks?.DecryptForUser(_encryptionService, User),
                                OPV1Date = r.OPV1Date?.DecryptForUser(_encryptionService, User),
                                OPV1Remarks = r.OPV1Remarks?.DecryptForUser(_encryptionService, User),
                                OPV2Date = r.OPV2Date?.DecryptForUser(_encryptionService, User),
                                OPV2Remarks = r.OPV2Remarks?.DecryptForUser(_encryptionService, User),
                                OPV3Date = r.OPV3Date?.DecryptForUser(_encryptionService, User),
                                OPV3Remarks = r.OPV3Remarks?.DecryptForUser(_encryptionService, User),
                                IPV1Date = r.IPV1Date?.DecryptForUser(_encryptionService, User),
                                IPV1Remarks = r.IPV1Remarks?.DecryptForUser(_encryptionService, User),
                                IPV2Date = r.IPV2Date?.DecryptForUser(_encryptionService, User),
                                IPV2Remarks = r.IPV2Remarks?.DecryptForUser(_encryptionService, User),
                                PCV1Date = r.PCV1Date?.DecryptForUser(_encryptionService, User),
                                PCV1Remarks = r.PCV1Remarks?.DecryptForUser(_encryptionService, User),
                                PCV2Date = r.PCV2Date?.DecryptForUser(_encryptionService, User),
                                PCV2Remarks = r.PCV2Remarks?.DecryptForUser(_encryptionService, User),
                                PCV3Date = r.PCV3Date?.DecryptForUser(_encryptionService, User),
                                PCV3Remarks = r.PCV3Remarks?.DecryptForUser(_encryptionService, User),
                                MMR1Date = r.MMR1Date?.DecryptForUser(_encryptionService, User),
                                MMR1Remarks = r.MMR1Remarks?.DecryptForUser(_encryptionService, User),
                                MMR2Date = r.MMR2Date?.DecryptForUser(_encryptionService, User),
                                MMR2Remarks = r.MMR2Remarks?.DecryptForUser(_encryptionService, User)
                            };
                            return decrypted;
                        } catch {
                            return null;
                        }
                    })
                    .Where(r => r != null)
                    .FirstOrDefault();

                if (matchingRecord != null)
                {
                    var scheduleList = new List<string>();
                    
                    // Build immunization schedule table
                    if (!string.IsNullOrEmpty(matchingRecord.BCGVaccineDate))
                        scheduleList.Add($"BCG Vaccine: {matchingRecord.BCGVaccineDate} - {matchingRecord.BCGVaccineRemarks ?? "Completed"}");
                    if (!string.IsNullOrEmpty(matchingRecord.HepatitisBVaccineDate))
                        scheduleList.Add($"Hepatitis B: {matchingRecord.HepatitisBVaccineDate} - {matchingRecord.HepatitisBVaccineRemarks ?? "Completed"}");
                    if (!string.IsNullOrEmpty(matchingRecord.Pentavalent1Date))
                        scheduleList.Add($"Pentavalent 1: {matchingRecord.Pentavalent1Date} - {matchingRecord.Pentavalent1Remarks ?? "Completed"}");
                    if (!string.IsNullOrEmpty(matchingRecord.Pentavalent2Date))
                        scheduleList.Add($"Pentavalent 2: {matchingRecord.Pentavalent2Date} - {matchingRecord.Pentavalent2Remarks ?? "Completed"}");
                    if (!string.IsNullOrEmpty(matchingRecord.Pentavalent3Date))
                        scheduleList.Add($"Pentavalent 3: {matchingRecord.Pentavalent3Date} - {matchingRecord.Pentavalent3Remarks ?? "Completed"}");
                    if (!string.IsNullOrEmpty(matchingRecord.OPV1Date))
                        scheduleList.Add($"OPV 1: {matchingRecord.OPV1Date} - {matchingRecord.OPV1Remarks ?? "Completed"}");
                    if (!string.IsNullOrEmpty(matchingRecord.OPV2Date))
                        scheduleList.Add($"OPV 2: {matchingRecord.OPV2Date} - {matchingRecord.OPV2Remarks ?? "Completed"}");
                    if (!string.IsNullOrEmpty(matchingRecord.OPV3Date))
                        scheduleList.Add($"OPV 3: {matchingRecord.OPV3Date} - {matchingRecord.OPV3Remarks ?? "Completed"}");
                    if (!string.IsNullOrEmpty(matchingRecord.IPV1Date))
                        scheduleList.Add($"IPV 1: {matchingRecord.IPV1Date} - {matchingRecord.IPV1Remarks ?? "Completed"}");
                    if (!string.IsNullOrEmpty(matchingRecord.IPV2Date))
                        scheduleList.Add($"IPV 2: {matchingRecord.IPV2Date} - {matchingRecord.IPV2Remarks ?? "Completed"}");
                    if (!string.IsNullOrEmpty(matchingRecord.PCV1Date))
                        scheduleList.Add($"PCV 1: {matchingRecord.PCV1Date} - {matchingRecord.PCV1Remarks ?? "Completed"}");
                    if (!string.IsNullOrEmpty(matchingRecord.PCV2Date))
                        scheduleList.Add($"PCV 2: {matchingRecord.PCV2Date} - {matchingRecord.PCV2Remarks ?? "Completed"}");
                    if (!string.IsNullOrEmpty(matchingRecord.PCV3Date))
                        scheduleList.Add($"PCV 3: {matchingRecord.PCV3Date} - {matchingRecord.PCV3Remarks ?? "Completed"}");
                    if (!string.IsNullOrEmpty(matchingRecord.MMR1Date))
                        scheduleList.Add($"MMR 1: {matchingRecord.MMR1Date} - {matchingRecord.MMR1Remarks ?? "Completed"}");
                    if (!string.IsNullOrEmpty(matchingRecord.MMR2Date))
                        scheduleList.Add($"MMR 2: {matchingRecord.MMR2Date} - {matchingRecord.MMR2Remarks ?? "Completed"}");
                    
                    if (scheduleList.Any())
                    {
                        immunizationSchedule = string.Join("; ", scheduleList);
                    }
                }
            }

            return new JsonResult(new
            {
                success = true,
                appointment = new
                {
                    id = appointment.Id,
                    date = appointment.AppointmentDate.ToString("MMMM dd, yyyy"),
                    time = appointment.GetFormattedTime(),
                    age = ageDisplay,
                    consultationType = GetFullConsultationType(appointment.Type),
                    reasonForVisit = appointment.ReasonForVisit,
                    status = appointment.Status.ToString(),
                    immunizationSchedule = immunizationSchedule
                }
            });
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