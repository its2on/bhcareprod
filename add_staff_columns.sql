-- Add new columns to StaffMembers table
-- Run this script manually in Azure SQL Database

-- Check if columns exist before adding them
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[StaffMembers]') AND name = 'FirstName')
BEGIN
    ALTER TABLE [dbo].[StaffMembers] ADD [FirstName] nvarchar(max) NOT NULL DEFAULT '';
    PRINT 'Added FirstName column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[StaffMembers]') AND name = 'MiddleName')
BEGIN
    ALTER TABLE [dbo].[StaffMembers] ADD [MiddleName] nvarchar(max) NULL;
    PRINT 'Added MiddleName column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[StaffMembers]') AND name = 'LastName')
BEGIN
    -- Check if Name column exists to rename it
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[StaffMembers]') AND name = 'Name')
    BEGIN
        EXEC sp_rename 'StaffMembers.Name', 'LastName', 'COLUMN';
        PRINT 'Renamed Name to LastName';
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[StaffMembers] ADD [LastName] nvarchar(max) NOT NULL DEFAULT '';
        PRINT 'Added LastName column';
    END
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[StaffMembers]') AND name = 'Gender')
BEGIN
    ALTER TABLE [dbo].[StaffMembers] ADD [Gender] nvarchar(max) NOT NULL DEFAULT '';
    PRINT 'Added Gender column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[StaffMembers]') AND name = 'DateOfBirth')
BEGIN
    ALTER TABLE [dbo].[StaffMembers] ADD [DateOfBirth] datetime2 NOT NULL DEFAULT '1990-01-01';
    PRINT 'Added DateOfBirth column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[StaffMembers]') AND name = 'Address')
BEGIN
    ALTER TABLE [dbo].[StaffMembers] ADD [Address] nvarchar(max) NOT NULL DEFAULT '';
    PRINT 'Added Address column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[StaffMembers]') AND name = 'CivilStatus')
BEGIN
    ALTER TABLE [dbo].[StaffMembers] ADD [CivilStatus] nvarchar(max) NOT NULL DEFAULT '';
    PRINT 'Added CivilStatus column';
END

-- Update existing records to populate FirstName and LastName from old Name column if it exists
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[StaffMembers]') AND name = 'LastName')
BEGIN
    UPDATE [dbo].[StaffMembers]
    SET 
        [FirstName] = CASE 
            WHEN CHARINDEX(' ', [LastName]) > 0 
            THEN LEFT([LastName], CHARINDEX(' ', [LastName]) - 1)
            ELSE [LastName]
        END,
        [LastName] = CASE 
            WHEN CHARINDEX(' ', [LastName]) > 0 
            THEN SUBSTRING([LastName], CHARINDEX(' ', [LastName]) + 1, LEN([LastName]))
            ELSE [LastName]
        END
    WHERE [FirstName] = '' OR [FirstName] IS NULL;
    
    PRINT 'Updated existing records with split names';
END

PRINT 'All columns added successfully!';
