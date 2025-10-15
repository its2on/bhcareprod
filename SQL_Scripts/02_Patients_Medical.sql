-- ============================================
-- BH CARE - PART 2: PATIENTS & MEDICAL RECORDS
-- ============================================

USE [Barangay]
GO

-- Patients
CREATE TABLE [Patients] (
    [UserId] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [FullName] NVARCHAR(1000) NOT NULL,
    [Gender] NVARCHAR(10) NOT NULL,
    [BirthDate] DATETIME2 NOT NULL,
    [Address] NVARCHAR(1000) NOT NULL,
    [ContactNumber] NVARCHAR(100) NOT NULL,
    [EmergencyContact] NVARCHAR(500) NOT NULL,
    [EmergencyContactNumber] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(500) NOT NULL,
    [Status] NVARCHAR(50) NULL,
    [Room] NVARCHAR(20) NULL,
    [Diagnosis] NVARCHAR(2000) NULL,
    [Alert] NVARCHAR(2000) NULL,
    [Time] TIME NULL,
    [Allergies] NVARCHAR(2000) NULL,
    [MedicalHistory] NVARCHAR(MAX) NULL,
    [CurrentMedications] NVARCHAR(MAX) NULL,
    [Weight] DECIMAL(5,2) NULL,
    [Height] DECIMAL(5,2) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    [BloodType] NVARCHAR(100) NULL,
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_Patients_UserId] ON [Patients] ([UserId]);
GO

-- MedicalRecords
CREATE TABLE [MedicalRecords] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [PatientId] NVARCHAR(450) NOT NULL,
    [DoctorId] NVARCHAR(450) NOT NULL,
    [Date] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [Type] NVARCHAR(MAX) NULL,
    [ChiefComplaint] NVARCHAR(MAX) NULL,
    [Diagnosis] NVARCHAR(MAX) NULL,
    [Treatment] NVARCHAR(MAX) NULL,
    [Notes] NVARCHAR(MAX) NULL,
    [Status] NVARCHAR(MAX) NULL,
    [DoctorName] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    FOREIGN KEY ([PatientId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION,
    FOREIGN KEY ([DoctorId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_MedicalRecords_PatientId] ON [MedicalRecords] ([PatientId]);
CREATE INDEX [IX_MedicalRecords_DoctorId] ON [MedicalRecords] ([DoctorId]);
CREATE INDEX [IX_MedicalRecords_Date] ON [MedicalRecords] ([Date]);
GO

-- VitalSigns
CREATE TABLE [VitalSigns] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [PatientId] NVARCHAR(450) NULL,
    [Temperature] NVARCHAR(MAX) NULL,
    [BloodPressure] NVARCHAR(MAX) NULL,
    [HeartRate] NVARCHAR(MAX) NULL,
    [RespiratoryRate] NVARCHAR(MAX) NULL,
    [OxygenSaturation] NVARCHAR(MAX) NULL,
    [Weight] NVARCHAR(MAX) NULL,
    [Height] NVARCHAR(MAX) NULL,
    [BMI] NVARCHAR(MAX) NULL,
    [RecordedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [RecordedBy] NVARCHAR(MAX) NULL,
    [Notes] NVARCHAR(MAX) NULL,
    FOREIGN KEY ([PatientId]) REFERENCES [Patients]([UserId]) ON DELETE NO ACTION
);
CREATE INDEX [IX_VitalSigns_PatientId] ON [VitalSigns] ([PatientId]);
CREATE INDEX [IX_VitalSigns_RecordedAt] ON [VitalSigns] ([RecordedAt]);
GO

-- FamilyMembers
CREATE TABLE [FamilyMembers] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [PatientId] NVARCHAR(450) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Relationship] NVARCHAR(50) NOT NULL,
    [Age] INT NOT NULL,
    [Gender] NVARCHAR(10) NOT NULL,
    [ContactNumber] NVARCHAR(20) NULL,
    [MedicalConditions] NVARCHAR(MAX) NULL,
    FOREIGN KEY ([PatientId]) REFERENCES [Patients]([UserId]) ON DELETE NO ACTION
);
CREATE INDEX [IX_FamilyMembers_PatientId] ON [FamilyMembers] ([PatientId]);
GO

-- MedicalHistories
CREATE TABLE [MedicalHistories] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [PatientId] NVARCHAR(450) NOT NULL,
    [Condition] NVARCHAR(MAX) NOT NULL,
    [DiagnosedDate] DATETIME2 NULL,
    [Treatment] NVARCHAR(MAX) NULL,
    [Status] NVARCHAR(50) NULL,
    [Notes] NVARCHAR(MAX) NULL,
    FOREIGN KEY ([PatientId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_MedicalHistories_PatientId] ON [MedicalHistories] ([PatientId]);
GO

-- LabResults
CREATE TABLE [LabResults] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [PatientId] NVARCHAR(450) NOT NULL,
    [TestName] NVARCHAR(MAX) NOT NULL,
    [Result] NVARCHAR(MAX) NULL,
    [TestDate] DATETIME2 NOT NULL,
    [Notes] NVARCHAR(MAX) NULL,
    FOREIGN KEY ([PatientId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_LabResults_PatientId] ON [LabResults] ([PatientId]);
CREATE INDEX [IX_LabResults_TestDate] ON [LabResults] ([TestDate]);
GO

-- PatientHistories
CREATE TABLE [PatientHistories] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [PatientId] NVARCHAR(450) NOT NULL,
    [Action] NVARCHAR(MAX) NOT NULL,
    [Details] NVARCHAR(MAX) NULL,
    [PerformedBy] NVARCHAR(MAX) NOT NULL,
    [PerformedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY ([PatientId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_PatientHistories_PatientId] ON [PatientHistories] ([PatientId]);
CREATE INDEX [IX_PatientHistories_PerformedAt] ON [PatientHistories] ([PerformedAt]);
GO
