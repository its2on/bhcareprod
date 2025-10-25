-- Script to identify encrypted FamilyNo values in the database
-- Run this to see which records have encrypted family numbers

-- Check HEEADSSSAssessments
SELECT 
    Id,
    UserId,
    FamilyNo,
    CASE 
        WHEN FamilyNo LIKE '%==%' OR LEN(FamilyNo) > 50 THEN 'Likely Encrypted'
        WHEN FamilyNo LIKE '%-%' AND LEN(FamilyNo) < 20 THEN 'Plain Text'
        ELSE 'Unknown'
    END AS Status,
    CreatedAt
FROM HEEADSSSAssessments
WHERE FamilyNo IS NOT NULL
ORDER BY CreatedAt DESC;

-- Check NCDRiskAssessments
SELECT 
    Id,
    UserId,
    FamilyNo,
    CASE 
        WHEN FamilyNo LIKE '%==%' OR LEN(FamilyNo) > 50 THEN 'Likely Encrypted'
        WHEN FamilyNo LIKE '%-%' AND LEN(FamilyNo) < 20 THEN 'Plain Text'
        ELSE 'Unknown'
    END AS Status,
    CreatedAt
FROM NCDRiskAssessments
WHERE FamilyNo IS NOT NULL
ORDER BY CreatedAt DESC;

-- OPTION 1: Delete all encrypted family numbers (they can regenerate)
-- UNCOMMENT ONLY IF YOU WANT TO DELETE ENCRYPTED DATA
-- UPDATE HEEADSSSAssessments SET FamilyNo = NULL WHERE FamilyNo LIKE '%==%' OR LEN(FamilyNo) > 50;
-- UPDATE NCDRiskAssessments SET FamilyNo = NULL WHERE FamilyNo LIKE '%==%' OR LEN(FamilyNo) > 50;

-- OPTION 2: Replace encrypted values with a default pattern based on user's last name
-- This requires manual intervention or a C# migration script
