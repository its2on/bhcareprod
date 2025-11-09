using System;
using System.Threading.Tasks;
using System.Linq;
using Barangay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Barangay.Data;
using Barangay.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Barangay.Extensions;

namespace Barangay.Pages.Nurse
{
    // Authorization: Admin can edit all forms, Nurse/Doctor can edit their assigned forms
    // Patients cannot access edit pages - they can only view/read their submitted forms
    [Authorize(Roles = "Nurse,Head Nurse,Doctor,Head Doctor,Admin")]
    public class EditHEEADSSSAssessmentModel : PageModel
    {
        private readonly EncryptedDbContext _context;
        private readonly ILogger<EditHEEADSSSAssessmentModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDataEncryptionService _encryptionService;

        public EditHEEADSSSAssessmentModel(
            EncryptedDbContext context,
            ILogger<EditHEEADSSSAssessmentModel> logger,
            UserManager<ApplicationUser> userManager,
            IDataEncryptionService encryptionService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _encryptionService = encryptionService;
        }

        [BindProperty]
        public HEEADSSSAssessmentViewModel Assessment { get; set; }

        /// <summary>
        /// Checks if the current user is an Admin role
        /// Admin has full edit permissions for all forms across all roles
        /// </summary>
        private bool IsAdminRole()
        {
            return User.IsInRole("Admin");
        }

        /// <summary>
        /// Checks if the current user is a Doctor role (includes Admin)
        /// Used for layout selection and navigation redirects
        /// </summary>
        private bool IsDoctorRole()
        {
            return User.IsInRole("Doctor") || User.IsInRole("Head Doctor") || User.IsInRole("Admin");
        }

        /// <summary>
        /// Checks if the current user is a Nurse role
        /// </summary>
        private bool IsNurseRole()
        {
            return User.IsInRole("Nurse") || User.IsInRole("Head Nurse");
        }

