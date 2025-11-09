-- Seed default consultation services
-- Only insert if the table is empty
IF NOT EXISTS (SELECT 1 FROM [ConsultationServices])
BEGIN
    INSERT INTO [ConsultationServices] 
    ([ServiceName], [ServiceKey], [Description], [IconClass], [ColorTheme], [IsActive], [DisplayOrder], [RequiresAgeBasedAssessment], [Category], [AllowsWalkIn], [AverageDurationMinutes], [CreatedAt])
    VALUES
    ('General Consult', 'general-consult', 'Comprehensive health check-up and consultation', 'fa-solid fa-stethoscope', '#fd7e14', 1, 1, 1, 'Clinical', 1, 30, GETUTCDATE()),
    ('Dental', 'dental', 'Dental check-up and treatment', 'fa-solid fa-tooth', '#20c997', 1, 2, 0, 'Specialized', 1, 45, GETUTCDATE()),
    ('Immunization', 'immunization', 'Vaccination and immunization services', 'fa-solid fa-syringe', '#0d6efd', 1, 3, 0, 'Preventive', 1, 15, GETUTCDATE()),
    ('Prenatal & Family Planning', 'prenatal', 'Prenatal care and family planning services', 'fa-solid fa-baby', '#d63384', 1, 4, 0, 'Maternal', 1, 30, GETUTCDATE()),
    ('DOTS Consult', 'dots', 'Directly Observed Treatment Short-course for TB', 'fa-solid fa-lungs', '#6f42c1', 1, 5, 0, 'Clinical', 1, 20, GETUTCDATE());
    
    PRINT 'Default consultation services seeded successfully.';
END
ELSE
BEGIN
    PRINT 'ConsultationServices table already contains data. Skipping seed.';
END
GO
