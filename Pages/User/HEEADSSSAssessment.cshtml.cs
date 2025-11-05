using System;
using System.Threading.Tasks;
using System.Linq;
using Barangay.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Barangay.Data;
using Barangay.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Barangay.Extensions;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;

namespace Barangay.Pages.User
{
    [Authorize]
    public class HEEADSSSAssessmentModel : PageModel
    {
        private readonly EncryptedDbContext _context;
        private readonly ILogger<HEEADSSSAssessmentModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDataEncryptionService _encryptionService;
        private readonly IAuditTrailService _auditTrail;
        private readonly INotificationService _notificationService;
        private readonly IFamilyNumberService _familyNumberService;
        private readonly IDynamicFormService _dynamicFormService;

        public HEEADSSSAssessmentModel(
            EncryptedDbContext context,
            ILogger<HEEADSSSAssessmentModel> logger,
            UserManager<ApplicationUser> userManager,
            IDataEncryptionService encryptionService,
            IAuditTrailService auditTrail,
            INotificationService notificationService,
            IFamilyNumberService familyNumberService,
            IDynamicFormService dynamicFormService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _encryptionService = encryptionService;
            _auditTrail = auditTrail;
            _notificationService = notificationService;
            _familyNumberService = familyNumberService;
            _dynamicFormService = dynamicFormService;
        }

        [BindProperty]
        public HEEADSSSAssessmentViewModel Assessment { get; set; }

        [BindProperty]
        public string HealthFacility { get; set; } = "Barangay Health Center";

        // FamilyNo is NOT a BindProperty - it's set in OnGetAsync like NCDRiskAssessment
        public string FamilyNo { get; set; }
        public bool FamilyNoPreexisting { get; set; }

        [BindProperty]
        public int? AppointmentId { get; set; }

        [BindProperty]
        public string UserId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Check if appointmentId is provided in the query string
            string? appointmentIdStr = Request.Query["appointmentId"];
            
            // Check if dynamic HEEADSSS form exists first (admin-editable form)
            // Try multiple possible form keys for flexibility
            FormTemplate? dynamicHEEADSSSForm = null;
            var possibleFormKeys = new[] { "heeadsss-assessment", "heeadsss", "heeadsss-health-assessment" };
            
            foreach (var formKey in possibleFormKeys)
            {
                dynamicHEEADSSSForm = await _dynamicFormService.GetFormByKeyAsync(formKey);
                if (dynamicHEEADSSSForm != null && dynamicHEEADSSSForm.IsActive)
                {
                    _logger.LogInformation("Dynamic HEEADSSS form found with key '{FormKey}', redirecting to dynamic form", formKey);
                    break;
                }
            }
            
            // Also check for active forms in the Assessment category with MinAge 10-19
            if (dynamicHEEADSSSForm == null)
            {
                var ageRestrictedForms = await _context.FormTemplates
                    .Where(f => f.IsActive && 
                                f.Category == "Assessment" && 
                                f.MinAge.HasValue && 
                                f.MinAge >= 10 && 
                                f.MinAge <= 19 &&
                                (!f.MaxAge.HasValue || f.MaxAge >= 19) &&
                                f.ShowInAppointmentFlow)
                    .OrderByDescending(f => f.DisplayOrder)
                    .FirstOrDefaultAsync();
                
                if (ageRestrictedForms != null)
                {
                    dynamicHEEADSSSForm = ageRestrictedForms;
                    _logger.LogInformation("Found dynamic HEEADSSS form by age restriction (MinAge 10-19): {FormKey}", dynamicHEEADSSSForm.FormKey);
                }
            }
            
            if (dynamicHEEADSSSForm != null && dynamicHEEADSSSForm.IsActive)
            {
                _logger.LogInformation("Redirecting to dynamic HEEADSSS form: {FormKey}", dynamicHEEADSSSForm.FormKey);
                // Redirect to dynamic form instead of hard-coded form
                if (!string.IsNullOrEmpty(appointmentIdStr))
                {
                    return Redirect($"/Forms/SubmitForm/{dynamicHEEADSSSForm.FormKey}?appointmentId={appointmentIdStr}");
                }
                return Redirect($"/Forms/SubmitForm/{dynamicHEEADSSSForm.FormKey}");
            }
            
            // If no dynamic form exists, use hard-coded form (backward compatibility)
            _logger.LogInformation("No dynamic HEEADSSS form found, using hard-coded form");
            
            if (string.IsNullOrEmpty(appointmentIdStr))
            {
                TempData["StatusMessage"] = "Error: Assessment not found. Please access this form through your appointment details.";
                return RedirectToPage("/User/Appointments");
            }

            // Decrypt user data for authorized users
            user = user.DecryptSensitiveData(_encryptionService, User);
            
            // Manually decrypt PhoneNumber since it's not marked with [Encrypted] attribute
            if (!string.IsNullOrEmpty(user.PhoneNumber) && _encryptionService.IsEncrypted(user.PhoneNumber))
            {
                user.PhoneNumber = user.PhoneNumber.DecryptForUser(_encryptionService, User);
            }

            // Set default values
            HealthFacility = "Baesa Health Center";
            UserId = user.Id;
            
            // Get or generate family number (like NCDRiskAssessment does)
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
            
            // Set ViewData for the view
            ViewData["HealthFacility"] = HealthFacility;
            ViewData["FamilyNo"] = FamilyNo;
            
            _logger.LogInformation("OnGetAsync - Setting default values: UserId={UserId}, HealthFacility={HealthFacility}, FamilyNo={FamilyNo}",
                UserId, HealthFacility, FamilyNo);
            
            // Parse appointmentId (already validated above)
            if (int.TryParse(appointmentIdStr, out int appointmentId))
            {
                AppointmentId = appointmentId;
                _logger.LogInformation("OnGetAsync - AppointmentId set from query string: {AppointmentId}", AppointmentId);
                
                // Get appointment details to use patient information (include Patient for dependent bookings)
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);
                
                if (appointment != null)
                {
                    // Calculate age from DateOfBirth if available, otherwise use AgeValue
                    int calculatedAge = 19;
                    if (appointment.DateOfBirth.HasValue)
                    {
                        var today = DateTime.Today;
                        calculatedAge = today.Year - appointment.DateOfBirth.Value.Year;
                        if (appointment.DateOfBirth.Value.Date > today.AddYears(-calculatedAge))
                            calculatedAge--;
                    }
                    else if (appointment.AgeValue > 0)
                    {
                        calculatedAge = appointment.AgeValue;
                    }
                    
                    // Use appointment patient information - prioritize dependent name if available
                    string correctPatientName = !string.IsNullOrEmpty(appointment.DependentFullName) 
                        ? appointment.DependentFullName 
                        : appointment.PatientName ?? user.FullName;
                    
                    // CRITICAL: Use correct UserId for dependent bookings
                    string correctPatientUserId = user.Id; // Default to logged-in user
                    if (appointment.Patient != null && !string.IsNullOrEmpty(appointment.Patient.UserId))
                    {
                        correctPatientUserId = appointment.Patient.UserId;
                        _logger.LogInformation("OnGetAsync: Using Patient UserId from appointment: {UserId} for patient: {PatientName}", 
                            correctPatientUserId, correctPatientName);
                    }
                    
                    ViewData["PatientName"] = correctPatientName;
                    ViewData["PatientUserId"] = correctPatientUserId;
                    ViewData["PatientAge"] = calculatedAge;
                    ViewData["PatientPhone"] = appointment.ContactNumber ?? user.PhoneNumber ?? string.Empty;
                    ViewData["PatientBirthdate"] = appointment.DateOfBirth?.ToString("yyyy-MM-dd") ?? DateTime.Today.AddYears(-calculatedAge).ToString("yyyy-MM-dd");
                    
                    // Store appointment context data for display in the view
                    var contextDisplayName = !string.IsNullOrEmpty(appointment.DependentFullName) ? appointment.DependentFullName : appointment.PatientName;
                    var contextDisplayAge = appointment.DependentAge ?? appointment.AgeValue;
                    
                    ViewData["AppointmentContext_DisplayName"] = contextDisplayName;
                    ViewData["AppointmentContext_DisplayAge"] = contextDisplayAge;
                    ViewData["AppointmentContext_BookedBy"] = appointment.PatientName;
                    ViewData["AppointmentContext_AppointmentDate"] = appointment.AppointmentDate.ToString("MMM dd, yyyy");
                    ViewData["AppointmentContext_FamilyNumber"] = appointment.FamilyNumber;
                    ViewData["AppointmentContext_BookingForOther"] = appointment.BookingForOther;
                    ViewData["AppointmentContext_Relationship"] = appointment.Relationship;
                    
                    _logger.LogInformation("=== APPOINTMENT CONTEXT DATA ===");
                    _logger.LogInformation("BookingForOther: {BookingForOther}", appointment.BookingForOther);
                    _logger.LogInformation("PatientName (Booker): {PatientName}", appointment.PatientName);
                    _logger.LogInformation("DependentFullName: {DependentFullName}", appointment.DependentFullName ?? "NULL");
                    _logger.LogInformation("DependentAge: {DependentAge}", appointment.DependentAge?.ToString() ?? "NULL");
                    _logger.LogInformation("AgeValue: {AgeValue}", appointment.AgeValue);
                    _logger.LogInformation("Context Display Name: {DisplayName}", contextDisplayName);
                    _logger.LogInformation("Context Display Age: {DisplayAge}", contextDisplayAge);
                    _logger.LogInformation("FamilyNumber: {FamilyNumber}", appointment.FamilyNumber ?? "NULL");
                    _logger.LogInformation("Relationship: {Relationship}", appointment.Relationship ?? "NULL");
                    
                    _logger.LogInformation($"Using appointment patient info: Name={appointment.PatientName}, CalculatedAge={calculatedAge}, AgeValue={appointment.AgeValue}, DateOfBirth={appointment.DateOfBirth}, Phone={appointment.ContactNumber}");
                    
                    // DEBUGGING: Check if there's an existing HEEADSSS assessment for this appointment
                    _logger.LogInformation("=== CHECKING FOR EXISTING HEEADSSS ASSESSMENT ===");
                    var existingAssessmentData = await _context.HEEADSSSAssessments
                        .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId.ToString());
                    
