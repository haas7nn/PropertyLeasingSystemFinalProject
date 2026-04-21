IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Buildings] (
    [BuildingId] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Address] nvarchar(500) NOT NULL,
    [Location] nvarchar(100) NULL,
    [TotalUnits] int NOT NULL,
    CONSTRAINT [PK_Buildings] PRIMARY KEY ([BuildingId])
);

CREATE TABLE [Notifications] (
    [NotificationId] int NOT NULL IDENTITY,
    [UserId] nvarchar(max) NOT NULL,
    [Message] nvarchar(500) NOT NULL,
    [Type] nvarchar(100) NOT NULL,
    [IsRead] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([NotificationId])
);

CREATE TABLE [Roles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [Id] nvarchar(450) NOT NULL,
    [Discriminator] nvarchar(21) NOT NULL,
    [Skills] nvarchar(500) NULL,
    [AvailabilityStatus] nvarchar(50) NULL,
    [CPR] nvarchar(20) NULL,
    [EmergencyContact] nvarchar(200) NULL,
    [Occupation] nvarchar(200) NULL,
    [RegistrationDate] datetime2 NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [RoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_RoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RoleClaims_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_UserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_UserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_UserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_UserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_UserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Leases] (
    [LeaseId] int NOT NULL IDENTITY,
    [UnitId] int NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [ApplicationDate] datetime2 NOT NULL,
    [StartDate] datetime2 NULL,
    [EndDate] datetime2 NULL,
    [MonthlyRent] decimal(18,2) NOT NULL,
    [SecurityDeposit] decimal(18,2) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [RejectionReason] nvarchar(500) NULL,
    [ScreeningNotes] nvarchar(1000) NULL,
    CONSTRAINT [PK_Leases] PRIMARY KEY ([LeaseId]),
    CONSTRAINT [FK_Leases_Users_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Payments] (
    [PaymentId] int NOT NULL IDENTITY,
    [LeaseId] int NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [AmountDue] decimal(18,2) NOT NULL,
    [AmountPaid] decimal(18,2) NOT NULL,
    [PaymentDate] datetime2 NULL,
    [Status] nvarchar(50) NOT NULL,
    [PaymentMethod] nvarchar(100) NULL,
    [ReceiptNumber] nvarchar(100) NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([PaymentId]),
    CONSTRAINT [FK_Payments_Leases_LeaseId] FOREIGN KEY ([LeaseId]) REFERENCES [Leases] ([LeaseId]) ON DELETE CASCADE
);

CREATE TABLE [Units] (
    [UnitId] int NOT NULL IDENTITY,
    [BuildingId] int NOT NULL,
    [UnitNumber] nvarchar(50) NOT NULL,
    [Type] nvarchar(50) NOT NULL,
    [Bedrooms] int NULL,
    [Bathrooms] int NULL,
    [SizeInSqFt] decimal(18,2) NOT NULL,
    [MonthlyRent] decimal(18,2) NOT NULL,
    [Amenities] nvarchar(1000) NULL,
    [AvailabilityStatus] nvarchar(50) NOT NULL,
    [CurrentLeaseId] int NULL,
    CONSTRAINT [PK_Units] PRIMARY KEY ([UnitId]),
    CONSTRAINT [FK_Units_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([BuildingId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Units_Leases_CurrentLeaseId] FOREIGN KEY ([CurrentLeaseId]) REFERENCES [Leases] ([LeaseId]) ON DELETE SET NULL
);

CREATE TABLE [MaintenanceRequests] (
    [RequestId] int NOT NULL IDENTITY,
    [TicketNumber] nvarchar(20) NOT NULL,
    [TenantId] nvarchar(450) NOT NULL,
    [UnitId] int NOT NULL,
    [Category] nvarchar(100) NOT NULL,
    [Priority] nvarchar(50) NOT NULL,
    [Description] nvarchar(2000) NOT NULL,
    [SubmittedDate] datetime2 NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [AssignedStaffId] nvarchar(450) NULL,
    [ResolutionNotes] nvarchar(2000) NULL,
    [ClosedDate] datetime2 NULL,
    CONSTRAINT [PK_MaintenanceRequests] PRIMARY KEY ([RequestId]),
    CONSTRAINT [FK_MaintenanceRequests_Units_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [Units] ([UnitId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_MaintenanceRequests_Users_AssignedStaffId] FOREIGN KEY ([AssignedStaffId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_MaintenanceRequests_Users_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConcurrencyStamp', N'Name', N'NormalizedName') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] ON;
INSERT INTO [Roles] ([Id], [ConcurrencyStamp], [Name], [NormalizedName])
VALUES (N'1', N'abd50d47-34de-4624-8f09-3db962e84e59', N'Tenant', N'TENANT'),
(N'2', N'a9811772-0b21-4ca7-9a68-3e4e3839323d', N'PropertyManager', N'PROPERTYMANAGER'),
(N'3', N'a171d661-9500-4280-8d81-0d43690d26d0', N'MaintenanceStaff', N'MAINTENANCESTAFF');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConcurrencyStamp', N'Name', N'NormalizedName') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] OFF;

CREATE INDEX [IX_Leases_TenantId] ON [Leases] ([TenantId]);

CREATE INDEX [IX_Leases_UnitId] ON [Leases] ([UnitId]);

CREATE INDEX [IX_MaintenanceRequests_AssignedStaffId] ON [MaintenanceRequests] ([AssignedStaffId]);

CREATE INDEX [IX_MaintenanceRequests_TenantId] ON [MaintenanceRequests] ([TenantId]);

CREATE UNIQUE INDEX [IX_MaintenanceRequests_TicketNumber] ON [MaintenanceRequests] ([TicketNumber]);

CREATE INDEX [IX_MaintenanceRequests_UnitId] ON [MaintenanceRequests] ([UnitId]);

CREATE INDEX [IX_Payments_LeaseId] ON [Payments] ([LeaseId]);

CREATE INDEX [IX_RoleClaims_RoleId] ON [RoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [Roles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_Units_BuildingId] ON [Units] ([BuildingId]);

CREATE UNIQUE INDEX [IX_Units_CurrentLeaseId] ON [Units] ([CurrentLeaseId]) WHERE [CurrentLeaseId] IS NOT NULL;

CREATE INDEX [IX_UserClaims_UserId] ON [UserClaims] ([UserId]);

CREATE INDEX [IX_UserLogins_UserId] ON [UserLogins] ([UserId]);

CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [Users] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

ALTER TABLE [Leases] ADD CONSTRAINT [FK_Leases_Units_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [Units] ([UnitId]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260421213423_InitialCreate', N'9.0.0');

COMMIT;
GO

