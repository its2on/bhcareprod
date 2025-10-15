USE [Barangay];
GO

SET QUOTED_IDENTIFIER ON;
GO

-- Check if nurse has a role assigned
SELECT u.Id, u.Email, u.UserName, r.Name as RoleName 
FROM AspNetUsers u 
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId 
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id 
WHERE u.Email = 'nurse@bhcare.com';

-- If no role, assign Nurse role
DECLARE @NurseUserId NVARCHAR(450) = (SELECT Id FROM AspNetUsers WHERE Email = 'nurse@bhcare.com');
DECLARE @NurseRoleId NVARCHAR(450) = (SELECT Id FROM AspNetRoles WHERE Name = 'Nurse');

IF @NurseUserId IS NOT NULL AND @NurseRoleId IS NOT NULL
BEGIN
    -- Check if role assignment already exists
    IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @NurseUserId AND RoleId = @NurseRoleId)
    BEGIN
        INSERT INTO AspNetUserRoles (UserId, RoleId)
        VALUES (@NurseUserId, @NurseRoleId);
        
        PRINT '✓ Nurse role assigned to nurse@bhcare.com';
    END
    ELSE
    BEGIN
        PRINT '✓ Nurse role already assigned';
    END
END
ELSE
BEGIN
    PRINT '❌ Could not find user or role';
END

-- Verify the assignment
SELECT u.Id, u.Email, u.UserName, r.Name as RoleName 
FROM AspNetUsers u 
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId 
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id 
WHERE u.Email = 'nurse@bhcare.com';

GO
