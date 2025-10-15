-- ============================================
-- BH CARE - PART 7: IMMUNIZATION & FAMILY RECORDS
-- ============================================

USE [Barangay]
GO

-- ImmunizationRecords
CREATE TABLE [ImmunizationRecords] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [PatientId] NVARCHAR(450) NOT NULL,
    [VaccineName] NVARCHAR(MAX) NOT NULL,
    [DateAdministered] DATETIME2 NOT NULL,
    [DoseNumber] INT NULL,
    [AdministeredBy] NVARCHAR(MAX) NULL,
    [NextDueDate] DATETIME2 NULL,
    [Notes] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY ([PatientId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_ImmunizationRecords_PatientId] ON [ImmunizationRecords] ([PatientId]);
CREATE INDEX [IX_ImmunizationRecords_DateAdministered] ON [ImmunizationRecords] ([DateAdministered]);
GO

-- ImmunizationShortcutForms
CREATE TABLE [ImmunizationShortcutForms] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [PatientId] NVARCHAR(450) NOT NULL,
    [FormData] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    FOREIGN KEY ([PatientId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_ImmunizationShortcutForms_PatientId] ON [ImmunizationShortcutForms] ([PatientId]);
GO

-- FamilyRecords
CREATE TABLE [FamilyRecords] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [FamilyNumber] NVARCHAR(MAX) NOT NULL,
    [HeadOfFamily] NVARCHAR(MAX) NOT NULL,
    [Address] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL
);
GO

-- FamilyNumberCounters
CREATE TABLE [FamilyNumberCounters] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Year] INT NOT NULL,
    [LastNumber] INT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL
);
CREATE UNIQUE INDEX [IX_FamilyNumberCounters_Year] ON [FamilyNumberCounters] ([Year]);
GO

-- GuardianInformation
CREATE TABLE [GuardianInformation] (
    [GuardianId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [GuardianName] NVARCHAR(MAX) NULL,
    [Relationship] NVARCHAR(MAX) NULL,
    [ContactNumber] NVARCHAR(MAX) NULL,
    [Address] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_GuardianInformation_UserId] ON [GuardianInformation] ([UserId]);
GO
