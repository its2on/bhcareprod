-- Check the current user's gender value
SELECT TOP 1
    Id,
    UserName,
    FirstName,
    LastName,
    Gender,
    Email
FROM AspNetUsers
ORDER BY Id DESC;

-- Check the Kasarian field configuration in NCD form
SELECT 
    ft.FormName,
    ft.FormKey,
    ff.FieldName,
    ff.FieldLabel,
    ff.FieldType,
    ff.DisplayOrder,
    ffo.OptionLabel,
    ffo.OptionValue,
    ffo.IsDefault
FROM FormTemplates ft
INNER JOIN FormFields ff ON ft.FormTemplateId = ff.FormTemplateId
LEFT JOIN FormFieldOptions ffo ON ff.FormFieldId = ffo.FormFieldId
WHERE ft.FormKey = 'ncd-risk-assessment-form'
AND (
    ff.FieldName LIKE '%kasarian%' 
    OR ff.FieldName LIKE '%sex%' 
    OR ff.FieldName LIKE '%gender%'
    OR ff.FieldLabel LIKE '%kasarian%' 
    OR ff.FieldLabel LIKE '%sex%'
    OR ff.FieldLabel LIKE '%gender%'
)
ORDER BY ff.DisplayOrder, ffo.DisplayOrder;