        public async Task<IActionResult> OnGetAsync(int appointmentId)
        {
            try
            {
                // Get the appointment
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);

                if (appointment == null)
                {
                    TempData["StatusMessage"] = "Error: Appointment not found.";
                    return IsDoctorRole() ? RedirectToPage("/Doctor/Consultations") : RedirectToPage("/Nurse/Appointments");
                }

                // Get existing HEEADSSS assessment
                // Get HEEADSSS assessment by UserId (same logic as AppointmentDetails)
                HEEADSSSAssessment? existingAssessment = null;
                
                if (appointment.Patient != null)
                {
                    // Decrypt patient data first
                    appointment.Patient.DecryptSensitiveData(_encryptionService, User);
                    
                    // Look for HEEADSSS assessment by UserId
                    existingAssessment = await _context.HEEADSSSAssessments
                        .Where(a => a.UserId == appointment.Patient.UserId)
                        .OrderByDescending(a => a.CreatedAt)
                        .FirstOrDefaultAsync();
                }

                if (existingAssessment == null)
                {
                    TempData["StatusMessage"] = "Error: HEEADSSS assessment not found.";
                    return IsDoctorRole() ? RedirectToPage("/Doctor/Consultation", new { id = appointmentId }) : RedirectToPage("/Nurse/AppointmentDetails", new { id = appointmentId });
                }

                // Decrypt existing assessment data for editing
                existingAssessment.DecryptSensitiveData(_encryptionService, User);
                
                // Manual decryption fallback for all HEEADSSS fields
                // Personal Information
                if (!string.IsNullOrEmpty(existingAssessment.FullName) && _encryptionService.IsEncrypted(existingAssessment.FullName))
                {
                    existingAssessment.FullName = _encryptionService.DecryptForUser(existingAssessment.FullName, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.Age) && _encryptionService.IsEncrypted(existingAssessment.Age))
                {
                    existingAssessment.Age = _encryptionService.DecryptForUser(existingAssessment.Age, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.Gender) && _encryptionService.IsEncrypted(existingAssessment.Gender))
                {
                    existingAssessment.Gender = _encryptionService.DecryptForUser(existingAssessment.Gender, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.Address) && _encryptionService.IsEncrypted(existingAssessment.Address))
                {
                    existingAssessment.Address = _encryptionService.DecryptForUser(existingAssessment.Address, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.ContactNumber) && _encryptionService.IsEncrypted(existingAssessment.ContactNumber))
                {
                    existingAssessment.ContactNumber = _encryptionService.DecryptForUser(existingAssessment.ContactNumber, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.HealthFacility) && _encryptionService.IsEncrypted(existingAssessment.HealthFacility))
                {
                    existingAssessment.HealthFacility = _encryptionService.DecryptForUser(existingAssessment.HealthFacility, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.FamilyNo) && _encryptionService.IsEncrypted(existingAssessment.FamilyNo))
                {
                    existingAssessment.FamilyNo = _encryptionService.DecryptForUser(existingAssessment.FamilyNo, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.IsNHPTS) && _encryptionService.IsEncrypted(existingAssessment.IsNHPTS))
                {
                    existingAssessment.IsNHPTS = _encryptionService.DecryptForUser(existingAssessment.IsNHPTS, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.Is4Ps) && _encryptionService.IsEncrypted(existingAssessment.Is4Ps))
                {
                    existingAssessment.Is4Ps = _encryptionService.DecryptForUser(existingAssessment.Is4Ps, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.IsPhilHealthBeneficiaryOnly) && _encryptionService.IsEncrypted(existingAssessment.IsPhilHealthBeneficiaryOnly))
                {
                    existingAssessment.IsPhilHealthBeneficiaryOnly = _encryptionService.DecryptForUser(existingAssessment.IsPhilHealthBeneficiaryOnly, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.IsOwnPhilHealth) && _encryptionService.IsEncrypted(existingAssessment.IsOwnPhilHealth))
                {
                    existingAssessment.IsOwnPhilHealth = _encryptionService.DecryptForUser(existingAssessment.IsOwnPhilHealth, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.PhilHealthPIN) && _encryptionService.IsEncrypted(existingAssessment.PhilHealthPIN))
                {
                    existingAssessment.PhilHealthPIN = _encryptionService.DecryptForUser(existingAssessment.PhilHealthPIN, User);
                }
                
                // Measurements and Health Information
                if (!string.IsNullOrEmpty(existingAssessment.Height) && _encryptionService.IsEncrypted(existingAssessment.Height))
                {
                    existingAssessment.Height = _encryptionService.DecryptForUser(existingAssessment.Height, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.Weight) && _encryptionService.IsEncrypted(existingAssessment.Weight))
                {
                    existingAssessment.Weight = _encryptionService.DecryptForUser(existingAssessment.Weight, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.BMI) && _encryptionService.IsEncrypted(existingAssessment.BMI))
                {
                    existingAssessment.BMI = _encryptionService.DecryptForUser(existingAssessment.BMI, User);
                }
                
                // Immunization Status
                if (!string.IsNullOrEmpty(existingAssessment.ImmunizationMR) && _encryptionService.IsEncrypted(existingAssessment.ImmunizationMR))
                {
                    existingAssessment.ImmunizationMR = _encryptionService.DecryptForUser(existingAssessment.ImmunizationMR, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.ImmunizationTd) && _encryptionService.IsEncrypted(existingAssessment.ImmunizationTd))
                {
                    existingAssessment.ImmunizationTd = _encryptionService.DecryptForUser(existingAssessment.ImmunizationTd, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.ImmunizationHPV) && _encryptionService.IsEncrypted(existingAssessment.ImmunizationHPV))
                {
                    existingAssessment.ImmunizationHPV = _encryptionService.DecryptForUser(existingAssessment.ImmunizationHPV, User);
                }
                
                // For Females Only
                if (!string.IsNullOrEmpty(existingAssessment.DateOfMenarche) && _encryptionService.IsEncrypted(existingAssessment.DateOfMenarche))
                {
                    existingAssessment.DateOfMenarche = _encryptionService.DecryptForUser(existingAssessment.DateOfMenarche, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.AgeOfFirstPregnancy) && _encryptionService.IsEncrypted(existingAssessment.AgeOfFirstPregnancy))
                {
                    existingAssessment.AgeOfFirstPregnancy = _encryptionService.DecryptForUser(existingAssessment.AgeOfFirstPregnancy, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.OBScore) && _encryptionService.IsEncrypted(existingAssessment.OBScore))
                {
                    existingAssessment.OBScore = _encryptionService.DecryptForUser(existingAssessment.OBScore, User);
                }
                
                // Vital Signs
                if (!string.IsNullOrEmpty(existingAssessment.VitalTemp) && _encryptionService.IsEncrypted(existingAssessment.VitalTemp))
                {
                    existingAssessment.VitalTemp = _encryptionService.DecryptForUser(existingAssessment.VitalTemp, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.VitalRR) && _encryptionService.IsEncrypted(existingAssessment.VitalRR))
                {
                    existingAssessment.VitalRR = _encryptionService.DecryptForUser(existingAssessment.VitalRR, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.VitalPR) && _encryptionService.IsEncrypted(existingAssessment.VitalPR))
                {
                    existingAssessment.VitalPR = _encryptionService.DecryptForUser(existingAssessment.VitalPR, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.VitalBP) && _encryptionService.IsEncrypted(existingAssessment.VitalBP))
                {
                    existingAssessment.VitalBP = _encryptionService.DecryptForUser(existingAssessment.VitalBP, User);
                }
                
                // Medical Information
                if (!string.IsNullOrEmpty(existingAssessment.ChiefComplaint) && _encryptionService.IsEncrypted(existingAssessment.ChiefComplaint))
                {
                    existingAssessment.ChiefComplaint = _encryptionService.DecryptForUser(existingAssessment.ChiefComplaint, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.HistoryOfPresentIllness) && _encryptionService.IsEncrypted(existingAssessment.HistoryOfPresentIllness))
                {
                    existingAssessment.HistoryOfPresentIllness = _encryptionService.DecryptForUser(existingAssessment.HistoryOfPresentIllness, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.PhysicalExaminationFindings) && _encryptionService.IsEncrypted(existingAssessment.PhysicalExaminationFindings))
                {
                    existingAssessment.PhysicalExaminationFindings = _encryptionService.DecryptForUser(existingAssessment.PhysicalExaminationFindings, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.PastMedicalHistory) && _encryptionService.IsEncrypted(existingAssessment.PastMedicalHistory))
                {
                    existingAssessment.PastMedicalHistory = _encryptionService.DecryptForUser(existingAssessment.PastMedicalHistory, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.WorkingDiagnosis) && _encryptionService.IsEncrypted(existingAssessment.WorkingDiagnosis))
                {
                    existingAssessment.WorkingDiagnosis = _encryptionService.DecryptForUser(existingAssessment.WorkingDiagnosis, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.Management) && _encryptionService.IsEncrypted(existingAssessment.Management))
                {
                    existingAssessment.Management = _encryptionService.DecryptForUser(existingAssessment.Management, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.FamilyHistory) && _encryptionService.IsEncrypted(existingAssessment.FamilyHistory))
                {
                    existingAssessment.FamilyHistory = _encryptionService.DecryptForUser(existingAssessment.FamilyHistory, User);
                }
                
                // Referral Information
                if (!string.IsNullOrEmpty(existingAssessment.ReferredTo) && _encryptionService.IsEncrypted(existingAssessment.ReferredTo))
                {
                    existingAssessment.ReferredTo = _encryptionService.DecryptForUser(existingAssessment.ReferredTo, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.ReasonForReferral) && _encryptionService.IsEncrypted(existingAssessment.ReasonForReferral))
                {
                    existingAssessment.ReasonForReferral = _encryptionService.DecryptForUser(existingAssessment.ReasonForReferral, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.FollowUpDate) && _encryptionService.IsEncrypted(existingAssessment.FollowUpDate))
                {
                    existingAssessment.FollowUpDate = _encryptionService.DecryptForUser(existingAssessment.FollowUpDate, User);
                }
                
                // HOME section fields
                if (!string.IsNullOrEmpty(existingAssessment.HomeEnvironment) && _encryptionService.IsEncrypted(existingAssessment.HomeEnvironment))
                {
                    existingAssessment.HomeEnvironment = _encryptionService.DecryptForUser(existingAssessment.HomeEnvironment, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.FamilyRelationship) && _encryptionService.IsEncrypted(existingAssessment.FamilyRelationship))
                {
                    existingAssessment.FamilyRelationship = _encryptionService.DecryptForUser(existingAssessment.FamilyRelationship, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.HomeFamilyProblems) && _encryptionService.IsEncrypted(existingAssessment.HomeFamilyProblems))
                {
                    existingAssessment.HomeFamilyProblems = _encryptionService.DecryptForUser(existingAssessment.HomeFamilyProblems, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.HomeParentalListening) && _encryptionService.IsEncrypted(existingAssessment.HomeParentalListening))
                {
                    existingAssessment.HomeParentalListening = _encryptionService.DecryptForUser(existingAssessment.HomeParentalListening, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.HomeParentalBlame) && _encryptionService.IsEncrypted(existingAssessment.HomeParentalBlame))
                {
                    existingAssessment.HomeParentalBlame = _encryptionService.DecryptForUser(existingAssessment.HomeParentalBlame, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.HomeFamilyChanges) && _encryptionService.IsEncrypted(existingAssessment.HomeFamilyChanges))
                {
                    existingAssessment.HomeFamilyChanges = _encryptionService.DecryptForUser(existingAssessment.HomeFamilyChanges, User);
                }
                
                // EDUCATION section fields  
                if (!string.IsNullOrEmpty(existingAssessment.SchoolPerformance) && _encryptionService.IsEncrypted(existingAssessment.SchoolPerformance))
                {
                    existingAssessment.SchoolPerformance = _encryptionService.DecryptForUser(existingAssessment.SchoolPerformance, User);
                }
                // AttendanceIssues is now a boolean field, no need to decrypt
                if (!string.IsNullOrEmpty(existingAssessment.CareerPlans) && _encryptionService.IsEncrypted(existingAssessment.CareerPlans))
                {
                    existingAssessment.CareerPlans = _encryptionService.DecryptForUser(existingAssessment.CareerPlans, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.EducationCurrentlyStudying) && _encryptionService.IsEncrypted(existingAssessment.EducationCurrentlyStudying))
                {
                    existingAssessment.EducationCurrentlyStudying = _encryptionService.DecryptForUser(existingAssessment.EducationCurrentlyStudying, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.EducationWorking) && _encryptionService.IsEncrypted(existingAssessment.EducationWorking))
                {
                    existingAssessment.EducationWorking = _encryptionService.DecryptForUser(existingAssessment.EducationWorking, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.EducationSchoolWorkProblems) && _encryptionService.IsEncrypted(existingAssessment.EducationSchoolWorkProblems))
                {
                    existingAssessment.EducationSchoolWorkProblems = _encryptionService.DecryptForUser(existingAssessment.EducationSchoolWorkProblems, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.EducationBullying) && _encryptionService.IsEncrypted(existingAssessment.EducationBullying))
                {
                    existingAssessment.EducationBullying = _encryptionService.DecryptForUser(existingAssessment.EducationBullying, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.EducationEmployment) && _encryptionService.IsEncrypted(existingAssessment.EducationEmployment))
                {
                    existingAssessment.EducationEmployment = _encryptionService.DecryptForUser(existingAssessment.EducationEmployment, User);
                }
                
                // EATING HABITS section fields
                if (!string.IsNullOrEmpty(existingAssessment.DietDescription) && _encryptionService.IsEncrypted(existingAssessment.DietDescription))
                {
                    existingAssessment.DietDescription = _encryptionService.DecryptForUser(existingAssessment.DietDescription, User);
                }
                // WeightConcerns and EatingDisorderSymptoms are now boolean fields, no need to decrypt
                if (!string.IsNullOrEmpty(existingAssessment.EatingBodyImageSatisfaction) && _encryptionService.IsEncrypted(existingAssessment.EatingBodyImageSatisfaction))
                {
                    existingAssessment.EatingBodyImageSatisfaction = _encryptionService.DecryptForUser(existingAssessment.EatingBodyImageSatisfaction, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.EatingDisorderedEatingBehaviors) && _encryptionService.IsEncrypted(existingAssessment.EatingDisorderedEatingBehaviors))
                {
                    existingAssessment.EatingDisorderedEatingBehaviors = _encryptionService.DecryptForUser(existingAssessment.EatingDisorderedEatingBehaviors, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.EatingWeightComments) && _encryptionService.IsEncrypted(existingAssessment.EatingWeightComments))
                {
                    existingAssessment.EatingWeightComments = _encryptionService.DecryptForUser(existingAssessment.EatingWeightComments, User);
                }
                
                // ACTIVITIES section fields
                if (!string.IsNullOrEmpty(existingAssessment.Hobbies) && _encryptionService.IsEncrypted(existingAssessment.Hobbies))
                {
                    existingAssessment.Hobbies = _encryptionService.DecryptForUser(existingAssessment.Hobbies, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.PhysicalActivity) && _encryptionService.IsEncrypted(existingAssessment.PhysicalActivity))
                {
                    existingAssessment.PhysicalActivity = _encryptionService.DecryptForUser(existingAssessment.PhysicalActivity, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.ScreenTime) && _encryptionService.IsEncrypted(existingAssessment.ScreenTime))
                {
                    existingAssessment.ScreenTime = _encryptionService.DecryptForUser(existingAssessment.ScreenTime, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.ActivitiesParticipation) && _encryptionService.IsEncrypted(existingAssessment.ActivitiesParticipation))
                {
                    existingAssessment.ActivitiesParticipation = _encryptionService.DecryptForUser(existingAssessment.ActivitiesParticipation, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.ActivitiesRegularExercise) && _encryptionService.IsEncrypted(existingAssessment.ActivitiesRegularExercise))
                {
                    existingAssessment.ActivitiesRegularExercise = _encryptionService.DecryptForUser(existingAssessment.ActivitiesRegularExercise, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.ActivitiesScreenTime) && _encryptionService.IsEncrypted(existingAssessment.ActivitiesScreenTime))
                {
                    existingAssessment.ActivitiesScreenTime = _encryptionService.DecryptForUser(existingAssessment.ActivitiesScreenTime, User);
                }
                
                // DRUGS section fields
                // SubstanceUse is now a boolean field, no need to decrypt
                if (!string.IsNullOrEmpty(existingAssessment.SubstanceType) && _encryptionService.IsEncrypted(existingAssessment.SubstanceType))
                {
                    existingAssessment.SubstanceType = _encryptionService.DecryptForUser(existingAssessment.SubstanceType, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.DrugsTobaccoUse) && _encryptionService.IsEncrypted(existingAssessment.DrugsTobaccoUse))
                {
                    existingAssessment.DrugsTobaccoUse = _encryptionService.DecryptForUser(existingAssessment.DrugsTobaccoUse, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.DrugsAlcoholUse) && _encryptionService.IsEncrypted(existingAssessment.DrugsAlcoholUse))
                {
                    existingAssessment.DrugsAlcoholUse = _encryptionService.DecryptForUser(existingAssessment.DrugsAlcoholUse, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.DrugsIllicitDrugUse) && _encryptionService.IsEncrypted(existingAssessment.DrugsIllicitDrugUse))
                {
                    existingAssessment.DrugsIllicitDrugUse = _encryptionService.DecryptForUser(existingAssessment.DrugsIllicitDrugUse, User);
                }
                
                // SEXUALITY section fields
                if (!string.IsNullOrEmpty(existingAssessment.DatingRelationships) && _encryptionService.IsEncrypted(existingAssessment.DatingRelationships))
                {
                    existingAssessment.DatingRelationships = _encryptionService.DecryptForUser(existingAssessment.DatingRelationships, User);
                }
                // SexualActivity is now a boolean field, no need to decrypt
                if (!string.IsNullOrEmpty(existingAssessment.SexualOrientation) && _encryptionService.IsEncrypted(existingAssessment.SexualOrientation))
                {
                    existingAssessment.SexualOrientation = _encryptionService.DecryptForUser(existingAssessment.SexualOrientation, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.SexualityBodyConcerns) && _encryptionService.IsEncrypted(existingAssessment.SexualityBodyConcerns))
                {
                    existingAssessment.SexualityBodyConcerns = _encryptionService.DecryptForUser(existingAssessment.SexualityBodyConcerns, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.SexualityHealthConcerns) && _encryptionService.IsEncrypted(existingAssessment.SexualityHealthConcerns))
                {
                    existingAssessment.SexualityHealthConcerns = _encryptionService.DecryptForUser(existingAssessment.SexualityHealthConcerns, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.SexualityPartnersCount) && _encryptionService.IsEncrypted(existingAssessment.SexualityPartnersCount))
                {
                    existingAssessment.SexualityPartnersCount = _encryptionService.DecryptForUser(existingAssessment.SexualityPartnersCount, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.SexualityIntimateRelationships) && _encryptionService.IsEncrypted(existingAssessment.SexualityIntimateRelationships))
                {
                    existingAssessment.SexualityIntimateRelationships = _encryptionService.DecryptForUser(existingAssessment.SexualityIntimateRelationships, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.SexualityPartners) && _encryptionService.IsEncrypted(existingAssessment.SexualityPartners))
                {
                    existingAssessment.SexualityPartners = _encryptionService.DecryptForUser(existingAssessment.SexualityPartners, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.SexualitySexualOrientation) && _encryptionService.IsEncrypted(existingAssessment.SexualitySexualOrientation))
                {
                    existingAssessment.SexualitySexualOrientation = _encryptionService.DecryptForUser(existingAssessment.SexualitySexualOrientation, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.SexualityPregnancy) && _encryptionService.IsEncrypted(existingAssessment.SexualityPregnancy))
                {
                    existingAssessment.SexualityPregnancy = _encryptionService.DecryptForUser(existingAssessment.SexualityPregnancy, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.SexualitySTI) && _encryptionService.IsEncrypted(existingAssessment.SexualitySTI))
                {
                    existingAssessment.SexualitySTI = _encryptionService.DecryptForUser(existingAssessment.SexualitySTI, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.SexualityProtection) && _encryptionService.IsEncrypted(existingAssessment.SexualityProtection))
                {
                    existingAssessment.SexualityProtection = _encryptionService.DecryptForUser(existingAssessment.SexualityProtection, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.SexualityPregnancyExperience) && _encryptionService.IsEncrypted(existingAssessment.SexualityPregnancyExperience))
                {
                    existingAssessment.SexualityPregnancyExperience = _encryptionService.DecryptForUser(existingAssessment.SexualityPregnancyExperience, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.SexualitySTIExperience) && _encryptionService.IsEncrypted(existingAssessment.SexualitySTIExperience))
                {
                    existingAssessment.SexualitySTIExperience = _encryptionService.DecryptForUser(existingAssessment.SexualitySTIExperience, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.SexualityProtectionUse) && _encryptionService.IsEncrypted(existingAssessment.SexualityProtectionUse))
                {
                    existingAssessment.SexualityProtectionUse = _encryptionService.DecryptForUser(existingAssessment.SexualityProtectionUse, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.SexualityHarassment) && _encryptionService.IsEncrypted(existingAssessment.SexualityHarassment))
                {
                    existingAssessment.SexualityHarassment = _encryptionService.DecryptForUser(existingAssessment.SexualityHarassment, User);
                }
                
                // SUICIDE/DEPRESSION section fields
                // MoodChanges, SuicidalThoughts, and SelfHarmBehavior are now boolean fields, no need to decrypt
                if (!string.IsNullOrEmpty(existingAssessment.SuicideDepressionFeelings) && _encryptionService.IsEncrypted(existingAssessment.SuicideDepressionFeelings))
                {
                    existingAssessment.SuicideDepressionFeelings = _encryptionService.DecryptForUser(existingAssessment.SuicideDepressionFeelings, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.SuicideSelfHarmThoughts) && _encryptionService.IsEncrypted(existingAssessment.SuicideSelfHarmThoughts))
                {
                    existingAssessment.SuicideSelfHarmThoughts = _encryptionService.DecryptForUser(existingAssessment.SuicideSelfHarmThoughts, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.SuicideFamilyHistory) && _encryptionService.IsEncrypted(existingAssessment.SuicideFamilyHistory))
                {
                    existingAssessment.SuicideFamilyHistory = _encryptionService.DecryptForUser(existingAssessment.SuicideFamilyHistory, User);
                }
                
                // SAFETY section fields
                // FeelsSafeAtHome, FeelsSafeAtSchool, and ExperiencedBullying are now boolean fields, no need to decrypt
                if (!string.IsNullOrEmpty(existingAssessment.SafetyPhysicalAbuse) && _encryptionService.IsEncrypted(existingAssessment.SafetyPhysicalAbuse))
                {
                    existingAssessment.SafetyPhysicalAbuse = _encryptionService.DecryptForUser(existingAssessment.SafetyPhysicalAbuse, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.SafetyRelationshipViolence) && _encryptionService.IsEncrypted(existingAssessment.SafetyRelationshipViolence))
                {
                    existingAssessment.SafetyRelationshipViolence = _encryptionService.DecryptForUser(existingAssessment.SafetyRelationshipViolence, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.SafetyProtectiveGear) && _encryptionService.IsEncrypted(existingAssessment.SafetyProtectiveGear))
                {
                    existingAssessment.SafetyProtectiveGear = _encryptionService.DecryptForUser(existingAssessment.SafetyProtectiveGear, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.SafetyGunsAtHome) && _encryptionService.IsEncrypted(existingAssessment.SafetyGunsAtHome))
                {
                    existingAssessment.SafetyGunsAtHome = _encryptionService.DecryptForUser(existingAssessment.SafetyGunsAtHome, User);
                }
                
                // STRENGTHS section fields
                if (!string.IsNullOrEmpty(existingAssessment.PersonalStrengths) && _encryptionService.IsEncrypted(existingAssessment.PersonalStrengths))
                {
                    existingAssessment.PersonalStrengths = _encryptionService.DecryptForUser(existingAssessment.PersonalStrengths, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.SupportSystems) && _encryptionService.IsEncrypted(existingAssessment.SupportSystems))
                {
                    existingAssessment.SupportSystems = _encryptionService.DecryptForUser(existingAssessment.SupportSystems, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.CopingMechanisms) && _encryptionService.IsEncrypted(existingAssessment.CopingMechanisms))
                {
                    existingAssessment.CopingMechanisms = _encryptionService.DecryptForUser(existingAssessment.CopingMechanisms, User);
                }
                
                // Assessment Information
                if (!string.IsNullOrEmpty(existingAssessment.AssessmentNotes) && _encryptionService.IsEncrypted(existingAssessment.AssessmentNotes))
                {
                    existingAssessment.AssessmentNotes = _encryptionService.DecryptForUser(existingAssessment.AssessmentNotes, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.RecommendedActions) && _encryptionService.IsEncrypted(existingAssessment.RecommendedActions))
                {
                    existingAssessment.RecommendedActions = _encryptionService.DecryptForUser(existingAssessment.RecommendedActions, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.FollowUpPlan) && _encryptionService.IsEncrypted(existingAssessment.FollowUpPlan))
                {
                    existingAssessment.FollowUpPlan = _encryptionService.DecryptForUser(existingAssessment.FollowUpPlan, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.Notes) && _encryptionService.IsEncrypted(existingAssessment.Notes))
                {
                    existingAssessment.Notes = _encryptionService.DecryptForUser(existingAssessment.Notes, User);
                }
                if (!string.IsNullOrEmpty(existingAssessment.AssessedBy) && _encryptionService.IsEncrypted(existingAssessment.AssessedBy))
                {
                    existingAssessment.AssessedBy = _encryptionService.DecryptForUser(existingAssessment.AssessedBy, User);
                }

                // Map to ViewModel
                Assessment = new HEEADSSSAssessmentViewModel
                {
                    AppointmentId = appointmentId,
                    UserId = appointment.PatientId,
                    FullName = existingAssessment.FullName ?? appointment.Patient?.FullName,
                    Gender = existingAssessment.Gender ?? appointment.Patient?.Gender,
                    Age = existingAssessment.Age ?? appointment.AgeValue.ToString(),
                    Birthday = appointment.DateOfBirth ?? DateTime.Today.AddYears(-19),
                    HealthFacility = existingAssessment.HealthFacility ?? "Baesa Health Center",
                    FamilyNo = existingAssessment.FamilyNo ?? "C-001",
                    Address = existingAssessment.Address,
                    ContactNumber = existingAssessment.ContactNumber,
                    
                    // Health Program Information
                    IsNHPTS = existingAssessment.IsNHPTS,
                    Is4Ps = existingAssessment.Is4Ps,
                    IsPhilHealthBeneficiaryOnly = existingAssessment.IsPhilHealthBeneficiaryOnly,
                    IsOwnPhilHealth = existingAssessment.IsOwnPhilHealth,
                    PhilHealthPIN = existingAssessment.PhilHealthPIN,
                    
                    // Measurements and Health Information
                    Height = existingAssessment.Height,
                    Weight = existingAssessment.Weight,
                    BMI = existingAssessment.BMI,
                    BMIUnderweight = existingAssessment.BMIUnderweight?.ToString(),
                    BMINormal = existingAssessment.BMINormal?.ToString(),
                    BMIOverweight = existingAssessment.BMIOverweight?.ToString(),
                    BMIObese = existingAssessment.BMIObese?.ToString(),
                    
                    // Immunization Status
                    ImmunizationMR = existingAssessment.ImmunizationMR,
                    ImmunizationTd = existingAssessment.ImmunizationTd,
                    ImmunizationHPV = existingAssessment.ImmunizationHPV,
                    
                    // For Females Only
                    DateOfMenarche = existingAssessment.DateOfMenarche,
                    AgeOfFirstPregnancy = existingAssessment.AgeOfFirstPregnancy,
                    OBScore = existingAssessment.OBScore,
                    
                    // Vital Signs
                    VitalTemp = existingAssessment.VitalTemp,
                    VitalRR = existingAssessment.VitalRR,
                    VitalPR = existingAssessment.VitalPR,
                    VitalBP = existingAssessment.VitalBP,
                    
                    // Medical Information
                    ChiefComplaint = existingAssessment.ChiefComplaint,
                    HistoryOfPresentIllness = existingAssessment.HistoryOfPresentIllness,
                    PhysicalExaminationFindings = existingAssessment.PhysicalExaminationFindings,
                    PastMedicalHistory = existingAssessment.PastMedicalHistory,
                    WorkingDiagnosis = existingAssessment.WorkingDiagnosis,
                    Management = existingAssessment.Management,
                    FamilyHistory = existingAssessment.FamilyHistory,
                    
                    // Referral Information
                    ReferredBy = existingAssessment.ReferredBy,
                    ReferredTo = existingAssessment.ReferredTo,
                    ReasonForReferral = existingAssessment.ReasonForReferral,
                    FollowUpDate = existingAssessment.FollowUpDate,
                    
                    // HOME section
                    HomeEnvironment = existingAssessment.HomeEnvironment,
                    FamilyRelationship = existingAssessment.FamilyRelationship,
                    HomeFamilyProblems = existingAssessment.HomeFamilyProblems,
                    HomeParentalListening = existingAssessment.HomeParentalListening,
                    HomeParentalBlame = existingAssessment.HomeParentalBlame,
                    HomeRunawayThoughts = existingAssessment.HomeRunawayThoughts,
                    HomeFamilyChanges = existingAssessment.HomeFamilyChanges,
                    
                    // EDUCATION section
                    SchoolPerformance = existingAssessment.SchoolPerformance,
                    AttendanceIssues = existingAssessment.AttendanceIssues,
                    CareerPlans = existingAssessment.CareerPlans,
                    EducationCurrentlyStudying = existingAssessment.EducationCurrentlyStudying,
                    EducationWorking = existingAssessment.EducationWorking,
                    EducationSchoolWorkProblems = existingAssessment.EducationSchoolWorkProblems,
                    EducationBullying = existingAssessment.EducationBullying,
                    EducationBullyingExperience = existingAssessment.EducationBullyingExperience,
                    EducationEmployment = existingAssessment.EducationEmployment,
                    
                    // EATING HABITS section
                    DietDescription = existingAssessment.DietDescription,
                    WeightConcerns = existingAssessment.WeightConcerns,
                    EatingDisorderSymptoms = existingAssessment.EatingDisorderSymptoms,
                    EatingBodyImageSatisfaction = existingAssessment.EatingBodyImageSatisfaction,
                    EatingDisorderedEatingBehaviors = existingAssessment.EatingDisorderedEatingBehaviors,
                    EatingWeightComments = existingAssessment.EatingWeightComments,
                    
                    // Missing eating habits checkbox fields
                    EatingVomiting = existingAssessment.EatingVomiting,
                    EatingDietPills = existingAssessment.EatingDietPills,
                    EatingLaxatives = existingAssessment.EatingLaxatives,
                    EatingStarvation = existingAssessment.EatingStarvation,
                    
                    // ACTIVITIES section
                    Hobbies = existingAssessment.Hobbies,
                    PhysicalActivity = existingAssessment.PhysicalActivity,
                    ScreenTime = existingAssessment.ScreenTime,
                    ActivitiesParticipation = existingAssessment.ActivitiesParticipation,
                    ActivitiesRegularExercise = existingAssessment.ActivitiesRegularExercise,
                    ActivitiesScreenTime = existingAssessment.ActivitiesScreenTime,
                    ActivitiesInternetGadgetUse = existingAssessment.ActivitiesInternetGadgetUse,
                    
                    // DRUGS section
                    SubstanceUse = existingAssessment.SubstanceUse,
                    SubstanceType = existingAssessment.SubstanceType,
                    DrugsTobaccoUse = existingAssessment.DrugsTobaccoUse,
                    DrugsAlcoholUse = existingAssessment.DrugsAlcoholUse,
                    DrugsIllicitDrugUse = existingAssessment.DrugsIllicitDrugUse,
                    DrugsStreetDrugs = existingAssessment.DrugsStreetDrugs,
                    
                    // SEXUALITY section
                    DatingRelationships = existingAssessment.DatingRelationships,
                    SexualActivity = existingAssessment.SexualActivity,
                    SexualOrientation = existingAssessment.SexualOrientation,
                    SexualityBodyConcerns = existingAssessment.SexualityBodyConcerns,
                    SexualityHealthConcerns = existingAssessment.SexualityHealthConcerns,
                    SexualityPartnersCount = existingAssessment.SexualityPartnersCount,
                    SexualityIntimateRelationships = existingAssessment.SexualityIntimateRelationships,
                    SexualityPartners = existingAssessment.SexualityPartners,
                    SexualitySexualOrientation = existingAssessment.SexualitySexualOrientation,
                    SexualityPregnancy = existingAssessment.SexualityPregnancy,
                    SexualitySTI = existingAssessment.SexualitySTI,
                    SexualityProtection = existingAssessment.SexualityProtection,
                    SexualityPregnancyExperience = existingAssessment.SexualityPregnancyExperience,
                    SexualitySTIExperience = existingAssessment.SexualitySTIExperience,
                    SexualityProtectionUse = existingAssessment.SexualityProtectionUse,
                    SexualityHarassment = existingAssessment.SexualityHarassment,
                    
                    // Missing sexuality checkbox fields
                    SexualityGay = existingAssessment.SexualityGay,
                    SexualityLesbian = existingAssessment.SexualityLesbian,
                    SexualityBisexual = existingAssessment.SexualityBisexual,
                    
                    // SUICIDE/DEPRESSION section
                    MoodChanges = existingAssessment.MoodChanges,
                    SuicidalThoughts = existingAssessment.SuicidalThoughts,
                    SelfHarmBehavior = existingAssessment.SelfHarmBehavior,
                    SuicideDepressionFeelings = existingAssessment.SuicideDepressionFeelings,
                    SuicideSelfHarmThoughts = existingAssessment.SuicideSelfHarmThoughts,
                    SuicideFamilyHistory = existingAssessment.SuicideFamilyHistory,
                    
                    // SAFETY section
                    FeelsSafeAtHome = existingAssessment.FeelsSafeAtHome,
                    FeelsSafeAtSchool = existingAssessment.FeelsSafeAtSchool,
                    ExperiencedBullying = existingAssessment.ExperiencedBullying,
                    SafetyPhysicalAbuse = existingAssessment.SafetyPhysicalAbuse,
                    SafetyRelationshipViolence = existingAssessment.SafetyRelationshipViolence,
                    SafetyProtectiveGear = existingAssessment.SafetyProtectiveGear,
                    SafetyGunsAtHome = existingAssessment.SafetyGunsAtHome,
                    SafetyWeaponAccess = existingAssessment.SafetyWeaponAccess,
                    
                    // STRENGTHS section
                    PersonalStrengths = existingAssessment.PersonalStrengths,
                    SupportSystems = existingAssessment.SupportSystems,
                    CopingMechanisms = existingAssessment.CopingMechanisms,
                    
                    // Assessment Information
                    AssessmentNotes = existingAssessment.AssessmentNotes,
                    RecommendedActions = existingAssessment.RecommendedActions,
                    FollowUpPlan = existingAssessment.FollowUpPlan,
                    Notes = existingAssessment.Notes,
                    AssessedBy = existingAssessment.AssessedBy
                };

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading HEEADSSS assessment for appointment {AppointmentId}", appointmentId);
                TempData["StatusMessage"] = "Error: Unable to load assessment.";
                return IsDoctorRole() ? RedirectToPage("/Doctor/Consultations") : RedirectToPage("/Nurse/Appointments");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Page();
                }