                    if (existingAssessmentData != null)
                    {
                        _logger.LogInformation("Found existing HEEADSSS assessment with ID: {AssessmentId}", existingAssessmentData.Id);
                        
                        // Decrypt the existing assessment data
                        try
                        {
                            existingAssessmentData.DecryptSensitiveData(_encryptionService, User);
                            _logger.LogInformation("Successfully decrypted existing assessment data");
                            
                            // DEBUGGING: Log the decrypted checkbox and radio button values
                            _logger.LogInformation("=== EXISTING ASSESSMENT DATA (DECRYPTED) ===");
                            _logger.LogInformation("DRUGS Section:");
                            _logger.LogInformation("  DrugsTobaccoUse: '{DrugsTobaccoUse}'", existingAssessmentData.DrugsTobaccoUse);
                            _logger.LogInformation("  DrugsAlcoholUse: '{DrugsAlcoholUse}'", existingAssessmentData.DrugsAlcoholUse);
                            _logger.LogInformation("  DrugsStreetDrugs: '{DrugsStreetDrugs}'", existingAssessmentData.DrugsStreetDrugs);
                            
                            _logger.LogInformation("SEXUALITY Section:");
                            _logger.LogInformation("  SexualityIntimateRelationships: '{SexualityIntimateRelationships}'", existingAssessmentData.SexualityIntimateRelationships);
                            _logger.LogInformation("  SexualityPregnancyExperience: '{SexualityPregnancyExperience}'", existingAssessmentData.SexualityPregnancyExperience);
                            _logger.LogInformation("  SexualitySTIExperience: '{SexualitySTIExperience}'", existingAssessmentData.SexualitySTIExperience);
                            _logger.LogInformation("  SexualityProtectionUse: '{SexualityProtectionUse}'", existingAssessmentData.SexualityProtectionUse);
                            
                            _logger.LogInformation("SEXUALITY Checkboxes:");
                            _logger.LogInformation("  SexualityGay: '{SexualityGay}'", existingAssessmentData.SexualityGay);
                            _logger.LogInformation("  SexualityLesbian: '{SexualityLesbian}'", existingAssessmentData.SexualityLesbian);
                            _logger.LogInformation("  SexualityBisexual: '{SexualityBisexual}'", existingAssessmentData.SexualityBisexual);
                            
                            // Store the existing assessment for use in the form
                            ViewData["ExistingAssessment"] = existingAssessmentData;
                            _logger.LogInformation("Stored existing assessment in ViewData for form binding");
                        }
                        catch (Exception decryptEx)
                        {
                            _logger.LogError(decryptEx, "Failed to decrypt existing assessment data");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("No existing HEEADSSS assessment found for appointment ID: {AppointmentId}", appointmentId);
                    }
                }
                else
                {
                    // Fallback to logged-in user info
                    DateTime birthDate = DateTime.Today.AddYears(-19);
                    int age = 19;
                    ViewData["PatientName"] = user.FullName;
                    ViewData["PatientUserId"] = user.Id; // Fallback to logged-in user
                    ViewData["PatientAge"] = age;
                    ViewData["PatientPhone"] = user.PhoneNumber ?? string.Empty;
                    ViewData["PatientBirthdate"] = birthDate.ToString("yyyy-MM-dd");
                    
                    _logger.LogInformation($"Appointment not found, using logged-in user info: Name={user.FullName}, Age={age}");
                }
            }
            else
            {
                // This should not happen since we validate appointmentId above
                TempData["StatusMessage"] = "Error: Invalid appointment ID. Please access this form through your appointment details.";
                return RedirectToPage("/User/Appointments");
            }

            // Initialize Assessment if not already initialized
            if (Assessment == null)
            {
                Assessment = new HEEADSSSAssessmentViewModel();
            }
            
