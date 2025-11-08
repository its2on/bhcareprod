-- ==========================================
-- Check if Forms Are Being Saved
-- ==========================================

-- 1. Check recent form submissions
SELECT TOP 10
    fs.FormSubmissionId,
    fs.FormTemplateId,
    ft.FormName,
    fs.AppointmentId,
    fs.UserId,
    fs.Status,
    fs.SubmittedAt,
    fs.IpAddress,
    LEN(fs.FormData) as DataLength,
    fs.FormData
FROM FormSubmissions fs
LEFT JOIN FormTemplates ft ON fs.FormTemplateId = ft.FormTemplateId
ORDER BY fs.SubmittedAt DESC;

-- 2. Check if any submissions exist
SELECT 
    COUNT(*) as TotalSubmissions,
    MAX(SubmittedAt) as MostRecentSubmission
FROM FormSubmissions;

-- 3. Check submissions by appointment
SELECT 
    a.Id as AppointmentId,
    a.PatientName,
    a.AppointmentDate,
    fs.FormSubmissionId,
    ft.FormName,
    fs.SubmittedAt,
    fs.Status
FROM Appointments a
LEFT JOIN FormSubmissions fs ON a.Id = fs.AppointmentId
LEFT JOIN FormTemplates ft ON fs.FormTemplateId = ft.FormTemplateId
WHERE a.AppointmentDate >= DATEADD(day, -7, GETDATE())
ORDER BY a.AppointmentDate DESC;

-- 4. Check form data for specific submission (replace ID)
SELECT 
    FormSubmissionId,
    FormData,
    SubmittedAt
FROM FormSubmissions
WHERE FormSubmissionId = 1; -- Replace with actual ID

-- 5. Parse JSON data from submission
SELECT 
    fs.FormSubmissionId,
    ft.FormName,
    fs.SubmittedAt,
    JSON_VALUE(fs.FormData, '$."Health Facility"') as HealthFacility,
    JSON_VALUE(fs.FormData, '$."Family No."') as FamilyNumber,
    JSON_VALUE(fs.FormData, '$."Apelyido"') as LastName,
    JSON_VALUE(fs.FormData, '$."Pangalan"') as FirstName
FROM FormSubmissions fs
LEFT JOIN FormTemplates ft ON fs.FormTemplateId = ft.FormTemplateId
ORDER BY fs.SubmittedAt DESC;

-- 6. Check for submissions without appointments
SELECT 
    fs.FormSubmissionId,
    ft.FormName,
    fs.AppointmentId,
    fs.UserId,
    fs.SubmittedAt
FROM FormSubmissions fs
LEFT JOIN FormTemplates ft ON fs.FormTemplateId = ft.FormTemplateId
WHERE fs.AppointmentId IS NULL
ORDER BY fs.SubmittedAt DESC;

-- 7. Check if form template exists and is active
SELECT 
    FormTemplateId,
    FormName,
    FormKey,
    IsActive,
    MinAge,
    MaxAge,
    CreatedAt
FROM FormTemplates
WHERE IsActive = 1
ORDER BY CreatedAt DESC;

-- 8. Count submissions per form template
SELECT 
    ft.FormName,
    ft.FormKey,
    COUNT(fs.FormSubmissionId) as SubmissionCount,
    MAX(fs.SubmittedAt) as LastSubmission
FROM FormTemplates ft
LEFT JOIN FormSubmissions fs ON ft.FormTemplateId = fs.FormTemplateId
WHERE ft.IsActive = 1
GROUP BY ft.FormName, ft.FormKey
ORDER BY SubmissionCount DESC;

-- ==========================================
-- Expected Results if Forms Are Saving:
-- ==========================================
-- Query 1: Should show recent submissions with data
-- Query 2: TotalSubmissions > 0
-- Query 3: Should show appointments with linked submissions
-- Query 5: Should show parsed field values
-- Query 8: Should show submission counts per form

-- ==========================================
-- If No Results:
-- ==========================================
-- - Forms are NOT being saved
-- - Check server logs for errors
-- - Check if OnPostAsync is being called
-- - Check ModelState validation errors
