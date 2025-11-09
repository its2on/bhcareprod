-- Enable Dental form to show in appointment workflow
UPDATE FormTemplates
SET ShowInAppointmentFlow = 1
WHERE FormName = 'Dental' OR FormKey = 'dental';

-- Verify the update
SELECT 
    FormName, 
    ServiceId, 
    IsActive, 
    ShowInAppointmentFlow,
    DisplayOrder
FROM FormTemplates
WHERE FormName = 'Dental' OR FormKey = 'dental';
