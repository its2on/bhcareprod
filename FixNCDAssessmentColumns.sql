-- Fix NCD Risk Assessment Missing Columns
-- Run this script to add any missing columns that may cause "Invalid column name" errors

USE [Barangay]; -- Replace with your actual database name
GO

-- Check and add missing columns to NCDRiskAssessments table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'AlcoholStoppedDuration')
BEGIN
    ALTER TABLE NCDRiskAssessments ADD AlcoholStoppedDuration NVARCHAR(4000) NULL;
    PRINT 'Added AlcoholStoppedDuration column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'EyeDiseaseMedication')
BEGIN
    ALTER TABLE NCDRiskAssessments ADD EyeDiseaseMedication NVARCHAR(200) NULL;
    PRINT 'Added EyeDiseaseMedication column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'EyeDiseaseYear')
BEGIN
    ALTER TABLE NCDRiskAssessments ADD EyeDiseaseYear NVARCHAR(50) NULL;
    PRINT 'Added EyeDiseaseYear column';
END

-- Add chest pain assessment columns (Pananakit 2.1-2.8)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'Pananakit21')
BEGIN
    ALTER TABLE NCDRiskAssessments ADD Pananakit21 NVARCHAR(4000) NULL;
    PRINT 'Added Pananakit21 column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'Pananakit22')
BEGIN
    ALTER TABLE NCDRiskAssessments ADD Pananakit22 NVARCHAR(4000) NULL;
    PRINT 'Added Pananakit22 column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'Pananakit23')
BEGIN
    ALTER TABLE NCDRiskAssessments ADD Pananakit23 NVARCHAR(4000) NULL;
    PRINT 'Added Pananakit23 column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'Pananakit24')
BEGIN
    ALTER TABLE NCDRiskAssessments ADD Pananakit24 NVARCHAR(4000) NULL;
    PRINT 'Added Pananakit24 column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'Pananakit25')
BEGIN
    ALTER TABLE NCDRiskAssessments ADD Pananakit25 NVARCHAR(4000) NULL;
    PRINT 'Added Pananakit25 column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'Pananakit26')
BEGIN
    ALTER TABLE NCDRiskAssessments ADD Pananakit26 NVARCHAR(4000) NULL;
    PRINT 'Added Pananakit26 column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'Pananakit27')
BEGIN
    ALTER TABLE NCDRiskAssessments ADD Pananakit27 NVARCHAR(4000) NULL;
    PRINT 'Added Pananakit27 column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'Pananakit28')
BEGIN
    ALTER TABLE NCDRiskAssessments ADD Pananakit28 NVARCHAR(4000) NULL;
    PRINT 'Added Pananakit28 column';
END

-- Add alcohol assessment columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'AlcoholInom')
BEGIN
    ALTER TABLE NCDRiskAssessments ADD AlcoholInom NVARCHAR(4000) NULL;
    PRINT 'Added AlcoholInom column';
END

-- Add stress assessment columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'StressMadalas')
BEGIN
    ALTER TABLE NCDRiskAssessments ADD StressMadalas NVARCHAR(4000) NULL;
    PRINT 'Added StressMadalas column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'StressSino')
BEGIN
    ALTER TABLE NCDRiskAssessments ADD StressSino NVARCHAR(4000) NULL;
    PRINT 'Added StressSino column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'StressEpekto')
BEGIN
    ALTER TABLE NCDRiskAssessments ADD StressEpekto NVARCHAR(4000) NULL;
    PRINT 'Added StressEpekto column';
END

-- Add nutrition assessment columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'NutrisyonMadalasGulay')
BEGIN
    ALTER TABLE NCDRiskAssessments ADD NutrisyonMadalasGulay NVARCHAR(4000) NULL;
    PRINT 'Added NutrisyonMadalasGulay column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'NutrisyonMadalasPratas')
BEGIN
    ALTER TABLE NCDRiskAssessments ADD NutrisyonMadalasPratas NVARCHAR(4000) NULL;
    PRINT 'Added NutrisyonMadalasPratas column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'NutrisyonMadalasIsda')
BEGIN
    ALTER TABLE NCDRiskAssessments ADD NutrisyonMadalasIsda NVARCHAR(4000) NULL;
    PRINT 'Added NutrisyonMadalasIsda column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'NutrisyonMadalasKarne')
BEGIN
    ALTER TABLE NCDRiskAssessments ADD NutrisyonMadalasKarne NVARCHAR(4000) NULL;
    PRINT 'Added NutrisyonMadalasKarne column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'NutrisyonKumakainMatatamis')
BEGIN
    ALTER TABLE NCDRiskAssessments ADD NutrisyonKumakainMatatamis NVARCHAR(4000) NULL;
    PRINT 'Added NutrisyonKumakainMatatamis column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[NCDRiskAssessments]') AND name = 'NutrisyonKumakainMamantika')
BEGIN
    ALTER TABLE NCDRiskAssessments ADD NutrisyonKumakainMamantika NVARCHAR(4000) NULL;
    PRINT 'Added NutrisyonKumakainMamantika column';
END

PRINT 'NCD Risk Assessment table column fixes completed successfully!';
PRINT 'All missing columns have been added to support the assessment form.';
