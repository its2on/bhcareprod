-- Verify if columns exist in AspNetUsers table
USE [Barangay];
GO

SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AspNetUsers'
    AND COLUMN_NAME IN ('Age', 'HasChangedPassword', 'IsFirstLogin', 'LastPasswordChangeDate')
ORDER BY COLUMN_NAME;

-- If no results, columns are missing
-- If 4 rows returned, columns exist
