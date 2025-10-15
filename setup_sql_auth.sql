-- =============================================
-- SQL Server Authentication Setup for BH Care
-- =============================================

USE [master];
GO

-- Enable SQL Server Authentication (Mixed Mode)
-- Note: You may need to restart SQL Server after enabling this
EXEC xp_instance_regwrite 
    N'HKEY_LOCAL_MACHINE', 
    N'Software\Microsoft\MSSQLServer\MSSQLServer', 
    N'LoginMode', 
    REG_DWORD, 
    2;
GO

PRINT '✓ SQL Server Authentication enabled (Mixed Mode)';
PRINT 'NOTE: You must restart SQL Server for this to take effect!';
PRINT '';
GO

-- Create SQL Login for BH Care application
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'bhcare_app')
BEGIN
    CREATE LOGIN [bhcare_app] WITH PASSWORD = 'BHCare@2024!Secure', 
        DEFAULT_DATABASE = [Barangay],
        CHECK_EXPIRATION = OFF,
        CHECK_POLICY = OFF;
    PRINT '✓ Created SQL Login: bhcare_app';
END
ELSE
BEGIN
    PRINT '✓ SQL Login already exists: bhcare_app';
END
GO

-- Switch to Barangay database
USE [Barangay];
GO

-- Create database user for the login
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'bhcare_app')
BEGIN
    CREATE USER [bhcare_app] FOR LOGIN [bhcare_app];
    PRINT '✓ Created database user: bhcare_app';
END
ELSE
BEGIN
    PRINT '✓ Database user already exists: bhcare_app';
END
GO

-- Grant necessary permissions
ALTER ROLE [db_datareader] ADD MEMBER [bhcare_app];
ALTER ROLE [db_datawriter] ADD MEMBER [bhcare_app];
ALTER ROLE [db_ddladmin] ADD MEMBER [bhcare_app];
GO

PRINT '✓ Granted permissions to bhcare_app';
PRINT '';
PRINT '=== SQL Authentication Setup Complete ===';
PRINT '';
PRINT 'Connection String:';
PRINT 'Server=DESKTOP-NU53VS3\SQLEXPRESS;Database=Barangay;User Id=bhcare_app;Password=BHCare@2024!Secure;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False;';
PRINT '';
PRINT 'IMPORTANT: Restart SQL Server service for Mixed Mode authentication to work!';
PRINT 'Run in PowerShell (as Administrator):';
PRINT '  Restart-Service MSSQL$SQLEXPRESS';
GO
