USE [Barangay];
GO

PRINT '=== Adding Missing Columns Safely ===';
GO

-- Add Age column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'Age')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [Age] NVARCHAR(MAX) NULL;
    PRINT '✓ Added Age column';
END
ELSE
BEGIN
    PRINT '✓ Age column already exists';
END
GO

-- Add HasChangedPassword column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'HasChangedPassword')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [HasChangedPassword] BIT NOT NULL DEFAULT 0;
    PRINT '✓ Added HasChangedPassword column';
END
ELSE
BEGIN
    PRINT '✓ HasChangedPassword column already exists';
END
GO

-- Add IsFirstLogin column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'IsFirstLogin')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [IsFirstLogin] BIT NOT NULL DEFAULT 0;
    PRINT '✓ Added IsFirstLogin column';
END
ELSE
BEGIN
    PRINT '✓ IsFirstLogin column already exists';
END
GO

-- Add LastPasswordChangeDate column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'LastPasswordChangeDate')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers] ADD [LastPasswordChangeDate] DATETIME2 NULL;
    PRINT '✓ Added LastPasswordChangeDate column';
END
ELSE
BEGIN
    PRINT '✓ LastPasswordChangeDate column already exists';
END
GO

PRINT '';
PRINT '=== All columns verified! ===';
GO
