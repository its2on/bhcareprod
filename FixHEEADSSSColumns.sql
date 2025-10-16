-- Fix HEEADSSS Assessment Missing Sexuality Columns
-- Run this script to add missing sexuality-related columns

USE [bhcareDB]; -- Replace with your actual database name
GO

-- Check if the table exists first
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'HEEADSSSAssessments')
BEGIN
    PRINT 'HEEADSSSAssessments table found. Adding missing sexuality columns...';
    
    -- Add missing sexuality columns
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityBodyConcerns')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityBodyConcerns NVARCHAR(4000) NULL;
        PRINT 'Added SexualityBodyConcerns column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityHealthConcerns')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityHealthConcerns NVARCHAR(4000) NULL;
        PRINT 'Added SexualityHealthConcerns column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityPartnersCount')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityPartnersCount NVARCHAR(4000) NULL;
        PRINT 'Added SexualityPartnersCount column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityIntimateRelationships')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityIntimateRelationships NVARCHAR(4000) NULL;
        PRINT 'Added SexualityIntimateRelationships column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityPartners')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityPartners NVARCHAR(4000) NULL;
        PRINT 'Added SexualityPartners column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualitySexualOrientation')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualitySexualOrientation NVARCHAR(4000) NULL;
        PRINT 'Added SexualitySexualOrientation column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityPregnancy')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityPregnancy NVARCHAR(4000) NULL;
        PRINT 'Added SexualityPregnancy column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualitySTI')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualitySTI NVARCHAR(4000) NULL;
        PRINT 'Added SexualitySTI column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityProtection')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityProtection NVARCHAR(4000) NULL;
        PRINT 'Added SexualityProtection column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityPregnancyExperience')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityPregnancyExperience NVARCHAR(4000) NULL;
        PRINT 'Added SexualityPregnancyExperience column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualitySTIExperience')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualitySTIExperience NVARCHAR(4000) NULL;
        PRINT 'Added SexualitySTIExperience column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityProtectionUse')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityProtectionUse NVARCHAR(4000) NULL;
        PRINT 'Added SexualityProtectionUse column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityHarassment')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityHarassment NVARCHAR(4000) NULL;
        PRINT 'Added SexualityHarassment column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityGay')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityGay NVARCHAR(4000) NULL;
        PRINT 'Added SexualityGay column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityLesbian')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityLesbian NVARCHAR(4000) NULL;
        PRINT 'Added SexualityLesbian column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityBisexual')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityBisexual NVARCHAR(4000) NULL;
        PRINT 'Added SexualityBisexual column';
    END
    
    PRINT 'HEEADSSS Assessment sexuality columns have been added successfully!';
END
ELSE
BEGIN
    PRINT 'ERROR: HEEADSSSAssessments table not found!';
END

GO

-- Verify the columns were added
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'HEEADSSSAssessments')
BEGIN
    PRINT 'Verifying added columns...';
    
    SELECT 
        COLUMN_NAME,
        DATA_TYPE,
        IS_NULLABLE
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'HEEADSSSAssessments' 
    AND COLUMN_NAME IN (
        'SexualityBodyConcerns',
        'SexualityHealthConcerns', 
        'SexualityPartnersCount',
        'SexualityIntimateRelationships',
        'SexualityPartners',
        'SexualitySexualOrientation',
        'SexualityPregnancy',
        'SexualitySTI',
        'SexualityProtection',
        'SexualityPregnancyExperience',
        'SexualitySTIExperience',
        'SexualityProtectionUse',
        'SexualityHarassment',
        'SexualityGay',
        'SexualityLesbian',
        'SexualityBisexual'
    )
    ORDER BY COLUMN_NAME;
    
    PRINT 'Column verification complete.';
END
