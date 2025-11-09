using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Barangay.Data;
using Barangay.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Barangay.Services;
using System.Security.Claims;

namespace Barangay.Pages.Nurse
{
    // Authorization: Admin can edit all forms, Nurse/Doctor can edit their assigned forms
    // Patients cannot access edit pages - they can only view/read their submitted forms
    [Authorize(Roles = "Nurse,Head Nurse,Doctor,Head Doctor,Admin")]
    public class EditNCDAssessmentModel : PageModel
    {
        private readonly EncryptedDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<EditNCDAssessmentModel> _logger;
        private readonly IPermissionService _permissionService;
        private readonly IDataEncryptionService _encryptionService;

        public EditNCDAssessmentModel(
            EncryptedDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<EditNCDAssessmentModel> logger,
            IPermissionService permissionService,
            IDataEncryptionService encryptionService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _permissionService = permissionService;
            _encryptionService = encryptionService;
        }

        [BindProperty]
        public NCDRiskAssessmentViewModel NCDRiskAssessment { get; set; }

        public int AppointmentId { get; set; }
        public string UserId { get; set; }
        public string PatientName { get; set; }

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

        public async Task<IActionResult> OnGetAsync(int? appointmentId)
        {
            try
            {
                if (appointmentId == null)
                {
                    _logger.LogWarning("Appointment ID not provided to EditN assessment");
                    TempData["StatusMessage"] = "Error: Appointment ID must be provided.";
                    return IsDoctorRole() ? RedirectToPage("/Doctor/Consultations") : RedirectToPage("/Nurse/Appointments");
                }
                
                // Users with appropriate roles have permission to edit assessments
                _logger.LogInformation("User editing NCD assessment for appointment {AppointmentId}", appointmentId);

                var assessment = await _context.NCDRiskAssessments
                    .FirstOrDefaultAsync(n => n.AppointmentId == appointmentId);

                if (assessment == null)
                {
                    TempData["StatusMessage"] = "Error: Assessment not found.";
                    return IsDoctorRole() ? RedirectToPage("/Doctor/Consultation", new { id = appointmentId }) : RedirectToPage("/Nurse/AppointmentDetails", new { id = appointmentId });
                }

                // Decrypt sensitive data for display
                try
                {
                    _logger.LogInformation("=== DECRYPTION DEBUGGING STARTED ===");
                    _logger.LogInformation("Attempting to decrypt assessment data for user {User}", User.Identity?.Name);
                    _logger.LogInformation("User roles: {Roles}", string.Join(", ", User.Claims.Where(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Select(c => c.Value)));
                    _logger.LogInformation("Can user decrypt: {CanDecrypt}", _encryptionService.CanUserDecrypt(User));
                    
                    // DEBUGGING: Log encrypted values before decryption
                    _logger.LogInformation("=== ENCRYPTED VALUES BEFORE DECRYPTION ===");
                    _logger.LogInformation("HasDiabetes: '{HasDiabetes}'", assessment.HasDiabetes?.Substring(0, Math.Min(20, assessment.HasDiabetes?.Length ?? 0)) + "...");
                    _logger.LogInformation("HasChestPain: '{HasChestPain}'", assessment.HasChestPain?.Substring(0, Math.Min(20, assessment.HasChestPain?.Length ?? 0)) + "...");
                    _logger.LogInformation("DrinksAlcohol: '{DrinksAlcohol}'", assessment.DrinksAlcohol?.Substring(0, Math.Min(20, assessment.DrinksAlcohol?.Length ?? 0)) + "...");
                    _logger.LogInformation("HasHistoryOfSmoking: '{HasHistoryOfSmoking}'", assessment.HasHistoryOfSmoking?.Substring(0, Math.Min(20, assessment.HasHistoryOfSmoking?.Length ?? 0)) + "...");
                    _logger.LogInformation("HasStress: '{HasStress}'", assessment.HasStress?.Substring(0, Math.Min(20, assessment.HasStress?.Length ?? 0)) + "...");
                    _logger.LogInformation("EatsVegetablesDaily: '{EatsVegetablesDaily}'", assessment.EatsVegetablesDaily?.Substring(0, Math.Min(20, assessment.EatsVegetablesDaily?.Length ?? 0)) + "...");
                    _logger.LogInformation("FamilyHistoryHeartDiseaseFather: '{FamilyHistoryHeartDiseaseFather}'", assessment.FamilyHistoryHeartDiseaseFather?.Substring(0, Math.Min(20, assessment.FamilyHistoryHeartDiseaseFather?.Length ?? 0)) + "...");
                    
                    assessment.DecryptSensitiveData(_encryptionService, User);
                    
                    // DEBUGGING: Log decrypted values after decryption
                    _logger.LogInformation("=== DECRYPTED VALUES AFTER DECRYPTION ===");
                    _logger.LogInformation("HasDiabetes: '{HasDiabetes}'", assessment.HasDiabetes);
                    _logger.LogInformation("HasChestPain: '{HasChestPain}'", assessment.HasChestPain);
                    _logger.LogInformation("DrinksAlcohol: '{DrinksAlcohol}'", assessment.DrinksAlcohol);
                    _logger.LogInformation("HasHistoryOfSmoking: '{HasHistoryOfSmoking}'", assessment.HasHistoryOfSmoking);
                    _logger.LogInformation("HasStress: '{HasStress}'", assessment.HasStress);
                    _logger.LogInformation("EatsVegetablesDaily: '{EatsVegetablesDaily}'", assessment.EatsVegetablesDaily);
                    _logger.LogInformation("FamilyHistoryHeartDiseaseFather: '{FamilyHistoryHeartDiseaseFather}'", assessment.FamilyHistoryHeartDiseaseFather);
                    
                    _logger.LogInformation("Assessment data decryption completed successfully");
                    
                    // Manual decryption fallback for all NCD fields
                    // Personal Information
                    if (!string.IsNullOrEmpty(assessment.FirstName) && _encryptionService.IsEncrypted(assessment.FirstName))
                    {
                        assessment.FirstName = _encryptionService.DecryptForUser(assessment.FirstName, User);
                    }
                    
                    // Decrypt boolean fields for checkbox binding
                    if (!string.IsNullOrEmpty(assessment.HasChestPain) && _encryptionService.IsEncrypted(assessment.HasChestPain))
                    {
                        assessment.HasChestPain = _encryptionService.DecryptForUser(assessment.HasChestPain, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.ChestPainSpreadsToArm) && _encryptionService.IsEncrypted(assessment.ChestPainSpreadsToArm))
                    {
                        assessment.ChestPainSpreadsToArm = _encryptionService.DecryptForUser(assessment.ChestPainSpreadsToArm, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.NumbnessWhenWalkingFast) && _encryptionService.IsEncrypted(assessment.NumbnessWhenWalkingFast))
                    {
                        assessment.NumbnessWhenWalkingFast = _encryptionService.DecryptForUser(assessment.NumbnessWhenWalkingFast, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.PainRelievedWithRest) && _encryptionService.IsEncrypted(assessment.PainRelievedWithRest))
                    {
                        assessment.PainRelievedWithRest = _encryptionService.DecryptForUser(assessment.PainRelievedWithRest, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.LossOfConsciousnessLessThan10Min) && _encryptionService.IsEncrypted(assessment.LossOfConsciousnessLessThan10Min))
                    {
                        assessment.LossOfConsciousnessLessThan10Min = _encryptionService.DecryptForUser(assessment.LossOfConsciousnessLessThan10Min, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.PainLastsMoreThan30Min) && _encryptionService.IsEncrypted(assessment.PainLastsMoreThan30Min))
                    {
                        assessment.PainLastsMoreThan30Min = _encryptionService.DecryptForUser(assessment.PainLastsMoreThan30Min, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.SeeDoctorIfYes) && _encryptionService.IsEncrypted(assessment.SeeDoctorIfYes))
                    {
                        assessment.SeeDoctorIfYes = _encryptionService.DecryptForUser(assessment.SeeDoctorIfYes, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasPolyuria) && _encryptionService.IsEncrypted(assessment.HasPolyuria))
                    {
                        assessment.HasPolyuria = _encryptionService.DecryptForUser(assessment.HasPolyuria, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasPolydipsia) && _encryptionService.IsEncrypted(assessment.HasPolydipsia))
                    {
                        assessment.HasPolydipsia = _encryptionService.DecryptForUser(assessment.HasPolydipsia, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasPolyphagia) && _encryptionService.IsEncrypted(assessment.HasPolyphagia))
                    {
                        assessment.HasPolyphagia = _encryptionService.DecryptForUser(assessment.HasPolyphagia, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasWeightLoss) && _encryptionService.IsEncrypted(assessment.HasWeightLoss))
                    {
                        assessment.HasWeightLoss = _encryptionService.DecryptForUser(assessment.HasWeightLoss, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasUrineProtein) && _encryptionService.IsEncrypted(assessment.HasUrineProtein))
                    {
                        assessment.HasUrineProtein = _encryptionService.DecryptForUser(assessment.HasUrineProtein, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasUrineKetones) && _encryptionService.IsEncrypted(assessment.HasUrineKetones))
                    {
                        assessment.HasUrineKetones = _encryptionService.DecryptForUser(assessment.HasUrineKetones, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.BreastCancerScreened) && _encryptionService.IsEncrypted(assessment.BreastCancerScreened))
                    {
                        assessment.BreastCancerScreened = _encryptionService.DecryptForUser(assessment.BreastCancerScreened, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.CervicalCancerScreened) && _encryptionService.IsEncrypted(assessment.CervicalCancerScreened))
                    {
                        assessment.CervicalCancerScreened = _encryptionService.DecryptForUser(assessment.CervicalCancerScreened, User);
                    }
                    
                    // Decrypt additional boolean fields that are still encrypted
                    if (!string.IsNullOrEmpty(assessment.EatsVegetablesDaily) && _encryptionService.IsEncrypted(assessment.EatsVegetablesDaily))
                    {
                        assessment.EatsVegetablesDaily = _encryptionService.DecryptForUser(assessment.EatsVegetablesDaily, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.EatsFruitsDaily) && _encryptionService.IsEncrypted(assessment.EatsFruitsDaily))
                    {
                        assessment.EatsFruitsDaily = _encryptionService.DecryptForUser(assessment.EatsFruitsDaily, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.EatsFishDaily) && _encryptionService.IsEncrypted(assessment.EatsFishDaily))
                    {
                        assessment.EatsFishDaily = _encryptionService.DecryptForUser(assessment.EatsFishDaily, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.EatsMeatDaily) && _encryptionService.IsEncrypted(assessment.EatsMeatDaily))
                    {
                        assessment.EatsMeatDaily = _encryptionService.DecryptForUser(assessment.EatsMeatDaily, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasUnhealthyDiet) && _encryptionService.IsEncrypted(assessment.HasUnhealthyDiet))
                    {
                        assessment.HasUnhealthyDiet = _encryptionService.DecryptForUser(assessment.HasUnhealthyDiet, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.EatsFattyFoodMoreThan2TimesPerWeek) && _encryptionService.IsEncrypted(assessment.EatsFattyFoodMoreThan2TimesPerWeek))
                    {
                        assessment.EatsFattyFoodMoreThan2TimesPerWeek = _encryptionService.DecryptForUser(assessment.EatsFattyFoodMoreThan2TimesPerWeek, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.EatsSweetFoodMoreThan2TimesPerWeek) && _encryptionService.IsEncrypted(assessment.EatsSweetFoodMoreThan2TimesPerWeek))
                    {
                        assessment.EatsSweetFoodMoreThan2TimesPerWeek = _encryptionService.DecryptForUser(assessment.EatsSweetFoodMoreThan2TimesPerWeek, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.EatsOilyFoodMoreThan2TimesPerWeek) && _encryptionService.IsEncrypted(assessment.EatsOilyFoodMoreThan2TimesPerWeek))
                    {
                        assessment.EatsOilyFoodMoreThan2TimesPerWeek = _encryptionService.DecryptForUser(assessment.EatsOilyFoodMoreThan2TimesPerWeek, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasHighSaltIntake) && _encryptionService.IsEncrypted(assessment.HasHighSaltIntake))
                    {
                        assessment.HasHighSaltIntake = _encryptionService.DecryptForUser(assessment.HasHighSaltIntake, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.DrinksAlcohol) && _encryptionService.IsEncrypted(assessment.DrinksAlcohol))
                    {
                        assessment.DrinksAlcohol = _encryptionService.DecryptForUser(assessment.DrinksAlcohol, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.DrinksBeer) && _encryptionService.IsEncrypted(assessment.DrinksBeer))
                    {
                        assessment.DrinksBeer = _encryptionService.DecryptForUser(assessment.DrinksBeer, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.DrinksWine) && _encryptionService.IsEncrypted(assessment.DrinksWine))
                    {
                        assessment.DrinksWine = _encryptionService.DecryptForUser(assessment.DrinksWine, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.DrinksWhiskyGinBrandy) && _encryptionService.IsEncrypted(assessment.DrinksWhiskyGinBrandy))
                    {
                        assessment.DrinksWhiskyGinBrandy = _encryptionService.DecryptForUser(assessment.DrinksWhiskyGinBrandy, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.AlcoholAmount1Bottle320ml) && _encryptionService.IsEncrypted(assessment.AlcoholAmount1Bottle320ml))
                    {
                        assessment.AlcoholAmount1Bottle320ml = _encryptionService.DecryptForUser(assessment.AlcoholAmount1Bottle320ml, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.AlcoholAmount2Bottle640ml) && _encryptionService.IsEncrypted(assessment.AlcoholAmount2Bottle640ml))
                    {
                        assessment.AlcoholAmount2Bottle640ml = _encryptionService.DecryptForUser(assessment.AlcoholAmount2Bottle640ml, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.AlcoholAmountLessThan3Shot45ml) && _encryptionService.IsEncrypted(assessment.AlcoholAmountLessThan3Shot45ml))
                    {
                        assessment.AlcoholAmountLessThan3Shot45ml = _encryptionService.DecryptForUser(assessment.AlcoholAmountLessThan3Shot45ml, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.AlcoholAmount3to4WineGlasses300ml) && _encryptionService.IsEncrypted(assessment.AlcoholAmount3to4WineGlasses300ml))
                    {
                        assessment.AlcoholAmount3to4WineGlasses300ml = _encryptionService.DecryptForUser(assessment.AlcoholAmount3to4WineGlasses300ml, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.AlcoholAmountMoreThan4Shots75ml) && _encryptionService.IsEncrypted(assessment.AlcoholAmountMoreThan4Shots75ml))
                    {
                        assessment.AlcoholAmountMoreThan4Shots75ml = _encryptionService.DecryptForUser(assessment.AlcoholAmountMoreThan4Shots75ml, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.BeerConsumption3) && _encryptionService.IsEncrypted(assessment.BeerConsumption3))
                    {
                        assessment.BeerConsumption3 = _encryptionService.DecryptForUser(assessment.BeerConsumption3, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.WineConsumption2) && _encryptionService.IsEncrypted(assessment.WineConsumption2))
                    {
                        assessment.WineConsumption2 = _encryptionService.DecryptForUser(assessment.WineConsumption2, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.AlcoholPerOccasion) && _encryptionService.IsEncrypted(assessment.AlcoholPerOccasion))
                    {
                        assessment.AlcoholPerOccasion = _encryptionService.DecryptForUser(assessment.AlcoholPerOccasion, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.AlcoholFrequency1to3TimesPerWeek) && _encryptionService.IsEncrypted(assessment.AlcoholFrequency1to3TimesPerWeek))
                    {
                        assessment.AlcoholFrequency1to3TimesPerWeek = _encryptionService.DecryptForUser(assessment.AlcoholFrequency1to3TimesPerWeek, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.AlcoholFrequencyMoreThan4TimesPerWeek) && _encryptionService.IsEncrypted(assessment.AlcoholFrequencyMoreThan4TimesPerWeek))
                    {
                        assessment.AlcoholFrequencyMoreThan4TimesPerWeek = _encryptionService.DecryptForUser(assessment.AlcoholFrequencyMoreThan4TimesPerWeek, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.IsBingeDrinker) && _encryptionService.IsEncrypted(assessment.IsBingeDrinker))
                    {
                        assessment.IsBingeDrinker = _encryptionService.DecryptForUser(assessment.IsBingeDrinker, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.ModerateIntensityExercise) && _encryptionService.IsEncrypted(assessment.ModerateIntensityExercise))
                    {
                        assessment.ModerateIntensityExercise = _encryptionService.DecryptForUser(assessment.ModerateIntensityExercise, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.VigorousIntensityExercise) && _encryptionService.IsEncrypted(assessment.VigorousIntensityExercise))
                    {
                        assessment.VigorousIntensityExercise = _encryptionService.DecryptForUser(assessment.VigorousIntensityExercise, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.CombinationExercise) && _encryptionService.IsEncrypted(assessment.CombinationExercise))
                    {
                        assessment.CombinationExercise = _encryptionService.DecryptForUser(assessment.CombinationExercise, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.InsufficientPhysicalActivity) && _encryptionService.IsEncrypted(assessment.InsufficientPhysicalActivity))
                    {
                        assessment.InsufficientPhysicalActivity = _encryptionService.DecryptForUser(assessment.InsufficientPhysicalActivity, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FormerSmoker) && _encryptionService.IsEncrypted(assessment.FormerSmoker))
                    {
                        assessment.FormerSmoker = _encryptionService.DecryptForUser(assessment.FormerSmoker, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.NeverSmokedButExposedToSmoke) && _encryptionService.IsEncrypted(assessment.NeverSmokedButExposedToSmoke))
                    {
                        var decryptedValue = _encryptionService.DecryptForUser(assessment.NeverSmokedButExposedToSmoke, User);
                        _logger.LogInformation("NeverSmokedButExposedToSmoke decryption: Original='{Original}', Decrypted='{Decrypted}'", 
                            assessment.NeverSmokedButExposedToSmoke?.Substring(0, Math.Min(20, assessment.NeverSmokedButExposedToSmoke?.Length ?? 0)) + "...", 
                            decryptedValue);
                        assessment.NeverSmokedButExposedToSmoke = decryptedValue;
                    }
                    if (!string.IsNullOrEmpty(assessment.HasHistoryOfSmoking) && _encryptionService.IsEncrypted(assessment.HasHistoryOfSmoking))
                    {
                        assessment.HasHistoryOfSmoking = _encryptionService.DecryptForUser(assessment.HasHistoryOfSmoking, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasStress) && _encryptionService.IsEncrypted(assessment.HasStress))
                    {
                        assessment.HasStress = _encryptionService.DecryptForUser(assessment.HasStress, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.LastName) && _encryptionService.IsEncrypted(assessment.LastName))
                    {
                        assessment.LastName = _encryptionService.DecryptForUser(assessment.LastName, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.MiddleName) && _encryptionService.IsEncrypted(assessment.MiddleName))
                    {
                        assessment.MiddleName = _encryptionService.DecryptForUser(assessment.MiddleName, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HealthFacility) && _encryptionService.IsEncrypted(assessment.HealthFacility))
                    {
                        assessment.HealthFacility = _encryptionService.DecryptForUser(assessment.HealthFacility, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyNo) && _encryptionService.IsEncrypted(assessment.FamilyNo))
                    {
                        assessment.FamilyNo = _encryptionService.DecryptForUser(assessment.FamilyNo, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.Address) && _encryptionService.IsEncrypted(assessment.Address))
                    {
                        assessment.Address = _encryptionService.DecryptForUser(assessment.Address, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.Barangay) && _encryptionService.IsEncrypted(assessment.Barangay))
                    {
                        assessment.Barangay = _encryptionService.DecryptForUser(assessment.Barangay, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.Telepono) && _encryptionService.IsEncrypted(assessment.Telepono))
                    {
                        assessment.Telepono = _encryptionService.DecryptForUser(assessment.Telepono, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.Edad) && _encryptionService.IsEncrypted(assessment.Edad))
                    {
                        assessment.Edad = _encryptionService.DecryptForUser(assessment.Edad, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.Relihiyon) && _encryptionService.IsEncrypted(assessment.Relihiyon))
                    {
                        assessment.Relihiyon = _encryptionService.DecryptForUser(assessment.Relihiyon, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.Occupation) && _encryptionService.IsEncrypted(assessment.Occupation))
                    {
                        assessment.Occupation = _encryptionService.DecryptForUser(assessment.Occupation, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.CivilStatus) && _encryptionService.IsEncrypted(assessment.CivilStatus))
                    {
                        assessment.CivilStatus = _encryptionService.DecryptForUser(assessment.CivilStatus, User);
                    }
                    // Birthday is now a DateTime, no decryption needed
                    if (!string.IsNullOrEmpty(assessment.Kasarian) && _encryptionService.IsEncrypted(assessment.Kasarian))
                    {
                        assessment.Kasarian = _encryptionService.DecryptForUser(assessment.Kasarian, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.IDNumber) && _encryptionService.IsEncrypted(assessment.IDNumber))
                    {
                        assessment.IDNumber = _encryptionService.DecryptForUser(assessment.IDNumber, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.IDNo) && _encryptionService.IsEncrypted(assessment.IDNo))
                    {
                        assessment.IDNo = _encryptionService.DecryptForUser(assessment.IDNo, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.DateOfAssessment) && _encryptionService.IsEncrypted(assessment.DateOfAssessment))
                    {
                        assessment.DateOfAssessment = _encryptionService.DecryptForUser(assessment.DateOfAssessment, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.AssessmentDate) && _encryptionService.IsEncrypted(assessment.AssessmentDate))
                    {
                        assessment.AssessmentDate = _encryptionService.DecryptForUser(assessment.AssessmentDate, User);
                    }
                    
                    // Medical History
                    if (!string.IsNullOrEmpty(assessment.HasDiabetes) && _encryptionService.IsEncrypted(assessment.HasDiabetes))
                    {
                        assessment.HasDiabetes = _encryptionService.DecryptForUser(assessment.HasDiabetes, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasHypertension) && _encryptionService.IsEncrypted(assessment.HasHypertension))
                    {
                        assessment.HasHypertension = _encryptionService.DecryptForUser(assessment.HasHypertension, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasCancer) && _encryptionService.IsEncrypted(assessment.HasCancer))
                    {
                        assessment.HasCancer = _encryptionService.DecryptForUser(assessment.HasCancer, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.CancerSite) && _encryptionService.IsEncrypted(assessment.CancerSite))
                    {
                        assessment.CancerSite = _encryptionService.DecryptForUser(assessment.CancerSite, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasCOPD) && _encryptionService.IsEncrypted(assessment.HasCOPD))
                    {
                        assessment.HasCOPD = _encryptionService.DecryptForUser(assessment.HasCOPD, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.COPDYear) && _encryptionService.IsEncrypted(assessment.COPDYear))
                    {
                        assessment.COPDYear = _encryptionService.DecryptForUser(assessment.COPDYear, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.COPDMedication) && _encryptionService.IsEncrypted(assessment.COPDMedication))
                    {
                        assessment.COPDMedication = _encryptionService.DecryptForUser(assessment.COPDMedication, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasLungDisease) && _encryptionService.IsEncrypted(assessment.HasLungDisease))
                    {
                        assessment.HasLungDisease = _encryptionService.DecryptForUser(assessment.HasLungDisease, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasEyeDisease) && _encryptionService.IsEncrypted(assessment.HasEyeDisease))
                    {
                        assessment.HasEyeDisease = _encryptionService.DecryptForUser(assessment.HasEyeDisease, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.CancerType) && _encryptionService.IsEncrypted(assessment.CancerType))
                    {
                        assessment.CancerType = _encryptionService.DecryptForUser(assessment.CancerType, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.CancerYear) && _encryptionService.IsEncrypted(assessment.CancerYear))
                    {
                        assessment.CancerYear = _encryptionService.DecryptForUser(assessment.CancerYear, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.CancerMedication) && _encryptionService.IsEncrypted(assessment.CancerMedication))
                    {
                        assessment.CancerMedication = _encryptionService.DecryptForUser(assessment.CancerMedication, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.DiabetesYear) && _encryptionService.IsEncrypted(assessment.DiabetesYear))
                    {
                        assessment.DiabetesYear = _encryptionService.DecryptForUser(assessment.DiabetesYear, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.DiabetesMedication) && _encryptionService.IsEncrypted(assessment.DiabetesMedication))
                    {
                        assessment.DiabetesMedication = _encryptionService.DecryptForUser(assessment.DiabetesMedication, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HypertensionYear) && _encryptionService.IsEncrypted(assessment.HypertensionYear))
                    {
                        assessment.HypertensionYear = _encryptionService.DecryptForUser(assessment.HypertensionYear, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HypertensionMedication) && _encryptionService.IsEncrypted(assessment.HypertensionMedication))
                    {
                        assessment.HypertensionMedication = _encryptionService.DecryptForUser(assessment.HypertensionMedication, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.LungDiseaseYear) && _encryptionService.IsEncrypted(assessment.LungDiseaseYear))
                    {
                        assessment.LungDiseaseYear = _encryptionService.DecryptForUser(assessment.LungDiseaseYear, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.LungDiseaseMedication) && _encryptionService.IsEncrypted(assessment.LungDiseaseMedication))
                    {
                        assessment.LungDiseaseMedication = _encryptionService.DecryptForUser(assessment.LungDiseaseMedication, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.EyeDiseaseYear) && _encryptionService.IsEncrypted(assessment.EyeDiseaseYear))
                    {
                        assessment.EyeDiseaseYear = _encryptionService.DecryptForUser(assessment.EyeDiseaseYear, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.EyeDiseaseMedication) && _encryptionService.IsEncrypted(assessment.EyeDiseaseMedication))
                    {
                        assessment.EyeDiseaseMedication = _encryptionService.DecryptForUser(assessment.EyeDiseaseMedication, User);
                    }
                    
                    // Family History
                    if (!string.IsNullOrEmpty(assessment.FamilyHasHypertension) && _encryptionService.IsEncrypted(assessment.FamilyHasHypertension))
                    {
                        assessment.FamilyHasHypertension = _encryptionService.DecryptForUser(assessment.FamilyHasHypertension, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHasHeartDisease) && _encryptionService.IsEncrypted(assessment.FamilyHasHeartDisease))
                    {
                        assessment.FamilyHasHeartDisease = _encryptionService.DecryptForUser(assessment.FamilyHasHeartDisease, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHasStroke) && _encryptionService.IsEncrypted(assessment.FamilyHasStroke))
                    {
                        assessment.FamilyHasStroke = _encryptionService.DecryptForUser(assessment.FamilyHasStroke, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHasDiabetes) && _encryptionService.IsEncrypted(assessment.FamilyHasDiabetes))
                    {
                        assessment.FamilyHasDiabetes = _encryptionService.DecryptForUser(assessment.FamilyHasDiabetes, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHasCancer) && _encryptionService.IsEncrypted(assessment.FamilyHasCancer))
                    {
                        assessment.FamilyHasCancer = _encryptionService.DecryptForUser(assessment.FamilyHasCancer, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHasKidneyDisease) && _encryptionService.IsEncrypted(assessment.FamilyHasKidneyDisease))
                    {
                        assessment.FamilyHasKidneyDisease = _encryptionService.DecryptForUser(assessment.FamilyHasKidneyDisease, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHasOtherDisease) && _encryptionService.IsEncrypted(assessment.FamilyHasOtherDisease))
                    {
                        assessment.FamilyHasOtherDisease = _encryptionService.DecryptForUser(assessment.FamilyHasOtherDisease, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyOtherDiseaseDetails) && _encryptionService.IsEncrypted(assessment.FamilyOtherDiseaseDetails))
                    {
                        assessment.FamilyOtherDiseaseDetails = _encryptionService.DecryptForUser(assessment.FamilyOtherDiseaseDetails, User);
                    }
                    
                    // Detailed Family History
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryCancerFather) && _encryptionService.IsEncrypted(assessment.FamilyHistoryCancerFather))
                    {
                        assessment.FamilyHistoryCancerFather = _encryptionService.DecryptForUser(assessment.FamilyHistoryCancerFather, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryCancerMother) && _encryptionService.IsEncrypted(assessment.FamilyHistoryCancerMother))
                    {
                        assessment.FamilyHistoryCancerMother = _encryptionService.DecryptForUser(assessment.FamilyHistoryCancerMother, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryCancerSibling) && _encryptionService.IsEncrypted(assessment.FamilyHistoryCancerSibling))
                    {
                        assessment.FamilyHistoryCancerSibling = _encryptionService.DecryptForUser(assessment.FamilyHistoryCancerSibling, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryDiabetesFather) && _encryptionService.IsEncrypted(assessment.FamilyHistoryDiabetesFather))
                    {
                        assessment.FamilyHistoryDiabetesFather = _encryptionService.DecryptForUser(assessment.FamilyHistoryDiabetesFather, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryDiabetesMother) && _encryptionService.IsEncrypted(assessment.FamilyHistoryDiabetesMother))
                    {
                        assessment.FamilyHistoryDiabetesMother = _encryptionService.DecryptForUser(assessment.FamilyHistoryDiabetesMother, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryDiabetesSibling) && _encryptionService.IsEncrypted(assessment.FamilyHistoryDiabetesSibling))
                    {
                        assessment.FamilyHistoryDiabetesSibling = _encryptionService.DecryptForUser(assessment.FamilyHistoryDiabetesSibling, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryHeartDiseaseFather) && _encryptionService.IsEncrypted(assessment.FamilyHistoryHeartDiseaseFather))
                    {
                        assessment.FamilyHistoryHeartDiseaseFather = _encryptionService.DecryptForUser(assessment.FamilyHistoryHeartDiseaseFather, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryHeartDiseaseMother) && _encryptionService.IsEncrypted(assessment.FamilyHistoryHeartDiseaseMother))
                    {
                        assessment.FamilyHistoryHeartDiseaseMother = _encryptionService.DecryptForUser(assessment.FamilyHistoryHeartDiseaseMother, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryHeartDiseaseSibling) && _encryptionService.IsEncrypted(assessment.FamilyHistoryHeartDiseaseSibling))
                    {
                        assessment.FamilyHistoryHeartDiseaseSibling = _encryptionService.DecryptForUser(assessment.FamilyHistoryHeartDiseaseSibling, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryLungDiseaseFather) && _encryptionService.IsEncrypted(assessment.FamilyHistoryLungDiseaseFather))
                    {
                        assessment.FamilyHistoryLungDiseaseFather = _encryptionService.DecryptForUser(assessment.FamilyHistoryLungDiseaseFather, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryLungDiseaseMother) && _encryptionService.IsEncrypted(assessment.FamilyHistoryLungDiseaseMother))
                    {
                        assessment.FamilyHistoryLungDiseaseMother = _encryptionService.DecryptForUser(assessment.FamilyHistoryLungDiseaseMother, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryLungDiseaseSibling) && _encryptionService.IsEncrypted(assessment.FamilyHistoryLungDiseaseSibling))
                    {
                        assessment.FamilyHistoryLungDiseaseSibling = _encryptionService.DecryptForUser(assessment.FamilyHistoryLungDiseaseSibling, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryStrokeFather) && _encryptionService.IsEncrypted(assessment.FamilyHistoryStrokeFather))
                    {
                        assessment.FamilyHistoryStrokeFather = _encryptionService.DecryptForUser(assessment.FamilyHistoryStrokeFather, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryStrokeMother) && _encryptionService.IsEncrypted(assessment.FamilyHistoryStrokeMother))
                    {
                        assessment.FamilyHistoryStrokeMother = _encryptionService.DecryptForUser(assessment.FamilyHistoryStrokeMother, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryStrokeSibling) && _encryptionService.IsEncrypted(assessment.FamilyHistoryStrokeSibling))
                    {
                        assessment.FamilyHistoryStrokeSibling = _encryptionService.DecryptForUser(assessment.FamilyHistoryStrokeSibling, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryKidneyDiseaseFather) && _encryptionService.IsEncrypted(assessment.FamilyHistoryKidneyDiseaseFather))
                    {
                        assessment.FamilyHistoryKidneyDiseaseFather = _encryptionService.DecryptForUser(assessment.FamilyHistoryKidneyDiseaseFather, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryKidneyDiseaseMother) && _encryptionService.IsEncrypted(assessment.FamilyHistoryKidneyDiseaseMother))
                    {
                        assessment.FamilyHistoryKidneyDiseaseMother = _encryptionService.DecryptForUser(assessment.FamilyHistoryKidneyDiseaseMother, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryKidneyDiseaseSibling) && _encryptionService.IsEncrypted(assessment.FamilyHistoryKidneyDiseaseSibling))
                    {
                        assessment.FamilyHistoryKidneyDiseaseSibling = _encryptionService.DecryptForUser(assessment.FamilyHistoryKidneyDiseaseSibling, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryEyeDiseaseFather) && _encryptionService.IsEncrypted(assessment.FamilyHistoryEyeDiseaseFather))
                    {
                        assessment.FamilyHistoryEyeDiseaseFather = _encryptionService.DecryptForUser(assessment.FamilyHistoryEyeDiseaseFather, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryEyeDiseaseMother) && _encryptionService.IsEncrypted(assessment.FamilyHistoryEyeDiseaseMother))
                    {
                        assessment.FamilyHistoryEyeDiseaseMother = _encryptionService.DecryptForUser(assessment.FamilyHistoryEyeDiseaseMother, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryEyeDiseaseSibling) && _encryptionService.IsEncrypted(assessment.FamilyHistoryEyeDiseaseSibling))
                    {
                        assessment.FamilyHistoryEyeDiseaseSibling = _encryptionService.DecryptForUser(assessment.FamilyHistoryEyeDiseaseSibling, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryOther) && _encryptionService.IsEncrypted(assessment.FamilyHistoryOther))
                    {
                        assessment.FamilyHistoryOther = _encryptionService.DecryptForUser(assessment.FamilyHistoryOther, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryOtherFather) && _encryptionService.IsEncrypted(assessment.FamilyHistoryOtherFather))
                    {
                        assessment.FamilyHistoryOtherFather = _encryptionService.DecryptForUser(assessment.FamilyHistoryOtherFather, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryOtherMother) && _encryptionService.IsEncrypted(assessment.FamilyHistoryOtherMother))
                    {
                        assessment.FamilyHistoryOtherMother = _encryptionService.DecryptForUser(assessment.FamilyHistoryOtherMother, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.FamilyHistoryOtherSibling) && _encryptionService.IsEncrypted(assessment.FamilyHistoryOtherSibling))
                    {
                        assessment.FamilyHistoryOtherSibling = _encryptionService.DecryptForUser(assessment.FamilyHistoryOtherSibling, User);
                    }
                    
                    // Lifestyle Factors
                    if (!string.IsNullOrEmpty(assessment.SmokingStatus) && _encryptionService.IsEncrypted(assessment.SmokingStatus))
                    {
                        assessment.SmokingStatus = _encryptionService.DecryptForUser(assessment.SmokingStatus, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.SmokingQuitDuration) && _encryptionService.IsEncrypted(assessment.SmokingQuitDuration))
                    {
                        assessment.SmokingQuitDuration = _encryptionService.DecryptForUser(assessment.SmokingQuitDuration, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HighSaltIntake) && _encryptionService.IsEncrypted(assessment.HighSaltIntake))
                    {
                        assessment.HighSaltIntake = _encryptionService.DecryptForUser(assessment.HighSaltIntake, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.AlcoholFrequency) && _encryptionService.IsEncrypted(assessment.AlcoholFrequency))
                    {
                        assessment.AlcoholFrequency = _encryptionService.DecryptForUser(assessment.AlcoholFrequency, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.AlcoholConsumption) && _encryptionService.IsEncrypted(assessment.AlcoholConsumption))
                    {
                        assessment.AlcoholConsumption = _encryptionService.DecryptForUser(assessment.AlcoholConsumption, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.ExerciseDuration) && _encryptionService.IsEncrypted(assessment.ExerciseDuration))
                    {
                        assessment.ExerciseDuration = _encryptionService.DecryptForUser(assessment.ExerciseDuration, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.RiskStatus) && _encryptionService.IsEncrypted(assessment.RiskStatus))
                    {
                        assessment.RiskStatus = _encryptionService.DecryptForUser(assessment.RiskStatus, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.AlcoholStoppedDuration) && _encryptionService.IsEncrypted(assessment.AlcoholStoppedDuration))
                    {
                        assessment.AlcoholStoppedDuration = _encryptionService.DecryptForUser(assessment.AlcoholStoppedDuration, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.Smoked100Sticks) && _encryptionService.IsEncrypted(assessment.Smoked100Sticks))
                    {
                        assessment.Smoked100Sticks = _encryptionService.DecryptForUser(assessment.Smoked100Sticks, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasEnoughExercise) && _encryptionService.IsEncrypted(assessment.HasEnoughExercise))
                    {
                        assessment.HasEnoughExercise = _encryptionService.DecryptForUser(assessment.HasEnoughExercise, User);
                    }

                    // Decrypt medical history boolean-like fields
                    if (!string.IsNullOrEmpty(assessment.HasDiabetes) && _encryptionService.IsEncrypted(assessment.HasDiabetes))
                    {
                        assessment.HasDiabetes = _encryptionService.DecryptForUser(assessment.HasDiabetes, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasHypertension) && _encryptionService.IsEncrypted(assessment.HasHypertension))
                    {
                        assessment.HasHypertension = _encryptionService.DecryptForUser(assessment.HasHypertension, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasCancer) && _encryptionService.IsEncrypted(assessment.HasCancer))
                    {
                        assessment.HasCancer = _encryptionService.DecryptForUser(assessment.HasCancer, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.CancerSite) && _encryptionService.IsEncrypted(assessment.CancerSite))
                    {
                        assessment.CancerSite = _encryptionService.DecryptForUser(assessment.CancerSite, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasCOPD) && _encryptionService.IsEncrypted(assessment.HasCOPD))
                    {
                        assessment.HasCOPD = _encryptionService.DecryptForUser(assessment.HasCOPD, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.COPDYear) && _encryptionService.IsEncrypted(assessment.COPDYear))
                    {
                        assessment.COPDYear = _encryptionService.DecryptForUser(assessment.COPDYear, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.COPDMedication) && _encryptionService.IsEncrypted(assessment.COPDMedication))
                    {
                        assessment.COPDMedication = _encryptionService.DecryptForUser(assessment.COPDMedication, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasLungDisease) && _encryptionService.IsEncrypted(assessment.HasLungDisease))
                    {
                        assessment.HasLungDisease = _encryptionService.DecryptForUser(assessment.HasLungDisease, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasEyeDisease) && _encryptionService.IsEncrypted(assessment.HasEyeDisease))
                    {
                        assessment.HasEyeDisease = _encryptionService.DecryptForUser(assessment.HasEyeDisease, User);
                    }
                    
                    // Chest Pain and Symptoms
                    if (!string.IsNullOrEmpty(assessment.HasChestPain) && _encryptionService.IsEncrypted(assessment.HasChestPain))
                    {
                        assessment.HasChestPain = _encryptionService.DecryptForUser(assessment.HasChestPain, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.ChestPainSpreadsToArm) && _encryptionService.IsEncrypted(assessment.ChestPainSpreadsToArm))
                    {
                        assessment.ChestPainSpreadsToArm = _encryptionService.DecryptForUser(assessment.ChestPainSpreadsToArm, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.NumbnessWhenWalkingFast) && _encryptionService.IsEncrypted(assessment.NumbnessWhenWalkingFast))
                    {
                        assessment.NumbnessWhenWalkingFast = _encryptionService.DecryptForUser(assessment.NumbnessWhenWalkingFast, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.PainRelievedWithRest) && _encryptionService.IsEncrypted(assessment.PainRelievedWithRest))
                    {
                        assessment.PainRelievedWithRest = _encryptionService.DecryptForUser(assessment.PainRelievedWithRest, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.LossOfConsciousnessLessThan10Min) && _encryptionService.IsEncrypted(assessment.LossOfConsciousnessLessThan10Min))
                    {
                        assessment.LossOfConsciousnessLessThan10Min = _encryptionService.DecryptForUser(assessment.LossOfConsciousnessLessThan10Min, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.PainLastsMoreThan30Min) && _encryptionService.IsEncrypted(assessment.PainLastsMoreThan30Min))
                    {
                        assessment.PainLastsMoreThan30Min = _encryptionService.DecryptForUser(assessment.PainLastsMoreThan30Min, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.SeeDoctorIfYes) && _encryptionService.IsEncrypted(assessment.SeeDoctorIfYes))
                    {
                        assessment.SeeDoctorIfYes = _encryptionService.DecryptForUser(assessment.SeeDoctorIfYes, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.ChestPain) && _encryptionService.IsEncrypted(assessment.ChestPain))
                    {
                        assessment.ChestPain = _encryptionService.DecryptForUser(assessment.ChestPain, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.ChestPainLocation) && _encryptionService.IsEncrypted(assessment.ChestPainLocation))
                    {
                        assessment.ChestPainLocation = _encryptionService.DecryptForUser(assessment.ChestPainLocation, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.ChestPainValue) && _encryptionService.IsEncrypted(assessment.ChestPainValue))
                    {
                        assessment.ChestPainValue = _encryptionService.DecryptForUser(assessment.ChestPainValue, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasDifficultyBreathing) && _encryptionService.IsEncrypted(assessment.HasDifficultyBreathing))
                    {
                        assessment.HasDifficultyBreathing = _encryptionService.DecryptForUser(assessment.HasDifficultyBreathing, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasAsthma) && _encryptionService.IsEncrypted(assessment.HasAsthma))
                    {
                        assessment.HasAsthma = _encryptionService.DecryptForUser(assessment.HasAsthma, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasStrokeSymptoms) && _encryptionService.IsEncrypted(assessment.HasStrokeSymptoms))
                    {
                        assessment.HasStrokeSymptoms = _encryptionService.DecryptForUser(assessment.HasStrokeSymptoms, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.HasNoRegularExercise) && _encryptionService.IsEncrypted(assessment.HasNoRegularExercise))
                    {
                        assessment.HasNoRegularExercise = _encryptionService.DecryptForUser(assessment.HasNoRegularExercise, User);
                    }

                    // Nutrition booleans
                    if (!string.IsNullOrEmpty(assessment.EatsVegetablesDaily) && _encryptionService.IsEncrypted(assessment.EatsVegetablesDaily))
                        assessment.EatsVegetablesDaily = _encryptionService.DecryptForUser(assessment.EatsVegetablesDaily, User);
                    if (!string.IsNullOrEmpty(assessment.EatsFruitsDaily) && _encryptionService.IsEncrypted(assessment.EatsFruitsDaily))
                        assessment.EatsFruitsDaily = _encryptionService.DecryptForUser(assessment.EatsFruitsDaily, User);
                    if (!string.IsNullOrEmpty(assessment.EatsFishDaily) && _encryptionService.IsEncrypted(assessment.EatsFishDaily))
                        assessment.EatsFishDaily = _encryptionService.DecryptForUser(assessment.EatsFishDaily, User);
                    if (!string.IsNullOrEmpty(assessment.EatsMeatDaily) && _encryptionService.IsEncrypted(assessment.EatsMeatDaily))
                        assessment.EatsMeatDaily = _encryptionService.DecryptForUser(assessment.EatsMeatDaily, User);
                    if (!string.IsNullOrEmpty(assessment.HasUnhealthyDiet) && _encryptionService.IsEncrypted(assessment.HasUnhealthyDiet))
                        assessment.HasUnhealthyDiet = _encryptionService.DecryptForUser(assessment.HasUnhealthyDiet, User);
                    if (!string.IsNullOrEmpty(assessment.EatsFattyFoodMoreThan2TimesPerWeek) && _encryptionService.IsEncrypted(assessment.EatsFattyFoodMoreThan2TimesPerWeek))
                        assessment.EatsFattyFoodMoreThan2TimesPerWeek = _encryptionService.DecryptForUser(assessment.EatsFattyFoodMoreThan2TimesPerWeek, User);
                    if (!string.IsNullOrEmpty(assessment.EatsSweetFoodMoreThan2TimesPerWeek) && _encryptionService.IsEncrypted(assessment.EatsSweetFoodMoreThan2TimesPerWeek))
                        assessment.EatsSweetFoodMoreThan2TimesPerWeek = _encryptionService.DecryptForUser(assessment.EatsSweetFoodMoreThan2TimesPerWeek, User);
                    if (!string.IsNullOrEmpty(assessment.EatsOilyFoodMoreThan2TimesPerWeek) && _encryptionService.IsEncrypted(assessment.EatsOilyFoodMoreThan2TimesPerWeek))
                        assessment.EatsOilyFoodMoreThan2TimesPerWeek = _encryptionService.DecryptForUser(assessment.EatsOilyFoodMoreThan2TimesPerWeek, User);
                    if (!string.IsNullOrEmpty(assessment.HasHighSaltIntake) && _encryptionService.IsEncrypted(assessment.HasHighSaltIntake))
                        assessment.HasHighSaltIntake = _encryptionService.DecryptForUser(assessment.HasHighSaltIntake, User);

                    // Alcohol details
                    if (!string.IsNullOrEmpty(assessment.DrinksAlcohol) && _encryptionService.IsEncrypted(assessment.DrinksAlcohol))
                        assessment.DrinksAlcohol = _encryptionService.DecryptForUser(assessment.DrinksAlcohol, User);
                    if (!string.IsNullOrEmpty(assessment.DrinksBeer) && _encryptionService.IsEncrypted(assessment.DrinksBeer))
                        assessment.DrinksBeer = _encryptionService.DecryptForUser(assessment.DrinksBeer, User);
                    if (!string.IsNullOrEmpty(assessment.DrinksWine) && _encryptionService.IsEncrypted(assessment.DrinksWine))
                        assessment.DrinksWine = _encryptionService.DecryptForUser(assessment.DrinksWine, User);
                    if (!string.IsNullOrEmpty(assessment.DrinksWhiskyGinBrandy) && _encryptionService.IsEncrypted(assessment.DrinksWhiskyGinBrandy))
                        assessment.DrinksWhiskyGinBrandy = _encryptionService.DecryptForUser(assessment.DrinksWhiskyGinBrandy, User);
                    if (!string.IsNullOrEmpty(assessment.AlcoholAmount1Bottle320ml) && _encryptionService.IsEncrypted(assessment.AlcoholAmount1Bottle320ml))
                        assessment.AlcoholAmount1Bottle320ml = _encryptionService.DecryptForUser(assessment.AlcoholAmount1Bottle320ml, User);
                    if (!string.IsNullOrEmpty(assessment.AlcoholAmount2Bottle640ml) && _encryptionService.IsEncrypted(assessment.AlcoholAmount2Bottle640ml))
                        assessment.AlcoholAmount2Bottle640ml = _encryptionService.DecryptForUser(assessment.AlcoholAmount2Bottle640ml, User);
                    if (!string.IsNullOrEmpty(assessment.AlcoholAmountLessThan3Shot45ml) && _encryptionService.IsEncrypted(assessment.AlcoholAmountLessThan3Shot45ml))
                        assessment.AlcoholAmountLessThan3Shot45ml = _encryptionService.DecryptForUser(assessment.AlcoholAmountLessThan3Shot45ml, User);
                    if (!string.IsNullOrEmpty(assessment.AlcoholAmount3to4WineGlasses300ml) && _encryptionService.IsEncrypted(assessment.AlcoholAmount3to4WineGlasses300ml))
                        assessment.AlcoholAmount3to4WineGlasses300ml = _encryptionService.DecryptForUser(assessment.AlcoholAmount3to4WineGlasses300ml, User);
                    if (!string.IsNullOrEmpty(assessment.AlcoholAmountMoreThan4Shots75ml) && _encryptionService.IsEncrypted(assessment.AlcoholAmountMoreThan4Shots75ml))
                        assessment.AlcoholAmountMoreThan4Shots75ml = _encryptionService.DecryptForUser(assessment.AlcoholAmountMoreThan4Shots75ml, User);
                    if (!string.IsNullOrEmpty(assessment.AlcoholFrequency1to3TimesPerWeek) && _encryptionService.IsEncrypted(assessment.AlcoholFrequency1to3TimesPerWeek))
                        assessment.AlcoholFrequency1to3TimesPerWeek = _encryptionService.DecryptForUser(assessment.AlcoholFrequency1to3TimesPerWeek, User);
                    if (!string.IsNullOrEmpty(assessment.AlcoholFrequencyMoreThan4TimesPerWeek) && _encryptionService.IsEncrypted(assessment.AlcoholFrequencyMoreThan4TimesPerWeek))
                        assessment.AlcoholFrequencyMoreThan4TimesPerWeek = _encryptionService.DecryptForUser(assessment.AlcoholFrequencyMoreThan4TimesPerWeek, User);
                    if (!string.IsNullOrEmpty(assessment.IsBingeDrinker) && _encryptionService.IsEncrypted(assessment.IsBingeDrinker))
                        assessment.IsBingeDrinker = _encryptionService.DecryptForUser(assessment.IsBingeDrinker, User);

                    // Exercise details
                    if (!string.IsNullOrEmpty(assessment.ModerateIntensityExercise) && _encryptionService.IsEncrypted(assessment.ModerateIntensityExercise))
                        assessment.ModerateIntensityExercise = _encryptionService.DecryptForUser(assessment.ModerateIntensityExercise, User);
                    if (!string.IsNullOrEmpty(assessment.VigorousIntensityExercise) && _encryptionService.IsEncrypted(assessment.VigorousIntensityExercise))
                        assessment.VigorousIntensityExercise = _encryptionService.DecryptForUser(assessment.VigorousIntensityExercise, User);
                    if (!string.IsNullOrEmpty(assessment.CombinationExercise) && _encryptionService.IsEncrypted(assessment.CombinationExercise))
                        assessment.CombinationExercise = _encryptionService.DecryptForUser(assessment.CombinationExercise, User);
                    if (!string.IsNullOrEmpty(assessment.InsufficientPhysicalActivity) && _encryptionService.IsEncrypted(assessment.InsufficientPhysicalActivity))
                        assessment.InsufficientPhysicalActivity = _encryptionService.DecryptForUser(assessment.InsufficientPhysicalActivity, User);

                    // Smoking details
                    if (!string.IsNullOrEmpty(assessment.FormerSmoker) && _encryptionService.IsEncrypted(assessment.FormerSmoker))
                        assessment.FormerSmoker = _encryptionService.DecryptForUser(assessment.FormerSmoker, User);
                    if (!string.IsNullOrEmpty(assessment.NeverSmokedButExposedToSmoke) && _encryptionService.IsEncrypted(assessment.NeverSmokedButExposedToSmoke))
                    {
                        var decryptedValue = _encryptionService.DecryptForUser(assessment.NeverSmokedButExposedToSmoke, User);
                        _logger.LogInformation("NeverSmokedButExposedToSmoke decryption (second occurrence): Original='{Original}', Decrypted='{Decrypted}'", 
                            assessment.NeverSmokedButExposedToSmoke?.Substring(0, Math.Min(20, assessment.NeverSmokedButExposedToSmoke?.Length ?? 0)) + "...", 
                            decryptedValue);
                        assessment.NeverSmokedButExposedToSmoke = decryptedValue;
                    }
                    if (!string.IsNullOrEmpty(assessment.HasHistoryOfSmoking) && _encryptionService.IsEncrypted(assessment.HasHistoryOfSmoking))
                        assessment.HasHistoryOfSmoking = _encryptionService.DecryptForUser(assessment.HasHistoryOfSmoking, User);
                    
                    // System Fields - CreatedAt and UpdatedAt are now DateTime, no decryption needed
                    if (!string.IsNullOrEmpty(assessment.AppointmentType) && _encryptionService.IsEncrypted(assessment.AppointmentType))
                    {
                        assessment.AppointmentType = _encryptionService.DecryptForUser(assessment.AppointmentType, User);
                    }
                    
                    // MISSING DECRYPTION FIELDS - Add these critical missing fields
                    
                    // Pananakit (Chest Pain) Questions 2.1-2.8 - These are missing!
                    if (!string.IsNullOrEmpty(assessment.Pananakit21) && _encryptionService.IsEncrypted(assessment.Pananakit21))
                    {
                        assessment.Pananakit21 = _encryptionService.DecryptForUser(assessment.Pananakit21, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.Pananakit22) && _encryptionService.IsEncrypted(assessment.Pananakit22))
                    {
                        assessment.Pananakit22 = _encryptionService.DecryptForUser(assessment.Pananakit22, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.Pananakit23) && _encryptionService.IsEncrypted(assessment.Pananakit23))
                    {
                        assessment.Pananakit23 = _encryptionService.DecryptForUser(assessment.Pananakit23, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.Pananakit24) && _encryptionService.IsEncrypted(assessment.Pananakit24))
                    {
                        assessment.Pananakit24 = _encryptionService.DecryptForUser(assessment.Pananakit24, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.Pananakit25) && _encryptionService.IsEncrypted(assessment.Pananakit25))
                    {
                        assessment.Pananakit25 = _encryptionService.DecryptForUser(assessment.Pananakit25, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.Pananakit26) && _encryptionService.IsEncrypted(assessment.Pananakit26))
                    {
                        assessment.Pananakit26 = _encryptionService.DecryptForUser(assessment.Pananakit26, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.Pananakit27) && _encryptionService.IsEncrypted(assessment.Pananakit27))
                    {
                        assessment.Pananakit27 = _encryptionService.DecryptForUser(assessment.Pananakit27, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.Pananakit28) && _encryptionService.IsEncrypted(assessment.Pananakit28))
                    {
                        assessment.Pananakit28 = _encryptionService.DecryptForUser(assessment.Pananakit28, User);
                    }
                    
                    // Nutrition - Missing detailed mappings
                    if (!string.IsNullOrEmpty(assessment.NutrisyonMadalasGulay) && _encryptionService.IsEncrypted(assessment.NutrisyonMadalasGulay))
                    {
                        assessment.NutrisyonMadalasGulay = _encryptionService.DecryptForUser(assessment.NutrisyonMadalasGulay, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.NutrisyonMadalasPratas) && _encryptionService.IsEncrypted(assessment.NutrisyonMadalasPratas))
                    {
                        assessment.NutrisyonMadalasPratas = _encryptionService.DecryptForUser(assessment.NutrisyonMadalasPratas, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.NutrisyonMadalasIsda) && _encryptionService.IsEncrypted(assessment.NutrisyonMadalasIsda))
                    {
                        assessment.NutrisyonMadalasIsda = _encryptionService.DecryptForUser(assessment.NutrisyonMadalasIsda, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.NutrisyonMadalasKarne) && _encryptionService.IsEncrypted(assessment.NutrisyonMadalasKarne))
                    {
                        assessment.NutrisyonMadalasKarne = _encryptionService.DecryptForUser(assessment.NutrisyonMadalasKarne, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.NutrisyonKumakainMatatamis) && _encryptionService.IsEncrypted(assessment.NutrisyonKumakainMatatamis))
                    {
                        assessment.NutrisyonKumakainMatatamis = _encryptionService.DecryptForUser(assessment.NutrisyonKumakainMatatamis, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.NutrisyonKumakainMamantika) && _encryptionService.IsEncrypted(assessment.NutrisyonKumakainMamantika))
                    {
                        assessment.NutrisyonKumakainMamantika = _encryptionService.DecryptForUser(assessment.NutrisyonKumakainMamantika, User);
                    }
                    
                    // Alcohol - Missing detailed mappings
                    if (!string.IsNullOrEmpty(assessment.AlcoholInom) && _encryptionService.IsEncrypted(assessment.AlcoholInom))
                    {
                        assessment.AlcoholInom = _encryptionService.DecryptForUser(assessment.AlcoholInom, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.AlchoholTypeBeer) && _encryptionService.IsEncrypted(assessment.AlchoholTypeBeer))
                    {
                        assessment.AlchoholTypeBeer = _encryptionService.DecryptForUser(assessment.AlchoholTypeBeer, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.AlchoholTypeWine) && _encryptionService.IsEncrypted(assessment.AlchoholTypeWine))
                    {
                        assessment.AlchoholTypeWine = _encryptionService.DecryptForUser(assessment.AlchoholTypeWine, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.AlchoholTypeWhisky) && _encryptionService.IsEncrypted(assessment.AlchoholTypeWhisky))
                    {
                        assessment.AlchoholTypeWhisky = _encryptionService.DecryptForUser(assessment.AlchoholTypeWhisky, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.BeerConsumption1) && _encryptionService.IsEncrypted(assessment.BeerConsumption1))
                    {
                        assessment.BeerConsumption1 = _encryptionService.DecryptForUser(assessment.BeerConsumption1, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.BeerConsumption2) && _encryptionService.IsEncrypted(assessment.BeerConsumption2))
                    {
                        assessment.BeerConsumption2 = _encryptionService.DecryptForUser(assessment.BeerConsumption2, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.WineConsumption1) && _encryptionService.IsEncrypted(assessment.WineConsumption1))
                    {
                        assessment.WineConsumption1 = _encryptionService.DecryptForUser(assessment.WineConsumption1, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.WhiskyConsumption1) && _encryptionService.IsEncrypted(assessment.WhiskyConsumption1))
                    {
                        assessment.WhiskyConsumption1 = _encryptionService.DecryptForUser(assessment.WhiskyConsumption1, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.WhiskyConsumption2) && _encryptionService.IsEncrypted(assessment.WhiskyConsumption2))
                    {
                        assessment.WhiskyConsumption2 = _encryptionService.DecryptForUser(assessment.WhiskyConsumption2, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.AlcoholOkasyon) && _encryptionService.IsEncrypted(assessment.AlcoholOkasyon))
                    {
                        assessment.AlcoholOkasyon = _encryptionService.DecryptForUser(assessment.AlcoholOkasyon, User);
                    }
                    
                    // Exercise - Missing detailed mappings
                    if (!string.IsNullOrEmpty(assessment.EhersisyoRegular) && _encryptionService.IsEncrypted(assessment.EhersisyoRegular))
                    {
                        assessment.EhersisyoRegular = _encryptionService.DecryptForUser(assessment.EhersisyoRegular, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.EhersisyoDuration) && _encryptionService.IsEncrypted(assessment.EhersisyoDuration))
                    {
                        assessment.EhersisyoDuration = _encryptionService.DecryptForUser(assessment.EhersisyoDuration, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.EhersisyoType) && _encryptionService.IsEncrypted(assessment.EhersisyoType))
                    {
                        assessment.EhersisyoType = _encryptionService.DecryptForUser(assessment.EhersisyoType, User);
                    }
                    
                    // Smoking - Missing detailed mappings
                    if (!string.IsNullOrEmpty(assessment.SigarilyoKadami) && _encryptionService.IsEncrypted(assessment.SigarilyoKadami))
                    {
                        assessment.SigarilyoKadami = _encryptionService.DecryptForUser(assessment.SigarilyoKadami, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.SigarilyoTumigil) && _encryptionService.IsEncrypted(assessment.SigarilyoTumigil))
                    {
                        assessment.SigarilyoTumigil = _encryptionService.DecryptForUser(assessment.SigarilyoTumigil, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.SigarilyoUsok) && _encryptionService.IsEncrypted(assessment.SigarilyoUsok))
                    {
                        assessment.SigarilyoUsok = _encryptionService.DecryptForUser(assessment.SigarilyoUsok, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.SigarilyoSticks) && _encryptionService.IsEncrypted(assessment.SigarilyoSticks))
                    {
                        assessment.SigarilyoSticks = _encryptionService.DecryptForUser(assessment.SigarilyoSticks, User);
                    }
                    
                    // Stress - Missing detailed mappings
                    if (!string.IsNullOrEmpty(assessment.StressMadalas) && _encryptionService.IsEncrypted(assessment.StressMadalas))
                    {
                        assessment.StressMadalas = _encryptionService.DecryptForUser(assessment.StressMadalas, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.StressSino) && _encryptionService.IsEncrypted(assessment.StressSino))
                    {
                        assessment.StressSino = _encryptionService.DecryptForUser(assessment.StressSino, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.StressEpekto) && _encryptionService.IsEncrypted(assessment.StressEpekto))
                    {
                        assessment.StressEpekto = _encryptionService.DecryptForUser(assessment.StressEpekto, User);
                    }
                    
                    // Additional missing fields for complete form mapping
                    if (!string.IsNullOrEmpty(assessment.HealthFacilityName) && _encryptionService.IsEncrypted(assessment.HealthFacilityName))
                    {
                        assessment.HealthFacilityName = _encryptionService.DecryptForUser(assessment.HealthFacilityName, User);
                    }
                    if (!string.IsNullOrEmpty(assessment.DateAssessment) && _encryptionService.IsEncrypted(assessment.DateAssessment))
                    {
                        assessment.DateAssessment = _encryptionService.DecryptForUser(assessment.DateAssessment, User);
                    }
                    
                    // Lung Disease - Missing proper mapping
                    if (!string.IsNullOrEmpty(assessment.HasLungDiseaseNonInfectious) && _encryptionService.IsEncrypted(assessment.HasLungDiseaseNonInfectious))
                    {
                        assessment.HasLungDiseaseNonInfectious = _encryptionService.DecryptForUser(assessment.HasLungDiseaseNonInfectious, User);
                    }
                    
                    // Eye Disease - Missing proper mapping  
                    if (!string.IsNullOrEmpty(assessment.HasEyeDiseaseCondition) && _encryptionService.IsEncrypted(assessment.HasEyeDiseaseCondition))
                    {
                        assessment.HasEyeDiseaseCondition = _encryptionService.DecryptForUser(assessment.HasEyeDiseaseCondition, User);
                    }
                }
                catch (Exception decryptEx)
                {
                    _logger.LogError(decryptEx, "Failed to decrypt assessment data for appointment {AppointmentId}", appointmentId);
                    // Continue with encrypted data rather than failing completely
                    _logger.LogWarning("Continuing with encrypted data due to decryption failure");
                }

                // Convert to view model
                _logger.LogInformation("Creating view model for assessment data");
                
                // Test basic properties first
                _logger.LogInformation("Testing first name: {FirstName}", assessment.FirstName);
                
                try
                {
                    // DEBUGGING: Log values before creating view model
                    _logger.LogInformation("=== CREATING VIEW MODEL ===");
                    _logger.LogInformation("Assessment values before view model creation:");
                    _logger.LogInformation("  HasDiabetes: '{HasDiabetes}'", assessment.HasDiabetes);
                    _logger.LogInformation("  HasChestPain: '{HasChestPain}'", assessment.HasChestPain);
                    _logger.LogInformation("  DrinksAlcohol: '{DrinksAlcohol}'", assessment.DrinksAlcohol);
                    _logger.LogInformation("  HasHistoryOfSmoking: '{HasHistoryOfSmoking}'", assessment.HasHistoryOfSmoking);
                    _logger.LogInformation("  HasStress: '{HasStress}'", assessment.HasStress);
                    _logger.LogInformation("  EatsVegetablesDaily: '{EatsVegetablesDaily}'", assessment.EatsVegetablesDaily);
                    _logger.LogInformation("  FamilyHistoryHeartDiseaseFather: '{FamilyHistoryHeartDiseaseFather}'", assessment.FamilyHistoryHeartDiseaseFather);
                    
                    // Create complete view model with all fields
                    NCDRiskAssessment = new NCDRiskAssessmentViewModel
                    {
                        AppointmentId = appointmentId.Value,
                        UserId = assessment.UserId?.ToString() ?? "",
                        HealthFacility = assessment.HealthFacility,
                        FamilyNo = assessment.FamilyNo,
                        Address = assessment.Address,
                        FirstName = assessment.FirstName,
                        LastName = assessment.LastName,
                        MiddleName = assessment.MiddleName,
                        DateOfAssessment = !string.IsNullOrEmpty(assessment.DateOfAssessment) ? DateTime.TryParse(assessment.DateOfAssessment, out var date) ? date : DateTime.Now : DateTime.Now,
                        IDNumber = assessment.IDNumber,
                        Barangay = assessment.Barangay,
                        Telepono = assessment.Telepono,
                        Birthday = !string.IsNullOrEmpty(assessment.Birthday) ? DateTime.TryParse(assessment.Birthday, out var birthday) ? birthday : (DateTime?)null : null,
                        Edad = assessment.Edad,
                        Kasarian = assessment.Kasarian,
                        Relihiyon = assessment.Relihiyon,
                        Occupation = assessment.Occupation,
                        CivilStatus = assessment.CivilStatus,
                        
                        // Medical History
                        HasDiabetes = assessment.HasDiabetes,
                        HasHypertension = assessment.HasHypertension,
                        HasCancer = assessment.HasCancer,
                        HasCOPD = assessment.HasCOPD,
                        COPDYear = assessment.COPDYear,
                        COPDMedication = assessment.COPDMedication,
                        HasLungDisease = assessment.HasLungDisease,
                        HasEyeDisease = assessment.HasEyeDisease,
                        CancerType = assessment.CancerType,
                        CancerSite = assessment.CancerSite,
                        CancerYear = assessment.CancerYear,
                        CancerMedication = assessment.CancerMedication,
                        DiabetesYear = assessment.DiabetesYear,
                        DiabetesMedication = assessment.DiabetesMedication,
                        HypertensionYear = assessment.HypertensionYear,
                        HypertensionMedication = assessment.HypertensionMedication,
                        LungDiseaseYear = assessment.LungDiseaseYear,
                        LungDiseaseMedication = assessment.LungDiseaseMedication,
                        EyeDiseaseYear = assessment.EyeDiseaseYear,
                        EyeDiseaseMedication = assessment.EyeDiseaseMedication,
                        HasAsthma = assessment.HasAsthma,
                        HasDifficultyBreathing = assessment.HasDifficultyBreathing,
                        HasStrokeSymptoms = assessment.HasStrokeSymptoms,
                        
                        // Chest Pain and Symptoms
                        HasChestPain = assessment.HasChestPain,
                        ChestPainSpreadsToArm = assessment.ChestPainSpreadsToArm,
                        NumbnessWhenWalkingFast = assessment.NumbnessWhenWalkingFast,
                        PainRelievedWithRest = assessment.PainRelievedWithRest,
                        LossOfConsciousnessLessThan10Min = assessment.LossOfConsciousnessLessThan10Min,
                        PainLastsMoreThan30Min = assessment.PainLastsMoreThan30Min,
                        SeeDoctorIfYes = assessment.SeeDoctorIfYes,
                        DoctorName = assessment.DoctorName,
                        
                        // Family History
                        FamilyHasHypertension = assessment.FamilyHasHypertension,
                        FamilyHasHeartDisease = assessment.FamilyHasHeartDisease,
                        FamilyHasStroke = assessment.FamilyHasStroke,
                        FamilyHasDiabetes = assessment.FamilyHasDiabetes,
                        FamilyHasCancer = assessment.FamilyHasCancer,
                        FamilyHasKidneyDisease = assessment.FamilyHasKidneyDisease,
                        FamilyHasOtherDisease = assessment.FamilyHasOtherDisease,
                        FamilyOtherDiseaseDetails = assessment.FamilyOtherDiseaseDetails,
                        
                        // Detailed Family History
                        FamilyHistoryCancerFather = assessment.FamilyHistoryCancerFather,
                        FamilyHistoryCancerMother = assessment.FamilyHistoryCancerMother,
                        FamilyHistoryCancerSibling = assessment.FamilyHistoryCancerSibling,
                        FamilyHistoryDiabetesFather = assessment.FamilyHistoryDiabetesFather,
                        FamilyHistoryDiabetesMother = assessment.FamilyHistoryDiabetesMother,
                        FamilyHistoryDiabetesSibling = assessment.FamilyHistoryDiabetesSibling,
                        FamilyHistoryHeartDiseaseFather = assessment.FamilyHistoryHeartDiseaseFather,
                        FamilyHistoryHeartDiseaseMother = assessment.FamilyHistoryHeartDiseaseMother,
                        FamilyHistoryHeartDiseaseSibling = assessment.FamilyHistoryHeartDiseaseSibling,
                        FamilyHistoryLungDiseaseFather = assessment.FamilyHistoryLungDiseaseFather,
                        FamilyHistoryLungDiseaseMother = assessment.FamilyHistoryLungDiseaseMother,
                        FamilyHistoryLungDiseaseSibling = assessment.FamilyHistoryLungDiseaseSibling,
                        FamilyHistoryStrokeFather = assessment.FamilyHistoryStrokeFather,
                        FamilyHistoryStrokeMother = assessment.FamilyHistoryStrokeMother,
                        FamilyHistoryStrokeSibling = assessment.FamilyHistoryStrokeSibling,
                        FamilyHistoryKidneyDiseaseFather = assessment.FamilyHistoryKidneyDiseaseFather,
                        FamilyHistoryKidneyDiseaseMother = assessment.FamilyHistoryKidneyDiseaseMother,
                        FamilyHistoryKidneyDiseaseSibling = assessment.FamilyHistoryKidneyDiseaseSibling,
                        FamilyHistoryEyeDiseaseFather = assessment.FamilyHistoryEyeDiseaseFather,
                        FamilyHistoryEyeDiseaseMother = assessment.FamilyHistoryEyeDiseaseMother,
                        FamilyHistoryEyeDiseaseSibling = assessment.FamilyHistoryEyeDiseaseSibling,
                        FamilyHistoryOther = assessment.FamilyHistoryOther,
                        FamilyHistoryOtherFather = assessment.FamilyHistoryOtherFather,
                        FamilyHistoryOtherMother = assessment.FamilyHistoryOtherMother,
                        FamilyHistoryOtherSibling = assessment.FamilyHistoryOtherSibling,
                        
                        // Lifestyle Factors
                        EatsVegetablesDaily = assessment.EatsVegetablesDaily,
                        EatsFruitsDaily = assessment.EatsFruitsDaily,
                        EatsFishDaily = assessment.EatsFishDaily,
                        EatsMeatDaily = assessment.EatsMeatDaily,
                        HasUnhealthyDiet = assessment.HasUnhealthyDiet,
                        EatsFattyFoodMoreThan2TimesPerWeek = assessment.EatsFattyFoodMoreThan2TimesPerWeek,
                        EatsSweetFoodMoreThan2TimesPerWeek = assessment.EatsSweetFoodMoreThan2TimesPerWeek,
                        EatsOilyFoodMoreThan2TimesPerWeek = assessment.EatsOilyFoodMoreThan2TimesPerWeek,
                        HasHighSaltIntake = assessment.HasHighSaltIntake,
                        
                        // Alcohol
                        DrinksAlcohol = assessment.DrinksAlcohol,
                        DrinksBeer = assessment.DrinksBeer,
                        DrinksWine = assessment.DrinksWine,
                        DrinksWhiskyGinBrandy = assessment.DrinksWhiskyGinBrandy,
                        AlcoholAmount1Bottle320ml = assessment.AlcoholAmount1Bottle320ml,
                        AlcoholAmount2Bottle640ml = assessment.AlcoholAmount2Bottle640ml,
                        AlcoholAmountLessThan3Shot45ml = assessment.AlcoholAmountLessThan3Shot45ml,
                        AlcoholAmount3to4WineGlasses300ml = assessment.AlcoholAmount3to4WineGlasses300ml,
                        AlcoholAmountMoreThan4Shots75ml = assessment.AlcoholAmountMoreThan4Shots75ml,
                        BeerConsumption3 = assessment.BeerConsumption3,
                        WineConsumption2 = assessment.WineConsumption2,
                        AlcoholFrequency1to3TimesPerWeek = assessment.AlcoholFrequency1to3TimesPerWeek,
                        AlcoholFrequencyMoreThan4TimesPerWeek = assessment.AlcoholFrequencyMoreThan4TimesPerWeek,
                        IsBingeDrinker = assessment.IsBingeDrinker,
                        AlcoholStoppedDuration = assessment.AlcoholStoppedDuration,
                        AlcoholPerOccasion = assessment.AlcoholPerOccasion,
                        
                        // Exercise
                        ModerateIntensityExercise = assessment.ModerateIntensityExercise,
                        VigorousIntensityExercise = assessment.VigorousIntensityExercise,
                        CombinationExercise = assessment.CombinationExercise,
                        InsufficientPhysicalActivity = assessment.InsufficientPhysicalActivity,
                        HasEnoughExercise = assessment.HasEnoughExercise,
                        HasNoRegularExercise = assessment.HasNoRegularExercise,
                        
                        // Smoking
                        HasHistoryOfSmoking = assessment.HasHistoryOfSmoking,
                        FormerSmoker = assessment.FormerSmoker,
                        NeverSmokedButExposedToSmoke = assessment.NeverSmokedButExposedToSmoke,
                        Smoked100Sticks = assessment.Smoked100Sticks,
                        SmokingStatus = assessment.SmokingStatus,
                        SmokingQuitDuration = assessment.SmokingQuitDuration,
                        
                        // Stress
                        HasStress = assessment.HasStress,
                        
                        // Anthropometric Measurements
                        Weight = assessment.Weight,
                        Height = assessment.Height,
                        BMI = assessment.BMI,
                        Waist = assessment.Waist,
                        Hip = assessment.Hip,
                        WHRatio = assessment.WHRatio,
                        BMIStatus = assessment.BMIStatus,
                        WHStatus = assessment.WHStatus,
                        
                        // Blood Sugar
                        FastingBloodSugar = assessment.FastingBloodSugar,
                        RandomBloodSugar = assessment.RandomBloodSugar,
                        BloodSugarStatus = assessment.BloodSugarStatus,
                        HasPolyuria = assessment.HasPolyuria,
                        HasPolydipsia = assessment.HasPolydipsia,
                        HasPolyphagia = assessment.HasPolyphagia,
                        HasWeightLoss = assessment.HasWeightLoss,
                        
                        // Blood Pressure
                        LeftArmMeanBP = assessment.LeftArmMeanBP,
                        RightArmMeanBP = assessment.RightArmMeanBP,
                        BaselineBP = assessment.BaselineBP,
                        BPStatus = assessment.BPStatus,
                        
                        // Cholesterol
                        CholesterolResult = assessment.CholesterolResult,
                        CholesterolStatus = assessment.CholesterolStatus,
                        
                        // Urine
                        UrineProtein = assessment.UrineProtein,
                        UrineKetones = assessment.UrineKetones,
                        HasUrineProtein = assessment.HasUrineProtein,
                        HasUrineKetones = assessment.HasUrineKetones,
                        
                        // Risk Profile
                        RiskPercentage = assessment.RiskPercentage,
                        
                        // Cancer Screening
                        BreastCancerScreened = assessment.BreastCancerScreened,
                        CervicalCancerScreened = assessment.CervicalCancerScreened,
                        CancerScreeningStatus = assessment.CancerScreeningStatus,
                        
                        // Assessment Information
                        InterviewedBy = assessment.InterviewedBy,
                        Designation = assessment.Designation,
                        AssessmentDate = assessment.AssessmentDate,
                        PatientSignature = assessment.PatientSignature,
                        
                        // Missing properties
                        IDNo = assessment.IDNo,
                        
                        // MISSING VIEWMODEL FIELDS - Add these critical missing fields
                        
                        // Pananakit (Chest Pain) Questions 2.1-2.8
                        Pananakit21 = assessment.Pananakit21,
                        Pananakit22 = assessment.Pananakit22,
                        Pananakit23 = assessment.Pananakit23,
                        Pananakit24 = assessment.Pananakit24,
                        Pananakit25 = assessment.Pananakit25,
                        Pananakit26 = assessment.Pananakit26,
                        Pananakit27 = assessment.Pananakit27,
                        Pananakit28 = assessment.Pananakit28,
                        
                        // Nutrition - Missing detailed mappings
                        NutrisyonMadalasGulay = assessment.NutrisyonMadalasGulay,
                        NutrisyonMadalasPratas = assessment.NutrisyonMadalasPratas,
                        NutrisyonMadalasIsda = assessment.NutrisyonMadalasIsda,
                        NutrisyonMadalasKarne = assessment.NutrisyonMadalasKarne,
                        NutrisyonKumakainMatatamis = assessment.NutrisyonKumakainMatatamis,
                        NutrisyonKumakainMamantika = assessment.NutrisyonKumakainMamantika,
                        
                        // Alcohol - Missing detailed mappings
                        AlcoholInom = assessment.AlcoholInom,
                        AlchoholTypeBeer = assessment.AlchoholTypeBeer,
                        AlchoholTypeWine = assessment.AlchoholTypeWine,
                        AlchoholTypeWhisky = assessment.AlchoholTypeWhisky,
                        BeerConsumption1 = assessment.BeerConsumption1,
                        BeerConsumption2 = assessment.BeerConsumption2,
                        WineConsumption1 = assessment.WineConsumption1,
                        WhiskyConsumption1 = assessment.WhiskyConsumption1,
                        WhiskyConsumption2 = assessment.WhiskyConsumption2,
                        AlcoholOkasyon = assessment.AlcoholOkasyon,
                        
                        // Exercise - Missing detailed mappings
                        EhersisyoRegular = assessment.EhersisyoRegular,
                        EhersisyoDuration = assessment.EhersisyoDuration,
                        EhersisyoType = assessment.EhersisyoType,
                        
                        // Smoking - Missing detailed mappings
                        SigarilyoKadami = assessment.SigarilyoKadami,
                        SigarilyoTumigil = assessment.SigarilyoTumigil,
                        SigarilyoUsok = assessment.SigarilyoUsok,
                        SigarilyoSticks = assessment.SigarilyoSticks,
                        
                        // Stress - Missing detailed mappings
                        StressMadalas = assessment.StressMadalas,
                        StressSino = assessment.StressSino,
                        StressEpekto = assessment.StressEpekto,
                        
                        // Additional missing fields for complete form mapping
                        HealthFacilityName = assessment.HealthFacilityName,
                        DateAssessment = assessment.DateAssessment,
                        
                        // Lung Disease - Missing proper mapping
                        HasLungDiseaseNonInfectious = assessment.HasLungDiseaseNonInfectious,
                        
                        // Eye Disease - Missing proper mapping  
                        HasEyeDiseaseCondition = assessment.HasEyeDiseaseCondition
                    };

                    // DEBUGGING: Log Risk Status fields loaded from database
                    _logger.LogInformation("=== RISK STATUS FIELDS LOADED FROM DATABASE ===");
                    _logger.LogInformation("HasDiabetes loaded: '{HasDiabetes}'", assessment.HasDiabetes);
                    _logger.LogInformation("HasHypertension loaded: '{HasHypertension}'", assessment.HasHypertension);
                    _logger.LogInformation("HasCancer loaded: '{HasCancer}'", assessment.HasCancer);
                    _logger.LogInformation("CancerSite loaded: '{CancerSite}'", assessment.CancerSite);
                    _logger.LogInformation("HasCOPD loaded: '{HasCOPD}'", assessment.HasCOPD);
                    _logger.LogInformation("=== END RISK STATUS LOAD DEBUGGING ===");

                    _logger.LogInformation("Basic view model created successfully");
                    
                    // DEBUGGING: Log the view model values after creation
                    _logger.LogInformation("=== VIEW MODEL VALUES AFTER CREATION ===");
                    _logger.LogInformation("HasDiabetes: '{HasDiabetes}'", NCDRiskAssessment.HasDiabetes);
                    _logger.LogInformation("HasChestPain: '{HasChestPain}'", NCDRiskAssessment.HasChestPain);
                    _logger.LogInformation("DrinksAlcohol: '{DrinksAlcohol}'", NCDRiskAssessment.DrinksAlcohol);
                    _logger.LogInformation("HasHistoryOfSmoking: '{HasHistoryOfSmoking}'", NCDRiskAssessment.HasHistoryOfSmoking);
                    _logger.LogInformation("HasStress: '{HasStress}'", NCDRiskAssessment.HasStress);
                    _logger.LogInformation("EatsVegetablesDaily: '{EatsVegetablesDaily}'", NCDRiskAssessment.EatsVegetablesDaily);
                    _logger.LogInformation("FamilyHistoryHeartDiseaseFather: '{FamilyHistoryHeartDiseaseFather}'", NCDRiskAssessment.FamilyHistoryHeartDiseaseFather);
                    
                    // DEBUGGING: Log the values before normalization
                    _logger.LogInformation("=== DEBUGGING: Values before normalization ===");
                    _logger.LogInformation("HasChestPain: '{HasChestPain}'", assessment.HasChestPain);
                    _logger.LogInformation("ChestPainSpreadsToArm: '{ChestPainSpreadsToArm}'", assessment.ChestPainSpreadsToArm);
                    _logger.LogInformation("DrinksAlcohol: '{DrinksAlcohol}'", assessment.DrinksAlcohol);
                    _logger.LogInformation("HasHistoryOfSmoking: '{HasHistoryOfSmoking}'", assessment.HasHistoryOfSmoking);
                    _logger.LogInformation("HasStress: '{HasStress}'", assessment.HasStress);
                    _logger.LogInformation("HasStrokeSymptoms: '{HasStrokeSymptoms}'", assessment.HasStrokeSymptoms);

                    AppointmentId = appointmentId.Value;
                    UserId = assessment.UserId;
                    
                    // Get patient name from appointment
                    var appointment = await _context.Appointments
                        .Include(a => a.Patient)
                        .ThenInclude(p => p.User)
                        .FirstOrDefaultAsync(a => a.Id == appointmentId.Value);
                    
                    if (appointment?.Patient?.User != null)
                    {
                        PatientName = $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}";
                    }
                    else
                    {
                        PatientName = "Unknown Patient";
                    }
                    // Normalize legacy boolean-like strings for safe checkbox binding
                    _logger.LogInformation("Starting normalization process...");
                    NormalizeCheckboxStrings(NCDRiskAssessment);
                    
                    // DEBUGGING: Log the values after normalization
                    _logger.LogInformation("=== DEBUGGING: Values after normalization ===");
                    _logger.LogInformation("HasDiabetes: '{HasDiabetes}'", NCDRiskAssessment.HasDiabetes);
                    _logger.LogInformation("HasChestPain: '{HasChestPain}'", NCDRiskAssessment.HasChestPain);
                    _logger.LogInformation("ChestPainSpreadsToArm: '{ChestPainSpreadsToArm}'", NCDRiskAssessment.ChestPainSpreadsToArm);
                    _logger.LogInformation("DrinksAlcohol: '{DrinksAlcohol}'", NCDRiskAssessment.DrinksAlcohol);
                    _logger.LogInformation("HasHistoryOfSmoking: '{HasHistoryOfSmoking}'", NCDRiskAssessment.HasHistoryOfSmoking);
                    _logger.LogInformation("HasStress: '{HasStress}'", NCDRiskAssessment.HasStress);
                    _logger.LogInformation("HasStrokeSymptoms: '{HasStrokeSymptoms}'", NCDRiskAssessment.HasStrokeSymptoms);
                    _logger.LogInformation("EatsVegetablesDaily: '{EatsVegetablesDaily}'", NCDRiskAssessment.EatsVegetablesDaily);
                    _logger.LogInformation("FamilyHistoryHeartDiseaseFather: '{FamilyHistoryHeartDiseaseFather}'", NCDRiskAssessment.FamilyHistoryHeartDiseaseFather);
                    _logger.LogInformation("ModerateIntensityExercise: '{ModerateIntensityExercise}'", NCDRiskAssessment.ModerateIntensityExercise);
                    _logger.LogInformation("FormerSmoker: '{FormerSmoker}'", NCDRiskAssessment.FormerSmoker);
                    _logger.LogInformation("HasPolyuria: '{HasPolyuria}'", NCDRiskAssessment.HasPolyuria);
                    _logger.LogInformation("BreastCancerScreened: '{BreastCancerScreened}'", NCDRiskAssessment.BreastCancerScreened);
                }
                catch (Exception viewModelEx)
                {
                    _logger.LogError(viewModelEx, "Exception creating view model for appointment {AppointmentId}", appointmentId);
                    throw; // Re-throw to be caught by outer catch
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading NCD assessment for editing, appointment {AppointmentId}", appointmentId);
                TempData["StatusMessage"] = "Error: Unable to load assessment for editing.";
                return IsDoctorRole() ? RedirectToPage("/Doctor/Consultation", new { id = appointmentId }) : RedirectToPage("/Nurse/AppointmentDetails", new { id = appointmentId });
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // DEBUGGING: Log all form data received
                _logger.LogInformation("=== FORM SUBMISSION DEBUGGING STARTED ===");
                _logger.LogInformation("ModelState.IsValid: {IsValid}", ModelState.IsValid);
                
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Model state is invalid for NCD assessment update");
                    _logger.LogWarning("ModelState errors:");
                    foreach (var error in ModelState)
                    {
                        if (error.Value.Errors.Count > 0)
                        {
                            _logger.LogWarning("Field: {Field}, Errors: {Errors}", 
                                error.Key, string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                        }
                    }
                    return Page();
                }

                if (NCDRiskAssessment?.AppointmentId == null)
                {
                    _logger.LogWarning("Appointment ID is missing from form data");
                    TempData["StatusMessage"] = "Error: Appointment ID is required.";
                    return IsDoctorRole() ? RedirectToPage("/Doctor/Consultations") : RedirectToPage("/Nurse/Appointments");
                }

                _logger.LogInformation("Processing NCD assessment update for appointment {AppointmentId}", NCDRiskAssessment.AppointmentId);
                
                // DEBUGGING: Log received form data
                _logger.LogInformation("=== RECEIVED FORM DATA ===");
                _logger.LogInformation("AppointmentId: {AppointmentId}", NCDRiskAssessment.AppointmentId);
                _logger.LogInformation("UserId: {UserId}", NCDRiskAssessment.UserId);
                _logger.LogInformation("FirstName: {FirstName}", NCDRiskAssessment.FirstName);
                _logger.LogInformation("LastName: {LastName}", NCDRiskAssessment.LastName);
                
                // DEBUGGING: Log medical history checkboxes
                _logger.LogInformation("=== MEDICAL HISTORY CHECKBOXES ===");
                _logger.LogInformation("HasDiabetes: '{HasDiabetes}'", NCDRiskAssessment.HasDiabetes);
                _logger.LogInformation("HasHypertension: '{HasHypertension}'", NCDRiskAssessment.HasHypertension);
                _logger.LogInformation("HasCancer: '{HasCancer}'", NCDRiskAssessment.HasCancer);
                _logger.LogInformation("HasLungDisease: '{HasLungDisease}'", NCDRiskAssessment.HasLungDisease);
                _logger.LogInformation("HasEyeDisease: '{HasEyeDisease}'", NCDRiskAssessment.HasEyeDisease);
                _logger.LogInformation("HasAsthma: '{HasAsthma}'", NCDRiskAssessment.HasAsthma);
                _logger.LogInformation("HasDifficultyBreathing: '{HasDifficultyBreathing}'", NCDRiskAssessment.HasDifficultyBreathing);
                _logger.LogInformation("HasStrokeSymptoms: '{HasStrokeSymptoms}'", NCDRiskAssessment.HasStrokeSymptoms);
                
                // DEBUGGING: Log chest pain radio buttons
                _logger.LogInformation("=== CHEST PAIN RADIO BUTTONS ===");
                _logger.LogInformation("HasChestPain: '{HasChestPain}'", NCDRiskAssessment.HasChestPain);
                _logger.LogInformation("ChestPainSpreadsToArm: '{ChestPainSpreadsToArm}'", NCDRiskAssessment.ChestPainSpreadsToArm);
                _logger.LogInformation("NumbnessWhenWalkingFast: '{NumbnessWhenWalkingFast}'", NCDRiskAssessment.NumbnessWhenWalkingFast);
                _logger.LogInformation("PainRelievedWithRest: '{PainRelievedWithRest}'", NCDRiskAssessment.PainRelievedWithRest);
                _logger.LogInformation("LossOfConsciousnessLessThan10Min: '{LossOfConsciousnessLessThan10Min}'", NCDRiskAssessment.LossOfConsciousnessLessThan10Min);
                _logger.LogInformation("PainLastsMoreThan30Min: '{PainLastsMoreThan30Min}'", NCDRiskAssessment.PainLastsMoreThan30Min);
                _logger.LogInformation("SeeDoctorIfYes: '{SeeDoctorIfYes}'", NCDRiskAssessment.SeeDoctorIfYes);
                
                // DEBUGGING: Log family history checkboxes
                _logger.LogInformation("=== FAMILY HISTORY CHECKBOXES ===");
                _logger.LogInformation("FamilyHistoryHeartDiseaseFather: '{FamilyHistoryHeartDiseaseFather}'", NCDRiskAssessment.FamilyHistoryHeartDiseaseFather);
                _logger.LogInformation("FamilyHistoryStrokeFather: '{FamilyHistoryStrokeFather}'", NCDRiskAssessment.FamilyHistoryStrokeFather);
                _logger.LogInformation("FamilyHistoryDiabetesFather: '{FamilyHistoryDiabetesFather}'", NCDRiskAssessment.FamilyHistoryDiabetesFather);
                _logger.LogInformation("FamilyHistoryCancerFather: '{FamilyHistoryCancerFather}'", NCDRiskAssessment.FamilyHistoryCancerFather);
                _logger.LogInformation("FamilyHistoryLungDiseaseFather: '{FamilyHistoryLungDiseaseFather}'", NCDRiskAssessment.FamilyHistoryLungDiseaseFather);
                _logger.LogInformation("FamilyHistoryKidneyDiseaseFather: '{FamilyHistoryKidneyDiseaseFather}'", NCDRiskAssessment.FamilyHistoryKidneyDiseaseFather);
                
                // DEBUGGING: Log nutrition checkboxes
                _logger.LogInformation("=== NUTRITION CHECKBOXES ===");
                _logger.LogInformation("EatsVegetablesDaily: '{EatsVegetablesDaily}'", NCDRiskAssessment.EatsVegetablesDaily);
                _logger.LogInformation("EatsFruitsDaily: '{EatsFruitsDaily}'", NCDRiskAssessment.EatsFruitsDaily);
                _logger.LogInformation("EatsFishDaily: '{EatsFishDaily}'", NCDRiskAssessment.EatsFishDaily);
                _logger.LogInformation("EatsMeatDaily: '{EatsMeatDaily}'", NCDRiskAssessment.EatsMeatDaily);
                _logger.LogInformation("HasUnhealthyDiet: '{HasUnhealthyDiet}'", NCDRiskAssessment.HasUnhealthyDiet);
                _logger.LogInformation("EatsSweetFoodMoreThan2TimesPerWeek: '{EatsSweetFoodMoreThan2TimesPerWeek}'", NCDRiskAssessment.EatsSweetFoodMoreThan2TimesPerWeek);
                _logger.LogInformation("HasHighSaltIntake: '{HasHighSaltIntake}'", NCDRiskAssessment.HasHighSaltIntake);
                _logger.LogInformation("EatsFattyFoodMoreThan2TimesPerWeek: '{EatsFattyFoodMoreThan2TimesPerWeek}'", NCDRiskAssessment.EatsFattyFoodMoreThan2TimesPerWeek);
                
                // DEBUGGING: Log alcohol radio buttons and checkboxes
                _logger.LogInformation("=== ALCOHOL RADIO BUTTONS AND CHECKBOXES ===");
                _logger.LogInformation("DrinksAlcohol: '{DrinksAlcohol}'", NCDRiskAssessment.DrinksAlcohol);
                _logger.LogInformation("AlcoholAmount1Bottle320ml: '{AlcoholAmount1Bottle320ml}'", NCDRiskAssessment.AlcoholAmount1Bottle320ml);
                _logger.LogInformation("AlcoholAmount2Bottle640ml: '{AlcoholAmount2Bottle640ml}'", NCDRiskAssessment.AlcoholAmount2Bottle640ml);
                _logger.LogInformation("AlcoholAmountLessThan3Shot45ml: '{AlcoholAmountLessThan3Shot45ml}'", NCDRiskAssessment.AlcoholAmountLessThan3Shot45ml);
                _logger.LogInformation("AlcoholFrequency1to3TimesPerWeek: '{AlcoholFrequency1to3TimesPerWeek}'", NCDRiskAssessment.AlcoholFrequency1to3TimesPerWeek);
                _logger.LogInformation("AlcoholFrequencyMoreThan4TimesPerWeek: '{AlcoholFrequencyMoreThan4TimesPerWeek}'", NCDRiskAssessment.AlcoholFrequencyMoreThan4TimesPerWeek);
                
                // DEBUGGING: Log exercise checkboxes
                _logger.LogInformation("=== EXERCISE CHECKBOXES ===");
                _logger.LogInformation("ModerateIntensityExercise: '{ModerateIntensityExercise}'", NCDRiskAssessment.ModerateIntensityExercise);
                _logger.LogInformation("VigorousIntensityExercise: '{VigorousIntensityExercise}'", NCDRiskAssessment.VigorousIntensityExercise);
                _logger.LogInformation("InsufficientPhysicalActivity: '{InsufficientPhysicalActivity}'", NCDRiskAssessment.InsufficientPhysicalActivity);
                
                // DEBUGGING: Log smoking radio buttons and checkboxes
                _logger.LogInformation("=== SMOKING RADIO BUTTONS AND CHECKBOXES ===");
                _logger.LogInformation("HasHistoryOfSmoking: '{HasHistoryOfSmoking}'", NCDRiskAssessment.HasHistoryOfSmoking);
                _logger.LogInformation("FormerSmoker: '{FormerSmoker}'", NCDRiskAssessment.FormerSmoker);
                _logger.LogInformation("NeverSmokedButExposedToSmoke: '{NeverSmokedButExposedToSmoke}'", NCDRiskAssessment.NeverSmokedButExposedToSmoke);
                _logger.LogInformation("Smoked100Sticks: '{Smoked100Sticks}'", NCDRiskAssessment.Smoked100Sticks);
                
                // DEBUGGING: Log stress radio buttons
                _logger.LogInformation("=== STRESS RADIO BUTTONS ===");
                _logger.LogInformation("HasStress: '{HasStress}'", NCDRiskAssessment.HasStress);
                
                // DEBUGGING: Log blood sugar checkboxes
                _logger.LogInformation("=== BLOOD SUGAR CHECKBOXES ===");
                _logger.LogInformation("HasPolyuria: '{HasPolyuria}'", NCDRiskAssessment.HasPolyuria);
                _logger.LogInformation("HasPolydipsia: '{HasPolydipsia}'", NCDRiskAssessment.HasPolydipsia);
                _logger.LogInformation("HasPolyphagia: '{HasPolyphagia}'", NCDRiskAssessment.HasPolyphagia);
                _logger.LogInformation("HasWeightLoss: '{HasWeightLoss}'", NCDRiskAssessment.HasWeightLoss);
                
                // DEBUGGING: Log urine checkboxes
                _logger.LogInformation("=== URINE CHECKBOXES ===");
                _logger.LogInformation("HasUrineProtein: '{HasUrineProtein}'", NCDRiskAssessment.HasUrineProtein);
                _logger.LogInformation("HasUrineKetones: '{HasUrineKetones}'", NCDRiskAssessment.HasUrineKetones);
                
                // DEBUGGING: Log cancer screening checkboxes
                _logger.LogInformation("=== CANCER SCREENING CHECKBOXES ===");
                _logger.LogInformation("BreastCancerScreened: '{BreastCancerScreened}'", NCDRiskAssessment.BreastCancerScreened);
                _logger.LogInformation("CervicalCancerScreened: '{CervicalCancerScreened}'", NCDRiskAssessment.CervicalCancerScreened);

                // Find the existing assessment
                var existingAssessment = await _context.NCDRiskAssessments
                    .FirstOrDefaultAsync(a => a.AppointmentId == NCDRiskAssessment.AppointmentId);

                if (existingAssessment == null)
                {
                    _logger.LogWarning("No existing NCD assessment found for appointment {AppointmentId}", NCDRiskAssessment.AppointmentId);
                    TempData["StatusMessage"] = "Error: Assessment not found.";
                    return IsDoctorRole() ? RedirectToPage("/Doctor/Consultation", new { id = NCDRiskAssessment.AppointmentId }) : RedirectToPage("/Nurse/AppointmentDetails", new { id = NCDRiskAssessment.AppointmentId });
                }

                // DEBUGGING: Log before updating assessment data
                _logger.LogInformation("=== UPDATING ASSESSMENT DATA ===");
                
                // Update the assessment with form data
                // Demographics
                _logger.LogInformation("Updating demographics...");
                existingAssessment.HealthFacility = NCDRiskAssessment.HealthFacility;
                existingAssessment.FamilyNo = NCDRiskAssessment.FamilyNo;
                existingAssessment.Address = NCDRiskAssessment.Address;
                existingAssessment.FirstName = NCDRiskAssessment.FirstName;
                existingAssessment.LastName = NCDRiskAssessment.LastName;
                existingAssessment.MiddleName = NCDRiskAssessment.MiddleName;
                existingAssessment.DateOfAssessment = NCDRiskAssessment.DateOfAssessment?.ToString("yyyy-MM-dd HH:mm:ss");
                existingAssessment.IDNumber = NCDRiskAssessment.IDNumber;
                existingAssessment.IDNo = NCDRiskAssessment.IDNo;
                existingAssessment.Barangay = NCDRiskAssessment.Barangay;
                existingAssessment.Telepono = NCDRiskAssessment.Telepono;
                existingAssessment.Birthday = NCDRiskAssessment.Birthday?.ToString("yyyy-MM-dd HH:mm:ss");
                existingAssessment.Edad = NCDRiskAssessment.Edad;
                existingAssessment.Kasarian = NCDRiskAssessment.Kasarian;
                existingAssessment.Relihiyon = NCDRiskAssessment.Relihiyon;
                existingAssessment.Occupation = NCDRiskAssessment.Occupation;
                existingAssessment.CivilStatus = NCDRiskAssessment.CivilStatus;
                
                // Medical History
                _logger.LogInformation("Updating medical history...");
                existingAssessment.HasDiabetes = NCDRiskAssessment.HasDiabetes;
                existingAssessment.HasHypertension = NCDRiskAssessment.HasHypertension;
                existingAssessment.HasCancer = NCDRiskAssessment.HasCancer;
                existingAssessment.HasCOPD = NCDRiskAssessment.HasCOPD;
                existingAssessment.COPDYear = NCDRiskAssessment.COPDYear;
                existingAssessment.COPDMedication = NCDRiskAssessment.COPDMedication;
                existingAssessment.HasLungDisease = NCDRiskAssessment.HasLungDisease;
                existingAssessment.HasEyeDisease = NCDRiskAssessment.HasEyeDisease;
                existingAssessment.CancerType = NCDRiskAssessment.CancerType;
                existingAssessment.CancerSite = NCDRiskAssessment.CancerSite;
                existingAssessment.CancerYear = NCDRiskAssessment.CancerYear;
                existingAssessment.CancerMedication = NCDRiskAssessment.CancerMedication;
                existingAssessment.DiabetesYear = NCDRiskAssessment.DiabetesYear;
                existingAssessment.DiabetesMedication = NCDRiskAssessment.DiabetesMedication;
                existingAssessment.HypertensionYear = NCDRiskAssessment.HypertensionYear;
                existingAssessment.HypertensionMedication = NCDRiskAssessment.HypertensionMedication;
                existingAssessment.LungDiseaseYear = NCDRiskAssessment.LungDiseaseYear;
                existingAssessment.LungDiseaseMedication = NCDRiskAssessment.LungDiseaseMedication;
                existingAssessment.EyeDiseaseYear = NCDRiskAssessment.EyeDiseaseYear;
                existingAssessment.EyeDiseaseMedication = NCDRiskAssessment.EyeDiseaseMedication;
                existingAssessment.HasAsthma = NCDRiskAssessment.HasAsthma;
                existingAssessment.HasDifficultyBreathing = NCDRiskAssessment.HasDifficultyBreathing;
                existingAssessment.HasStrokeSymptoms = NCDRiskAssessment.HasStrokeSymptoms;
                
                // DEBUGGING: Log medical history values after assignment
                _logger.LogInformation("Medical history values assigned:");
                _logger.LogInformation("  HasDiabetes: '{HasDiabetes}'", existingAssessment.HasDiabetes);
                _logger.LogInformation("  HasHypertension: '{HasHypertension}'", existingAssessment.HasHypertension);
                _logger.LogInformation("  HasCancer: '{HasCancer}'", existingAssessment.HasCancer);
                _logger.LogInformation("  HasAsthma: '{HasAsthma}'", existingAssessment.HasAsthma);
                _logger.LogInformation("  HasStrokeSymptoms: '{HasStrokeSymptoms}'", existingAssessment.HasStrokeSymptoms);
                
                // Chest Pain and Symptoms
                existingAssessment.HasChestPain = NCDRiskAssessment.HasChestPain;
                existingAssessment.ChestPainSpreadsToArm = NCDRiskAssessment.ChestPainSpreadsToArm;
                existingAssessment.NumbnessWhenWalkingFast = NCDRiskAssessment.NumbnessWhenWalkingFast;
                existingAssessment.PainRelievedWithRest = NCDRiskAssessment.PainRelievedWithRest;
                existingAssessment.LossOfConsciousnessLessThan10Min = NCDRiskAssessment.LossOfConsciousnessLessThan10Min;
                existingAssessment.PainLastsMoreThan30Min = NCDRiskAssessment.PainLastsMoreThan30Min;
                existingAssessment.SeeDoctorIfYes = NCDRiskAssessment.SeeDoctorIfYes;
                existingAssessment.DoctorName = NCDRiskAssessment.DoctorName;
                
                // Family History
                existingAssessment.FamilyHasHypertension = NCDRiskAssessment.FamilyHasHypertension;
                existingAssessment.FamilyHasHeartDisease = NCDRiskAssessment.FamilyHasHeartDisease;
                existingAssessment.FamilyHasStroke = NCDRiskAssessment.FamilyHasStroke;
                existingAssessment.FamilyHasDiabetes = NCDRiskAssessment.FamilyHasDiabetes;
                existingAssessment.FamilyHasCancer = NCDRiskAssessment.FamilyHasCancer;
                existingAssessment.FamilyHasKidneyDisease = NCDRiskAssessment.FamilyHasKidneyDisease;
                existingAssessment.FamilyHasOtherDisease = NCDRiskAssessment.FamilyHasOtherDisease;
                existingAssessment.FamilyOtherDiseaseDetails = NCDRiskAssessment.FamilyOtherDiseaseDetails;
                
                // Detailed Family History
                existingAssessment.FamilyHistoryCancerFather = NCDRiskAssessment.FamilyHistoryCancerFather;
                existingAssessment.FamilyHistoryCancerMother = NCDRiskAssessment.FamilyHistoryCancerMother;
                existingAssessment.FamilyHistoryCancerSibling = NCDRiskAssessment.FamilyHistoryCancerSibling;
                existingAssessment.FamilyHistoryDiabetesFather = NCDRiskAssessment.FamilyHistoryDiabetesFather;
                existingAssessment.FamilyHistoryDiabetesMother = NCDRiskAssessment.FamilyHistoryDiabetesMother;
                existingAssessment.FamilyHistoryDiabetesSibling = NCDRiskAssessment.FamilyHistoryDiabetesSibling;
                existingAssessment.FamilyHistoryHeartDiseaseFather = NCDRiskAssessment.FamilyHistoryHeartDiseaseFather;
                existingAssessment.FamilyHistoryHeartDiseaseMother = NCDRiskAssessment.FamilyHistoryHeartDiseaseMother;
                existingAssessment.FamilyHistoryHeartDiseaseSibling = NCDRiskAssessment.FamilyHistoryHeartDiseaseSibling;
                existingAssessment.FamilyHistoryLungDiseaseFather = NCDRiskAssessment.FamilyHistoryLungDiseaseFather;
                existingAssessment.FamilyHistoryLungDiseaseMother = NCDRiskAssessment.FamilyHistoryLungDiseaseMother;
                existingAssessment.FamilyHistoryLungDiseaseSibling = NCDRiskAssessment.FamilyHistoryLungDiseaseSibling;
                existingAssessment.FamilyHistoryStrokeFather = NCDRiskAssessment.FamilyHistoryStrokeFather;
                existingAssessment.FamilyHistoryStrokeMother = NCDRiskAssessment.FamilyHistoryStrokeMother;
                existingAssessment.FamilyHistoryStrokeSibling = NCDRiskAssessment.FamilyHistoryStrokeSibling;
                existingAssessment.FamilyHistoryKidneyDiseaseFather = NCDRiskAssessment.FamilyHistoryKidneyDiseaseFather;
                existingAssessment.FamilyHistoryKidneyDiseaseMother = NCDRiskAssessment.FamilyHistoryKidneyDiseaseMother;
                existingAssessment.FamilyHistoryKidneyDiseaseSibling = NCDRiskAssessment.FamilyHistoryKidneyDiseaseSibling;
                existingAssessment.FamilyHistoryEyeDiseaseFather = NCDRiskAssessment.FamilyHistoryEyeDiseaseFather;
                existingAssessment.FamilyHistoryEyeDiseaseMother = NCDRiskAssessment.FamilyHistoryEyeDiseaseMother;
                existingAssessment.FamilyHistoryEyeDiseaseSibling = NCDRiskAssessment.FamilyHistoryEyeDiseaseSibling;
                existingAssessment.FamilyHistoryOther = NCDRiskAssessment.FamilyHistoryOther;
                existingAssessment.FamilyHistoryOtherFather = NCDRiskAssessment.FamilyHistoryOtherFather;
                existingAssessment.FamilyHistoryOtherMother = NCDRiskAssessment.FamilyHistoryOtherMother;
                existingAssessment.FamilyHistoryOtherSibling = NCDRiskAssessment.FamilyHistoryOtherSibling;
                
                // Lifestyle Factors
                existingAssessment.EatsVegetablesDaily = NCDRiskAssessment.EatsVegetablesDaily;
                existingAssessment.EatsFruitsDaily = NCDRiskAssessment.EatsFruitsDaily;
                existingAssessment.EatsFishDaily = NCDRiskAssessment.EatsFishDaily;
                existingAssessment.EatsMeatDaily = NCDRiskAssessment.EatsMeatDaily;
                existingAssessment.HasUnhealthyDiet = NCDRiskAssessment.HasUnhealthyDiet;
                existingAssessment.EatsFattyFoodMoreThan2TimesPerWeek = NCDRiskAssessment.EatsFattyFoodMoreThan2TimesPerWeek;
                existingAssessment.EatsSweetFoodMoreThan2TimesPerWeek = NCDRiskAssessment.EatsSweetFoodMoreThan2TimesPerWeek;
                existingAssessment.EatsOilyFoodMoreThan2TimesPerWeek = NCDRiskAssessment.EatsOilyFoodMoreThan2TimesPerWeek;
                existingAssessment.HasHighSaltIntake = NCDRiskAssessment.HasHighSaltIntake;
                
                // Alcohol
                existingAssessment.DrinksAlcohol = NCDRiskAssessment.DrinksAlcohol;
                existingAssessment.DrinksBeer = NCDRiskAssessment.DrinksBeer;
                existingAssessment.DrinksWine = NCDRiskAssessment.DrinksWine;
                existingAssessment.DrinksWhiskyGinBrandy = NCDRiskAssessment.DrinksWhiskyGinBrandy;
                existingAssessment.AlcoholAmount1Bottle320ml = NCDRiskAssessment.AlcoholAmount1Bottle320ml;
                existingAssessment.AlcoholAmount2Bottle640ml = NCDRiskAssessment.AlcoholAmount2Bottle640ml;
                existingAssessment.AlcoholAmountLessThan3Shot45ml = NCDRiskAssessment.AlcoholAmountLessThan3Shot45ml;
                existingAssessment.AlcoholAmount3to4WineGlasses300ml = NCDRiskAssessment.AlcoholAmount3to4WineGlasses300ml;
                existingAssessment.AlcoholAmountMoreThan4Shots75ml = NCDRiskAssessment.AlcoholAmountMoreThan4Shots75ml;
                existingAssessment.BeerConsumption3 = NCDRiskAssessment.BeerConsumption3;
                existingAssessment.WineConsumption2 = NCDRiskAssessment.WineConsumption2;
                existingAssessment.AlcoholFrequency1to3TimesPerWeek = NCDRiskAssessment.AlcoholFrequency1to3TimesPerWeek;
                existingAssessment.AlcoholFrequencyMoreThan4TimesPerWeek = NCDRiskAssessment.AlcoholFrequencyMoreThan4TimesPerWeek;
                existingAssessment.IsBingeDrinker = NCDRiskAssessment.IsBingeDrinker;
                existingAssessment.AlcoholStoppedDuration = NCDRiskAssessment.AlcoholStoppedDuration;
                existingAssessment.AlcoholPerOccasion = NCDRiskAssessment.AlcoholPerOccasion;
                
                // Exercise
                existingAssessment.ModerateIntensityExercise = NCDRiskAssessment.ModerateIntensityExercise;
                existingAssessment.VigorousIntensityExercise = NCDRiskAssessment.VigorousIntensityExercise;
                existingAssessment.CombinationExercise = NCDRiskAssessment.CombinationExercise;
                existingAssessment.InsufficientPhysicalActivity = NCDRiskAssessment.InsufficientPhysicalActivity;
                existingAssessment.HasEnoughExercise = NCDRiskAssessment.HasEnoughExercise;
                existingAssessment.HasNoRegularExercise = NCDRiskAssessment.HasNoRegularExercise;
                
                // Smoking
                existingAssessment.HasHistoryOfSmoking = NCDRiskAssessment.HasHistoryOfSmoking;
                existingAssessment.FormerSmoker = NCDRiskAssessment.FormerSmoker;
                existingAssessment.NeverSmokedButExposedToSmoke = NCDRiskAssessment.NeverSmokedButExposedToSmoke;
                existingAssessment.Smoked100Sticks = NCDRiskAssessment.Smoked100Sticks;
                existingAssessment.SmokingStatus = NCDRiskAssessment.SmokingStatus;
                existingAssessment.SmokingQuitDuration = NCDRiskAssessment.SmokingQuitDuration;
                
                // Risk Status
                existingAssessment.HasDiabetes = NCDRiskAssessment.HasDiabetes;
                existingAssessment.HasHypertension = NCDRiskAssessment.HasHypertension;
                existingAssessment.HasCancer = NCDRiskAssessment.HasCancer;
                existingAssessment.CancerSite = NCDRiskAssessment.CancerSite;
                existingAssessment.HasCOPD = NCDRiskAssessment.HasCOPD;
                
                // DEBUGGING: Log Risk Status fields before save
                _logger.LogInformation("=== RISK STATUS FIELDS UPDATE DEBUGGING ===");
                _logger.LogInformation("HasDiabetes: '{HasDiabetes}' (from ViewModel: '{ViewModelValue}')", 
                    existingAssessment.HasDiabetes, NCDRiskAssessment.HasDiabetes);
                _logger.LogInformation("HasHypertension: '{HasHypertension}' (from ViewModel: '{ViewModelValue}')", 
                    existingAssessment.HasHypertension, NCDRiskAssessment.HasHypertension);
                _logger.LogInformation("HasCancer: '{HasCancer}' (from ViewModel: '{ViewModelValue}')", 
                    existingAssessment.HasCancer, NCDRiskAssessment.HasCancer);
                _logger.LogInformation("CancerSite: '{CancerSite}' (from ViewModel: '{ViewModelValue}')", 
                    existingAssessment.CancerSite, NCDRiskAssessment.CancerSite);
                _logger.LogInformation("HasCOPD: '{HasCOPD}' (from ViewModel: '{ViewModelValue}')", 
                    existingAssessment.HasCOPD, NCDRiskAssessment.HasCOPD);
                _logger.LogInformation("=== END RISK STATUS UPDATE DEBUGGING ===");
                
                // Stress
                existingAssessment.HasStress = NCDRiskAssessment.HasStress;
                
                // Anthropometric Measurements
                existingAssessment.Weight = NCDRiskAssessment.Weight;
                existingAssessment.Height = NCDRiskAssessment.Height;
                existingAssessment.BMI = NCDRiskAssessment.BMI;
                existingAssessment.Waist = NCDRiskAssessment.Waist;
                existingAssessment.Hip = NCDRiskAssessment.Hip;
                existingAssessment.WHRatio = NCDRiskAssessment.WHRatio;
                existingAssessment.BMIStatus = NCDRiskAssessment.BMIStatus;
                existingAssessment.WHStatus = NCDRiskAssessment.WHStatus;
                
                // Blood Sugar
                existingAssessment.FastingBloodSugar = NCDRiskAssessment.FastingBloodSugar;
                existingAssessment.RandomBloodSugar = NCDRiskAssessment.RandomBloodSugar;
                existingAssessment.BloodSugarStatus = NCDRiskAssessment.BloodSugarStatus;
                existingAssessment.HasPolyuria = NCDRiskAssessment.HasPolyuria;
                existingAssessment.HasPolydipsia = NCDRiskAssessment.HasPolydipsia;
                existingAssessment.HasPolyphagia = NCDRiskAssessment.HasPolyphagia;
                existingAssessment.HasWeightLoss = NCDRiskAssessment.HasWeightLoss;
                
                // Blood Pressure
                existingAssessment.LeftArmMeanBP = NCDRiskAssessment.LeftArmMeanBP;
                existingAssessment.RightArmMeanBP = NCDRiskAssessment.RightArmMeanBP;
                existingAssessment.BaselineBP = NCDRiskAssessment.BaselineBP;
                existingAssessment.BPStatus = NCDRiskAssessment.BPStatus;
                
                // Cholesterol
                existingAssessment.CholesterolResult = NCDRiskAssessment.CholesterolResult;
                existingAssessment.CholesterolStatus = NCDRiskAssessment.CholesterolStatus;
                
                // Urine
                existingAssessment.UrineProtein = NCDRiskAssessment.UrineProtein;
                existingAssessment.UrineKetones = NCDRiskAssessment.UrineKetones;
                existingAssessment.HasUrineProtein = NCDRiskAssessment.HasUrineProtein;
                existingAssessment.HasUrineKetones = NCDRiskAssessment.HasUrineKetones;
                
                // Risk Profile
                existingAssessment.RiskPercentage = NCDRiskAssessment.RiskPercentage;
                
                // Cancer Screening
                existingAssessment.BreastCancerScreened = NCDRiskAssessment.BreastCancerScreened;
                existingAssessment.CervicalCancerScreened = NCDRiskAssessment.CervicalCancerScreened;
                existingAssessment.CancerScreeningStatus = NCDRiskAssessment.CancerScreeningStatus;
                
                // Assessment Information
                existingAssessment.InterviewedBy = NCDRiskAssessment.InterviewedBy;
                existingAssessment.Designation = NCDRiskAssessment.Designation;
                existingAssessment.AssessmentDate = NCDRiskAssessment.AssessmentDate;
                existingAssessment.PatientSignature = NCDRiskAssessment.PatientSignature;
                
                // MISSING UPDATE FIELDS - Add these critical missing fields
                
                // Pananakit (Chest Pain) Questions 2.1-2.8
                existingAssessment.Pananakit21 = NCDRiskAssessment.Pananakit21;
                existingAssessment.Pananakit22 = NCDRiskAssessment.Pananakit22;
                existingAssessment.Pananakit23 = NCDRiskAssessment.Pananakit23;
                existingAssessment.Pananakit24 = NCDRiskAssessment.Pananakit24;
                existingAssessment.Pananakit25 = NCDRiskAssessment.Pananakit25;
                existingAssessment.Pananakit26 = NCDRiskAssessment.Pananakit26;
                existingAssessment.Pananakit27 = NCDRiskAssessment.Pananakit27;
                existingAssessment.Pananakit28 = NCDRiskAssessment.Pananakit28;
                
                // Nutrition - Missing detailed mappings
                existingAssessment.NutrisyonMadalasGulay = NCDRiskAssessment.NutrisyonMadalasGulay;
                existingAssessment.NutrisyonMadalasPratas = NCDRiskAssessment.NutrisyonMadalasPratas;
                existingAssessment.NutrisyonMadalasIsda = NCDRiskAssessment.NutrisyonMadalasIsda;
                existingAssessment.NutrisyonMadalasKarne = NCDRiskAssessment.NutrisyonMadalasKarne;
                existingAssessment.NutrisyonKumakainMatatamis = NCDRiskAssessment.NutrisyonKumakainMatatamis;
                existingAssessment.NutrisyonKumakainMamantika = NCDRiskAssessment.NutrisyonKumakainMamantika;
                
                // Alcohol - Missing detailed mappings
                existingAssessment.AlcoholInom = NCDRiskAssessment.AlcoholInom;
                existingAssessment.AlchoholTypeBeer = NCDRiskAssessment.AlchoholTypeBeer;
                existingAssessment.AlchoholTypeWine = NCDRiskAssessment.AlchoholTypeWine;
                existingAssessment.AlchoholTypeWhisky = NCDRiskAssessment.AlchoholTypeWhisky;
                existingAssessment.BeerConsumption1 = NCDRiskAssessment.BeerConsumption1;
                existingAssessment.BeerConsumption2 = NCDRiskAssessment.BeerConsumption2;
                existingAssessment.WineConsumption1 = NCDRiskAssessment.WineConsumption1;
                existingAssessment.WhiskyConsumption1 = NCDRiskAssessment.WhiskyConsumption1;
                existingAssessment.WhiskyConsumption2 = NCDRiskAssessment.WhiskyConsumption2;
                existingAssessment.AlcoholOkasyon = NCDRiskAssessment.AlcoholOkasyon;
                
                // Exercise - Missing detailed mappings
                existingAssessment.EhersisyoRegular = NCDRiskAssessment.EhersisyoRegular;
                existingAssessment.EhersisyoDuration = NCDRiskAssessment.EhersisyoDuration;
                existingAssessment.EhersisyoType = NCDRiskAssessment.EhersisyoType;
                
                // Smoking - Missing detailed mappings
                existingAssessment.SigarilyoKadami = NCDRiskAssessment.SigarilyoKadami;
                existingAssessment.SigarilyoTumigil = NCDRiskAssessment.SigarilyoTumigil;
                existingAssessment.SigarilyoUsok = NCDRiskAssessment.SigarilyoUsok;
                existingAssessment.SigarilyoSticks = NCDRiskAssessment.SigarilyoSticks;
                
                // Stress - Missing detailed mappings
                existingAssessment.StressMadalas = NCDRiskAssessment.StressMadalas;
                existingAssessment.StressSino = NCDRiskAssessment.StressSino;
                existingAssessment.StressEpekto = NCDRiskAssessment.StressEpekto;
                
                // Additional missing fields for complete form mapping
                existingAssessment.HealthFacilityName = NCDRiskAssessment.HealthFacilityName;
                existingAssessment.DateAssessment = NCDRiskAssessment.DateAssessment;
                
                // Lung Disease - Missing proper mapping
                existingAssessment.HasLungDiseaseNonInfectious = NCDRiskAssessment.HasLungDiseaseNonInfectious;
                
                // Eye Disease - Missing proper mapping  
                existingAssessment.HasEyeDiseaseCondition = NCDRiskAssessment.HasEyeDiseaseCondition;

                // DEBUGGING: Log values before encryption
                _logger.LogInformation("=== VALUES BEFORE ENCRYPTION ===");
                _logger.LogInformation("HasDiabetes: '{HasDiabetes}'", existingAssessment.HasDiabetes);
                _logger.LogInformation("HasChestPain: '{HasChestPain}'", existingAssessment.HasChestPain);
                _logger.LogInformation("DrinksAlcohol: '{DrinksAlcohol}'", existingAssessment.DrinksAlcohol);
                _logger.LogInformation("HasHistoryOfSmoking: '{HasHistoryOfSmoking}'", existingAssessment.HasHistoryOfSmoking);
                _logger.LogInformation("HasStress: '{HasStress}'", existingAssessment.HasStress);
                _logger.LogInformation("EatsVegetablesDaily: '{EatsVegetablesDaily}'", existingAssessment.EatsVegetablesDaily);
                _logger.LogInformation("FamilyHistoryHeartDiseaseFather: '{FamilyHistoryHeartDiseaseFather}'", existingAssessment.FamilyHistoryHeartDiseaseFather);

                // Encrypt sensitive data before saving
                try
                {
                    _logger.LogInformation("Starting encryption process...");
                    existingAssessment.EncryptSensitiveData(_encryptionService);
                    _logger.LogInformation("Assessment data encrypted successfully");
                    
                    // DEBUGGING: Log values after encryption
                    _logger.LogInformation("=== VALUES AFTER ENCRYPTION ===");
                    _logger.LogInformation("HasDiabetes: '{HasDiabetes}'", existingAssessment.HasDiabetes?.Substring(0, Math.Min(20, existingAssessment.HasDiabetes?.Length ?? 0)) + "...");
                    _logger.LogInformation("HasChestPain: '{HasChestPain}'", existingAssessment.HasChestPain?.Substring(0, Math.Min(20, existingAssessment.HasChestPain?.Length ?? 0)) + "...");
                    _logger.LogInformation("DrinksAlcohol: '{DrinksAlcohol}'", existingAssessment.DrinksAlcohol?.Substring(0, Math.Min(20, existingAssessment.DrinksAlcohol?.Length ?? 0)) + "...");
                    _logger.LogInformation("HasHistoryOfSmoking: '{HasHistoryOfSmoking}'", existingAssessment.HasHistoryOfSmoking?.Substring(0, Math.Min(20, existingAssessment.HasHistoryOfSmoking?.Length ?? 0)) + "...");
                    _logger.LogInformation("HasStress: '{HasStress}'", existingAssessment.HasStress?.Substring(0, Math.Min(20, existingAssessment.HasStress?.Length ?? 0)) + "...");
                    _logger.LogInformation("EatsVegetablesDaily: '{EatsVegetablesDaily}'", existingAssessment.EatsVegetablesDaily?.Substring(0, Math.Min(20, existingAssessment.EatsVegetablesDaily?.Length ?? 0)) + "...");
                    _logger.LogInformation("FamilyHistoryHeartDiseaseFather: '{FamilyHistoryHeartDiseaseFather}'", existingAssessment.FamilyHistoryHeartDiseaseFather?.Substring(0, Math.Min(20, existingAssessment.FamilyHistoryHeartDiseaseFather?.Length ?? 0)) + "...");
                }
                catch (Exception encryptEx)
                {
                    _logger.LogError(encryptEx, "Failed to encrypt assessment data");
                    TempData["StatusMessage"] = "Error: Failed to save assessment data securely.";
                    return Page();
                }

                // Save changes
                _logger.LogInformation("Saving changes to database...");
                await _context.SaveChangesAsync();
                _logger.LogInformation("NCD assessment updated successfully for appointment {AppointmentId}", NCDRiskAssessment.AppointmentId);
                
                // DEBUGGING: Log successful update with Risk Status fields
                _logger.LogInformation("=== RISK STATUS FIELDS UPDATED IN DATABASE ===");
                _logger.LogInformation("Assessment ID: {Id}", existingAssessment.Id);
                _logger.LogInformation("HasDiabetes updated: '{HasDiabetes}'", existingAssessment.HasDiabetes);
                _logger.LogInformation("HasHypertension updated: '{HasHypertension}'", existingAssessment.HasHypertension);
                _logger.LogInformation("HasCancer updated: '{HasCancer}'", existingAssessment.HasCancer);
                _logger.LogInformation("CancerSite updated: '{CancerSite}'", existingAssessment.CancerSite);
                _logger.LogInformation("HasCOPD updated: '{HasCOPD}'", existingAssessment.HasCOPD);
                _logger.LogInformation("=== END RISK STATUS UPDATE SAVE DEBUGGING ===");

                TempData["StatusMessage"] = "NCD assessment updated successfully.";
                return IsDoctorRole() ? RedirectToPage("/Doctor/Consultation", new { id = NCDRiskAssessment.AppointmentId }) : RedirectToPage("/Nurse/AppointmentDetails", new { id = NCDRiskAssessment.AppointmentId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating NCD assessment for appointment {AppointmentId}", NCDRiskAssessment?.AppointmentId);
                TempData["StatusMessage"] = "Error: Failed to update assessment.";
            return Page();
            }
        }

        // Normalize legacy boolean-like strings to strict 'true'/'false' for checkbox bindings
        private static void NormalizeCheckboxStrings(NCDRiskAssessmentViewModel vm)
        {
            if (vm == null) return;

            // DEBUGGING: Log normalization process
            Console.WriteLine("=== NORMALIZATION DEBUGGING ===");
            Console.WriteLine($"HasDiabetes before: '{vm.HasDiabetes}'");
            Console.WriteLine($"HasChestPain before: '{vm.HasChestPain}'");
            Console.WriteLine($"DrinksAlcohol before: '{vm.DrinksAlcohol}'");
            Console.WriteLine($"EatsVegetablesDaily before: '{vm.EatsVegetablesDaily}'");

            // Medical history
            vm.HasDiabetes = NormalizeBool(vm.HasDiabetes);
            vm.HasHypertension = NormalizeBool(vm.HasHypertension);
            vm.HasCancer = NormalizeBool(vm.HasCancer);
            vm.HasCOPD = NormalizeBool(vm.HasCOPD);
            vm.HasLungDisease = NormalizeBool(vm.HasLungDisease);
            vm.HasEyeDisease = NormalizeBool(vm.HasEyeDisease);
            vm.HasAsthma = NormalizeBool(vm.HasAsthma);
            vm.HasDifficultyBreathing = NormalizeBool(vm.HasDifficultyBreathing);
            vm.HasStrokeSymptoms = NormalizeRadioButton(vm.HasStrokeSymptoms);
            
            Console.WriteLine($"HasDiabetes after: '{vm.HasDiabetes}'");
            Console.WriteLine($"HasChestPain after: '{vm.HasChestPain}'");
            Console.WriteLine($"DrinksAlcohol after: '{vm.DrinksAlcohol}'");
            Console.WriteLine($"EatsVegetablesDaily after: '{vm.EatsVegetablesDaily}'");

            // Chest pain items - Keep Filipino values for radio buttons
            vm.HasChestPain = NormalizeRadioButton(vm.HasChestPain);
            vm.ChestPainSpreadsToArm = NormalizeRadioButton(vm.ChestPainSpreadsToArm);
            vm.NumbnessWhenWalkingFast = NormalizeRadioButton(vm.NumbnessWhenWalkingFast);
            vm.PainRelievedWithRest = NormalizeRadioButton(vm.PainRelievedWithRest);
            vm.LossOfConsciousnessLessThan10Min = NormalizeRadioButton(vm.LossOfConsciousnessLessThan10Min);
            vm.PainLastsMoreThan30Min = NormalizeRadioButton(vm.PainLastsMoreThan30Min);
            vm.SeeDoctorIfYes = NormalizeRadioButton(vm.SeeDoctorIfYes);

            // Aggregated family history flags
            vm.FamilyHasHypertension = NormalizeBool(vm.FamilyHasHypertension);
            vm.FamilyHasHeartDisease = NormalizeBool(vm.FamilyHasHeartDisease);
            vm.FamilyHasStroke = NormalizeBool(vm.FamilyHasStroke);
            vm.FamilyHasDiabetes = NormalizeBool(vm.FamilyHasDiabetes);
            vm.FamilyHasCancer = NormalizeBool(vm.FamilyHasCancer);
            vm.FamilyHasKidneyDisease = NormalizeBool(vm.FamilyHasKidneyDisease);
            vm.FamilyHasOtherDisease = NormalizeBool(vm.FamilyHasOtherDisease);

            // Detailed family history (father)
            vm.FamilyHistoryHypertensionFather = NormalizeBool(vm.FamilyHistoryHypertensionFather);
            vm.FamilyHistoryStrokeFather = NormalizeBool(vm.FamilyHistoryStrokeFather);
            vm.FamilyHistoryDiabetesFather = NormalizeBool(vm.FamilyHistoryDiabetesFather);
            vm.FamilyHistoryCancerFather = NormalizeBool(vm.FamilyHistoryCancerFather);
            vm.FamilyHistoryLungDiseaseFather = NormalizeBool(vm.FamilyHistoryLungDiseaseFather);
            vm.FamilyHistoryKidneyDiseaseFather = NormalizeBool(vm.FamilyHistoryKidneyDiseaseFather);
            vm.FamilyHistoryEyeDiseaseFather = NormalizeBool(vm.FamilyHistoryEyeDiseaseFather);

            // Nutrition
            vm.EatsVegetablesDaily = NormalizeBool(vm.EatsVegetablesDaily);
            vm.EatsFruitsDaily = NormalizeBool(vm.EatsFruitsDaily);
            vm.EatsFishDaily = NormalizeBool(vm.EatsFishDaily);
            vm.EatsMeatDaily = NormalizeBool(vm.EatsMeatDaily);
            vm.HasUnhealthyDiet = NormalizeBool(vm.HasUnhealthyDiet);
            vm.EatsFattyFoodMoreThan2TimesPerWeek = NormalizeBool(vm.EatsFattyFoodMoreThan2TimesPerWeek);
            vm.EatsSweetFoodMoreThan2TimesPerWeek = NormalizeBool(vm.EatsSweetFoodMoreThan2TimesPerWeek);
            vm.EatsOilyFoodMoreThan2TimesPerWeek = NormalizeBool(vm.EatsOilyFoodMoreThan2TimesPerWeek);
            vm.HasHighSaltIntake = NormalizeBool(vm.HasHighSaltIntake);

            // Alcohol details - Keep Filipino values for radio buttons
            vm.DrinksAlcohol = NormalizeRadioButton(vm.DrinksAlcohol);
            vm.DrinksBeer = NormalizeBool(vm.DrinksBeer);
            vm.DrinksWine = NormalizeBool(vm.DrinksWine);
            vm.DrinksWhiskyGinBrandy = NormalizeBool(vm.DrinksWhiskyGinBrandy);
            vm.AlcoholAmount1Bottle320ml = NormalizeBool(vm.AlcoholAmount1Bottle320ml);
            vm.AlcoholAmount2Bottle640ml = NormalizeBool(vm.AlcoholAmount2Bottle640ml);
            vm.AlcoholAmountLessThan3Shot45ml = NormalizeBool(vm.AlcoholAmountLessThan3Shot45ml);
            vm.AlcoholAmount3to4WineGlasses300ml = NormalizeBool(vm.AlcoholAmount3to4WineGlasses300ml);
            vm.AlcoholAmountMoreThan4Shots75ml = NormalizeBool(vm.AlcoholAmountMoreThan4Shots75ml);
            vm.AlcoholFrequency1to3TimesPerWeek = NormalizeBool(vm.AlcoholFrequency1to3TimesPerWeek);
            vm.AlcoholFrequencyMoreThan4TimesPerWeek = NormalizeBool(vm.AlcoholFrequencyMoreThan4TimesPerWeek);
            vm.IsBingeDrinker = NormalizeBool(vm.IsBingeDrinker);

            // Exercise
            vm.ModerateIntensityExercise = NormalizeBool(vm.ModerateIntensityExercise);
            vm.VigorousIntensityExercise = NormalizeBool(vm.VigorousIntensityExercise);
            vm.CombinationExercise = NormalizeBool(vm.CombinationExercise);
            vm.InsufficientPhysicalActivity = NormalizeBool(vm.InsufficientPhysicalActivity);
            vm.HasEnoughExercise = NormalizeBool(vm.HasEnoughExercise);
            vm.HasNoRegularExercise = NormalizeBool(vm.HasNoRegularExercise);

            // Smoking - Keep Filipino values for radio buttons
            vm.HasHistoryOfSmoking = NormalizeRadioButton(vm.HasHistoryOfSmoking);
            vm.FormerSmoker = NormalizeBool(vm.FormerSmoker);
            vm.NeverSmokedButExposedToSmoke = NormalizeBool(vm.NeverSmokedButExposedToSmoke);
            vm.Smoked100Sticks = NormalizeBool(vm.Smoked100Sticks);

            // Urine and blood sugar flags
            vm.HasPolyuria = NormalizeBool(vm.HasPolyuria);
            vm.HasPolydipsia = NormalizeBool(vm.HasPolydipsia);
            vm.HasPolyphagia = NormalizeBool(vm.HasPolyphagia);
            vm.HasWeightLoss = NormalizeBool(vm.HasWeightLoss);
            vm.HasUrineProtein = NormalizeBool(vm.HasUrineProtein);
            vm.HasUrineKetones = NormalizeBool(vm.HasUrineKetones);

            // Cancer screening
            vm.BreastCancerScreened = NormalizeBool(vm.BreastCancerScreened);
            vm.CervicalCancerScreened = NormalizeBool(vm.CervicalCancerScreened);

            // Stress - Keep Filipino values for radio buttons
            vm.HasStress = NormalizeRadioButton(vm.HasStress);
            
            // MISSING NORMALIZATION FIELDS - Add these critical missing fields
            
            // Pananakit (Chest Pain) Questions 2.1-2.8 - Keep Filipino values for radio buttons
            vm.Pananakit21 = NormalizeRadioButton(vm.Pananakit21);
            vm.Pananakit22 = NormalizeRadioButton(vm.Pananakit22);
            vm.Pananakit23 = NormalizeRadioButton(vm.Pananakit23);
            vm.Pananakit24 = NormalizeRadioButton(vm.Pananakit24);
            vm.Pananakit25 = NormalizeRadioButton(vm.Pananakit25);
            vm.Pananakit26 = NormalizeRadioButton(vm.Pananakit26);
            vm.Pananakit27 = NormalizeRadioButton(vm.Pananakit27);
            vm.Pananakit28 = NormalizeRadioButton(vm.Pananakit28);
            
            // Nutrition - Missing detailed mappings
            vm.NutrisyonMadalasGulay = NormalizeBool(vm.NutrisyonMadalasGulay);
            vm.NutrisyonMadalasPratas = NormalizeBool(vm.NutrisyonMadalasPratas);
            vm.NutrisyonMadalasIsda = NormalizeBool(vm.NutrisyonMadalasIsda);
            vm.NutrisyonMadalasKarne = NormalizeBool(vm.NutrisyonMadalasKarne);
            vm.NutrisyonKumakainMatatamis = NormalizeBool(vm.NutrisyonKumakainMatatamis);
            vm.NutrisyonKumakainMamantika = NormalizeBool(vm.NutrisyonKumakainMamantika);
            
            // Alcohol - Missing detailed mappings - Keep Filipino values for radio buttons
            vm.AlcoholInom = NormalizeRadioButton(vm.AlcoholInom);
            vm.AlchoholTypeBeer = NormalizeBool(vm.AlchoholTypeBeer);
            vm.AlchoholTypeWine = NormalizeBool(vm.AlchoholTypeWine);
            vm.AlchoholTypeWhisky = NormalizeBool(vm.AlchoholTypeWhisky);
            vm.BeerConsumption1 = NormalizeBool(vm.BeerConsumption1);
            vm.BeerConsumption2 = NormalizeBool(vm.BeerConsumption2);
            vm.WineConsumption1 = NormalizeBool(vm.WineConsumption1);
            vm.WhiskyConsumption1 = NormalizeBool(vm.WhiskyConsumption1);
            vm.WhiskyConsumption2 = NormalizeBool(vm.WhiskyConsumption2);
            vm.AlcoholOkasyon = NormalizeRadioButton(vm.AlcoholOkasyon);
            
            // Exercise - Missing detailed mappings
            vm.EhersisyoRegular = NormalizeBool(vm.EhersisyoRegular);
            vm.EhersisyoDuration = NormalizeBool(vm.EhersisyoDuration);
            vm.EhersisyoType = NormalizeBool(vm.EhersisyoType);
            
            // Smoking - Missing detailed mappings - Keep Filipino values for radio buttons
            vm.SigarilyoKadami = NormalizeRadioButton(vm.SigarilyoKadami);
            vm.SigarilyoTumigil = NormalizeRadioButton(vm.SigarilyoTumigil);
            vm.SigarilyoUsok = NormalizeBool(vm.SigarilyoUsok);
            vm.SigarilyoSticks = NormalizeBool(vm.SigarilyoSticks);
            
            // Stress - Missing detailed mappings - Keep Filipino values for radio buttons
            vm.StressMadalas = NormalizeRadioButton(vm.StressMadalas);
            vm.StressSino = NormalizeBool(vm.StressSino); // This is a text field, not radio button
            vm.StressEpekto = NormalizeRadioButton(vm.StressEpekto);
            
            // Additional missing fields for complete form mapping
            vm.HealthFacilityName = NormalizeBool(vm.HealthFacilityName); // This is a text field
            vm.DateAssessment = NormalizeBool(vm.DateAssessment); // This is a text field
            
            // Lung Disease - Missing proper mapping
            vm.HasLungDiseaseNonInfectious = NormalizeBool(vm.HasLungDiseaseNonInfectious);
            
            // Eye Disease - Missing proper mapping  
            vm.HasEyeDiseaseCondition = NormalizeBool(vm.HasEyeDiseaseCondition);
        }

        private static string NormalizeBool(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "false";
            var v = value.Trim().ToLowerInvariant();
            
            // DEBUGGING: Log normalization process
            Console.WriteLine($"NormalizeBool: '{value}' -> '{v}'");
            
            switch (v)
            {
                case "true":
                case "1":
                case "oo":
                case "yes":
                case "mayroon":
                    Console.WriteLine($"NormalizeBool: '{value}' -> 'true'");
                    return "true";
                case "false":
                case "0":
                case "hindi":
                case "no":
                case "wala":
                case "non-smoker":
                    Console.WriteLine($"NormalizeBool: '{value}' -> 'false'");
                    return "false";
                default:
                    Console.WriteLine($"NormalizeBool: '{value}' -> 'false' (default)");
                    return "false";
            }
        }

        private static string NormalizeRadioButton(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Hindi";
            
            // If already in correct format, don't normalize
            if (value == "Oo" || value == "Hindi")
            {
                Console.WriteLine($"NormalizeRadioButton: '{value}' -> '{value}' (already correct)");
                return value;
            }
            
            var v = value.Trim().ToLowerInvariant();
            
            // DEBUGGING: Log normalization process
            Console.WriteLine($"NormalizeRadioButton: '{value}' -> '{v}'");
            
            switch (v)
            {
                case "true":
                case "1":
                case "oo":
                case "yes":
                case "mayroon":
                    Console.WriteLine($"NormalizeRadioButton: '{value}' -> 'Oo'");
                    return "Oo";
                case "false":
                case "0":
                case "hindi":
                case "no":
                case "wala":
                case "non-smoker":
                    Console.WriteLine($"NormalizeRadioButton: '{value}' -> 'Hindi'");
                    return "Hindi";
                default:
                    Console.WriteLine($"NormalizeRadioButton: '{value}' -> 'Hindi' (default)");
                    return "Hindi";
            }
        }
    }
}