                // Get existing assessment by UserId (same logic as OnGetAsync)
                // First get the appointment to find the patient's UserId
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.Id == Assessment.AppointmentId);

                if (appointment == null)
                {
                    TempData["StatusMessage"] = "Error: Appointment not found.";
                    return IsDoctorRole() ? RedirectToPage("/Doctor/Consultations") : RedirectToPage("/Nurse/Appointments");
                }

                HEEADSSSAssessment? existingAssessment = null;

                if (appointment.Patient != null)
                {
                    // Look for HEEADSSS assessment by UserId
                    existingAssessment = await _context.HEEADSSSAssessments
                        .Where(a => a.UserId == appointment.Patient.UserId)
                        .OrderByDescending(a => a.CreatedAt)
                        .FirstOrDefaultAsync();
                }

                if (existingAssessment == null)
                {
                    TempData["StatusMessage"] = "Error: Assessment not found.";
                    return IsDoctorRole() ? RedirectToPage("/Doctor/Consultations") : RedirectToPage("/Nurse/Appointments");
                }

                // Update the assessment with all fields
                // Personal Information
                existingAssessment.FullName = Assessment.FullName;
                existingAssessment.Age = Assessment.Age;
                existingAssessment.Gender = Assessment.Gender;
                existingAssessment.Address = Assessment.Address;
                existingAssessment.ContactNumber = Assessment.ContactNumber;
                existingAssessment.HealthFacility = Assessment.HealthFacility;
                existingAssessment.FamilyNo = Assessment.FamilyNo;
                
