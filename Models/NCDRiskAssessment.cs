using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Barangay.Attributes;

namespace Barangay.Models
{
    public class NCDRiskAssessment
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public int? AppointmentId { get; set; }

        [ForeignKey("AppointmentId")]
        public virtual Appointment? Appointment { get; set; }

        // Health Facility Information
        [Display(Name = "Health Facility")]
        [StringLength(4000)]
        [Encrypted]
        public string? HealthFacility { get; set; }

        [Display(Name = "Family No")]
        [StringLength(4000)]
        // FamilyNo is a public identifier used for grouping - should NOT be encrypted
        public string? FamilyNo { get; set; }

        [Display(Name = "Address")]
        [StringLength(500)]
        [Encrypted]
        public string? Address { get; set; }

        [Display(Name = "Barangay")]
        [StringLength(4000)]
        [Encrypted]
        public string? Barangay { get; set; }

        [Display(Name = "Birthday")]
        [StringLength(4000)]
        [Encrypted]
        public string? Birthday { get; set; }

        [Display(Name = "Telepono")]
        [StringLength(4000)]
        [Encrypted]
        public string? Telepono { get; set; }

        [Display(Name = "Edad")]
        [StringLength(4000)]
        [Encrypted]
        public string? Edad { get; set; }

        [Display(Name = "Kasarian")]
        [StringLength(4000)]
        [Encrypted]
        public string? Kasarian { get; set; }

        [Display(Name = "Relihiyon")]
        [StringLength(4000)]
        [Encrypted]
        public string? Relihiyon { get; set; }

