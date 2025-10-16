# Database Column Fix Guide

## 🚨 **Issue Identified**
The application is failing to start due to missing columns in the database. Specifically, the `HEEADSSSAssessments` table is missing several sexuality-related columns that are defined in the C# model but don't exist in the database.

## 📋 **Missing Columns**
The following columns are missing from the `HEEADSSSAssessments` table:

1. `SexualityBodyConcerns`
2. `SexualityHealthConcerns`
3. `SexualityPartnersCount`
4. `SexualityIntimateRelationships`
5. `SexualityPartners`
6. `SexualitySexualOrientation`
7. `SexualityPregnancy`
8. `SexualitySTI`
9. `SexualityProtection`
10. `SexualityPregnancyExperience`
11. `SexualitySTIExperience`
12. `SexualityProtectionUse`
13. `SexualityHarassment`
14. `SexualityGay`
15. `SexualityLesbian`
16. `SexualityBisexual`

## 🔧 **How to Fix**

### **Option 1: Using SQL Server Management Studio (Recommended)**
1. Open SQL Server Management Studio
2. Connect to your database server: `bhcareserverprod.database.windows.net`
3. Open the file `FixHEEADSSSColumns.sql`
4. Execute the script
5. Verify all columns were added successfully

### **Option 2: Using Azure Data Studio**
1. Open Azure Data Studio
2. Connect to your database
3. Open and run the `FixHEEADSSSColumns.sql` script

### **Option 3: Using Command Line (if sqlcmd is available)**
```bash
sqlcmd -S "bhcareserverprod.database.windows.net" -d "bhcareDB" -U "bhcareprod" -P "prodcarebh.123" -i "FixHEEADSSSColumns.sql"
```

## 📝 **SQL Script Contents**
The `FixHEEADSSSColumns.sql` script will:
- Check if the `HEEADSSSAssessments` table exists
- Add each missing column only if it doesn't already exist
- Use `NVARCHAR(4000)` data type to match the encrypted field requirements
- Verify that all columns were added successfully

## ✅ **Verification Steps**
After running the script:

1. **Check the script output** - it should show "Added [ColumnName] column" for each missing column
2. **Verify in database** - you can run this query to confirm:
   ```sql
   SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
   FROM INFORMATION_SCHEMA.COLUMNS 
   WHERE TABLE_NAME = 'HEEADSSSAssessments' 
   AND COLUMN_NAME LIKE 'Sexuality%'
   ORDER BY COLUMN_NAME;
   ```
3. **Restart the application** - run `dotnet run` again

## 🎯 **Expected Result**
After applying the fix:
- ✅ Application should start without database errors
- ✅ HEEADSSS assessments should save properly
- ✅ All sexuality-related fields should work correctly
- ✅ No more "Invalid column name" errors

## 🚨 **Important Notes**
- **Backup First**: Always backup your database before making schema changes
- **Test Environment**: If possible, test this on a development database first
- **All Columns**: The script adds ALL missing sexuality columns to prevent future issues
- **Data Type**: Uses `NVARCHAR(4000)` to support encrypted data storage

## 📞 **If Issues Persist**
If you still get errors after running the fix:
1. Check that all 16 columns were actually added
2. Restart the application completely (`dotnet clean && dotnet build && dotnet run`)
3. Check for any other missing columns in the error messages
4. Verify database connection and permissions

## 🔄 **Related Fixes**
You may also need to run:
- `QuickFixNCDColumns.sql` - for NCD Risk Assessment missing columns
- Any other migration scripts in the project

The application should work properly once all missing database columns are added!