                // Health Program Information
                existingAssessment.IsNHPTS = Assessment.IsNHPTS;
                existingAssessment.Is4Ps = Assessment.Is4Ps;
                existingAssessment.IsPhilHealthBeneficiaryOnly = Assessment.IsPhilHealthBeneficiaryOnly;
                existingAssessment.IsOwnPhilHealth = Assessment.IsOwnPhilHealth;
                existingAssessment.PhilHealthPIN = Assessment.PhilHealthPIN;
                
                // Measurements and Health Information
                existingAssessment.Height = Assessment.Height;
                existingAssessment.Weight = Assessment.Weight;
                existingAssessment.BMI = Assessment.BMI;
                
                // BMI Status - only one should be true
                existingAssessment.BMIUnderweight = Assessment.BMIUnderweight == "True";
                existingAssessment.BMINormal = Assessment.BMINormal == "True";
                existingAssessment.BMIOverweight = Assessment.BMIOverweight == "True";
                existingAssessment.BMIObese = Assessment.BMIObese == "True";
                
                // Immunization Status
                existingAssessment.ImmunizationMR = Assessment.ImmunizationMR;
                existingAssessment.ImmunizationTd = Assessment.ImmunizationTd;
                existingAssessment.ImmunizationHPV = Assessment.ImmunizationHPV;
                