        // Medical History
        [Display(Name = "Has Diabetes")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasDiabetes { get; set; } = "false";

        [Display(Name = "Has Hypertension")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasHypertension { get; set; } = "false";

        [Display(Name = "Has Cancer")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasCancer { get; set; } = "false";

        [Display(Name = "Has COPD")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasCOPD { get; set; } = "false";

        [Display(Name = "COPD Year")]
        [StringLength(50)]
        [Encrypted]
        public string? COPDYear { get; set; }

        [Display(Name = "COPD Medication")]
        [StringLength(4000)]
        [Encrypted]
        public string? COPDMedication { get; set; }

        [Display(Name = "Has Lung Disease")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasLungDisease { get; set; } = "false";

        [Display(Name = "Has Eye Disease")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasEyeDisease { get; set; } = "false";

        [Display(Name = "Eye Disease Year")]
        [StringLength(50)]
        [Encrypted]
        public string? EyeDiseaseYear { get; set; }

        [Display(Name = "Eye Disease Medication")]
        [StringLength(200)]
        [Encrypted]
        public string? EyeDiseaseMedication { get; set; }

        [Display(Name = "Alcohol Stopped Duration")]
        [StringLength(4000)]
        [Encrypted]
        public string? AlcoholStoppedDuration { get; set; }

        [Display(Name = "Smoked 100 Sticks")]
        [StringLength(4000)]
        [Encrypted]
        public string? Smoked100Sticks { get; set; } = "false";

        [Display(Name = "Has Enough Exercise")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasEnoughExercise { get; set; } = "false";

        [Display(Name = "Cancer Type")]
        [StringLength(200)]
        [Encrypted]
        public string? CancerType { get; set; }

        [Display(Name = "Cancer Site")]
        [StringLength(200)]
        [Encrypted]
        public string? CancerSite { get; set; }

        // Family History
        [Display(Name = "Family Has Hypertension")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHasHypertension { get; set; } = "false";

        [Display(Name = "Family Has Heart Disease")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHasHeartDisease { get; set; } = "false";

        [Display(Name = "Family Has Stroke")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHasStroke { get; set; } = "false";

        [Display(Name = "Family Has Diabetes")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHasDiabetes { get; set; } = "false";

        [Display(Name = "Family Has Cancer")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHasCancer { get; set; } = "false";

        [Display(Name = "Family Has Kidney Disease")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHasKidneyDisease { get; set; } = "false";

        [Display(Name = "Family Has Other Disease")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHasOtherDisease { get; set; } = "false";

        [Display(Name = "Family Other Disease Details")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyOtherDiseaseDetails { get; set; }

        // Detailed Family History (by Family Member)
        [Display(Name = "Family History Kidney Disease Father")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryKidneyDiseaseFather { get; set; } = "false";

        [Display(Name = "Family History Kidney Disease Mother")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryKidneyDiseaseMother { get; set; } = "false";

        [Display(Name = "Family History Kidney Disease Sibling")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryKidneyDiseaseSibling { get; set; } = "false";

        [Display(Name = "Family History Eye Disease Father")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryEyeDiseaseFather { get; set; } = "false";

        [Display(Name = "Family History Eye Disease Mother")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryEyeDiseaseMother { get; set; } = "false";

        [Display(Name = "Family History Eye Disease Sibling")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryEyeDiseaseSibling { get; set; } = "false";

        // Lifestyle Factors
        [Display(Name = "Smoking Status")]
        [StringLength(4000)]
        [Encrypted]
        public string? SmokingStatus { get; set; }

        [Display(Name = "Smoking Quit Duration")]
        [StringLength(4000)]
        [Encrypted]
        public string? SmokingQuitDuration { get; set; }

        [Display(Name = "High Salt Intake")]
        [StringLength(4000)]
        [Encrypted]
        public string? HighSaltIntake { get; set; } = "false";

        [Display(Name = "Alcohol Frequency")]
        [StringLength(4000)]
        [Encrypted]
        public string? AlcoholFrequency { get; set; }

        [Display(Name = "Alcohol Consumption")]
        [StringLength(4000)]
        [Encrypted]
        public string? AlcoholConsumption { get; set; }

        [Display(Name = "Exercise Duration")]
        [StringLength(4000)]
        [Encrypted]
        public string? ExerciseDuration { get; set; }

        [Display(Name = "Risk Status")]
        [StringLength(4000)]
        [Encrypted]
        public string? RiskStatus { get; set; }

        // Chest Pain
        [Display(Name = "Chest Pain")]
        [StringLength(200)]
        [Encrypted]
        public string? ChestPain { get; set; }

        [Display(Name = "Chest Pain Location")]
        [StringLength(200)]
        [Encrypted]
        public string? ChestPainLocation { get; set; }

        [Display(Name = "Chest Pain Value")]
        [StringLength(4000)]
        [Encrypted]
        public string? ChestPainValue { get; set; }

        [Display(Name = "Has Difficulty Breathing")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasDifficultyBreathing { get; set; } = "false";

        [Display(Name = "Has Asthma")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasAsthma { get; set; } = "false";

        [Display(Name = "Has No Regular Exercise")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasNoRegularExercise { get; set; } = "false";

        // System Fields
        [Display(Name = "Created At")]
        [StringLength(4000)]
        [Encrypted]
        public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        [Display(Name = "Updated At")]
        [StringLength(4000)]
        [Encrypted]
        public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        [Display(Name = "Appointment Type")]
        [StringLength(200)]
        [Encrypted]
        public string? AppointmentType { get; set; }

        // Medication and Year Fields
        [Display(Name = "Cancer Medication")]
        [StringLength(200)]
        [Encrypted]
        public string? CancerMedication { get; set; }

        [Display(Name = "Cancer Year")]
        [StringLength(4000)]
        [Encrypted]
        public string? CancerYear { get; set; }

        [Display(Name = "Civil Status")]
        [StringLength(4000)]
        [Encrypted]
        public string? CivilStatus { get; set; }

        [Display(Name = "Diabetes Medication")]
        [StringLength(200)]
        [Encrypted]
        public string? DiabetesMedication { get; set; }

        [Display(Name = "Diabetes Year")]
        [StringLength(4000)]
        [Encrypted]
        public string? DiabetesYear { get; set; }

        // Family History Details
        [Display(Name = "Family History Cancer Father")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryCancerFather { get; set; } = "false";

        [Display(Name = "Family History Cancer Mother")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryCancerMother { get; set; } = "false";

        [Display(Name = "Family History Cancer Sibling")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryCancerSibling { get; set; } = "false";

        [Display(Name = "Family History Diabetes Father")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryDiabetesFather { get; set; } = "false";

        [Display(Name = "Family History Diabetes Mother")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryDiabetesMother { get; set; } = "false";

        [Display(Name = "Family History Diabetes Sibling")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryDiabetesSibling { get; set; } = "false";

        [Display(Name = "Family History Heart Disease Father")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryHeartDiseaseFather { get; set; } = "false";

        [Display(Name = "Family History Heart Disease Mother")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryHeartDiseaseMother { get; set; } = "false";

        [Display(Name = "Family History Heart Disease Sibling")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryHeartDiseaseSibling { get; set; } = "false";

        [Display(Name = "Family History Lung Disease Father")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryLungDiseaseFather { get; set; } = "false";

        [Display(Name = "Family History Lung Disease Mother")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryLungDiseaseMother { get; set; } = "false";

        [Display(Name = "Family History Lung Disease Sibling")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryLungDiseaseSibling { get; set; } = "false";

        [Display(Name = "Family History Other")]
        [StringLength(200)]
        [Encrypted]
        public string? FamilyHistoryOther { get; set; }

        [Display(Name = "Family History Other Father")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryOtherFather { get; set; } = "false";

        [Display(Name = "Family History Other Mother")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryOtherMother { get; set; } = "false";

        [Display(Name = "Family History Other Sibling")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryOtherSibling { get; set; } = "false";

        [Display(Name = "Family History Stroke Father")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryStrokeFather { get; set; } = "false";

        [Display(Name = "Family History Stroke Mother")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryStrokeMother { get; set; } = "false";

        [Display(Name = "Family History Stroke Sibling")]
        [StringLength(4000)]
        [Encrypted]
        public string? FamilyHistoryStrokeSibling { get; set; } = "false";

        [Display(Name = "First Name")]
        [StringLength(4000)]
        [Encrypted]
        public string? FirstName { get; set; }

        [Display(Name = "Hypertension Medication")]
        [StringLength(200)]
        [Encrypted]
        public string? HypertensionMedication { get; set; }

        [Display(Name = "Hypertension Year")]
        [StringLength(4000)]
        [Encrypted]
        public string? HypertensionYear { get; set; }

        [Display(Name = "Last Name")]
        [StringLength(4000)]
        [Encrypted]
        public string? LastName { get; set; }

        [Display(Name = "Lung Disease Medication")]
        [StringLength(200)]
        [Encrypted]
        public string? LungDiseaseMedication { get; set; }

        [Display(Name = "Lung Disease Year")]
        [StringLength(4000)]
        [Encrypted]
        public string? LungDiseaseYear { get; set; }

        [Display(Name = "Middle Name")]
        [StringLength(4000)]
        [Encrypted]
        public string? MiddleName { get; set; }

        [Display(Name = "Occupation")]
        [StringLength(200)]
        [Encrypted]
        public string? Occupation { get; set; }

        // Anthropometric Measurements
        [Display(Name = "Weight")]
        [StringLength(4000)]
        [Encrypted]
        public string? Weight { get; set; }

        [Display(Name = "Height")]
        [StringLength(4000)]
        [Encrypted]
        public string? Height { get; set; }

        [Display(Name = "BMI")]
        [StringLength(4000)]
        [Encrypted]
        public string? BMI { get; set; }

        [Display(Name = "Waist")]
        [StringLength(4000)]
        [Encrypted]
        public string? Waist { get; set; }

        [Display(Name = "Hip")]
        [StringLength(4000)]
        [Encrypted]
        public string? Hip { get; set; }

        [Display(Name = "WH Ratio")]
        [StringLength(4000)]
        [Encrypted]
        public string? WHRatio { get; set; }

        [Display(Name = "BMI Status")]
        [StringLength(4000)]
        [Encrypted]
        public string? BMIStatus { get; set; }

        [Display(Name = "WH Status")]
        [StringLength(4000)]
        [Encrypted]
        public string? WHStatus { get; set; }

        // Blood Sugar
        [Display(Name = "Fasting Blood Sugar")]
        [StringLength(4000)]
        [Encrypted]
        public string? FastingBloodSugar { get; set; }

        [Display(Name = "Random Blood Sugar")]
        [StringLength(4000)]
        [Encrypted]
        public string? RandomBloodSugar { get; set; }

        [Display(Name = "Blood Sugar Status")]
        [StringLength(4000)]
        [Encrypted]
        public string? BloodSugarStatus { get; set; }

        [Display(Name = "Has Polyuria")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasPolyuria { get; set; } = "false";

        [Display(Name = "Has Polydipsia")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasPolydipsia { get; set; } = "false";

        [Display(Name = "Has Polyphagia")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasPolyphagia { get; set; } = "false";

        [Display(Name = "Has Weight Loss")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasWeightLoss { get; set; } = "false";

        // Blood Pressure
        [Display(Name = "Left Arm Mean BP")]
        [StringLength(4000)]
        [Encrypted]
        public string? LeftArmMeanBP { get; set; }

        [Display(Name = "Right Arm Mean BP")]
        [StringLength(4000)]
        [Encrypted]
        public string? RightArmMeanBP { get; set; }

        [Display(Name = "Baseline BP")]
        [StringLength(4000)]
        [Encrypted]
        public string? BaselineBP { get; set; }

        [Display(Name = "BP Status")]
        [StringLength(4000)]
        [Encrypted]
        public string? BPStatus { get; set; }

        // Cholesterol
        [Display(Name = "Cholesterol Result")]
        [StringLength(4000)]
        [Encrypted]
        public string? CholesterolResult { get; set; }

        [Display(Name = "Cholesterol Status")]
        [StringLength(4000)]
        [Encrypted]
        public string? CholesterolStatus { get; set; }

        // Urine Dipstick Test
        [Display(Name = "Urine Protein")]
        [StringLength(4000)]
        [Encrypted]
        public string? UrineProtein { get; set; }

        [Display(Name = "Urine Ketones")]
        [StringLength(4000)]
        [Encrypted]
        public string? UrineKetones { get; set; }

        [Display(Name = "Has Urine Protein")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasUrineProtein { get; set; } = "false";

        [Display(Name = "Has Urine Ketones")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasUrineKetones { get; set; } = "false";

        // Risk Profile (For Doctors Only)
        [Display(Name = "Risk Percentage")]
        [StringLength(4000)]
        [Encrypted]
        public string? RiskPercentage { get; set; }

        // Cancer Screening (Women 30 years old and above)
        [Display(Name = "Breast Cancer Screened")]
        [StringLength(4000)]
        [Encrypted]
        public string? BreastCancerScreened { get; set; } = "false";

        [Display(Name = "Cervical Cancer Screened")]
        [StringLength(4000)]
        [Encrypted]
        public string? CervicalCancerScreened { get; set; } = "false";

        [Display(Name = "Cancer Screening Status")]
        [StringLength(4000)]
        [Encrypted]
        public string? CancerScreeningStatus { get; set; }

        // Assessment Information
        [Display(Name = "Interviewed By")]
        [StringLength(200)]
        [Encrypted]
        public string? InterviewedBy { get; set; }

        [Display(Name = "Designation")]
        [StringLength(200)]
        [Encrypted]
        public string? Designation { get; set; }

        [Display(Name = "Assessment Date")]
        [StringLength(4000)]
        [Encrypted]
        public string? AssessmentDate { get; set; }

        [Display(Name = "Patient Signature")]
        [StringLength(200)]
        [Encrypted]
        public string? PatientSignature { get; set; }

        // Chest Pain Details (Q2.1-2.7)
        [Display(Name = "Has Chest Pain")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasChestPain { get; set; } = "false";

        [Display(Name = "Chest Pain Spreads to Arm")]
        [StringLength(4000)]
        [Encrypted]
        public string? ChestPainSpreadsToArm { get; set; } = "false";

        [Display(Name = "Numbness When Walking Fast")]
        [StringLength(4000)]
        [Encrypted]
        public string? NumbnessWhenWalkingFast { get; set; } = "false";

        [Display(Name = "Pain Relieved with Rest")]
        [StringLength(4000)]
        [Encrypted]
        public string? PainRelievedWithRest { get; set; } = "false";

        [Display(Name = "Loss of Consciousness Less Than 10 Min")]
        [StringLength(4000)]
        [Encrypted]
        public string? LossOfConsciousnessLessThan10Min { get; set; } = "false";

        [Display(Name = "Pain Lasts More Than 30 Min")]
        [StringLength(4000)]
        [Encrypted]
        public string? PainLastsMoreThan30Min { get; set; } = "false";

        [Display(Name = "See Doctor If Yes")]
        [StringLength(4000)]
        [Encrypted]
        public string? SeeDoctorIfYes { get; set; } = "false";

        [Display(Name = "Doctor Name")]
        [StringLength(200)]
        [Encrypted]
        public string? DoctorName { get; set; }

        // Nutrition Details
        [Display(Name = "Eats Vegetables Daily")]
        [StringLength(4000)]
        [Encrypted]
        public string? EatsVegetablesDaily { get; set; } = "false";

        [Display(Name = "Eats Fruits Daily")]
        [StringLength(4000)]
        [Encrypted]
        public string? EatsFruitsDaily { get; set; } = "false";

        [Display(Name = "Eats Fish Daily")]
        [StringLength(4000)]
        [Encrypted]
        public string? EatsFishDaily { get; set; } = "false";

        [Display(Name = "Eats Meat Daily")]
        [StringLength(4000)]
        [Encrypted]
        public string? EatsMeatDaily { get; set; } = "false";

        [Display(Name = "Has Unhealthy Diet")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasUnhealthyDiet { get; set; } = "false";

        [Display(Name = "Eats Fatty Food More Than 2 Times Per Week")]
        [StringLength(4000)]
        [Encrypted]
        public string? EatsFattyFoodMoreThan2TimesPerWeek { get; set; } = "false";

        [Display(Name = "Eats Sweet Food More Than 2 Times Per Week")]
        [StringLength(4000)]
        [Encrypted]
        public string? EatsSweetFoodMoreThan2TimesPerWeek { get; set; } = "false";

        [Display(Name = "Eats Oily Food More Than 2 Times Per Week")]
        [StringLength(4000)]
        [Encrypted]
        public string? EatsOilyFoodMoreThan2TimesPerWeek { get; set; } = "false";

        [Display(Name = "Has High Salt Intake")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasHighSaltIntake { get; set; } = "false";

        // Alcohol Details
        [Display(Name = "Drinks Alcohol")]
        [StringLength(4000)]
        [Encrypted]
        public string? DrinksAlcohol { get; set; } = "false";

        [Display(Name = "Drinks Beer")]
        [StringLength(4000)]
        [Encrypted]
        public string? DrinksBeer { get; set; } = "false";

        [Display(Name = "Drinks Wine")]
        [StringLength(4000)]
        [Encrypted]
        public string? DrinksWine { get; set; } = "false";

        [Display(Name = "Drinks Whisky Gin Brandy")]
        [StringLength(4000)]
        [Encrypted]
        public string? DrinksWhiskyGinBrandy { get; set; } = "false";

        [Display(Name = "Alcohol Amount 1 Bottle 320ml")]
        [StringLength(4000)]
        [Encrypted]
        public string? AlcoholAmount1Bottle320ml { get; set; } = "false";

        [Display(Name = "Alcohol Amount 2 Bottle 640ml")]
        [StringLength(4000)]
        [Encrypted]
        public string? AlcoholAmount2Bottle640ml { get; set; } = "false";

        [Display(Name = "Alcohol Amount Less Than 3 Shot 45ml")]
        [StringLength(4000)]
        [Encrypted]
        public string? AlcoholAmountLessThan3Shot45ml { get; set; } = "false";

        [Display(Name = "Alcohol Amount 3-4 Wine Glasses 300ml")]
        [StringLength(4000)]
        [Encrypted]
        public string? AlcoholAmount3to4WineGlasses300ml { get; set; } = "false";

        [Display(Name = "Alcohol Amount More Than 4 Shots 75ml")]
        [StringLength(4000)]
        [Encrypted]
        public string? AlcoholAmountMoreThan4Shots75ml { get; set; } = "false";

        [Display(Name = "Alcohol Per Occasion")]
        [StringLength(4000)]
        [Encrypted]
        public string? AlcoholPerOccasion { get; set; }

        [Display(Name = "Alcohol Frequency 1-3 Times Per Week")]
        [StringLength(4000)]
        [Encrypted]
        public string? AlcoholFrequency1to3TimesPerWeek { get; set; } = "false";

        [Display(Name = "Alcohol Frequency More Than 4 Times Per Week")]
        [StringLength(4000)]
        [Encrypted]
        public string? AlcoholFrequencyMoreThan4TimesPerWeek { get; set; } = "false";

        [Display(Name = "Is Binge Drinker")]
        [StringLength(4000)]
        [Encrypted]
        public string? IsBingeDrinker { get; set; } = "false";

        // Physical Activity Details
        [Display(Name = "Moderate Intensity Exercise")]
        [StringLength(4000)]
        [Encrypted]
        public string? ModerateIntensityExercise { get; set; } = "false";

        [Display(Name = "Vigorous Intensity Exercise")]
        [StringLength(4000)]
        [Encrypted]
        public string? VigorousIntensityExercise { get; set; } = "false";

        [Display(Name = "Combination Exercise")]
        [StringLength(4000)]
        [Encrypted]
        public string? CombinationExercise { get; set; } = "false";

        [Display(Name = "Insufficient Physical Activity")]
        [StringLength(4000)]
        [Encrypted]
        public string? InsufficientPhysicalActivity { get; set; } = "false";

        // Smoking Details
        [Display(Name = "Former Smoker")]
        [StringLength(4000)]
        [Encrypted]
        public string? FormerSmoker { get; set; } = "false";

        [Display(Name = "Never Smoked But Exposed to Smoke")]
        [StringLength(4000)]
        [Encrypted]
        public string? NeverSmokedButExposedToSmoke { get; set; } = "false";

        [Display(Name = "Has History of Smoking")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasHistoryOfSmoking { get; set; } = "false";

        // Stress
        [Display(Name = "Has Stress")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasStress { get; set; } = "false";

        // ID Number
        [Display(Name = "ID Number")]
        [StringLength(4000)]
        [Encrypted]
        public string? IDNumber { get; set; }

        [Display(Name = "ID No")]
        [StringLength(4000)]
        [Encrypted]
        public string? IDNo { get; set; }

        // Date of Assessment
        [Display(Name = "Date of Assessment")]
        [StringLength(4000)]
        [Encrypted]
        public string? DateOfAssessment { get; set; }

        // Missing form fields that need to be added for complete mapping
        
        // Chest Pain Questions (Q2.1-2.8) - Missing mappings
        [Display(Name = "Pananakit 2.1")]
        [StringLength(4000)]
        [Encrypted]
        public string? Pananakit21 { get; set; } = "false"; // Q2.1 Nakakaramdam ka ba ng Pananakit o kabigatan sa dibdib?

        [Display(Name = "Pananakit 2.2")]
        [StringLength(4000)]
        [Encrypted]
        public string? Pananakit22 { get; set; } = "false"; // Q2.2 Ang sakit ba ay nasa gitna ng dibdib, kaliwang bahagi ng dibdib hanggang sa kaliwang braso?

        [Display(Name = "Pananakit 2.3")]
        [StringLength(4000)]
        [Encrypted]
        public string? Pananakit23 { get; set; } = "false"; // Q2.3 Nararamdaman mo ba ito kung nagmamadali o naglalakad ng mabilis o pakiramdam na paakyat?

        [Display(Name = "Pananakit 2.4")]
        [StringLength(4000)]
        [Encrypted]
        public string? Pananakit24 { get; set; } = "false"; // Q2.4 Napapatigil ka ba sa paglalakad kapag sumasakit ang iyong dibdib?

        [Display(Name = "Pananakit 2.5")]
        [StringLength(4000)]
        [Encrypted]
        public string? Pananakit25 { get; set; } = "false"; // Q2.5 Nawawala ba ang sakit kapag hindi ka kumikilos o naglalagay ng gamot sa ilalim ng dila?

        [Display(Name = "Pananakit 2.6")]
        [StringLength(4000)]
        [Encrypted]
        public string? Pananakit26 { get; set; } = "false"; // Q2.6 Nawawala ba ang pananakit sa loob ng 10 minuto?

        [Display(Name = "Pananakit 2.7")]
        [StringLength(4000)]
        [Encrypted]
        public string? Pananakit27 { get; set; } = "false"; // Q2.7 Nakakaramdam ka ba ng sakit sa dibdib na tumatagal higit sa 30 minuto?

        [Display(Name = "Pananakit 2.8")]
        [StringLength(4000)]
        [Encrypted]
        public string? Pananakit28 { get; set; } = "false"; // Q2.8 Nakakaramdam ka ba hirap sa pagsasalita, panghihina ng braso at/o binti o pamamanhid sa kalahating bahagi ng katawan?
        
        // Nutrition - Missing detailed mappings
        [Display(Name = "Nutrisyon Madalas Gulay")]
        [StringLength(4000)]
        [Encrypted]
        public string? NutrisyonMadalasGulay { get; set; } = "false"; // Madalas mo bang kainin ang mga sumusunod? - Gulay

        [Display(Name = "Nutrisyon Madalas Pratas")]
        [StringLength(4000)]
        [Encrypted]
        public string? NutrisyonMadalasPratas { get; set; } = "false"; // Madalas mo bang kainin ang mga sumusunod? - Prutas

        [Display(Name = "Nutrisyon Madalas Isda")]
        [StringLength(4000)]
        [Encrypted]
        public string? NutrisyonMadalasIsda { get; set; } = "false"; // Madalas mo bang kainin ang mga sumusunod? - Isda

        [Display(Name = "Nutrisyon Madalas Karne")]
        [StringLength(4000)]
        [Encrypted]
        public string? NutrisyonMadalasKarne { get; set; } = "false"; // Madalas mo bang kainin ang mga sumusunod? - Karne

        [Display(Name = "Nutrisyon Kumakain Matatamis")]
        [StringLength(4000)]
        [Encrypted]
        public string? NutrisyonKumakainMatatamis { get; set; } = "false"; // Kumakain ka ba ng higit sa 2 beses kada linggo ng - Matatamis na pagkain

        [Display(Name = "Nutrisyon Kumakain Mamantika")]
        [StringLength(4000)]
        [Encrypted]
        public string? NutrisyonKumakainMamantika { get; set; } = "false"; // Kumakain ka ba ng higit sa 2 beses kada linggo ng - Mamantikang pagkain
        
        // Alcohol - Missing detailed mappings
        [Display(Name = "Alcohol Inom")]
        [StringLength(4000)]
        [Encrypted]
        public string? AlcoholInom { get; set; } = "false"; // Umiinom ka ba ng alak?

        [Display(Name = "Alchohol Type Beer")]
        [StringLength(4000)]
        [Encrypted]
        public string? AlchoholTypeBeer { get; set; } = "false"; // Alcohol type - Beer

        [Display(Name = "Alchohol Type Wine")]
        [StringLength(4000)]
        [Encrypted]
        public string? AlchoholTypeWine { get; set; } = "false"; // Alcohol type - Wine

        [Display(Name = "Alchohol Type Whisky")]
        [StringLength(4000)]
        [Encrypted]
        public string? AlchoholTypeWhisky { get; set; } = "false"; // Alcohol type - Whisky/Gin/Brandy

        [Display(Name = "Beer Consumption 1")]
        [StringLength(4000)]
        [Encrypted]
        public string? BeerConsumption1 { get; set; } = "false"; // Beer consumption - 1 bote (320 ml)

        [Display(Name = "Beer Consumption 2")]
        [StringLength(4000)]
        [Encrypted]
        public string? BeerConsumption2 { get; set; } = "false"; // Beer consumption - 2 bote (640 ml)

        [Display(Name = "Beer Consumption 3")]
        [StringLength(4000)]
        [Encrypted]
        public string? BeerConsumption3 { get; set; } = "false"; // Beer consumption - ≥ 3 bote

        [Display(Name = "Wine Consumption 1")]
        [StringLength(4000)]
        [Encrypted]
        public string? WineConsumption1 { get; set; } = "false"; // Wine consumption - ≤ 3 wine glasses (300 ml)

        [Display(Name = "Wine Consumption 2")]
        [StringLength(4000)]
        [Encrypted]
        public string? WineConsumption2 { get; set; } = "false"; // Wine consumption - > 4 wine glasses

        [Display(Name = "Whisky Consumption 1")]
        [StringLength(4000)]
        [Encrypted]
        public string? WhiskyConsumption1 { get; set; } = "false"; // Whisky consumption - ≤ 3 shots/jigger (75 ml)

        [Display(Name = "Whisky Consumption 2")]
        [StringLength(4000)]
        [Encrypted]
        public string? WhiskyConsumption2 { get; set; } = "false"; // Whisky consumption - ≥ 4 shots / jigger

        [Display(Name = "Alcohol Okasyon")]
        [StringLength(4000)]
        [Encrypted]
        public string? AlcoholOkasyon { get; set; } = "false"; // Sa isang okasyon, ilang bote ng alak ang naiinom mo?
        
        // Exercise - Missing detailed mappings
        [Display(Name = "Ehersisyo Regular")]
        [StringLength(4000)]
        [Encrypted]
        public string? EhersisyoRegular { get; set; } = "false"; // May sapat ka bang ehersisyo?

        [Display(Name = "Ehersisyo Duration")]
        [StringLength(4000)]
        [Encrypted]
        public string? EhersisyoDuration { get; set; } = "false"; // Gaano katagal sa isang araw?

        [Display(Name = "Ehersisyo Type")]
        [StringLength(4000)]
        [Encrypted]
        public string? EhersisyoType { get; set; } = "false"; // Anong klaseng ehersisyo ang ginagawa mo?
        
        // Smoking - Missing detailed mappings
        [Display(Name = "Sigarilyo Kadami")]
        [StringLength(4000)]
        [Encrypted]
        public string? SigarilyoKadami { get; set; } = "false"; // Gaano kadami ang nauubos sa isang araw?

        [Display(Name = "Sigarilyo Tumigil")]
        [StringLength(4000)]
        [Encrypted]
        public string? SigarilyoTumigil { get; set; } = "false"; // Tumigil na sa paninigarilyo?

        [Display(Name = "Sigarilyo Usok")]
        [StringLength(4000)]
        [Encrypted]
        public string? SigarilyoUsok { get; set; } = "false"; // Hindi, pero lantad ka ba sa usok ng sigarilyo?

        [Display(Name = "Sigarilyo Sticks")]
        [StringLength(4000)]
        [Encrypted]
        public string? SigarilyoSticks { get; set; } = "false"; // Ikaw ba ay nanatili sa usok ng sigarilyo?
        
        // Stress - Missing detailed mappings
        [Display(Name = "Stress Madalas")]
        [StringLength(4000)]
        [Encrypted]
        public string? StressMadalas { get; set; } = "false"; // Madalas ka bang ma-stress?

        [Display(Name = "Stress Sino")]
        [StringLength(4000)]
        [Encrypted]
        public string? StressSino { get; set; } = "false"; // Ano o Sino ang dahilan ng iyong stress?

        [Display(Name = "Stress Epekto")]
        [StringLength(4000)]
        [Encrypted]
        public string? StressEpekto { get; set; } = "false"; // Naepektuhan ba nito ang iyong pang-araw-araw na gawain?
        
        // Additional missing fields for complete form mapping
        [Display(Name = "Health Facility Name")]
        [StringLength(4000)]
        [Encrypted]
        public string? HealthFacilityName { get; set; } = "Baesa Health Center"; // Health Facility name

        [Display(Name = "Date Assessment")]
        [StringLength(4000)]
        [Encrypted]
        public string? DateAssessment { get; set; } = ""; // Date of Assessment
        
        // Lung Disease - Missing proper mapping
        [Display(Name = "Has Lung Disease Non Infectious")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasLungDiseaseNonInfectious { get; set; } = "false"; // Sakit sa baga hindu nakakahawa
        
        // Eye Disease - Missing proper mapping  
        [Display(Name = "Has Eye Disease Condition")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasEyeDiseaseCondition { get; set; } = "false"; // Sakit sa mata
        
        // Stroke Symptoms (Q2.8)
        [Display(Name = "Has Stroke Symptoms")]
        [StringLength(4000)]
        [Encrypted]
        public string? HasStrokeSymptoms { get; set; } = "false"; // Q2.8 Nakakaramdam ka ba hirap sa pagsasalita, panghihina ng braso at/o binti o pamamanhid sa kalahating bahagi ng katawan?

    }
}