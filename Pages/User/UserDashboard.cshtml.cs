using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Barangay.Data;
using Barangay.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using System.Text.Json.Serialization;
using Barangay.Services;
using Barangay.Extensions;

namespace Barangay.Pages.User {
    [Authorize(Roles = "User,Patient")]
    public partial class UserDashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserDashboardModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IDataEncryptionService _encryptionService;

        public List<Barangay.Models.Appointment> TodayAppointments { get; set; } = new();
        public List<Barangay.Models.Appointment> UpcomingAppointments { get; set; } = new();
        public List<Barangay.Models.Appointment> PastAppointments { get; set; } = new();
        public HealthReport LatestHealthReport { get; set; }
        public int CurrentQueueCount { get; set; }
        public int EstimatedWaitTime { get; set; }
        public ApplicationUser CurrentUser { get; set; }

        public bool IsDoctor { get; set; }
        public bool IsNurse { get; set; }
        public bool IsPatient { get; set; }
        
        // Property to store the user's age
        public int UserAge { get; set; }
        
        // Property to check if the user is eligible for NCD Risk Assessment
        public bool IsEligibleForNCDAssessment => IsPatient && UserAge >= 20;

        [BindProperty]
        public Barangay.Models.Appointment Appointment { get; set; } = new();

        public List<ApplicationUser> AvailableDoctors { get; set; } = new();

        public UserDashboardModel(ApplicationDbContext context, ILogger<UserDashboardModel> logger, UserManager<ApplicationUser> userManager, IDataEncryptionService encryptionService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _encryptionService = encryptionService;
            
            // Setup JSON serializer options to handle circular references
            _jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve,
                MaxDepth = 64
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToPage("/Account/Login");

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

                CurrentUser = user;
                IsDoctor = await _userManager.IsInRoleAsync(user, "Doctor");
                IsNurse = await _userManager.IsInRoleAsync(user, "Nurse");
                IsPatient = await _userManager.IsInRoleAsync(user, "Patient");
                
                // Calculate user's age
                var currentDate = DateTime.Today;
                var userBirthDate = user.BirthDate ?? DateTime.MinValue;
                UserAge = currentDate.Year - userBirthDate.Year;
                // Adjust age if birthday hasn't occurred yet this year
                if (userBirthDate.Date > currentDate.AddYears(-UserAge)) 
                {
                    UserAge--;
                }

                // Load latest health report
                try
                {
                    LatestHealthReport = await _context.HealthReports
                        .Where(hr => hr.UserId == user.Id)
                        .OrderByDescending(hr => hr.CheckupDate)
                        .FirstOrDefaultAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to load health report: {ex.Message}");
                    // Continue execution without health reports
                    LatestHealthReport = null;
                }

                // Get current queue info
                var today = DateTime.Today;
                CurrentQueueCount = await _context.Appointments
                    .CountAsync(a => a.Status == AppointmentStatus.Pending && 
                                   a.AppointmentDate.Date == today);
                EstimatedWaitTime = CurrentQueueCount * 15; // 15 minutes per patient

                // Load available doctors
                var doctorRole = await _userManager.GetUsersInRoleAsync("Doctor");
                AvailableDoctors = doctorRole.Select(d => d.DecryptSensitiveData(_encryptionService, User)).ToList();

                await LoadAppointments(user.Id, today);

                // Load all appointments for the nurse dashboard
                if (IsNurse)
                {
                    var allAppointments = await _context.Appointments
                        .Include(a => a.Doctor)
                        .Include(a => a.Patient)
                        .ToListAsync();

                    // Pass the appointments to the view
                    ViewData["AllAppointments"] = allAppointments;
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user dashboard");
                ModelState.AddModelError(string.Empty, "Error loading dashboard data.");
                return Page();
            }
        }

        public async Task<JsonResult> OnGetRefreshAppointmentsAsync()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var today = DateTime.Today;
                await LoadAppointments(user.Id, today);


                // Add formatted date and time strings to the appointments
                var todayAppointmentsWithFormatting = TodayAppointments.Select(a => new {
                    id = a.Id,
                    patientName = a.PatientName,
                    doctorId = a.DoctorId,
                    doctorName = a.Doctor?.FullName ?? "Not Assigned",
                    appointmentDate = a.AppointmentDate,
                    appointmentTime = a.AppointmentTime,
                    formattedDate = a.GetFormattedDate(),
                    formattedTime = a.GetFormattedTime(),
                    status = a.Status.ToString(),
                    reasonForVisit = a.ReasonForVisit
                });

