-- Add columns to StaffMembers table
-- Execute this in Azure Portal Query Editor or SQL Server Management Studio

-- Add FirstName if it doesn't exist
IF COL_LENGTH('dbo.StaffMembers', 'FirstName') IS NULL
BEGIN
    ALTER TABLE [dbo].[StaffMembers] ADD [FirstName] nvarchar(max) NOT NULL DEFAULT '';
    PRINT 'Added FirstName';
END
ELSE
    PRINT 'FirstName already exists';

-- Add MiddleName if it doesn't exist
IF COL_LENGTH('dbo.StaffMembers', 'MiddleName') IS NULL
BEGIN
    ALTER TABLE [dbo].[StaffMembers] ADD [MiddleName] nvarchar(max) NULL;
    PRINT 'Added MiddleName';
END
ELSE
    PRINT 'MiddleName already exists';

-- Add Gender if it doesn't exist
IF COL_LENGTH('dbo.StaffMembers', 'Gender') IS NULL
BEGIN
    ALTER TABLE [dbo].[StaffMembers] ADD [Gender] nvarchar(max) NOT NULL DEFAULT '';
    PRINT 'Added Gender';
END
ELSE
    PRINT 'Gender already exists';

-- Add DateOfBirth if it doesn't exist
IF COL_LENGTH('dbo.StaffMembers', 'DateOfBirth') IS NULL
BEGIN
    ALTER TABLE [dbo].[StaffMembers] ADD [DateOfBirth] datetime2 NOT NULL DEFAULT '1990-01-01';
    PRINT 'Added DateOfBirth';
END
ELSE
    PRINT 'DateOfBirth already exists';

-- Add Address if it doesn't exist
IF COL_LENGTH('dbo.StaffMembers', 'Address') IS NULL
BEGIN
    ALTER TABLE [dbo].[StaffMembers] ADD [Address] nvarchar(max) NOT NULL DEFAULT '';
    PRINT 'Added Address';
END
ELSE
    PRINT 'Address already exists';

-- Add CivilStatus if it doesn't exist
IF COL_LENGTH('dbo.StaffMembers', 'CivilStatus') IS NULL
BEGIN
    ALTER TABLE [dbo].[StaffMembers] ADD [CivilStatus] nvarchar(max) NOT NULL DEFAULT '';
    PRINT 'Added CivilStatus';
END
ELSE
    PRINT 'CivilStatus already exists';

-- Handle Name to LastName rename
IF COL_LENGTH('dbo.StaffMembers', 'Name') IS NOT NULL
BEGIN
    -- Name column exists, rename it to LastName
    EXEC sp_rename 'dbo.StaffMembers.Name', 'LastName', 'COLUMN';
    PRINT 'Renamed Name to LastName';
END
ELSE IF COL_LENGTH('dbo.StaffMembers', 'LastName') IS NULL
BEGIN
    -- Neither Name nor LastName exists, create LastName
    ALTER TABLE [dbo].[StaffMembers] ADD [LastName] nvarchar(max) NOT NULL DEFAULT '';
    PRINT 'Added LastName';
END
ELSE
    PRINT 'LastName already exists';

PRINT 'All columns processed successfully!';
