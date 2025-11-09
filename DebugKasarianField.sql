-- Check the Kasarian field configuration in the NCD Risk Assessment form
SELECT 
    ft.FormName,
    ff.FieldName,
    ff.FieldLabel,
    ff.FieldType,
    ff.DisplayOrder,
    ffo.OptionLabel,
    ffo.OptionValue,
    ffo.IsDefault,
    ffo.DisplayOrder as OptionDisplayOrder
FROM FormTemplates ft
INNER JOIN FormFields ff ON ft.FormTemplateId = ff.FormTemplateId
LEFT JOIN FormFieldOptions ffo ON ff.FormFieldId = ffo.FormFieldId
WHERE ft.FormKey = 'ncd-risk-assessment-form'
AND (ff.FieldName LIKE '%kasarian%' OR ff.FieldName LIKE '%sex%' OR ff.FieldLabel LIKE '%kasarian%' OR ff.FieldLabel LIKE '%sex%')
ORDER BY ff.DisplayOrder, ffo.DisplayOrder;

-- Check what gender value is stored for the current user
SELECT TOP 1
    Id,
    UserName,
    FirstName,
    LastName,
    Gender,
    Religion,
    CivilStatus
FROM AspNetUsers
ORDER BY Id DESC;
