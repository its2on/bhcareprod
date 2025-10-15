-- ============================================
-- BH CARE - PART 1: IDENTITY & USERS
-- ============================================

USE [Barangay]
GO

-- AspNetRoles
CREATE TABLE [AspNetRoles] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(256) NULL,
    [NormalizedName] NVARCHAR(256) NULL,
    [ConcurrencyStamp] NVARCHAR(MAX) NULL
);
CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO

-- AspNetUsers (Extended ApplicationUser)
CREATE TABLE [AspNetUsers] (
    [Id] NVARCHAR(450) NOT NULL PRIMARY KEY,
    [UserName] NVARCHAR(256) NULL,
    [NormalizedUserName] NVARCHAR(256) NULL,
    [Email] NVARCHAR(256) NULL,
    [NormalizedEmail] NVARCHAR(256) NULL,
    [EmailConfirmed] BIT NOT NULL DEFAULT 0,
    [PasswordHash] NVARCHAR(MAX) NULL,
    [SecurityStamp] NVARCHAR(MAX) NULL,
    [ConcurrencyStamp] NVARCHAR(MAX) NULL,
    [PhoneNumber] NVARCHAR(MAX) NULL,
    [PhoneNumberConfirmed] BIT NOT NULL DEFAULT 0,
    [TwoFactorEnabled] BIT NOT NULL DEFAULT 0,
    [LockoutEnd] DATETIMEOFFSET NULL,
    [LockoutEnabled] BIT NOT NULL DEFAULT 0,
    [AccessFailedCount] INT NOT NULL DEFAULT 0,
    [UserNumber] INT NOT NULL DEFAULT 0,
    [Name] NVARCHAR(MAX) NOT NULL DEFAULT '',
    [Status] NVARCHAR(MAX) NOT NULL DEFAULT 'Pending',
    [Specialization] NVARCHAR(MAX) NOT NULL DEFAULT '',
    [IsActive] BIT NOT NULL DEFAULT 0,
    [WorkingDays] NVARCHAR(MAX) NOT NULL DEFAULT '',
    [WorkingHours] NVARCHAR(MAX) NOT NULL DEFAULT '',
    [MaxDailyPatients] INT NOT NULL DEFAULT 20,
    [BirthDate] DATETIME2 NULL,
    [Gender] NVARCHAR(MAX) NOT NULL DEFAULT '',
    [Age] NVARCHAR(MAX) NOT NULL DEFAULT '',
    [Address] NVARCHAR(MAX) NOT NULL DEFAULT '',
    [Barangay] NVARCHAR(MAX) NOT NULL DEFAULT '',
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ProfilePicture] NVARCHAR(MAX) NOT NULL DEFAULT '',
    [PhilHealthId] NVARCHAR(MAX) NOT NULL DEFAULT '',
    [LastActive] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [JoinDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UserType] INT NOT NULL DEFAULT 0,
    [HasAgreedToTerms] BIT NOT NULL DEFAULT 0,
    [AgreedAt] DATETIME2 NULL,
    [IsFirstLogin] BIT NOT NULL DEFAULT 0,
    [HasChangedPassword] BIT NOT NULL DEFAULT 0,
    [LastPasswordChangeDate] DATETIME2 NULL,
    [AppointmentReminders] BIT NOT NULL DEFAULT 1,
    [PrescriptionAlerts] BIT NOT NULL DEFAULT 1,
    [HealthTips] BIT NOT NULL DEFAULT 0,
    [FirstName] NVARCHAR(MAX) NOT NULL DEFAULT '',
    [MiddleName] NVARCHAR(MAX) NOT NULL DEFAULT '',
    [LastName] NVARCHAR(MAX) NOT NULL DEFAULT '',
    [Suffix] NVARCHAR(MAX) NULL,
    [Occupation] NVARCHAR(MAX) NULL,
    [CivilStatus] NVARCHAR(MAX) NULL,
    [Religion] NVARCHAR(MAX) NULL,
    [FullName] AS TRIM(ISNULL([FirstName] + ' ', '') + ISNULL([MiddleName] + ' ', '') + ISNULL([LastName], '')) PERSISTED,
    [EncryptedStatus] NVARCHAR(MAX) NOT NULL DEFAULT '',
    [EncryptedFullName] NVARCHAR(MAX) NOT NULL DEFAULT '',
    [ProfileImage] NVARCHAR(MAX) NOT NULL DEFAULT ''
);
CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO

-- AspNetUserRoles
CREATE TABLE [AspNetUserRoles] (
    [UserId] NVARCHAR(450) NOT NULL,
    [RoleId] NVARCHAR(450) NOT NULL,
    PRIMARY KEY ([UserId], [RoleId]),
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE,
    FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO

-- AspNetUserClaims
CREATE TABLE [AspNetUserClaims] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [UserId] NVARCHAR(450) NOT NULL,
    [ClaimType] NVARCHAR(MAX) NULL,
    [ClaimValue] NVARCHAR(MAX) NULL,
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO

-- AspNetUserLogins
CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] NVARCHAR(450) NOT NULL,
    [ProviderKey] NVARCHAR(450) NOT NULL,
    [ProviderDisplayName] NVARCHAR(MAX) NULL,
    [UserId] NVARCHAR(450) NOT NULL,
    PRIMARY KEY ([LoginProvider], [ProviderKey]),
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO

-- AspNetUserTokens
CREATE TABLE [AspNetUserTokens] (
    [UserId] NVARCHAR(450) NOT NULL,
    [LoginProvider] NVARCHAR(450) NOT NULL,
    [Name] NVARCHAR(450) NOT NULL,
    [Value] NVARCHAR(MAX) NULL,
    PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE NO ACTION
);
GO

-- AspNetRoleClaims
CREATE TABLE [AspNetRoleClaims] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [RoleId] NVARCHAR(450) NOT NULL,
    [ClaimType] NVARCHAR(MAX) NULL,
    [ClaimValue] NVARCHAR(MAX) NULL,
    FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles]([Id]) ON DELETE NO ACTION
);
CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO
