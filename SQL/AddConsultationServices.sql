-- =============================================
-- Create ConsultationServices Table
-- Author: System
-- Date: November 8, 2024
-- Description: Adds dynamic service management for consultation types
-- =============================================

-- Create ConsultationServices table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ConsultationServices]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ConsultationServices] (
        [ServiceId] INT IDENTITY(1,1) NOT NULL,
        [ServiceName] NVARCHAR(100) NOT NULL,
        [ServiceKey] NVARCHAR(50) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [IconClass] NVARCHAR(100) NULL,
        [ColorTheme] NVARCHAR(20) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [DisplayOrder] INT NOT NULL DEFAULT 0,
        [RequiresAgeBasedAssessment] BIT NOT NULL DEFAULT 0,
        [Category] NVARCHAR(100) NULL,
        [MinAge] INT NULL,
        [MaxAge] INT NULL,
        [AllowsWalkIn] BIT NOT NULL DEFAULT 1,
        [AverageDurationMinutes] INT NULL,
        [SpecialInstructions] NVARCHAR(1000) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(450) NULL,
        [UpdatedBy] NVARCHAR(450) NULL,
        CONSTRAINT [PK_ConsultationServices] PRIMARY KEY CLUSTERED ([ServiceId] ASC),
        CONSTRAINT [UK_ConsultationServices_ServiceKey] UNIQUE ([ServiceKey])
    );
    PRINT 'ConsultationServices table created successfully.';
END
ELSE
BEGIN
    PRINT 'ConsultationServices table already exists.';
END
GO

-- Add ServiceId column to FormTemplates table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[FormTemplates]') AND name = 'ServiceId')
BEGIN
    ALTER TABLE [dbo].[FormTemplates]
    ADD [ServiceId] INT NULL;
    PRINT 'ServiceId column added to FormTemplates table.';
END
ELSE
BEGIN
    PRINT 'ServiceId column already exists in FormTemplates table.';
END
GO

-- Add foreign key constraint for FormTemplates.ServiceId
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_FormTemplates_ConsultationServices_ServiceId]'))
BEGIN
    ALTER TABLE [dbo].[FormTemplates]
    ADD CONSTRAINT [FK_FormTemplates_ConsultationServices_ServiceId]
    FOREIGN KEY ([ServiceId]) REFERENCES [dbo].[ConsultationServices]([ServiceId])
    ON DELETE SET NULL;
    PRINT 'Foreign key constraint added for FormTemplates.ServiceId.';
END
ELSE
BEGIN
    PRINT 'Foreign key constraint already exists for FormTemplates.ServiceId.';
END
GO

-- Add ServiceId column to Appointments table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Appointments]') AND name = 'ServiceId')
BEGIN
    ALTER TABLE [dbo].[Appointments]
    ADD [ServiceId] INT NULL;
    PRINT 'ServiceId column added to Appointments table.';
END
ELSE
BEGIN
    PRINT 'ServiceId column already exists in Appointments table.';
END
GO

-- Add foreign key constraint for Appointments.ServiceId
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Appointments_ConsultationServices_ServiceId]'))
BEGIN
    ALTER TABLE [dbo].[Appointments]
    ADD CONSTRAINT [FK_Appointments_ConsultationServices_ServiceId]
    FOREIGN KEY ([ServiceId]) REFERENCES [dbo].[ConsultationServices]([ServiceId])
    ON DELETE SET NULL;
    PRINT 'Foreign key constraint added for Appointments.ServiceId.';
END
ELSE
BEGIN
    PRINT 'Foreign key constraint already exists for Appointments.ServiceId.';
END
GO

-- Seed default consultation services
-- Only insert if the table is empty
IF NOT EXISTS (SELECT 1 FROM [dbo].[ConsultationServices])
BEGIN
    INSERT INTO [dbo].[ConsultationServices] 
    ([ServiceName], [ServiceKey], [Description], [IconClass], [ColorTheme], [IsActive], [DisplayOrder], [RequiresAgeBasedAssessment], [Category], [AllowsWalkIn], [AverageDurationMinutes], [CreatedAt])
    VALUES
    ('General Consult', 'general-consult', 'Comprehensive health check-up and consultation', 'fa-solid fa-stethoscope', '#fd7e14', 1, 1, 1, 'Clinical', 1, 30, GETUTCDATE()),
    ('Dental', 'dental', 'Dental check-up and treatment', 'fa-solid fa-tooth', '#20c997', 1, 2, 0, 'Specialized', 1, 45, GETUTCDATE()),
    ('Immunization', 'immunization', 'Vaccination and immunization services', 'fa-solid fa-syringe', '#0d6efd', 1, 3, 0, 'Preventive', 1, 15, GETUTCDATE()),
    ('Prenatal & Family Planning', 'prenatal', 'Prenatal care and family planning services', 'fa-solid fa-baby', '#d63384', 1, 4, 0, 'Maternal', 1, 30, GETUTCDATE()),
    ('DOTS Consult', 'dots', 'Directly Observed Treatment Short-course for TB', 'fa-solid fa-lungs', '#6f42c1', 1, 5, 0, 'Clinical', 1, 20, GETUTCDATE());
    
    PRINT 'Default consultation services seeded successfully.';
END
ELSE
BEGIN
    PRINT 'ConsultationServices table already contains data. Skipping seed.';
END
GO

-- Update existing appointments to link to "General Consult" service (for backward compatibility)
-- This ensures existing appointments without ServiceId are mapped to General Consult
IF EXISTS (SELECT 1 FROM [dbo].[Appointments] WHERE [ServiceId] IS NULL)
BEGIN
    DECLARE @GeneralConsultId INT;
    SELECT @GeneralConsultId = [ServiceId] FROM [dbo].[ConsultationServices] WHERE [ServiceKey] = 'general-consult';
    
    IF @GeneralConsultId IS NOT NULL
    BEGIN
        UPDATE [dbo].[Appointments]
        SET [ServiceId] = @GeneralConsultId
        WHERE [ServiceId] IS NULL;
        
        PRINT 'Existing appointments updated with General Consult service.';
    END
END
GO

-- Create index on ServiceId for better query performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Appointments]') AND name = 'IX_Appointments_ServiceId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Appointments_ServiceId]
    ON [dbo].[Appointments] ([ServiceId]);
    PRINT 'Index created on Appointments.ServiceId.';
END
ELSE
BEGIN
    PRINT 'Index already exists on Appointments.ServiceId.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[FormTemplates]') AND name = 'IX_FormTemplates_ServiceId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_FormTemplates_ServiceId]
    ON [dbo].[FormTemplates] ([ServiceId]);
    PRINT 'Index created on FormTemplates.ServiceId.';
END
ELSE
BEGIN
    PRINT 'Index already exists on FormTemplates.ServiceId.';
END
GO

PRINT '==============================================';
PRINT 'ConsultationServices migration completed successfully!';
PRINT '==============================================';
