USE [Barangay];

DECLARE @UserId NVARCHAR(450) = NEWID();

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
    @UserId,'admin@bhcare.com','ADMIN@BHCARE.COM','admin@bhcare.com','ADMIN@BHCARE.COM',1,
    'AQAAAAEAACcQAAAAEJ89jl0VWrcaFZOWCe9gpkkzzr5OXJCSm+bAybabRH+My6QvVTMBGaHoP7IftIH3uw==',
    NEWID(),NEWID(),0,0,1,0,1,
    'System Administrator','Active',1,20,GETDATE(),GETDATE(),
    GETDATE(),GETDATE(),3,1,GETDATE(),
    0,1,1,1,0,'System','Administrator','','','',
    '','','','','','','','','',''
);

-- Assign Admin Staff role
INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
SELECT @UserId, [Id] FROM [AspNetRoles] WHERE [Name] = 'Admin Staff';

PRINT 'Admin account created: admin@bhcare.com / Admin123!';
