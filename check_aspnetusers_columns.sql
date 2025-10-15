USE [Barangay];
GO

PRINT '=== AspNetUsers Table Structure ===';
PRINT '';

-- Show all columns in AspNetUsers table
SELECT 
    COLUMN_NAME AS ColumnName,
    DATA_TYPE AS DataType,
    CHARACTER_MAXIMUM_LENGTH AS MaxLength,
    IS_NULLABLE AS IsNullable,
    COLUMN_DEFAULT AS DefaultValue
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AspNetUsers'
ORDER BY ORDINAL_POSITION;

PRINT '';
PRINT '=== Checking Required Columns ===';
PRINT '';

-- Check specifically for the 4 required columns
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
PRINT '=== User Count ===';
SELECT COUNT(*) AS TotalUsers FROM AspNetUsers;

PRINT '';
PRINT '=== Sample Users ===';
SELECT TOP 5 
    UserName,
    Email,
    Name,
    Status,
    IsActive,
    EmailConfirmed
FROM AspNetUsers
ORDER BY CreatedAt DESC;
GO
