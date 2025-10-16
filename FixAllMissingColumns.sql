-- Comprehensive fix for ALL missing database columns
-- This script fixes both NCDRiskAssessments and HEEADSSSAssessments tables

USE [bhcareDB]; -- Replace with your actual database name
GO

PRINT '=== STARTING COMPREHENSIVE DATABASE COLUMN FIX ===';
PRINT '';

-- ========================================
-- FIX NCDRiskAssessments TABLE
-- ========================================
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'NCDRiskAssessments')
BEGIN
    PRINT '1. NCDRiskAssessments table found. Adding missing columns...';
    
    -- Original missing columns
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'Pananakit21')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD Pananakit21 NVARCHAR(4000) NULL;
        PRINT '   ✓ Added Pananakit21 column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'Pananakit22')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD Pananakit22 NVARCHAR(4000) NULL;
        PRINT '   ✓ Added Pananakit22 column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'Pananakit23')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD Pananakit23 NVARCHAR(4000) NULL;
        PRINT '   ✓ Added Pananakit23 column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'Pananakit24')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD Pananakit24 NVARCHAR(4000) NULL;
        PRINT '   ✓ Added Pananakit24 column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'Pananakit25')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD Pananakit25 NVARCHAR(4000) NULL;
        PRINT '   ✓ Added Pananakit25 column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'Pananakit26')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD Pananakit26 NVARCHAR(4000) NULL;
        PRINT '   ✓ Added Pananakit26 column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'Pananakit27')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD Pananakit27 NVARCHAR(4000) NULL;
        PRINT '   ✓ Added Pananakit27 column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'Pananakit28')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD Pananakit28 NVARCHAR(4000) NULL;
        PRINT '   ✓ Added Pananakit28 column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'AlcoholInom')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD AlcoholInom NVARCHAR(4000) NULL;
        PRINT '   ✓ Added AlcoholInom column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'StressMadalas')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD StressMadalas NVARCHAR(4000) NULL;
        PRINT '   ✓ Added StressMadalas column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'StressSino')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD StressSino NVARCHAR(4000) NULL;
        PRINT '   ✓ Added StressSino column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'StressEpekto')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD StressEpekto NVARCHAR(4000) NULL;
        PRINT '   ✓ Added StressEpekto column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'HealthFacilityName')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD HealthFacilityName NVARCHAR(4000) NULL;
        PRINT '   ✓ Added HealthFacilityName column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'DateAssessment')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD DateAssessment NVARCHAR(4000) NULL;
        PRINT '   ✓ Added DateAssessment column';
    END
    
    -- Additional missing columns from latest error
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'AlcoholStoppedDuration')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD AlcoholStoppedDuration NVARCHAR(4000) NULL;
        PRINT '   ✓ Added AlcoholStoppedDuration column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'EyeDiseaseMedication')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD EyeDiseaseMedication NVARCHAR(4000) NULL;
        PRINT '   ✓ Added EyeDiseaseMedication column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'EyeDiseaseYear')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD EyeDiseaseYear NVARCHAR(4000) NULL;
        PRINT '   ✓ Added EyeDiseaseYear column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'FamilyHistoryEyeDiseaseFather')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD FamilyHistoryEyeDiseaseFather NVARCHAR(4000) NULL;
        PRINT '   ✓ Added FamilyHistoryEyeDiseaseFather column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'FamilyHistoryEyeDiseaseMother')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD FamilyHistoryEyeDiseaseMother NVARCHAR(4000) NULL;
        PRINT '   ✓ Added FamilyHistoryEyeDiseaseMother column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'FamilyHistoryEyeDiseaseSibling')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD FamilyHistoryEyeDiseaseSibling NVARCHAR(4000) NULL;
        PRINT '   ✓ Added FamilyHistoryEyeDiseaseSibling column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'FamilyHistoryKidneyDiseaseFather')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD FamilyHistoryKidneyDiseaseFather NVARCHAR(4000) NULL;
        PRINT '   ✓ Added FamilyHistoryKidneyDiseaseFather column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'FamilyHistoryKidneyDiseaseMother')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD FamilyHistoryKidneyDiseaseMother NVARCHAR(4000) NULL;
        PRINT '   ✓ Added FamilyHistoryKidneyDiseaseMother column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'FamilyHistoryKidneyDiseaseSibling')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD FamilyHistoryKidneyDiseaseSibling NVARCHAR(4000) NULL;
        PRINT '   ✓ Added FamilyHistoryKidneyDiseaseSibling column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'HasEnoughExercise')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD HasEnoughExercise NVARCHAR(4000) NULL;
        PRINT '   ✓ Added HasEnoughExercise column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'IDNo')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD IDNo NVARCHAR(4000) NULL;
        PRINT '   ✓ Added IDNo column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'Smoked100Sticks')
    BEGIN
        ALTER TABLE NCDRiskAssessments ADD Smoked100Sticks NVARCHAR(4000) NULL;
        PRINT '   ✓ Added Smoked100Sticks column';
    END
    
    PRINT '   ✅ NCDRiskAssessments table updated successfully!';
