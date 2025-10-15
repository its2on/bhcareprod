-- SQL Script to find users by specific criteria
SET QUOTED_IDENTIFIER ON
GO
USE [bhcareDB]
GO

PRINT 'Searching for users with specific criteria...';

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

-- Search for users with REJECTED status
PRINT 'Searching for REJECTED users:';
SELECT Id, UserNumber, Email, Name, FullName, Status, PhoneNumber, BirthDate, CreatedAt
FROM AspNetUsers
WHERE Status = 'Rejected';

-- Search for users with specific UserNumber patterns
PRINT 'Searching for UserNumber 731153:';
SELECT Id, UserNumber, Email, Name, FullName, Status, PhoneNumber, BirthDate, CreatedAt
FROM AspNetUsers
WHERE UserNumber = 731153;

PRINT 'Searching for UserNumber 5087:';
SELECT Id, UserNumber, Email, Name, FullName, Status, PhoneNumber, BirthDate, CreatedAt
FROM AspNetUsers
WHERE UserNumber = 5087;

-- Show all users with UserNumber > 0 to see the pattern
PRINT 'All users with UserNumber > 0:';
SELECT Id, UserNumber, Email, Name, FullName, Status, PhoneNumber, BirthDate, CreatedAt
FROM AspNetUsers
WHERE UserNumber > 0
ORDER BY UserNumber;

PRINT 'Search completed.';

