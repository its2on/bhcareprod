USE [Barangay];
GO

PRINT '=== Creating Test Users and Admin Accounts ===';
GO

-- Password hash for "Test123!" for all test accounts
DECLARE @TestPasswordHash NVARCHAR(MAX) = 'AQAAAAEAACcQAAAAEJ89jl0VWrcaFZOWCe9gpkkzzr5OXJCSm+bAybabRH+My6QvVTMBGaHoP7IftIH3uw==';

-- ============================================
-- 1. ADMIN ACCOUNT
-- ============================================
DECLARE @AdminId NVARCHAR(450) = NEWID();

IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Email] = 'admin@bhcare.com')
BEGIN
    INSERT INTO [AspNetUsers] (
        [Id],[UserName],[NormalizedUserName],[Email],[NormalizedEmail],[EmailConfirmed],
        [PasswordHash],[SecurityStamp],[ConcurrencyStamp],[PhoneNumberConfirmed],
        [TwoFactorEnabled],[LockoutEnabled],[AccessFailedCount],[UserNumber],
        [Name],[Status],[IsActive],[MaxDailyPatients],[CreatedAt],[UpdatedAt],
        [LastActive],[JoinDate],[UserType],[HasAgreedToTerms],[AgreedAt],
        [IsFirstLogin],[HasChangedPassword],[AppointmentReminders],[PrescriptionAlerts],
        [HealthTips],[FirstName],[LastName],[Specialization],[WorkingDays],[WorkingHours],
        [Gender],[Age],[Address],[Barangay],[ProfilePicture],[PhilHealthId],[MiddleName],
        [EncryptedStatus],[EncryptedFullName],[ProfileImage]
    )
    VALUES (
        @AdminId,'admin@bhcare.com','ADMIN@BHCARE.COM','admin@bhcare.com','ADMIN@BHCARE.COM',1,
        @TestPasswordHash,NEWID(),NEWID(),0,0,1,0,1,
        'System Administrator','Active',1,20,GETDATE(),GETDATE(),
        GETDATE(),GETDATE(),3,1,GETDATE(),
        0,1,1,1,0,'System','Administrator','','','',
        '','','','','','','','','',''
    );

    INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
    SELECT @AdminId, [Id] FROM [AspNetRoles] WHERE [Name] = 'Admin Staff';

    PRINT '✓ Admin created: admin@bhcare.com / Test123!';
END
ELSE
BEGIN
    PRINT '✓ Admin already exists: admin@bhcare.com';
END
GO

-- ============================================
-- 2. DOCTOR ACCOUNT
-- ============================================
DECLARE @DoctorId NVARCHAR(450) = NEWID();
DECLARE @TestPasswordHash NVARCHAR(MAX) = 'AQAAAAEAACcQAAAAEJ89jl0VWrcaFZOWCe9gpkkzzr5OXJCSm+bAybabRH+My6QvVTMBGaHoP7IftIH3uw==';

IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Email] = 'doctor@bhcare.com')
BEGIN
    INSERT INTO [AspNetUsers] (
        [Id],[UserName],[NormalizedUserName],[Email],[NormalizedEmail],[EmailConfirmed],
        [PasswordHash],[SecurityStamp],[ConcurrencyStamp],[PhoneNumberConfirmed],
        [TwoFactorEnabled],[LockoutEnabled],[AccessFailedCount],[UserNumber],
        [Name],[Status],[IsActive],[MaxDailyPatients],[CreatedAt],[UpdatedAt],
        [LastActive],[JoinDate],[UserType],[HasAgreedToTerms],[AgreedAt],
        [IsFirstLogin],[HasChangedPassword],[AppointmentReminders],[PrescriptionAlerts],
        [HealthTips],[FirstName],[LastName],[Specialization],[WorkingDays],[WorkingHours],
        [Gender],[Age],[Address],[Barangay],[ProfilePicture],[PhilHealthId],[MiddleName],
        [EncryptedStatus],[EncryptedFullName],[ProfileImage],[PhoneNumber]
    )
    VALUES (
        @DoctorId,'doctor@bhcare.com','DOCTOR@BHCARE.COM','doctor@bhcare.com','DOCTOR@BHCARE.COM',1,
        @TestPasswordHash,NEWID(),NEWID(),0,0,1,0,2,
        'Dr. Juan Dela Cruz','Active',1,30,GETDATE(),GETDATE(),
        GETDATE(),GETDATE(),2,1,GETDATE(),
        0,1,1,1,0,'Juan','Dela Cruz','General Practice','Monday,Tuesday,Wednesday,Thursday,Friday','9:00 AM - 5:00 PM',
        'Male','45','123 Medical St.','Barangay 158','','',''
        ,'','','',('+63 912 345 6789')
    );

    INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
    SELECT @DoctorId, [Id] FROM [AspNetRoles] WHERE [Name] = 'Doctor';

    -- Add to StaffMembers table
    INSERT INTO [StaffMembers] ([UserId],[Name],[Email],[ContactNumber],[Position],[Department],[HireDate],[IsActive])
    VALUES (@DoctorId,'Dr. Juan Dela Cruz','doctor@bhcare.com','+63 912 345 6789','Doctor','Medical',GETDATE(),1);

    PRINT '✓ Doctor created: doctor@bhcare.com / Test123!';
