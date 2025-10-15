-- ============================================
-- BH CARE - PART 3: APPOINTMENTS
-- ============================================

USE [Barangay]
GO

-- Appointments
CREATE TABLE [Appointments] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [PatientId] NVARCHAR(450) NOT NULL,
    [DoctorId] NVARCHAR(450) NULL,
    [AppointmentDate] DATETIME2 NOT NULL,
    [TimeSlot] NVARCHAR(MAX) NULL,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    [Reason] NVARCHAR(MAX) NULL,
    [Notes] NVARCHAR(MAX) NULL,
    [Type] NVARCHAR(50) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    [CancelledAt] DATETIME2 NULL,
    [CancellationReason] NVARCHAR(MAX) NULL,
    [CompletedAt] DATETIME2 NULL,
    FOREIGN KEY ([PatientId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION,
    FOREIGN KEY ([DoctorId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_Appointments_PatientId] ON [Appointments] ([PatientId]);
CREATE INDEX [IX_Appointments_DoctorId] ON [Appointments] ([DoctorId]);
CREATE INDEX [IX_Appointments_AppointmentDate] ON [Appointments] ([AppointmentDate]);
CREATE INDEX [IX_Appointments_Status] ON [Appointments] ([Status]);
GO

-- AppointmentAttachments
CREATE TABLE [AppointmentAttachments] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [AppointmentId] INT NOT NULL,
    [FileName] NVARCHAR(MAX) NOT NULL,
    [FilePath] NVARCHAR(MAX) NOT NULL,
    [FileType] NVARCHAR(50) NULL,
    [UploadedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments]([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_AppointmentAttachments_AppointmentId] ON [AppointmentAttachments] ([AppointmentId]);
GO

-- AppointmentFiles
CREATE TABLE [AppointmentFiles] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [AppointmentId] INT NOT NULL,
    [FileName] NVARCHAR(MAX) NOT NULL,
    [FilePath] NVARCHAR(MAX) NOT NULL,
    [UploadedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments]([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_AppointmentFiles_AppointmentId] ON [AppointmentFiles] ([AppointmentId]);
GO

-- ConsultationTimeSlots
CREATE TABLE [ConsultationTimeSlots] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [DoctorId] NVARCHAR(450) NULL,
    [Date] DATETIME2 NOT NULL,
    [StartTime] TIME NOT NULL,
    [EndTime] TIME NOT NULL,
    [IsAvailable] BIT NOT NULL DEFAULT 1,
    [MaxPatients] INT NOT NULL DEFAULT 1,
    [CurrentPatients] INT NOT NULL DEFAULT 0
);
CREATE INDEX [IX_ConsultationTimeSlots_DoctorId] ON [ConsultationTimeSlots] ([DoctorId]);
CREATE INDEX [IX_ConsultationTimeSlots_Date] ON [ConsultationTimeSlots] ([Date]);
GO

-- DoctorAvailabilities
CREATE TABLE [DoctorAvailabilities] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [DoctorId] NVARCHAR(450) NOT NULL,
    [DayOfWeek] INT NOT NULL,
    [StartTime] TIME NOT NULL,
    [EndTime] TIME NOT NULL,
    [IsAvailable] BIT NOT NULL DEFAULT 1,
    [MaxPatients] INT NOT NULL DEFAULT 20,
    FOREIGN KEY ([DoctorId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_DoctorAvailabilities_DoctorId] ON [DoctorAvailabilities] ([DoctorId]);
CREATE INDEX [IX_DoctorAvailabilities_DayOfWeek] ON [DoctorAvailabilities] ([DayOfWeek]);
GO
