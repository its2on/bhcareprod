using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Barangay.Models;
using Barangay.Data;
using Barangay.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Linq;
using Barangay.Extensions;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Barangay.Pages
{
    [Authorize]
    public class BookAppointmentModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<BookAppointmentModel> _logger;
        private readonly IDatabaseDebugService _dbDebugService;
        private readonly IDataEncryptionService _encryptionService;
        private readonly IAuditTrailService _auditTrail;
        private readonly INotificationService _notificationService;
        private readonly IFamilyNumberService _familyNumberService;

        public BookAppointmentModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            ILogger<BookAppointmentModel> logger,
            IDatabaseDebugService dbDebugService,
            IDataEncryptionService encryptionService,
            IAuditTrailService auditTrail,
            INotificationService notificationService,
            IFamilyNumberService familyNumberService)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _dbDebugService = dbDebugService;
            _encryptionService = encryptionService;
            _auditTrail = auditTrail;
            _notificationService = notificationService;
            _familyNumberService = familyNumberService;
        }

        [BindProperty]
        public AppointmentBookingViewModel BookingModel { get; set; } = new();

        [BindProperty]
        public IFormFile? AttachmentFile { get; set; }

        [BindProperty]
        public NCDRiskAssessmentViewModel NCDModel { get; set; } = new();

        [BindProperty]
        public HEEADSSSAssessmentViewModel HEEADSSSModel { get; set; } = new();

        public List<Barangay.Models.Doctor> Doctors { get; set; } = new();

        public UserProfile UserProfile { get; set; } = new();

        // Default doctor used when there's no doctor selection on the UI
        public string DefaultDoctorId { get; set; } = string.Empty;
        
        // Dynamic Forms for this patient
        public List<DynamicFormInfo> AvailableDynamicForms { get; set; } = new();
        
        // Available consultation services
        public List<ConsultationService> ConsultationServices { get; set; } = new();
        
        public class DynamicFormInfo
        {
            public string FormName { get; set; }
            public string IconClass { get; set; }
            public int? MinAge { get; set; }
            public int? MaxAge { get; set; }
        }

        [BindProperty]
        public bool BookingSuccess { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public UserDetailsViewModel UserDetails { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var doctorsInRole = await _userManager.GetUsersInRoleAsync("Doctor");
            try
            {
                // Initialize the booking model and set the first step
                BookingModel = new AppointmentBookingViewModel { CurrentStep = 1 };
                
                // Initialize NCD Risk Assessment Model
                NCDModel = new NCDRiskAssessmentViewModel();
                
                // Initialize HEEADSSS Assessment Model
                HEEADSSSModel = new HEEADSSSAssessmentViewModel();

                // Load user profile data to pre-fill certain fields
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                _logger.LogInformation($"BookAppointment - Before decryption: FullName='{user.FullName}', FirstName='{user.FirstName}', LastName='{user.LastName}', Email='{user.Email}'");

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

                _logger.LogInformation($"BookAppointment - After decryption: FullName='{user.FullName}', FirstName='{user.FirstName}', LastName='{user.LastName}', Email='{user.Email}'");

                    // Set the public property for the Razor page
                    UserDetails = new UserDetailsViewModel
                    {
                        FullName = user.FullName ?? $"{user.FirstName} {user.LastName}".Trim(),
                        Age = CalculateAge(user.BirthDate ?? DateTime.MinValue),
                        Gender = user.Gender ?? "Male" // Default to Male if not set
                    };

                    _logger.LogInformation($"BookAppointment - UserDetails set: FullName='{UserDetails.FullName}', Age={UserDetails.Age}, Gender='{UserDetails.Gender}'");

                    // Pre-fill name if available
                    if (!string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName))
                    {
                        BookingModel.FirstName = user.FirstName;
                        BookingModel.LastName = user.LastName;
                        BookingModel.FullName = $"{user.FirstName} {user.LastName}";
                    }
                    else if (!string.IsNullOrEmpty(user.FullName))
                    {
                        // Split full name if first name and last name are not available
                        var nameParts = user.FullName.Split(' ');
                        if (nameParts.Length >= 2)
                        {
                            BookingModel.FirstName = nameParts[0];
                            BookingModel.LastName = nameParts[nameParts.Length - 1];
                            BookingModel.FullName = user.FullName;
                        }
                    }
                    
                    // Pre-fill address if available
                    if (!string.IsNullOrEmpty(user.Address))
                    {
                        BookingModel.Address = user.Address;
                        NCDModel.Address = user.Address;
                    }
                    
                    // Pre-fill date of birth if available
                    if (user.BirthDate.HasValue)
                    {
                        BookingModel.DateOfBirth = user.BirthDate.Value;
                        NCDModel.Birthday = user.BirthDate.Value;
                        BookingModel.Age = CalculateAge(user.BirthDate.Value);
                    }
                    
                    // Pre-fill phone number if available
                    if (!string.IsNullOrEmpty(user.PhoneNumber))
                    {
                        BookingModel.PhoneNumber = user.PhoneNumber;
                        NCDModel.Telepono = user.PhoneNumber;
                    }
                    
                    // Initialize UserProfile with FamilyNumber
                    UserProfile = new UserProfile
                    {
                        FullName = user.FullName ?? $"{user.FirstName} {user.LastName}".Trim(),
                        Email = user.Email,
                        FamilyNo = user.FamilyNumber
                    };
                }
                else
                {
                    // Initialize empty UserProfile if user not found
                    UserProfile = new UserProfile();
                }

                // Load available doctors with safe fallback
                var doctorUsers = await _userManager.GetUsersInRoleAsync("Doctor");
                var dbDoctors = await _context.Doctors
                    .Where(d => doctorUsers.Select(du => du.Id).Contains(d.UserId))
                    .Include(d => d.User)
                    .ToListAsync();

                if (dbDoctors != null && dbDoctors.Any())
                {
                    Doctors = dbDoctors;
                }
                else
                {
                    // Fallback: build doctor list from AspNetUsers in Doctor role
                    Doctors = doctorUsers
                        .Select(u => new Barangay.Models.Doctor { Id = u.Id, UserId = u.Id, FullName = u.FullName ?? u.UserName })
                        .ToList();
                }

                DefaultDoctorId = Doctors.FirstOrDefault()?.UserId ?? string.Empty;
                
                _logger.LogInformation($"BookAppointment OnGetAsync - Found {Doctors.Count} doctors, DefaultDoctorId: {DefaultDoctorId}");

                // Load dynamic forms that will be shown in appointment workflow
                await LoadAvailableDynamicFormsAsync();

                // Load available consultation services from database
                // Only show services that are active AND either:
                // 1. Have no forms (standalone service), OR
                // 2. Have at least one active form
                ConsultationServices = await _context.ConsultationServices
                    .Include(s => s.AssociatedForms)
                    .Where(s => s.IsActive && (!s.AssociatedForms.Any() || s.AssociatedForms.Any(f => f.IsActive)))
                    .OrderBy(s => s.DisplayOrder)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnGetAsync: {ErrorMessage}", ex.Message);
                
                // Initialize a basic model in case of error
                BookingModel = new AppointmentBookingViewModel { CurrentStep = 1 };
                NCDModel = new NCDRiskAssessmentViewModel();
                HEEADSSSModel = new HEEADSSSAssessmentViewModel();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Clear ModelState to bypass validation
                ModelState.Clear();
                
                if (string.IsNullOrEmpty(BookingModel.HealthFacilityId))
                {
                    BookingModel.HealthFacilityId = GenerateHealthFacilityId();
                }
                
                // Check if user is authenticated before proceeding
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    // Instead of throwing an exception which causes logout, redirect to login
                    _logger.LogWarning("User is not authenticated when submitting form");
                    TempData["ErrorMessage"] = "Your session has expired. Please log in again to continue.";
                    return RedirectToPage("/Account/Login");
                }
                
                // Process form data from submitted fields
                try 
                {
                    // Extract form data directly from the form collection if the model binding didn't work
                    if (string.IsNullOrEmpty(BookingModel.TimeSlot) && Request.Form["timeSlot"].Count > 0)
                    {
                        BookingModel.TimeSlot = Request.Form["timeSlot"];
                        _logger.LogInformation($"Retrieved time slot from form: {BookingModel.TimeSlot}");
                    }
                    
                    if (string.IsNullOrEmpty(BookingModel.AppointmentDate) && Request.Form["appointmentDate"].Count > 0)
                    {
                        BookingModel.AppointmentDate = Request.Form["appointmentDate"];
                        _logger.LogInformation($"Retrieved appointment date from form: {BookingModel.AppointmentDate}");
                    }
                    
                    if (string.IsNullOrEmpty(BookingModel.Gender) && Request.Form["gender"].Count > 0)
                    {
                        BookingModel.Gender = Request.Form["gender"];
                    }
                    else if (string.IsNullOrEmpty(BookingModel.Gender))
                    {
                        // Try to get gender from user profile or default to "Not specified"
                        BookingModel.Gender = user.Gender ?? "Not specified";
                    }
                    
                    // Ensure we have a value for FullName
                    if (string.IsNullOrEmpty(BookingModel.FullName) && !string.IsNullOrEmpty(BookingModel.FirstName) && !string.IsNullOrEmpty(BookingModel.LastName))
                    {
                        BookingModel.FullName = $"{BookingModel.FirstName} {BookingModel.LastName}".Trim();
                    }
                    else if (string.IsNullOrEmpty(BookingModel.FullName) && Request.Form["fullName"].Count > 0)
                    {
                        BookingModel.FullName = Request.Form["fullName"];
                    }
                    
                    _logger.LogInformation("Form data processed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error processing form data: {ex.Message}");
                    // Continue with the data we have
                }
                
                // Process the form submission based on the current step
                if (ModelState.IsValid)
                {
                    try
                    {
                        // Save all booking information to the database
                        SaveBookingInformationAsync();
                    
                        // Display a success message
                        TempData["SuccessMessage"] = "Your appointment has been booked successfully!";
                    
                        // Redirect to User Dashboard instead of Index to prevent logout
                        return RedirectToPage("/User/UserDashboard");
                }
                catch (Exception ex)
                {
                        _logger.LogError(ex, "Error saving booking information");
                        ModelState.AddModelError(string.Empty, "An error occurred while booking your appointment. Please try again later.");
                    return Page();
                }
            }
                else
            {
                    // If the model is invalid, redisplay the form with validation errors
                return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in OnPostAsync");
                TempData["ErrorMessage"] = "An error occurred while booking your appointment. Please try again later.";
                return RedirectToPage("/BookAppointment");
            }
        }

        // Add a new handler for AJAX requests to create appointments
        public async Task<IActionResult> OnPostCreateAjaxAsync()
        {
            try
            {
                _logger.LogInformation("Processing AJAX appointment creation request");
                
                // Clear ModelState to bypass validation
                ModelState.Clear();
                
                // Check if user is authenticated
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User is not authenticated during AJAX appointment creation");
                    return new JsonResult(new { success = false, error = "User not authenticated" });
                }
                
                // Check if booking for someone else
                bool bookingForOther = false;
                string? relationship = null;
                
                if (Request.Form.TryGetValue("bookingForOther", out var bookingForOtherValue))
                {
                    bookingForOther = bookingForOtherValue.ToString().ToLower() == "true";
                    _logger.LogInformation("Received bookingForOther from form: {BookingForOther}", bookingForOther);
                }
                else if (Request.Form.TryGetValue("bookingForOtherHidden", out var bookingForOtherHiddenValue))
                {
                    bookingForOther = bookingForOtherHiddenValue.ToString().ToLower() == "true";
                    _logger.LogInformation("Received bookingForOtherHidden from form: {BookingForOther}", bookingForOther);
                }
                else
                {
                    _logger.LogWarning("bookingForOther not received from form");
                }
                
                // Fallback: If patient details are provided and different from logged-in user, assume booking for other
                if (!bookingForOther && Request.Form.TryGetValue("fullName", out var fullNameValue))
                {
                    var loggedInUser = await _userManager.GetUserAsync(User);
                    if (loggedInUser != null)
                    {
                        loggedInUser = loggedInUser.DecryptSensitiveData(_encryptionService, User);
                        if (!string.IsNullOrEmpty(fullNameValue) && !string.Equals(fullNameValue, loggedInUser.FullName, StringComparison.OrdinalIgnoreCase))
                        {
                            bookingForOther = true;
                            _logger.LogInformation("Detected booking for other based on different patient name: {PatientName} vs {LoggedInUser}", fullNameValue, loggedInUser.FullName);
                        }
                    }
                }
                
                if (bookingForOther && Request.Form.TryGetValue("relationship", out var relationshipValue))
                {
                    relationship = relationshipValue.ToString();
                }
                
                _logger.LogInformation("===== BOOKING FOR OTHER SERVER-SIDE DEBUG =====");
                _logger.LogInformation("BookingForOther detected: {BookingForOther}", bookingForOther);
                _logger.LogInformation("Relationship received: {Relationship}", relationship ?? "NULL");
                
                // Extract form data
                var bookingModel = new AppointmentBookingViewModel();

                if (Request.Form.TryGetValue("fullName", out var fullName))
                {
                    bookingModel.FullName = fullName;
                    _logger.LogInformation("Received fullName from form: {FullName}", fullName);
                    var nameParts = fullName.ToString().Split(' ');
                    if (nameParts.Length > 0)
                        bookingModel.FirstName = nameParts[0];
                    if (nameParts.Length > 1)
                        bookingModel.LastName = nameParts[nameParts.Length - 1];
                }
                else
                {
                    _logger.LogWarning("FullName not received from form");
                }
                
                if (Request.Form.TryGetValue("age", out var age))
                {
                    if (int.TryParse(age, out int ageValue))
                    {
                        bookingModel.Age = ageValue;
                        _logger.LogInformation("Received age from form: {Age}", ageValue);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid age value from form: {AgeRaw}", age.ToString());
                        return new JsonResult(new { success = false, error = "Please enter a valid age (0-120)." });
                    }
                }
                
                if (Request.Form.TryGetValue("birthday", out var birthday) && DateTime.TryParse(birthday, out DateTime birthdayValue))
                {
                    bookingModel.Birthday = birthdayValue;
                    _logger.LogInformation("Received birthday from form: {Birthday}", birthdayValue);
                }
                else
                {
                    _logger.LogWarning("Birthday not received or invalid from form. Value: {Birthday}", birthday);
                }
                
                if (Request.Form.TryGetValue("phoneNumber", out var phoneNumber))
                {
                    bookingModel.PhoneNumber = phoneNumber;
                }
                
                if (Request.Form.TryGetValue("gender", out var gender))
                {
                    bookingModel.Gender = gender;
                    _logger.LogInformation("Received gender from form: {Gender}", gender);
                }
                else
                {
                    _logger.LogWarning("Gender not received from form");
                }
                
                if (Request.Form.TryGetValue("appointmentDate", out var appointmentDate))
                {
                    bookingModel.AppointmentDate = appointmentDate;
                }
                
                if (Request.Form.TryGetValue("timeSlot", out var timeSlot))
                {
                    bookingModel.TimeSlot = timeSlot;
                }
                
                if (Request.Form.TryGetValue("consultationType", out var consultationType))
                {
                    bookingModel.ConsultationType = consultationType;
                }
                
                if (Request.Form.TryGetValue("ReasonForVisit", out var reasonForVisit))
                {
                    bookingModel.ReasonForVisit = reasonForVisit.ToString();
                }

                if (Request.Form.TryGetValue("DoctorId", out var doctorIdValue))
                {
                    bookingModel.DoctorId = doctorIdValue;
                }
                
                // Extract family number from model
                string familyNumber = BookingModel.FamilyNumber ?? string.Empty;
                _logger.LogInformation("Received family number from model: {FamilyNumber}", familyNumber);
                
                // Also try from form as fallback for compatibility
                if (string.IsNullOrEmpty(familyNumber) && Request.Form.TryGetValue("familyNumber", out var familyNumberValue))
                {
                    familyNumber = familyNumberValue.ToString();
                    _logger.LogInformation("Received family number from form (fallback): {FamilyNumber}", familyNumber);
                }
                
                if (bookingForOther)
                {
                    bookingModel.Relationship = relationship;
                }

                // Server-side validation for age, phone number, and profanity in reason
                if (bookingModel.Age < 0 || bookingModel.Age > 120)
                {
                    return new JsonResult(new { success = false, error = "Age must be between 0 and 120." });
                }

                if (string.IsNullOrWhiteSpace(bookingModel.PhoneNumber) || !Regex.IsMatch(bookingModel.PhoneNumber, @"^(?:09\d{9}|\+63\d{9,12})$"))
                {
                    return new JsonResult(new { success = false, error = "Contact number must be 11 digits starting with 09 or 12-15 digits starting with +63." });
                }

                if (!string.IsNullOrWhiteSpace(bookingModel.ReasonForVisit))
                {
                    // Basic profanity filter (EN/TL)
                    var badWords = new[] { "fuck","shit","bitch","asshole","bastard","damn","puta","putang ina","pakyu","ulol","gago","tarantado","tangina" };
                    var containsBad = badWords.Any(w => Regex.IsMatch(bookingModel.ReasonForVisit, $"(^|\\b){Regex.Escape(w)}(\\b|$)", RegexOptions.IgnoreCase));
                    if (containsBad)
                    {
                        return new JsonResult(new { success = false, error = "Please remove inappropriate language from the Reason for Visit." });
                    }
                    if (!Regex.IsMatch(bookingModel.ReasonForVisit, @"^[A-Za-z0-9\s]{1,300}$"))
                    {
                        return new JsonResult(new { success = false, error = "Reason for Visit can only contain letters, numbers, and spaces (max 300 characters)." });
                    }
                    if (bookingModel.ReasonForVisit.Length > 300)
                    {
                        return new JsonResult(new { success = false, error = "Reason for Visit must be 300 characters or less." });
                    }
                }

                // Verify that the selected time slot is still available
                var validationResult = await ValidateTimeSlotAsync(bookingModel);
                if (validationResult != null)
                {
                    return validationResult;
                }
                
                try
                {
                    var appointmentId = await CreateTemporaryAppointmentAsync(user.Id, bookingModel, bookingForOther, familyNumber);
                    
                    if (appointmentId > 0)
                    {
                        _logger.LogInformation($"Successfully created temporary appointment with ID: {appointmentId}");
                        
                        // Create a notification for the doctor
                        if (!string.IsNullOrEmpty(bookingModel.DoctorId))
                        {
                            var notification = new Notification
                            {
                                Title = "New Appointment",
                                Message = $"You have a new appointment with {bookingModel.FullName} on {bookingModel.AppointmentDate:d} at {bookingModel.TimeSlot:t}.",
                                UserId = bookingModel.DoctorId, // The user who the notification is for
                                RecipientId = bookingModel.DoctorId,
                                CreatedAt = DateTime.Now,
                                IsRead = false,
                                Type = "Info",
                                Link = "/Doctor/Appointments"
                            };
                            _context.Notifications.Add(notification);
                            await _context.SaveChangesAsync();
                        }

                        return new JsonResult(new { 
                            success = true, 
                            appointmentId = appointmentId,
                            age = bookingModel.Age, // Frontend expects 'age' not 'AgeValue'
                            bookingForOther = bookingForOther,
                            relationship = relationship,
                            message = "Appointment created as draft. Please complete the assessment form to finalize your booking."
                        });
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create temporary appointment");
                        return new JsonResult(new { success = false, error = "Failed to create appointment" });
                    }
                }
                catch (InvalidOperationException iex)
                {
                    _logger.LogWarning($"Invalid operation during appointment creation: {iex.Message}");
                    return new JsonResult(new { 
                        success = false, 
                        error = iex.Message,
                        errorType = "TimeSlotConflict"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating appointment");
                    return new JsonResult(new { 
                        success = false, 
                        error = "Error creating appointment: " + ex.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnPostCreateAjaxAsync");
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }
        
        private async Task EnsurePatientRecordExistsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;
            
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
            
            if (patient == null)
            {
                patient = new Patient
                {
                    UserId = userId,
                    FullName = user.FullName ?? "",
                    Gender = user.Gender ?? "Not specified",
                    BirthDate = user.BirthDate ?? DateTime.Now.AddYears(-30),
                    Address = user.Address ?? "",
                    ContactNumber = user.PhoneNumber ?? "",
                    EmergencyContact = "To be updated",
                    EmergencyContactNumber = "To be updated",
                    Email = user.Email ?? "",
                    FamilyNumber = user.FamilyNumber,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Created new patient record for user ID: {userId} with FamilyNumber: {user.FamilyNumber}");
            }
            else if (!string.IsNullOrWhiteSpace(user.FamilyNumber) && 
                     string.IsNullOrWhiteSpace(patient.FamilyNumber))
            {
                // Sync family number from user to patient if missing
                if (!string.IsNullOrWhiteSpace(user.FamilyNumber))
                {
                    patient.FamilyNumber = user.FamilyNumber;
                    patient.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Updated existing patient record for user ID: {userId} with FamilyNumber: {user.FamilyNumber}");
                }
            }
        }

        
        private async Task<int> CreateTemporaryAppointmentAsync(string userId, AppointmentBookingViewModel bookingModel, bool bookingForOther, string familyNumber = null)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) throw new Exception("User not found.");

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

                // ALWAYS ensure Patient record exists before creating appointment
                // This is required to satisfy the FK_Appointments_Patients_PatientId foreign key constraint
                await EnsurePatientRecordExistsAsync(userId);

                // Save family number to patient record if provided (for regular bookings only)
                // For "booking for someone else", family number is saved to Appointment.FamilyNumber below
                if (!string.IsNullOrEmpty(familyNumber) && !bookingForOther)
                {
                    // For regular bookings, save to the logged-in user's record
                    var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
                    if (patient != null)
                    {
                        // Save or update family number
                        if (string.IsNullOrWhiteSpace(patient.FamilyNumber))
                        {
                            patient.FamilyNumber = familyNumber;
                            patient.UpdatedAt = DateTime.UtcNow;
                            _context.Patients.Update(patient);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Updated patient record with family number: {FamilyNumber} for user: {UserId}", 
                                familyNumber, userId);
                        }
                        else
                        {
                            _logger.LogInformation("Patient {UserId} already has family number: {ExistingFamilyNumber}", 
                                userId, patient.FamilyNumber);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Patient record not found for user: {UserId}, family number not saved", userId);
                    }
                }

                if (!DateTime.TryParse(bookingModel.AppointmentDate, out DateTime appointmentDate))
                {
                    _logger.LogError("Invalid appointment date supplied: {AppointmentDate}", bookingModel.AppointmentDate);
                    return -1;
                }
                
                // Convert from 12-hour format to TimeSpan
                // Handle both single time ("8:00 AM") and time range ("8:00 AM - 8:06 AM")
                TimeSpan selectedApptTime;
                string timeSlotToParse = bookingModel.TimeSlot ?? string.Empty;
                if (string.IsNullOrWhiteSpace(timeSlotToParse))
                {
                    _logger.LogError("Time slot was not provided for appointment creation.");
                    return -1;
                }
                
                // If it's a time range (contains " - "), extract the start time
                if (timeSlotToParse.Contains(" - "))
                {
                    timeSlotToParse = timeSlotToParse.Split(new[] { " - " }, StringSplitOptions.None)[0].Trim();
                    _logger.LogInformation($"Extracted start time from range: {bookingModel.TimeSlot} -> {timeSlotToParse}");
                }
                
                if (DateTime.TryParse(timeSlotToParse, out DateTime parsedTime))
                {
                    selectedApptTime = parsedTime.TimeOfDay;
                    _logger.LogInformation($"Successfully parsed time: {timeSlotToParse} to {selectedApptTime}");
                }
                else
                {
                    _logger.LogError($"Failed to parse time string: {timeSlotToParse} (original: {bookingModel.TimeSlot})");
                    return -1; // Indicate failure with a negative value
                }
                string selectedConsultationType = bookingModel.ConsultationType ?? "medical";
                
                // Look up the ServiceId from ConsultationType
                int? serviceId = null;
                var consultationService = await _context.ConsultationServices
                    .FirstOrDefaultAsync(s => s.ServiceName.ToLower() == selectedConsultationType.ToLower());
                if (consultationService != null)
                {
                    serviceId = consultationService.ServiceId;
                    _logger.LogInformation("Mapped consultation type '{ConsultationType}' to ServiceId: {ServiceId}", 
                        selectedConsultationType, serviceId);
                }
                else
                {
                    _logger.LogWarning("Could not find ServiceId for consultation type: {ConsultationType}", 
                        selectedConsultationType);
                }

                // Centralized time slot validation
                var validationResult = await ValidateTimeSlotAsync(bookingModel);
                if (validationResult != null)
                {
                    throw new InvalidOperationException("Time slot conflict.");
                }

                // FIXED: Store patient data differently based on who the appointment is for
                string patientName;
                string? dependentFullName = null;
                int? dependentAge = null;
                DateTime? dependentBirthday = null;
                
                if (bookingForOther)
                {
                    // For dependent bookings: store booker's name in PatientName, dependent's info in Dependent* fields
                    patientName = user.FullName; // Booker's name
                    dependentFullName = bookingModel.FullName; // Dependent's name
                    dependentAge = bookingModel.Age;
                    dependentBirthday = bookingModel.Birthday;
                }
                else
                {
                    // For self bookings: only PatientName is used
                    patientName = user.FullName;
                }
                
                // Use the age from the form if available, otherwise calculate from birth date
                var userBirthDate = user.BirthDate ?? DateTime.MinValue;
                var patientAge = bookingForOther ? bookingModel.Age : 
                    (bookingModel.Age > 0 ? bookingModel.Age : CalculateAge(userBirthDate));
                var patientBirthday = bookingForOther ? bookingModel.Birthday : userBirthDate;

                _logger.LogInformation("===== APPOINTMENT CREATION DEBUG =====");
                _logger.LogInformation("BookingForOther: {BookingForOther}", bookingForOther);
                _logger.LogInformation("PatientName (Booker): {PatientName}", patientName);
                _logger.LogInformation("DependentFullName: {DependentFullName}", dependentFullName ?? "NULL");
                _logger.LogInformation("DependentAge: {DependentAge}", dependentAge?.ToString() ?? "NULL");
                _logger.LogInformation("AgeValue (Person receiving care): {AgeValue}", patientAge);
                _logger.LogInformation("Birthday: {Birthday}", patientBirthday);
                _logger.LogInformation("Relationship: {Relationship}", bookingModel.Relationship ?? "NULL");
                _logger.LogInformation("FamilyNumber: {FamilyNumber}", familyNumber ?? "NULL");
                _logger.LogInformation("===== END APPOINTMENT CREATION DEBUG =====");

                // Use the selected doctor from the booking model
                var doctor = await _context.Users.FindAsync(bookingModel.DoctorId);
                
                // Determine initial status based on whether forms exist for this service
                // If forms exist, status should be Draft (user must complete forms first)
                // If no forms exist, status should be Pending (ready for doctor review)
                var hasFormsForService = await _context.FormTemplates
                    .AnyAsync(f => f.IsActive && 
                                  f.ShowInAppointmentFlow && 
                                  (f.ServiceId == serviceId || 
                                   (f.ServiceId == null && selectedConsultationType.ToLower() == "general consult")));
                
                var initialStatus = hasFormsForService ? AppointmentStatus.Draft : AppointmentStatus.Pending;
                
                _logger.LogInformation("Service '{Service}' (ServiceId: {ServiceId}) has forms: {HasForms}, Initial status: {Status}", 
                    selectedConsultationType, serviceId, hasFormsForService, initialStatus);
                
                var newAppointment = new Models.Appointment
                {
                    ApplicationUserId = userId, // Link to the user who booked the appointment
                    // PatientId always points to the logged-in user (booker) to satisfy FK constraint
                    // PatientName = booker's name (logged-in user)
                    // DependentFullName = dependent's name (if booking for someone else)
                    PatientId = userId,
                    PatientName = patientName, // Always the booker's name
                    DependentFullName = dependentFullName, // Dependent's name (null if booking for self)
                    DependentAge = dependentAge, // Dependent's age (null if booking for self)
                    AgeValue = patientAge, // Age of the person receiving care (dependent or self)
                    DateOfBirth = patientBirthday, // DOB of the person receiving care
                    Gender = bookingModel.Gender,
                    ContactNumber = bookingModel.PhoneNumber,
                    AppointmentDate = appointmentDate,
                    AppointmentTime = selectedApptTime,
                    Type = selectedConsultationType,
                    ServiceId = serviceId, // Link to ConsultationService for form filtering
                    ReasonForVisit = bookingModel.ReasonForVisit,
                    Status = initialStatus, // Set to Pending for non-assessment types, Draft for others
                    DoctorId = doctor?.Id, // Assign a doctor if found
                    BookingForOther = bookingForOther,
                    Relationship = bookingForOther ? bookingModel.Relationship : null,
                    FamilyNumber = familyNumber // Store family number with appointment
                };

                _context.Appointments.Add(newAppointment);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("===== APPOINTMENT SAVED TO DATABASE =====");
                _logger.LogInformation("Appointment ID: {AppointmentId}", newAppointment.Id);
                _logger.LogInformation("SAVED - PatientName: {PatientName}", newAppointment.PatientName);
                _logger.LogInformation("SAVED - DependentFullName: {DependentFullName}", newAppointment.DependentFullName ?? "NULL");
                _logger.LogInformation("SAVED - DependentAge: {DependentAge}", newAppointment.DependentAge?.ToString() ?? "NULL");
                _logger.LogInformation("SAVED - BookingForOther: {BookingForOther}", newAppointment.BookingForOther);
                _logger.LogInformation("SAVED - Relationship: {Relationship}", newAppointment.Relationship ?? "NULL");
                _logger.LogInformation("SAVED - FamilyNumber: {FamilyNumber}", newAppointment.FamilyNumber ?? "NULL");
                _logger.LogInformation("===== END DATABASE SAVE =====");
                
                // Save family number to ApplicationUser record as well
                if (!string.IsNullOrEmpty(familyNumber))
                {
                    if (string.IsNullOrEmpty(user.FamilyNumber))
                    {
                        user.FamilyNumber = familyNumber;
                        await _userManager.UpdateAsync(user);
                        _logger.LogInformation("Updated ApplicationUser {UserId} with family number: {FamilyNumber}", userId, familyNumber);
                    }
                    
                    _logger.LogInformation("Appointment created with FamilyNumber: {FamilyNumber} for {PatientName} (BookingForOther: {BookingForOther})", 
                        familyNumber, patientName, bookingForOther);
                    
                    // If booking for someone else, create/update FamilyMember record
                    if (bookingForOther && !string.IsNullOrEmpty(bookingModel.Relationship))
                    {
                        var existingFamilyMember = await _context.FamilyMembers
                            .FirstOrDefaultAsync(fm => fm.PatientId == userId && 
                                                      fm.Name == patientName &&
                                                      fm.FamilyNumber == familyNumber);
                        
                        if (existingFamilyMember == null)
                        {
                            var familyMember = new FamilyMember
                            {
                                PatientId = userId,
                                UserId = userId,
                                Name = patientName,
                                Relationship = bookingModel.Relationship,
                                Age = patientAge,
                                ContactNumber = bookingModel.PhoneNumber,
                                FamilyNumber = familyNumber,
                                CreatedAt = DateTime.UtcNow
                            };
                            
                            _context.FamilyMembers.Add(familyMember);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Created FamilyMember record for {Name} with family number {FamilyNumber}", 
                                patientName, familyNumber);
                        }
                        else
                        {
                            _logger.LogInformation("FamilyMember record already exists for {Name} with family number {FamilyNumber}", 
                                patientName, familyNumber);
                        }
                    }
                }

                // AUDIT: Log appointment booking
                await _auditTrail.LogAsync(
                    "Create",
                    $"Booked appointment for {appointmentDate:yyyy-MM-dd}",
                    "Appointment",
                    newAppointment.Id.ToString(),
                    null,
                    JsonConvert.SerializeObject(new {
                        AppointmentDate = appointmentDate.ToString("yyyy-MM-dd"),
                        AppointmentTime = selectedApptTime,
                        Type = selectedConsultationType,
                        DoctorId = doctor?.Id,
                        Status = initialStatus,
                        BookingForOther = bookingForOther
                    }),
                    $"Patient booked appointment - Type: {selectedConsultationType}"
                );

                // Create notification for the user
                try
                {
                    var notificationMessage = $"Your appointment on {appointmentDate:MMM dd, yyyy} at {bookingModel.TimeSlot} has been successfully booked. Type: {selectedConsultationType}.";
                    
                    await _notificationService.CreateNotificationForUserAsync(
                        userId,
                        "Appointment Booked",
                        notificationMessage,
                        "Appointment Booked",
                        $"/User/Appointments"
                    );
                    
                    _logger.LogInformation($"Notification created for user {userId} - Appointment booked");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating notification for appointment booking");
                    // Don't fail the appointment booking if notification fails
                }

                return newAppointment.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating temporary appointment");
                return 0; // Indicate failure
            }
        }

        private string GenerateHealthFacilityId()
        {
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string numbers = "0123456789";
            var random = new Random();
            
            return new string(Enumerable.Repeat(letters, 4)
                .Select(s => s[random.Next(s.Length)]).ToArray()) +
                new string(Enumerable.Repeat(numbers, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        
        public IActionResult SaveBookingInformationAsync()
        {
            // This method appears to be a remnant of a previous implementation and is no longer called.
            // The logic is now handled by OnPostCreateAjaxAsync and CreateTemporaryAppointmentAsync.
            // To prevent confusion, it can be removed or marked as obsolete.
            _logger.LogWarning("SaveBookingInformationAsync was called, but it is considered obsolete.");
            return Page();
        }

        private int CalculateAge(DateTime dateOfBirth)
        {
            // Handle default/invalid birth dates
            if (dateOfBirth == default(DateTime) || dateOfBirth == DateTime.MinValue || dateOfBirth.Year < 1900)
            {
                return 0; // Return 0 for invalid birth dates
            }
            
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }
        
        // Handler for generating family numbers
        public async Task<JsonResult> OnPostGenerateFamilyNumberAsync([FromBody] GenerateFamilyNumberRequest request)
        {
            try
            {
                _logger.LogInformation("=== GENERATE FAMILY NUMBER REQUEST ===");
                _logger.LogInformation("LastName: {LastName}, SameFamily: {SameFamily}", request.LastName, request.SameFamily);
                
                if (string.IsNullOrWhiteSpace(request.LastName))
                {
                    return new JsonResult(new { success = false, error = "Last name is required" });
                }
                
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return new JsonResult(new { success = false, error = "User not found" });
                }
                
                // Use the new service method that handles both generation and reuse
                var response = await _familyNumberService.GenerateOrReuseFamilyNumberAsync(
                    request.LastName, 
                    user.Id, 
                    request.SameFamily);
                
                if (!response.Success)
                {
                    _logger.LogError("Failed to process family number: {Error}", response.Error);
                    return new JsonResult(new { success = false, error = response.Error });
                }
                
                // Save family number to user profile if not already set
                if (string.IsNullOrWhiteSpace(user.FamilyNumber))
                {
                    user.FamilyNumber = response.FamilyNumber;
                    _logger.LogInformation("Updated user FamilyNumber: {FamilyNumber}", response.FamilyNumber);
                }
                
                // Also update the Patient record if it exists
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (patient != null)
                {
                    if (string.IsNullOrWhiteSpace(patient.FamilyNumber))
                    {
                        patient.FamilyNumber = response.FamilyNumber;
                        patient.UpdatedAt = DateTime.UtcNow;
                        _logger.LogInformation("Updated Patient record with FamilyNumber: {FamilyNumber}", response.FamilyNumber);
                    }
                }
                else
                {
                    _logger.LogWarning("Patient record not found for user {UserId}, will be created when booking appointment", user.Id);
                }
                
                await _context.SaveChangesAsync();
                
                // Log audit trail
                await _auditTrail.LogAsync(
                    response.IsPreexisting ? "Reused" : "Generated",
                    $"Family number {response.FamilyNumber} for {request.LastName}",
                    "FamilyNumber",
                    response.FamilyNumber,
                    null,
                    JsonConvert.SerializeObject(new {
                        LastName = request.LastName,
                        FamilyNumber = response.FamilyNumber,
                        SameFamily = request.SameFamily,
                        IsPreexisting = response.IsPreexisting
                    })
                );
                
                _logger.LogInformation("Family number {FamilyNumber} assigned to user {UserId}", response.FamilyNumber, user.Id);
                
                return new JsonResult(new { 
                    success = true, 
                    familyNumber = response.FamilyNumber,
                    isPreexisting = response.IsPreexisting,
                    message = response.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing family number");
                return new JsonResult(new { success = false, error = "An error occurred while processing the family number" });
            }
        }

        private int GetConsultationTypeDuration(string consultationType)
        {
            // Return duration in minutes based on consultation type
            switch (consultationType?.ToLower())
            {
                case "general consult":
                    return 30;
                case "dental":
                    return 45;
                case "immunization":
                    return 20;
                case "prenatal & family planning":
                case "prenatal and family planning":
                    return 30;
                case "dots consult":
                    return 30;
                default:
                    return 30; // Default duration
            }
        }

        // Defines allowed days and time windows per consultation type
        // Windows are in 24-hour TimeSpan ranges and will be intersected with doctor availability
        private (HashSet<DayOfWeek> Days, List<(TimeSpan Start, TimeSpan End)> Windows) GetConsultationTypeSchedule(string consultationType)
        {
            var type = consultationType?.ToLower() ?? string.Empty;
            var days = new HashSet<DayOfWeek>();
            var windows = new List<(TimeSpan Start, TimeSpan End)>();

            switch (type)
            {
                // General Consult (8AM-11AM, 1PM-4PM, Mon-Fri) - TEMPORARILY INCLUDING WEEKENDS FOR TESTING
                case "general consult":
                    days.Add(DayOfWeek.Monday);
                    days.Add(DayOfWeek.Tuesday);
                    days.Add(DayOfWeek.Wednesday);
                    days.Add(DayOfWeek.Thursday);
                    days.Add(DayOfWeek.Friday);
                    // TEMPORARILY ADD WEEKENDS FOR TESTING
                    days.Add(DayOfWeek.Saturday);
                    days.Add(DayOfWeek.Sunday);
                    windows.Add((TimeSpan.FromHours(8), TimeSpan.FromHours(11)));
                    windows.Add((TimeSpan.FromHours(13), TimeSpan.FromHours(16)));
                    break;

                // Dental (8-11AM, Mon/Wed/Fri) - TEMPORARILY INCLUDING WEEKENDS FOR TESTING
                case "dental":
                    days.Add(DayOfWeek.Monday);
                    days.Add(DayOfWeek.Wednesday);
                    days.Add(DayOfWeek.Friday);
                    // TEMPORARILY ADD WEEKENDS FOR TESTING
                    days.Add(DayOfWeek.Saturday);
                    days.Add(DayOfWeek.Sunday);
                    windows.Add((TimeSpan.FromHours(8), TimeSpan.FromHours(11)));
                    break;

                // Immunization (8AM-12PM, Wed) - TEMPORARILY INCLUDING WEEKENDS FOR TESTING
                case "immunization":
                    days.Add(DayOfWeek.Wednesday);
                    // TEMPORARILY ADD WEEKENDS FOR TESTING
                    days.Add(DayOfWeek.Saturday);
                    days.Add(DayOfWeek.Sunday);
                    windows.Add((TimeSpan.FromHours(8), TimeSpan.FromHours(12)));
                    break;

                // Prenatal & Family Planning (8AM-11AM, 1PM-4PM, Mon/Wed/Fri) - TEMPORARILY INCLUDING WEEKENDS FOR TESTING
                case "prenatal & family planning":
                case "prenatal and family planning":
                    days.Add(DayOfWeek.Monday);
                    days.Add(DayOfWeek.Wednesday);
                    days.Add(DayOfWeek.Friday);
                    // TEMPORARILY ADD WEEKENDS FOR TESTING
                    days.Add(DayOfWeek.Saturday);
                    days.Add(DayOfWeek.Sunday);
                    windows.Add((TimeSpan.FromHours(8), TimeSpan.FromHours(11)));
                    windows.Add((TimeSpan.FromHours(13), TimeSpan.FromHours(16)));
                    break;

                // DOTS Consult (1-4PM, Mon-Fri) - TEMPORARILY INCLUDING WEEKENDS FOR TESTING
                case "dots consult":
                    days.Add(DayOfWeek.Monday);
                    days.Add(DayOfWeek.Tuesday);
                    days.Add(DayOfWeek.Wednesday);
                    days.Add(DayOfWeek.Thursday);
                    days.Add(DayOfWeek.Friday);
                    // TEMPORARILY ADD WEEKENDS FOR TESTING
                    days.Add(DayOfWeek.Saturday);
                    days.Add(DayOfWeek.Sunday);
                    windows.Add((TimeSpan.FromHours(13), TimeSpan.FromHours(16)));
                    break;


                // Default: fallback to general weekdays, full day window (will also be intersected with doctor availability) - TEMPORARILY INCLUDING WEEKENDS FOR TESTING
                default:
                    days.Add(DayOfWeek.Monday);
                    days.Add(DayOfWeek.Tuesday);
                    days.Add(DayOfWeek.Wednesday);
                    days.Add(DayOfWeek.Thursday);
                    days.Add(DayOfWeek.Friday);
                    // TEMPORARILY ADD WEEKENDS FOR TESTING
                    days.Add(DayOfWeek.Saturday);
                    days.Add(DayOfWeek.Sunday);
                    windows.Add((TimeSpan.FromHours(8), TimeSpan.FromHours(17)));
                    break;
            }

            return (days, windows);
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
                _logger.LogError(ex, "Error fixing weekend appointments");
                return new JsonResult(new { 
                    success = false, 
                    error = ex.Message 
                });
            }
        }

        public async Task<IActionResult> OnGetBookedTimeSlotsAsync(string date, string consultationType, string doctorId)
        {
            try
            {
                _logger.LogInformation($"Getting time slots for date: {date}, consultation type: {consultationType}, doctor: {doctorId}");

                if (string.IsNullOrEmpty(doctorId) || !DateTime.TryParse(date, out var selectedDate))
                {
                    _logger.LogWarning($"Invalid parameters: doctorId={doctorId}, date={date}");
                    return new JsonResult(new { availableSlots = new List<string>(), debug = "Invalid parameters" });
                }

                // Log all available doctors for debugging
                var allDoctors = await _context.DoctorAvailabilities.ToListAsync();
                _logger.LogInformation($"Found {allDoctors.Count} doctor availability records");
                foreach (var doc in allDoctors)
                {
                    _logger.LogInformation($"Doctor ID: {doc.DoctorId}, Available: {doc.IsAvailable}");
                }

                var bookedAppointments = await _context.Appointments
                    .Where(a => a.AppointmentDate.Date == selectedDate.Date
                                && a.Status != AppointmentStatus.Cancelled)
                    .Select(a => new { a.AppointmentTime, a.Type })
                    .ToListAsync();

                var dayOfWeek = selectedDate.DayOfWeek;
                _logger.LogInformation($"Selected date: {selectedDate:yyyy-MM-dd}, Day of week: {dayOfWeek}");

                var clinicSchedule = await _context.DoctorAvailabilities.FirstOrDefaultAsync(cs => cs.DoctorId == doctorId);
                int slotDuration = GetConsultationTypeDuration(consultationType);
                _logger.LogInformation($"Consultation duration: {slotDuration} minutes");

                if (clinicSchedule == null)
                {
                    _logger.LogWarning($"DATABASE CHECK: No clinic schedule found for doctor ID '{doctorId}'. Please ensure the DoctorAvailabilities table has a record for this doctor.");
                    var allSchedules = await _context.DoctorAvailabilities.ToListAsync();
                    _logger.LogWarning($"DATABASE CHECK: Found {allSchedules.Count} total records in DoctorAvailabilities table.");

                    // Create a default availability for the doctor - ENABLE WEEKENDS
                    clinicSchedule = new DoctorAvailability
                    {
                        DoctorId = doctorId,
                        IsAvailable = true,
                        Monday = true,
                        Tuesday = true,
                        Wednesday = true,
                        Thursday = true,
                        Friday = true,
                        Saturday = true,  // ENABLE SATURDAY
                        Sunday = true,    // ENABLE SUNDAY
                        StartTime = new TimeSpan(8, 0, 0), // 8:00 AM
                        EndTime = new TimeSpan(17, 0, 0),  // 5:00 PM
                        LastUpdated = DateTime.UtcNow
                    };

                    _context.DoctorAvailabilities.Add(clinicSchedule);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Created default availability for doctor {doctorId}");

                    return new JsonResult(new { 
                        availableSlots = new List<string>(),
                        debug = "Created new doctor availability. Please try again." 
                    });
                }
                else
                {
                    _logger.LogInformation($"DATABASE CHECK: Found clinic schedule for doctor ID '{doctorId}'.");
                }

                bool isDoctorAvailableOnDay = dayOfWeek switch
                {
                    DayOfWeek.Monday => clinicSchedule.Monday,
                    DayOfWeek.Tuesday => clinicSchedule.Tuesday,
                    DayOfWeek.Wednesday => clinicSchedule.Wednesday,
                    DayOfWeek.Thursday => clinicSchedule.Thursday,
                    DayOfWeek.Friday => clinicSchedule.Friday,
                    DayOfWeek.Saturday => clinicSchedule.Saturday,
                    DayOfWeek.Sunday => clinicSchedule.Sunday,
                    _ => false
                };

                // Check consultation-type-specific schedule
                var (allowedDays, typeWindows) = GetConsultationTypeSchedule(consultationType);

                _logger.LogInformation($"Doctor availability on {dayOfWeek}: {isDoctorAvailableOnDay}, IsAvailable: {clinicSchedule.IsAvailable}");
                _logger.LogInformation($"Consultation windows: {string.Join(", ", typeWindows.Select(w => $"{w.Start}-{w.End}"))}");
                _logger.LogInformation($"Allowed days for consultation: {string.Join(", ", allowedDays)}");

                if (!clinicSchedule.IsAvailable || !isDoctorAvailableOnDay)
                {
                    _logger.LogWarning($"Doctor {doctorId} is not available on {dayOfWeek}");
                    return new JsonResult(new { 
                        availableSlots = new List<string>(),
                        debug = $"Doctor not available on {dayOfWeek}" 
                    });
                }

                if (!allowedDays.Contains(dayOfWeek))
                {
                    _logger.LogInformation($"Consultation type '{consultationType}' is not offered on {dayOfWeek}.");
                    return new JsonResult(new { 
                        availableSlots = new List<string>(),
                        debug = $"Consultation type '{consultationType}' is not offered on {dayOfWeek}" 
                    });
                }
                
                _logger.LogInformation($"Found clinic schedule: Start={clinicSchedule.StartTime}, End={clinicSchedule.EndTime}, Available={clinicSchedule.IsAvailable}");

                var availableSlotsSet = new HashSet<string>();

                // Intersect doctor availability with consultation windows
                foreach (var window in typeWindows)
                {
                    var rangeStart = clinicSchedule.StartTime > window.Start ? clinicSchedule.StartTime : window.Start;
                    var rangeEnd = clinicSchedule.EndTime < window.End ? clinicSchedule.EndTime : window.End;

                    _logger.LogInformation($"Window range: {rangeStart}-{rangeEnd} (doctor hours: {clinicSchedule.StartTime}-{clinicSchedule.EndTime}, consultation window: {window.Start}-{window.End})");

                    if (rangeStart >= rangeEnd)
                    {
                        _logger.LogWarning($"No overlap between doctor hours and consultation window: {rangeStart} >= {rangeEnd}");
                        continue; // No overlap between doctor hours and consultation window
                    }

                    _logger.LogInformation($"Generating time slots within window {rangeStart} - {rangeEnd} with {slotDuration} minute intervals");

                    var currentTime = rangeStart;
                    int slotsGenerated = 0;
                    int slotsAvailable = 0;
                    while (currentTime.Add(TimeSpan.FromMinutes(slotDuration)) <= rangeEnd)
                    {
                        var slotStart = currentTime;
                        var slotEnd = currentTime.Add(TimeSpan.FromMinutes(slotDuration));
                        slotsGenerated++;

                        // Remove the check for past times on the current day
                        // This allows booking any time slot regardless of current time

                        bool isBooked = bookedAppointments.Any(b =>
                        {
                            var bookedStart = b.AppointmentTime;
                            var bookedDuration = GetConsultationTypeDuration(string.IsNullOrWhiteSpace(b.Type) ? consultationType : b.Type);
                            var bookedEnd = bookedStart.Add(TimeSpan.FromMinutes(bookedDuration));
                            return slotStart < bookedEnd && bookedStart < slotEnd;
                        });

                        if (!isBooked)
                        {
                            // Format the time as "hh:mm AM/PM" instead of "hh:mm"
                            string formattedTime = DateTime.Today.Add(currentTime).ToString("h:mm tt");
                            availableSlotsSet.Add(formattedTime);
                            slotsAvailable++;
                        }

                        currentTime = currentTime.Add(TimeSpan.FromMinutes(slotDuration)); // Move to the next slot
                    }
                    _logger.LogInformation($"Generated {slotsGenerated} slots, {slotsAvailable} available");
                }

                var availableSlots = availableSlotsSet.OrderBy(t => DateTime.Parse(t).TimeOfDay).ToList();

                _logger.LogInformation($"Generated {availableSlots.Count} available time slots: [{string.Join(", ", availableSlots)}]");
                return new JsonResult(new { 
                    availableSlots,
                    debug = availableSlots.Count > 0 ? "Success" : "No time slots available after processing"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching booked time slots");
                return new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> OnGetGetDefaultDoctorAsync()
        {
            try
            {
                _logger.LogInformation("Getting default doctor...");

                // First, try to get doctor from DoctorAvailabilities table
                var doctorAvailability = await _context.DoctorAvailabilities
                    .Where(da => da.IsAvailable)
                    .FirstOrDefaultAsync();

                if (doctorAvailability != null)
                {
                    _logger.LogInformation($"Found doctor from availability: {doctorAvailability.DoctorId}");
                    return new JsonResult(new { doctorId = doctorAvailability.DoctorId });
                }

                // If no availability found, try to get any doctor from users table
                var doctorUsers = await _userManager.GetUsersInRoleAsync("Doctor");
                _logger.LogInformation($"Found {doctorUsers.Count} doctors in role");
                
                if (doctorUsers.Any())
                {
                    var firstDoctor = doctorUsers.First();
                    _logger.LogInformation($"Using first doctor from users: {firstDoctor.Id}");
                    
                    // Create availability record for this doctor
                    var newAvailability = new DoctorAvailability
                    {
                        DoctorId = firstDoctor.Id,
                        IsAvailable = true,
                        Monday = true, Tuesday = true, Wednesday = true, Thursday = true, Friday = true, Saturday = true, Sunday = true,
                        StartTime = new TimeSpan(8, 0, 0),
                        EndTime = new TimeSpan(17, 0, 0),
                        LastUpdated = DateTime.Now
                    };
                    
                    _context.DoctorAvailabilities.Add(newAvailability);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Created availability record for doctor {firstDoctor.Id}");
                    
                    return new JsonResult(new { doctorId = firstDoctor.Id });
                }

                // If still no doctors found, try to find any user with "doctor" in email
                var doctorByEmail = await _context.Users
                    .Where(u => u.Email.Contains("doctor") || u.UserName.Contains("doctor"))
                    .FirstOrDefaultAsync();

                if (doctorByEmail != null)
                {
                    _logger.LogInformation($"Found doctor by email: {doctorByEmail.Id}");
                    
                    // Assign Doctor role if not already assigned
                    if (!await _userManager.IsInRoleAsync(doctorByEmail, "Doctor"))
                    {
                        await _userManager.AddToRoleAsync(doctorByEmail, "Doctor");
                        _logger.LogInformation($"Assigned Doctor role to {doctorByEmail.Email}");
                    }
                    
                    // Create availability record
                    var newAvailability = new DoctorAvailability
                    {
                        DoctorId = doctorByEmail.Id,
                        IsAvailable = true,
                        Monday = true, Tuesday = true, Wednesday = true, Thursday = true, Friday = true, Saturday = true, Sunday = true,
                        StartTime = new TimeSpan(8, 0, 0),
                        EndTime = new TimeSpan(17, 0, 0),
                        LastUpdated = DateTime.Now
                    };
                    
                    _context.DoctorAvailabilities.Add(newAvailability);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Created availability record for doctor {doctorByEmail.Id}");
                    
                    return new JsonResult(new { doctorId = doctorByEmail.Id });
                }

                _logger.LogWarning("No doctors found in the system");
                return new JsonResult(new { doctorId = "", error = "No doctors available" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting default doctor");
                return new JsonResult(new { doctorId = "", error = ex.Message });
            }
        }

        private NCDRiskAssessment CreateNCDRiskAssessment(NCDRiskAssessmentViewModel model, int appointmentId)
        {
            return new NCDRiskAssessment
            {
                AppointmentId = appointmentId,
                UserId = model.UserId,
                HealthFacility = model.HealthFacility,
                FamilyNo = model.FamilyNo,
                Address = model.Address,
                Barangay = model.Barangay,
                Birthday = model.Birthday?.ToString("yyyy-MM-dd"),
                Telepono = model.Telepono,
                Edad = model.Edad,
                Kasarian = model.Kasarian,
                Relihiyon = model.Relihiyon,
                HasDiabetes = model.HasDiabetes,
                HasHypertension = model.HasHypertension,
                HasCancer = model.HasCancer,
                CancerType = model.CancerType,
                HasCOPD = model.HasCOPD,
                HasLungDisease = model.HasLungDisease,
                HasEyeDisease = model.HasEyeDisease,
                FamilyHasHypertension = model.FamilyHasHypertension,
                FamilyHasHeartDisease = model.FamilyHasHeartDisease,
                FamilyHasStroke = model.FamilyHasStroke,
                FamilyHasDiabetes = model.FamilyHasDiabetes,
                FamilyHasCancer = model.FamilyHasCancer,
                FamilyHasKidneyDisease = model.FamilyHasKidneyDisease,
                FamilyHasOtherDisease = model.FamilyHasOtherDisease,
                FamilyOtherDiseaseDetails = model.FamilyOtherDiseaseDetails,
                HighSaltIntake = model.HighSaltIntake,
                AlcoholFrequency = model.AlcoholFrequency,
                AlcoholConsumption = model.AlcoholConsumption,
                ExerciseDuration = model.ExerciseDuration,
                SmokingStatus = model.SmokingStatus,
                AppointmentType = model.AppointmentType
            };
        }

        private HEEADSSSAssessment CreateHEEADSSSAssessment(HEEADSSSAssessmentViewModel model, int appointmentId)
        {
            return new HEEADSSSAssessment
            {
                AppointmentId = appointmentId.ToString(),
                UserId = model.UserId,
                HealthFacility = model.HealthFacility,
                FamilyNo = model.FamilyNo,
                FullName = model.FullName,
                Age = model.Age.ToString(),
                Birthday = model.Birthday,
                Gender = model.Gender,
                Address = model.Address,
                ContactNumber = model.ContactNumber,
                HomeFamilyProblems = model.HomeFamilyProblems,
                HomeParentalListening = model.HomeParentalListening,
                HomeParentalBlame = model.HomeParentalBlame,
                HomeFamilyChanges = model.HomeFamilyChanges,
                EducationCurrentlyStudying = model.EducationCurrentlyStudying,
                EducationWorking = model.EducationWorking,
                EducationSchoolWorkProblems = model.EducationSchoolWorkProblems,
                EducationBullying = model.EducationBullying,
                EatingBodyImageSatisfaction = model.EatingBodyImageSatisfaction,
                EatingDisorderedEatingBehaviors = model.EatingDisorderedEatingBehaviors,
                EatingWeightComments = model.EatingWeightComments,
                ActivitiesParticipation = model.ActivitiesParticipation,
                ActivitiesRegularExercise = model.ActivitiesRegularExercise,
                ActivitiesScreenTime = model.ActivitiesScreenTime,
                DrugsTobaccoUse = model.DrugsTobaccoUse,
                DrugsAlcoholUse = model.DrugsAlcoholUse,
                DrugsIllicitDrugUse = model.DrugsIllicitDrugUse,
                SexualityBodyConcerns = model.SexualityBodyConcerns,
                SexualityIntimateRelationships = model.SexualityIntimateRelationships,
                SexualityPartners = model.SexualityPartners,
                SexualitySexualOrientation = model.SexualitySexualOrientation,
                SexualityPregnancy = model.SexualityPregnancy,
                SexualitySTI = model.SexualitySTI,
                SexualityProtection = model.SexualityProtection,
                SuicideDepressionFeelings = model.SuicideDepressionFeelings,
                SuicideSelfHarmThoughts = model.SuicideSelfHarmThoughts,
                SuicideFamilyHistory = model.SuicideFamilyHistory,
                SafetyPhysicalAbuse = model.SafetyPhysicalAbuse,
                SafetyRelationshipViolence = model.SafetyRelationshipViolence,
                SafetyProtectiveGear = model.SafetyProtectiveGear,
                SafetyGunsAtHome = model.SafetyGunsAtHome,
                Notes = model.Notes,
                RecommendedActions = model.RecommendedActions,
                AssessedBy = model.AssessedBy
            };
        }

        private async Task<JsonResult?> ValidateTimeSlotAsync(AppointmentBookingViewModel bookingModel)
        {
            if (!string.IsNullOrEmpty(bookingModel.AppointmentDate) && !string.IsNullOrEmpty(bookingModel.TimeSlot))
            {
                try
                {
                    DateTime selectedApptDate = DateTime.Parse(bookingModel.AppointmentDate);
                    
                    // Convert from 12-hour format to TimeSpan
                    // Handle both single time ("8:00 AM") and time range ("8:00 AM - 8:06 AM")
                    TimeSpan selectedApptTime;
                    string timeSlotToParse = bookingModel.TimeSlot;
                    
                    // If it's a time range (contains " - "), extract the start time
                    if (timeSlotToParse.Contains(" - "))
                    {
                        timeSlotToParse = timeSlotToParse.Split(new[] { " - " }, StringSplitOptions.None)[0].Trim();
                        _logger.LogInformation($"[ValidateTimeSlot] Extracted start time from range: {bookingModel.TimeSlot} -> {timeSlotToParse}");
                    }
                    
                    if (DateTime.TryParse(timeSlotToParse, out DateTime parsedTime))
                    {
                        selectedApptTime = parsedTime.TimeOfDay;
                        _logger.LogInformation($"[ValidateTimeSlot] Successfully parsed time: {timeSlotToParse} to {selectedApptTime}");
                    }
                    else
                    {
                        _logger.LogError($"[ValidateTimeSlot] Failed to parse time string: {timeSlotToParse} (original: {bookingModel.TimeSlot})");
                        return new JsonResult(new 
                        { 
                            success = false, 
                            error = "Invalid time format. Please select a valid time.", 
                            errorType = "ValidationError" 
                        });
                    }

                    // Get doctor ID (use default doctor if not specified)
                    var doctorId = bookingModel.DoctorId;
                    if (string.IsNullOrEmpty(doctorId))
                    {
                        // Get default doctor
                        var defaultDoctor = await _userManager.GetUsersInRoleAsync("Doctor");
                        if (defaultDoctor.Any())
                        {
                            doctorId = defaultDoctor.First().Id;
                        }
                    }

                    if (string.IsNullOrEmpty(doctorId))
                    {
                        return new JsonResult(new
                        {
                            success = false,
                            error = "No doctor available for this appointment.",
                            errorType = "ValidationError"
                        });
                    }

                    // Use transaction to prevent race conditions
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // Check if date is weekend and if doctor works on weekends
                        var availability = await _context.DoctorAvailabilities
                            .FirstOrDefaultAsync(da => da.DoctorId == doctorId);

                        if (availability == null)
                        {
                            await transaction.RollbackAsync();
                            return new JsonResult(new
                            {
                                success = false,
                                error = "Doctor availability not configured. Please contact support.",
                                errorType = "ValidationError"
                            });
                        }

                        // Check if doctor is available on the selected date
                        if (!availability.IsAvailableOnDate(selectedApptDate))
                        {
                            await transaction.RollbackAsync();
                            var dayName = selectedApptDate.DayOfWeek.ToString();
                            return new JsonResult(new
                            {
                                success = false,
                                error = $"Doctor is not available on {dayName}s. Please select a different date.",
                                errorType = "DayNotAvailable"
                            });
                        }

                        // Check daily appointment slot cap WITH ROW LOCKING
                        var bookedCount = await _context.Appointments
                            .Where(a => a.DoctorId == doctorId &&
                                       a.AppointmentDate.Date == selectedApptDate.Date &&
                                       a.Status != AppointmentStatus.Cancelled)
                            .CountAsync();

                        _logger.LogInformation($"Checking slot availability: {bookedCount}/{availability.MaxAppointmentsPerDay} slots used");

                        if (bookedCount >= availability.MaxAppointmentsPerDay)
                        {
                            await transaction.RollbackAsync();
                            return new JsonResult(new
                            {
                                success = false,
                                error = $"All appointment slots ({availability.MaxAppointmentsPerDay}) are fully booked for {selectedApptDate:MMM dd, yyyy}. Please select a different date.",
                                errorType = "FullyBooked"
                            });
                        }

                        // Check for exact time slot conflict
                        var existingAtSameTime = await _context.Appointments
                            .AnyAsync(a => a.DoctorId == doctorId &&
                                          a.AppointmentDate.Date == selectedApptDate.Date &&
                                          a.AppointmentTime == selectedApptTime &&
                                          a.Status != AppointmentStatus.Cancelled);

                        if (existingAtSameTime)
                        {
                            await transaction.RollbackAsync();
                            _logger.LogWarning($"Time slot conflict detected: {selectedApptTime} already booked");
                            return new JsonResult(new
                            {
                                success = false,
                                error = "This time slot has already been booked. Please select a different time.",
                                errorType = "TimeSlotConflict"
                            });
                        }

                        // Commit transaction - slot is valid
                        await transaction.CommitAsync();
                        _logger.LogInformation($"Slot validation passed: {selectedApptDate:MMM dd, yyyy} at {selectedApptTime}");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error during transaction-safe slot validation");
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking time slot availability");
                    return new JsonResult(new
                    {
                        success = false,
                        error = "Error checking time slot availability. Please try again.",
                        errorType = "ValidationError"
                    });
                }
            }
            return null;
        }
        
        private async Task LoadAvailableDynamicFormsAsync()
        {
            try
            {
                // Get all active dynamic forms that should appear in appointment workflow
                var dynamicForms = await _context.FormTemplates
                    .Where(f => f.IsActive && f.ShowInAppointmentFlow)
                    .OrderBy(f => f.DisplayOrder)
                    .Select(f => new DynamicFormInfo
                    {
                        FormName = f.FormName,
                        IconClass = f.IconClass ?? "fa-solid fa-file-medical",
                        MinAge = f.MinAge,
                        MaxAge = f.MaxAge
                    })
                    .ToListAsync();

                AvailableDynamicForms = dynamicForms;
                _logger.LogInformation($"Loaded {AvailableDynamicForms.Count} dynamic forms for booking page");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dynamic forms for booking page");
                AvailableDynamicForms = new List<DynamicFormInfo>();
            }
        }
        
        // API endpoint to get available forms for a specific age and consultation type
        public async Task<JsonResult> OnGetGetAvailableFormsForAgeAsync(int age, string? consultationType = null)
        {
            try
            {
                _logger.LogInformation($"Getting available forms for age: {age}, consultation type: {consultationType}");
                
                // Get service ID from consultation type
                int? serviceId = null;
                if (!string.IsNullOrEmpty(consultationType))
                {
                    var service = await _context.ConsultationServices
                        .FirstOrDefaultAsync(s => s.ServiceName.ToLower() == consultationType.ToLower());
                    serviceId = service?.ServiceId;
                }
                
                // Get all active dynamic forms that should appear in appointment workflow
                var formsQuery = _context.FormTemplates
                    .Where(f => f.IsActive && f.ShowInAppointmentFlow);
                
                // Filter by service:
                // - Show forms linked to the selected service (ServiceId == serviceId)
                // - ALSO show general forms (ServiceId == null) for all services
                if (serviceId.HasValue)
                {
                    formsQuery = formsQuery.Where(f => f.ServiceId == serviceId.Value || f.ServiceId == null);
                }
                
                var forms = await formsQuery
                    .OrderBy(f => f.DisplayOrder)
                    .ToListAsync();
                
                // Filter by age restrictions
                var availableForms = forms.Where(f => 
                {
                    bool isAgeAppropriate = true;
                    
                    if (f.MinAge.HasValue && age < f.MinAge.Value)
                        isAgeAppropriate = false;
                    
                    if (f.MaxAge.HasValue && age > f.MaxAge.Value)
                        isAgeAppropriate = false;
                    
                    return isAgeAppropriate;
                })
                .Select(f => new 
                {
                    formKey = f.FormKey,
                    formName = f.FormName,
                    iconClass = f.IconClass ?? "fa-solid fa-file-medical",
                    minAge = f.MinAge,
                    maxAge = f.MaxAge
                })
                .ToList();
                
                _logger.LogInformation($"Found {availableForms.Count} available forms for age {age}");
                
                return new JsonResult(new 
                { 
                    success = true, 
                    forms = availableForms,
                    message = availableForms.Any() 
                        ? $"Found {availableForms.Count} form(s)"
                        : "No forms available for this age"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting available forms for age {age}");
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        // NEW: Search for family members by name
        public async Task<JsonResult> OnGetSearchFamilyMembersAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
                {
                    return new JsonResult(new { success = true, familyGroups = new List<object>() });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return new JsonResult(new { success = false, error = "User not found" });
                }

                // Search in ApplicationUsers (AspNetUsers table)
                var searchTermLower = searchTerm.ToLower();
                var matchingUsers = await _context.Users
                    .Where(u => !string.IsNullOrEmpty(u.FamilyNumber) &&
                               (u.FirstName.ToLower().Contains(searchTermLower) ||
                                u.LastName.ToLower().Contains(searchTermLower) ||
                                (u.FirstName + " " + u.LastName).ToLower().Contains(searchTermLower)))
                    .Select(u => new
                    {
                        userId = u.Id,
                        fullName = u.FirstName + " " + u.LastName,
                        lastName = u.LastName,
                        familyNumber = u.FamilyNumber,
                        relationship = u.Id == user.Id ? "You" : "Family Member"
                    })
                    .Take(20)
                    .ToListAsync();

                // Get UserIds from matching users to exclude them from Patients search
                var userIdsFromUsers = matchingUsers.Select(u => u.userId).ToList();

                // Also search in Patients table (for dependents/family members)
                // Exclude patients who are also in the Users table to avoid duplicates
                var matchingPatients = await _context.Patients
                    .Where(p => !string.IsNullOrEmpty(p.FamilyNumber) &&
                                !userIdsFromUsers.Contains(p.UserId) &&
                                p.FullName.ToLower().Contains(searchTermLower))
                    .Select(p => new
                    {
                        userId = p.UserId ?? "",
                        fullName = p.FullName,
                        lastName = "", // Will extract from FullName
                        familyNumber = p.FamilyNumber,
                        relationship = "Family Member"
                    })
                    .ToListAsync();

                // Extract last names from matching results
                var matchingLastNames = matchingUsers
                    .Select(u => u.lastName.ToLower().Trim())
                    .Where(ln => !string.IsNullOrEmpty(ln))
                    .Distinct()
                    .ToList();

                // Extract last names from Patients (last word in FullName)
                foreach (var patient in matchingPatients)
                {
                    if (!string.IsNullOrEmpty(patient.fullName))
                    {
                        var nameParts = patient.fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (nameParts.Length > 0)
                        {
                            var lastName = nameParts[nameParts.Length - 1].ToLower().Trim();
                            if (!string.IsNullOrEmpty(lastName) && !matchingLastNames.Contains(lastName))
                            {
                                matchingLastNames.Add(lastName);
                            }
                        }
                    }
                }

                if (!matchingLastNames.Any())
                {
                    return new JsonResult(new { success = true, familyGroups = new List<object>() });
                }

                // Get ALL users with matching last names (not just the matching ones)
                var allFamilyMembersFromUsers = await _context.Users
                    .Where(u => !string.IsNullOrEmpty(u.FamilyNumber) && 
                               !string.IsNullOrEmpty(u.LastName) &&
                               matchingLastNames.Contains(u.LastName.ToLower().Trim()))
                    .Select(u => new
                    {
                        userId = u.Id,
                        fullName = u.FirstName + " " + u.LastName,
                        lastName = u.LastName,
                        familyNumber = u.FamilyNumber,
                        relationship = u.Id == user.Id ? "You" : "Family Member"
                    })
                    .ToListAsync();

                var allUserIds = allFamilyMembersFromUsers.Select(u => u.userId).ToList();

                // Get ALL patients with matching last names
                var allFamilyMembersFromPatients = await _context.Patients
                    .Where(p => !string.IsNullOrEmpty(p.FamilyNumber) && 
                               !string.IsNullOrEmpty(p.FullName) &&
                               !allUserIds.Contains(p.UserId))
                    .ToListAsync();

                // Filter patients by last name and project
                var filteredPatients = allFamilyMembersFromPatients
                    .Where(p =>
                    {
                        if (string.IsNullOrEmpty(p.FullName)) return false;
                        var nameParts = p.FullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (nameParts.Length == 0) return false;
                        var lastName = nameParts[nameParts.Length - 1].ToLower().Trim();
                        return matchingLastNames.Contains(lastName);
                    })
                    .Select(p => new
                    {
                        userId = p.UserId ?? "",
                        fullName = p.FullName,
                        lastName = p.FullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "",
                        familyNumber = p.FamilyNumber,
                        relationship = "Family Member"
                    })
                    .ToList();

                // Combine all family members
                var allFamilyMembers = allFamilyMembersFromUsers
                    .Concat(filteredPatients)
                    .Select(m => new 
                    { 
                        fullName = m.fullName,
                        lastName = m.lastName.ToLower().Trim(),
                        familyNumber = m.familyNumber, 
                        relationship = m.relationship 
                    })
                    .OrderBy(m => m.fullName)
                    .Take(20)
                    .ToList();

                _logger.LogInformation("Found {Count} family members matching '{SearchTerm}'", allFamilyMembers.Count, searchTerm);

                // Return flat list of members for the frontend
                return new JsonResult(new { success = true, members = allFamilyMembers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching family members for term: {SearchTerm}", searchTerm);
                return new JsonResult(new { success = false, error = "An error occurred while searching" });
            }
        }

        // NEW: Generate a brand new family number
        public async Task<JsonResult> OnPostGenerateNewFamilyNumberAsync([FromBody] GenerateNewFamilyNumberRequest request)
        {
            try
            {
                _logger.LogInformation("=== GENERATE NEW FAMILY NUMBER REQUEST ===");
                _logger.LogInformation("LastName: {LastName}, FirstName: {FirstName}", request.LastName, request.FirstName);

                if (string.IsNullOrWhiteSpace(request.LastName))
                {
                    return new JsonResult(new { success = false, error = "Last name is required" });
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return new JsonResult(new { success = false, error = "User not found" });
                }

                // Generate a completely new family number (never reuse)
                var newFamilyNumber = await _familyNumberService.GenerateBrandNewFamilyNumberAsync(request.LastName);

                if (string.IsNullOrWhiteSpace(newFamilyNumber))
                {
                    return new JsonResult(new { success = false, error = "Failed to generate family number" });
                }

                // Save to user profile
                user.FamilyNumber = newFamilyNumber;
                await _userManager.UpdateAsync(user);
                _logger.LogInformation("Assigned new family number {FamilyNumber} to user {UserId}", newFamilyNumber, user.Id);

                // Also update Patient record if exists
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (patient != null)
                {
                    patient.FamilyNumber = newFamilyNumber;
                    patient.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Updated Patient record with family number {FamilyNumber}", newFamilyNumber);
                }

                _logger.LogInformation("Successfully generated and assigned family number: {FamilyNumber}", newFamilyNumber);

                return new JsonResult(new
                {
                    success = true,
                    familyNumber = newFamilyNumber,
                    message = $"New family number {newFamilyNumber} has been generated."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating new family number");
                return new JsonResult(new { success = false, error = "An error occurred while generating family number" });
            }
        }
    }

    public class UserDetailsViewModel
    {
        public string FullName { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
    }

    public class AppointmentBookingViewModel
    {
        public int CurrentStep { get; set; } = 1;
        public int? AppointmentId { get; set; }
        public string DoctorId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public int Age { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime? Birthday { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public string? AppointmentDate { get; set; }
        public string? TimeSlot { get; set; }
        public string? ConsultationType { get; set; }
        [StringLength(300, ErrorMessage = "Reason for Visit must be 300 characters or less.")]
        [RegularExpression(@"^[A-Za-z0-9\s]*$", ErrorMessage = "Reason for Visit can only contain letters, numbers, and spaces.")]
        public string? ReasonForVisit { get; set; }
        public string? Symptoms { get; set; }
        public string? HealthFacilityId { get; set; }
        public bool BookingForOther { get; set; }
        public string? Relationship { get; set; }
        public bool HasFamilyNumber { get; set; }
        public string? FamilyNumber { get; set; }
        public decimal? Temperature { get; set; }
        public string? BloodPressure { get; set; }
        public int? PulseRate { get; set; }
        public int? SelectedTimeSlotId { get; set; }
        public List<ConsultationTimeSlot> AvailableTimeSlots { get; set; } = new List<ConsultationTimeSlot>();
    }

    public class GenerateNewFamilyNumberRequest
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
    }
}