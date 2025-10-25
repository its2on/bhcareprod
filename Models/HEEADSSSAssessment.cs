using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Barangay.Attributes;

namespace Barangay.Models
{
    public class HEEADSSSAssessment
    {
        [Key]
        public int Id { get; set; }
        
        public string? UserId { get; set; }
        
        [Encrypted]
        public string? AppointmentId { get; set; }
        
        [Encrypted]
        public string? HealthFacility { get; set; }
        
        // FamilyNo is a public identifier used for grouping - should NOT be encrypted
        public string? FamilyNo { get; set; }
        
        [Encrypted]
        public string? IsNHPTS { get; set; }
        
        [Encrypted]
        public string? Is4Ps { get; set; }
        
        [Encrypted]
        public string? IsPhilHealthBeneficiaryOnly { get; set; }
        
        [Encrypted]
        public string? IsOwnPhilHealth { get; set; }
        
        [Encrypted]
        public string? PhilHealthPIN { get; set; }
        
        // Personal Information 
        [Encrypted]
        public string? FullName { get; set; }
        
        [NotMapped]
        public DateTime? Birthday { get; set; }
        
        [Encrypted]
        public string? Age { get; set; }

        [Encrypted]
        public string? Gender { get; set; }

        [Encrypted]
        public string? Address { get; set; }

        [Encrypted]
        public string? ContactNumber { get; set; }
        
        // ADOLESCENT HEALTH INFORMATION - B. Measurements
        [Encrypted]
        public string? Height { get; set; }
        
        [Encrypted]
        public string? Weight { get; set; }
        
        [Encrypted]
        public string? BMI { get; set; }
        
        public bool? BMIUnderweight { get; set; }
        
        public bool? BMINormal { get; set; }
        
        public bool? BMIOverweight { get; set; }
        
        public bool? BMIObese { get; set; }
        
        // Immunization Status
        [Encrypted]
        public string? ImmunizationMR { get; set; }
        
        [Encrypted]
        public string? ImmunizationTd { get; set; }
        
        [Encrypted]
        public string? ImmunizationHPV { get; set; }
        
        // For Females Only
        [Encrypted]
        public string? DateOfMenarche { get; set; }
        
        [Encrypted]
        public string? AgeOfFirstPregnancy { get; set; }
        
        [Encrypted]
        public string? OBScore { get; set; }
        
        // Vital Signs
        [Encrypted]
        public string? VitalTemp { get; set; }
        
        [Encrypted]
        public string? VitalRR { get; set; }
        
        [Encrypted]
        public string? VitalPR { get; set; }
        
        [Encrypted]
        public string? VitalBP { get; set; }
        
        // Medical Information
        [Encrypted]
        public string? ChiefComplaint { get; set; }
        
        [Encrypted]
        public string? HistoryOfPresentIllness { get; set; }
        
        [Encrypted]
        public string? PhysicalExaminationFindings { get; set; }
        
        [Encrypted]
        public string? PastMedicalHistory { get; set; }
        
        [Encrypted]
        public string? WorkingDiagnosis { get; set; }
        
        [Encrypted]
        public string? Management { get; set; }
        
        [Encrypted]
        public string? FamilyHistory { get; set; }
        
        // Referral Information
        [Encrypted]
        public string? ReferredBy { get; set; }
        
        [Encrypted]
        public string? ReferredTo { get; set; }
        
        [Encrypted]
        public string? ReasonForReferral { get; set; }
        
        [Encrypted]
        public string? FollowUpDate { get; set; }
        
        // HOME
        [Encrypted]
        public string? HomeEnvironment { get; set; }
        
        [Encrypted]
        public string? FamilyRelationship { get; set; }
        
        [Encrypted]
        public string? HomeFamilyProblems { get; set; }
        
        [Encrypted]
        public string? HomeParentalListening { get; set; }
        
        [Encrypted]
        public string? HomeParentalBlame { get; set; }
        
        [Encrypted]
        public string? HomeFamilyChanges { get; set; }
        
        [Encrypted]
        public string? HomeRunawayThoughts { get; set; }
        
        // EDUCATION
        [Encrypted]
        public string? SchoolPerformance { get; set; }
        
        public bool? AttendanceIssues { get; set; }
        
        [Encrypted]
        public string? CareerPlans { get; set; }
        
        [Encrypted]
        public string? EducationCurrentlyStudying { get; set; }
        
        [Encrypted]
        public string? EducationWorking { get; set; }
        
        [Encrypted]
        public string? EducationSchoolWorkProblems { get; set; }
        
        [Encrypted]
        public string? EducationBullying { get; set; }
        
        [Encrypted]
        public string? EducationBullyingExperience { get; set; }
        
        [Encrypted]
        public string? EducationEmployment { get; set; }
        
        // EATING HABITS
        [Encrypted]
        public string? DietDescription { get; set; }
        
        public bool? WeightConcerns { get; set; }
        
        public bool? EatingDisorderSymptoms { get; set; }
        