END
ELSE
BEGIN
    PRINT '   ❌ ERROR: NCDRiskAssessments table not found!';
END

PRINT '';

-- ========================================
-- FIX HEEADSSSAssessments TABLE
-- ========================================
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'HEEADSSSAssessments')
BEGIN
    PRINT '2. HEEADSSSAssessments table found. Adding missing sexuality columns...';
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityBodyConcerns')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityBodyConcerns NVARCHAR(4000) NULL;
        PRINT '   ✓ Added SexualityBodyConcerns column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityHealthConcerns')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityHealthConcerns NVARCHAR(4000) NULL;
        PRINT '   ✓ Added SexualityHealthConcerns column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityPartnersCount')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityPartnersCount NVARCHAR(4000) NULL;
        PRINT '   ✓ Added SexualityPartnersCount column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityIntimateRelationships')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityIntimateRelationships NVARCHAR(4000) NULL;
        PRINT '   ✓ Added SexualityIntimateRelationships column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityPartners')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityPartners NVARCHAR(4000) NULL;
        PRINT '   ✓ Added SexualityPartners column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualitySexualOrientation')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualitySexualOrientation NVARCHAR(4000) NULL;
        PRINT '   ✓ Added SexualitySexualOrientation column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityPregnancy')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityPregnancy NVARCHAR(4000) NULL;
        PRINT '   ✓ Added SexualityPregnancy column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualitySTI')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualitySTI NVARCHAR(4000) NULL;
        PRINT '   ✓ Added SexualitySTI column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityProtection')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityProtection NVARCHAR(4000) NULL;
        PRINT '   ✓ Added SexualityProtection column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityPregnancyExperience')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityPregnancyExperience NVARCHAR(4000) NULL;
        PRINT '   ✓ Added SexualityPregnancyExperience column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualitySTIExperience')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualitySTIExperience NVARCHAR(4000) NULL;
        PRINT '   ✓ Added SexualitySTIExperience column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityProtectionUse')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityProtectionUse NVARCHAR(4000) NULL;
        PRINT '   ✓ Added SexualityProtectionUse column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityHarassment')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityHarassment NVARCHAR(4000) NULL;
        PRINT '   ✓ Added SexualityHarassment column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityGay')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityGay NVARCHAR(4000) NULL;
        PRINT '   ✓ Added SexualityGay column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityLesbian')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityLesbian NVARCHAR(4000) NULL;
        PRINT '   ✓ Added SexualityLesbian column';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HEEADSSSAssessments]') AND name = 'SexualityBisexual')
    BEGIN
        ALTER TABLE HEEADSSSAssessments ADD SexualityBisexual NVARCHAR(4000) NULL;
        PRINT '   ✓ Added SexualityBisexual column';
    END
    
    PRINT '   ✅ HEEADSSSAssessments table updated successfully!';
END
ELSE
BEGIN
    PRINT '   ❌ ERROR: HEEADSSSAssessments table not found!';
END

PRINT '';
PRINT '=== DATABASE COLUMN FIX COMPLETED ===';
PRINT '';
PRINT '🎯 Next Steps:';
PRINT '1. Restart your application: dotnet clean && dotnet build && dotnet run';
PRINT '2. Test the forms to ensure they work properly';
PRINT '3. Check for any remaining column errors';
PRINT '';

GO