END
ELSE
BEGIN
    PRINT '✓ Doctor already exists: doctor@bhcare.com';
END
GO

-- ============================================
-- 3. NURSE ACCOUNT
-- ============================================
DECLARE @NurseId NVARCHAR(450) = NEWID();
DECLARE @TestPasswordHash NVARCHAR(MAX) = 'AQAAAAEAACcQAAAAEJ89jl0VWrcaFZOWCe9gpkkzzr5OXJCSm+bAybabRH+My6QvVTMBGaHoP7IftIH3uw==';

IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Email] = 'nurse@bhcare.com')
BEGIN
    INSERT INTO [AspNetUsers] (
        [Id],[UserName],[NormalizedUserName],[Email],[NormalizedEmail],[EmailConfirmed],
        [PasswordHash],[SecurityStamp],[ConcurrencyStamp],[PhoneNumberConfirmed],
        [TwoFactorEnabled],[LockoutEnabled],[AccessFailedCount],[UserNumber],
        [Name],[Status],[IsActive],[MaxDailyPatients],[CreatedAt],[UpdatedAt],
        [LastActive],[JoinDate],[UserType],[HasAgreedToTerms],[AgreedAt],
        [IsFirstLogin],[HasChangedPassword],[AppointmentReminders],[PrescriptionAlerts],
        [HealthTips],[FirstName],[LastName],[Specialization],[WorkingDays],[WorkingHours],
        [Gender],[Age],[Address],[Barangay],[ProfilePicture],[PhilHealthId],[MiddleName],
        [EncryptedStatus],[EncryptedFullName],[ProfileImage],[PhoneNumber]
    )
    VALUES (
        @NurseId,'nurse@bhcare.com','NURSE@BHCARE.COM','nurse@bhcare.com','NURSE@BHCARE.COM',1,
        @TestPasswordHash,NEWID(),NEWID(),0,0,1,0,3,
        'Maria Santos','Active',1,20,GETDATE(),GETDATE(),
        GETDATE(),GETDATE(),2,1,GETDATE(),
        0,1,1,1,0,'Maria','Santos','','Monday,Tuesday,Wednesday,Thursday,Friday','8:00 AM - 4:00 PM',
        'Female','35','456 Health Ave.','Barangay 158','','',''
        ,'','','',('+63 923 456 7890')
    );

    INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
    SELECT @NurseId, [Id] FROM [AspNetRoles] WHERE [Name] = 'Nurse';

    -- Add to StaffMembers table
    INSERT INTO [StaffMembers] ([UserId],[Name],[Email],[ContactNumber],[Position],[Department],[HireDate],[IsActive])
    VALUES (@NurseId,'Maria Santos','nurse@bhcare.com','+63 923 456 7890','Nurse','Nursing',GETDATE(),1);

    PRINT '✓ Nurse created: nurse@bhcare.com / Test123!';
END
ELSE
BEGIN
    PRINT '✓ Nurse already exists: nurse@bhcare.com';
END
GO

-- ============================================
-- 4. PATIENT/USER ACCOUNTS
-- ============================================
DECLARE @Patient1Id NVARCHAR(450) = NEWID();
DECLARE @Patient2Id NVARCHAR(450) = NEWID();
DECLARE @TestPasswordHash NVARCHAR(MAX) = 'AQAAAAEAACcQAAAAEJ89jl0VWrcaFZOWCe9gpkkzzr5OXJCSm+bAybabRH+My6QvVTMBGaHoP7IftIH3uw==';