                // For Females Only
                existingAssessment.DateOfMenarche = Assessment.DateOfMenarche;
                existingAssessment.AgeOfFirstPregnancy = Assessment.AgeOfFirstPregnancy;
                existingAssessment.OBScore = Assessment.OBScore;
                
                // Vital Signs
                existingAssessment.VitalTemp = Assessment.VitalTemp;
                existingAssessment.VitalRR = Assessment.VitalRR;
                existingAssessment.VitalPR = Assessment.VitalPR;
                existingAssessment.VitalBP = Assessment.VitalBP;
                
                // Medical Information
                existingAssessment.ChiefComplaint = Assessment.ChiefComplaint;
                existingAssessment.HistoryOfPresentIllness = Assessment.HistoryOfPresentIllness;
                existingAssessment.PhysicalExaminationFindings = Assessment.PhysicalExaminationFindings;
                existingAssessment.PastMedicalHistory = Assessment.PastMedicalHistory;
                existingAssessment.WorkingDiagnosis = Assessment.WorkingDiagnosis;
                existingAssessment.Management = Assessment.Management;
                existingAssessment.FamilyHistory = Assessment.FamilyHistory;
                
                // Referral Information
                existingAssessment.ReferredBy = Assessment.ReferredBy;
                existingAssessment.ReferredTo = Assessment.ReferredTo;
                existingAssessment.ReasonForReferral = Assessment.ReasonForReferral;
                existingAssessment.FollowUpDate = Assessment.FollowUpDate;
                