            // Check if we have an existing assessment to populate the form
            var existingAssessmentFromViewData = ViewData["ExistingAssessment"] as HEEADSSSAssessment;
            if (existingAssessmentFromViewData != null)
            {
                _logger.LogInformation("=== POPULATING FORM WITH EXISTING ASSESSMENT DATA ===");
                
                // Map existing assessment data to the view model
                Assessment.UserId = existingAssessmentFromViewData.UserId ?? user.Id;
                Assessment.HealthFacility = existingAssessmentFromViewData.HealthFacility ?? HealthFacility;
                Assessment.FamilyNo = existingAssessmentFromViewData.FamilyNo ?? FamilyNo;
                Assessment.AppointmentId = existingAssessmentFromViewData.AppointmentId != null ? int.Parse(existingAssessmentFromViewData.AppointmentId) : AppointmentId;
                Assessment.Age = existingAssessmentFromViewData.Age ?? (ViewData["PatientAge"] as int? ?? 19).ToString();
                Assessment.Birthday = existingAssessmentFromViewData.Birthday ?? DateTime.Parse(ViewData["PatientBirthdate"] as string ?? DateTime.Today.AddYears(-19).ToString("yyyy-MM-dd"));
                Assessment.FullName = existingAssessmentFromViewData.FullName ?? ViewData["PatientName"] as string ?? user.FullName;
                Assessment.Gender = existingAssessmentFromViewData.Gender;
                Assessment.Address = existingAssessmentFromViewData.Address;
                Assessment.ContactNumber = existingAssessmentFromViewData.ContactNumber;
                Assessment.ReferredBy = existingAssessmentFromViewData.ReferredBy;
                
                // Map all the HEEADSSS fields
                Assessment.HomeFamilyProblems = existingAssessmentFromViewData.HomeFamilyProblems;
                Assessment.HomeParentalListening = existingAssessmentFromViewData.HomeParentalListening;
                Assessment.HomeRunawayThoughts = existingAssessmentFromViewData.HomeRunawayThoughts;
                Assessment.HomeParentalBlame = existingAssessmentFromViewData.HomeParentalBlame;
                Assessment.HomeFamilyChanges = existingAssessmentFromViewData.HomeFamilyChanges;
                
                Assessment.EducationCurrentlyStudying = existingAssessmentFromViewData.EducationCurrentlyStudying;
                Assessment.EducationWorking = existingAssessmentFromViewData.EducationWorking;
                Assessment.EducationSchoolWorkProblems = existingAssessmentFromViewData.EducationSchoolWorkProblems;
                Assessment.EducationBullyingExperience = existingAssessmentFromViewData.EducationBullyingExperience;
                Assessment.EducationBullying = existingAssessmentFromViewData.EducationBullying;
                
                Assessment.EatingBodyImageSatisfaction = existingAssessmentFromViewData.EatingBodyImageSatisfaction;
                Assessment.EatingDisorderedEatingBehaviors = existingAssessmentFromViewData.EatingDisorderedEatingBehaviors;
                Assessment.EatingWeightComments = existingAssessmentFromViewData.EatingWeightComments;
                
                Assessment.ActivitiesParticipation = existingAssessmentFromViewData.ActivitiesParticipation;
                Assessment.ActivitiesRegularExercise = existingAssessmentFromViewData.ActivitiesRegularExercise;
                Assessment.ActivitiesInternetGadgetUse = existingAssessmentFromViewData.ActivitiesInternetGadgetUse;
                Assessment.ActivitiesScreenTime = existingAssessmentFromViewData.ActivitiesScreenTime;
                
                // Map checkbox and radio button values
                Assessment.DrugsTobaccoUse = existingAssessmentFromViewData.DrugsTobaccoUse;
                Assessment.DrugsAlcoholUse = existingAssessmentFromViewData.DrugsAlcoholUse;
                Assessment.DrugsStreetDrugs = existingAssessmentFromViewData.DrugsStreetDrugs;
                
                Assessment.SexualityHealthConcerns = existingAssessmentFromViewData.SexualityHealthConcerns;
                Assessment.SexualityIntimateRelationships = existingAssessmentFromViewData.SexualityIntimateRelationships;
                Assessment.SexualityPartnersCount = existingAssessmentFromViewData.SexualityPartnersCount;
                Assessment.SexualityPregnancyExperience = existingAssessmentFromViewData.SexualityPregnancyExperience;
                Assessment.SexualitySTIExperience = existingAssessmentFromViewData.SexualitySTIExperience;
                Assessment.SexualityProtectionUse = existingAssessmentFromViewData.SexualityProtectionUse;
                
                Assessment.SexualityGay = existingAssessmentFromViewData.SexualityGay;
                Assessment.SexualityLesbian = existingAssessmentFromViewData.SexualityLesbian;
                Assessment.SexualityBisexual = existingAssessmentFromViewData.SexualityBisexual;
                
                Assessment.SafetyPhysicalAbuse = existingAssessmentFromViewData.SafetyPhysicalAbuse;
                Assessment.SafetyRelationshipViolence = existingAssessmentFromViewData.SafetyRelationshipViolence;
                Assessment.SafetyProtectiveGear = existingAssessmentFromViewData.SafetyProtectiveGear;
                Assessment.SafetyWeaponAccess = existingAssessmentFromViewData.SafetyWeaponAccess;
                Assessment.SafetyGunsAtHome = existingAssessmentFromViewData.SafetyGunsAtHome;
                
                Assessment.SuicideDepressionFeelings = existingAssessmentFromViewData.SuicideDepressionFeelings;
                Assessment.SuicideSelfHarmThoughts = existingAssessmentFromViewData.SuicideSelfHarmThoughts;
                Assessment.SuicideFamilyHistory = existingAssessmentFromViewData.SuicideFamilyHistory;
                
                Assessment.Notes = existingAssessmentFromViewData.Notes;
                Assessment.AssessedBy = existingAssessmentFromViewData.AssessedBy;
                
                // Map new fields from Nurse form
                Assessment.Height = existingAssessmentFromViewData.Height;
                Assessment.Weight = existingAssessmentFromViewData.Weight;
                Assessment.BMI = existingAssessmentFromViewData.BMI;
                Assessment.BMIUnderweight = existingAssessmentFromViewData.BMIUnderweight?.ToString() == "True" ? "True" : "False";
                Assessment.BMINormal = existingAssessmentFromViewData.BMINormal?.ToString() == "True" ? "True" : "False";
                Assessment.BMIOverweight = existingAssessmentFromViewData.BMIOverweight?.ToString() == "True" ? "True" : "False";
                Assessment.BMIObese = existingAssessmentFromViewData.BMIObese?.ToString() == "True" ? "True" : "False";
                
                Assessment.ImmunizationMR = existingAssessmentFromViewData.ImmunizationMR;
                Assessment.ImmunizationTd = existingAssessmentFromViewData.ImmunizationTd;
                Assessment.ImmunizationHPV = existingAssessmentFromViewData.ImmunizationHPV;
                
                Assessment.DateOfMenarche = existingAssessmentFromViewData.DateOfMenarche;
                Assessment.AgeOfFirstPregnancy = existingAssessmentFromViewData.AgeOfFirstPregnancy;
                Assessment.OBScore = existingAssessmentFromViewData.OBScore;
                
                Assessment.VitalTemp = existingAssessmentFromViewData.VitalTemp;
                Assessment.VitalRR = existingAssessmentFromViewData.VitalRR;
                Assessment.VitalPR = existingAssessmentFromViewData.VitalPR;
                Assessment.VitalBP = existingAssessmentFromViewData.VitalBP;
                
                Assessment.ChiefComplaint = existingAssessmentFromViewData.ChiefComplaint;
                Assessment.HistoryOfPresentIllness = existingAssessmentFromViewData.HistoryOfPresentIllness;
                Assessment.PhysicalExaminationFindings = existingAssessmentFromViewData.PhysicalExaminationFindings;
                Assessment.PastMedicalHistory = existingAssessmentFromViewData.PastMedicalHistory;
                Assessment.WorkingDiagnosis = existingAssessmentFromViewData.WorkingDiagnosis;
                Assessment.Management = existingAssessmentFromViewData.Management;
                Assessment.FamilyHistory = existingAssessmentFromViewData.FamilyHistory;
                
                Assessment.ReferredTo = existingAssessmentFromViewData.ReferredTo;
                Assessment.ReasonForReferral = existingAssessmentFromViewData.ReasonForReferral;
                Assessment.FollowUpDate = existingAssessmentFromViewData.FollowUpDate;
                
                // Map eating habits fields
                if (!string.IsNullOrEmpty(existingAssessmentFromViewData.EatingVomiting) && _encryptionService.IsEncrypted(existingAssessmentFromViewData.EatingVomiting))
                {
                    existingAssessmentFromViewData.EatingVomiting = _encryptionService.DecryptForUser(existingAssessmentFromViewData.EatingVomiting, User);
                }
                if (!string.IsNullOrEmpty(existingAssessmentFromViewData.EatingDietPills) && _encryptionService.IsEncrypted(existingAssessmentFromViewData.EatingDietPills))
                {
                    existingAssessmentFromViewData.EatingDietPills = _encryptionService.DecryptForUser(existingAssessmentFromViewData.EatingDietPills, User);
                }
                if (!string.IsNullOrEmpty(existingAssessmentFromViewData.EatingLaxatives) && _encryptionService.IsEncrypted(existingAssessmentFromViewData.EatingLaxatives))
                {
                    existingAssessmentFromViewData.EatingLaxatives = _encryptionService.DecryptForUser(existingAssessmentFromViewData.EatingLaxatives, User);
                }
                if (!string.IsNullOrEmpty(existingAssessmentFromViewData.EatingStarvation) && _encryptionService.IsEncrypted(existingAssessmentFromViewData.EatingStarvation))
                {
                    existingAssessmentFromViewData.EatingStarvation = _encryptionService.DecryptForUser(existingAssessmentFromViewData.EatingStarvation, User);
                }
                Assessment.EatingVomiting = existingAssessmentFromViewData.EatingVomiting;
                Assessment.EatingDietPills = existingAssessmentFromViewData.EatingDietPills;
                Assessment.EatingLaxatives = existingAssessmentFromViewData.EatingLaxatives;
                Assessment.EatingStarvation = existingAssessmentFromViewData.EatingStarvation;
                
                Assessment.SexualityHarassment = existingAssessmentFromViewData.SexualityHarassment;
                
                _logger.LogInformation("Successfully populated form with existing assessment data");
                _logger.LogInformation("DRUGS values - Tobacco: '{Tobacco}', Alcohol: '{Alcohol}', Street: '{Street}'", 
                    Assessment.DrugsTobaccoUse, Assessment.DrugsAlcoholUse, Assessment.DrugsStreetDrugs);
                _logger.LogInformation("SEXUALITY values - Intimate: '{Intimate}', Pregnancy: '{Pregnancy}', STI: '{STI}', Protection: '{Protection}'", 
                    Assessment.SexualityIntimateRelationships, Assessment.SexualityPregnancyExperience, Assessment.SexualitySTIExperience, Assessment.SexualityProtectionUse);
                _logger.LogInformation("SEXUALITY checkboxes - Gay: '{Gay}', Lesbian: '{Lesbian}', Bisexual: '{Bisexual}'", 
                    Assessment.SexualityGay, Assessment.SexualityLesbian, Assessment.SexualityBisexual);
            }
            else
            {
                // Always set these required fields for new assessments
                // Use correct UserId (from appointment Patient if dependent booking, otherwise logged-in user)
                Assessment.UserId = ViewData["PatientUserId"] as string ?? user.Id;
                Assessment.HealthFacility = HealthFacility;
                // Use the generated/retrieved FamilyNo from GetOrGenerateFamilyNumberAsync
                Assessment.FamilyNo = FamilyNo;
                Assessment.AppointmentId = AppointmentId;
                Assessment.Age = (ViewData["PatientAge"] as int? ?? 19).ToString(); // Use patient age from appointment
                Assessment.Birthday = DateTime.Parse(ViewData["PatientBirthdate"] as string ?? DateTime.Today.AddYears(-19).ToString("yyyy-MM-dd")); // Use patient birthdate from appointment
                Assessment.FullName = ViewData["PatientName"] as string ?? user.FullName; // Use patient name from appointment
                
                _logger.LogInformation("No existing assessment found, initialized with FamilyNo: {FamilyNo}, UserId: {UserId}, PatientName: {PatientName}", 
                    Assessment.FamilyNo, Assessment.UserId, Assessment.FullName);
            }
            
