-- ============================================
-- BH CARE - PART 8: NOTIFICATIONS & MESSAGES
-- ============================================

USE [BHCareDB]
GO

-- Messages
CREATE TABLE [Messages] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [SenderId] NVARCHAR(450) NOT NULL,
    [ReceiverId] NVARCHAR(450) NOT NULL,
    [Subject] NVARCHAR(MAX) NULL,
    [Content] NVARCHAR(MAX) NOT NULL,
    [IsRead] BIT NOT NULL DEFAULT 0,
    [SentAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ReadAt] DATETIME2 NULL,
    FOREIGN KEY ([SenderId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION,
    FOREIGN KEY ([ReceiverId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_Messages_SenderId] ON [Messages] ([SenderId]);
CREATE INDEX [IX_Messages_ReceiverId] ON [Messages] ([ReceiverId]);
CREATE INDEX [IX_Messages_SentAt] ON [Messages] ([SentAt]);
GO

-- Notifications
CREATE TABLE [Notifications] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [Title] NVARCHAR(MAX) NOT NULL,
    [Message] NVARCHAR(MAX) NOT NULL,
    [Type] NVARCHAR(50) NULL,
    [IsRead] BIT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ReadAt] DATETIME2 NULL,
    [Link] NVARCHAR(MAX) NULL,
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
CREATE INDEX [IX_Notifications_CreatedAt] ON [Notifications] ([CreatedAt]);
CREATE INDEX [IX_Notifications_IsRead] ON [Notifications] ([IsRead]);
GO

-- Feedbacks
CREATE TABLE [Feedbacks] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [Subject] NVARCHAR(MAX) NULL,
    [Message] NVARCHAR(MAX) NOT NULL,
    [Rating] INT NULL,
    [Status] NVARCHAR(50) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [Response] NVARCHAR(MAX) NULL,
    [RespondedAt] DATETIME2 NULL,
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_Feedbacks_UserId] ON [Feedbacks] ([UserId]);
CREATE INDEX [IX_Feedbacks_CreatedAt] ON [Feedbacks] ([CreatedAt]);
GO

-- FeedbackRatings
CREATE TABLE [FeedbackRatings] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [ServiceType] NVARCHAR(MAX) NULL,
    [Rating] INT NOT NULL,
    [Comments] NVARCHAR(MAX) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_FeedbackRatings_UserId] ON [FeedbackRatings] ([UserId]);
GO