                // HOME section
                existingAssessment.HomeEnvironment = Assessment.HomeEnvironment;
                existingAssessment.FamilyRelationship = Assessment.FamilyRelationship;
                existingAssessment.HomeFamilyProblems = Assessment.HomeFamilyProblems;
                existingAssessment.HomeParentalListening = Assessment.HomeParentalListening;
                existingAssessment.HomeParentalBlame = Assessment.HomeParentalBlame;
                existingAssessment.HomeRunawayThoughts = Assessment.HomeRunawayThoughts;
                existingAssessment.HomeFamilyChanges = Assessment.HomeFamilyChanges;
                
                // EDUCATION section
                existingAssessment.SchoolPerformance = Assessment.SchoolPerformance;
                existingAssessment.AttendanceIssues = Assessment.AttendanceIssues;
                existingAssessment.CareerPlans = Assessment.CareerPlans;
                existingAssessment.EducationCurrentlyStudying = Assessment.EducationCurrentlyStudying;
                existingAssessment.EducationWorking = Assessment.EducationWorking;
                existingAssessment.EducationSchoolWorkProblems = Assessment.EducationSchoolWorkProblems;
                existingAssessment.EducationBullying = Assessment.EducationBullying;
                existingAssessment.EducationBullyingExperience = Assessment.EducationBullyingExperience;
                existingAssessment.EducationEmployment = Assessment.EducationEmployment;
                