            _logger.LogInformation("OnGetAsync - Assessment initialized with UserId={UserId}, HealthFacility={HealthFacility}, FamilyNo={FamilyNo}, AppointmentId={AppointmentId}, Age={Age}",
                Assessment.UserId, Assessment.HealthFacility, Assessment.FamilyNo, Assessment.AppointmentId, Assessment.Age);

            return Page();
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

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                _logger.LogInformation("=== HEEADSSS ASSESSMENT SUBMISSION DEBUGGING STARTED ===");
                _logger.LogInformation("Request method: {Method}, Content-Type: {ContentType}", Request.Method, Request.ContentType);
                
                // Log all form values to help diagnose issues
                _logger.LogInformation("=== ALL FORM VALUES RECEIVED ===");
                foreach (var key in Request.Form.Keys)
                {
                    _logger.LogInformation("- {Key}: {Value}", key, Request.Form[key].ToString());
                }
                
                // DEBUGGING: Log specific HEEADSSS sections
                _logger.LogInformation("=== HEEADSSS SECTION-SPECIFIC DEBUGGING ===");
                
                // HOME section debugging
                _logger.LogInformation("HOME Section:");
                _logger.LogInformation("  HomeFamilyProblems: '{HomeFamilyProblems}'", Request.Form["Assessment.HomeFamilyProblems"].ToString());
                _logger.LogInformation("  HomeParentalListening: '{HomeParentalListening}'", Request.Form["Assessment.HomeParentalListening"].ToString());
                _logger.LogInformation("  HomeRunawayThoughts: '{HomeRunawayThoughts}'", Request.Form["Assessment.HomeRunawayThoughts"].ToString());
                _logger.LogInformation("  HomeFamilyChanges: '{HomeFamilyChanges}'", Request.Form["Assessment.HomeFamilyChanges"].ToString());
                
                // EDUCATION section debugging
                _logger.LogInformation("EDUCATION Section:");
                _logger.LogInformation("  EducationCurrentlyStudying: '{EducationCurrentlyStudying}'", Request.Form["Assessment.EducationCurrentlyStudying"].ToString());
                _logger.LogInformation("  EducationWorking: '{EducationWorking}'", Request.Form["Assessment.EducationWorking"].ToString());
                _logger.LogInformation("  EducationSchoolWorkProblems: '{EducationSchoolWorkProblems}'", Request.Form["Assessment.EducationSchoolWorkProblems"].ToString());
                _logger.LogInformation("  EducationBullyingExperience: '{EducationBullyingExperience}'", Request.Form["Assessment.EducationBullyingExperience"].ToString());
                
                // EATING HABITS section debugging
                _logger.LogInformation("EATING HABITS Section:");
                _logger.LogInformation("  EatingBodyImageSatisfaction: '{EatingBodyImageSatisfaction}'", Request.Form["Assessment.EatingBodyImageSatisfaction"].ToString());
                _logger.LogInformation("  EatingDisorderedEatingBehaviors: '{EatingDisorderedEatingBehaviors}'", Request.Form["Assessment.EatingDisorderedEatingBehaviors"].ToString());
                _logger.LogInformation("  EatingWeightComments: '{EatingWeightComments}'", Request.Form["Assessment.EatingWeightComments"].ToString());
                
                // ACTIVITIES section debugging
                _logger.LogInformation("ACTIVITIES Section:");
                _logger.LogInformation("  ActivitiesParticipation: '{ActivitiesParticipation}'", Request.Form["Assessment.ActivitiesParticipation"].ToString());
                _logger.LogInformation("  ActivitiesRegularExercise: '{ActivitiesRegularExercise}'", Request.Form["Assessment.ActivitiesRegularExercise"].ToString());
                _logger.LogInformation("  ActivitiesInternetGadgetUse: '{ActivitiesInternetGadgetUse}'", Request.Form["Assessment.ActivitiesInternetGadgetUse"].ToString());
                
                // DRUGS section debugging (checkboxes)
                _logger.LogInformation("DRUGS Section (Checkboxes):");
                _logger.LogInformation("  DrugsTobaccoUse: '{DrugsTobaccoUse}'", Request.Form["Assessment.DrugsTobaccoUse"].ToString());
                _logger.LogInformation("  DrugsAlcoholUse: '{DrugsAlcoholUse}'", Request.Form["Assessment.DrugsAlcoholUse"].ToString());
                _logger.LogInformation("  DrugsStreetDrugs: '{DrugsStreetDrugs}'", Request.Form["Assessment.DrugsStreetDrugs"].ToString());
                
                // SEXUALITY section debugging
                _logger.LogInformation("SEXUALITY Section:");
                _logger.LogInformation("  SexualityHealthConcerns: '{SexualityHealthConcerns}'", Request.Form["Assessment.SexualityHealthConcerns"].ToString());
                _logger.LogInformation("  SexualityPartnersCount: '{SexualityPartnersCount}'", Request.Form["Assessment.SexualityPartnersCount"].ToString());
                
                // SEXUALITY radio buttons debugging
                _logger.LogInformation("SEXUALITY Radio Buttons:");
                _logger.LogInformation("  SexualityIntimateRelationships: '{SexualityIntimateRelationships}'", Request.Form["SexualityIntimateRelationships"].ToString());
                _logger.LogInformation("  SexualityPregnancyExperience: '{SexualityPregnancyExperience}'", Request.Form["SexualityPregnancyExperience"].ToString());
                _logger.LogInformation("  SexualitySTIExperience: '{SexualitySTIExperience}'", Request.Form["SexualitySTIExperience"].ToString());
                _logger.LogInformation("  SexualityProtectionUse: '{SexualityProtectionUse}'", Request.Form["SexualityProtectionUse"].ToString());
                
                // SEXUALITY checkboxes debugging
                _logger.LogInformation("SEXUALITY Checkboxes:");
                _logger.LogInformation("  SexualityGay: '{SexualityGay}'", Request.Form["Assessment.SexualityGay"].ToString());
                _logger.LogInformation("  SexualityLesbian: '{SexualityLesbian}'", Request.Form["Assessment.SexualityLesbian"].ToString());
                _logger.LogInformation("  SexualityBisexual: '{SexualityBisexual}'", Request.Form["Assessment.SexualityBisexual"].ToString());
                
                // SAFETY section debugging
                _logger.LogInformation("SAFETY Section:");
                _logger.LogInformation("  SafetyPhysicalAbuse: '{SafetyPhysicalAbuse}'", Request.Form["Assessment.SafetyPhysicalAbuse"].ToString());
                _logger.LogInformation("  SafetyRelationshipViolence: '{SafetyRelationshipViolence}'", Request.Form["Assessment.SafetyRelationshipViolence"].ToString());
                _logger.LogInformation("  SafetyProtectiveGear: '{SafetyProtectiveGear}'", Request.Form["Assessment.SafetyProtectiveGear"].ToString());
                _logger.LogInformation("  SafetyWeaponAccess: '{SafetyWeaponAccess}'", Request.Form["Assessment.SafetyWeaponAccess"].ToString());
                
                // SUICIDE/DEPRESSION section debugging
                _logger.LogInformation("SUICIDE/DEPRESSION Section:");
                _logger.LogInformation("  SuicideDepressionFeelings: '{SuicideDepressionFeelings}'", Request.Form["Assessment.SuicideDepressionFeelings"].ToString());
                _logger.LogInformation("  SuicideSelfHarmThoughts: '{SuicideSelfHarmThoughts}'", Request.Form["Assessment.SuicideSelfHarmThoughts"].ToString());
                _logger.LogInformation("  SuicideFamilyHistory: '{SuicideFamilyHistory}'", Request.Form["Assessment.SuicideFamilyHistory"].ToString());
                
                // Notes and Assessment fields debugging
                _logger.LogInformation("Notes and Assessment Fields:");
                _logger.LogInformation("  Notes: '{Notes}'", Request.Form["Assessment.Notes"].ToString());
                _logger.LogInformation("  AssessedBy: '{AssessedBy}'", Request.Form["Assessment.AssessedBy"].ToString());
                
                // Log specific important fields
                _logger.LogInformation("Key form fields - UserId: '{UserId}', HealthFacility: '{HealthFacility}', FamilyNo: '{FamilyNo}', AppointmentId: '{AppointmentId}', FullName: '{FullName}'",
                    Request.Form["Assessment.UserId"].ToString(),
                    Request.Form["Assessment.HealthFacility"].ToString(),
                    Request.Form["Assessment.FamilyNo"].ToString(),
                    Request.Form["Assessment.AppointmentId"].ToString(),
                    Request.Form["Assessment.FullName"].ToString());
                
