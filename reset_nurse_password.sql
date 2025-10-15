USE [Barangay];
GO

SET QUOTED_IDENTIFIER ON;
GO

-- Password hash for "Test123!"
DECLARE @PasswordHash NVARCHAR(MAX) = 'AQAAAAEAACcQAAAAEJ89jl0VWrcaFZOWCe9gpkkzzr5OXJCSm+bAybabRH+My6QvVTMBGaHoP7IftIH3uw==';

-- Reset nurse password
UPDATE [AspNetUsers]
SET 
    [PasswordHash] = @PasswordHash,
    [SecurityStamp] = NEWID(),
    [ConcurrencyStamp] = NEWID(),
    [LockoutEnd] = NULL,
    [AccessFailedCount] = 0,
    [LockoutEnabled] = 1
WHERE [Email] = 'nurse@bhcare.com';

PRINT '✓ Password reset for nurse@bhcare.com';
PRINT 'New password: Test123!';
GO
