using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Barangay.Data;
using Barangay.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Barangay.Pages.User
{
    public class AppointmentModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AppointmentModel> _logger;

        public AppointmentModel(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ILogger<AppointmentModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            Input = new AppointmentInputModel();
        }

        public List<Barangay.Models.Appointment>? Appointments { get; set; }
        public List<ApplicationUser>? Doctors { get; set; }
        public List<ApplicationUser>? AvailableDoctors { get; set; }
        
        [BindProperty]
        public Barangay.Models.Appointment Appointment { get; set; } = new Barangay.Models.Appointment();
        
        public Dictionary<string, int> DoctorAvailableSlots { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, string> DoctorSpecializations { get; set; } = new();
        public Dictionary<string, string> DoctorWorkingHours { get; set; } = new();
        
        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        // Add missing properties that are referenced in the Razor view
        [BindProperty]
        public bool? IsBookingForDependent { get; set; } = false;

        [BindProperty]
        public string? DependentFullName { get; set; }

        [BindProperty]
        public int? DependentAge { get; set; }

        [BindProperty]
        public string? RelationshipToDependent { get; set; }

        [BindProperty]
        public string? ConsultationType { get; set; }

        [BindProperty]
        public ApplicationUser CurrentUser { get; set; } = new ApplicationUser();

        [BindProperty]
        public AppointmentInputModel Input { get; set; }

        public string HealthFacility { get; set; }
        public string FamilyNo { get; set; }
        public bool FamilyNoPreexisting { get; set; }

        // Reference date for age calculation: May 29, 2025, 1:36 PM PST (assuming PST is UTC-8)
        // 13:36 PST = 21:36 UTC. For simplicity using local time on server, ensure server is configured or use DateTimeOffset.
        // The prompt states 1:36 PM PST. Let's represent this as a specific point in time.
        // For consistency with previous age calculation, using DateTime.
        private static readonly DateTime AgeReferenceDate = new DateTime(2025, 5, 29, 13, 36, 0);

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // Get current user and set to CurrentUser property for the view to access
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                CurrentUser = user;
                
                // Get patient data from Patients table if it exists
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);
                // Check if patient record exists first
                if (patient != null)
                {
                    // If we have patient data, use its BirthDate directly
                    DateTime patientBirthDate = patient.BirthDate;
                    var age = CalculateAge(patientBirthDate);
                    ViewData["PatientAge"] = age;
                }
                else
                {
                    // If no patient record, use user's birthdate
                    var userBirthDateForAge = user.BirthDate ?? DateTime.MinValue;
                    ViewData["PatientAge"] = CalculateAge(userBirthDateForAge);
                }
            }

            // Get user's appointments
            try 
            {
                // Use raw SQL to safely retrieve appointments and avoid type conversion errors
                var sql = @"SELECT a.*, u.FullName AS DoctorName, u.Specialization 
                          FROM Appointments a 
                          LEFT JOIN AspNetUsers u ON a.DoctorId = u.Id 
                          WHERE a.PatientId = @userId 
                          ORDER BY a.CreatedAt DESC";
                
                var appointmentsList = new List<Barangay.Models.Appointment>();
                
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@userId";
                    parameter.Value = userId;
                    command.Parameters.Add(parameter);
                    
                    _context.Database.OpenConnection();
                    
                    using (var result = command.ExecuteReader())
                    {
                        while (result.Read())
                        {
                            var appointment = new Barangay.Models.Appointment
                            {
                                Id = result["Id"] is DBNull ? 0 : Convert.ToInt32(result["Id"]),
                                PatientId = result["PatientId"]?.ToString(),
                                DoctorId = result["DoctorId"]?.ToString(),
                                PatientName = result["PatientName"]?.ToString(),
                                AppointmentDate = result["AppointmentDate"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(result["AppointmentDate"]),
                                AppointmentTime = result["AppointmentTime"] is DBNull ? TimeSpan.Zero : TimeSpan.Parse(result["AppointmentTime"].ToString()),
                                Status = result["Status"] is DBNull ? AppointmentStatus.Pending : (AppointmentStatus)Convert.ToInt32(result["Status"]),
                                Description = result["Description"]?.ToString(),
                                CreatedAt = result["CreatedAt"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(result["CreatedAt"]),
                                Type = result["Type"]?.ToString()
                            };
                            
                            // Set Doctor property with minimal data to avoid navigation property issues
                            appointment.Doctor = new ApplicationUser
                            {
                                Id = result["DoctorId"]?.ToString(),
                                FullName = result["DoctorName"]?.ToString(),
                                Specialization = result["Specialization"]?.ToString()
                            };
                            
                            appointmentsList.Add(appointment);
                        }
                    }
                }
                
                Appointments = appointmentsList;
                _logger.LogInformation($"Found {Appointments.Count} appointments for user {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving appointments: {ex.Message}\n{ex.StackTrace}");
                Appointments = new List<Barangay.Models.Appointment>();
            }

            // Get doctors from UserManager instead of StaffMembers
            var doctorUsers = await _userManager.GetUsersInRoleAsync("Doctor");
            Doctors = doctorUsers.ToList();
            AvailableDoctors = Doctors;

            foreach (var doctor in Doctors)
            {
                DoctorSpecializations[doctor.Id] = doctor.Specialization ?? "General Medicine";
                DoctorWorkingHours[doctor.Id] = doctor.WorkingHours ?? "9:00 AM - 5:00 PM";
                
                // Calculate available slots for each doctor
                DoctorAvailableSlots[doctor.Id] = CalculateAvailableSlots(doctor.Id);
            }

            // 1. Health Facility
            if (user.Address != null && user.Address.Contains("Barangay"))
            {
                // Attempt to parse a number from the address as Barangay identifier
                var addressParts = user.Address.Split(' ');
                var barangayNumberPart = addressParts.LastOrDefault(part => int.TryParse(part, out _));
                if (barangayNumberPart != null)
                {
                    HealthFacility = $"Barangay Health Center {barangayNumberPart}";
                }
                else
                {
                    HealthFacility = "Barangay Health Center (Default)"; // Fallback if parsing fails
                }
            }
            else
            {
                HealthFacility = "Barangay Health Center 161"; // Default
            }
            _logger.LogInformation($"Health Facility determined: {HealthFacility}");

            // 2. Family No
            // Assuming IntegratedAssessments is a DbSet in ApplicationDbContext
            // And IntegratedAssessment entity has string UserId and string FamilyNo
            try
            {
                IntegratedAssessment? existingAssessment = null;
                try
                {
                    existingAssessment = await _context.IntegratedAssessments
                        .FirstOrDefaultAsync(a => a.UserId == user.Id && !string.IsNullOrEmpty(a.FamilyNo));
                }
                catch (InvalidOperationException ex)
                {
                    // This could happen if the table doesn't exist
                    _logger.LogError(ex, "Error querying IntegratedAssessments. Table might not exist.");
                }

                if (existingAssessment != null && !string.IsNullOrEmpty(existingAssessment.FamilyNo))
                {
                    FamilyNo = existingAssessment.FamilyNo;
                    FamilyNoPreexisting = true;
                    _logger.LogInformation($"Existing FamilyNo found: {FamilyNo}");
                }
                else
                {
                    var firstLetter = "X"; // Default if no last name
                    if (!string.IsNullOrEmpty(user.LastName))
                    {
                        firstLetter = user.LastName.Substring(0, 1).ToUpper();
                    }
                    else if (!string.IsNullOrEmpty(user.UserName))
                    {
                         // Attempt to get first letter from UserName if LastName is unavailable
                        firstLetter = user.UserName.Substring(0,1).ToUpper();
                    }
                    
                    // Try to get the count, but if it fails, just set a default count
                    int count = 1;
                    try
                    {
                        count = await _context.IntegratedAssessments
                            .CountAsync(a => a.FamilyNo != null && a.FamilyNo.StartsWith(firstLetter + "-"));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error counting IntegratedAssessments, using default count.");
                    }
                    
                    FamilyNo = $"{firstLetter}-{(count + 1):D3}";
                    FamilyNoPreexisting = false;
                    _logger.LogInformation($"New FamilyNo generated: {FamilyNo}");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error accessing IntegratedAssessments for FamilyNo. Using default.");
                // Ensure we set a default value if the database lookup fails
                var firstLetter = !string.IsNullOrEmpty(user.LastName) ? 
                    user.LastName.Substring(0, 1).ToUpper() : 
                    (!string.IsNullOrEmpty(user.UserName) ? user.UserName.Substring(0, 1).ToUpper() : "X");
                    
                FamilyNo = $"{firstLetter}-001"; // Default family number
                FamilyNoPreexisting = false;
                _logger.LogWarning($"Using default FamilyNo: {FamilyNo}");
            }

            // Pre-fill parts of the Input model if necessary (e.g., from user profile)
            Input.Address = user.Address;
            Input.Telepono = user.PhoneNumber;
            if (user.BirthDate.HasValue)
            {
                Input.Birthday = user.BirthDate.Value;
                Input.Edad = CalculateAge(user.BirthDate.Value);
            }

            return Page();
        }

        private int CalculateAvailableSlots(string doctorId)
        {
            var today = DateTime.Today;
            
            // Get doctor from ApplicationUser
            var doctor = _userManager.Users
                .FirstOrDefault(d => d.Id == doctorId);
            
            if (doctor == null) return 0;

            // Parse working hours with default values
            var defaultWorkingHours = "09:00-17:00";
            var workingHours = doctor.WorkingHours ?? defaultWorkingHours;
            var hours = workingHours.Split('-');
            
            if (hours.Length != 2) return 0;

            var startHour = hours[0].Trim();
            var endHour = hours[1].Trim();

            if (!DateTime.TryParseExact(startHour, new[] { "HH:mm", "H:mm" }, 
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.None, out DateTime startTime) ||
                !DateTime.TryParseExact(endHour, new[] { "HH:mm", "H:mm" }, 
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime endTime))
            {
                return 0;
            }

            var totalMinutes = (endTime - startTime).TotalMinutes;
            var interval = 30; // 30-minute slots
            var totalSlots = (int)(totalMinutes / interval);

            // Count booked appointments
            var bookedSlots = _context.Appointments
                .Count(a => a.DoctorId == doctorId && 
                       a.AppointmentDate.Date == today && 
                       a.Status != AppointmentStatus.Cancelled);

            return Math.Max(0, totalSlots - bookedSlots);
        }

        public JsonResult OnGetAvailableTimeSlotsAsync(DateTime date)
        {
            try
            {
                // Define default working hours for the clinic
                var defaultWorkingHours = "09:00-17:00";
                
                // Parse working hours
                var hours = defaultWorkingHours.Split('-');
                if (hours.Length != 2) return new JsonResult(new { error = "Invalid working hours format" });

                var startHour = hours[0].Trim();
                var endHour = hours[1].Trim();

                if (!DateTime.TryParseExact(startHour, new[] { "HH:mm", "H:mm" }, 
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime startTime) ||
                    !DateTime.TryParseExact(endHour, new[] { "HH:mm", "H:mm" },
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime endTime))
                {
                    return new JsonResult(new { error = "Invalid time format" });
                }

                var availableSlots = new List<string>();
                var currentTime = startTime;
                while (currentTime < endTime)
                {
                    availableSlots.Add(currentTime.ToString("HH:mm"));
                    currentTime = currentTime.AddMinutes(30);
                }

                // Get booked appointments for any doctor on this date
                var bookedTimes = _context.Appointments
                    .Where(a => a.AppointmentDate.Date == date.Date && 
                           a.Status != AppointmentStatus.Cancelled)
                    .Select(a => a.AppointmentTime)
                    .ToList();

                // Remove booked slots
                foreach (var time in bookedTimes)
                {
                    availableSlots.Remove(time.ToString("HH:mm"));
                }

                return new JsonResult(availableSlots);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Error loading time slots: {ex.Message}" });
            }
        }

        public JsonResult OnGetAvailableDoctorsAsync(DateTime date)
        {
            try
            {
                // Get all doctors
                var doctorUsers = _userManager.GetUsersInRoleAsync("Doctor").Result;
                var doctors = doctorUsers.ToList();
                
                // Format doctor data for the frontend
                var doctorData = doctors.Select(d => new {
                    id = d.Id,
                    name = d.FullName ?? d.UserName ?? "Unknown",
                    specialization = d.Specialization ?? "General Medicine",
                    workingHours = d.WorkingHours ?? "9:00 AM - 5:00 PM",
                    availableSlots = CalculateAvailableSlotsForDate(d.Id, date)
                }).ToList();
                
                return new JsonResult(doctorData);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Error loading doctors: {ex.Message}" });
            }
        }
        
        private int CalculateAvailableSlotsForDate(string doctorId, DateTime date)
        {
            // Get doctor from ApplicationUser
            var doctor = _userManager.Users
                .FirstOrDefault(d => d.Id == doctorId);
            
            if (doctor == null) return 0;

            // Parse working hours with default values
            var defaultWorkingHours = "09:00-17:00";
            var workingHours = doctor.WorkingHours ?? defaultWorkingHours;
            var hours = workingHours.Split('-');
            
            if (hours.Length != 2) return 0;

            var startHour = hours[0].Trim();
            var endHour = hours[1].Trim();

            if (!DateTime.TryParseExact(startHour, new[] { "HH:mm", "H:mm" }, 
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.None, out DateTime startTime) ||
                !DateTime.TryParseExact(endHour, new[] { "HH:mm", "H:mm" }, 
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime endTime))
            {
                return 0;
            }

            var totalMinutes = (endTime - startTime).TotalMinutes;
            var interval = 30; // 30-minute slots
            var totalSlots = (int)(totalMinutes / interval);

            // Count booked appointments for the specific date
            var bookedSlots = _context.Appointments
                .Count(a => a.DoctorId == doctorId && 
                       a.AppointmentDate.Date == date.Date && 
                       a.Status != AppointmentStatus.Cancelled);

            return Math.Max(0, totalSlots - bookedSlots);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Remove ModelState validation check
            // We still repopulate HealthFacility and FamilyNo for display consistency
            var userForRepost = await _userManager.GetUserAsync(User);
            if (userForRepost != null) {
                // Simplified re-population logic, consider abstracting if complex
                if (userForRepost.Address != null && userForRepost.Address.Contains("Barangay")) { /*...*/ HealthFacility = "Barangay Health Center (Default)"; } else { HealthFacility = "Barangay Health Center 161"; }
                if (string.IsNullOrEmpty(FamilyNo)) { FamilyNo = "TEMP-000"; } // Avoid losing this on postback
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            _logger.LogInformation($"Form submitted by User ID: {user.Id}");
            _logger.LogInformation($"Input Address: {Input.Address}");
            _logger.LogInformation($"Input Barangay: {Input.Barangay}");
            // ... log other fields

            // TODO: Implement saving logic to IntegratedAssessments or NCDRiskAssessments
            // TODO: Handle AppointmentId (create new appointment if needed)
            // Example:
            // var newAssessment = new IntegratedAssessment // or NCDRiskAssessment
            // {
            //     UserId = user.Id,
            //     HealthFacility = this.HealthFacility, // Or from Input if it becomes part of form
            //     FamilyNo = this.FamilyNo, // Or from Input
            //     Address = Input.Address,
            //     Barangay = Input.Barangay,
            //     Birthday = Input.Birthday,
            //     Telepono = Input.Telepono,
            //     Edad = CalculateAge(Input.Birthday), // Recalculate or use Input.Edad
            //     Kasarian = Input.Kasarian,
            //     Relihiyon = Input.Relihiyon,
            //     // ... map other properties from Input model and Step 2, 3 etc.
            //     CreatedAt = DateTime.UtcNow,
            // };
            // _context.IntegratedAssessments.Add(newAssessment);
            // await _context.SaveChangesAsync();
                
            // TODO: Guardian consent logic if age < 18

            TempData["StatusMessage"] = "Appointment data (partially) processed.";
            return RedirectToPage("./UserDashboard"); // Or a confirmation page
        }

        public async Task<IActionResult> OnPostCancelAppointmentAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.PatientId == userId);
                
            if (appointment == null) return NotFound();
            
            // Update appointment status
            appointment.Status = AppointmentStatus.Cancelled;
            appointment.UpdatedAt = DateTime.Now;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Appointment {id} cancelled by user {userId}");
            StatusMessage = "Appointment cancelled successfully";
            
            return RedirectToPage();
        }
        
        
        private int CalculateAge(DateTime birthDate)
        {
            var today = AgeReferenceDate; // Using the specified reference date
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age))
            {
                age--;
            }
            return age < 0 ? 0 : age; // Ensure age is not negative
        }
    }

    // Define the InputModel for the form fields
    public class AppointmentInputModel
    {
        [Display(Name = "Health Facility")]
        public string? HealthFacility { get; set; } // If it becomes an editable part of the form

        [Display(Name = "Family No.")]
        public string? FamilyNo { get; set; } // If it becomes an editable part of the form

        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "Barangay")]
        public string? Barangay { get; set; }
        
        [Display(Name = "Birthday")]
        [DataType(DataType.Date)]
        public DateTime? Birthday { get; set; } = DateTime.Now.AddYears(-15); // Default value

        [Display(Name = "Telepono (Phone Number)")]
        [Phone]
        public string? Telepono { get; set; }

        public int? Edad { get; set; } // Auto-calculated

        [Display(Name = "Kasarian (Gender)")]
        public string? Kasarian { get; set; } // e.g., "Male", "Female"

        public string? Relihiyon { get; set; }

        // Fields from Step 2: Past Medical History
        public bool HasDiabetes { get; set; }
        public bool HasHypertension { get; set; } // HPN
        public bool HasCancer { get; set; }
        public bool HasCOPD { get; set; }
        // These were in NCDRiskAssessmentViewModel, adding them here for completeness based on typical assessment forms
        public bool HasLungDisease { get; set; } 
        public bool HasEyeDisease { get; set; }

        // Additional fields from NCD context that might be part of this "Integrated" form
        public string? CancerType { get; set; } // If HasCancer is true
        public string? AppointmentType { get; set; } = "General Checkup"; // Default
        public string? FamilyOtherDiseaseDetails { get; set; }
    }

    // Placeholder for DbContext if not already defined or if IntegratedAssessments is new
    // public class IntegratedAssessment
    // {
    //     public int Id { get; set; }
    //     public string UserId { get; set; }
    //     public ApplicationUser User { get; set; }
    //     public string FamilyNo { get; set; }
    //     public string HealthFacility {get; set;}
    //     // Add other relevant assessment fields
    //     public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // }
    // In ApplicationDbContext.cs:
    // public DbSet<IntegratedAssessment> IntegratedAssessments { get; set; }
}
