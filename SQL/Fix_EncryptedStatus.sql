-- Fix existing users who have Status = 'Active' but EncryptedStatus is still 'Pending'
-- This causes login failures because Login.cshtml.cs checks BOTH Status and EncryptedStatus

-- Preview affected users before fixing
SELECT 
    Id,
    Email,
    FirstName,
    LastName,
    Status,
    EncryptedStatus,
    VerificationStatus,
    IsApproved,
    IsActive
FROM AspNetUsers
WHERE Status = 'Active' 
  AND (EncryptedStatus IS NULL OR EncryptedStatus = 'Pending');

-- Fix: Update EncryptedStatus to match Status
UPDATE AspNetUsers
SET EncryptedStatus = Status
WHERE Status = 'Active' 
  AND (EncryptedStatus IS NULL OR EncryptedStatus = 'Pending');

-- Preview affected users with Status = 'Verified'
SELECT 
    Id,
    Email,
    FirstName,
    LastName,
    Status,
    EncryptedStatus,
    VerificationStatus,
    IsApproved,
    IsActive
FROM AspNetUsers
WHERE Status = 'Verified' 
  AND (EncryptedStatus IS NULL OR EncryptedStatus = 'Pending');

-- Fix: Update EncryptedStatus to match Status for Verified users
UPDATE AspNetUsers
SET EncryptedStatus = Status
WHERE Status = 'Verified' 
  AND (EncryptedStatus IS NULL OR EncryptedStatus = 'Pending');

-- Verify the fix
SELECT 
    Id,
    Email,
    FirstName,
    LastName,
    Status,
    EncryptedStatus,
    VerificationStatus,
    IsApproved,
    IsActive
FROM AspNetUsers
WHERE Status IN ('Active', 'Verified')
ORDER BY CreatedAt DESC;
