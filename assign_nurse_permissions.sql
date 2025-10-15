USE [Barangay];
GO

SET QUOTED_IDENTIFIER ON;
GO

DECLARE @NurseUserId NVARCHAR(450) = '512259E6-1093-4C25-AD19-ECA7E4E5099F';

PRINT 'Assigning essential Nurse permissions...';
PRINT '';

-- Get or create essential nurse permissions
DECLARE @PermissionIds TABLE (Id INT, Name NVARCHAR(200));

-- Ensure essential nurse permissions exist
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'NurseDashboard')
    INSERT INTO Permissions (Name, Description, Category) VALUES ('NurseDashboard', 'Access to Nurse Dashboard page', 'Nurse Pages');

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'PatientList')
    INSERT INTO Permissions (Name, Description, Category) VALUES ('PatientList', 'Access to Patient List page', 'Nurse Pages');

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'Appointments')
    INSERT INTO Permissions (Name, Description, Category) VALUES ('Appointments', 'Access to Appointments page', 'Nurse Pages');

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'VitalSigns')
    INSERT INTO Permissions (Name, Description, Category) VALUES ('VitalSigns', 'Access to Vital Signs page', 'Nurse Pages');

IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'PatientQueue')
    INSERT INTO Permissions (Name, Description, Category) VALUES ('PatientQueue', 'Access to Patient Queue page', 'Nurse Pages');

-- Get permission IDs
INSERT INTO @PermissionIds (Id, Name)
SELECT Id, Name FROM Permissions 
WHERE Name IN ('NurseDashboard', 'PatientList', 'Appointments', 'VitalSigns', 'PatientQueue');

-- Assign permissions to nurse user
INSERT INTO UserPermissions (UserId, PermissionId)
SELECT @NurseUserId, Id 
FROM @PermissionIds
WHERE NOT EXISTS (
    SELECT 1 FROM UserPermissions 
    WHERE UserId = @NurseUserId AND PermissionId = Id
);

PRINT '✓ Assigned permissions to nurse account';
PRINT '';

-- Show assigned permissions
SELECT p.Name, p.Description, p.Category
FROM UserPermissions up
INNER JOIN Permissions p ON up.PermissionId = p.Id
WHERE up.UserId = @NurseUserId
ORDER BY p.Category, p.Name;

GO
