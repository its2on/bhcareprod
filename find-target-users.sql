-- SQL Script to find the specific duplicate Remy Budbod Martin accounts
SET QUOTED_IDENTIFIER ON
GO
USE [bhcareDB]
GO

PRINT 'Searching for duplicate Remy Budbod Martin accounts...';

-- Search by UserNumber patterns (BHC-731153 and BHC-05087f)
-- The UserNumber might be stored as 731153 and 5087
PRINT 'Searching for UserNumber 731153:';
SELECT Id, UserNumber, Email, Name, FullName, Status, PhoneNumber, BirthDate, CreatedAt
FROM AspNetUsers
WHERE UserNumber = 731153;

PRINT 'Searching for UserNumber 5087:';
SELECT Id, UserNumber, Email, Name, FullName, Status, PhoneNumber, BirthDate, CreatedAt
FROM AspNetUsers
WHERE UserNumber = 5087;

-- Search for users with phone number 09393020275
PRINT 'Searching for phone number 09393020275:';
SELECT Id, UserNumber, Email, Name, FullName, Status, PhoneNumber, BirthDate, CreatedAt
FROM AspNetUsers
WHERE PhoneNumber = '09393020275';

-- Search for users with birth date 2004-02-02
PRINT 'Searching for birth date 2004-02-02:';
SELECT Id, UserNumber, Email, Name, FullName, Status, PhoneNumber, BirthDate, CreatedAt
FROM AspNetUsers
WHERE BirthDate = '2004-02-02';

-- Search for users created on 2025-10-07
PRINT 'Searching for users created on 2025-10-07:';
SELECT Id, UserNumber, Email, Name, FullName, Status, PhoneNumber, BirthDate, CreatedAt
FROM AspNetUsers
WHERE CAST(CreatedAt AS DATE) = '2025-10-07'
ORDER BY CreatedAt;

-- Search for users with VERIFIED and REJECTED status
PRINT 'Searching for VERIFIED users:';
SELECT Id, UserNumber, Email, Name, FullName, Status, PhoneNumber, BirthDate, CreatedAt
FROM AspNetUsers
WHERE Status = 'Verified';

PRINT 'Searching for REJECTED users:';
SELECT Id, UserNumber, Email, Name, FullName, Status, PhoneNumber, BirthDate, CreatedAt
FROM AspNetUsers
WHERE Status = 'Rejected';

-- Show all users to help identify the accounts
PRINT 'All users (last 20):';
SELECT TOP 20 Id, UserNumber, Email, Name, FullName, Status, PhoneNumber, BirthDate, CreatedAt
FROM AspNetUsers
ORDER BY CreatedAt DESC;

PRINT 'Search completed.';

