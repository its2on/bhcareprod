-- ============================================
-- BH CARE - PART 6: ASSESSMENTS & FORMS
-- ============================================

USE [BHCareDB]
GO

-- HEEADSSSAssessments
CREATE TABLE [HEEADSSSAssessments] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [AppointmentId] NVARCHAR(MAX) NULL,
    [FullName] NVARCHAR(MAX) NULL,
    [HealthFacility] NVARCHAR(MAX) NULL,
    [FamilyNo] NVARCHAR(MAX) NULL,
    [Is4Ps] NVARCHAR(MAX) NULL,
    [IsNHPTS] NVARCHAR(MAX) NULL,
    [IsPhilHealthBeneficiaryOnly] NVARCHAR(MAX) NULL,
    [IsOwnPhilHealth] NVARCHAR(MAX) NULL,
    [PhilHealthPIN] NVARCHAR(MAX) NULL,
    [HomeEnvironment] NVARCHAR(MAX) NULL,
    [FamilyRelationships] NVARCHAR(MAX) NULL,
    [LivingSituation] NVARCHAR(MAX) NULL,
    [SchoolPerformance] NVARCHAR(MAX) NULL,
    [EducationalGoals] NVARCHAR(MAX) NULL,
    [SchoolProblems] NVARCHAR(MAX) NULL,
    [DietHabits] NVARCHAR(MAX) NULL,
    [BodyImage] NVARCHAR(MAX) NULL,
    [EatingDisorders] NVARCHAR(MAX) NULL,
    [PhysicalActivity] NVARCHAR(MAX) NULL,
    [Hobbies] NVARCHAR(MAX) NULL,
    [ScreenTime] NVARCHAR(MAX) NULL,
    [SubstanceUse] NVARCHAR(MAX) NULL,
    [AlcoholUse] NVARCHAR(MAX) NULL,
    [TobaccoUse] NVARCHAR(MAX) NULL,
    [SexualActivity] NVARCHAR(MAX) NULL,
    [SexualOrientation] NVARCHAR(MAX) NULL,
    [Contraception] NVARCHAR(MAX) NULL,
    [MentalHealth] NVARCHAR(MAX) NULL,
    [Depression] NVARCHAR(MAX) NULL,
    [SuicidalThoughts] NVARCHAR(MAX) NULL,
    [SafetyAtHome] NVARCHAR(MAX) NULL,
    [BullyingExperience] NVARCHAR(MAX) NULL,
    [ViolenceExposure] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_HEEADSSSAssessments_UserId] ON [HEEADSSSAssessments] ([UserId]);
GO

-- NCDRiskAssessments
CREATE TABLE [NCDRiskAssessments] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [AppointmentId] INT NULL,
    [Age] INT NULL,
    [Gender] NVARCHAR(MAX) NULL,
    [Weight] DECIMAL(5,2) NULL,
    [Height] DECIMAL(5,2) NULL,
    [BMI] DECIMAL(5,2) NULL,
    [WaistCircumference] DECIMAL(5,2) NULL,
    [SmokingStatus] NVARCHAR(MAX) NULL,
    [AlcoholConsumption] NVARCHAR(MAX) NULL,
    [PhysicalActivity] NVARCHAR(MAX) NULL,
    [DietQuality] NVARCHAR(MAX) NULL,
    [Hypertension] BIT NULL,
    [Diabetes] BIT NULL,
    [HighCholesterol] BIT NULL,
    [FamilyHistory] NVARCHAR(MAX) NULL,
    [RiskScore] DECIMAL(5,2) NULL,
    [RiskLevel] NVARCHAR(50) NULL,
    [Recommendations] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION,
    FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments]([Id]) ON DELETE SET NULL
);
CREATE INDEX [IX_NCDRiskAssessments_UserId] ON [NCDRiskAssessments] ([UserId]);
CREATE INDEX [IX_NCDRiskAssessments_AppointmentId] ON [NCDRiskAssessments] ([AppointmentId]);
GO

-- AdolescentHealthInfo
CREATE TABLE [AdolescentHealthInfo] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(MAX) NOT NULL,
    [AppointmentId] NVARCHAR(MAX) NULL,
    [PatientName] NVARCHAR(MAX) NULL,
    [PatientAge] NVARCHAR(MAX) NULL,
    [PatientGender] NVARCHAR(MAX) NULL,
    [PatientContact] NVARCHAR(MAX) NULL,
    [PatientAddress] NVARCHAR(MAX) NULL,
    [ChiefComplaint] NVARCHAR(MAX) NULL,
    [HistoryOfPresentIllness] NVARCHAR(MAX) NULL,
    [PastMedicalHistory] NVARCHAR(MAX) NULL,
    [FamilyHistory] NVARCHAR(MAX) NULL,
    [DateOfMenarche] NVARCHAR(MAX) NULL,
    [OBScoreGravida] NVARCHAR(MAX) NULL,
    [OBScoreParity] NVARCHAR(MAX) NULL,
    [AgeOf1stPregnancy] NVARCHAR(MAX) NULL,
    [WeightKg] NVARCHAR(MAX) NULL,
    [HeightCm] NVARCHAR(MAX) NULL,
    [BMI] NVARCHAR(MAX) NULL,
    [BMICategory] NVARCHAR(MAX) NULL,
    [BloodPressure] NVARCHAR(MAX) NULL,
    [Temperature] NVARCHAR(MAX) NULL,
    [PulseRate] NVARCHAR(MAX) NULL,
    [RespiratoryRate] NVARCHAR(MAX) NULL,
    [PhysicalExaminationFindings] NVARCHAR(MAX) NULL,
    [WorkingDiagnosis] NVARCHAR(MAX) NULL,
    [Management] NVARCHAR(MAX) NULL,
    [TdDateGiven] NVARCHAR(MAX) NULL,
    [MRMMRDateGiven] NVARCHAR(MAX) NULL,
    [HPVDateGiven] NVARCHAR(MAX) NULL,
    [ReferredTo] NVARCHAR(MAX) NULL,
    [ReasonForReferral] NVARCHAR(MAX) NULL,
    [FollowUpDate] NVARCHAR(MAX) NULL,
    [RecordedBy] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- IntegratedAssessments
CREATE TABLE [IntegratedAssessments] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [AppointmentId] INT NULL,
    [AssessmentType] NVARCHAR(100) NOT NULL,
    [AssessmentData] NVARCHAR(MAX) NULL,
    [Score] DECIMAL(5,2) NULL,
    [Recommendations] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_IntegratedAssessments_UserId] ON [IntegratedAssessments] ([UserId]);
GO

-- Assessments
CREATE TABLE [Assessments] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [PatientId] NVARCHAR(450) NOT NULL,
    [AssessmentType] NVARCHAR(MAX) NULL,
    [AssessmentDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [Results] NVARCHAR(MAX) NULL,
    FOREIGN KEY ([PatientId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_Assessments_PatientId] ON [Assessments] ([PatientId]);
GO
