-- ============================================
-- BH CARE - PART 10: REPORTS & MISCELLANEOUS
-- ============================================

USE [Barangay]
GO

-- HealthReports
CREATE TABLE [HealthReports] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [DoctorId] NVARCHAR(450) NOT NULL,
    [ReportType] NVARCHAR(MAX) NULL,
    [ReportData] NVARCHAR(MAX) NULL,
    [GeneratedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [Notes] NVARCHAR(MAX) NULL,
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION,
    FOREIGN KEY ([DoctorId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_HealthReports_UserId] ON [HealthReports] ([UserId]);
CREATE INDEX [IX_HealthReports_DoctorId] ON [HealthReports] ([DoctorId]);
CREATE INDEX [IX_HealthReports_GeneratedAt] ON [HealthReports] ([GeneratedAt]);
GO

PRINT 'Part 10: Reports & Miscellaneous tables created successfully.'
GO
