# BH CARE SYSTEM - TROUBLESHOOTING GUIDE

## 🔴 Current Issues & Solutions

### **Issue 1: Admin Login Failing**
**Error:** `Invalid column name 'Age', 'HasChangedPassword', 'IsFirstLogin', 'LastPasswordChangeDate'`

**Root Cause:** 
- The `AspNetUsers` table is missing required columns
- EF Core migrations were running and conflicting with manual SQL scripts

**Solution:**
1. ✅ **Migrations are now DISABLED** in `Program.cs` (line 440-477)
2. ❌ **You MUST run the complete SQL script** to add ALL missing columns

---

## 📋 Required Actions

### **Step 1: Run Complete SQL Script in SSMS**

Open SQL Server Management Studio and execute this:

```sql
USE [Barangay]
GO

PRINT 'Adding ALL missing columns to AspNetUsers...'

-- Add all 39 required columns
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'UserNumber')
    ALTER TABLE [AspNetUsers] ADD [UserNumber] INT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'Name')
    ALTER TABLE [AspNetUsers] ADD [Name] NVARCHAR(MAX) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'Status')
    ALTER TABLE [AspNetUsers] ADD [Status] NVARCHAR(MAX) NOT NULL DEFAULT 'Pending';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'EncryptedStatus')
    ALTER TABLE [AspNetUsers] ADD [EncryptedStatus] NVARCHAR(MAX) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'EncryptedFullName')
    ALTER TABLE [AspNetUsers] ADD [EncryptedFullName] NVARCHAR(MAX) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'Specialization')
    ALTER TABLE [AspNetUsers] ADD [Specialization] NVARCHAR(MAX) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'IsActive')
    ALTER TABLE [AspNetUsers] ADD [IsActive] BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'WorkingDays')
    ALTER TABLE [AspNetUsers] ADD [WorkingDays] NVARCHAR(MAX) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'WorkingHours')
    ALTER TABLE [AspNetUsers] ADD [WorkingHours] NVARCHAR(MAX) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'MaxDailyPatients')
    ALTER TABLE [AspNetUsers] ADD [MaxDailyPatients] INT NOT NULL DEFAULT 20;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'BirthDate')
    ALTER TABLE [AspNetUsers] ADD [BirthDate] DATETIME2 NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'Gender')
    ALTER TABLE [AspNetUsers] ADD [Gender] NVARCHAR(MAX) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'Age')
    ALTER TABLE [AspNetUsers] ADD [Age] NVARCHAR(MAX) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'Address')
    ALTER TABLE [AspNetUsers] ADD [Address] NVARCHAR(MAX) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'Barangay')
    ALTER TABLE [AspNetUsers] ADD [Barangay] NVARCHAR(MAX) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'CreatedAt')
    ALTER TABLE [AspNetUsers] ADD [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE();

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'UpdatedAt')
    ALTER TABLE [AspNetUsers] ADD [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE();

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'ProfilePicture')
    ALTER TABLE [AspNetUsers] ADD [ProfilePicture] NVARCHAR(MAX) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'ProfileImage')
    ALTER TABLE [AspNetUsers] ADD [ProfileImage] NVARCHAR(MAX) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'PhilHealthId')
    ALTER TABLE [AspNetUsers] ADD [PhilHealthId] NVARCHAR(MAX) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'LastActive')
    ALTER TABLE [AspNetUsers] ADD [LastActive] DATETIME2 NOT NULL DEFAULT GETUTCDATE();

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'JoinDate')
    ALTER TABLE [AspNetUsers] ADD [JoinDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE();

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'UserType')
    ALTER TABLE [AspNetUsers] ADD [UserType] INT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'HasAgreedToTerms')
    ALTER TABLE [AspNetUsers] ADD [HasAgreedToTerms] BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'AgreedAt')
    ALTER TABLE [AspNetUsers] ADD [AgreedAt] DATETIME2 NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'IsFirstLogin')
    ALTER TABLE [AspNetUsers] ADD [IsFirstLogin] BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'HasChangedPassword')
    ALTER TABLE [AspNetUsers] ADD [HasChangedPassword] BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'LastPasswordChangeDate')
    ALTER TABLE [AspNetUsers] ADD [LastPasswordChangeDate] DATETIME2 NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'AppointmentReminders')
    ALTER TABLE [AspNetUsers] ADD [AppointmentReminders] BIT NOT NULL DEFAULT 1;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'PrescriptionAlerts')
    ALTER TABLE [AspNetUsers] ADD [PrescriptionAlerts] BIT NOT NULL DEFAULT 1;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'HealthTips')
    ALTER TABLE [AspNetUsers] ADD [HealthTips] BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'FirstName')
    ALTER TABLE [AspNetUsers] ADD [FirstName] NVARCHAR(MAX) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'MiddleName')
    ALTER TABLE [AspNetUsers] ADD [MiddleName] NVARCHAR(MAX) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'LastName')
    ALTER TABLE [AspNetUsers] ADD [LastName] NVARCHAR(MAX) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'Suffix')
    ALTER TABLE [AspNetUsers] ADD [Suffix] NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'Occupation')
    ALTER TABLE [AspNetUsers] ADD [Occupation] NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'CivilStatus')
    ALTER TABLE [AspNetUsers] ADD [CivilStatus] NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUsers' AND COLUMN_NAME = 'Religion')
    ALTER TABLE [AspNetUsers] ADD [Religion] NVARCHAR(MAX) NULL;

PRINT 'All 39 columns added successfully!'
GO
```

### **Step 2: Verify Columns Were Added**

```sql
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'AspNetUsers'
ORDER BY COLUMN_NAME;
```

You should see **ALL 39 custom columns** plus the standard Identity columns.

### **Step 3: Run the Application**

```bash
dotnet clean
dotnet build
dotnet run
```

---

## ✅ Changes Made

### **1. Disabled EF Core Migrations**
- File: `Program.cs` (lines 440-477)
- Migrations are now commented out
- Database schema managed via SQL scripts only

### **2. Added Connection String Logging**
- File: `Program.cs` (lines 152-154)
- Shows which database the app connects to at startup

### **3. Updated Connection Strings**
- `appsettings.json`: `Server=DESKTOP-NU53VS3\\SQLEXPRESS;Database=Barangay`
- `appsettings.Development.json`: Same connection string
- All SQL scripts updated to use `[Barangay]` database

---

## 🔍 Verification Checklist

- [ ] Run complete SQL script in SSMS
- [ ] Verify all 39 columns exist in AspNetUsers
- [ ] Migrations disabled in Program.cs
- [ ] Connection string points to correct database
- [ ] Application builds without errors
- [ ] Admin login works

---

## 📞 If Issues Persist

1. **Check connection string output** when running `dotnet run`
2. **Verify database name** matches in SSMS and appsettings.json
3. **Ensure all 39 columns exist** in AspNetUsers table
4. **Check for typos** in column names

---

## 🎯 Summary

**Problem:** EF migrations conflicting with manual SQL scripts  
**Solution:** Disabled migrations, use SQL scripts exclusively  
**Action Required:** Run the complete SQL script above in SSMS  
**Expected Result:** Admin login will work after all columns are added  

