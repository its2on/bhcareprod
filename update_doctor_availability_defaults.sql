-- Update existing DoctorAvailabilities records with proper defaults
-- This sets up the daily appointment slot cap system

-- Update MaxAppointmentsPerDay to 30 (default) for all doctors where it's 0
UPDATE DoctorAvailabilities 
SET MaxAppointmentsPerDay = 30
WHERE MaxAppointmentsPerDay = 0;

-- Calculate and update SlotDurationMinutes for all doctors
-- Formula: (EndTime - StartTime in minutes) / MaxAppointmentsPerDay
UPDATE DoctorAvailabilities
SET SlotDurationMinutes = DATEDIFF(MINUTE, StartTime, EndTime) / NULLIF(MaxAppointmentsPerDay, 0)
WHERE MaxAppointmentsPerDay > 0;

-- Disable weekends by default (Mon-Fri only)
UPDATE DoctorAvailabilities 
SET Saturday = 0, Sunday = 0
WHERE Saturday = 1 OR Sunday = 1;

-- Verify the updates
SELECT 
    DoctorId,
    MaxAppointmentsPerDay,
    SlotDurationMinutes,
    StartTime,
    EndTime,
    Monday,
    Tuesday,
    Wednesday,
    Thursday,
    Friday,
    Saturday,
    Sunday,
    IsAvailable
FROM DoctorAvailabilities;

