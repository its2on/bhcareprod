-- View all services and their linked forms
SELECT 
    cs.ServiceId,
    cs.ServiceName,
    cs.ServiceKey,
    cs.IsActive AS ServiceActive,
    COUNT(ft.FormTemplateId) AS LinkedForms
FROM ConsultationServices cs
LEFT JOIN FormTemplates ft ON cs.ServiceId = ft.ServiceId AND ft.IsActive = 1 AND ft.ShowInAppointmentFlow = 1
WHERE cs.IsActive = 1
GROUP BY cs.ServiceId, cs.ServiceName, cs.ServiceKey, cs.IsActive
ORDER BY cs.DisplayOrder;

-- View all active forms and their linked services
SELECT 
    ft.FormName,
    ft.FormKey,
    cs.ServiceName AS LinkedService,
    ft.ShowInAppointmentFlow,
    ft.IsActive,
    ft.DisplayOrder,
    CASE 
        WHEN ft.ServiceId IS NULL THEN 'General (All Services)'
        ELSE cs.ServiceName
    END AS AppearsFor
FROM FormTemplates ft
LEFT JOIN ConsultationServices cs ON ft.ServiceId = cs.ServiceId
WHERE ft.IsActive = 1
ORDER BY cs.DisplayOrder, ft.DisplayOrder;
