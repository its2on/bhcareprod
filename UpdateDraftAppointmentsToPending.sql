-- Update existing Draft appointments for consultation types that don't require assessments
-- This will make them visible in Nurse/Appointments and User Ongoing Appointments

UPDATE Appointments
SET Status = 1  -- 1 = Pending status
WHERE Status = 0  -- 0 = Draft status
AND (
    LOWER(Type) = 'immunization' OR
    LOWER(Type) = 'dental' OR
    LOWER(Type) = 'dots consult' OR
    LOWER(Type) = 'prenatal & family planning' OR
    LOWER(Type) = 'prenatal and family planning'
);

-- Verify the update
SELECT Id, PatientName, Type, Status, AppointmentDate, AppointmentTime
FROM Appointments
WHERE (
    LOWER(Type) = 'immunization' OR
    LOWER(Type) = 'dental' OR
    LOWER(Type) = 'dots consult' OR
    LOWER(Type) = 'prenatal & family planning' OR
    LOWER(Type) = 'prenatal and family planning'
)
ORDER BY AppointmentDate DESC, AppointmentTime DESC;