                // EATING HABITS section
                existingAssessment.DietDescription = Assessment.DietDescription;
                existingAssessment.WeightConcerns = Assessment.WeightConcerns;
                existingAssessment.EatingDisorderSymptoms = Assessment.EatingDisorderSymptoms;
                existingAssessment.EatingBodyImageSatisfaction = Assessment.EatingBodyImageSatisfaction;
                existingAssessment.EatingDisorderedEatingBehaviors = Assessment.EatingDisorderedEatingBehaviors;
                existingAssessment.EatingWeightComments = Assessment.EatingWeightComments;
                
                // Missing eating habits checkbox fields
                existingAssessment.EatingVomiting = Assessment.EatingVomiting;
                existingAssessment.EatingDietPills = Assessment.EatingDietPills;
                existingAssessment.EatingLaxatives = Assessment.EatingLaxatives;
                existingAssessment.EatingStarvation = Assessment.EatingStarvation;
                
                // ACTIVITIES section
                existingAssessment.Hobbies = Assessment.Hobbies;
                existingAssessment.PhysicalActivity = Assessment.PhysicalActivity;
                existingAssessment.ScreenTime = Assessment.ScreenTime;
                existingAssessment.ActivitiesParticipation = Assessment.ActivitiesParticipation;
                existingAssessment.ActivitiesRegularExercise = Assessment.ActivitiesRegularExercise;
                existingAssessment.ActivitiesScreenTime = Assessment.ActivitiesScreenTime;
                existingAssessment.ActivitiesInternetGadgetUse = Assessment.ActivitiesInternetGadgetUse;
                
                // DRUGS section
                existingAssessment.SubstanceUse = Assessment.SubstanceUse;
                existingAssessment.SubstanceType = Assessment.SubstanceType;
                existingAssessment.DrugsTobaccoUse = Assessment.DrugsTobaccoUse;
                existingAssessment.DrugsAlcoholUse = Assessment.DrugsAlcoholUse;
                existingAssessment.DrugsIllicitDrugUse = Assessment.DrugsIllicitDrugUse;
                existingAssessment.DrugsStreetDrugs = Assessment.DrugsStreetDrugs;
                
