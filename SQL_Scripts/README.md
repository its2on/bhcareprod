# BH Care Database Schema - SQL Scripts

## Overview
Complete Microsoft SQL Server database schema for the BH Care (Barangay Health Care) system.

**Generated:** October 14, 2025  
**Database Name:** BHCareDB  
**Total Tables:** 50+ tables  
**SQL Server Version:** Microsoft SQL Server 2019+

---

## 📁 File Structure

### Execution Order

Execute the SQL scripts in the following order:

1. **00_Master_Script.sql** - Creates database and default roles
2. **01_Identity_Users.sql** - ASP.NET Identity tables (Users, Roles, Claims)
3. **02_Patients_Medical.sql** - Patient records and medical history
4. **03_Appointments.sql** - Appointment management
5. **04_Prescriptions.sql** - Prescriptions and medications
6. **05_Staff_Permissions.sql** - Staff management and RBAC
7. **06_Assessments.sql** - Health assessments (HEEADSSS, NCD)
8. **07_Immunization_Family.sql** - Immunization and family records
9. **08_Notifications_Messages.sql** - Notifications and messaging
10. **09_Security_Documents.sql** - Security tokens and documents
11. **10_Reports_Misc.sql** - Reports and miscellaneous tables

---

## 📊 Database Tables Summary

### 1. Identity & Authentication (9 tables)
- `AspNetUsers` - Extended user accounts with BH Care fields
- `AspNetRoles` - User roles (Admin, Doctor, Nurse, Patient)
- `AspNetUserRoles` - User-role assignments
- `AspNetUserClaims` - User claims
- `AspNetUserLogins` - External login providers
- `AspNetUserTokens` - Authentication tokens
- `AspNetRoleClaims` - Role-based claims

### 2. Patient Management (7 tables)
- `Patients` - Patient demographics and basic info
- `MedicalRecords` - Medical consultation records
- `VitalSigns` - Patient vital signs tracking
- `FamilyMembers` - Patient family information
- `MedicalHistories` - Medical history records
- `LabResults` - Laboratory test results
- `PatientHistories` - Audit trail of patient actions

### 3. Appointments (5 tables)
- `Appointments` - Appointment bookings
- `AppointmentAttachments` - File attachments for appointments
- `AppointmentFiles` - Additional appointment files
- `ConsultationTimeSlots` - Available consultation slots
- `DoctorAvailabilities` - Doctor schedule management

### 4. Prescriptions & Medications (3 tables)
- `Prescriptions` - Prescription records
- `Medications` - Medication inventory
- `PrescriptionMedications` - Prescribed medication details

### 5. Staff & Permissions (8 tables)
- `Doctors` - Doctor-specific information
- `StaffMembers` - Staff member records
- `Permissions` - System permissions
- `UserPermissions` - User-permission assignments
- `StaffPositions` - Staff position definitions
- `StaffPermissions` - Staff-permission assignments
- `RolePermissions` - Role-permission assignments
- `StaffPositionPermission` - Position-permission junction

### 6. Health Assessments (5 tables)
- `HEEADSSSAssessments` - Adolescent health assessments
- `NCDRiskAssessments` - Non-communicable disease risk assessments
- `AdolescentHealthInfo` - Adolescent health information
- `IntegratedAssessments` - Integrated assessment records
- `Assessments` - General assessment records

### 7. Immunization & Family (4 tables)
- `ImmunizationRecords` - Vaccination records
- `ImmunizationShortcutForms` - Immunization form data
- `FamilyRecords` - Family record management
- `FamilyNumberCounters` - Family number generation
- `GuardianInformation` - Guardian details for minors

### 8. Communication (4 tables)
- `Messages` - User-to-user messaging
- `Notifications` - System notifications
- `Feedbacks` - User feedback submissions
- `FeedbackRatings` - Service ratings

### 9. Security & Documents (5 tables)
- `EmailVerifications` - Email verification codes
- `EmailSuspensions` - Email suspension tracking
- `PasswordResetOTPs` - Password reset OTP codes
- `UserDocuments` - User document uploads
- `UrlTokens` - Secure URL token management
- `UserSuspensions` - User suspension records

