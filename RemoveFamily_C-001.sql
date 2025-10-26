-- SQL script to remove Family C-001 from the database
-- IMPORTANT: Create a backup before running this script

-- Start a transaction so we can roll back if needed
BEGIN TRANSACTION;

-- Display records to be affected (for verification)
SELECT 'ApplicationUsers to update:' AS Operation;
SELECT Id, Email, FullName, FamilyNumber 
FROM AspNetUsers 
WHERE FamilyNumber = 'C-001';

SELECT 'Patients to update:' AS Operation;
SELECT UserId, FullName, FamilyNumber 
FROM Patients 
WHERE FamilyNumber = 'C-001';

SELECT 'Family members to delete:' AS Operation;
SELECT * FROM FamilyMembers 
WHERE FamilyNumber = 'C-001';

-- Update ApplicationUser records
UPDATE AspNetUsers
SET FamilyNumber = NULL,
    UpdatedAt = GETUTCDATE()
WHERE FamilyNumber = 'C-001';

-- Update Patient records 
UPDATE Patients
SET FamilyNumber = NULL,
    UpdatedAt = GETUTCDATE()
WHERE FamilyNumber = 'C-001';

-- Delete any family member records
DELETE FROM FamilyMembers
WHERE FamilyNumber = 'C-001';

-- Update any NCDRiskAssessment records
UPDATE NCDRiskAssessments
SET FamilyNo = NULL,
    UpdatedAt = GETUTCDATE()
WHERE FamilyNo = 'C-001';

-- Update any HEEADSSSAssessment records
UPDATE HEEADSSSAssessments
SET FamilyNo = NULL,
    UpdatedAt = GETUTCDATE()
WHERE FamilyNo = 'C-001';

-- Update any appointment records
UPDATE Appointments
SET FamilyNumber = NULL,
    UpdatedAt = GETUTCDATE()
WHERE FamilyNumber = 'C-001';

-- Verify records were updated/deleted
SELECT 'Verification - Users after update:' AS Operation;
SELECT Id, Email, FullName, FamilyNumber 
FROM AspNetUsers 
WHERE FamilyNumber IS NULL AND Id IN 
  (SELECT Id FROM AspNetUsers WHERE FamilyNumber = 'C-001' BEFORE UPDATE);

SELECT 'Verification - Family members after deletion:' AS Operation;
SELECT COUNT(*) AS RemainingMembers 
FROM FamilyMembers 
WHERE FamilyNumber = 'C-001';

-- If everything looks correct, commit the transaction
-- COMMIT TRANSACTION;

-- If something went wrong, uncomment the next line to roll back
-- ROLLBACK TRANSACTION;

-- NOTE: By default, this script ends with the transaction uncommitted
-- Review the results and then manually run either COMMIT or ROLLBACK
