USE [Barangay];
GO

PRINT 'Adding missing columns to AspNetUsers table...';
GO

-- Add Age column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'Age')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [Age] NVARCHAR(MAX) NULL;
    PRINT '✓ Added Age column';
END
ELSE
    PRINT '✓ Age already exists';
GO

-- Add HasChangedPassword column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'HasChangedPassword')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [HasChangedPassword] BIT NOT NULL DEFAULT 0;
    PRINT '✓ Added HasChangedPassword column';
END
ELSE
    PRINT '✓ HasChangedPassword already exists';
GO

-- Add IsFirstLogin column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'IsFirstLogin')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [IsFirstLogin] BIT NOT NULL DEFAULT 0;
    PRINT '✓ Added IsFirstLogin column';
END
ELSE
    PRINT '✓ IsFirstLogin already exists';
GO

-- Add LastPasswordChangeDate column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'LastPasswordChangeDate')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [LastPasswordChangeDate] DATETIME2 NULL;
    PRINT '✓ Added LastPasswordChangeDate column';
END
ELSE
    PRINT '✓ LastPasswordChangeDate already exists';
GO

PRINT '';
PRINT '=== VERIFICATION ===';
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AspNetUsers' 
AND COLUMN_NAME IN ('Age', 'HasChangedPassword', 'IsFirstLogin', 'LastPasswordChangeDate')
ORDER BY COLUMN_NAME;

PRINT '';
PRINT '=== COMPLETE ===';
GO
