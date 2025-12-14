using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Barangay.Data;
using Barangay.Models;
using Barangay.Services;
using System.Text.Json;
using System;
using System.Globalization;

namespace Barangay.Pages.Admin
{
    public class FamilyData
    {
        public string FamilyNumber { get; set; } = "";
        public List<object> ImmunizationRecords { get; set; } = new();
        public List<object> HEEADSSSRecords { get; set; } = new();
        public List<object> NCDRecords { get; set; } = new();
        public List<object> VitalSignsRecords { get; set; } = new();
        public string LastUpdated { get; set; } = "";
        public string MotherName { get; set; } = "";
        public string FatherName { get; set; } = "";
        public string Address { get; set; } = "";
        public string ContactNumber { get; set; } = "";
    }

    public class ArchiveModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IDataEncryptionService _encryptionService;

        public ArchiveModel(ApplicationDbContext context, IDataEncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        public List<ImmunizationRecord> ImmunizationRecords { get; set; } = new();
        public List<HEEADSSSAssessment> HEEADSSSAssessments { get; set; } = new();
        public List<NCDRiskAssessment> NCDRiskAssessments { get; set; } = new();
        public List<VitalSign> VitalSigns { get; set; } = new();
        public List<string> FamilyIdentifiers { get; set; } = new();

        
        private string NormalizeFamilyNumber(string familyNumber)
        {
            if (string.IsNullOrEmpty(familyNumber))
                return familyNumber;

            // If it's not encrypted (like a raw GUID), try to convert it to a readable format
            if (!_encryptionService.IsEncrypted(familyNumber) && familyNumber.Length > 20)
            {
                // Check if it looks like a GUID and convert it to a readable format
                if (Guid.TryParse(familyNumber, out Guid guid))
                {
                    // Convert GUID to a more readable format like "GUID-ABCD"
                    return $"GUID-{guid.ToString().Substring(0, 4).ToUpper()}";
                }
            }

            // For encrypted values, decrypt them
            string decrypted = _encryptionService.Decrypt(familyNumber);

            // If decryption returns the same value, it might already be decrypted or not encrypted
            if (decrypted == familyNumber)
            {
                return familyNumber;
            }

            return decrypted;
        }

        
        private string CreateFamilyKey(string familyNumber)
        {
            if (string.IsNullOrEmpty(familyNumber))
                return "";

            string normalized = NormalizeFamilyNumber(familyNumber);

            // Create a consistent key for matching similar family numbers
            // Remove common prefixes/suffixes and normalize case
            string key = normalized.ToUpper().Trim();

            // Remove common prefixes
            if (key.StartsWith("FAM")) key = key.Substring(3);
            if (key.StartsWith("FAMILY")) key = key.Substring(6);
            if (key.StartsWith("GUID-")) key = key.Substring(5);

            // Remove common suffixes
            if (key.EndsWith("-FAMILY")) key = key.Substring(0, key.Length - 7);
            if (key.EndsWith("-GUID")) key = key.Substring(0, key.Length - 5);

            // Extract just the numeric or alphanumeric part
            var match = System.Text.RegularExpressions.Regex.Match(key, @"([A-Z0-9]+)");
            if (match.Success)
            {
                key = match.Groups[1].Value;
            }

            return key;
        }

        public async Task OnGetAsync()
        {
            try
            {
                ImmunizationRecords = await _context.ImmunizationRecords
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception)
            {
                // ImmunizationRecords table doesn't exist yet, continue with empty list
                ImmunizationRecords = new List<ImmunizationRecord>();
            }

            try
            {
                HEEADSSSAssessments = await _context.HEEADSSSAssessments
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception)
            {
                // HEEADSSSAssessments table doesn't exist yet, continue with empty list
                HEEADSSSAssessments = new List<HEEADSSSAssessment>();
            }

            try
            {
                NCDRiskAssessments = await _context.NCDRiskAssessments
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception)
            {
                // NCDRiskAssessments table doesn't exist yet, continue with empty list
                NCDRiskAssessments = new List<NCDRiskAssessment>();
            }

            try
            {
                VitalSigns = await _context.VitalSigns
                    .OrderByDescending(r => r.RecordedAt)
                    .ToListAsync();
            }
            catch (Exception)
            {
                // VitalSigns table doesn't exist yet, continue with empty list
                VitalSigns = new List<VitalSign>();
            }

            // Extract unique family identifiers from all record types and consolidate them
            var familyGroups = new Dictionary<string, List<string>>(); // Key: normalized family key, Value: list of original values
            var familyIdSources = new Dictionary<string, string>(); // Track original encrypted value for each normalized family number

            // Helper function to add family number to groups
            void AddFamilyNumber(string source, string familyNumber)
            {
                if (string.IsNullOrEmpty(familyNumber)) return;

                string normalizedFamilyNumber = NormalizeFamilyNumber(familyNumber);
                string familyKey = CreateFamilyKey(familyNumber);

                if (!familyGroups.ContainsKey(familyKey))
                {
                    familyGroups[familyKey] = new List<string>();
                }
                familyGroups[familyKey].Add(normalizedFamilyNumber);

                // Track the original encrypted source for each normalized family number
                if (!familyIdSources.ContainsKey(normalizedFamilyNumber))
                {
                    familyIdSources[normalizedFamilyNumber] = source;
                }
            }

            // From ImmunizationRecords
            foreach (var record in ImmunizationRecords)
            {
                AddFamilyNumber($"Immunization:{record.Id}", record.FamilyNumber);
            }

            // From HEEADSSSAssessments
            foreach (var record in HEEADSSSAssessments)
            {
                AddFamilyNumber($"HEEADSSS:{record.Id}", record.FamilyNo);
            }

            // From NCDRiskAssessments
            foreach (var record in NCDRiskAssessments)
            {
                AddFamilyNumber($"NCD:{record.Id}", record.FamilyNo);
            }

            // From VitalSigns (assuming PatientId might be family number)
            foreach (var record in VitalSigns)
            {
                AddFamilyNumber($"VitalSigns:{record.Id}", record.PatientId);
            }

            // Consolidate family groups - for each group, pick the most representative family number
            var consolidatedFamilyIds = new List<string>();
            foreach (var group in familyGroups)
            {
                if (group.Value.Any())
                {
                    // Pick the first non-empty, non-GUID family number as the primary identifier
                    string primaryFamilyId = group.Value.FirstOrDefault(id => !string.IsNullOrEmpty(id) && !id.StartsWith("GUID-"));
                    if (string.IsNullOrEmpty(primaryFamilyId))
                    {
                        primaryFamilyId = group.Value.FirstOrDefault();
                    }
                    if (!string.IsNullOrEmpty(primaryFamilyId))
                    {
                        consolidatedFamilyIds.Add(primaryFamilyId);
                    }
                }
            }

            // Store both the consolidated family numbers and their original encrypted sources
            FamilyIdentifiers = consolidatedFamilyIds.Distinct().ToList();
            ViewData["FamilyIdSources"] = familyIdSources;
            ViewData["FamilyGroups"] = familyGroups;
        }


        public async Task<IActionResult> OnGetFamilyDetailsAsync(string familyId)
        {
            try
            {
                // Normalize the family ID (handle both encrypted and unencrypted cases)
                string normalizedFamilyId = NormalizeFamilyNumber(familyId);
                string familyKey = CreateFamilyKey(familyId);

                // Fetch all records for this family using direct property access to avoid LINQ translation issues
                // First try exact match with normalized family ID
                var immunizationRecords = await _context.ImmunizationRecords
                    .Where(r => r.FamilyNumber == normalizedFamilyId)
                    .ToListAsync();

                var heeadsssRecords = await _context.HEEADSSSAssessments
                    .Where(r => r.FamilyNo == normalizedFamilyId)
                    .ToListAsync();

                var ncdRecords = await _context.NCDRiskAssessments
                    .Where(r => r.FamilyNo == normalizedFamilyId)
                    .ToListAsync();

                var vitalSignsRecords = await _context.VitalSigns
                    .Where(r => r.PatientId == normalizedFamilyId)
                    .ToListAsync();

                // If no exact matches, try to find by family key pattern
                if (!immunizationRecords.Any() && !heeadsssRecords.Any() && !ncdRecords.Any() && !vitalSignsRecords.Any())
                {
                    // Load all records and filter in memory using family key pattern
                    var allImmunizationRecords = await _context.ImmunizationRecords.ToListAsync();
                    var allHeeadsssRecords = await _context.HEEADSSSAssessments.ToListAsync();
                    var allNcdRecords = await _context.NCDRiskAssessments.ToListAsync();
                    var allVitalSignsRecords = await _context.VitalSigns.ToListAsync();

                    immunizationRecords = allImmunizationRecords
                        .Where(r => CreateFamilyKey(r.FamilyNumber) == familyKey)
                        .ToList();

                    heeadsssRecords = allHeeadsssRecords
                        .Where(r => CreateFamilyKey(r.FamilyNo) == familyKey)
                        .ToList();

                    ncdRecords = allNcdRecords
                        .Where(r => CreateFamilyKey(r.FamilyNo) == familyKey)
                        .ToList();

                    vitalSignsRecords = allVitalSignsRecords
                        .Where(r => CreateFamilyKey(r.PatientId) == familyKey)
                        .ToList();
                }

                // Process records to return readable data
                var processedImmunizationRecords = ProcessImmunizationRecords(immunizationRecords);
                var processedHeeadsssRecords = ProcessHeeadsssRecords(heeadsssRecords);
                var processedNcdRecords = ProcessNcdRecords(ncdRecords);
                var processedVitalSignsRecords = ProcessVitalSignsRecords(vitalSignsRecords);

                // Get family info
                var familyInfo = GetFamilyInfo(immunizationRecords, heeadsssRecords, ncdRecords, vitalSignsRecords, normalizedFamilyId);

                // Calculate statistics
                var totalRecords = processedImmunizationRecords.Count + processedHeeadsssRecords.Count + 
                                 processedNcdRecords.Count + processedVitalSignsRecords.Count;

                var stats = new
                {
                    total = totalRecords,
                    immunization = processedImmunizationRecords.Count,
                    heeadsss = processedHeeadsssRecords.Count,
                    ncd = processedNcdRecords.Count,
                    vitalSigns = processedVitalSignsRecords.Count,
                    familyInfo = familyInfo
                };

                return new JsonResult(new
                {
                    success = true,
                    stats = stats,
                    data = new
                    {
                        immunization = processedImmunizationRecords,
                        heeadsss = processedHeeadsssRecords,
                        ncd = processedNcdRecords,
                        vitalSigns = processedVitalSignsRecords
                    },
                    familyId = normalizedFamilyId,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    error = ex.Message,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
        }

        private List<object> ProcessImmunizationRecords(List<ImmunizationRecord> records)
        {
            var processedRecords = new List<object>();

            foreach (var record in records)
            {
                var processedRecord = new
                {
                    Id = record.Id,
                    ChildName = record.ChildName ?? "N/A",
                    DateOfBirth = record.DateOfBirth ?? "N/A",
                    PlaceOfBirth = record.PlaceOfBirth ?? "N/A",
                    Address = record.Address ?? "N/A",
                    MotherName = record.MotherName ?? "N/A",
                    FatherName = record.FatherName ?? "N/A",
                    Sex = record.Sex ?? "N/A",
                    BirthHeight = record.BirthHeight ?? "N/A",
                    BirthWeight = record.BirthWeight ?? "N/A",
                    HealthCenter = record.HealthCenter ?? "N/A",
                    Barangay = record.Barangay ?? "N/A",
                    FamilyNumber = record.FamilyNumber ?? "N/A",
                    Email = record.Email ?? "N/A",
                    ContactNumber = record.ContactNumber ?? "N/A",
                    BCGVaccineDate = record.BCGVaccineDate ?? "N/A",
                    BCGVaccineRemarks = record.BCGVaccineRemarks ?? "N/A",
                    HepatitisBVaccineDate = record.HepatitisBVaccineDate ?? "N/A",
                    HepatitisBVaccineRemarks = record.HepatitisBVaccineRemarks ?? "N/A",
                    Pentavalent1Date = record.Pentavalent1Date ?? "N/A",
                    Pentavalent1Remarks = record.Pentavalent1Remarks ?? "N/A",
                    Pentavalent2Date = record.Pentavalent2Date ?? "N/A",
                    Pentavalent2Remarks = record.Pentavalent2Remarks ?? "N/A",
                    Pentavalent3Date = record.Pentavalent3Date ?? "N/A",
                    Pentavalent3Remarks = record.Pentavalent3Remarks ?? "N/A",
                    OPV1Date = record.OPV1Date ?? "N/A",
                    OPV1Remarks = record.OPV1Remarks ?? "N/A",
                    OPV2Date = record.OPV2Date ?? "N/A",
                    OPV2Remarks = record.OPV2Remarks ?? "N/A",
                    OPV3Date = record.OPV3Date ?? "N/A",
                    OPV3Remarks = record.OPV3Remarks ?? "N/A",
                    IPV1Date = record.IPV1Date ?? "N/A",
                    IPV1Remarks = record.IPV1Remarks ?? "N/A",
                    IPV2Date = record.IPV2Date ?? "N/A",
                    IPV2Remarks = record.IPV2Remarks ?? "N/A",
                    PCV1Date = record.PCV1Date ?? "N/A",
                    PCV1Remarks = record.PCV1Remarks ?? "N/A",
                    PCV2Date = record.PCV2Date ?? "N/A",
                    PCV2Remarks = record.PCV2Remarks ?? "N/A",
                    PCV3Date = record.PCV3Date ?? "N/A",
                    PCV3Remarks = record.PCV3Remarks ?? "N/A",
                    MMR1Date = record.MMR1Date ?? "N/A",
                    MMR1Remarks = record.MMR1Remarks ?? "N/A",
                    MMR2Date = record.MMR2Date ?? "N/A",
                    MMR2Remarks = record.MMR2Remarks ?? "N/A",
                    CreatedAt = record.CreatedAt ?? "N/A",
                    UpdatedAt = record.UpdatedAt ?? "N/A",
                    CreatedBy = record.CreatedBy ?? "N/A",
                    UpdatedBy = record.UpdatedBy ?? "N/A",
                    Status = record.Status ?? "N/A"
                };

                processedRecords.Add(processedRecord);
            }

            return processedRecords;
        }

        private List<object> ProcessHeeadsssRecords(List<HEEADSSSAssessment> records)
        {
            var processedRecords = new List<object>();

            foreach (var record in records)
            {
                var processedRecord = new
                {
                    Id = record.Id,
                    UserId = record.UserId ?? "N/A",
                    AppointmentId = record.AppointmentId ?? "N/A",
                    HealthFacility = record.HealthFacility ?? "N/A",
                    FamilyNo = record.FamilyNo ?? "N/A",
                    FullName = record.FullName ?? "N/A",
                    Age = record.Age ?? "N/A",
                    Gender = record.Gender ?? "N/A",
                    Address = record.Address ?? "N/A",
                    ContactNumber = record.ContactNumber ?? "N/A",
                    HomeEnvironment = record.HomeEnvironment ?? "N/A",
                    FamilyRelationship = record.FamilyRelationship ?? "N/A",
                    HomeFamilyProblems = record.HomeFamilyProblems ?? "N/A",
                    HomeParentalListening = record.HomeParentalListening ?? "N/A",
                    HomeParentalBlame = record.HomeParentalBlame ?? "N/A",
                    HomeFamilyChanges = record.HomeFamilyChanges ?? "N/A",
                    SchoolPerformance = record.SchoolPerformance ?? "N/A",
                    AttendanceIssues = record.AttendanceIssues?.ToString() ?? "N/A",
                    CareerPlans = record.CareerPlans ?? "N/A",
                    EducationCurrentlyStudying = record.EducationCurrentlyStudying ?? "N/A",
                    EducationWorking = record.EducationWorking ?? "N/A",
                    EducationSchoolWorkProblems = record.EducationSchoolWorkProblems ?? "N/A",
                    EducationBullying = record.EducationBullying ?? "N/A",
                    EducationEmployment = record.EducationEmployment ?? "N/A",
                    DietDescription = record.DietDescription ?? "N/A",
                    WeightConcerns = record.WeightConcerns?.ToString() ?? "N/A",
                    EatingDisorderSymptoms = record.EatingDisorderSymptoms?.ToString() ?? "N/A",
                    EatingBodyImageSatisfaction = record.EatingBodyImageSatisfaction ?? "N/A",
                    EatingDisorderedEatingBehaviors = record.EatingDisorderedEatingBehaviors ?? "N/A",
                    EatingWeightComments = record.EatingWeightComments ?? "N/A",
                    Hobbies = record.Hobbies ?? "N/A",
                    PhysicalActivity = record.PhysicalActivity ?? "N/A",
                    ScreenTime = record.ScreenTime ?? "N/A",
                    ActivitiesParticipation = record.ActivitiesParticipation ?? "N/A",
                    ActivitiesRegularExercise = record.ActivitiesRegularExercise ?? "N/A",
                    ActivitiesScreenTime = record.ActivitiesScreenTime ?? "N/A",
                    SubstanceUse = record.SubstanceUse?.ToString() ?? "N/A",
                    SubstanceType = record.SubstanceType ?? "N/A",
                    DrugsTobaccoUse = record.DrugsTobaccoUse ?? "N/A",
                    DrugsAlcoholUse = record.DrugsAlcoholUse ?? "N/A",
                    DrugsIllicitDrugUse = record.DrugsIllicitDrugUse ?? "N/A",
                    DatingRelationships = record.DatingRelationships ?? "N/A",
                    SexualActivity = record.SexualActivity?.ToString() ?? "N/A",
                    SexualOrientation = record.SexualOrientation ?? "N/A",
                    SexualityBodyConcerns = record.SexualityBodyConcerns ?? "N/A",
                    SexualityIntimateRelationships = record.SexualityIntimateRelationships ?? "N/A",
                    SexualityPartners = record.SexualityPartners ?? "N/A",
                    SexualitySexualOrientation = record.SexualitySexualOrientation ?? "N/A",
                    SexualityPregnancy = record.SexualityPregnancy ?? "N/A",
                    SexualitySTI = record.SexualitySTI ?? "N/A",
                    SexualityProtection = record.SexualityProtection ?? "N/A",
                    MoodChanges = record.MoodChanges?.ToString() ?? "N/A",
                    SuicidalThoughts = record.SuicidalThoughts?.ToString() ?? "N/A",
                    SelfHarmBehavior = record.SelfHarmBehavior?.ToString() ?? "N/A",
                    FeelsSafeAtHome = record.FeelsSafeAtHome?.ToString() ?? "N/A",
                    FeelsSafeAtSchool = record.FeelsSafeAtSchool?.ToString() ?? "N/A",
                    ExperiencedBullying = record.ExperiencedBullying?.ToString() ?? "N/A",
                    PersonalStrengths = record.PersonalStrengths ?? "N/A",
                    SupportSystems = record.SupportSystems ?? "N/A",
                    CopingMechanisms = record.CopingMechanisms ?? "N/A",
                    SafetyPhysicalAbuse = record.SafetyPhysicalAbuse ?? "N/A",
                    SafetyRelationshipViolence = record.SafetyRelationshipViolence ?? "N/A",
                    SafetyProtectiveGear = record.SafetyProtectiveGear ?? "N/A",
                    SafetyGunsAtHome = record.SafetyGunsAtHome ?? "N/A",
                    SuicideDepressionFeelings = record.SuicideDepressionFeelings ?? "N/A",
                    SuicideSelfHarmThoughts = record.SuicideSelfHarmThoughts ?? "N/A",
                    SuicideFamilyHistory = record.SuicideFamilyHistory ?? "N/A",
                    AssessmentNotes = record.AssessmentNotes ?? "N/A",
                    RecommendedActions = record.RecommendedActions ?? "N/A",
                    FollowUpPlan = record.FollowUpPlan ?? "N/A",
                    Notes = record.Notes ?? "N/A",
                    AssessedBy = record.AssessedBy ?? "N/A",
                    CreatedAt = record.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    UpdatedAt = record.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"
                };

                processedRecords.Add(processedRecord);
            }

            return processedRecords;
        }

        private List<object> ProcessNcdRecords(List<NCDRiskAssessment> records)
        {
            var processedRecords = new List<object>();

            foreach (var record in records)
            {
                var processedRecord = new
                {
                    // Basic Information (Part I - Profile)
                    Id = record.Id,
                    UserId = record.UserId ?? "N/A",
                    AppointmentId = record.AppointmentId?.ToString() ?? "N/A",
                    HealthFacility = record.HealthFacility ?? "N/A",
                    FamilyNo = record.FamilyNo ?? "N/A",
                    FirstName = record.FirstName ?? "N/A",
                    MiddleName = record.MiddleName ?? "N/A",
                    LastName = record.LastName ?? "N/A",
                    Address = record.Address ?? "N/A",
                    Barangay = record.Barangay ?? "N/A",
                    Birthday = record.Birthday ?? "N/A",
                    Telepono = record.Telepono ?? "N/A",
                    Edad = record.Edad?.ToString() ?? "N/A",
                    Kasarian = record.Kasarian ?? "N/A",
                    Relihiyon = record.Relihiyon ?? "N/A",
                    CivilStatus = record.CivilStatus ?? "N/A",
                    Occupation = record.Occupation ?? "N/A",
                    IDNumber = record.IDNumber ?? "N/A",

                    // Part II - Past Medical History
                    HasDiabetes = record.HasDiabetes,
                    DiabetesYear = record.DiabetesYear?.ToString() ?? "N/A",
                    DiabetesMedication = record.DiabetesMedication ?? "N/A",
                    HasHypertension = record.HasHypertension,
                    HypertensionYear = record.HypertensionYear?.ToString() ?? "N/A",
                    HypertensionMedication = record.HypertensionMedication ?? "N/A",
                    HasCancer = record.HasCancer,
                    CancerType = record.CancerType ?? "N/A",
                    CancerSite = record.CancerSite ?? "N/A",
                    CancerYear = record.CancerYear?.ToString() ?? "N/A",
                    CancerMedication = record.CancerMedication ?? "N/A",
                    HasCOPD = record.HasCOPD,
                    COPDYear = record.COPDYear?.ToString() ?? "N/A",
                    COPDMedication = record.COPDMedication ?? "N/A",
                    HasLungDisease = record.HasLungDisease,
                    LungDiseaseYear = record.LungDiseaseYear?.ToString() ?? "N/A",
                    LungDiseaseMedication = record.LungDiseaseMedication ?? "N/A",
                    HasEyeDisease = record.HasEyeDisease,
                    EyeDiseaseYear = record.EyeDiseaseYear?.ToString() ?? "N/A",
                    EyeDiseaseMedication = record.EyeDiseaseMedication ?? "N/A",
                    HasStrokeSymptoms = record.HasStrokeSymptoms,

                    // Part III - Risk Factors (Nutrition, Alcohol, Exercise, Smoking, Stress)
                    // Nutrition
                    EatsMeatDaily = record.EatsMeatDaily,
                    EatsFishDaily = record.EatsFishDaily,
                    EatsVegetablesDaily = record.EatsVegetablesDaily,
                    EatsFruitsDaily = record.EatsFruitsDaily,
                    EatsFattyFoodMoreThan2TimesPerWeek = record.EatsFattyFoodMoreThan2TimesPerWeek,
                    EatsOilyFoodMoreThan2TimesPerWeek = record.EatsOilyFoodMoreThan2TimesPerWeek,
                    EatsSweetFoodMoreThan2TimesPerWeek = record.EatsSweetFoodMoreThan2TimesPerWeek,
                    HasUnhealthyDiet = record.HasUnhealthyDiet,

                    // Alcohol
                    DrinksAlcohol = record.DrinksAlcohol,
                    AlcoholFrequency = record.AlcoholFrequency ?? "N/A",
                    IsBingeDrinker = record.IsBingeDrinker,
                    DrinksBeer = record.DrinksBeer,
                    BeerConsumption1 = record.BeerConsumption1 ?? "N/A",
                    DrinksWine = record.DrinksWine,
                    WineConsumption1 = record.WineConsumption1 ?? "N/A",
                    DrinksWhiskyGinBrandy = record.DrinksWhiskyGinBrandy,
                    WhiskyConsumption1 = record.WhiskyConsumption1 ?? "N/A",
                    AlcoholAmount1Bottle320ml = record.AlcoholAmount1Bottle320ml,
                    AlcoholAmount2Bottle640ml = record.AlcoholAmount2Bottle640ml,

                    // Exercise
                    HasNoRegularExercise = record.HasNoRegularExercise,
                    HasEnoughExercise = record.HasEnoughExercise,
                    InsufficientPhysicalActivity = record.InsufficientPhysicalActivity,
                    CombinationExercise = record.CombinationExercise,
                    ModerateIntensityExercise = record.ModerateIntensityExercise,
                    VigorousIntensityExercise = record.VigorousIntensityExercise,
                    ExerciseDuration = record.ExerciseDuration ?? "N/A",

                    // Smoking
                    SmokingStatus = record.SmokingStatus ?? "N/A",
                    HasHistoryOfSmoking = record.HasHistoryOfSmoking,
                    Smoked100Sticks = record.Smoked100Sticks,
                    SmokingQuitDuration = record.SmokingQuitDuration ?? "N/A",
                    FormerSmoker = record.FormerSmoker,
                    NeverSmokedButExposedToSmoke = record.NeverSmokedButExposedToSmoke,

                    // Stress
                    HasStress = record.HasStress,
                    StressMadalas = record.StressMadalas ?? "N/A",
                    StressSino = record.StressSino ?? "N/A",
                    StressEpekto = record.StressEpekto ?? "N/A",

                    // Part IV - Anthropometric Measurements
                    Height = record.Height ?? "N/A",
                    Weight = record.Weight ?? "N/A",
                    Waist = record.Waist ?? "N/A",
                    Hip = record.Hip ?? "N/A",
                    BMI = record.BMI ?? "N/A",
                    BMIStatus = record.BMIStatus ?? "N/A",
                    WHRatio = record.WHRatio ?? "N/A",
                    WHStatus = record.WHStatus ?? "N/A",

                    // Blood Pressure
                    BaselineBP = record.BaselineBP ?? "N/A",
                    BPStatus = record.BPStatus ?? "N/A",
                    LeftArmMeanBP = record.LeftArmMeanBP ?? "N/A",
                    RightArmMeanBP = record.RightArmMeanBP ?? "N/A",

                    // Blood Sugar
                    FastingBloodSugar = record.FastingBloodSugar ?? "N/A",
                    RandomBloodSugar = record.RandomBloodSugar ?? "N/A",
                    BloodSugarStatus = record.BloodSugarStatus ?? "N/A",

                    // Cholesterol
                    CholesterolResult = record.CholesterolResult ?? "N/A",
                    CholesterolStatus = record.CholesterolStatus ?? "N/A",

                    // Urine Tests
                    HasUrineProtein = record.HasUrineProtein,
                    HasUrineKetones = record.HasUrineKetones,

                    // Symptoms
                    HasChestPain = record.HasChestPain,
                    ChestPain = record.ChestPain ?? "N/A",
                    ChestPainLocation = record.ChestPainLocation ?? "N/A",
                    ChestPainValue = record.ChestPainValue ?? "N/A",
                    ChestPainSpreadsToArm = record.ChestPainSpreadsToArm,
                    PainLastsMoreThan30Min = record.PainLastsMoreThan30Min,
                    PainRelievedWithRest = record.PainRelievedWithRest,

                    HasDifficultyBreathing = record.HasDifficultyBreathing,
                    HasAsthma = record.HasAsthma,
                    NumbnessWhenWalkingFast = record.NumbnessWhenWalkingFast,
                    LossOfConsciousnessLessThan10Min = record.LossOfConsciousnessLessThan10Min,

                    HasPolydipsia = record.HasPolydipsia,
                    HasPolyphagia = record.HasPolyphagia,
                    HasPolyuria = record.HasPolyuria,
                    HasWeightLoss = record.HasWeightLoss,

                    // Part V - Assessment
                    RiskStatus = record.RiskStatus ?? "N/A",
                    RiskPercentage = record.RiskPercentage ?? "N/A",
                    SeeDoctorIfYes = record.SeeDoctorIfYes,

                    // Family History
                    FamilyHistoryDiabetesFather = record.FamilyHistoryDiabetesFather,
                    FamilyHistoryDiabetesMother = record.FamilyHistoryDiabetesMother,
                    FamilyHistoryDiabetesSibling = record.FamilyHistoryDiabetesSibling,
                    FamilyHistoryCancerFather = record.FamilyHistoryCancerFather,
                    FamilyHistoryCancerMother = record.FamilyHistoryCancerMother,
                    FamilyHistoryCancerSibling = record.FamilyHistoryCancerSibling,
                    FamilyHistoryHeartDiseaseFather = record.FamilyHistoryHeartDiseaseFather,
                    FamilyHistoryHeartDiseaseMother = record.FamilyHistoryHeartDiseaseMother,
                    FamilyHistoryHeartDiseaseSibling = record.FamilyHistoryHeartDiseaseSibling,
                    FamilyHistoryStrokeFather = record.FamilyHistoryStrokeFather,
                    FamilyHistoryStrokeMother = record.FamilyHistoryStrokeMother,
                    FamilyHistoryStrokeSibling = record.FamilyHistoryStrokeSibling,
                    FamilyHistoryLungDiseaseFather = record.FamilyHistoryLungDiseaseFather,
                    FamilyHistoryLungDiseaseMother = record.FamilyHistoryLungDiseaseMother,
                    FamilyHistoryLungDiseaseSibling = record.FamilyHistoryLungDiseaseSibling,
                    FamilyHistoryKidneyDiseaseFather = record.FamilyHistoryKidneyDiseaseFather,
                    FamilyHistoryKidneyDiseaseMother = record.FamilyHistoryKidneyDiseaseMother,
                    FamilyHistoryKidneyDiseaseSibling = record.FamilyHistoryKidneyDiseaseSibling,
                    FamilyHistoryEyeDiseaseFather = record.FamilyHistoryEyeDiseaseFather,
                    FamilyHistoryEyeDiseaseMother = record.FamilyHistoryEyeDiseaseMother,
                    FamilyHistoryEyeDiseaseSibling = record.FamilyHistoryEyeDiseaseSibling,
                    FamilyHistoryOtherFather = record.FamilyHistoryOtherFather,
                    FamilyHistoryOtherMother = record.FamilyHistoryOtherMother,
                    FamilyHistoryOtherSibling = record.FamilyHistoryOtherSibling,

                    FamilyHasHypertension = record.FamilyHasHypertension,
                    FamilyHasHeartDisease = record.FamilyHasHeartDisease,
                    FamilyHasStroke = record.FamilyHasStroke,
                    FamilyHasDiabetes = record.FamilyHasDiabetes,
                    FamilyHasCancer = record.FamilyHasCancer,
                    FamilyHasKidneyDisease = record.FamilyHasKidneyDisease,
                    FamilyHasOtherDisease = record.FamilyHasOtherDisease,
                    FamilyOtherDiseaseDetails = record.FamilyOtherDiseaseDetails ?? "N/A",

                    // Cancer Screening
                    BreastCancerScreened = record.BreastCancerScreened,
                    CervicalCancerScreened = record.CervicalCancerScreened,
                    CancerScreeningStatus = record.CancerScreeningStatus ?? "N/A",

                    // Assessment Details
                    AssessmentDate = record.AssessmentDate ?? "N/A",
                    InterviewedBy = record.InterviewedBy ?? "N/A",
                    DoctorName = record.DoctorName ?? "N/A",
                    Designation = record.Designation ?? "N/A",
                    PatientSignature = record.PatientSignature ?? "N/A",
                    CreatedAt = record.CreatedAt ?? "N/A",
                    UpdatedAt = record.UpdatedAt ?? "N/A",
                    AppointmentType = record.AppointmentType ?? "N/A"
                };

                processedRecords.Add(processedRecord);
            }

            return processedRecords;
        }

        private List<object> ProcessVitalSignsRecords(List<VitalSign> records)
        {
            var processedRecords = new List<object>();

            foreach (var record in records)
            {
                var processedRecord = new
                {
                    Id = record.Id,
                    PatientId = record.PatientId ?? "N/A",
                    BloodPressure = record.BloodPressure ?? "N/A",
                    HeartRate = record.HeartRate ?? "N/A",
                    Temperature = record.Temperature ?? "N/A",
                    RespiratoryRate = record.RespiratoryRate ?? "N/A",
                    SpO2 = record.SpO2 ?? "N/A",
                    Height = record.Height ?? "N/A",
                    Weight = record.Weight ?? "N/A",
                    RecordedAt = record.RecordedAt.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
                    Notes = record.Notes ?? "N/A"
                };

                processedRecords.Add(processedRecord);
            }

            return processedRecords;
        }

        private object GetFamilyInfo(
            List<ImmunizationRecord> immunizationRecords,
            List<HEEADSSSAssessment> heeadsssRecords,
            List<NCDRiskAssessment> ncdRecords,
            List<VitalSign> vitalSignsRecords,
            string decryptedFamilyId)
        {
            // Consolidate all available family information from all record types
            var familyInfo = new
            {
                familyNumber = decryptedFamilyId,
                motherName = "Unknown",
                fatherName = "Unknown",
                fullName = "Unknown",
                firstName = "Unknown",
                lastName = "Unknown",
                address = "Unknown",
                barangay = "Unknown",
                contactNumber = "Unknown",
                email = "Unknown",
                recordCount = immunizationRecords.Count + heeadsssRecords.Count + ncdRecords.Count + vitalSignsRecords.Count,
                lastUpdated = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // Get family info from ImmunizationRecords (usually most complete)
            if (immunizationRecords.Any())
            {
                var firstRecord = immunizationRecords.First();
                familyInfo = familyInfo with
                {
                    motherName = firstRecord.MotherName ?? "Unknown",
                    fatherName = firstRecord.FatherName ?? "Unknown",
                    address = firstRecord.Address ?? "Unknown",
                    barangay = firstRecord.Barangay ?? "Unknown",
                    contactNumber = firstRecord.ContactNumber ?? "Unknown",
                    email = firstRecord.Email ?? "Unknown",
                    lastUpdated = !string.IsNullOrEmpty(firstRecord.UpdatedAt) ? firstRecord.UpdatedAt : familyInfo.lastUpdated
                };
            }

            // Supplement with HEEADSSS information if available
            if (heeadsssRecords.Any())
            {
                var firstRecord = heeadsssRecords.First();
                familyInfo = familyInfo with
                {
                    fullName = firstRecord.FullName ?? familyInfo.fullName,
                    address = firstRecord.Address ?? familyInfo.address,
                    contactNumber = firstRecord.ContactNumber ?? familyInfo.contactNumber,
                    lastUpdated = firstRecord.UpdatedAt.HasValue ? firstRecord.UpdatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) : familyInfo.lastUpdated
                };
            }

            // Supplement with NCD information if available
            if (ncdRecords.Any())
            {
                var firstRecord = ncdRecords.First();
                familyInfo = familyInfo with
                {
                    firstName = firstRecord.FirstName ?? familyInfo.firstName,
                    lastName = firstRecord.LastName ?? familyInfo.lastName,
                    address = firstRecord.Address ?? familyInfo.address,
                    barangay = firstRecord.Barangay ?? familyInfo.barangay,
                    lastUpdated = !string.IsNullOrEmpty(firstRecord.UpdatedAt) ? firstRecord.UpdatedAt : familyInfo.lastUpdated
                };
            }

            // Add record metadata
            return new
            {
                familyNumber = decryptedFamilyId,
                familyInfo = familyInfo,
                recordSummary = new
                {
                    immunization = immunizationRecords.Count,
                    heeadsss = heeadsssRecords.Count,
                    ncd = ncdRecords.Count,
                    vitalSigns = vitalSignsRecords.Count,
                    total = familyInfo.recordCount
                },
                message = familyInfo.recordCount > 0 ? $"Found {familyInfo.recordCount} health records for this family" : "No records found for this family"
            };
        }
    }
}
