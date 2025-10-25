using System;
using System.Threading.Tasks;
using System.Linq;
using Barangay.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Barangay.Data;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic; // Added for Dictionary
using System.Data; // Added for DBNull
using Barangay.Services;
using Barangay.Extensions; // Added for DecryptSensitiveData extension method
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;

namespace Barangay.Pages.User
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class NCDRiskAssessmentModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NCDRiskAssessmentModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDataEncryptionService _encryptionService;
        private readonly IFamilyNumberService _familyNumberService;
        private readonly IAuditTrailService _auditTrail;
        private readonly INotificationService _notificationService;
        private static readonly Random _random = new Random();

        private static readonly string[] _healthFacilities = new[]
        {
            "Barangay 158",
            "Barangay 159", 
            "Barangay 160",
            "Barangay 161"
        };

        public NCDRiskAssessmentModel(
            ApplicationDbContext context,
            ILogger<NCDRiskAssessmentModel> logger,
            UserManager<ApplicationUser> userManager,
            IDataEncryptionService encryptionService,
            IFamilyNumberService familyNumberService,
            IAuditTrailService auditTrail,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _familyNumberService = familyNumberService;
            _auditTrail = auditTrail;
            _notificationService = notificationService;
            Assessment = new NCDRiskAssessmentViewModel();
        }

        [BindProperty]
        public NCDRiskAssessmentViewModel Assessment { get; set; }

        public string HealthFacility { get; set; }
        public string FamilyNo { get; set; }
        public bool FamilyNoPreexisting { get; set; }
        public int? CalculatedAge { get; set; }

        [TempData]
        public string StatusMessage { get; set; }


        public async Task<IActionResult> OnGetAsync(string appointmentId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User not found");
                    return NotFound("User not found");
                }

                HealthFacility = GetHealthFacility(user);
                _logger.LogInformation("Health Facility set to: {HealthFacility}", HealthFacility);

                try
                {
                    (string familyNo, bool isPreexisting) = await GetOrGenerateFamilyNumberAsync(user);
                    FamilyNo = familyNo;
                    FamilyNoPreexisting = isPreexisting;
                    _logger.LogInformation("Family No set to: {FamilyNo} (Preexisting: {FamilyNoPreexisting})", FamilyNo, FamilyNoPreexisting);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting family number, using default");
                    string lastNameInitial = !string.IsNullOrEmpty(user.LastName) ? user.LastName.Substring(0, 1).ToUpper() : "X";
                    FamilyNo = $"{lastNameInitial}-001";
                    FamilyNoPreexisting = false;
                }

                int? appointmentIdInt = null;
                if (int.TryParse(appointmentId, out int parsedId))
                {
                    appointmentIdInt = parsedId;
                }

                // Initialize with user data first
                Assessment = new NCDRiskAssessmentViewModel
                {
                    AppointmentId = appointmentIdInt,
                    UserId = user.Id,
                    HealthFacility = HealthFacility,
                    FamilyNo = FamilyNo,
                    Address = user.Address ?? "",
                    Barangay = user.Barangay ?? "160", // Use user's barangay from signup
                    Birthday = user.BirthDate,
                    Telepono = user.PhoneNumber ?? "",
                    Kasarian = user.Gender == "Male" ? "Lalaki" : user.Gender == "Female" ? "Babae" : "",
                    FirstName = user.FirstName,
                    MiddleName = user.MiddleName,
                    LastName = user.LastName,
                    Occupation = user.Occupation,
                    CivilStatus = user.CivilStatus,
                    Relihiyon = user.Religion,
                    InterviewedBy = "", // Initialize empty for user input
                    Designation = "", // Initialize empty for user input
                    DoctorName = "" // Initialize empty for user input
                };

                // If appointment ID is provided, try to get appointment-specific data
                if (appointmentIdInt.HasValue)
                {
                    try
                    {
                        var appointment = await _context.Appointments.FindAsync(appointmentIdInt.Value);
                        if (appointment != null)
                        {
                            _logger.LogInformation("Found appointment {AppointmentId} for NCD assessment", appointmentIdInt.Value);
                            
                            // Use appointment patient information - prioritize dependent name if available
                            string correctPatientName = !string.IsNullOrEmpty(appointment.DependentFullName) 
                                ? appointment.DependentFullName 
                                : appointment.PatientName ?? user.FullName;
                            
                            // Parse the correct patient name into components
                            var nameParts = correctPatientName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            Assessment.FirstName = nameParts.Length > 0 ? nameParts[0] : user.FirstName;
                            Assessment.LastName = nameParts.Length > 1 ? nameParts[nameParts.Length - 1] : user.LastName;
                            Assessment.MiddleName = nameParts.Length > 2 ? string.Join(" ", nameParts.Skip(1).Take(nameParts.Length - 2)) : user.MiddleName;
                            
                            // Use appointment-specific data if available
                            if (!string.IsNullOrEmpty(appointment.ContactNumber))
                                Assessment.Telepono = appointment.ContactNumber;
                            
                            if (appointment.DateOfBirth.HasValue)
                            {
                                Assessment.Birthday = appointment.DateOfBirth.Value;
                                var age = CalculateAge(appointment.DateOfBirth.Value);
                                Assessment.Edad = age.ToString();
                                CalculatedAge = age;
                                _logger.LogInformation("Using appointment birthdate, calculated age: {Age}", CalculatedAge);
                            }
                            else if (appointment.AgeValue > 0)
                            {
                                Assessment.Edad = appointment.AgeValue.ToString();
                                CalculatedAge = appointment.AgeValue;
                                _logger.LogInformation("Using appointment age value: {Age}", CalculatedAge);
                            }
                            
                            _logger.LogInformation("Updated NCD assessment with appointment data: Name={CorrectPatientName}, Age={CalculatedAge}", correctPatientName, CalculatedAge);
                        }
                        else
                        {
                            _logger.LogWarning("Appointment {AppointmentId} not found for NCD assessment", appointmentIdInt.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading appointment data for NCD assessment {AppointmentId}", appointmentIdInt.Value);
                        // Continue with user data if appointment loading fails
                    }
                }

                // Set age from user data if not already set from appointment
                if (user.BirthDate.HasValue && !CalculatedAge.HasValue)
                {
                    var age = CalculateAge(user.BirthDate.Value);
                    Assessment.Edad = age.ToString();
                    CalculatedAge = age;
                    _logger.LogInformation("Using user birthdate, calculated age: {Age}", CalculatedAge);
                }
                else if (!CalculatedAge.HasValue)
                {
                    _logger.LogWarning("Birthday not available for age calculation.");
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NCDRiskAssessment GET");
                StatusMessage = "Error loading assessment page. Please try again.";
                return RedirectToPage("/Index");
            }
        }

        private string GetHealthFacility(ApplicationUser user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Address))
            {
                return _healthFacilities[0];
            }

            int hashCode = Math.Abs(user.Address.GetHashCode());
            int index = hashCode % _healthFacilities.Length;
            return _healthFacilities[index];
        }

        private async Task<(string familyNo, bool isPreexisting)> GetOrGenerateFamilyNumberAsync(ApplicationUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            // Check if user already has a family number - only query essential fields to avoid column errors
            var existingAssessment = await _context.NCDRiskAssessments
                .Where(a => a.UserId == user.Id && !string.IsNullOrEmpty(a.FamilyNo))
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new { a.Id, a.FamilyNo, a.CreatedAt })
                .FirstOrDefaultAsync();

            if (existingAssessment != null)
            {
                // Decrypt FamilyNo if it's encrypted
                var decryptedFamilyNo = existingAssessment.FamilyNo;
                if (!string.IsNullOrEmpty(decryptedFamilyNo) && _encryptionService.CanUserDecrypt(User))
                {
                    try
                    {
                        decryptedFamilyNo = _encryptionService.Decrypt(decryptedFamilyNo);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to decrypt FamilyNo, using encrypted value");
                    }
                }
                return (decryptedFamilyNo ?? "UNKNOWN-000", true);
            }

            // Generate new family number based on first letter of last name
            string lastNameInitial = (user.LastName?.Length > 0) ? user.LastName.Substring(0, 1).ToUpper() : "X";
            
            // Get the highest sequence number for this letter from both assessment types
            var ncdFamilyNos = await _context.NCDRiskAssessments
                .Where(a => a.FamilyNo != null && a.FamilyNo.StartsWith(lastNameInitial + "-"))
                .Select(a => a.FamilyNo)
                .ToListAsync();
            
            int lastNCDNumber = ncdFamilyNos
                .Select(fn => fn.Substring(2))
                .Where(n => n.All(char.IsDigit))
                .Select(n => int.Parse(n))
                .DefaultIfEmpty(0)
                .Max();
                
            var heeadsssFamilyNos = await _context.HEEADSSSAssessments
                .Where(a => a.FamilyNo != null && a.FamilyNo.StartsWith(lastNameInitial + "-"))
                .Select(a => a.FamilyNo)
                .ToListAsync();
                
            int lastHEEADSSSNumber = heeadsssFamilyNos
                .Select(fn => fn.Substring(2))
                .Where(n => n.All(char.IsDigit))
                .Select(n => int.Parse(n))
                .DefaultIfEmpty(0)
                .Max();
                
            // Take the highest of the two numbers
            int lastNumber = Math.Max(lastNCDNumber, lastHEEADSSSNumber);
            
            // Generate new family number
            int newSequence = lastNumber + 1;
            string newFamilyNo = $"{lastNameInitial}-{newSequence:D3}"; // Format: X-001, X-002, etc.
            return (newFamilyNo, false);
        }

        private int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            _logger.LogInformation("Calculated age for birthdate {BirthDate}: {Age} years", birthDate.ToString("M/d/yyyy"), age);
            return age;
        }

        public class AgeCalculationRequest
        {
            public DateTime Birthday { get; set; }
        }

        // Handler for AJAX calls to generate family number
        public async Task<IActionResult> OnPostGenerateFamilyNumberAsync([FromBody] GenerateFamilyNumberRequest request)
        {
            try
            {
                _logger.LogInformation("=== GENERATE FAMILY NUMBER STARTED ===");
                _logger.LogInformation("Request received: {Request}", request != null ? "Not null" : "Null");
                _logger.LogInformation("Request LastName: {LastName}", request?.LastName);
                _logger.LogInformation("Request method: {Method}", Request.Method);
                _logger.LogInformation("Request content type: {ContentType}", Request.ContentType);
                
                if (request == null)
                {
                    _logger.LogError("Request object is null");
                    return new JsonResult(new { success = false, error = "Request data is missing" });
                }
                
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogError("User not found");
                    return new JsonResult(new { success = false, error = "User not found" });
                }

                _logger.LogInformation("User found: {UserId}", user.Id);
                _logger.LogInformation("User LastName: {UserLastName}", user.LastName);

                // Use the last name from the request if provided, otherwise use user's last name
                string lastName = !string.IsNullOrEmpty(request.LastName) ? request.LastName : user.LastName;
                if (string.IsNullOrEmpty(lastName))
                {
                    _logger.LogError("Last name is empty");
                    return new JsonResult(new { success = false, error = "Last name is required to generate family number" });
                }
                
                _logger.LogInformation("Using lastName: {LastName}", lastName);

                // PRIORITY 1: Check Patient table first (permanent record)
                var existingPatient = await _context.Patients
                    .Where(p => p.UserId == user.Id && !string.IsNullOrEmpty(p.FamilyNumber))
                    .FirstOrDefaultAsync();

                if (existingPatient != null)
                {
                    _logger.LogInformation("Found existing Patient record with FamilyNumber: {FamilyNumber}", existingPatient.FamilyNumber);
                    return new JsonResult(new { 
                        success = true, 
                        familyNumber = existingPatient.FamilyNumber,
                        familyNo = existingPatient.FamilyNumber, // Support both property names
                        isPreexisting = true,
                        message = "You already have a family number"
                    });
                }

                // PRIORITY 2: Check if patient already has a family number in either assessment type
                var existingNCDAssessment = await _context.NCDRiskAssessments
                    .Where(a => a.UserId == user.Id && !string.IsNullOrEmpty(a.FamilyNo))
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync();

                if (existingNCDAssessment != null)
                {
                    _logger.LogInformation("Found existing NCD assessment with FamilyNo: {FamilyNo}", existingNCDAssessment.FamilyNo);
                    return new JsonResult(new { 
                        success = true, 
                        familyNumber = existingNCDAssessment.FamilyNo,
                        familyNo = existingNCDAssessment.FamilyNo, // Support both property names
                        isPreexisting = true,
                        message = "You already have a family number from NCD assessment"
                    });
                }

                // PRIORITY 3: Check HEEADSSS assessments
                var existingHEEADSSSAssessment = await _context.HEEADSSSAssessments
                    .Where(a => a.UserId == user.Id && !string.IsNullOrEmpty(a.FamilyNo))
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync();

                if (existingHEEADSSSAssessment != null)
                {
                    _logger.LogInformation("Found existing HEEADSSS assessment with FamilyNo: {FamilyNo}", existingHEEADSSSAssessment.FamilyNo);
                    // Decrypt FamilyNo if it's encrypted
                    var decryptedFamilyNo = existingHEEADSSSAssessment.FamilyNo;
                    if (!string.IsNullOrEmpty(decryptedFamilyNo) && _encryptionService.CanUserDecrypt(User))
                    {
                        try
                        {
                            decryptedFamilyNo = _encryptionService.Decrypt(decryptedFamilyNo);
                            _logger.LogInformation("Decrypted FamilyNo: {DecryptedFamilyNo}", decryptedFamilyNo);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to decrypt FamilyNo, using encrypted value");
                        }
                    }
                    return new JsonResult(new { 
                        success = true, 
                        familyNumber = decryptedFamilyNo,
                        familyNo = decryptedFamilyNo, // Support both property names
                        isPreexisting = true,
                        message = "You already have a family number from HEEADSSS assessment"
                    });
                }

                // Use the new atomic family number service
                var result = await _familyNumberService.GenerateFamilyNumberAsync(
                    lastName, 
                    HealthFacility, 
                    null // Don't override prefix with PatientCategory - use last name for first-come-first-serve
                );

                if (result.Success)
                {
                    _logger.LogInformation("Generated new family number: {FamilyNumber}", result.FamilyNumber);
                    _logger.LogInformation("=== GENERATE FAMILY NUMBER COMPLETED SUCCESSFULLY ===");
                    
                    return new JsonResult(new { 
                        success = true, 
                        familyNumber = result.FamilyNumber, // Primary property
                        familyNo = result.FamilyNumber, // Support legacy code
                        isPreexisting = false,
                        prefix = result.Prefix,
                        sequenceNumber = result.SequenceNumber,
                        message = $"New family number generated: {result.FamilyNumber}"
                    });
                }
                else
                {
                    _logger.LogError("Family number generation failed: {Error}", result.Error);
                    return new JsonResult(new { 
                        success = false, 
                        error = result.Error ?? "Error generating family number. Please try again." 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating family number");
                return new JsonResult(new { success = false, error = "Error generating family number. Please try again." });
            }
        }

        public class GenerateFamilyNumberRequest
        {
            public string LastName { get; set; } = string.Empty;
        }


        public IActionResult OnGetTestEndpoint()
        {
            _logger.LogInformation("Test endpoint called successfully");
            return new JsonResult(new { success = true, message = "Test endpoint working" });
        }

        public IActionResult OnPostTestFamilyNumberAsync()
        {
            _logger.LogInformation("Test family number endpoint called");
            return new JsonResult(new { 
                success = true, 
                familyNo = "TEST-001", 
                isPreexisting = false,
                message = "Test endpoint working" 
            });
        }

        public async Task<IActionResult> OnPostCancelAppointmentAsync(int appointmentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return new JsonResult(new { success = false, error = "User not found" });
            }

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.PatientId == user.Id);

            if (appointment == null)
            {
                return new JsonResult(new { success = false, error = "Appointment not found" });
            }

            try
            {
                var oldStatus = appointment.Status;
                appointment.Status = AppointmentStatus.Cancelled;
                appointment.UpdatedAt = DateTime.Now;
                
                await _context.SaveChangesAsync();
                
                // Delete appointment booking notifications for this user
                try
                {
                    var notifications = await _context.Notifications
                        .Where(n => n.UserId == user.Id && 
                                    n.Title == "Appointment Booked" && 
                                    n.Message.Contains(appointment.AppointmentDate.ToString("MMM dd, yyyy")))
                        .ToListAsync();
                    
                    foreach (var notification in notifications)
                    {
                        await _notificationService.DeleteNotificationAsync(notification.Id);
                    }
                    
                    _logger.LogInformation("Deleted {Count} appointment booking notifications for user {UserId}", notifications.Count, user.Id);
                }
                catch (Exception notifEx)
                {
                    _logger.LogWarning(notifEx, "Error deleting appointment booking notifications");
                    // Don't fail the cancellation if notification deletion fails
                }
                
                // Log to audit trail
                try
                {
                    await _auditTrail.LogAsync(
                        "Appointment Cancelled",
                        "Cancel Appointment",
                        "Appointment",
                        appointmentId.ToString(),
                        $"Status: {oldStatus}",
                        $"Status: {AppointmentStatus.Cancelled}",
                        $"User cancelled appointment on {appointment.AppointmentDate:MMM dd, yyyy} at {appointment.AppointmentTime:hh\\:mm tt}"
                    );
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(auditEx, "Error logging to audit trail");
                    // Don't fail the cancellation if audit logging fails
                }
                
                return new JsonResult(new { success = true, message = "Appointment cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling appointment {AppointmentId}", appointmentId);
                return new JsonResult(new { success = false, error = "Failed to cancel appointment" });
            }
        }

        public async Task<IActionResult> OnPostSubmitAssessmentAsync([FromForm] string jsonData)
        {
            try
            {
                _logger.LogInformation("=== SUBMIT ASSESSMENT CALLED ===");
                _logger.LogInformation("Request method: {Method}", Request.Method);
                _logger.LogInformation("Request content type: {ContentType}", Request.ContentType);
                
                if (string.IsNullOrEmpty(jsonData))
                {
                    _logger.LogError("JSON data is null or empty");
                    return new JsonResult(new { success = false, error = "No JSON data provided" });
                }
                
                _logger.LogInformation("JSON data length: {Length}", jsonData.Length);
                _logger.LogInformation("JSON data preview: {Preview}", jsonData.Substring(0, Math.Min(100, jsonData.Length)) + "...");
                
                // Deserialize the JSON data with enhanced error handling
                NCDRiskAssessmentViewModel assessment;
                try
                {
                    assessment = System.Text.Json.JsonSerializer.Deserialize<NCDRiskAssessmentViewModel>(jsonData, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                        Converters = { new FlexibleStringConverter(), new FlexibleIntConverter(), new FlexibleBooleanConverter() }
                    });
                }
                catch (System.Text.Json.JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "JSON Deserialization Error: {Message}", jsonEx.Message);
                    _logger.LogError("JSON Path: {Path}", jsonEx.Path);
                    _logger.LogError("Byte Position: {Position}", jsonEx.BytePositionInLine);
                    
                    // Try to identify the problematic field
                    if (!string.IsNullOrEmpty(jsonEx.Path))
                    {
                        _logger.LogError("Problematic field: {Field}", jsonEx.Path);
                        
                        // Extract field name from path (e.g., "$.IsSmoker" -> "IsSmoker")
                        var fieldName = jsonEx.Path.Replace("$.", "").Replace("$", "");
                        _logger.LogError("Field causing error: {FieldName}", fieldName);
                        
                        // Log the JSON around the error position
                        if (jsonEx.BytePositionInLine.HasValue)
                        {
                            var startPos = Math.Max(0, (int)jsonEx.BytePositionInLine.Value - 50);
                            var endPos = Math.Min(jsonData.Length, (int)jsonEx.BytePositionInLine.Value + 50);
                            var context = jsonData.Substring(startPos, endPos - startPos);
                            _logger.LogError("JSON context around error: {Context}", context);
                        }
                    }
                    
                    return new JsonResult(new { 
                        success = false, 
                        error = $"JSON deserialization failed: {jsonEx.Message}",
                        field = jsonEx.Path?.Replace("$.", ""),
                        position = jsonEx.BytePositionInLine
                    });
                }
                
                if (assessment == null)
                {
                    _logger.LogError("Failed to deserialize assessment data");
                    return new JsonResult(new { success = false, error = "Failed to deserialize assessment data" });
                }
                
                _logger.LogInformation("Assessment deserialized successfully. UserId: {UserId}, AppointmentId: {AppointmentId}", 
                    assessment.UserId, assessment.AppointmentId);
                
                // Enhanced logging for debugging AppointmentId issues
                _logger.LogInformation("=== NCD ASSESSMENT SUBMISSION DEBUG ===");
                _logger.LogInformation("Raw AppointmentId from form: {RawAppointmentId}", assessment.AppointmentId);
                _logger.LogInformation("AppointmentId type: {AppointmentIdType}", assessment.AppointmentId?.GetType().Name);
                _logger.LogInformation("AppointmentId is null: {IsNull}", assessment.AppointmentId == null);
                _logger.LogInformation("AppointmentId has value: {HasValue}", assessment.AppointmentId.HasValue);
                if (assessment.AppointmentId.HasValue)
                {
                    _logger.LogInformation("AppointmentId value: {Value}", assessment.AppointmentId.Value);
                }
                
                // Validate required fields
                if (string.IsNullOrEmpty(assessment.UserId))
                {
                    _logger.LogError("UserId is required but not provided");
                    return new JsonResult(new { success = false, error = "User ID is required" });
                }
                
                // Parse AppointmentId
                int? appointmentIdInt = assessment.AppointmentId;
                if (appointmentIdInt.HasValue)
                {
                    _logger.LogInformation("Using AppointmentId: {AppointmentId}", appointmentIdInt);
                }
                else
                {
                    _logger.LogWarning("AppointmentId is null or invalid: {AppointmentId}", assessment.AppointmentId);
                }
                
                // Verify appointment exists if AppointmentId is provided
                if (appointmentIdInt.HasValue)
                {
                    var appointmentExists = await _context.Appointments.AnyAsync(a => a.Id == appointmentIdInt.Value);
                    if (!appointmentExists)
                    {
                        _logger.LogError("Appointment with ID {AppointmentId} does not exist", appointmentIdInt.Value);
                        return new JsonResult(new { success = false, error = "Appointment not found" });
                    }
                }
                
                // Map ViewModel to Entity
                var ncdEntity = new NCDRiskAssessment
                {
                    UserId = assessment.UserId,
                    AppointmentId = appointmentIdInt,
                    HealthFacility = assessment.HealthFacility ?? "Barangay Health Center",
                    FamilyNo = assessment.FamilyNo,
                    Address = assessment.Address,
                    Barangay = assessment.Barangay,
                    Birthday = assessment.Birthday?.ToString(),
                    Telepono = assessment.Telepono,
                    Edad = assessment.Edad,
                    Kasarian = assessment.Kasarian,
                    Relihiyon = assessment.Relihiyon,
                    CivilStatus = assessment.CivilStatus,
                    FirstName = assessment.FirstName,
                    MiddleName = assessment.MiddleName,
                    LastName = assessment.LastName,
                    Occupation = assessment.Occupation,
                    AppointmentType = assessment.AppointmentType ?? "General Checkup",
                    
                    // Medical History
                    HasDiabetes = assessment.HasDiabetes,
                    DiabetesYear = assessment.DiabetesYear,
                    DiabetesMedication = assessment.DiabetesMedication,
                    HasHypertension = assessment.HasHypertension,
                    HypertensionYear = assessment.HypertensionYear,
                    HypertensionMedication = assessment.HypertensionMedication,
                    HasCancer = assessment.HasCancer,
                    CancerType = assessment.CancerType,
                    CancerSite = assessment.CancerSite,
                    CancerYear = assessment.CancerYear,
                    CancerMedication = assessment.CancerMedication,
                    HasCOPD = assessment.HasCOPD,
                    COPDYear = assessment.COPDYear,
                    COPDMedication = assessment.COPDMedication,
                    HasLungDisease = assessment.HasLungDisease,
                    LungDiseaseYear = assessment.LungDiseaseYear,
                    LungDiseaseMedication = assessment.LungDiseaseMedication,
                    HasEyeDisease = assessment.HasEyeDisease,
                    EyeDiseaseYear = assessment.EyeDiseaseYear,
                    EyeDiseaseMedication = assessment.EyeDiseaseMedication,
                    
                    // Individual Family History Fields
                    FamilyHistoryCancerFather = assessment.FamilyHistoryCancerFather ?? "false",
                    FamilyHistoryCancerMother = assessment.FamilyHistoryCancerMother ?? "false",
                    FamilyHistoryCancerSibling = assessment.FamilyHistoryCancerSibling ?? "false",
                    FamilyHistoryDiabetesFather = assessment.FamilyHistoryDiabetesFather ?? "false",
                    FamilyHistoryDiabetesMother = assessment.FamilyHistoryDiabetesMother ?? "false",
                    FamilyHistoryDiabetesSibling = assessment.FamilyHistoryDiabetesSibling ?? "false",
                    FamilyHistoryHeartDiseaseFather = assessment.FamilyHistoryHeartDiseaseFather ?? "false",
                    FamilyHistoryHeartDiseaseMother = assessment.FamilyHistoryHeartDiseaseMother ?? "false",
                    FamilyHistoryHeartDiseaseSibling = assessment.FamilyHistoryHeartDiseaseSibling ?? "false",
                    FamilyHistoryLungDiseaseFather = assessment.FamilyHistoryLungDiseaseFather ?? "false",
                    FamilyHistoryLungDiseaseMother = assessment.FamilyHistoryLungDiseaseMother ?? "false",
                    FamilyHistoryLungDiseaseSibling = assessment.FamilyHistoryLungDiseaseSibling ?? "false",
                    FamilyHistoryStrokeFather = assessment.FamilyHistoryStrokeFather ?? "false",
                    FamilyHistoryStrokeMother = assessment.FamilyHistoryStrokeMother ?? "false",
                    FamilyHistoryStrokeSibling = assessment.FamilyHistoryStrokeSibling ?? "false",
                    FamilyHistoryKidneyDiseaseFather = assessment.FamilyHistoryKidneyDiseaseFather ?? "false",
                    FamilyHistoryKidneyDiseaseMother = assessment.FamilyHistoryKidneyDiseaseMother ?? "false",
                    FamilyHistoryKidneyDiseaseSibling = assessment.FamilyHistoryKidneyDiseaseSibling ?? "false",
                    FamilyHistoryEyeDiseaseFather = assessment.FamilyHistoryEyeDiseaseFather ?? "false",
                    FamilyHistoryEyeDiseaseMother = assessment.FamilyHistoryEyeDiseaseMother ?? "false",
                    FamilyHistoryEyeDiseaseSibling = assessment.FamilyHistoryEyeDiseaseSibling ?? "false",
                    
                    // Family History Aggregated
                    FamilyHasHypertension = assessment.FamilyHasHypertension,
                    FamilyHasHeartDisease = assessment.FamilyHasHeartDisease,
                    FamilyHasStroke = assessment.FamilyHasStroke,
                    FamilyHasDiabetes = assessment.FamilyHasDiabetes,
                    FamilyHasCancer = assessment.FamilyHasCancer,
                    FamilyHasKidneyDisease = assessment.FamilyHasKidneyDisease,
                    FamilyHasOtherDisease = assessment.FamilyHasOtherDisease,
                    FamilyOtherDiseaseDetails = assessment.FamilyOtherDiseaseDetails,
                    
                    // Chest Pain Details
                    ChestPain = assessment.ChestPain ?? "false",
                    ChestPainLocation = assessment.ChestPainLocation ?? "false",
                    ChestPainValue = assessment.ChestPainValue ?? "false",
                    HasChestPain = assessment.HasChestPain ?? "false",
                    ChestPainSpreadsToArm = assessment.ChestPainSpreadsToArm ?? "false",
                    NumbnessWhenWalkingFast = assessment.NumbnessWhenWalkingFast ?? "false",
                    LossOfConsciousnessLessThan10Min = assessment.LossOfConsciousnessLessThan10Min ?? "false",
                    PainLastsMoreThan30Min = assessment.PainLastsMoreThan30Min ?? "false",
                    PainRelievedWithRest = assessment.PainRelievedWithRest ?? "false",
                    SeeDoctorIfYes = assessment.SeeDoctorIfYes ?? "false",
                    
                    // Lifestyle Factors
                    SmokingStatus = assessment.SmokingStatus ?? "Non-smoker",
                    HighSaltIntake = assessment.HighSaltIntake,
                    AlcoholFrequency = assessment.AlcoholFrequency,
                    AlcoholConsumption = assessment.AlcoholConsumption,
                    AlcoholStoppedDuration = assessment.AlcoholStoppedDuration,
                    
                    // Alcohol Details
                    DrinksAlcohol = assessment.DrinksAlcohol ?? "false",
                    DrinksBeer = assessment.DrinksBeer ?? "false",
                    DrinksWine = assessment.DrinksWine ?? "false",
                    DrinksWhiskyGinBrandy = assessment.DrinksWhiskyGinBrandy ?? "false",
                    AlcoholAmount1Bottle320ml = assessment.AlcoholAmount1Bottle320ml ?? "false",
                    AlcoholAmount2Bottle640ml = assessment.AlcoholAmount2Bottle640ml ?? "false",
                    AlcoholAmount3to4WineGlasses300ml = assessment.AlcoholAmount3to4WineGlasses300ml ?? "false",
                    AlcoholAmountLessThan3Shot45ml = assessment.AlcoholAmountLessThan3Shot45ml ?? "false",
                    AlcoholAmountMoreThan4Shots75ml = assessment.AlcoholAmountMoreThan4Shots75ml ?? "false",
                    AlcoholFrequency1to3TimesPerWeek = assessment.AlcoholFrequency1to3TimesPerWeek ?? "false",
                    AlcoholFrequencyMoreThan4TimesPerWeek = assessment.AlcoholFrequencyMoreThan4TimesPerWeek ?? "false",
                    IsBingeDrinker = assessment.IsBingeDrinker ?? "false",
                    
                    // Exercise Details
                    ExerciseDuration = assessment.ExerciseDuration,
                    HasNoRegularExercise = assessment.HasNoRegularExercise,
                    HasEnoughExercise = assessment.HasEnoughExercise?.ToString() ?? "false",
                    InsufficientPhysicalActivity = assessment.InsufficientPhysicalActivity ?? "false",
                    ModerateIntensityExercise = assessment.ModerateIntensityExercise ?? "false",
                    VigorousIntensityExercise = assessment.VigorousIntensityExercise ?? "false",
                    CombinationExercise = assessment.CombinationExercise ?? "false",
                    
                    // Smoking Details
                    FormerSmoker = assessment.FormerSmoker ?? "false",
                    NeverSmokedButExposedToSmoke = assessment.NeverSmokedButExposedToSmoke ?? "false",
                    HasHistoryOfSmoking = assessment.HasHistoryOfSmoking ?? "false",
                    Smoked100Sticks = assessment.Smoked100Sticks ?? "false",
                    SmokingQuitDuration = assessment.SmokingQuitDuration ?? "",
                    
                    // Nutrition Details
                    EatsVegetablesDaily = assessment.EatsVegetablesDaily ?? "false",
                    EatsFruitsDaily = assessment.EatsFruitsDaily ?? "false",
                    EatsFishDaily = assessment.EatsFishDaily ?? "false",
                    EatsMeatDaily = assessment.EatsMeatDaily ?? "false",
                    HasUnhealthyDiet = assessment.HasUnhealthyDiet ?? "false",
                    EatsFattyFoodMoreThan2TimesPerWeek = assessment.EatsFattyFoodMoreThan2TimesPerWeek ?? "false",
                    EatsSweetFoodMoreThan2TimesPerWeek = assessment.EatsSweetFoodMoreThan2TimesPerWeek ?? "false",
                    EatsOilyFoodMoreThan2TimesPerWeek = assessment.EatsOilyFoodMoreThan2TimesPerWeek ?? "false",
                    HasHighSaltIntake = assessment.HasHighSaltIntake ?? "false",
                    HasStress = assessment.HasStress ?? "false",
                    
                    // Health Conditions
                    
                    // Risk Status
                    RiskStatus = assessment.RiskStatus ?? "Low Risk",
                    RiskPercentage = assessment.RiskPercentage,
                    
                    // Assessment Information
                    AssessmentDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    DateOfAssessment = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    InterviewedBy = assessment.InterviewedBy ?? "",
                    Designation = assessment.Designation ?? "",
                    DoctorName = assessment.DoctorName ?? "",
                    
                    // Identity Fields
                    IDNumber = assessment.IDNumber ?? "",
                    IDNo = assessment.IDNo ?? assessment.FamilyNo,
                    
                    // Chest Pain Questions (Q2.1-2.8) - Additional mappings
                    Pananakit21 = assessment.Pananakit21 ?? "false",
                    Pananakit22 = assessment.Pananakit22 ?? "false",
                    Pananakit23 = assessment.Pananakit23 ?? "false",
                    Pananakit24 = assessment.Pananakit24 ?? "false",
                    Pananakit25 = assessment.Pananakit25 ?? "false",
                    Pananakit26 = assessment.Pananakit26 ?? "false",
                    Pananakit27 = assessment.Pananakit27 ?? "false",
                    Pananakit28 = assessment.Pananakit28 ?? "false",
                    
                    // Additional missing fields for complete form mapping
                    HealthFacilityName = assessment.HealthFacilityName ?? "Baesa Health Center",
                    DateAssessment = assessment.DateAssessment ?? "",
                    
                    // Lung Disease - Proper mapping
                    HasLungDiseaseNonInfectious = assessment.HasLungDiseaseNonInfectious ?? "false",
                    
                    // Eye Disease - Proper mapping  
                    HasEyeDiseaseCondition = assessment.HasEyeDiseaseCondition ?? "false",
                    
                    // Asthma - Proper mapping
                    HasAsthma = assessment.HasAsthma ?? "false",
                    HasDifficultyBreathing = assessment.HasDifficultyBreathing ?? "false",
                    
                    // System Fields - These are now string columns for encryption
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                };
                
                _logger.LogInformation("Created NCD entity with UserId: {UserId}, AppointmentId: {AppointmentId}", 
                    ncdEntity.UserId, ncdEntity.AppointmentId);
                
                // DEBUGGING: Log Risk Status fields before encryption
                _logger.LogInformation("=== RISK STATUS FIELDS DEBUGGING (BEFORE ENCRYPTION) ===");
                _logger.LogInformation("HasDiabetes: '{HasDiabetes}' (type: {Type})", ncdEntity.HasDiabetes, ncdEntity.HasDiabetes?.GetType().Name);
                _logger.LogInformation("HasHypertension: '{HasHypertension}' (type: {Type})", ncdEntity.HasHypertension, ncdEntity.HasHypertension?.GetType().Name);
                _logger.LogInformation("HasCancer: '{HasCancer}' (type: {Type})", ncdEntity.HasCancer, ncdEntity.HasCancer?.GetType().Name);
                _logger.LogInformation("CancerSite: '{CancerSite}' (type: {Type})", ncdEntity.CancerSite, ncdEntity.CancerSite?.GetType().Name);
                _logger.LogInformation("HasCOPD: '{HasCOPD}' (type: {Type})", ncdEntity.HasCOPD, ncdEntity.HasCOPD?.GetType().Name);
                _logger.LogInformation("=== END RISK STATUS DEBUGGING ===");
                
                // Encrypt sensitive data before saving
                _logger.LogInformation("Encrypting sensitive data for NCD assessment");
                try
                {
                    ncdEntity.EncryptSensitiveData(_encryptionService);
                    _logger.LogInformation("Encryption completed successfully");
                }
                catch (Exception encEx)
                {
                    _logger.LogError(encEx, "Encryption failed: {Error}", encEx.Message);
                    return new JsonResult(new { success = false, error = "Encryption failed. Please try again." });
                }
                
                // Add to context
                _context.NCDRiskAssessments.Add(ncdEntity);
                
                // Save to database
                var rowsAffected = await _context.SaveChangesAsync();
                _logger.LogInformation("Database save completed. Rows affected: {RowsAffected}", rowsAffected);
                
                if (rowsAffected > 0)
                {
                    _logger.LogInformation("NCD Risk Assessment saved successfully with ID: {Id}", ncdEntity.Id);
                    
                    // AUDIT: Log NCD assessment submission
                    await _auditTrail.LogAsync(
                        "Create",
                        "Submitted NCD Risk Assessment",
                        "NCDRiskAssessment",
                        ncdEntity.Id.ToString(),
                        null,
                        JsonConvert.SerializeObject(new {
                            AppointmentId = ncdEntity.AppointmentId,
                            HasDiabetes = ncdEntity.HasDiabetes,
                            HasHypertension = ncdEntity.HasHypertension,
                            HasCancer = ncdEntity.HasCancer,
                            HasCOPD = ncdEntity.HasCOPD,
                            SmokingStatus = "Assessed"
                        }),
                        "Patient completed NCD risk screening assessment"
                    );
                    
                    // DEBUGGING: Log successful save with Risk Status fields
                    _logger.LogInformation("=== RISK STATUS FIELDS SAVED TO DATABASE ===");
                    _logger.LogInformation("Assessment ID: {Id}", ncdEntity.Id);
                    _logger.LogInformation("HasDiabetes saved: '{HasDiabetes}'", ncdEntity.HasDiabetes);
                    _logger.LogInformation("HasHypertension saved: '{HasHypertension}'", ncdEntity.HasHypertension);
                    _logger.LogInformation("HasCancer saved: '{HasCancer}'", ncdEntity.HasCancer);
                    _logger.LogInformation("CancerSite saved: '{CancerSite}'", ncdEntity.CancerSite);
                    _logger.LogInformation("HasCOPD saved: '{HasCOPD}'", ncdEntity.HasCOPD);
                    _logger.LogInformation("=== END RISK STATUS SAVE DEBUGGING ===");
                    
                    // Update or create Patient table record with family number if provided
                    if (!string.IsNullOrEmpty(assessment.FamilyNo))
                    {
                        try
                        {
                            // Determine the actual patient UserId (could be different for "book for someone else")
                            string targetUserId = assessment.UserId;
                            
                            // If appointment exists, use the appointment's PatientId (handles "book for someone else")
                            if (ncdEntity.AppointmentId.HasValue)
                            {
                                var appointment = await _context.Appointments
                                    .Include(a => a.Patient)
                                    .FirstOrDefaultAsync(a => a.Id == ncdEntity.AppointmentId.Value);
                                
                                if (appointment?.Patient != null)
                                {
                                    targetUserId = appointment.Patient.UserId;
                                    _logger.LogInformation("Using PatientId from appointment for family number update: {PatientId}", targetUserId);
                                }
                            }
                            
                            var user = await _context.Users.FindAsync(targetUserId);
                            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == targetUserId);
                            
                            // Also update the Appointment.FamilyNumber if appointment exists
                            if (ncdEntity.AppointmentId.HasValue)
                            {
                                var appointment = await _context.Appointments.FindAsync(ncdEntity.AppointmentId.Value);
                                if (appointment != null && appointment.FamilyNumber != assessment.FamilyNo)
                                {
                                    appointment.FamilyNumber = assessment.FamilyNo;
                                    appointment.UpdatedAt = DateTime.UtcNow;
                                    await _context.SaveChangesAsync();
                                    _logger.LogInformation("Updated Appointment FamilyNumber to: {FamilyNumber}", assessment.FamilyNo);
                                }
                            }
                            
                            if (patient != null)
                            {
                                // Always update patient record with latest family number from assessment
                                if (patient.FamilyNumber != assessment.FamilyNo)
                                {
                                    _logger.LogInformation("Updating Patient FamilyNumber from '{OldNumber}' to '{NewNumber}'", 
                                        patient.FamilyNumber ?? "NULL", assessment.FamilyNo);
                                    patient.FamilyNumber = assessment.FamilyNo;
                                    patient.UpdatedAt = DateTime.UtcNow;
                                    await _context.SaveChangesAsync();
                                    _logger.LogInformation("Updated Patient table with FamilyNumber: {FamilyNumber}", assessment.FamilyNo);
                                }
                                else
                                {
                                    _logger.LogInformation("Patient FamilyNumber already matches assessment: {FamilyNumber}", patient.FamilyNumber);
                                }
                            }
                            else if (user != null)
                            {
                                // Create new patient record
                                patient = new Patient
                                {
                                    UserId = targetUserId,
                                    FullName = user.FullName ?? $"{assessment.FirstName} {assessment.LastName}",
                                    FamilyNumber = assessment.FamilyNo,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };
                                _context.Patients.Add(patient);
                                await _context.SaveChangesAsync();
                                _logger.LogInformation("Created new Patient record with FamilyNumber: {FamilyNumber} for UserId: {UserId}", assessment.FamilyNo, targetUserId);
                            }
                            else
                            {
                                _logger.LogWarning("User not found for UserId: {UserId}, cannot create Patient record", targetUserId);
                            }
                        }
                        catch (Exception patientEx)
                        {
                            _logger.LogWarning(patientEx, "Error updating/creating Patient table with family number, continuing...");
                            // Don't fail the assessment submission if patient update fails
                        }
                    }
                    
                    // Update appointment status to InProgress after successful form submission (not Completed)
                    if (ncdEntity.AppointmentId.HasValue)
                    {
                        _logger.LogInformation("Updating appointment status to InProgress");
                        var appointment = await _context.Appointments.FindAsync(ncdEntity.AppointmentId.Value);
                        if (appointment != null)
                        {
                            var oldStatus = appointment.Status;
                            appointment.Status = AppointmentStatus.InProgress; // 2 = InProgress (Ongoing)
                            appointment.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Appointment status updated to InProgress");
                            
                            // Log to audit trail
                            try
                            {
                                await _auditTrail.LogAsync(
                                    "Appointment Assessment Completed",
                                    "Complete Assessment Form",
                                    "Appointment",
                                    appointment.Id.ToString(),
                                    $"Status: {oldStatus}",
                                    $"Status: {AppointmentStatus.InProgress}",
                                    $"User completed NCD assessment for appointment on {appointment.AppointmentDate:MMM dd, yyyy} at {appointment.AppointmentTime:hh\\:mm tt}"
                                );
                            }
                            catch (Exception auditEx)
                            {
                                _logger.LogWarning(auditEx, "Error logging to audit trail");
                                // Don't fail the submission if audit logging fails
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Appointment not found for ID: {AppointmentId}", ncdEntity.AppointmentId);
                        }
                    }
                    
                    return new JsonResult(new { 
                        success = true, 
                        message = "Assessment submitted successfully!",
                        assessmentId = ncdEntity.Id,
                        rowsAffected = rowsAffected
                    });
                }
                else
                {
                    _logger.LogError("No rows were affected during save operation");
                    return new JsonResult(new { 
                        success = false, 
                        error = "Failed to save assessment - no rows affected" 
                    });
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database update error: {Message}", dbEx.Message);
                return new JsonResult(new { 
                    success = false, 
                    error = "Database error occurred while saving assessment",
                    details = dbEx.InnerException?.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR: {Error}", ex.Message);
                return new JsonResult(new { 
                    success = false, 
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
        
        // Decryption method that matches the JavaScript encryption
        private string DecryptData(string encryptedData)
        {
            try
            {
                _logger.LogInformation("Starting decryption process");
                
                // Use the same key as JavaScript (in production, this should be in appsettings.json)
                const string ENCRYPTION_KEY = "BHCARE_2024_SECRET_KEY_32BYTES_LONG";
                
                // Convert key to bytes (first 32 bytes for AES-256)
                var keyBytes = System.Text.Encoding.UTF8.GetBytes(ENCRYPTION_KEY).Take(32).ToArray();
                
                // Decode the base64 encrypted data
                var encryptedBytes = Convert.FromBase64String(encryptedData);
                
                // Extract IV (first 16 bytes) and encrypted data
                var iv = new byte[16];
                var encrypted = new byte[encryptedBytes.Length - 16];
                
                Array.Copy(encryptedBytes, 0, iv, 0, 16);
                Array.Copy(encryptedBytes, 16, encrypted, 0, encrypted.Length);
                
                // Decrypt using AES-256-CBC
                using (var aes = System.Security.Cryptography.Aes.Create())
                {
                    aes.Key = keyBytes;
                    aes.IV = iv;
                    aes.Mode = System.Security.Cryptography.CipherMode.CBC;
                    aes.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
                    
                    using (var decryptor = aes.CreateDecryptor())
                    using (var msDecrypt = new MemoryStream(encrypted))
                    using (var csDecrypt = new System.Security.Cryptography.CryptoStream(msDecrypt, decryptor, System.Security.Cryptography.CryptoStreamMode.Read))
                    using (var srDecrypt = new StreamReader(csDecrypt))
                    {
                        var decryptedText = srDecrypt.ReadToEnd();
                        _logger.LogInformation("Decryption successful. Decrypted text length: {Length}", decryptedText.Length);
                        return decryptedText;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Decryption failed: {Error}", ex.Message);
                return null;
            }
        }
        
        private string SafeEncrypt(string plainText)
        {
            try
            {
                if (string.IsNullOrEmpty(plainText))
                    return plainText;
                
                // Temporarily disable encryption to fix 400 error
                _logger.LogInformation("Encryption temporarily disabled, returning plain text for length: {Length}", plainText.Length);
                return plainText;
                
                /*
                if (_encryptionService == null)
                {
                    _logger.LogWarning("Encryption service is null, returning plain text");
                    return plainText;
                }
                
                var encrypted = _encryptionService.Encrypt(plainText);
                _logger.LogInformation("Successfully encrypted text of length: {Length}", plainText.Length);
                return encrypted;
                */
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Encryption failed for text: {Text}, using plain text", plainText);
                return plainText; // Return plain text if encryption fails
            }
        }
        

        // Simple database test endpoint
        public async Task<IActionResult> OnGetTestDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Testing database connection");
                
                // Test basic connection
                var canConnect = await _context.Database.CanConnectAsync();
                _logger.LogInformation("Database connection test: {CanConnect}", canConnect);
                
                if (!canConnect)
                {
                    return new JsonResult(new { success = false, error = "Cannot connect to database" });
                }
                
                // Test simple query
                var count = await _context.NCDRiskAssessments.CountAsync();
                _logger.LogInformation("NCDRiskAssessments table count: {Count}", count);
                
                // Test inserting a simple record
                var testAssessment = new NCDRiskAssessment
                {
                    UserId = "test-user-id",
                    HealthFacility = "Test Facility",
                    FamilyNo = "TEST-001",
                    Address = "Test Address",
                    Barangay = "Test Barangay",
                    Birthday = DateTime.Now.AddYears(-30).ToString(),
                    Telepono = "1234567890",
                    Edad = "30",
                    Kasarian = "Male",
                    Relihiyon = "Test Religion",
                    HasDiabetes = "false",
                    HasHypertension = "false",
                    HasCancer = "false",
                    HasCOPD = "false",
                    HasLungDisease = "false",
                    HasEyeDisease = "false",
                    CancerType = "None",
                    FamilyHasHypertension = "false",
                    FamilyHasHeartDisease = "false",
                    FamilyHasStroke = "false",
                    FamilyHasDiabetes = "false",
                    FamilyHasCancer = "false",
                    FamilyHasKidneyDisease = "false",
                    FamilyHasOtherDisease = "false",
                    FamilyOtherDiseaseDetails = "None",
                    HighSaltIntake = "false",
                    AlcoholFrequency = "None",
                    AlcoholConsumption = "None",
                    ExerciseDuration = "None",
                    SmokingStatus = "Non-smoker",
                    RiskStatus = "Low Risk",
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    AppointmentType = "Test",
                    FirstName = "Test",
                    MiddleName = "Test",
                    LastName = "User",
                    Occupation = "Test",
                    CivilStatus = "Single",
                    HasDifficultyBreathing = "false",
                    HasAsthma = "false",
                    HasNoRegularExercise = "false"
                };

                _context.NCDRiskAssessments.Add(testAssessment);
                var rowsAffected = await _context.SaveChangesAsync();
                _logger.LogInformation("Test record inserted successfully. Rows affected: {RowsAffected}", rowsAffected);

                // Clean up test record
                _context.NCDRiskAssessments.Remove(testAssessment);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Test record cleaned up");
                
                return new JsonResult(new { 
                    success = true, 
                    message = "Database connection working",
                    canConnect = canConnect,
                    tableCount = count,
                    testInsertSuccessful = rowsAffected > 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database test failed: {Message}", ex.Message);
                return new JsonResult(new { 
                    success = false, 
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    
        // AJAX endpoint to get next sequential family number
        public async Task<IActionResult> OnGetNextFamilyNumberAsync(string letterPrefix)
        {
            try
            {
                if (string.IsNullOrEmpty(letterPrefix) || letterPrefix.Length == 0)
                {
                    return new JsonResult(new { success = false, error = "Letter prefix is required" });
                }

                string firstLetter = letterPrefix.Substring(0, 1).ToUpper();
                
                // Get the highest sequence number for this letter from both assessment types
                var ncdFamilyNos = await _context.NCDRiskAssessments
                    .Where(a => a.FamilyNo != null && a.FamilyNo.StartsWith(firstLetter + "-"))
                    .Select(a => a.FamilyNo)
                    .ToListAsync();
                
                int lastNCDNumber = ncdFamilyNos
                    .Select(fn => fn.Substring(2))
                    .Where(n => n.All(char.IsDigit))
                    .Select(n => int.Parse(n))
                    .DefaultIfEmpty(0)
                    .Max();
                    
                var heeadsssFamilyNos = await _context.HEEADSSSAssessments
                    .Where(a => a.FamilyNo != null && a.FamilyNo.StartsWith(firstLetter + "-"))
                    .Select(a => a.FamilyNo)
                    .ToListAsync();
                    
                int lastHEEADSSSNumber = heeadsssFamilyNos
                    .Select(fn => fn.Substring(2))
                    .Where(n => n.All(char.IsDigit))
                    .Select(n => int.Parse(n))
                    .DefaultIfEmpty(0)
                    .Max();
                    
                // Take the highest of the two numbers
                int lastNumber = Math.Max(lastNCDNumber, lastHEEADSSSNumber);
                
                // Generate new family number
                int newSequence = lastNumber + 1;
                string newFamilyNo = $"{firstLetter}-{newSequence:D3}"; // Format: X-001, X-002, etc.
                
                return new JsonResult(new { 
                    success = true, 
                    familyNo = newFamilyNo,
                    lastNumber = lastNumber,
                    newSequence = newSequence
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating next family number");
                return new JsonResult(new { success = false, error = "Error generating next family number" });
            }
        }
    }
    
    // Custom JSON converter to handle flexible type conversion for string properties
    public class FlexibleStringConverter : System.Text.Json.Serialization.JsonConverter<string>
    {
        public override string Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
        {
            if (reader.TokenType == System.Text.Json.JsonTokenType.String)
            {
                return reader.GetString() ?? string.Empty;
            }
            else if (reader.TokenType == System.Text.Json.JsonTokenType.Number)
            {
                return reader.GetInt32().ToString();
            }
            else if (reader.TokenType == System.Text.Json.JsonTokenType.True)
            {
                return "true";
            }
            else if (reader.TokenType == System.Text.Json.JsonTokenType.False)
            {
                return "false";
            }
            else if (reader.TokenType == System.Text.Json.JsonTokenType.Null)
            {
                return string.Empty;
            }
            else
            {
                throw new System.Text.Json.JsonException($"Cannot convert {reader.TokenType} to string");
            }
        }

        public override void Write(System.Text.Json.Utf8JsonWriter writer, string value, System.Text.Json.JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
    
    // Custom JSON converter to handle flexible type conversion for int? properties
    public class FlexibleIntConverter : System.Text.Json.Serialization.JsonConverter<int?>
    {
        public override int? Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
        {
            if (reader.TokenType == System.Text.Json.JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (string.IsNullOrEmpty(stringValue))
                    return null;
                
                if (int.TryParse(stringValue, out int intValue))
                    return intValue;
                
                return null;
            }
            else if (reader.TokenType == System.Text.Json.JsonTokenType.Number)
            {
                return reader.GetInt32();
            }
            else if (reader.TokenType == System.Text.Json.JsonTokenType.Null)
            {
                return null;
            }
            else
            {
                throw new System.Text.Json.JsonException($"Cannot convert {reader.TokenType} to int?");
            }
        }

        public override void Write(System.Text.Json.Utf8JsonWriter writer, int? value, System.Text.Json.JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteNumberValue(value.Value);
            else
                writer.WriteNullValue();
        }
    }
    
    // Custom JSON converter to handle flexible type conversion for bool properties
    public class FlexibleBooleanConverter : System.Text.Json.Serialization.JsonConverter<bool>
    {
        public override bool Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
        {
            if (reader.TokenType == System.Text.Json.JsonTokenType.True)
            {
                return true;
            }
            else if (reader.TokenType == System.Text.Json.JsonTokenType.False)
            {
                return false;
            }
            else if (reader.TokenType == System.Text.Json.JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (string.IsNullOrEmpty(stringValue))
                    return false;
                
                // Handle various string representations of boolean values
                if (string.Equals(stringValue, "true", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(stringValue, "1", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(stringValue, "yes", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(stringValue, "on", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                else if (string.Equals(stringValue, "false", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(stringValue, "0", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(stringValue, "no", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(stringValue, "off", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                
                // If it's a non-empty string that doesn't match boolean patterns, treat as true
                return !string.IsNullOrWhiteSpace(stringValue);
            }
            else if (reader.TokenType == System.Text.Json.JsonTokenType.Number)
            {
                return reader.GetInt32() != 0;
            }
            else if (reader.TokenType == System.Text.Json.JsonTokenType.Null)
            {
                return false;
            }
            else
            {
                throw new System.Text.Json.JsonException($"Cannot convert {reader.TokenType} to boolean");
            }
        }

        public override void Write(System.Text.Json.Utf8JsonWriter writer, bool value, System.Text.Json.JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }
}
