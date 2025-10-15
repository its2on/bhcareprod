-- ============================================
-- BH CARE - PART 5: STAFF & PERMISSIONS
-- ============================================

USE [Barangay]
GO

-- Doctors
CREATE TABLE [Doctors] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [Specialization] NVARCHAR(MAX) NULL,
    [LicenseNumber] NVARCHAR(MAX) NULL,
    [IsAvailable] BIT NOT NULL DEFAULT 1,
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_Doctors_UserId] ON [Doctors] ([UserId]);
GO

-- StaffMembers
CREATE TABLE [StaffMembers] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [Name] NVARCHAR(MAX) NOT NULL,
    [Email] NVARCHAR(MAX) NOT NULL,
    [ContactNumber] NVARCHAR(MAX) NULL,
    [Position] NVARCHAR(MAX) NOT NULL,
    [Department] NVARCHAR(MAX) NULL,
    [HireDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [IsActive] BIT NOT NULL DEFAULT 1,
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_StaffMembers_UserId] ON [StaffMembers] ([UserId]);
GO

-- Permissions
CREATE TABLE [Permissions] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(MAX) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [Category] NVARCHAR(MAX) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1
);
GO

-- UserPermissions
CREATE TABLE [UserPermissions] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [PermissionId] INT NOT NULL,
    [GrantedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [GrantedBy] NVARCHAR(450) NULL,
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION,
    FOREIGN KEY ([PermissionId]) REFERENCES [Permissions]([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_UserPermissions_UserId] ON [UserPermissions] ([UserId]);
CREATE INDEX [IX_UserPermissions_PermissionId] ON [UserPermissions] ([PermissionId]);
GO

-- StaffPositions
CREATE TABLE [StaffPositions] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(MAX) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [Department] NVARCHAR(MAX) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1
);
GO

-- StaffPermissions
CREATE TABLE [StaffPermissions] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [StaffMemberId] INT NOT NULL,
    [PermissionId] INT NOT NULL,
    [GrantedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY ([StaffMemberId]) REFERENCES [StaffMembers]([Id]) ON DELETE NO ACTION,
    FOREIGN KEY ([PermissionId]) REFERENCES [Permissions]([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_StaffPermissions_StaffMemberId] ON [StaffPermissions] ([StaffMemberId]);
CREATE INDEX [IX_StaffPermissions_PermissionId] ON [StaffPermissions] ([PermissionId]);
GO

-- RolePermissions
CREATE TABLE [RolePermissions] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [RoleId] NVARCHAR(450) NOT NULL,
    [PermissionId] INT NOT NULL,
    FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles]([Id]) ON DELETE CASCADE,
    FOREIGN KEY ([PermissionId]) REFERENCES [Permissions]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_RolePermissions_RoleId] ON [RolePermissions] ([RoleId]);
CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);
GO

-- StaffPositionPermission (Junction Table)
CREATE TABLE [StaffPositionPermission] (
    [StaffPositionId] INT NOT NULL,
    [PermissionId] INT NOT NULL,
    PRIMARY KEY ([StaffPositionId], [PermissionId]),
    FOREIGN KEY ([StaffPositionId]) REFERENCES [StaffPositions]([Id]) ON DELETE NO ACTION,
    FOREIGN KEY ([PermissionId]) REFERENCES [Permissions]([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_StaffPositionPermission_PermissionId] ON [StaffPositionPermission] ([PermissionId]);
GO
