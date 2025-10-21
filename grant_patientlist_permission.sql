USE [Barangay];
GO

-- You need to replace this with your actual nurse user ID
-- Check your AspNetUsers table to find the correct ID
DECLARE @NurseUserId NVARCHAR(450) = 'YOUR_NURSE_USER_ID_HERE';

PRINT 'Granting PatientList permission to nurse...';

-- Ensure PatientList permission exists
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'PatientList')
    INSERT INTO Permissions (Name, Description, Category) VALUES ('PatientList', 'Access to Patient List pages', 'Nurse Pages');

-- Get the permission ID
DECLARE @PermissionId INT = (SELECT TOP 1 Id FROM Permissions WHERE Name = 'PatientList');

-- Grant permission to the nurse user
IF NOT EXISTS (SELECT 1 FROM UserPermissions WHERE UserId = @NurseUserId AND PermissionId = @PermissionId)
BEGIN
    INSERT INTO UserPermissions (UserId, PermissionId) VALUES (@NurseUserId, @PermissionId);
    PRINT '✓ PatientList permission granted to nurse';
END
ELSE
BEGIN
    PRINT '! PatientList permission already exists for this user';
END

-- Show all permissions for this user
SELECT u.UserName, p.Name as Permission, p.Description
FROM UserPermissions up
INNER JOIN AspNetUsers u ON up.UserId = u.Id  
INNER JOIN Permissions p ON up.PermissionId = p.Id
WHERE up.UserId = @NurseUserId
ORDER BY p.Name;

GO
