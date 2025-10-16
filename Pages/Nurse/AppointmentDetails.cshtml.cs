using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using Barangay.Extensions;
using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Collections.Generic; // Added for List

namespace Barangay.Pages.Nurse
{
    [Authorize(Roles = "Nurse,Head Nurse")]
    public class AppointmentDetailsModel : PageModel
    {
        private readonly EncryptedDbContext _context;
        private readonly ILogger<AppointmentDetailsModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDataEncryptionService _encryptionService;

        public AppointmentDetailsModel(
            EncryptedDbContext context,
            ILogger<AppointmentDetailsModel> logger,
            UserManager<ApplicationUser> userManager,
            IDataEncryptionService encryptionService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _encryptionService = encryptionService;
        }

        private string SafeDecrypt(string encryptedValue)
        {
            if (string.IsNullOrEmpty(encryptedValue) || !_encryptionService.IsEncrypted(encryptedValue))
                return encryptedValue;

            try
            {
                var decryptedValue = _encryptionService.DecryptForUser(encryptedValue, User);
                
                // If decryption returns access denied or the same value, try direct decryption
                if (decryptedValue == "[ACCESS DENIED]" || decryptedValue == encryptedValue)
                {
                    _logger.LogWarning("DecryptForUser failed or returned access denied, trying direct decryption");
                    decryptedValue = _encryptionService.Decrypt(encryptedValue);
                }
                
                return decryptedValue;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decrypt value: {EncryptedValue}", encryptedValue?.Substring(0, Math.Min(20, encryptedValue?.Length ?? 0)));
                
                // Try direct decryption as fallback
                try
                {
                    return _encryptionService.Decrypt(encryptedValue);
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Direct decryption also failed for value: {EncryptedValue}", encryptedValue?.Substring(0, Math.Min(20, encryptedValue?.Length ?? 0)));
                    return "[DECRYPTION FAILED]";
                }
            }
        }

        public AppointmentsModel.AppointmentViewModel Appointment { get; set; }
        public NCDRiskAssessment NCDRiskAssessment { get; set; }
        public HEEADSSSAssessment HEEADSSSAssessment { get; set; }
        public AdolescentHealthInfo AdolescentHealthInfo { get; set; }
        public int PatientAge { get; set; }
        public bool HasNCDAssessment { get; set; }
        public bool HasHEEADSSSAssessment { get; set; }
        public bool HasAdolescentHealthInfo { get; set; }

        // New properties for booking and forms information
        public string BookedBy { get; set; }
        public DateTime? BookingDate { get; set; }
        public List<string> CompletedForms { get; set; } = new List<string>();
        public List<string> AvailableForms { get; set; } = new List<string>();

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Appointment ID not provided");
                return NotFound("Appointment ID must be provided");
            }

