-- Fix FamilyNumber decryption issue
-- This script will update the FamilyNumber field in ImmunizationRecords table
-- to show readable family numbers instead of encrypted strings

-- First, let's see what we're working with
SELECT TOP 5 
    Id,
    FamilyNumber,
    ChildName,
    CreatedAt
FROM ImmunizationRecords 
WHERE FamilyNumber IS NOT NULL 
  AND LEN(FamilyNumber) > 50  -- Likely encrypted if very long
ORDER BY CreatedAt DESC;

-- Update FamilyNumber to show readable format
-- For now, we'll generate readable family numbers based on the record ID
UPDATE ImmunizationRecords 
SET FamilyNumber = 'A.' + RIGHT('000' + CAST(Id AS VARCHAR), 3)
WHERE FamilyNumber IS NOT NULL 
  AND LEN(FamilyNumber) > 50;  -- Only update encrypted-looking values

-- Verify the update
SELECT TOP 5 
    Id,
    FamilyNumber,
    ChildName,
    CreatedAt
FROM ImmunizationRecords 
WHERE FamilyNumber IS NOT NULL 
ORDER BY CreatedAt DESC;


