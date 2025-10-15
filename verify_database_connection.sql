-- Verify database connection and check AspNetUsers table structure
USE [Barangay];
GO

PRINT '=== DATABASE CONNECTION VERIFIED ===';
PRINT 'Connected to database: Barangay';
PRINT '';

-- Check if AspNetUsers table exists
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetUsers')
BEGIN
    PRINT '✓ AspNetUsers table exists';
    PRINT '';
    
    -- Check for the 4 required columns
    PRINT '=== Checking Required Columns ===';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'Age')
        PRINT '✓ Age column EXISTS'
    ELSE
        PRINT '✗ Age column MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'HasChangedPassword')
        PRINT '✓ HasChangedPassword column EXISTS'
    ELSE
        PRINT '✗ HasChangedPassword column MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'IsFirstLogin')
        PRINT '✓ IsFirstLogin column EXISTS'
    ELSE
        PRINT '✗ IsFirstLogin column MISSING';
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'LastPasswordChangeDate')
        PRINT '✓ LastPasswordChangeDate column EXISTS'
    ELSE
        PRINT '✗ LastPasswordChangeDate column MISSING';
    
    PRINT '';
    
    -- Count existing users
    DECLARE @UserCount INT;
    SELECT @UserCount = COUNT(*) FROM [AspNetUsers];
    PRINT 'Total users in database: ' + CAST(@UserCount AS NVARCHAR(10));
    
    -- Check for admin account
    IF EXISTS (SELECT 1 FROM [AspNetUsers] WHERE Email = 'admin@bhcare.com')
        PRINT '✓ Admin account EXISTS (admin@bhcare.com)'
    ELSE
        PRINT '✗ Admin account MISSING';
    
    PRINT '';
    PRINT '=== All Users ===';
    SELECT 
        UserName AS Email,
        Name,
        Status,
        IsActive,
        EmailConfirmed,
        CreatedAt
    FROM [AspNetUsers]
    ORDER BY CreatedAt DESC;
END
ELSE
BEGIN
    PRINT '✗ AspNetUsers table does NOT exist!';
END
GO
