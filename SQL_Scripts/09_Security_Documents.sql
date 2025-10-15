-- ============================================
-- BH CARE - PART 9: SECURITY & DOCUMENTS
-- ============================================

USE [Barangay]
GO

-- EmailVerifications
CREATE TABLE [EmailVerifications] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Email] NVARCHAR(MAX) NOT NULL,
    [VerificationCode] NVARCHAR(MAX) NOT NULL,
    [ExpiresAt] DATETIME2 NOT NULL,
    [IsVerified] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- EmailSuspensions
CREATE TABLE [EmailSuspensions] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Email] NVARCHAR(MAX) NOT NULL,
    [SuspendedUntil] DATETIME2 NOT NULL,
    [Reason] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- PasswordResetOTPs
CREATE TABLE [PasswordResetOTPs] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [OTPCode] NVARCHAR(MAX) NOT NULL,
    [ExpiresAt] DATETIME2 NOT NULL,
    [IsUsed] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_PasswordResetOTPs_UserId] ON [PasswordResetOTPs] ([UserId]);
GO

-- UserDocuments
CREATE TABLE [UserDocuments] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [DocumentType] NVARCHAR(MAX) NOT NULL,
    [FileName] NVARCHAR(MAX) NOT NULL,
    [FilePath] NVARCHAR(MAX) NOT NULL,
    [FileSize] BIGINT NULL,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    [UploadDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ApprovedBy] NVARCHAR(450) NULL,
    [ApprovedDate] DATETIME2 NULL,
    [RejectionReason] NVARCHAR(MAX) NULL,
    [Notes] NVARCHAR(MAX) NULL,
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE,
    FOREIGN KEY ([ApprovedBy]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_UserDocuments_UserId] ON [UserDocuments] ([UserId]);
CREATE INDEX [IX_UserDocuments_Status] ON [UserDocuments] ([Status]);
CREATE INDEX [IX_UserDocuments_UploadDate] ON [UserDocuments] ([UploadDate]);
GO

-- UrlTokens
CREATE TABLE [UrlTokens] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Token] NVARCHAR(450) NOT NULL,
    [ResourceType] NVARCHAR(MAX) NOT NULL,
    [ResourceId] NVARCHAR(450) NULL,
    [ExpiresAt] DATETIME2 NOT NULL,
    [IsUsed] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UsedAt] DATETIME2 NULL,
    FOREIGN KEY ([ResourceId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
);
CREATE UNIQUE INDEX [IX_UrlTokens_Token] ON [UrlTokens] ([Token]);
-- CREATE INDEX [IX_UrlTokens_ResourceType] ON [UrlTokens] ([ResourceType]); -- Cannot index NVARCHAR(MAX)
CREATE INDEX [IX_UrlTokens_ResourceId] ON [UrlTokens] ([ResourceId]);
CREATE INDEX [IX_UrlTokens_ExpiresAt] ON [UrlTokens] ([ExpiresAt]);
CREATE INDEX [IX_UrlTokens_IsUsed] ON [UrlTokens] ([IsUsed]);
GO

-- UserSuspensions
CREATE TABLE [UserSuspensions] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [Reason] NVARCHAR(MAX) NOT NULL,
    [SuspendedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [SuspendedUntil] DATETIME2 NULL,
    [SuspendedBy] NVARCHAR(450) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_UserSuspensions_UserId] ON [UserSuspensions] ([UserId]);
GO