-- Patient 1
IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Email] = 'patient1@test.com')
BEGIN
    INSERT INTO [AspNetUsers] (
        [Id],[UserName],[NormalizedUserName],[Email],[NormalizedEmail],[EmailConfirmed],
        [PasswordHash],[SecurityStamp],[ConcurrencyStamp],[PhoneNumberConfirmed],
        [TwoFactorEnabled],[LockoutEnabled],[AccessFailedCount],[UserNumber],
        [Name],[Status],[IsActive],[MaxDailyPatients],[CreatedAt],[UpdatedAt],
        [LastActive],[JoinDate],[UserType],[HasAgreedToTerms],[AgreedAt],
        [IsFirstLogin],[HasChangedPassword],[AppointmentReminders],[PrescriptionAlerts],
        [HealthTips],[FirstName],[LastName],[Specialization],[WorkingDays],[WorkingHours],
        [Gender],[Age],[Address],[Barangay],[ProfilePicture],[PhilHealthId],[MiddleName],
        [EncryptedStatus],[EncryptedFullName],[ProfileImage],[PhoneNumber],[BirthDate],[CivilStatus]
    )
    VALUES (
        @Patient1Id,'patient1@test.com','PATIENT1@TEST.COM','patient1@test.com','PATIENT1@TEST.COM',1,
        @TestPasswordHash,NEWID(),NEWID(),0,0,1,0,101,
        'Pedro Reyes','Active',1,20,GETDATE(),GETDATE(),
        GETDATE(),GETDATE(),0,1,GETDATE(),
        0,1,1,1,1,'Pedro','Reyes','','','',
        'Male','28','789 Patient St.','Barangay 158','','12345678901',''
        ,'','','',('+63 934 567 8901'),'1996-05-15','Single'
    );

    INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
    SELECT @Patient1Id, [Id] FROM [AspNetRoles] WHERE [Name] = 'Patient';

    PRINT '✓ Patient 1 created: patient1@test.com / Test123!';
END
ELSE
BEGIN
    PRINT '✓ Patient 1 already exists: patient1@test.com';
END

-- Patient 2
IF NOT EXISTS (SELECT 1 FROM [AspNetUsers] WHERE [Email] = 'patient2@test.com')
BEGIN
    INSERT INTO [AspNetUsers] (
        [Id],[UserName],[NormalizedUserName],[Email],[NormalizedEmail],[EmailConfirmed],
        [PasswordHash],[SecurityStamp],[ConcurrencyStamp],[PhoneNumberConfirmed],
        [TwoFactorEnabled],[LockoutEnabled],[AccessFailedCount],[UserNumber],
        [Name],[Status],[IsActive],[MaxDailyPatients],[CreatedAt],[UpdatedAt],
        [LastActive],[JoinDate],[UserType],[HasAgreedToTerms],[AgreedAt],
        [IsFirstLogin],[HasChangedPassword],[AppointmentReminders],[PrescriptionAlerts],
        [HealthTips],[FirstName],[LastName],[Specialization],[WorkingDays],[WorkingHours],
        [Gender],[Age],[Address],[Barangay],[ProfilePicture],[PhilHealthId],[MiddleName],
        [EncryptedStatus],[EncryptedFullName],[ProfileImage],[PhoneNumber],[BirthDate],[CivilStatus]
    )
    VALUES (
        @Patient2Id,'patient2@test.com','PATIENT2@TEST.COM','patient2@test.com','PATIENT2@TEST.COM',1,
        @TestPasswordHash,NEWID(),NEWID(),0,0,1,0,102,
        'Ana Garcia','Active',1,20,GETDATE(),GETDATE(),
        GETDATE(),GETDATE(),0,1,GETDATE(),
        0,1,1,1,1,'Ana','Garcia','','','',
        'Female','32','321 Wellness Rd.','Barangay 158','','98765432109',''
        ,'','','',('+63 945 678 9012'),'1992-08-22','Married'
    );

    INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
    SELECT @Patient2Id, [Id] FROM [AspNetRoles] WHERE [Name] = 'Patient';

    PRINT '✓ Patient 2 created: patient2@test.com / Test123!';
END
ELSE
BEGIN
    PRINT '✓ Patient 2 already exists: patient2@test.com';
END
GO

PRINT '';
PRINT '=== Test Users Created Successfully! ===';
PRINT '';
PRINT 'LOGIN CREDENTIALS (All passwords: Test123!)';
PRINT '-------------------------------------------';
PRINT 'Admin:    admin@bhcare.com / Test123!';
PRINT 'Doctor:   doctor@bhcare.com / Test123!';
PRINT 'Nurse:    nurse@bhcare.com / Test123!';
PRINT 'Patient1: patient1@test.com / Test123!';
PRINT 'Patient2: patient2@test.com / Test123!';
PRINT '';
GO
