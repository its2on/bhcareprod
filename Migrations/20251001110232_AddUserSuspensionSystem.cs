using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barangay.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSuspensionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdolescentHealthInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AppointmentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatientName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatientAge = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatientGender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatientAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatientContact = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeightCm = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WeightKg = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BMI = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BMICategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MRMMRDateGiven = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TdDateGiven = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HPVDateGiven = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Temperature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BloodPressure = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PulseRate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RespiratoryRate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChiefComplaint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkingDiagnosis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferredTo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfMenarche = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgeOf1stPregnancy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OBScoreGravida = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OBScoreParity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HistoryOfPresentIllness = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhysicalExaminationFindings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PastMedicalHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FamilyHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Management = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReasonForReferral = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FollowUpDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdolescentHealthInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EncryptedStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EncryptedFullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Specialization = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    WorkingDays = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkingHours = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxDailyPatients = table.Column<int>(type: "int", nullable: false),
                    BirthDate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Age = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Barangay = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProfilePicture = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProfileImage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhilHealthId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastActive = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JoinDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserType = table.Column<int>(type: "int", nullable: false),
                    HasAgreedToTerms = table.Column<bool>(type: "bit", nullable: false),
                    AgreedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppointmentReminders = table.Column<bool>(type: "bit", nullable: false),
                    PrescriptionAlerts = table.Column<bool>(type: "bit", nullable: false),
                    HealthTips = table.Column<bool>(type: "bit", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Suffix = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Occupation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CivilStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Religion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false, computedColumnSql: "TRIM(ISNULL(FirstName + ' ', '') + ISNULL(MiddleName + ' ', '') + ISNULL(LastName, ''))"),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Assessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FamilyNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReasonForVisit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Symptoms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConsultationTimeSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsultationType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsBooked = table.Column<bool>(type: "bit", nullable: false),
                    BookedById = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BookedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultationTimeSlots", x => x.Id);
                });

            // Check if EmailVerifications table already exists before creating it
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EmailVerifications')
                BEGIN
                    CREATE TABLE [EmailVerifications] (
                        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [Email] NVARCHAR(255) NOT NULL,
                        [VerificationCode] NVARCHAR(10) NOT NULL,
                        [ExpiryTime] DATETIME2 NOT NULL,
                        [IsVerified] BIT NOT NULL DEFAULT(0),
                        [CreatedAt] DATETIME2 NOT NULL DEFAULT(GETUTCDATE()),
                        [VerifiedAt] DATETIME2 NULL
                    );
                END
            ");

            migrationBuilder.CreateTable(
                name: "FamilyRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FamilyNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeedbackRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ServiceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AppointmentId = table.Column<int>(type: "int", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackRatings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HEEADSSSAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppointmentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HealthFacility = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FamilyNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Age = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Height = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Weight = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BMI = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BMIUnderweight = table.Column<bool>(type: "bit", nullable: true),
                    BMINormal = table.Column<bool>(type: "bit", nullable: true),
                    BMIOverweight = table.Column<bool>(type: "bit", nullable: true),
                    BMIObese = table.Column<bool>(type: "bit", nullable: true),
                    ImmunizationMR = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImmunizationTd = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImmunizationHPV = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfMenarche = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgeOfFirstPregnancy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OBScore = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VitalTemp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VitalRR = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VitalPR = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VitalBP = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChiefComplaint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HistoryOfPresentIllness = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhysicalExaminationFindings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PastMedicalHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkingDiagnosis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Management = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FamilyHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferredTo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReasonForReferral = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FollowUpDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HomeEnvironment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FamilyRelationship = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HomeFamilyProblems = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HomeParentalListening = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HomeParentalBlame = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HomeFamilyChanges = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SchoolPerformance = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttendanceIssues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CareerPlans = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EducationCurrentlyStudying = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EducationWorking = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EducationSchoolWorkProblems = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EducationBullying = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EducationEmployment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DietDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WeightConcerns = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EatingDisorderSymptoms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EatingBodyImageSatisfaction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EatingDisorderedEatingBehaviors = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EatingWeightComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Hobbies = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhysicalActivity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScreenTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActivitiesParticipation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActivitiesRegularExercise = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActivitiesScreenTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubstanceUse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubstanceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DrugsTobaccoUse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DrugsAlcoholUse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DrugsIllicitDrugUse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DatingRelationships = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SexualActivity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SexualOrientation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SexualityBodyConcerns = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SexualityHealthConcerns = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SexualityPartnersCount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SexualityIntimateRelationships = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SexualityPartners = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SexualitySexualOrientation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SexualityPregnancy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SexualitySTI = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SexualityProtection = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SexualityPregnancyExperience = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SexualitySTIExperience = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SexualityProtectionUse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SexualityHarassment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MoodChanges = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuicidalThoughts = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SelfHarmBehavior = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeelsSafeAtHome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeelsSafeAtSchool = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExperiencedBullying = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PersonalStrengths = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupportSystems = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CopingMechanisms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SafetyPhysicalAbuse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SafetyRelationshipViolence = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SafetyProtectiveGear = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SafetyGunsAtHome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuicideDepressionFeelings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuicideSelfHarmThoughts = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuicideFamilyHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssessmentNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecommendedActions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FollowUpPlan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssessedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HEEADSSSAssessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImmunizationRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChildName = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    DateOfBirth = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    PlaceOfBirth = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    MotherName = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    FatherName = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Sex = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    BirthHeight = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    BirthWeight = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    HealthCenter = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Barangay = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    FamilyNumber = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    BCGVaccineDate = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    BCGVaccineRemarks = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HepatitisBVaccineDate = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HepatitisBVaccineRemarks = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Pentavalent1Date = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Pentavalent1Remarks = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Pentavalent2Date = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Pentavalent2Remarks = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Pentavalent3Date = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Pentavalent3Remarks = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OPV1Date = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OPV1Remarks = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OPV2Date = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OPV2Remarks = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OPV3Date = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OPV3Remarks = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IPV1Date = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IPV1Remarks = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IPV2Date = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IPV2Remarks = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PCV1Date = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PCV1Remarks = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PCV2Date = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PCV2Remarks = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PCV3Date = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PCV3Remarks = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MMR1Date = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MMR1Remarks = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MMR2Date = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MMR2Remarks = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    UpdatedAt = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImmunizationRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImmunizationShortcutForms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChildName = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    MotherName = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    FatherName = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Barangay = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    PreferredDate = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    PreferredTime = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    UpdatedAt = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImmunizationShortcutForms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegratedAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FamilyNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HealthFacility = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Barangay = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Birthday = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Telepono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Edad = table.Column<int>(type: "int", nullable: true),
                    Kasarian = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Relihiyon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasDiabetes = table.Column<bool>(type: "bit", nullable: false),
                    HasHypertension = table.Column<bool>(type: "bit", nullable: false),
                    HasCancer = table.Column<bool>(type: "bit", nullable: false),
                    HasCOPD = table.Column<bool>(type: "bit", nullable: false),
                    HasLungDisease = table.Column<bool>(type: "bit", nullable: false),
                    HasEyeDisease = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegratedAssessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Medications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Manufacturer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaffPositions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffPositions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DoctorAvailabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    Monday = table.Column<bool>(type: "bit", nullable: false),
                    Tuesday = table.Column<bool>(type: "bit", nullable: false),
                    Wednesday = table.Column<bool>(type: "bit", nullable: false),
                    Thursday = table.Column<bool>(type: "bit", nullable: false),
                    Friday = table.Column<bool>(type: "bit", nullable: false),
                    Saturday = table.Column<bool>(type: "bit", nullable: false),
                    Sunday = table.Column<bool>(type: "bit", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorAvailabilities_AspNetUsers_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doctors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Specialization = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LicenseNumber = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doctors_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feedbacks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GuardianInformation",
                columns: table => new
                {
                    GuardianId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GuardianFirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GuardianLastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResidencyProof = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ResidencyProofPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProofType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConsentStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuardianInformation", x => x.GuardianId);
                    table.ForeignKey(
                        name: "FK_GuardianInformation_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HealthReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CheckupDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BloodPressure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HeartRate = table.Column<int>(type: "int", nullable: true),
                    BloodSugar = table.Column<decimal>(type: "decimal(5,1)", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(5,1)", nullable: true),
                    Temperature = table.Column<decimal>(type: "decimal(4,1)", nullable: true),
                    PhysicalActivity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DoctorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthReports_AspNetUsers_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HealthReports_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReceiverId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SenderName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecipientGroup = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_AspNetUsers_ReceiverId",
                        column: x => x.ReceiverId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Messages_AspNetUsers_SenderId",
                        column: x => x.SenderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Link = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RecipientId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetOTPs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OTP = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetOTPs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetOTPs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmergencyContact = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EmergencyContactNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Room = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Diagnosis = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Alert = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Time = table.Column<TimeSpan>(type: "time", nullable: true),
                    Allergies = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MedicalHistory = table.Column<string>(type: "text", nullable: true),
                    CurrentMedications = table.Column<string>(type: "text", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Height = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BloodType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Patients_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StaffMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Specialization = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LicenseNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkingDays = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkingHours = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JoinDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxDailyPatients = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UrlTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ResourceId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OriginalUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClientIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrlTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UrlTokens_AspNetUsers_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDocuments_AspNetUsers_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserDocuments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSuspensions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DenialCount = table.Column<int>(type: "int", nullable: false),
                    LastDenialDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SuspensionStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SuspensionEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SuspensionReason = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SuspensionLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSuspensions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSuspensions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPermissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StaffPositionPermission",
                columns: table => new
                {
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    StaffPositionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffPositionPermission", x => new { x.PermissionId, x.StaffPositionId });
                    table.ForeignKey(
                        name: "FK_StaffPositionPermission_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StaffPositionPermission_StaffPositions_StaffPositionId",
                        column: x => x.StaffPositionId,
                        principalTable: "StaffPositions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DoctorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PatientName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DependentFullName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DependentAge = table.Column<int>(type: "int", nullable: true),
                    RelationshipToDependent = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ContactNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EmergencyContact = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmergencyContactNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Allergies = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MedicalHistory = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CurrentMedications = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AttachmentsData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppointmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppointmentTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    AppointmentTimeInput = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ReasonForVisit = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AgeValue = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AttachmentPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Prescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PatientUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Appointments_AspNetUsers_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Appointments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Appointments_Patients_PatientUserId",
                        column: x => x.PatientUserId,
                        principalTable: "Patients",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "FamilyMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Age = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FamilyNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MedicalHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Allergies = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FamilyMembers_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "LabResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TestName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReferenceRange = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabResults_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicalHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChiefComplaint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HistoryOfPresentIllness = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Allergies = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentMedications = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PastMedicalHistory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FamilyHistory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PersonalSocialHistory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewOfSystems = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhysicalExamination = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateRecorded = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalHistories_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Prescriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DoctorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Diagnosis = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PrescriptionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PatientUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prescriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prescriptions_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Prescriptions_AspNetUsers_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Prescriptions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Prescriptions_Patients_PatientUserId",
                        column: x => x.PatientUserId,
                        principalTable: "Patients",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "VitalSigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Temperature = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BloodPressure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    HeartRate = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RespiratoryRate = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SpO2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Weight = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Height = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EncryptedTemperature = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EncryptedBloodPressure = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EncryptedHeartRate = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EncryptedRespiratoryRate = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EncryptedSpO2 = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EncryptedWeight = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EncryptedHeight = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VitalSigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VitalSigns_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "StaffPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StaffMemberId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StaffPermissions_StaffMembers_StaffMemberId",
                        column: x => x.StaffMemberId,
                        principalTable: "StaffMembers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AppointmentAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppointmentId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AttachmentsData = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentAttachments_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AppointmentAttachments_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AppointmentFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppointmentId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentFiles_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MedicalRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RecordDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Diagnosis = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Treatment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DoctorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChiefComplaint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Duration = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Medications = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Prescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AppointmentId = table.Column<int>(type: "int", nullable: true),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalRecords_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicalRecords_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicalRecords_AspNetUsers_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicalRecords_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "NCDRiskAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AppointmentId = table.Column<int>(type: "int", nullable: true),
                    HealthFacility = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyNo = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Barangay = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Birthday = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Telepono = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Edad = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Kasarian = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Relihiyon = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HasDiabetes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HasHypertension = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HasCancer = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HasCOPD = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HasLungDisease = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HasEyeDisease = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EyeDiseaseYear = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EyeDiseaseMedication = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AlcoholStoppedDuration = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Smoked100Sticks = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HasEnoughExercise = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CancerType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FamilyHasHypertension = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHasHeartDisease = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHasStroke = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHasDiabetes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHasCancer = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHasKidneyDisease = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHasOtherDisease = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyOtherDiseaseDetails = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryKidneyDiseaseFather = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryKidneyDiseaseMother = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryKidneyDiseaseSibling = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryEyeDiseaseFather = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryEyeDiseaseMother = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryEyeDiseaseSibling = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SmokingStatus = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HighSaltIntake = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AlcoholFrequency = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AlcoholConsumption = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ExerciseDuration = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RiskStatus = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ChestPain = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ChestPainLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ChestPainValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HasDifficultyBreathing = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HasAsthma = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HasNoRegularExercise = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    UpdatedAt = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AppointmentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CancerMedication = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CancerYear = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CivilStatus = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DiabetesMedication = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DiabetesYear = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryCancerFather = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryCancerMother = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryCancerSibling = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryDiabetesFather = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryDiabetesMother = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryDiabetesSibling = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryHeartDiseaseFather = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryHeartDiseaseMother = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryHeartDiseaseSibling = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryLungDiseaseFather = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryLungDiseaseMother = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryLungDiseaseSibling = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryOther = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FamilyHistoryOtherFather = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryOtherMother = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryOtherSibling = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryStrokeFather = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryStrokeMother = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FamilyHistoryStrokeSibling = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HypertensionMedication = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HypertensionYear = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    LungDiseaseMedication = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LungDiseaseYear = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MiddleName = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Occupation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Weight = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Height = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    BMI = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Waist = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Hip = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    WHRatio = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    BMIStatus = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    WHStatus = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FastingBloodSugar = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RandomBloodSugar = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    BloodSugarStatus = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HasPolyuria = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HasPolydipsia = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HasPolyphagia = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HasWeightLoss = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    LeftArmMeanBP = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RightArmMeanBP = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    BaselineBP = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    BPStatus = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CholesterolResult = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CholesterolStatus = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    UrineProtein = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    UrineKetones = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HasUrineProtein = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HasUrineKetones = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RiskPercentage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    BreastCancerScreened = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CervicalCancerScreened = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CancerScreeningStatus = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    InterviewedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Designation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AssessmentDate = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PatientSignature = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HasChestPain = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ChestPainSpreadsToArm = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    NumbnessWhenWalkingFast = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PainRelievedWithRest = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    LossOfConsciousnessLessThan10Min = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PainLastsMoreThan30Min = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SeeDoctorIfYes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DoctorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EatsVegetablesDaily = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EatsFruitsDaily = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EatsFishDaily = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EatsMeatDaily = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HasUnhealthyDiet = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EatsFattyFoodMoreThan2TimesPerWeek = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EatsSweetFoodMoreThan2TimesPerWeek = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EatsOilyFoodMoreThan2TimesPerWeek = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HasHighSaltIntake = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DrinksAlcohol = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DrinksBeer = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DrinksWine = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DrinksWhiskyGinBrandy = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AlcoholAmount1Bottle320ml = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AlcoholAmount2Bottle640ml = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AlcoholAmountLessThan3Shot45ml = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AlcoholAmount3to4WineGlasses300ml = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AlcoholAmountMoreThan4Shots75ml = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AlcoholFrequency1to3TimesPerWeek = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AlcoholFrequencyMoreThan4TimesPerWeek = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsBingeDrinker = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ModerateIntensityExercise = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    VigorousIntensityExercise = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CombinationExercise = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    InsufficientPhysicalActivity = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FormerSmoker = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    NeverSmokedButExposedToSmoke = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HasHistoryOfSmoking = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HasStress = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IDNumber = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IDNo = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DateOfAssessment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NCDRiskAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NCDRiskAssessments_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NCDRiskAssessments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AppointmentId = table.Column<int>(type: "int", nullable: true),
                    DoctorId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Diagnosis = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Symptoms = table.Column<string>(type: "ntext", nullable: false),
                    Treatment = table.Column<string>(type: "ntext", nullable: false),
                    Notes = table.Column<string>(type: "ntext", nullable: false),
                    Medications = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RecordDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientHistories_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PatientHistories_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionMedications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrescriptionId = table.Column<int>(type: "int", nullable: false),
                    MedicationId = table.Column<int>(type: "int", nullable: false),
                    MedicationName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dosage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MedicalRecordId = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionMedications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrescriptionMedications_MedicalRecords_MedicalRecordId",
                        column: x => x.MedicalRecordId,
                        principalTable: "MedicalRecords",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PrescriptionMedications_Medications_MedicationId",
                        column: x => x.MedicationId,
                        principalTable: "Medications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrescriptionMedications_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentAttachments_ApplicationUserId",
                table: "AppointmentAttachments",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentAttachments_AppointmentId",
                table: "AppointmentAttachments",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentFiles_AppointmentId",
                table: "AppointmentFiles",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ApplicationUserId",
                table: "Appointments",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId",
                table: "Appointments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientUserId",
                table: "Appointments",
                column: "PatientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAvailabilities_DoctorId",
                table: "DoctorAvailabilities",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_UserId",
                table: "Doctors",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMembers_PatientId",
                table: "FamilyMembers",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMembers_UserId",
                table: "FamilyMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_UserId",
                table: "Feedbacks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GuardianInformation_UserId",
                table: "GuardianInformation",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HealthReports_DoctorId",
                table: "HealthReports",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthReports_UserId",
                table: "HealthReports",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_PatientId",
                table: "LabResults",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalHistories_PatientId",
                table: "MedicalHistories",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_ApplicationUserId",
                table: "MedicalRecords",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_AppointmentId",
                table: "MedicalRecords",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_DoctorId",
                table: "MedicalRecords",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_PatientId",
                table: "MedicalRecords",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ReceiverId",
                table: "Messages",
                column: "ReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_SenderId",
                table: "Messages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_NCDRiskAssessments_AppointmentId",
                table: "NCDRiskAssessments",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_NCDRiskAssessments_UserId",
                table: "NCDRiskAssessments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetOTPs_UserId",
                table: "PasswordResetOTPs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientHistories_AppointmentId",
                table: "PatientHistories",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientHistories_PatientId",
                table: "PatientHistories",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionMedications_MedicalRecordId",
                table: "PrescriptionMedications",
                column: "MedicalRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionMedications_MedicationId",
                table: "PrescriptionMedications",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionMedications_PrescriptionId",
                table: "PrescriptionMedications",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_ApplicationUserId",
                table: "Prescriptions",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_DoctorId",
                table: "Prescriptions",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PatientId",
                table: "Prescriptions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PatientUserId",
                table: "Prescriptions",
                column: "PatientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_UserId",
                table: "StaffMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPermissions_PermissionId",
                table: "StaffPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPermissions_StaffMemberId",
                table: "StaffPermissions",
                column: "StaffMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPositionPermission_StaffPositionId",
                table: "StaffPositionPermission",
                column: "StaffPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_UrlTokens_ExpiresAt",
                table: "UrlTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UrlTokens_IsUsed",
                table: "UrlTokens",
                column: "IsUsed");

            migrationBuilder.CreateIndex(
                name: "IX_UrlTokens_ResourceId",
                table: "UrlTokens",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_UrlTokens_ResourceType",
                table: "UrlTokens",
                column: "ResourceType");

            migrationBuilder.CreateIndex(
                name: "IX_UrlTokens_Token",
                table: "UrlTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDocuments_ApprovedBy",
                table: "UserDocuments",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserDocuments_Status",
                table: "UserDocuments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserDocuments_UploadDate",
                table: "UserDocuments",
                column: "UploadDate");

            migrationBuilder.CreateIndex(
                name: "IX_UserDocuments_UserId",
                table: "UserDocuments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_PermissionId",
                table: "UserPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_UserId",
                table: "UserPermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSuspensions_UserId",
                table: "UserSuspensions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VitalSigns_PatientId",
                table: "VitalSigns",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdolescentHealthInfo");

            migrationBuilder.DropTable(
                name: "AppointmentAttachments");

            migrationBuilder.DropTable(
                name: "AppointmentFiles");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Assessments");

            migrationBuilder.DropTable(
                name: "ConsultationTimeSlots");

            migrationBuilder.DropTable(
                name: "DoctorAvailabilities");

            migrationBuilder.DropTable(
                name: "Doctors");

            migrationBuilder.DropTable(
                name: "EmailVerifications");

            migrationBuilder.DropTable(
                name: "FamilyMembers");

            migrationBuilder.DropTable(
                name: "FamilyRecords");

            migrationBuilder.DropTable(
                name: "FeedbackRatings");

            migrationBuilder.DropTable(
                name: "Feedbacks");

            migrationBuilder.DropTable(
                name: "GuardianInformation");

            migrationBuilder.DropTable(
                name: "HealthReports");

            migrationBuilder.DropTable(
                name: "HEEADSSSAssessments");

            migrationBuilder.DropTable(
                name: "ImmunizationRecords");

            migrationBuilder.DropTable(
                name: "ImmunizationShortcutForms");

            migrationBuilder.DropTable(
                name: "IntegratedAssessments");

            migrationBuilder.DropTable(
                name: "LabResults");

            migrationBuilder.DropTable(
                name: "MedicalHistories");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "NCDRiskAssessments");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PasswordResetOTPs");

            migrationBuilder.DropTable(
                name: "PatientHistories");

            migrationBuilder.DropTable(
                name: "PrescriptionMedications");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "StaffPermissions");

            migrationBuilder.DropTable(
                name: "StaffPositionPermission");

            migrationBuilder.DropTable(
                name: "UrlTokens");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "UserDocuments");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "UserSuspensions");

            migrationBuilder.DropTable(
                name: "VitalSigns");

            migrationBuilder.DropTable(
                name: "MedicalRecords");

            migrationBuilder.DropTable(
                name: "Medications");

            migrationBuilder.DropTable(
                name: "Prescriptions");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "StaffMembers");

            migrationBuilder.DropTable(
                name: "StaffPositions");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