                // Get current user
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User not found during HEEADSSS assessment submission");
                    return new JsonResult(new { success = false, error = "User not found" });
                }

                // Get required fields from form (all fields are under Assessment namespace in HTML)
                string userId = Request.Form["Assessment.UserId"].ToString();
                string healthFacility = Request.Form["Assessment.HealthFacility"].ToString();
                
                // DEBUG: Log all form keys to see what's available
                _logger.LogInformation("=== ALL FORM KEYS ===");
                foreach (var key in Request.Form.Keys)
                {
                    _logger.LogInformation("Form key: '{Key}' = '{Value}'", key, Request.Form[key]);
                }
                
                string familyNo = Request.Form["Assessment.FamilyNo"].ToString();
                _logger.LogInformation("READ FamilyNo from form: '{FamilyNo}'", familyNo);
                
                // Get appointmentId if provided
                string? appointmentId = null;
                var appointmentIdFormValue = Request.Form["Assessment.AppointmentId"].ToString();
                _logger.LogInformation("Received AppointmentId from form: '{AppointmentIdFormValue}'", appointmentIdFormValue);
                
                if (!string.IsNullOrEmpty(appointmentIdFormValue))
                {
                    appointmentId = appointmentIdFormValue;
                    _logger.LogInformation("Successfully set AppointmentId: {AppointmentId}", appointmentId);
                }
                else
                {
                    _logger.LogWarning("AppointmentId is empty from form value: '{AppointmentIdFormValue}'", appointmentIdFormValue);
                }
                
                // Get patient data from appointment if available
                string patientName = user.FullName ?? "Unknown";
                DateTime birthday = user.BirthDate ?? DateTime.MinValue;
                string patientGender = user.Gender ?? "Not specified";
                string patientAddress = user.Address ?? "Not specified";
                string patientPhone = user.PhoneNumber ?? "Not specified";
                
                if (!string.IsNullOrEmpty(appointmentId) && int.TryParse(appointmentId, out int parsedAppointmentId))
                {
                    var appointment = await _context.Appointments
                        .Include(a => a.Patient)
                        .FirstOrDefaultAsync(a => a.Id == parsedAppointmentId);
                    
                    if (appointment != null)
                    {
                        // Prioritize dependent name if booking for someone else (same logic as OnGetAsync)
                        patientName = !string.IsNullOrEmpty(appointment.DependentFullName)
                            ? appointment.DependentFullName
                            : appointment.PatientName ?? user.FullName ?? "Unknown";
                        
                        birthday = appointment.DateOfBirth ?? (user.BirthDate ?? DateTime.MinValue);
                        patientGender = appointment.Gender ?? user.Gender ?? "Not specified";
                        patientAddress = appointment.Address ?? user.Address ?? "Not specified";
                        patientPhone = appointment.ContactNumber ?? user.PhoneNumber ?? "Not specified";
                        
                        // CRITICAL: If booking for someone else, use the Patient's UserId, not logged-in user
                        if (appointment.Patient != null && !string.IsNullOrEmpty(appointment.Patient.UserId))
                        {
                            userId = appointment.Patient.UserId;
                            _logger.LogInformation("Using Patient UserId from appointment: {UserId} for patient: {PatientName}", userId, patientName);
                        }
                        
                        _logger.LogInformation($"Using appointment patient data: Name={patientName} (Dependent: {appointment.DependentFullName}), Birthday={birthday.ToShortDateString()}, Gender={patientGender}");
                    }
                    else
                    {
                        _logger.LogWarning("Appointment {AppointmentId} not found in database", appointmentId);
                    }
                }
                else
                {
                    _logger.LogWarning("No valid AppointmentId provided, using logged-in user data");
                }
                
                // Override with form data if provided
                if (DateTime.TryParse(Request.Form["Assessment.Birthday"].ToString(), out DateTime parsedBirthday))
                {
                    birthday = parsedBirthday;
                    _logger.LogInformation($"Using birthday from form: {birthday.ToShortDateString()}");
                }
                
                int age = CalculateAge(birthday);
                _logger.LogInformation($"Calculated age from birthday {birthday.ToShortDateString()}: {age} years");
                
                // Log the direct form values
                _logger.LogInformation("Form values - UserId: {UserId}, HealthFacility: {HealthFacility}, FamilyNo: {FamilyNo}, AppointmentId: {AppointmentId}, Age: {Age}",
                    userId, healthFacility, familyNo, appointmentId, age);

                // Use fallback values if needed
                if (string.IsNullOrEmpty(userId))
                {
                    userId = user.Id;
                    _logger.LogInformation("Using fallback UserId: {UserId}", userId);
                }
                
                if (string.IsNullOrEmpty(healthFacility))
                {
                    healthFacility = "Baesa Health Center";
                    _logger.LogInformation("Using fallback HealthFacility: {HealthFacility}", healthFacility);
                }
                
                // DO NOT set fallback for familyNo - it should be what user types or generates
                // Empty family number is acceptable
                if (string.IsNullOrEmpty(familyNo))
                {
                    _logger.LogWarning("FamilyNo is empty - user did not provide a family number");
                }

                // Note: Duplicate check removed temporarily due to encryption complexity
                // TODO: Implement proper duplicate check that handles encrypted data
                
                // Create assessment object
                var assessment = new HEEADSSSAssessment
                {
                    // Required fields
                    UserId = userId,
                    HealthFacility = healthFacility,
                    FamilyNo = familyNo,
                    AppointmentId = appointmentId,
                    
                    // PhilHealth and program fields
                    IsNHPTS = GetFormValueOrDefault("Assessment.IsNHPTS"),
                    Is4Ps = GetFormValueOrDefault("Assessment.Is4Ps"),
                    IsPhilHealthBeneficiaryOnly = GetFormValueOrDefault("Assessment.IsPhilHealthBeneficiaryOnly"),
                    IsOwnPhilHealth = GetFormValueOrDefault("Assessment.IsOwnPhilHealth"),
                    PhilHealthPIN = GetFormValueOrDefault("Assessment.PhilHealthPIN"),
                    
                    // Patient information (from appointment or form)
                    FullName = patientName,
                    Birthday = birthday,
                    Age = age.ToString(),
                    Gender = patientGender,
                    Address = patientAddress,
                    ContactNumber = patientPhone,
                    
                    // Medical measurements
                    Height = GetFormValueOrDefault("Assessment.Height"),
                    Weight = GetFormValueOrDefault("Assessment.Weight"),
                    BMI = GetFormValueOrDefault("Assessment.BMI"),
                    BMIUnderweight = GetFormBooleanValueOrDefault("Assessment.BMIUnderweight"),
                    BMINormal = GetFormBooleanValueOrDefault("Assessment.BMINormal"),
                    BMIOverweight = GetFormBooleanValueOrDefault("Assessment.BMIOverweight"),
                    BMIObese = GetFormBooleanValueOrDefault("Assessment.BMIObese"),
                    
                    // Immunization status
                    ImmunizationMR = GetFormValueOrDefault("Assessment.ImmunizationMR"),
                    ImmunizationTd = GetFormValueOrDefault("Assessment.ImmunizationTd"),
                    ImmunizationHPV = GetFormValueOrDefault("Assessment.ImmunizationHPV"),
                    
                    // Female-specific fields
                    DateOfMenarche = GetFormValueOrDefault("Assessment.DateOfMenarche"),
                    AgeOfFirstPregnancy = GetFormValueOrDefault("Assessment.AgeOfFirstPregnancy"),
                    OBScore = GetFormValueOrDefault("Assessment.OBScore"),
                    
                    // Vital signs
                    VitalTemp = GetFormValueOrDefault("Assessment.VitalTemp"),
                    VitalRR = GetFormValueOrDefault("Assessment.VitalRR"),
                    VitalPR = GetFormValueOrDefault("Assessment.VitalPR"),
                    VitalBP = GetFormValueOrDefault("Assessment.VitalBP"),
                    
                    // Medical information
                    ChiefComplaint = GetFormValueOrDefault("Assessment.ChiefComplaint"),
                    HistoryOfPresentIllness = GetFormValueOrDefault("Assessment.HistoryOfPresentIllness"),
                    PhysicalExaminationFindings = GetFormValueOrDefault("Assessment.PhysicalExaminationFindings"),
                    PastMedicalHistory = GetFormValueOrDefault("Assessment.PastMedicalHistory"),
                    WorkingDiagnosis = GetFormValueOrDefault("Assessment.WorkingDiagnosis"),
                    Management = GetFormValueOrDefault("Assessment.Management"),
                    FamilyHistory = GetFormValueOrDefault("Assessment.FamilyHistory"),
                    
                    // Referral information
                    ReferredBy = GetFormValueOrDefault("Assessment.ReferredBy"),
                    ReferredTo = GetFormValueOrDefault("Assessment.ReferredTo"),
                    ReasonForReferral = GetFormValueOrDefault("Assessment.ReasonForReferral"),
                    FollowUpDate = GetFormValueOrDefault("Assessment.FollowUpDate"),
                    
                    // Form fields - provide default values for non-nullable fields
                    HomeFamilyProblems = GetFormValueOrDefault("Assessment.HomeFamilyProblems"),
                    HomeParentalListening = GetFormValueOrDefault("Assessment.HomeParentalListening"),
                    HomeParentalBlame = GetFormValueOrDefault("Assessment.HomeParentalBlame"),
                    HomeFamilyChanges = GetFormValueOrDefault("Assessment.HomeFamilyChanges"),
                    HomeRunawayThoughts = GetFormValueOrDefault("Assessment.HomeRunawayThoughts"),
                    HomeEnvironment = GetFormValueOrDefault("Assessment.HomeEnvironment", "Not assessed"),
                    FamilyRelationship = GetFormValueOrDefault("Assessment.FamilyRelationship", "Not assessed"),
                    
                    EducationCurrentlyStudying = GetFormValueOrDefault("Assessment.EducationCurrentlyStudying"),
                    EducationWorking = GetFormValueOrDefault("Assessment.EducationWorking"),
                    EducationSchoolWorkProblems = GetFormValueOrDefault("Assessment.EducationSchoolWorkProblems"),
                    EducationBullying = GetFormValueOrDefault("Assessment.EducationBullying"),
                    EducationBullyingExperience = GetFormValueOrDefault("Assessment.EducationBullyingExperience"),
                    SchoolPerformance = GetFormValueOrDefault("Assessment.SchoolPerformance", "Not assessed"),
                    AttendanceIssues = GetFormBooleanValueOrDefault("Assessment.AttendanceIssues"),
                    CareerPlans = GetFormValueOrDefault("Assessment.CareerPlans", "Not assessed"),
                    EducationEmployment = GetFormValueOrDefault("Assessment.EducationEmployment", "Not assessed"),
                    
                    EatingBodyImageSatisfaction = GetFormValueOrDefault("Assessment.EatingBodyImageSatisfaction"),
                    EatingDisorderedEatingBehaviors = GetFormValueOrDefault("Assessment.EatingDisorderedEatingBehaviors"),
                    EatingWeightComments = GetFormValueOrDefault("Assessment.EatingWeightComments"),
                    DietDescription = GetFormValueOrDefault("Assessment.DietDescription", "Not assessed"),
                    WeightConcerns = GetFormBooleanValueOrDefault("Assessment.WeightConcerns"),
                    EatingDisorderSymptoms = GetFormBooleanValueOrDefault("Assessment.EatingDisorderSymptoms"),
                    
                    ActivitiesParticipation = GetFormValueOrDefault("Assessment.ActivitiesParticipation"),
                    ActivitiesRegularExercise = GetFormValueOrDefault("Assessment.ActivitiesRegularExercise"),
                    ActivitiesScreenTime = GetFormValueOrDefault("Assessment.ActivitiesScreenTime"),
                    ActivitiesInternetGadgetUse = GetFormValueOrDefault("Assessment.ActivitiesInternetGadgetUse"),
                    Hobbies = GetFormValueOrDefault("Assessment.Hobbies", "Not assessed"),
                    PhysicalActivity = GetFormValueOrDefault("Assessment.PhysicalActivity", "Not assessed"),
                    ScreenTime = GetFormValueOrDefault("Assessment.ScreenTime", "Not assessed"),
                    
                    DrugsTobaccoUse = GetFormValueOrDefault("Assessment.DrugsTobaccoUse"),
                    DrugsAlcoholUse = GetFormValueOrDefault("Assessment.DrugsAlcoholUse"),
                    DrugsIllicitDrugUse = GetFormValueOrDefault("Assessment.DrugsIllicitDrugUse"),
                    DrugsStreetDrugs = GetFormValueOrDefault("Assessment.DrugsStreetDrugs"),
                    SubstanceUse = GetFormBooleanValueOrDefault("Assessment.SubstanceUse"),
                    SubstanceType = GetFormValueOrDefault("Assessment.SubstanceType", "Not assessed"),
                    
                    SexualityBodyConcerns = GetFormValueOrDefault("Assessment.SexualityBodyConcerns"),
                    SexualityHealthConcerns = GetFormValueOrDefault("Assessment.SexualityHealthConcerns"),
                    SexualityIntimateRelationships = GetFormValueOrDefault("Assessment.SexualityIntimateRelationships"),
                    SexualityPartners = GetFormValueOrDefault("Assessment.SexualityPartners"),
                    SexualityPartnersCount = GetFormValueOrDefault("Assessment.SexualityPartnersCount"),
                    SexualitySexualOrientation = GetFormValueOrDefault("Assessment.SexualitySexualOrientation"),
                    SexualityPregnancy = GetFormValueOrDefault("Assessment.SexualityPregnancy"),
                    SexualitySTI = GetFormValueOrDefault("Assessment.SexualitySTI"),
                    SexualityProtection = GetFormValueOrDefault("Assessment.SexualityProtection"),
                    SexualityPregnancyExperience = GetFormValueOrDefault("Assessment.SexualityPregnancyExperience"),
                    SexualitySTIExperience = GetFormValueOrDefault("Assessment.SexualitySTIExperience"),
                    SexualityProtectionUse = GetFormValueOrDefault("Assessment.SexualityProtectionUse"),
                    SexualityHarassment = GetFormValueOrDefault("Assessment.SexualityHarassment"),
                    SexualityGay = GetFormValueOrDefault("Assessment.SexualityGay"),
                    SexualityLesbian = GetFormValueOrDefault("Assessment.SexualityLesbian"),
                    SexualityBisexual = GetFormValueOrDefault("Assessment.SexualityBisexual"),
                    DatingRelationships = GetFormValueOrDefault("Assessment.DatingRelationships", "Not assessed"),
                    SexualActivity = GetFormBooleanValueOrDefault("Assessment.SexualActivity"),
                    SexualOrientation = GetFormValueOrDefault("Assessment.SexualOrientation", "Not assessed"),
                    
                    SafetyPhysicalAbuse = GetFormValueOrDefault("Assessment.SafetyPhysicalAbuse"),
                    SafetyRelationshipViolence = GetFormValueOrDefault("Assessment.SafetyRelationshipViolence"),
                    SafetyProtectiveGear = GetFormValueOrDefault("Assessment.SafetyProtectiveGear"),
                    SafetyGunsAtHome = GetFormValueOrDefault("Assessment.SafetyGunsAtHome"),
                    SafetyWeaponAccess = GetFormValueOrDefault("Assessment.SafetyWeaponAccess"),
                    FeelsSafeAtHome = GetFormBooleanValueOrDefault("Assessment.FeelsSafeAtHome", true),
                    FeelsSafeAtSchool = GetFormBooleanValueOrDefault("Assessment.FeelsSafeAtSchool", true),
                    ExperiencedBullying = GetFormBooleanValueOrDefault("Assessment.ExperiencedBullying"),
                    
                    SuicideDepressionFeelings = GetFormValueOrDefault("Assessment.SuicideDepressionFeelings"),
                    SuicideSelfHarmThoughts = GetFormValueOrDefault("Assessment.SuicideSelfHarmThoughts"),
                    SuicideFamilyHistory = GetFormValueOrDefault("Assessment.SuicideFamilyHistory"),
                    MoodChanges = GetFormBooleanValueOrDefault("Assessment.MoodChanges"),
                    SuicidalThoughts = GetFormBooleanValueOrDefault("Assessment.SuicidalThoughts"),
                    SelfHarmBehavior = GetFormBooleanValueOrDefault("Assessment.SelfHarmBehavior"),
                    
                    // Strengths section
                    PersonalStrengths = GetFormValueOrDefault("Assessment.PersonalStrengths", "Not assessed"),
                    SupportSystems = GetFormValueOrDefault("Assessment.SupportSystems", "Not assessed"),
                    CopingMechanisms = GetFormValueOrDefault("Assessment.CopingMechanisms", "Not assessed"),
                    
                    // EATING HABITS fields
                    EatingVomiting = GetFormValueOrDefault("Assessment.EatingVomiting"),
                    EatingDietPills = GetFormValueOrDefault("Assessment.EatingDietPills"),
                    EatingLaxatives = GetFormValueOrDefault("Assessment.EatingLaxatives"),
                    EatingStarvation = GetFormValueOrDefault("Assessment.EatingStarvation"),
                    
                    // Non-nullable fields with default values
                    Notes = GetFormValueOrDefault("Assessment.Notes"),
                    AssessedBy = GetFormValueOrDefault("Assessment.AssessedBy"),
                    AssessmentNotes = GetFormValueOrDefault("Assessment.AssessmentNotes", "No assessment notes provided"),
                    RecommendedActions = GetFormValueOrDefault("Assessment.RecommendedActions", "No actions recommended"),
                    FollowUpPlan = GetFormValueOrDefault("Assessment.FollowUpPlan", "No follow-up plan specified"),
                    
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // DEBUGGING: Log assessment object before saving
                _logger.LogInformation("=== ASSESSMENT OBJECT BEFORE SAVING ===");
                _logger.LogInformation("Assessment ID: {Id}", assessment.Id);
                _logger.LogInformation("UserId: {UserId}", assessment.UserId);
                _logger.LogInformation("HealthFacility: {HealthFacility}", assessment.HealthFacility);
                _logger.LogInformation("FamilyNo: {FamilyNo}", assessment.FamilyNo);
                _logger.LogInformation("AppointmentId: {AppointmentId}", assessment.AppointmentId);
                _logger.LogInformation("FullName: {FullName}", assessment.FullName);
                _logger.LogInformation("Age: {Age}", assessment.Age);
                _logger.LogInformation("Birthday: {Birthday}", assessment.Birthday);
                
                // Log HEEADSSS specific fields
                _logger.LogInformation("=== HEEADSSS FIELDS BEFORE SAVING ===");
                _logger.LogInformation("HOME - HomeFamilyProblems: '{HomeFamilyProblems}'", assessment.HomeFamilyProblems);
                _logger.LogInformation("HOME - HomeParentalListening: '{HomeParentalListening}'", assessment.HomeParentalListening);
                _logger.LogInformation("HOME - HomeFamilyChanges: '{HomeFamilyChanges}'", assessment.HomeFamilyChanges);
                _logger.LogInformation("EDUCATION - EducationCurrentlyStudying: '{EducationCurrentlyStudying}'", assessment.EducationCurrentlyStudying);
                _logger.LogInformation("EDUCATION - EducationWorking: '{EducationWorking}'", assessment.EducationWorking);
                _logger.LogInformation("EATING - EatingBodyImageSatisfaction: '{EatingBodyImageSatisfaction}'", assessment.EatingBodyImageSatisfaction);
                _logger.LogInformation("ACTIVITIES - ActivitiesParticipation: '{ActivitiesParticipation}'", assessment.ActivitiesParticipation);
                _logger.LogInformation("DRUGS - DrugsTobaccoUse: '{DrugsTobaccoUse}'", assessment.DrugsTobaccoUse);
                _logger.LogInformation("DRUGS - DrugsAlcoholUse: '{DrugsAlcoholUse}'", assessment.DrugsAlcoholUse);
                _logger.LogInformation("SEXUALITY - SexualityHealthConcerns: '{SexualityHealthConcerns}'", assessment.SexualityHealthConcerns);
                _logger.LogInformation("SEXUALITY - SexualityIntimateRelationships: '{SexualityIntimateRelationships}'", assessment.SexualityIntimateRelationships);
                _logger.LogInformation("SAFETY - SafetyPhysicalAbuse: '{SafetyPhysicalAbuse}'", assessment.SafetyPhysicalAbuse);
                _logger.LogInformation("SUICIDE - SuicideDepressionFeelings: '{SuicideDepressionFeelings}'", assessment.SuicideDepressionFeelings);
                _logger.LogInformation("NOTES - Notes: '{Notes}'", assessment.Notes);
                _logger.LogInformation("ASSESSMENT - AssessedBy: '{AssessedBy}'", assessment.AssessedBy);
                
                // Note: Encryption is handled automatically by EncryptedDbContext.SaveChangesAsync()
                _logger.LogInformation("Assessment ready for saving");
                
                _context.HEEADSSSAssessments.Add(assessment);
                
                // Determine if form is complete (check if key required fields are filled)
                bool isFormComplete = !string.IsNullOrWhiteSpace(assessment.HomeEnvironment) &&
                                     !string.IsNullOrWhiteSpace(assessment.SchoolPerformance) &&
                                     !string.IsNullOrWhiteSpace(assessment.DietDescription) &&
                                     !string.IsNullOrWhiteSpace(assessment.Hobbies);
                
                _logger.LogInformation("Form completion check: {IsComplete}", isFormComplete);
                
                // Update appointment status if AppointmentId is provided
                if (!string.IsNullOrEmpty(appointmentId) && int.TryParse(appointmentId, out int parsedAppointmentIdForStatus) && parsedAppointmentIdForStatus > 0)
                {
                    _logger.LogInformation("Attempting to update appointment {AppointmentId} status", parsedAppointmentIdForStatus);
                    var appointment = await _context.Appointments.FindAsync(parsedAppointmentIdForStatus);
                    if (appointment != null)
                    {
                        _logger.LogInformation("Found appointment {AppointmentId}, current status: {Status}", parsedAppointmentIdForStatus, appointment.Status);
                        var oldStatus = appointment.Status;
                        
                        // Set status based on form completion
                        if (isFormComplete)
                        {
                            appointment.Status = Barangay.Models.AppointmentStatus.InProgress;
                            _logger.LogInformation("Form is complete - setting status to InProgress");
                        }
                        else
                        {
                            appointment.Status = Barangay.Models.AppointmentStatus.Draft;
                            _logger.LogInformation("Form is incomplete - setting status to Draft");
                        }
                        
                        appointment.UpdatedAt = DateTime.UtcNow;
                        _logger.LogInformation("Updated appointment {AppointmentId} status from {OldStatus} to {NewStatus}", 
                            parsedAppointmentIdForStatus, oldStatus, appointment.Status);
                        
                        // Store appointment for audit trail logging after save
                        var appointmentForAudit = appointment;
                        var oldStatusForAudit = oldStatus;
                    }
                    else
                    {
                        _logger.LogWarning("Appointment {AppointmentId} not found in database", parsedAppointmentIdForStatus);
                    }
                }
                else
                {
                    _logger.LogWarning("No valid AppointmentId provided for status update");
                }
                
                // Save to database with better error handling
                try
                {
                    _logger.LogInformation("=== ATTEMPTING DATABASE SAVE ===");
                    var rowsAffected = await _context.SaveChangesAsync();
                    _logger.LogInformation("=== DATABASE SAVE COMPLETED ===");
                    _logger.LogInformation("HEEADSSS assessment saved successfully with ID: {Id}, rows affected: {RowsAffected}", assessment.Id, rowsAffected);
                    
                    // Update or create Patient table record with family number if provided
                    if (!string.IsNullOrEmpty(assessment.FamilyNo))
                    {
                        try
                        {
                            // Determine the actual patient UserId (could be different for "book for someone else")
                            string targetUserId = assessment.UserId;
                            
                            // If appointment exists, use the appointment's PatientId (handles "book for someone else")
                            if (!string.IsNullOrEmpty(assessment.AppointmentId) && int.TryParse(assessment.AppointmentId, out int appointmentIdInt))
                            {
                                var appointment = await _context.Appointments
                                    .Include(a => a.Patient)
                                    .FirstOrDefaultAsync(a => a.Id == appointmentIdInt);
                                
                                if (appointment?.Patient != null)
                                {
                                    targetUserId = appointment.Patient.UserId;
                                    _logger.LogInformation("Using PatientId from appointment for family number update: {PatientId}", targetUserId);
                                }
                            }
                            
                            var currentUser = await _context.Users.FindAsync(targetUserId);
                            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == targetUserId);
                            
                            // Also update the Appointment.FamilyNumber if appointment exists
                            if (!string.IsNullOrEmpty(assessment.AppointmentId) && int.TryParse(assessment.AppointmentId, out int appointmentIdForUpdate))
                            {
                                var appointment = await _context.Appointments.FindAsync(appointmentIdForUpdate);
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
                            else if (currentUser != null)
                            {
                                // Create new patient record
                                patient = new Patient
                                {
                                    UserId = targetUserId,
                                    FullName = currentUser.FullName ?? assessment.FullName ?? "Unknown",
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
                    
                    // AUDIT: Log HEEADSSS assessment submission
                    await _auditTrail.LogAsync(
                        "Create",
                        "Submitted HEEADSSS Assessment",
                        "HEEADSSSAssessment",
                        assessment.Id.ToString(),
                        null,
                        "[Sensitive adolescent health data - encrypted]",
                        "Patient completed HEEADSSS adolescent health screening"
                    );
                    
                    // Log appointment status change to audit trail
                    if (!string.IsNullOrEmpty(appointmentId) && int.TryParse(appointmentId, out int apptIdForAudit) && apptIdForAudit > 0)
                    {
                        var apptForAudit = await _context.Appointments.FindAsync(apptIdForAudit);
                        if (apptForAudit != null)
                        {
                            try
                            {
                                await _auditTrail.LogAsync(
                                    "Appointment Assessment Completed",
                                    "Complete Assessment Form",
                                    "Appointment",
                                    apptForAudit.Id.ToString(),
                                    $"Status: Draft",
                                    $"Status: {Barangay.Models.AppointmentStatus.InProgress}",
                                    $"User completed HEEADSSS assessment for appointment on {apptForAudit.AppointmentDate:MMM dd, yyyy} at {apptForAudit.AppointmentTime:hh\\:mm tt}"
                                );
                            }
                            catch (Exception auditEx)
                            {
                                _logger.LogWarning(auditEx, "Error logging appointment status change to audit trail");
                            }
                        }
                    }
                    
                    // DEBUGGING: Verify the saved data by querying it back
                    _logger.LogInformation("=== VERIFYING SAVED DATA ===");
                    var savedAssessment = await _context.HEEADSSSAssessments.FindAsync(assessment.Id);
                    if (savedAssessment != null)
                    {
                        _logger.LogInformation("Saved assessment found with ID: {Id}", savedAssessment.Id);
                        _logger.LogInformation("Saved UserId: {UserId}", savedAssessment.UserId);
                        _logger.LogInformation("Saved FullName: {FullName}", savedAssessment.FullName);
                        _logger.LogInformation("Saved AppointmentId: {AppointmentId}", savedAssessment.AppointmentId);
                        
                        // Try to decrypt and verify some key fields
                        try
                        {
                            savedAssessment.DecryptSensitiveData(_encryptionService, User);
                            _logger.LogInformation("=== DECRYPTED SAVED DATA VERIFICATION ===");
                            _logger.LogInformation("Decrypted HomeFamilyProblems: '{HomeFamilyProblems}'", savedAssessment.HomeFamilyProblems);
                            _logger.LogInformation("Decrypted EducationCurrentlyStudying: '{EducationCurrentlyStudying}'", savedAssessment.EducationCurrentlyStudying);
                            _logger.LogInformation("Decrypted Notes: '{Notes}'", savedAssessment.Notes);
                        }
                        catch (Exception decryptEx)
                        {
                            _logger.LogWarning(decryptEx, "Could not decrypt saved assessment for verification");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Could not find saved assessment with ID: {Id}", assessment.Id);
                    }
                    
                    if (rowsAffected > 0)
                    {
                        return new JsonResult(new { success = true });
                    }
                    else
                    {
                        _logger.LogWarning("No rows were affected during save operation");
                        return new JsonResult(new { success = false, error = "Failed to save assessment. Please try again." });
                    }
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Database update error: {Message}", dbEx.Message);
                    return new JsonResult(new { success = false, error = "Database error occurred. Please check your data and try again." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving HEEADSSS assessment");
                return new JsonResult(new { success = false, error = "An error occurred while saving the assessment. Please try again." });
            }
        }

        private int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            
            if (today < birthDate.AddYears(age))
            {
                age--;
            }
            
            _logger.LogInformation($"Calculated age for birthdate {birthDate.ToShortDateString()}: {age} years (using current date: {today.ToShortDateString()})");
            return age;
        }

        // Copied from NCDRiskAssessment - exact same flow
        private async Task<(string familyNo, bool isPreexisting)> GetOrGenerateFamilyNumberAsync(ApplicationUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            // Check if user already has a family number - only query essential fields to avoid column errors
            var existingAssessment = await _context.HEEADSSSAssessments
                .Where(a => a.UserId == user.Id && !string.IsNullOrEmpty(a.FamilyNo))
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new { a.Id, a.FamilyNo, a.CreatedAt })
                .FirstOrDefaultAsync();

            if (existingAssessment != null)
            {
                // FamilyNo is no longer encrypted - use directly
                return (existingAssessment.FamilyNo, true);
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

        // Helper method to get form value with default
        private string GetFormValueOrDefault(string key, string defaultValue = "Not provided")
        {
            if (Request.Form.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value.ToString()))
            {
                return value.ToString();
            }
            return defaultValue;
        }
        
        // Helper method to get boolean form value with default
        private bool? GetFormBooleanValueOrDefault(string key, bool? defaultValue = false)
        {
            if (Request.Form.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value.ToString()))
            {
                var stringValue = value.ToString().ToLower();
                if (stringValue == "true" || stringValue == "1" || stringValue == "on" || stringValue == "yes")
                {
                    return true;
                }
                else if (stringValue == "false" || stringValue == "0" || stringValue == "no")
                {
                    return false;
                }
            }
            return defaultValue;
        }
        
        // AJAX endpoint for calculating age from birthdate
        public IActionResult OnPostCalculateAgeAsync(string birthdate)
        {
            _logger.LogInformation($"Calculating age for birthdate: {birthdate}");
            
            try
            {
                if (DateTime.TryParse(birthdate, out DateTime parsedDate))
                {
                    int age = CalculateAge(parsedDate);
                    _logger.LogInformation($"Server calculated age: {age}");
                    return new JsonResult(new { success = true, age = age });
                }
                else
                {
                    _logger.LogWarning($"Could not parse birthdate: {birthdate}");
                    return new JsonResult(new { success = false, error = "Invalid date format" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating age for birthdate: {birthdate}");
                return new JsonResult(new { success = false, error = ex.Message });
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
                int lastNCDNumber = await _context.NCDRiskAssessments
                    .Where(a => a.FamilyNo != null && a.FamilyNo.StartsWith(firstLetter + "-"))
                    .Select(a => a.FamilyNo.Substring(2))
                    .Where(n => n.All(char.IsDigit))
                    .Select(n => int.Parse(n))
                    .DefaultIfEmpty(0)
                    .MaxAsync();
                    
                int lastHEEADSSSNumber = await _context.HEEADSSSAssessments
                    .Where(a => a.FamilyNo != null && a.FamilyNo.StartsWith(firstLetter + "-"))
                    .Select(a => a.FamilyNo.Substring(2))
                    .Where(n => n.All(char.IsDigit))
                    .Select(n => int.Parse(n))
                    .DefaultIfEmpty(0)
                    .MaxAsync();
                    
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

        // Request model for family number generation (copied from BookAppointment)
        public class GenerateFamilyNumberRequest
        {
            public string LastName { get; set; }
            public bool SameFamily { get; set; } = false;
        }

        // Handler for generating family numbers (COPIED FROM BookAppointment - COMPLETE FLOW)
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
                    _logger.LogWarning("Patient record not found for user {UserId}, will be created when saving assessment", user.Id);
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
                    familyNo = response.FamilyNumber,
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

        public async Task<IActionResult> OnGetSearchFamiliesAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return new JsonResult(new { success = false, error = "Search term is required" });
                }

                // Get user to decrypt patient data
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return new JsonResult(new { success = false, error = "User not found" });
                }

                currentUser = currentUser.DecryptSensitiveData(_encryptionService, User);

                // Search for families by surname or family number
                var searchLower = searchTerm.ToLower();

                // Search by family number first
                var familyByNumber = await _context.Patients
                    .Where(p => p.FamilyNumber != null && p.FamilyNumber.Contains(searchTerm))
                    .ToListAsync();

                // If no results by family number, search by surname
                if (!familyByNumber.Any())
                {
                    // Get all patients and decrypt their names for searching
                    var allPatients = await _context.Patients
                        .ToListAsync();

                    // Decrypt patient names in memory
                    foreach (var patient in allPatients)
                    {
                        try
                        {
                            var decryptedName = patient.FullName;
                            if (_encryptionService.IsEncrypted(decryptedName))
                            {
                                decryptedName = patient.FullName.DecryptForUser(_encryptionService, User);
                            }
                            patient.FullName = decryptedName;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to decrypt name for patient {PatientId}", patient.UserId);
                        }
                    }

                    // Search by surname (last name)
                    familyByNumber = allPatients
                        .Where(p => 
                            p.FamilyNumber != null && 
                            !string.IsNullOrWhiteSpace(p.FullName) &&
                            p.FullName.ToLower().Contains(searchLower))
                        .ToList();
                }
                else
                {
                    // Decrypt names for patients found by family number
                    foreach (var patient in familyByNumber)
                    {
                        try
                        {
                            var decryptedName = patient.FullName;
                            if (_encryptionService.IsEncrypted(decryptedName))
                            {
                                decryptedName = patient.FullName.DecryptForUser(_encryptionService, User);
                            }
                            patient.FullName = decryptedName;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to decrypt name for patient {PatientId}", patient.UserId);
                        }
                    }
                }

                // Group by family number and format results
                var families = familyByNumber
                    .Where(p => !string.IsNullOrWhiteSpace(p.FamilyNumber))
                    .GroupBy(p => p.FamilyNumber)
                    .Select(g => new
                    {
                        familyNumber = g.Key,
                        members = g.Select(p => new
                        {
                            name = p.FullName,
                            familyNumber = p.FamilyNumber
                        }).Distinct().Take(5).ToList() // Limit to 5 members per family
                    })
                    .Take(10) // Limit to 10 families
                    .ToList();

                return new JsonResult(new { success = true, families = families });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for families");
                return new JsonResult(new { success = false, error = "An error occurred while searching for families" });
            }
        }

    }
} 