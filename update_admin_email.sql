-- Update admin user email in AspNetUsers table
-- This script updates the email for the admin user

-- Update the UserName and NormalizedUserName fields
UPDATE AspNetUsers
SET 
    Email = 'healthcenterbaesa@gmail.com',
    NormalizedEmail = 'HEALTHCENTERBAESA@GMAIL.COM',
    UserName = 'healthcenterbaesa@gmail.com',
    NormalizedUserName = 'HEALTHCENTERBAESA@GMAIL.COM'
WHERE 
    Email = 'admin@bhcare.com' OR Email = 'admin@example.com'
    OR UserName = 'admin@bhcare.com' OR UserName = 'admin@example.com';

-- Output the updated records
SELECT Id, UserName, Email, NormalizedUserName, NormalizedEmail 
FROM AspNetUsers 
WHERE Email = 'healthcenterbaesa@gmail.com' OR UserName = 'healthcenterbaesa@gmail.com';
