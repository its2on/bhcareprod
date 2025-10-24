-- Data Migration: Copy Family Numbers from ApplicationUser to Patient
-- Created: 2025-10-24
-- Purpose: Sync existing family numbers to Patient table after adding FamilyNumber column

-- This script copies family numbers from AspNetUsers (ApplicationUser) to Patients table
-- for users who already have family numbers but their Patient records don't have them yet

BEGIN TRANSACTION;

PRINT '========================================';
PRINT 'Family Number Data Migration';
PRINT 'Date: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '========================================';
PRINT '';

-- Check how many records need updating
DECLARE @RecordsToUpdate INT;
SELECT @RecordsToUpdate = COUNT(*)
FROM Patients P
INNER JOIN AspNetUsers U ON P.UserId = U.Id
WHERE U.FamilyNumber IS NOT NULL 
  AND U.FamilyNumber != ''
  AND (P.FamilyNumber IS NULL OR P.FamilyNumber = '');

PRINT 'Records to update: ' + CAST(@RecordsToUpdate AS VARCHAR(10));
PRINT '';

IF @RecordsToUpdate > 0
BEGIN
    -- Show sample of what will be updated
    PRINT 'Sample of records to be updated:';
    PRINT '----------------------------------------';
    SELECT TOP 10
        P.UserId,
        P.FullName as CurrentPatientName,
        P.FamilyNumber as CurrentPatientFamilyNumber,
        U.FamilyNumber as UserFamilyNumber,
        'Will update to: ' + U.FamilyNumber as Action
    FROM Patients P
    INNER JOIN AspNetUsers U ON P.UserId = U.Id
    WHERE U.FamilyNumber IS NOT NULL 
      AND U.FamilyNumber != ''
      AND (P.FamilyNumber IS NULL OR P.FamilyNumber = '');
    
    PRINT '';
    PRINT 'Updating Patient records with family numbers...';
    
    -- Perform the update
    UPDATE P
    SET P.FamilyNumber = U.FamilyNumber,
        P.UpdatedAt = GETUTCDATE()
    FROM Patients P
    INNER JOIN AspNetUsers U ON P.UserId = U.Id
    WHERE U.FamilyNumber IS NOT NULL 
      AND U.FamilyNumber != ''
      AND (P.FamilyNumber IS NULL OR P.FamilyNumber = '');
    
    PRINT 'Updated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' patient records';
    PRINT '';
    
    -- Verify the update
    PRINT 'Verification - Patients with family numbers:';
    SELECT COUNT(*) as TotalPatientsWithFamilyNumbers
    FROM Patients
    WHERE FamilyNumber IS NOT NULL AND FamilyNumber != '';
    
    PRINT '';
    PRINT 'Family number distribution:';
    SELECT 
        FamilyNumber,
        COUNT(*) as PatientCount
    FROM Patients
    WHERE FamilyNumber IS NOT NULL AND FamilyNumber != ''
    GROUP BY FamilyNumber
    ORDER BY FamilyNumber;
    
    PRINT '';
    PRINT '✅ Migration completed successfully!';
END
ELSE
BEGIN
    PRINT '✅ No records need updating. All family numbers are already synced.';
END

PRINT '';
PRINT '========================================';

-- Uncomment the line below to commit the transaction
-- COMMIT TRANSACTION;

-- For safety, the transaction is not committed by default
-- Review the output above, and if everything looks correct:
-- 1. Uncomment the COMMIT TRANSACTION line above
-- 2. Comment out or remove the ROLLBACK TRANSACTION line below
-- 3. Run the script again

ROLLBACK TRANSACTION;
PRINT '⚠️  TRANSACTION ROLLED BACK (for safety)';
PRINT '⚠️  To apply changes:';
PRINT '   1. Review the output above';
PRINT '   2. Uncomment COMMIT TRANSACTION';
PRINT '   3. Comment out ROLLBACK TRANSACTION';
PRINT '   4. Run script again';