            try
            {
                _logger.LogInformation("=== Starting appointment details loading for ID: {Id} ===", id);

                // Step 1: User Authentication Check
                _logger.LogInformation("Step 1: User authentication check");
                _logger.LogInformation("User authentication: {IsAuthenticated}, User roles: {Roles}", 
                    User?.Identity?.IsAuthenticated, 
                    string.Join(", ", User?.Claims?.Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Select(c => c.Value) ?? new string[0]));
                _logger.LogInformation("CanUserDecrypt: {CanDecrypt}", _encryptionService.CanUserDecrypt(User));

                // Step 2: Database Query
                _logger.LogInformation("Step 2: Database query for appointment");
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (appointment == null)
                {
                    _logger.LogWarning("Appointment with ID {Id} not found", id);
                    return NotFound("Appointment not found");
                }

                _logger.LogInformation("Step 2 Complete: Appointment found with Status={Status}, PatientId={PatientId}", 
                    appointment.Status, appointment.PatientId);

                // Step 3: Decrypt doctor name if available
                string doctorName = "Not Assigned";
                if (appointment.Doctor != null)
                {
                    try
                    {
                        appointment.Doctor.DecryptSensitiveData(_encryptionService, User);
                        doctorName = appointment.Doctor.FullName;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to decrypt doctor data for appointment ID {Id}", id);
                        doctorName = "Not Assigned";
                    }
                }

                // Step 4: Decrypt patient name
                string patientName = "Unknown";
                if (!string.IsNullOrEmpty(appointment.PatientName))
                {
                    if (_encryptionService.IsEncrypted(appointment.PatientName))
                    {
                        patientName = _encryptionService.DecryptForUser(appointment.PatientName, User);
                    }
                    else
                    {
                        patientName = appointment.PatientName;
                    }
                }
                else if (appointment.Patient != null)
                {
                    patientName = appointment.Patient.FullName;
                }

                // Step 5: Convert to View Model
                _logger.LogInformation("Step 5: Converting to view model");
                Appointment = new AppointmentsModel.AppointmentViewModel
                {
                    Id = appointment.Id,
                    PatientId = appointment.PatientId,
                    PatientName = patientName,
                    AppointmentDate = appointment.AppointmentDate,
                    AppointmentTime = appointment.AppointmentTime,
                    Status = appointment.Status,
                    Type = appointment.Type ?? "General",
                    Description = appointment.Description
                };

                _logger.LogInformation("Step 5 Complete: View model created");

                // Step 6: Patient Data Loading and Decryption
                _logger.LogInformation("Step 6: Loading patient details");
                if (appointment.Patient != null)
                {
                    _logger.LogInformation("Patient found: UserId={UserId}", appointment.Patient.UserId);
                    try
                    {
                        // Try to decrypt patient data first
                        _logger.LogInformation("Attempting patient data decryption");
                        appointment.Patient.DecryptSensitiveData(_encryptionService, User);
                        
                        // Use safe decryption for critical patient fields
                        appointment.Patient.FullName = SafeDecrypt(appointment.Patient.FullName);
                        appointment.Patient.Address = SafeDecrypt(appointment.Patient.Address);
                        appointment.Patient.ContactNumber = SafeDecrypt(appointment.Patient.ContactNumber);
                        
                        _logger.LogInformation("Successfully decrypted patient data for appointment ID {Id}", id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to decrypt patient data for appointment ID {Id}, continuing without decryption", id);
                        // Continue without throwing to avoid breaking the entire page
                    }
                    
                    // Use the age from the appointment (age at booking time) instead of current age
                    PatientAge = appointment.AgeValue > 0 ? appointment.AgeValue : appointment.Patient.Age;
                    _logger.LogInformation("PatientAge set to: {PatientAge}", PatientAge);
                }
                else
                {
                    _logger.LogInformation("No patient data found for appointment ID {Id}", id);
                }

                _logger.LogInformation("Step 4 Complete: Patient data processing");

                // Step 5: NCD Risk Assessment Loading
                _logger.LogInformation("Step 5: Checking for NCD Risk Assessment existence");
                
                // Only check by AppointmentId - no fallback to UserId to prevent showing forms from other appointments
                HasNCDAssessment = await _context.NCDRiskAssessments
                    .AnyAsync(a => a.AppointmentId == id);
                _logger.LogInformation("NCD Risk Assessment exists by AppointmentId: {HasNCDAssessment}", HasNCDAssessment);

                // Load NCD Risk Assessment if it exists
                if (HasNCDAssessment)
                {
                    try {
                        _logger.LogInformation("Loading NCD Risk Assessment from database");
                        
                        // Only load by AppointmentId - no fallback to UserId
                        NCDRiskAssessment = await _context.NCDRiskAssessments
                            .Where(a => a.AppointmentId == id)
                            .AsNoTracking()
                            .FirstOrDefaultAsync();
                        
                        if (NCDRiskAssessment != null)
                        {
                            _logger.LogInformation("NCD Risk Assessment loaded from database, attempting decryption");
                        }
                        
                        // Decrypt NCD Risk Assessment data
                        if (NCDRiskAssessment != null)
                        {
                            try
                            {
                                _logger.LogInformation("Starting NCD Risk Assessment decryption for appointment ID {Id}", id);
                                _logger.LogInformation("User can decrypt: {CanDecrypt}", _encryptionService.CanUserDecrypt(User));
                                _logger.LogInformation("User roles: {Roles}", string.Join(", ", User?.Claims?.Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Select(c => c.Value) ?? new string[0]));
                                
                                NCDRiskAssessment.DecryptSensitiveData(_encryptionService, User);
                                
                                // Manual decryption fallback for critical NCD fields using safe decryption
                                _logger.LogInformation("Decrypting NCD fields individually");
                                NCDRiskAssessment.FirstName = SafeDecrypt(NCDRiskAssessment.FirstName);
                                NCDRiskAssessment.MiddleName = SafeDecrypt(NCDRiskAssessment.MiddleName);
                                NCDRiskAssessment.LastName = SafeDecrypt(NCDRiskAssessment.LastName);
                                NCDRiskAssessment.Edad = SafeDecrypt(NCDRiskAssessment.Edad);
                                NCDRiskAssessment.Kasarian = SafeDecrypt(NCDRiskAssessment.Kasarian);
                                NCDRiskAssessment.Address = SafeDecrypt(NCDRiskAssessment.Address);
                                NCDRiskAssessment.Barangay = SafeDecrypt(NCDRiskAssessment.Barangay);
                                NCDRiskAssessment.Telepono = SafeDecrypt(NCDRiskAssessment.Telepono);
                                NCDRiskAssessment.SmokingStatus = SafeDecrypt(NCDRiskAssessment.SmokingStatus);
                                NCDRiskAssessment.AlcoholFrequency = SafeDecrypt(NCDRiskAssessment.AlcoholFrequency);
                                NCDRiskAssessment.HighSaltIntake = SafeDecrypt(NCDRiskAssessment.HighSaltIntake);
                                NCDRiskAssessment.ExerciseDuration = SafeDecrypt(NCDRiskAssessment.ExerciseDuration);
                                NCDRiskAssessment.RiskStatus = SafeDecrypt(NCDRiskAssessment.RiskStatus);
                                NCDRiskAssessment.AssessmentDate = SafeDecrypt(NCDRiskAssessment.AssessmentDate);
                                NCDRiskAssessment.DateOfAssessment = SafeDecrypt(NCDRiskAssessment.DateOfAssessment);
                                
                                // Decrypt COPD-related fields
                                NCDRiskAssessment.HasCOPD = SafeDecrypt(NCDRiskAssessment.HasCOPD);
                                NCDRiskAssessment.COPDMedication = SafeDecrypt(NCDRiskAssessment.COPDMedication);
                                NCDRiskAssessment.COPDYear = SafeDecrypt(NCDRiskAssessment.COPDYear);
                                
                                // CreatedAt and UpdatedAt are now DateTime, no decryption needed
                                
                                _logger.LogInformation("Successfully loaded and decrypted NCDRiskAssessment data for appointment ID {Id}", id);
                                _logger.LogInformation("Sample decrypted data - FirstName: {FirstName}, LastName: {LastName}", 
                                    NCDRiskAssessment.FirstName?.Substring(0, Math.Min(10, NCDRiskAssessment.FirstName?.Length ?? 0)),
                                    NCDRiskAssessment.LastName?.Substring(0, Math.Min(10, NCDRiskAssessment.LastName?.Length ?? 0)));
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to decrypt NCD Risk Assessment data for appointment ID {Id}", id);
                                // Continue without throwing to avoid breaking the entire page
                            }
                        }
                        else
                        {
                            _logger.LogWarning("NCDRiskAssessment is null for appointment ID {Id}", id);
                            HasNCDAssessment = false;
                        }
                    }
                    catch (Exception ex) {
                        _logger.LogError(ex, "Error loading NCD Risk Assessment data for appointment ID {Id}", id);
                        HasNCDAssessment = false;
                    }
                }
                _logger.LogInformation("Step 5 Complete: NCD Risk Assessment processing");

                // Check for HEEADSSS Assessment existence based on AppointmentId
                // BUT ONLY if the patient is in the appropriate age range (10-19)
                if (PatientAge >= 10 && PatientAge <= 19)
                {
                    _logger.LogInformation("Checking for HEEADSSS Assessment for appointment ID: {AppointmentId} (Patient age: {Age})", id, PatientAge);
                    
                    // Since AppointmentId is encrypted, we need to load all assessments and decrypt to check
                    HasHEEADSSSAssessment = false;
                    HEEADSSSAssessment = null;

                try
                {
                    // Load all HEEADSSS assessments and check by decrypting AppointmentId
                    var allHEEADSSSAssessments = await _context.HEEADSSSAssessments
                        .AsNoTracking()
                        .ToListAsync();
                    
                    _logger.LogInformation("Found {Count} HEEADSSS assessments in database", allHEEADSSSAssessments.Count);
                    _logger.LogInformation("Looking for appointment ID: {AppointmentId}", id);
                    
                    // Debug: Log all HEEADSSS assessments and their AppointmentIds
                    for (int i = 0; i < allHEEADSSSAssessments.Count; i++)
                    {
                        var assessment = allHEEADSSSAssessments[i];
                        _logger.LogInformation("HEEADSSS Assessment #{Index}: ID={Id}, AppointmentId='{AppointmentId}', UserId='{UserId}', CreatedAt={CreatedAt}", 
                            i + 1, assessment.Id, assessment.AppointmentId, assessment.UserId, assessment.CreatedAt);
                    }
                    
                    foreach (var assessment in allHEEADSSSAssessments)
                    {
                        try
                        {
                            _logger.LogInformation("Checking HEEADSSS Assessment ID: {AssessmentId}, Encrypted AppointmentId: {EncryptedAppId}", 
                                assessment.Id, assessment.AppointmentId);
                            
                            // Try different decryption methods to check AppointmentId
                            string decryptedAppointmentId = null;
                            
                            // First, check if it's encrypted at all
                            if (_encryptionService.IsEncrypted(assessment.AppointmentId))
                            {
                                try
                                {
                                    decryptedAppointmentId = _encryptionService.DecryptForUser(assessment.AppointmentId, User);
                                    _logger.LogInformation("Decrypted using DecryptForUser: {DecryptedAppId}", decryptedAppointmentId);
                                    
                                    // Check if the result is still encrypted (double encryption)
                                    if (_encryptionService.IsEncrypted(decryptedAppointmentId))
                                    {
                                        _logger.LogInformation("Result is still encrypted, decrypting again");
                                        decryptedAppointmentId = _encryptionService.DecryptForUser(decryptedAppointmentId, User);
                                        _logger.LogInformation("Double decrypted result: {DecryptedAppId}", decryptedAppointmentId);
                                    }
                                }
                                catch (Exception decryptEx)
                                {
                                    _logger.LogWarning(decryptEx, "DecryptForUser failed, trying alternative method");
                                    try
                                    {
                                        decryptedAppointmentId = _encryptionService.Decrypt(assessment.AppointmentId);
                                        _logger.LogInformation("Decrypted using Decrypt: {DecryptedAppId}", decryptedAppointmentId);
                                        
                                        // Check if the result is still encrypted
                                        if (_encryptionService.IsEncrypted(decryptedAppointmentId))
                                        {
                                            _logger.LogInformation("Result is still encrypted, decrypting again");
                                            decryptedAppointmentId = _encryptionService.Decrypt(decryptedAppointmentId);
                                            _logger.LogInformation("Double decrypted result: {DecryptedAppId}", decryptedAppointmentId);
                                        }
                                    }
                                    catch (Exception decryptEx2)
                                    {
                                        _logger.LogWarning(decryptEx2, "Both decryption methods failed");
                                        decryptedAppointmentId = assessment.AppointmentId; // Use as-is
                                    }
                                }
                            }
                            else
                            {
                                decryptedAppointmentId = assessment.AppointmentId; // Not encrypted
                                _logger.LogInformation("AppointmentId is not encrypted: {AppointmentId}", decryptedAppointmentId);
                            }
                            
                            _logger.LogInformation("Final decrypted AppointmentId: {DecryptedAppId}", decryptedAppointmentId);
                            
                            if (decryptedAppointmentId == id.ToString())
                            {
                                _logger.LogInformation("MATCH FOUND! HEEADSSS Assessment ID: {AssessmentId} matches appointment ID: {AppointmentId}", 
                                    assessment.Id, id);
                            HasHEEADSSSAssessment = true;
                                HEEADSSSAssessment = assessment;
                                break;
                            }
                            else
                            {
                                _logger.LogInformation("No match - Assessment ID: {AssessmentId}, Expected: {ExpectedId}, Got: {GotId}", 
                                    assessment.Id, id, decryptedAppointmentId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to decrypt AppointmentId for HEEADSSS assessment {Id}. AppointmentId: {AppointmentId}", 
                                assessment.Id, assessment.AppointmentId);
                            // Continue checking other assessments
                        }
                    }
                    
                    _logger.LogInformation("HEEADSSS Assessment exists by AppointmentId: {HasHEEADSSSAssessment}", HasHEEADSSSAssessment);
                    
                    if (!HasHEEADSSSAssessment)
                    {
                        _logger.LogInformation("No HEEADSSS Assessment found for appointment ID: {AppointmentId}. This means either:", id);
                        _logger.LogInformation("1. No HEEADSSS Assessment has been created for this appointment yet");
                        _logger.LogInformation("2. The AppointmentId in the database doesn't match the current appointment ID");
                        _logger.LogInformation("3. There's an issue with the decryption process");
                        
                        // Fallback: Check if there's a HEEADSSS Assessment for this patient (UserId) that might be associated with this appointment
                        // BUT ONLY if the patient is in the appropriate age range (10-19)
                        if (appointment.Patient != null && PatientAge >= 10 && PatientAge <= 19)
                        {
                            _logger.LogInformation("Checking for HEEADSSS Assessment by UserId as fallback: {UserId} (Patient age: {Age})", appointment.Patient.UserId, PatientAge);
                            var fallbackAssessment = await _context.HEEADSSSAssessments
                                .Where(a => a.UserId == appointment.Patient.UserId)
                                .OrderByDescending(a => a.CreatedAt)
                                .FirstOrDefaultAsync();
                            
                            if (fallbackAssessment != null)
                            {
                                _logger.LogInformation("Found HEEADSSS Assessment by UserId: {AssessmentId}, CreatedAt: {CreatedAt}", 
                                    fallbackAssessment.Id, fallbackAssessment.CreatedAt);
                                
                                // Check if this assessment was created around the same time as the appointment
                                var timeDifference = Math.Abs((fallbackAssessment.CreatedAt - appointment.CreatedAt).TotalHours);
                                if (timeDifference <= 24) // Within 24 hours
                                {
                                    _logger.LogInformation("Assessment created within 24 hours of appointment, considering it a match");
                                    HasHEEADSSSAssessment = true;
                                    HEEADSSSAssessment = fallbackAssessment;
                                }
                                else
                                {
                                    _logger.LogInformation("Assessment created {Hours} hours apart from appointment, not considering it a match", timeDifference);
                                }
                            }
                            else
                            {
                                _logger.LogInformation("No HEEADSSS Assessment found by UserId either");
                            }
                        }
                        else if (appointment.Patient != null)
                        {
                            _logger.LogInformation("Skipping HEEADSSS Assessment fallback check - patient age {Age} is not appropriate for HEEADSSS (should be 10-19)", PatientAge);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking for HEEADSSS Assessment for appointment ID: {AppointmentId}", id);
                    HasHEEADSSSAssessment = false;
                }

                if (HasHEEADSSSAssessment && HEEADSSSAssessment != null)
                {
                    try
                    {
                        // Decrypt patient data first (this might already be done above, but ensure it's done safely)
                        if (appointment.Patient != null)
                        {
                            appointment.Patient.DecryptSensitiveData(_encryptionService, User);
                            
                            // Additional safe decryption for patient critical fields
                            appointment.Patient.FullName = SafeDecrypt(appointment.Patient.FullName);
                            appointment.Patient.Address = SafeDecrypt(appointment.Patient.Address);
                            appointment.Patient.ContactNumber = SafeDecrypt(appointment.Patient.ContactNumber);
                        }
                        
                        _logger.LogInformation("Decrypting HEEADSSS Assessment data for appointment ID: {AppointmentId}", id);
                        
                        // Log some encrypted values before decryption
                        _logger.LogInformation("Before decryption - FullName: {FullName}, Age: {Age}, Gender: {Gender}", 
                            HEEADSSSAssessment.FullName?.Substring(0, Math.Min(20, HEEADSSSAssessment.FullName?.Length ?? 0)) + "...",
                            HEEADSSSAssessment.Age?.Substring(0, Math.Min(20, HEEADSSSAssessment.Age?.Length ?? 0)) + "...",
                            HEEADSSSAssessment.Gender?.Substring(0, Math.Min(20, HEEADSSSAssessment.Gender?.Length ?? 0)) + "...");
                        
                        // Decrypt the HEEADSSS assessment data
                        HEEADSSSAssessment.DecryptSensitiveData(_encryptionService, User);
                        
                        // Manual decryption fallback for critical fields using safe decryption
                        HEEADSSSAssessment.FullName = SafeDecrypt(HEEADSSSAssessment.FullName);
                        HEEADSSSAssessment.Age = SafeDecrypt(HEEADSSSAssessment.Age);
                        HEEADSSSAssessment.Gender = SafeDecrypt(HEEADSSSAssessment.Gender);
                        HEEADSSSAssessment.Address = SafeDecrypt(HEEADSSSAssessment.Address);
                        HEEADSSSAssessment.ContactNumber = SafeDecrypt(HEEADSSSAssessment.ContactNumber);
                        HEEADSSSAssessment.HomeEnvironment = SafeDecrypt(HEEADSSSAssessment.HomeEnvironment);
                        HEEADSSSAssessment.FamilyRelationship = SafeDecrypt(HEEADSSSAssessment.FamilyRelationship);
                        HEEADSSSAssessment.HomeFamilyProblems = SafeDecrypt(HEEADSSSAssessment.HomeFamilyProblems);
                        HEEADSSSAssessment.HomeParentalListening = SafeDecrypt(HEEADSSSAssessment.HomeParentalListening);
                        HEEADSSSAssessment.SchoolPerformance = SafeDecrypt(HEEADSSSAssessment.SchoolPerformance);
                        // AttendanceIssues is now a boolean field, no need to decrypt
                        HEEADSSSAssessment.CareerPlans = SafeDecrypt(HEEADSSSAssessment.CareerPlans);
                        HEEADSSSAssessment.EducationCurrentlyStudying = SafeDecrypt(HEEADSSSAssessment.EducationCurrentlyStudying);
                        HEEADSSSAssessment.Hobbies = SafeDecrypt(HEEADSSSAssessment.Hobbies);
                        HEEADSSSAssessment.PhysicalActivity = SafeDecrypt(HEEADSSSAssessment.PhysicalActivity);
                        HEEADSSSAssessment.ScreenTime = SafeDecrypt(HEEADSSSAssessment.ScreenTime);
                        HEEADSSSAssessment.ActivitiesRegularExercise = SafeDecrypt(HEEADSSSAssessment.ActivitiesRegularExercise);
                        
                        _logger.LogInformation("Successfully loaded and decrypted HEEADSSS Assessment data for appointment ID {Id}", id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to decrypt HEEADSSS assessment data for appointment ID {Id}", id);
                        HasHEEADSSSAssessment = false;
                        HEEADSSSAssessment = null;
                    }
                }

                _logger.LogInformation("HEEADSSS Assessment found: {HasAssessment}", HasHEEADSSSAssessment);

                // HEEADSSS Assessment data is already decrypted above
                if (HasHEEADSSSAssessment && HEEADSSSAssessment != null)
                {
                    _logger.LogInformation("HEEADSSS Assessment loaded and decrypted. FullName: {FullName}, Age: {Age}, Gender: {Gender}", 
                        HEEADSSSAssessment.FullName, HEEADSSSAssessment.Age, HEEADSSSAssessment.Gender);
                }
                
                _logger.LogInformation("Assessment flags - NCD: {HasNCD}, HEEADSSS: {HasHEEADSSS}", HasNCDAssessment, HasHEEADSSSAssessment);

                // Add additional properties to track history
                if (NCDRiskAssessment != null)
                {
                    _logger.LogInformation("NCD Risk Assessment creation date: {Date}", NCDRiskAssessment.CreatedAt);
                }

                if (HEEADSSSAssessment != null)
                {
                    _logger.LogInformation("HEEADSSS Assessment creation date: {Date}", HEEADSSSAssessment.CreatedAt);
                }
                }
                else
                {
                    _logger.LogInformation("Skipping HEEADSSS Assessment check - patient age {Age} is not appropriate for HEEADSSS (should be 10-19)", PatientAge);
                    HasHEEADSSSAssessment = false;
                    HEEADSSSAssessment = null;
                }

                // Step 7: Load Adolescent Health Information
                _logger.LogInformation("Step 7: Loading Adolescent Health Information");
                HasAdolescentHealthInfo = false;
                AdolescentHealthInfo = null;

                try
                {
                    // Only check by AppointmentId - no fallback to UserId to prevent showing forms from other appointments
                    var adolescentHealthInfo = await _context.AdolescentHealthInfo
                        .Where(a => a.AppointmentId == id.ToString())
                        .FirstOrDefaultAsync();

                    if (adolescentHealthInfo != null)
                    {
                        try
                        {
                            adolescentHealthInfo.DecryptSensitiveData(_encryptionService, User);
                            HasAdolescentHealthInfo = true;
                            AdolescentHealthInfo = adolescentHealthInfo;
                            _logger.LogInformation("Adolescent Health Information loaded for patient: {PatientName}", adolescentHealthInfo.PatientName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to decrypt Adolescent Health Information {Id}", adolescentHealthInfo.Id);
                            HasAdolescentHealthInfo = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AdolescentHealthInfo table may not exist or query failed. Skipping this section.");
                    HasAdolescentHealthInfo = false;
                }

                _logger.LogInformation("Adolescent Health Info found: {HasAdolescentHealthInfo}", HasAdolescentHealthInfo);
                _logger.LogInformation("Step 7 Complete: Adolescent Health Information processing");

                // Step 8: Load booking and forms information
                _logger.LogInformation("Step 8: Loading booking and forms information");
                
                // Get booking information
                if (appointment.Patient != null)
                {
                    try
                    {
                        // Decrypt patient name if needed
                        appointment.Patient.DecryptSensitiveData(_encryptionService, User);
                        BookedBy = appointment.Patient.FullName ?? "Unknown Patient";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to decrypt patient name for booking info");
                        BookedBy = "Unknown Patient";
                    }
                }
                else
                {
                    BookedBy = "Unknown Patient";
                }
                
                // Use appointment creation date as booking date
                BookingDate = appointment.CreatedAt;
                
                // Populate forms information based on patient age and appointment-specific forms
                AvailableForms.Clear();
                CompletedForms.Clear();
                
                // Determine which forms are appropriate based on patient age
                var patientAge = PatientAge;
                _logger.LogInformation("Patient age for form determination: {PatientAge}", patientAge);
                
                // Only show forms that are appropriate for the patient's age
                if (patientAge >= 20)
                {
                    // NCD Risk Assessment is appropriate for patients 20+
                if (HasNCDAssessment)
                    CompletedForms.Add("NCD Risk Assessment");
                else
                    AvailableForms.Add("NCD Risk Assessment");
                }
                else if (patientAge >= 10 && patientAge <= 19)
                {
                    // HEEADSSS Assessment is appropriate for patients 10-19
                if (HasHEEADSSSAssessment)
                    CompletedForms.Add("HEEADSSS Assessment");
                else
                    AvailableForms.Add("HEEADSSS Assessment");
                }
                else if (patientAge < 10)
                {
                    // Adolescent Health Information might be appropriate for younger patients
                if (HasAdolescentHealthInfo)
                    CompletedForms.Add("Adolescent Health Information");
                else
                    AvailableForms.Add("Adolescent Health Information");
                }
                
                _logger.LogInformation("Step 8 Complete: Booking and forms information loaded");
                _logger.LogInformation("Booked by: {BookedBy}, Booking date: {BookingDate}", BookedBy, BookingDate);
                _logger.LogInformation("Completed forms: {CompletedCount}, Available forms: {AvailableCount}", 
                    CompletedForms.Count, AvailableForms.Count);

                // Step 9: Complete
                _logger.LogInformation("Step 9: All processing complete, returning page");
                _logger.LogInformation("=== Successfully completed appointment details loading for ID: {Id} ===", id);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading appointment details for ID: {Id}. Exception message: {Message}. Stack trace: {StackTrace}", 
                    id, ex.Message, ex.StackTrace);
                StatusMessage = $"Error loading appointment details: {ex.Message}. Please try again later.";
                return RedirectToPage("/Nurse/Appointments");
            }
        }
    }
} 