-- ============================================
-- BH CARE SYSTEM - MASTER DATABASE SCRIPT
-- Microsoft SQL Server
-- Generated: October 14, 2025
-- ============================================
-- This master script executes all table creation scripts in order
-- Run this script to create the complete BH Care database
-- ============================================

USE [master]
GO

USE [Barangay]
GO

PRINT '============================================'
PRINT 'Starting BH Care Database Schema Creation'
PRINT '============================================'
PRINT ''

-- Execute scripts in order
PRINT 'Step 1/10: Creating Identity & User tables...'
-- Run: 01_Identity_Users.sql

PRINT 'Step 2/10: Creating Patient & Medical Record tables...'
-- Run: 02_Patients_Medical.sql

PRINT 'Step 3/10: Creating Appointment tables...'
-- Run: 03_Appointments.sql

PRINT 'Step 4/10: Creating Prescription & Medication tables...'
-- Run: 04_Prescriptions.sql

PRINT 'Step 5/10: Creating Staff & Permission tables...'
-- Run: 05_Staff_Permissions.sql

PRINT 'Step 6/10: Creating Assessment & Form tables...'
-- Run: 06_Assessments.sql

PRINT 'Step 7/10: Creating Immunization & Family Record tables...'
-- Run: 07_Immunization_Family.sql

PRINT 'Step 8/10: Creating Notification & Message tables...'
-- Run: 08_Notifications_Messages.sql

PRINT 'Step 9/10: Creating Security & Document tables...'
-- Run: 09_Security_Documents.sql

PRINT 'Step 10/10: Creating Report & Miscellaneous tables...'
-- Run: 10_Reports_Misc.sql

PRINT ''
PRINT '============================================'
PRINT 'Database Schema Creation Complete!'
PRINT '============================================'
PRINT ''
PRINT 'IMPORTANT: Execute each script file (01-10) in order'
PRINT 'Total Tables: 50+ tables'
PRINT 'Database: BHCareDB'
PRINT '============================================'
GO

-- Insert default roles
IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'Admin')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Admin', 'ADMIN', NEWID())
    PRINT 'Admin role created.'
END

IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'Doctor')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Doctor', 'DOCTOR', NEWID())
    PRINT 'Doctor role created.'
END

IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'Nurse')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Nurse', 'NURSE', NEWID())
    PRINT 'Nurse role created.'
END

IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'Patient')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Patient', 'PATIENT', NEWID())
    PRINT 'Patient role created.'
END

PRINT 'Default roles setup complete.'
GO