### 10. Reports & Misc (2 tables)
- `HealthReports` - Generated health reports
- `__EFMigrationsHistory` - Entity Framework migration tracking

---

## 🔑 Key Features

### Password Change Tracking (Recent Addition)
The following fields were added to `AspNetUsers` for password management:

```sql
[IsFirstLogin] BIT NOT NULL DEFAULT 0
[HasChangedPassword] BIT NOT NULL DEFAULT 0
[LastPasswordChangeDate] DATETIME2 NULL
```

**Purpose:**
- Track first-time logins for staff accounts
- Enforce password change on first login
- Maintain password change history

### Data Encryption
Many sensitive fields use NVARCHAR(MAX) to support encrypted data:
- Patient personal information
- Contact details
- Medical history
- Assessment data

### Computed Columns
- `AspNetUsers.FullName` - Auto-computed from FirstName, MiddleName, LastName

---

## 🚀 Quick Start

### Option 1: Execute All Scripts Manually
```sql
-- 1. Create database
USE master;
CREATE DATABASE BHCareDB;
GO

-- 2. Execute each script in order (01-10)
USE BHCareDB;
-- Run 01_Identity_Users.sql
-- Run 02_Patients_Medical.sql
-- ... and so on
```

### Option 2: Use Master Script
```sql
-- Execute the master script which guides you through the process
-- Run: 00_Master_Script.sql
```

---

## 📝 Important Notes

### 1. Foreign Key Relationships
- Most tables use `ON DELETE NO ACTION` to prevent cascading deletes
- Some tables use `ON DELETE CASCADE` for dependent records
- `ON DELETE SET NULL` used for optional relationships

### 2. Indexes
- Primary keys automatically indexed
- Foreign keys indexed for performance
- Common query fields indexed (dates, status, etc.)

### 3. Default Values
- Most datetime fields default to `GETUTCDATE()`
- Boolean fields have appropriate defaults
- Status fields default to 'Pending' where applicable

### 4. Data Types
- IDs: `NVARCHAR(450)` for ASP.NET Identity, `INT IDENTITY` for others
- Text: `NVARCHAR(MAX)` for encrypted/large text
- Dates: `DATETIME2` for precision
- Decimals: `DECIMAL(5,2)` for measurements

---

## 🔧 Maintenance

### Backup Recommendation
```sql
-- Regular backup
BACKUP DATABASE BHCareDB 
TO DISK = 'C:\Backups\BHCareDB.bak'
WITH FORMAT, INIT, NAME = 'Full Backup of BHCareDB';
```

### Check Database Size
```sql
SELECT 
    name AS DatabaseName,
    size * 8 / 1024 AS SizeMB
FROM sys.master_files
WHERE database_id = DB_ID('BHCareDB');
```

---

## 📞 Support

For issues or questions about the database schema:
1. Check migration files in `/Migrations` folder
2. Review `ApplicationDbContext.cs` for entity configurations
3. Consult the BH Care development team

---

## ⚠️ Warnings

1. **Do not modify** the `AspNetUsers.FullName` computed column
2. **Backup before** running any schema changes
3. **Test in development** before applying to production
4. **Encrypted fields** require proper encryption service configuration
5. **Session-based OTP** - OTP codes are NOT stored in database (stored in HttpContext.Session)

---

## 📜 Version History

- **v1.0** (Oct 14, 2025) - Initial complete schema
  - 50+ tables
  - Password change tracking
  - Full RBAC implementation
  - Health assessment modules
  - Document management
  - Messaging and notifications

---

## 🎯 Next Steps After Database Creation

1. **Create default admin account**
2. **Set up encryption keys** in appsettings.json
3. **Configure email service** for notifications
4. **Initialize default permissions**
5. **Test all CRUD operations**
6. **Run Entity Framework migrations** to sync with code

---

**End of Documentation**
