USE [Barangay];

-- Add the 4 missing columns
ALTER TABLE [dbo].[AspNetUsers] ADD [Age] NVARCHAR(MAX) NULL;
ALTER TABLE [dbo].[AspNetUsers] ADD [HasChangedPassword] BIT NOT NULL DEFAULT 0;
ALTER TABLE [dbo].[AspNetUsers] ADD [IsFirstLogin] BIT NOT NULL DEFAULT 0;
ALTER TABLE [dbo].[AspNetUsers] ADD [LastPasswordChangeDate] DATETIME2 NULL;

PRINT 'Columns added successfully!';
