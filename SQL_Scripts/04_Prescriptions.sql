-- ============================================
-- BH CARE - PART 4: PRESCRIPTIONS & MEDICATIONS
-- ============================================

USE [Barangay]
GO

-- Prescriptions
CREATE TABLE [Prescriptions] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [PatientId] NVARCHAR(450) NOT NULL,
    [DoctorId] NVARCHAR(450) NOT NULL,
    [PrescriptionDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [Notes] NVARCHAR(MAX) NULL,
    [Status] NVARCHAR(50) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    FOREIGN KEY ([PatientId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION,
    FOREIGN KEY ([DoctorId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_Prescriptions_PatientId] ON [Prescriptions] ([PatientId]);
CREATE INDEX [IX_Prescriptions_DoctorId] ON [Prescriptions] ([DoctorId]);
CREATE INDEX [IX_Prescriptions_PrescriptionDate] ON [Prescriptions] ([PrescriptionDate]);
GO

-- Medications
CREATE TABLE [Medications] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(MAX) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [StockQuantity] INT NOT NULL DEFAULT 0,
    [Unit] NVARCHAR(50) NULL
);
GO

-- PrescriptionMedications
CREATE TABLE [PrescriptionMedications] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [PrescriptionId] INT NOT NULL,
    [MedicalRecordId] INT NULL,
    [MedicationName] NVARCHAR(MAX) NOT NULL,
    [Dosage] NVARCHAR(MAX) NOT NULL,
    [Frequency] NVARCHAR(MAX) NULL,
    [Duration] NVARCHAR(MAX) NULL,
    [Instructions] NVARCHAR(MAX) NULL,
    [Quantity] INT NULL,
    FOREIGN KEY ([PrescriptionId]) REFERENCES [Prescriptions]([Id]) ON DELETE CASCADE,
    FOREIGN KEY ([MedicalRecordId]) REFERENCES [MedicalRecords]([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_PrescriptionMedications_PrescriptionId] ON [PrescriptionMedications] ([PrescriptionId]);
CREATE INDEX [IX_PrescriptionMedications_MedicalRecordId] ON [PrescriptionMedications] ([MedicalRecordId]);
GO