                // SEXUALITY section
                existingAssessment.DatingRelationships = Assessment.DatingRelationships;
                existingAssessment.SexualActivity = Assessment.SexualActivity;
                existingAssessment.SexualOrientation = Assessment.SexualOrientation;
                existingAssessment.SexualityBodyConcerns = Assessment.SexualityBodyConcerns;
                existingAssessment.SexualityHealthConcerns = Assessment.SexualityHealthConcerns;
                existingAssessment.SexualityPartnersCount = Assessment.SexualityPartnersCount;
                existingAssessment.SexualityIntimateRelationships = Assessment.SexualityIntimateRelationships;
                existingAssessment.SexualityPartners = Assessment.SexualityPartners;
                existingAssessment.SexualitySexualOrientation = Assessment.SexualitySexualOrientation;
                existingAssessment.SexualityPregnancy = Assessment.SexualityPregnancy;
                existingAssessment.SexualitySTI = Assessment.SexualitySTI;
                existingAssessment.SexualityProtection = Assessment.SexualityProtection;
                existingAssessment.SexualityPregnancyExperience = Assessment.SexualityPregnancyExperience;
                existingAssessment.SexualitySTIExperience = Assessment.SexualitySTIExperience;
                existingAssessment.SexualityProtectionUse = Assessment.SexualityProtectionUse;
                existingAssessment.SexualityHarassment = Assessment.SexualityHarassment;
                
                // Missing sexuality checkbox fields
                existingAssessment.SexualityGay = Assessment.SexualityGay;
                existingAssessment.SexualityLesbian = Assessment.SexualityLesbian;
                existingAssessment.SexualityBisexual = Assessment.SexualityBisexual;
                
                // Missing eating habits checkbox fields
                existingAssessment.EatingVomiting = Assessment.EatingVomiting;
                existingAssessment.EatingDietPills = Assessment.EatingDietPills;
                existingAssessment.EatingLaxatives = Assessment.EatingLaxatives;
                existingAssessment.EatingStarvation = Assessment.EatingStarvation;
                
                // SUICIDE/DEPRESSION section
                existingAssessment.MoodChanges = Assessment.MoodChanges;
                existingAssessment.SuicidalThoughts = Assessment.SuicidalThoughts;
                existingAssessment.SelfHarmBehavior = Assessment.SelfHarmBehavior;
                existingAssessment.SuicideDepressionFeelings = Assessment.SuicideDepressionFeelings;
                existingAssessment.SuicideSelfHarmThoughts = Assessment.SuicideSelfHarmThoughts;
                existingAssessment.SuicideFamilyHistory = Assessment.SuicideFamilyHistory;
                
                // SAFETY section
                existingAssessment.FeelsSafeAtHome = Assessment.FeelsSafeAtHome;
                existingAssessment.FeelsSafeAtSchool = Assessment.FeelsSafeAtSchool;
                existingAssessment.ExperiencedBullying = Assessment.ExperiencedBullying;
                existingAssessment.SafetyPhysicalAbuse = Assessment.SafetyPhysicalAbuse;
                existingAssessment.SafetyRelationshipViolence = Assessment.SafetyRelationshipViolence;
                existingAssessment.SafetyProtectiveGear = Assessment.SafetyProtectiveGear;
                existingAssessment.SafetyGunsAtHome = Assessment.SafetyGunsAtHome;
                existingAssessment.SafetyWeaponAccess = Assessment.SafetyWeaponAccess;
                
                // STRENGTHS section
                existingAssessment.PersonalStrengths = Assessment.PersonalStrengths;
                existingAssessment.SupportSystems = Assessment.SupportSystems;
                existingAssessment.CopingMechanisms = Assessment.CopingMechanisms;
                
                // Assessment Information
                existingAssessment.AssessmentNotes = Assessment.AssessmentNotes;
                existingAssessment.RecommendedActions = Assessment.RecommendedActions;
                existingAssessment.FollowUpPlan = Assessment.FollowUpPlan;
                existingAssessment.Notes = Assessment.Notes;
                existingAssessment.AssessedBy = Assessment.AssessedBy;
                existingAssessment.UpdatedAt = DateTime.Now;

                // Note: Encryption is handled automatically by EncryptedDbContext.SaveChangesAsync()
                await _context.SaveChangesAsync();

                _logger.LogInformation("HEEADSSS assessment updated successfully for appointment {AppointmentId}", Assessment.AppointmentId);
                TempData["StatusMessage"] = "HEEADSSS assessment updated successfully.";
                
                return IsDoctorRole() ? RedirectToPage("/Doctor/Consultation", new { id = Assessment.AppointmentId ?? 0 }) : RedirectToPage("/Nurse/AppointmentDetails", new { id = Assessment.AppointmentId ?? 0 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating HEEADSSS assessment for appointment {AppointmentId}", Assessment.AppointmentId);
                TempData["StatusMessage"] = "Error: Unable to update assessment.";
                return Page();
            }
        }

        // Handler for AJAX calls to generate family number
        public async Task<IActionResult> OnPostGenerateFamilyNumberAsync([FromBody] GenerateFamilyNumberRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return new JsonResult(new { success = false, error = "User not found" });
                }

                var lastName = request.LastName?.Trim();
                if (string.IsNullOrEmpty(lastName))
                {
                    return new JsonResult(new { success = false, error = "Last name is required" });
                }

                // Check if a family number already exists for this last name
                var existingAssessment = await _context.HEEADSSSAssessments
                    .Where(a => a.FamilyNo != null && !string.IsNullOrEmpty(a.FamilyNo))
                    .FirstOrDefaultAsync();

                string familyNo;
                bool isPreexisting = false;

                if (existingAssessment != null && !string.IsNullOrEmpty(existingAssessment.FamilyNo))
                {
                    // Decrypt the existing family number to check
                    var decryptedFamilyNo = _encryptionService.DecryptForUser(existingAssessment.FamilyNo, User);
                    if (decryptedFamilyNo != null && decryptedFamilyNo.StartsWith($"C-{lastName.ToUpper()}", StringComparison.OrdinalIgnoreCase))
                    {
                        familyNo = decryptedFamilyNo;
                        isPreexisting = true;
                    }
                    else
                    {
                        // Generate new family number
                        familyNo = $"C-{lastName.ToUpper()}-{DateTime.Now:yyyyMMddHHmmss}";
                    }
                }
                else
                {
                    // Generate new family number
                    familyNo = $"C-{lastName.ToUpper()}-{DateTime.Now:yyyyMMddHHmmss}";
                }

                _logger.LogInformation("Generated family number: {FamilyNo} for user {UserId}", familyNo, user.Id);

                return new JsonResult(new { 
                    success = true, 
                    familyNo = familyNo,
                    isPreexisting = isPreexisting
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating family number for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                return new JsonResult(new { success = false, error = "Failed to generate family number" });
            }
        }

        public class GenerateFamilyNumberRequest
        {
            public string LastName { get; set; }
        }
    }
}