        [Encrypted]
        public string? EatingBodyImageSatisfaction { get; set; }
        
        [Encrypted]
        public string? EatingDisorderedEatingBehaviors { get; set; }
        
        [Encrypted]
        public string? EatingWeightComments { get; set; }
        
        // EATING HABITS - Individual checkbox fields
        [Encrypted]
        public string? EatingVomiting { get; set; }
        
        [Encrypted]
        public string? EatingDietPills { get; set; }
        
        [Encrypted]
        public string? EatingLaxatives { get; set; }
        
        [Encrypted]
        public string? EatingStarvation { get; set; }
        
        // ACTIVITIES
        [Encrypted]
        public string? Hobbies { get; set; }
        
        [Encrypted]
        public string? PhysicalActivity { get; set; }
        
        [Encrypted]
        public string? ScreenTime { get; set; }
        
        [Encrypted]
        public string? ActivitiesParticipation { get; set; }
        
        [Encrypted]
        public string? ActivitiesRegularExercise { get; set; }
        
        [Encrypted]
        public string? ActivitiesScreenTime { get; set; }
        
        [Encrypted]
        public string? ActivitiesInternetGadgetUse { get; set; }
        
        // DRUGS
        public bool? SubstanceUse { get; set; }
        
        [Encrypted]
        public string? SubstanceType { get; set; }
        
        [Encrypted]
        public string? DrugsTobaccoUse { get; set; }
        
        [Encrypted]
        public string? DrugsAlcoholUse { get; set; }
        
        [Encrypted]
        public string? DrugsIllicitDrugUse { get; set; }
        
        [Encrypted]
        public string? DrugsStreetDrugs { get; set; }
        
        // SEXUALITY
        [Encrypted]
        public string? DatingRelationships { get; set; }
        
        public bool? SexualActivity { get; set; }
        
        [Encrypted]
        public string? SexualOrientation { get; set; }
        
        [Encrypted]
        public string? SexualityBodyConcerns { get; set; }
        
        [Encrypted]
        public string? SexualityHealthConcerns { get; set; }
        
        [Encrypted]
        public string? SexualityPartnersCount { get; set; }
        
        [Encrypted]
        public string? SexualityIntimateRelationships { get; set; }
        
        [Encrypted]
        public string? SexualityPartners { get; set; }
        
        [Encrypted]
        public string? SexualitySexualOrientation { get; set; }
        
        [Encrypted]
        public string? SexualityPregnancy { get; set; }
        
        [Encrypted]
        public string? SexualitySTI { get; set; }
        
        [Encrypted]
        public string? SexualityProtection { get; set; }
        
        [Encrypted]
        public string? SexualityPregnancyExperience { get; set; }
        
        [Encrypted]
        public string? SexualitySTIExperience { get; set; }
        
        [Encrypted]
        public string? SexualityProtectionUse { get; set; }
        
        [Encrypted]
        public string? SexualityHarassment { get; set; }
        
        [Encrypted]
        public string? SexualityGay { get; set; }
        
        [Encrypted]
        public string? SexualityLesbian { get; set; }
        
        [Encrypted]
        public string? SexualityBisexual { get; set; }
        
        // SUICIDE/DEPRESSION
        public bool? MoodChanges { get; set; }
        
        public bool? SuicidalThoughts { get; set; }
        
        public bool? SelfHarmBehavior { get; set; }
        
        // SAFETY
        public bool? FeelsSafeAtHome { get; set; }
        
        public bool? FeelsSafeAtSchool { get; set; }
        
        public bool? ExperiencedBullying { get; set; }
        
        // STRENGTHS
        [Encrypted]
        public string? PersonalStrengths { get; set; }
        
        [Encrypted]
        public string? SupportSystems { get; set; }
        
        [Encrypted]
        public string? CopingMechanisms { get; set; }
        
        // SAFETY/WEAPONS/VIOLENCE
        [Encrypted]
        public string? SafetyPhysicalAbuse { get; set; }
        
        [Encrypted]
        public string? SafetyRelationshipViolence { get; set; }
        
        [Encrypted]
        public string? SafetyProtectiveGear { get; set; }
        
        [Encrypted]
        public string? SafetyGunsAtHome { get; set; }
        
        [Encrypted]
        public string? SafetyWeaponAccess { get; set; }
        
        // SUICIDE/DEPRESSION
        [Encrypted]
        public string? SuicideDepressionFeelings { get; set; }
        
        [Encrypted]
        public string? SuicideSelfHarmThoughts { get; set; }
        
        [Encrypted]
        public string? SuicideFamilyHistory { get; set; }
        
        // Assessment Information
        [Encrypted]
        public string? AssessmentNotes { get; set; }
        
        [Encrypted]
        public string? RecommendedActions { get; set; }
        
        [Encrypted]
        public string? FollowUpPlan { get; set; }
        
        [Encrypted]
        public string? Notes { get; set; }
        
        [Encrypted]
        public string? AssessedBy { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
    }
} 