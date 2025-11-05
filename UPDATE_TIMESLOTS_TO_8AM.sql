-- ============================================================================
-- Update Doctor Availability Time Slots to 8:00 AM - 5:00 PM
-- Run this script directly on your Azure SQL Database
-- ============================================================================

-- Step 1: Check current values before updating
SELECT 
    Id,
    DoctorId,
    CAST(StartTime AS TIME) as CurrentStartTime,
    CAST(EndTime AS TIME) as CurrentEndTime,
    MaxAppointmentsPerDay as CurrentMaxSlots,
    SlotDurationMinutes as CurrentSlotDuration,
    LastUpdated
FROM DoctorAvailabilities
ORDER BY LastUpdated DESC;

GO

-- Step 2: Update all doctor availability records
-- New schedule: 8:00 AM to 5:00 PM (540 minutes) with 100 slots (5.4 min per slot)
UPDATE DoctorAvailabilities 
SET 
    StartTime = '08:00:00',              -- 8:00 AM
    EndTime = '17:00:00',                -- 5:00 PM
    MaxAppointmentsPerDay = 100,         -- 100 appointment slots
    SlotDurationMinutes = 5,             -- ~5 minutes per slot (540 min / 100 slots = 5.4)
    LastUpdated = GETUTCDATE()           -- Update timestamp
WHERE 
    StartTime != '08:00:00'              -- Only update if not already 8:00 AM
    OR EndTime != '17:00:00'             -- Or if end time is different
    OR MaxAppointmentsPerDay != 100      -- Or if slots are different
    OR SlotDurationMinutes != 5;         -- Or if slot duration is different

GO

-- Step 3: Verify the changes
SELECT 
    Id,
    DoctorId,
    CAST(StartTime AS TIME) as UpdatedStartTime,
    CAST(EndTime AS TIME) as UpdatedEndTime,
    MaxAppointmentsPerDay as UpdatedMaxSlots,
    SlotDurationMinutes as UpdatedSlotDuration,
    Monday,
    Tuesday,
    Wednesday,
    Thursday,
    Friday,
    Saturday,
    Sunday,
    IsAvailable,
    LastUpdated
FROM DoctorAvailabilities
ORDER BY LastUpdated DESC;

GO

-- Step 4: Check how many records were affected
SELECT 
    COUNT(*) as TotalDoctorAvailabilityRecords,
    SUM(CASE WHEN StartTime = '08:00:00' THEN 1 ELSE 0 END) as RecordsWithCorrectStartTime,
    SUM(CASE WHEN EndTime = '17:00:00' THEN 1 ELSE 0 END) as RecordsWithCorrectEndTime,
    SUM(CASE WHEN MaxAppointmentsPerDay = 100 THEN 1 ELSE 0 END) as RecordsWith100Slots
FROM DoctorAvailabilities;

GO

PRINT '========================================'
PRINT 'Time slot update completed!'
PRINT 'New schedule: 8:00 AM to 5:00 PM'
PRINT 'Slots per day: 100'
PRINT 'Slot duration: ~5 minutes'
PRINT '========================================'
