USE [Barangay];

-- Update the existing admin account password
UPDATE [AspNetUsers]
SET [PasswordHash] = 'AQAAAAEAACcQAAAAEJ89jl0VWrcaFZOWCe9gpkkzzr5OXJCSm+bAybabRH+My6QvVTMBGaHoP7IftIH3uw==',
    [SecurityStamp] = NEWID(),
    [ConcurrencyStamp] = NEWID(),
    [UpdatedAt] = GETDATE(),
    [HasChangedPassword] = 1,
    [IsFirstLogin] = 0
WHERE [NormalizedEmail] = 'ADMIN@BHCARE.COM';

PRINT 'Admin password updated successfully!';
PRINT 'Username: admin@bhcare.com';
PRINT 'Password: Admin123!';
