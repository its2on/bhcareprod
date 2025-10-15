USE [Barangay];
GO

SET QUOTED_IDENTIFIER ON;
GO

-- Add missing columns to StaffMembers table
PRINT 'Adding missing columns to StaffMembers table...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('StaffMembers') AND name = 'CreatedAt')
BEGIN
    ALTER TABLE StaffMembers ADD CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE();
    PRINT '✓ Added CreatedAt column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('StaffMembers') AND name = 'JoinDate')
BEGIN
    ALTER TABLE StaffMembers ADD JoinDate DATETIME2 NOT NULL DEFAULT GETDATE();
    PRINT '✓ Added JoinDate column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('StaffMembers') AND name = 'LicenseNumber')
BEGIN
    ALTER TABLE StaffMembers ADD LicenseNumber NVARCHAR(100) NULL;
    PRINT '✓ Added LicenseNumber column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('StaffMembers') AND name = 'MaxDailyPatients')
BEGIN
    ALTER TABLE StaffMembers ADD MaxDailyPatients INT NOT NULL DEFAULT 20;
    PRINT '✓ Added MaxDailyPatients column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('StaffMembers') AND name = 'Role')
BEGIN
    ALTER TABLE StaffMembers ADD Role NVARCHAR(50) NOT NULL DEFAULT 'Staff';
    PRINT '✓ Added Role column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('StaffMembers') AND name = 'Specialization')
BEGIN
    ALTER TABLE StaffMembers ADD Specialization NVARCHAR(200) NULL;
    PRINT '✓ Added Specialization column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('StaffMembers') AND name = 'WorkingDays')
BEGIN
    ALTER TABLE StaffMembers ADD WorkingDays NVARCHAR(200) NULL;
    PRINT '✓ Added WorkingDays column';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('StaffMembers') AND name = 'WorkingHours')
BEGIN
    ALTER TABLE StaffMembers ADD WorkingHours NVARCHAR(100) NULL;
    PRINT '✓ Added WorkingHours column';
END

PRINT '';
PRINT '✓ All StaffMembers columns added successfully!';
PRINT '';

-- Show current columns
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'StaffMembers' 
ORDER BY ORDINAL_POSITION;

GO
