-- Update the field "1_kayo_ba_ay_may_sumusunod_na_karamdaman" to make it optional (not required)
-- This field is in the NCD Risk Assessment Form (FormTemplateId = 2)

UPDATE [FormFields]
SET [IsRequired] = 0,
    [UpdatedAt] = GETUTCDATE()
WHERE [FieldName] = '1_kayo_ba_ay_may_sumusunod_na_karamdaman'
  AND [FormTemplateId] = 2;

-- Verify the update
SELECT [FormFieldId], [FieldName], [FieldLabel], [IsRequired], [FieldType], [DisplayOrder]
FROM [FormFields]
WHERE [FieldName] = '1_kayo_ba_ay_may_sumusunod_na_karamdaman'
  AND [FormTemplateId] = 2;



