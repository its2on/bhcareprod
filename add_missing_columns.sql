-- Add missing columns and create admin account
USE [Barangay];
GO

SET QUOTED_IDENTIFIER ON;
GO

-- Add ALL missing columns based on the error log

-- Appointments table columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'AppointmentTime')
    ALTER TABLE Appointments ADD AppointmentTime NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'AppointmentTimeInput')
    ALTER TABLE Appointments ADD AppointmentTimeInput NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'ReasonForVisit')
    ALTER TABLE Appointments ADD ReasonForVisit NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'PatientName')
    ALTER TABLE Appointments ADD PatientName NVARCHAR(256) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'ContactNumber')
    ALTER TABLE Appointments ADD ContactNumber NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'Address')
    ALTER TABLE Appointments ADD Address NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'DateOfBirth')
    ALTER TABLE Appointments ADD DateOfBirth DATETIME2 NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'Gender')
    ALTER TABLE Appointments ADD Gender NVARCHAR(20) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'AgeValue')
    ALTER TABLE Appointments ADD AgeValue INT NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'EmergencyContact')
    ALTER TABLE Appointments ADD EmergencyContact NVARCHAR(256) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'EmergencyContactNumber')
    ALTER TABLE Appointments ADD EmergencyContactNumber NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'MedicalHistory')
    ALTER TABLE Appointments ADD MedicalHistory NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'CurrentMedications')
    ALTER TABLE Appointments ADD CurrentMedications NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'Allergies')
    ALTER TABLE Appointments ADD Allergies NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'DependentFullName')
    ALTER TABLE Appointments ADD DependentFullName NVARCHAR(256) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'DependentAge')
    ALTER TABLE Appointments ADD DependentAge INT NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'RelationshipToDependent')
    ALTER TABLE Appointments ADD RelationshipToDependent NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'ApplicationUserId')
    ALTER TABLE Appointments ADD ApplicationUserId NVARCHAR(450) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Appointments') AND name = 'PatientUserId')
    ALTER TABLE Appointments ADD PatientUserId NVARCHAR(450) NULL;

-- MedicalRecords table columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('MedicalRecords') AND name = 'Prescription')
    ALTER TABLE MedicalRecords ADD Prescription NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('MedicalRecords') AND name = 'Instructions')
    ALTER TABLE MedicalRecords ADD Instructions NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('MedicalRecords') AND name = 'Description')
    ALTER TABLE MedicalRecords ADD Description NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('MedicalRecords') AND name = 'AttachmentPath')
    ALTER TABLE MedicalRecords ADD AttachmentPath NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('MedicalRecords') AND name = 'AttachmentsData')
    ALTER TABLE MedicalRecords ADD AttachmentsData NVARCHAR(MAX) NULL;

-- ErrorLogs table columns (if table exists)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ErrorLogs')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ErrorLogs') AND name = 'OriginalUrl')
        ALTER TABLE ErrorLogs ADD OriginalUrl NVARCHAR(500) NULL;
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ErrorLogs') AND name = 'UserAgent')
        ALTER TABLE ErrorLogs ADD UserAgent NVARCHAR(500) NULL;
END

PRINT '✓ All missing columns added successfully';
GO

-- Add Age column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'Age')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [Age] NVARCHAR(MAX) NULL;
    PRINT 'Added Age column';
END
ELSE
BEGIN
    PRINT 'Age column already exists';
END
GO

-- Add HasChangedPassword column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'HasChangedPassword')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [HasChangedPassword] BIT NOT NULL DEFAULT 0;
    PRINT 'Added HasChangedPassword column';
END
ELSE
BEGIN
    PRINT 'HasChangedPassword column already exists';
END
GO

-- Add IsFirstLogin column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'IsFirstLogin')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [IsFirstLogin] BIT NOT NULL DEFAULT 0;
    PRINT 'Added IsFirstLogin column';
END
ELSE
BEGIN
    PRINT 'IsFirstLogin column already exists';
END
GO

-- Add LastPasswordChangeDate column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'LastPasswordChangeDate')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [LastPasswordChangeDate] DATETIME2 NULL;
    PRINT 'Added LastPasswordChangeDate column';
END
ELSE
BEGIN
    PRINT 'LastPasswordChangeDate column already exists';
END
GO

PRINT '=== Step 2: Creating admin account ===';
GO

-- Create admin account with proper password hash
IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Email] = 'admin@bhcare.com')
BEGIN
    DECLARE @UserId NVARCHAR(450) = NEWID();

    INSERT INTO [AspNetUsers] (
        [Id],[UserName],[NormalizedUserName],[Email],[NormalizedEmail],[EmailConfirmed],
        [PasswordHash],[SecurityStamp],[ConcurrencyStamp],[PhoneNumberConfirmed],
        [TwoFactorEnabled],[LockoutEnabled],[AccessFailedCount],[UserNumber],
        [Name],[Status],[IsActive],[MaxDailyPatients],[CreatedAt],[UpdatedAt],
        [LastActive],[JoinDate],[UserType],[HasAgreedToTerms],[AgreedAt],
        [IsFirstLogin],[HasChangedPassword],[AppointmentReminders],[PrescriptionAlerts],
        [HealthTips],[FirstName],[LastName],[Specialization],[WorkingDays],[WorkingHours],
        [Gender],[Age],[Address],[Barangay],[ProfilePicture],[PhilHealthId],[MiddleName],
        [EncryptedStatus],[EncryptedFullName],[ProfileImage]
    )
    VALUES (
        @UserId,'admin@bhcare.com','ADMIN@BHCARE.COM','admin@bhcare.com','ADMIN@BHCARE.COM',1,
        'AQAAAAEAACcQAAAAEJ89jl0VWrcaFZOWCe9gpkkzzr5OXJCSm+bAybabRH+My6QvVTMBGaHoP7IftIH3uw==',
        NEWID(),NEWID(),0,0,1,0,1,
        'System Administrator','Active',1,20,GETDATE(),GETDATE(),
        GETDATE(),GETDATE(),3,1,GETDATE(),
        0,1,1,1,0,'System','Administrator','','','',
        '','','','','','','','','',''
    );

    -- Assign Admin Staff role
    INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
    SELECT @UserId, [Id] FROM [AspNetRoles] WHERE [Name] = 'Admin Staff';

    PRINT 'Admin account created successfully!';
    PRINT 'Username: admin@bhcare.com';
    PRINT 'Password: Admin123!';
END
ELSE
BEGIN
    PRINT 'Admin account already exists';
END
GO