                var upcomingAppointmentsWithFormatting = UpcomingAppointments.Select(a => new {
                    id = a.Id,
                    patientName = a.PatientName,
                    doctorId = a.DoctorId,
                    doctorName = a.Doctor?.FullName ?? "Not Assigned",
                    appointmentDate = a.AppointmentDate,
                    appointmentTime = a.AppointmentTime,
                    formattedDate = a.GetFormattedDate(),
                    formattedTime = a.GetFormattedTime(),
                    status = a.Status.ToString(),
                    reasonForVisit = a.ReasonForVisit
                });

                var recentAppointmentsWithFormatting = PastAppointments.Take(5).Select(a => new {
                    id = a.Id,
                    patientName = a.PatientName,
                    doctorId = a.DoctorId,
                    doctorName = a.Doctor?.FullName ?? "Not Assigned",
                    appointmentDate = a.AppointmentDate,
                    appointmentTime = a.AppointmentTime,
                    formattedDate = a.GetFormattedDate(),
                    formattedTime = a.GetFormattedTime(),
                    status = a.Status.ToString(),
                    reasonForVisit = a.ReasonForVisit
                });

                return new JsonResult(new
                {
                    success = true,
                    today = todayAppointmentsWithFormatting,
                    upcoming = upcomingAppointmentsWithFormatting,
                    recent = recentAppointmentsWithFormatting

                }, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing appointments");
                return new JsonResult(new { success = false, message = "Error refreshing appointments" }, _jsonOptions);
            }
        }

        private async Task LoadAppointments(string userId, DateTime today)
        {
            try
            {
                if (string.IsNullOrEmpty(userId)) return;

                var query = _context.Appointments
                    .Include(a => a.Doctor)
                    .AsQueryable();

                if (IsDoctor)
                {
                    query = query.Where(a => a.DoctorId == userId);
                }
                else
                {
                    // For regular users: show self bookings and bookings for others with same family number
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        user = user.DecryptSensitiveData(_encryptionService, User);
                        var userFamilyNumber = user.FamilyNumber;
                        
                        query = query.Where(a => a.PatientId == userId && 
                                               (a.BookingForOther == false || 
                                                (a.BookingForOther == true && 
                                                 !string.IsNullOrEmpty(a.FamilyNumber) && 
                                                 !string.IsNullOrEmpty(userFamilyNumber) && 
                                                 a.FamilyNumber == userFamilyNumber)));
                    }
                    else
                    {
                        query = query.Where(a => a.PatientId == userId);
                    }
                }

                var allAppointments = await query.ToListAsync();

                // Decrypt doctor names for all appointments
                foreach (var appointment in allAppointments)
                {
                    if (appointment.Doctor != null)
                    {
                        appointment.Doctor.DecryptSensitiveData(_encryptionService, User);
                    }
                }

                TodayAppointments = allAppointments
                    .Where(a => a.AppointmentDate.Date == today.Date)
                    .OrderBy(a => a.AppointmentTime)
                    .ToList();

                UpcomingAppointments = allAppointments
                    .Where(a => a.AppointmentDate.Date > today.Date)
                    .OrderBy(a => a.AppointmentDate)
                    .ToList();

                PastAppointments = allAppointments
                    .Where(a => a.AppointmentDate.Date < today.Date)
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading appointments");
            }
        }

        public async Task<IActionResult> OnPostBookAppointmentAsync(IFormFile? attachmentFile)
        {
            try
            {
                _logger.LogInformation("Starting appointment booking process.");

                var doctorRole = await _userManager.GetUsersInRoleAsync("Doctor");
                AvailableDoctors = doctorRole.ToList();

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User not found.");
                    throw new InvalidOperationException("User not found");
                }

                Appointment.PatientId = user.Id;
                // Remove the duplicate declaration and fix patientName handling:
                var patientName = user.UserName ?? user.Email ?? "Unknown";
                if (!string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName))
                {
                    patientName = $"{user.FirstName} {user.LastName}".Trim();
                }
                if(string.IsNullOrEmpty(patientName)) 
                {
                    patientName = user.UserName ?? user.Email ?? "Unknown";
                }
                
                // Store the PatientName from the form before reinitializing the Appointment object
                var patientNameFromForm = Appointment.PatientName;
                
                // Get the user's full name to use as default if no name is provided
                string userName;
                if (!string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName)) 
                {
                    userName = !string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName) 
                        ? $"{user.FirstName} {user.LastName}".Trim() 
                        : user.UserName ?? user.Email ?? "Unknown";
                }
                else
                {
                    userName = user.UserName ?? user.Email ?? "Unknown";
                }
                
                // Use the form-provided name if available, otherwise use the user's full name
                var finalPatientName = !string.IsNullOrWhiteSpace(patientNameFromForm) ? patientNameFromForm : userName;
                
                // When you reinitialize the Appointment object, include the PatientName
                Appointment = new Barangay.Models.Appointment 
                { 
                    PatientId = user.Id,
                    DoctorId = Appointment.DoctorId ?? string.Empty,
                    AppointmentDate = Appointment.AppointmentDate,
                    AppointmentTime = Appointment.AppointmentTime,
                    Description = string.Empty,
                    ReasonForVisit = Appointment.ReasonForVisit ?? string.Empty,
                    Type = Appointment.Type ?? string.Empty,
                    PatientName = finalPatientName, // Use the determined name
                    Status = Models.AppointmentStatus.Pending,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                // Later when creating newAppointment, use the preserved PatientName
                var newAppointment = new Barangay.Models.Appointment
                {
                    DoctorId = Appointment.DoctorId,
                    PatientId = Appointment.PatientId,
                    PatientName = Appointment.PatientName, // This will now have the final name value
                    AppointmentDate = Appointment.AppointmentDate,
                    AppointmentTime = Appointment.AppointmentTime,
                    ReasonForVisit = Appointment.ReasonForVisit,
                    Status = AppointmentStatus.Pending,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    AttachmentPath = Appointment.AttachmentPath,
                    Description = Appointment.Description,
                    Prescription = Appointment.Prescription,
                    Instructions = Appointment.Instructions,
                    Type = Appointment.Type
                };

                if (string.IsNullOrEmpty(Appointment.DoctorId))
                {
                    _logger.LogWarning("DoctorId is not provided.");
                    ModelState.AddModelError("Appointment.DoctorId", "Doctor is required");
                    await LoadAppointments(user.Id, DateTime.Today);
                    return Page();
                }

                var doctor = await _userManager.FindByIdAsync(Appointment.DoctorId);
                if (doctor == null)
                {
                    _logger.LogWarning("Doctor not found in AspNetUsers.");
                    ModelState.AddModelError("Appointment.DoctorId", "Selected doctor not found");
                    await LoadAppointments(user.Id, DateTime.Today);
                    return Page();
                }

                string? appointmentTimeString = Request.Form["Appointment.AppointmentTime"].ToString();
                if (!string.IsNullOrEmpty(appointmentTimeString) && Appointment.AppointmentTime == default)
                {
                    string[] timeParts = appointmentTimeString.Split(':');
                    if (timeParts.Length == 2 && int.TryParse(timeParts[0], out int hours) && int.TryParse(timeParts[1], out int minutes))
                    {
                        Appointment.AppointmentTime = new TimeSpan(hours, minutes, 0);
                    }
                }

                Appointment.Status = AppointmentStatus.Pending;
                Appointment.CreatedAt = DateTime.Now;
                Appointment.UpdatedAt = DateTime.Now;

                ModelState.Clear();

                if (attachmentFile != null && attachmentFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "appointments");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(attachmentFile.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await attachmentFile.CopyToAsync(fileStream);
                    }

                    Appointment.AttachmentPath = $"/uploads/appointments/{uniqueFileName}";
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Remove this duplicate declaration - we already have newAppointment defined above
                        // var newAppointment = new Barangay.Models.Appointment
                        // {
                        //     DoctorId = Appointment.DoctorId,
                        //     PatientId = Appointment.PatientId,
                        //     PatientName = Appointment.PatientName,
                        //     AppointmentDate = Appointment.AppointmentDate,
                        //     AppointmentTime = Appointment.AppointmentTime,
                        //     ReasonForVisit = Appointment.ReasonForVisit,
                        //     Status = AppointmentStatus.Pending,
                        //     CreatedAt = DateTime.Now,
                        //     UpdatedAt = DateTime.Now,
                        //     AttachmentPath = Appointment.AttachmentPath,
                        //     Description = Appointment.Description,
                        //     Prescription = Appointment.Prescription,
                        //     Instructions = Appointment.Instructions,
                        //     Type = Appointment.Type
                        // };
                        
                        // Use the newAppointment that was already created earlier
                        _context.Appointments.Add(newAppointment);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        
                        _logger.LogInformation($"Appointment booked successfully. ID: {newAppointment.Id}");
                        TempData["SuccessMessage"] = "Appointment booked successfully!";
                        return RedirectToPage();
                    }
                    catch (Exception efEx)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(efEx, "Error with EF insertion: {Message}", efEx.Message);

                        if (efEx.InnerException is SqlException sqlEx && sqlEx.Number == 547)
                        {
                            ModelState.AddModelError("Appointment.DoctorId", "Selected doctor is not valid. Please choose another doctor.");
                        }
                        else
                        {
                            ModelState.AddModelError(string.Empty, $"Error booking appointment: {efEx.Message}");
                        }

                        await LoadAppointments(user.Id, DateTime.Today);
                        return Page();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error booking appointment: {Message}", ex.Message);
                ModelState.AddModelError(string.Empty, $"Error booking appointment: {ex.Message}");
                
                // Fix: Add null check for User before accessing FindFirstValue
                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
                
                await LoadAppointments(userId, DateTime.Today);
                return Page();
            }
        }

        public async Task<JsonResult> OnGetAvailableDoctorsAsync()
        {
            try
            {
                var doctors = await _userManager.GetUsersInRoleAsync("Doctor");

                _logger.LogInformation($"Found {doctors.Count} doctors in role");

                var availableDoctors = doctors.Select(d => 
                {
                    var decryptedDoctor = d.DecryptSensitiveData(_encryptionService, User);
                    // Manually decrypt Email since it's not marked with [Encrypted] attribute
                    if (!string.IsNullOrEmpty(decryptedDoctor.Email) && _encryptionService.IsEncrypted(decryptedDoctor.Email))
                    {
                        decryptedDoctor.Email = decryptedDoctor.Email.DecryptForUser(_encryptionService, User);
                    }
                    return new
                    {
                        id = decryptedDoctor.Id,
                        name = decryptedDoctor.FullName ?? decryptedDoctor.Email ?? decryptedDoctor.UserName,
                        specialization = "General"
                    };
                }).ToList();

                _logger.LogInformation($"Returning {availableDoctors.Count} doctors");

                return new JsonResult(availableDoctors, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available doctors: {Message}", ex.Message);
                return new JsonResult(new List<object>(), _jsonOptions);
            }
        }

        public async Task<JsonResult> OnGetAvailableTimeSlotsAsync(string doctorId, string date, string consultationType)
        {
            try
            {
                if (string.IsNullOrEmpty(doctorId) || string.IsNullOrEmpty(date) || string.IsNullOrEmpty(consultationType))
                    return new JsonResult(new { success = false, error = "Missing required parameters" }, _jsonOptions);

                if (!DateTime.TryParse(date, out DateTime appointmentDate))
                    return new JsonResult(new { success = false, error = "Invalid date format" }, _jsonOptions);

                // Get times booked in Appointments table for ALL appointment types
                var bookedAppointmentTimes = await _context.Appointments
                    .Where(a => a.AppointmentDate.Date == appointmentDate.Date &&
                           a.Status != AppointmentStatus.Cancelled)
                    .Select(a => new { a.AppointmentTime, a.Type })
                    .ToListAsync();
                
                _logger.LogInformation($"Found {bookedAppointmentTimes.Count} booked appointment times on {appointmentDate.ToShortDateString()}");

                // Get booked time slots from ConsultationTimeSlots table
                var bookedTimeSlots = await _context.ConsultationTimeSlots
                    .Where(cts => cts.StartTime.Date == appointmentDate.Date && 
                                  cts.IsBooked)
                    .Select(cts => new { Time = cts.StartTime.TimeOfDay, cts.ConsultationType })
                    .ToListAsync();
                
                _logger.LogInformation($"Found {bookedTimeSlots.Count} booked time slots on {appointmentDate.ToShortDateString()}");

                // Create a set of unavailable times (including 30 minutes before and after each booking)
                var unavailableTimes = new HashSet<TimeSpan>();
                
                // Add all booked appointment times to the unavailable list
                foreach (var bookedTime in bookedAppointmentTimes)
                {
                    // Add the booked time
                    unavailableTimes.Add(bookedTime.AppointmentTime);
                    
                    // Add buffer times based on appointment type duration
                    int bufferMinutes = GetAppointmentTypeDuration(bookedTime.Type);
                    
                    // Add time slots that would overlap with this appointment
                    for (int i = -bufferMinutes; i <= bufferMinutes; i += 30)
                    {
                        var bufferTime = bookedTime.AppointmentTime.Add(TimeSpan.FromMinutes(i));
                        if (bufferTime.TotalMinutes >= 0 && bufferTime.TotalHours < 24)
                        {
                            unavailableTimes.Add(bufferTime);
                        }
                    }
                }
                
                // Add all booked consultation time slots to the unavailable list
                foreach (var bookedSlot in bookedTimeSlots)
                {
                    // Add the booked time
                    unavailableTimes.Add(bookedSlot.Time);
                    
                    // Add buffer times based on consultation type duration
                    int bufferMinutes = GetConsultationTypeDuration(bookedSlot.ConsultationType);
                    
                    // Add time slots that would overlap with this consultation
                    for (int i = -bufferMinutes; i <= bufferMinutes; i += 30)
                    {
                        var bufferTime = bookedSlot.Time.Add(TimeSpan.FromMinutes(i));
                        if (bufferTime.TotalMinutes >= 0 && bufferTime.TotalHours < 24)
                        {
                            unavailableTimes.Add(bufferTime);
                        }
                    }
                }

                // Generate ALL time slots based on consultation type, including unavailable ones
                var allTimeSlots = new List<object>();
                
                // Set start and end times based on consultation type
                var (startHour, endHour) = GetConsultationTypeHours(consultationType);
                
                for (int hour = startHour; hour <= endHour; hour++)
                {
                    for (int minute = 0; minute < 60; minute += 30)
                    {
                        var time = new TimeSpan(hour, minute, 0);
                        string formattedTime = $"{hour:D2}:{minute:D2}";
                        bool isAvailable = !unavailableTimes.Contains(time);
                        
                        allTimeSlots.Add(new {
                            time = formattedTime,
                            isAvailable = isAvailable,
                            displayTime = $"{(hour > 12 ? hour - 12 : hour)}:{minute:D2} {(hour >= 12 ? "PM" : "AM")}"
                        });
                    }
                }
                
                _logger.LogInformation($"Returning {allTimeSlots.Count} time slots for {consultationType} on {appointmentDate.ToShortDateString()}");

                return new JsonResult(new { success = true, timeSlots = allTimeSlots }, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading available time slots");
                return new JsonResult(new { success = false, error = "Error loading available time slots" }, _jsonOptions);
            }
        }
        
        private int GetAppointmentTypeDuration(string appointmentType)
        {
            // Return duration in minutes based on appointment type
            switch (appointmentType?.ToLower())
            {
                case "medical":
                    return 20;
                case "immunization":
                    return 15;
                case "checkup":
                    return 10;
                case "family":
                    return 20;
                default:
                    return 30; // Default buffer
            }
        }
        
        private int GetConsultationTypeDuration(string consultationType)
        {
            // Return duration in minutes based on consultation type
            switch (consultationType?.ToLower())
            {
                case "medical":
                    return 20;
                case "immunization":
                    return 15;
                case "checkup":
                    return 10;
                case "family":
                    return 20;
                default:
                    return 30; // Default buffer
            }
        }
        
        private (int startHour, int endHour) GetConsultationTypeHours(string consultationType)
        {
            // Return start and end hours based on consultation type
            switch (consultationType?.ToLower())
            {
                case "medical":
                    return (8, 17); // 8 AM to 5 PM
                case "immunization":
                    return (8, 12); // 8 AM to 12 PM (mornings only)
                case "checkup":
                    return (8, 17); // 8 AM to 5 PM
                case "family":
                    return (13, 17); // 1 PM to 5 PM (afternoons only)
                default:
                    return (8, 17); // Default hours
            }
        }
    }
}
