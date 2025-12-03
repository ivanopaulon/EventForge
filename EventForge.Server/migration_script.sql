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
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [Banks] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(20) NULL,
        [SwiftBic] nvarchar(20) NULL,
        [Branch] nvarchar(100) NULL,
        [Address] nvarchar(200) NULL,
        [Country] nvarchar(50) NULL,
        [Phone] nvarchar(30) NULL,
        [Email] nvarchar(100) NULL,
        [Notes] nvarchar(200) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Banks] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [Brands] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [Website] nvarchar(500) NULL,
        [Country] nvarchar(100) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Brands] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [BusinessParties] (
        [Id] uniqueidentifier NOT NULL,
        [PartyType] int NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [TaxCode] nvarchar(20) NULL,
        [VatNumber] nvarchar(20) NULL,
        [SdiCode] nvarchar(10) NULL,
        [Pec] nvarchar(100) NULL,
        [Notes] nvarchar(500) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_BusinessParties] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [ChatThreads] (
        [Id] uniqueidentifier NOT NULL,
        [Type] int NOT NULL,
        [Name] nvarchar(100) NULL,
        [Description] nvarchar(500) NULL,
        [IsPrivate] bit NOT NULL,
        [PreferredLocale] nvarchar(10) NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ChatThreads] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [ClassificationNodes] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(30) NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(200) NULL,
        [Type] int NOT NULL,
        [Status] int NOT NULL,
        [Level] int NOT NULL,
        [Order] int NOT NULL,
        [ParentId] uniqueidentifier NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ClassificationNodes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ClassificationNodes_ClassificationNodes_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [ClassificationNodes] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [DocumentReferences] (
        [Id] uniqueidentifier NOT NULL,
        [OwnerId] uniqueidentifier NULL,
        [OwnerType] nvarchar(50) NULL,
        [FileName] nvarchar(255) NOT NULL,
        [Type] int NOT NULL,
        [SubType] int NOT NULL,
        [MimeType] nvarchar(100) NOT NULL,
        [StorageKey] nvarchar(500) NOT NULL,
        [Url] nvarchar(1000) NULL,
        [ThumbnailStorageKey] nvarchar(500) NULL,
        [Expiry] datetime2 NULL,
        [FileSizeBytes] bigint NOT NULL,
        [Title] nvarchar(200) NULL,
        [Notes] nvarchar(500) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DocumentReferences] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [EntityChangeLogs] (
        [Id] uniqueidentifier NOT NULL,
        [EntityName] nvarchar(100) NOT NULL,
        [EntityDisplayName] nvarchar(100) NULL,
        [EntityId] uniqueidentifier NOT NULL,
        [PropertyName] nvarchar(100) NOT NULL,
        [OperationType] nvarchar(20) NOT NULL,
        [OldValue] nvarchar(max) NULL,
        [NewValue] nvarchar(max) NULL,
        [ChangedBy] nvarchar(100) NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EntityChangeLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [Events] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [ShortDescription] nvarchar(200) NOT NULL,
        [LongDescription] nvarchar(max) NOT NULL,
        [Location] nvarchar(200) NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [Capacity] int NOT NULL,
        [Status] int NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Events] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [Licenses] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [DisplayName] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [MaxUsers] int NOT NULL,
        [MaxApiCallsPerMonth] int NOT NULL,
        [TierLevel] int NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Licenses] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [LogEntries] (
        [Id] int NOT NULL IDENTITY,
        [TimeStamp] datetime2 NOT NULL,
        [Level] nvarchar(max) NOT NULL,
        [Message] nvarchar(max) NOT NULL,
        [Exception] nvarchar(max) NULL,
        [MachineName] nvarchar(max) NULL,
        [UserName] nvarchar(max) NULL,
        CONSTRAINT [PK_LogEntries] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [NoteFlags] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Color] nvarchar(7) NULL,
        [Icon] nvarchar(50) NULL,
        [IsActive] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_NoteFlags] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] uniqueidentifier NOT NULL,
        [SenderId] uniqueidentifier NULL,
        [Type] int NOT NULL,
        [Priority] int NOT NULL,
        [Status] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Message] nvarchar(1000) NOT NULL,
        [ActionUrl] nvarchar(500) NULL,
        [IconUrl] nvarchar(500) NULL,
        [Locale] nvarchar(10) NULL,
        [LocalizationParamsJson] nvarchar(max) NULL,
        [ExpiresAt] datetime2 NULL,
        [ReadAt] datetime2 NULL,
        [AcknowledgedAt] datetime2 NULL,
        [SilencedAt] datetime2 NULL,
        [ArchivedAt] datetime2 NULL,
        [IsArchived] bit NOT NULL,
        [MetadataJson] nvarchar(max) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [PaymentMethods] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Icon] nvarchar(50) NULL,
        [IsActive] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        [RequiresIntegration] bit NOT NULL,
        [IntegrationConfig] nvarchar(2000) NULL,
        [AllowsChange] bit NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_PaymentMethods] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [PaymentTerms] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(250) NULL,
        [DueDays] int NOT NULL,
        [PaymentMethod] int NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_PaymentTerms] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [Permissions] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [DisplayName] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [Category] nvarchar(100) NOT NULL,
        [Resource] nvarchar(100) NULL,
        [Action] nvarchar(50) NOT NULL,
        [IsSystemPermission] bit NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [Promotions] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [MinOrderAmount] decimal(18,6) NULL,
        [MaxUses] int NULL,
        [CouponCode] nvarchar(50) NULL,
        [Priority] int NOT NULL,
        [IsCombinable] bit NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Promotions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [DisplayName] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [IsSystemRole] bit NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [SaleSessions] (
        [Id] uniqueidentifier NOT NULL,
        [OperatorId] uniqueidentifier NOT NULL,
        [PosId] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NULL,
        [SaleType] nvarchar(50) NULL,
        [Status] int NOT NULL,
        [OriginalTotal] decimal(18,6) NOT NULL,
        [DiscountAmount] decimal(18,6) NOT NULL,
        [FinalTotal] decimal(18,6) NOT NULL,
        [TaxAmount] decimal(18,6) NOT NULL,
        [Currency] nvarchar(3) NOT NULL,
        [TableId] uniqueidentifier NULL,
        [DocumentId] uniqueidentifier NULL,
        [ClosedAt] datetime2 NULL,
        [CouponCodes] nvarchar(500) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_SaleSessions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [Stations] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(200) NULL,
        [Status] int NOT NULL,
        [Location] nvarchar(50) NULL,
        [SortOrder] int NOT NULL,
        [Notes] nvarchar(200) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Stations] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [StorageFacilities] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(30) NOT NULL,
        [Address] nvarchar(200) NULL,
        [Phone] nvarchar(30) NULL,
        [Email] nvarchar(100) NULL,
        [Manager] nvarchar(100) NULL,
        [IsFiscal] bit NOT NULL,
        [Notes] nvarchar(500) NULL,
        [AreaSquareMeters] int NULL,
        [Capacity] int NULL,
        [IsRefrigerated] bit NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_StorageFacilities] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [StoreUserPrivileges] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Category] nvarchar(50) NULL,
        [Description] nvarchar(200) NULL,
        [Status] int NOT NULL,
        [SortOrder] int NOT NULL,
        [IsSystemPrivilege] bit NOT NULL,
        [DefaultAssigned] bit NOT NULL,
        [Resource] nvarchar(100) NULL,
        [Action] nvarchar(50) NULL,
        [PermissionKey] nvarchar(200) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_StoreUserPrivileges] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [SystemConfigurations] (
        [Id] uniqueidentifier NOT NULL,
        [Key] nvarchar(100) NOT NULL,
        [Value] nvarchar(max) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Category] nvarchar(50) NOT NULL,
        [IsEncrypted] bit NOT NULL,
        [RequiresRestart] bit NOT NULL,
        [IsReadOnly] bit NOT NULL,
        [DefaultValue] nvarchar(1000) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_SystemConfigurations] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [Tenants] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [DisplayName] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Domain] nvarchar(100) NULL,
        [ContactEmail] nvarchar(256) NOT NULL,
        [MaxUsers] int NOT NULL,
        [SubscriptionExpiresAt] datetime2 NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [UMs] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Symbol] nvarchar(10) NOT NULL,
        [Description] nvarchar(200) NULL,
        [IsDefault] bit NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_UMs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [VatNatures] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(10) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_VatNatures] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [Models] (
        [Id] uniqueidentifier NOT NULL,
        [BrandId] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [ManufacturerPartNumber] nvarchar(100) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Models] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Models_Brands_BrandId] FOREIGN KEY ([BrandId]) REFERENCES [Brands] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [Addresses] (
        [Id] uniqueidentifier NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [OwnerType] nvarchar(50) NOT NULL,
        [AddressType] int NOT NULL,
        [Street] nvarchar(100) NULL,
        [City] nvarchar(50) NULL,
        [ZipCode] nvarchar(10) NULL,
        [Province] nvarchar(50) NULL,
        [Country] nvarchar(50) NULL,
        [Notes] nvarchar(100) NULL,
        [BankId] uniqueidentifier NULL,
        [BusinessPartyId] uniqueidentifier NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Addresses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Addresses_Banks_BankId] FOREIGN KEY ([BankId]) REFERENCES [Banks] ([Id]),
        CONSTRAINT [FK_Addresses_BusinessParties_BusinessPartyId] FOREIGN KEY ([BusinessPartyId]) REFERENCES [BusinessParties] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [References] (
        [Id] uniqueidentifier NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [OwnerType] nvarchar(50) NOT NULL,
        [FirstName] nvarchar(50) NOT NULL,
        [LastName] nvarchar(50) NOT NULL,
        [Department] nvarchar(50) NULL,
        [Notes] nvarchar(100) NULL,
        [BankId] uniqueidentifier NULL,
        [BusinessPartyId] uniqueidentifier NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_References] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_References_Banks_BankId] FOREIGN KEY ([BankId]) REFERENCES [Banks] ([Id]),
        CONSTRAINT [FK_References_BusinessParties_BusinessPartyId] FOREIGN KEY ([BusinessPartyId]) REFERENCES [BusinessParties] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [ChatMembers] (
        [Id] uniqueidentifier NOT NULL,
        [ChatThreadId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Role] int NOT NULL,
        [JoinedAt] datetime2 NOT NULL,
        [LastSeenAt] datetime2 NULL,
        [IsOnline] bit NOT NULL,
        [IsMuted] bit NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ChatMembers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChatMembers_ChatThreads_ChatThreadId] FOREIGN KEY ([ChatThreadId]) REFERENCES [ChatThreads] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [ChatMessages] (
        [Id] uniqueidentifier NOT NULL,
        [ChatThreadId] uniqueidentifier NOT NULL,
        [SenderId] uniqueidentifier NOT NULL,
        [Content] nvarchar(4000) NULL,
        [ReplyToMessageId] uniqueidentifier NULL,
        [Status] int NOT NULL,
        [SentAt] datetime2 NOT NULL,
        [DeliveredAt] datetime2 NULL,
        [ReadAt] datetime2 NULL,
        [EditedAt] datetime2 NULL,
        [DeletedAt] datetime2 NULL,
        [IsEdited] bit NOT NULL,
        [Locale] nvarchar(10) NULL,
        [MetadataJson] nvarchar(max) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChatMessages_ChatMessages_ReplyToMessageId] FOREIGN KEY ([ReplyToMessageId]) REFERENCES [ChatMessages] ([Id]),
        CONSTRAINT [FK_ChatMessages_ChatThreads_ChatThreadId] FOREIGN KEY ([ChatThreadId]) REFERENCES [ChatThreads] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [StorePoses] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Description] nvarchar(200) NULL,
        [Status] int NOT NULL,
        [Location] nvarchar(100) NULL,
        [LastOpenedAt] datetime2 NULL,
        [Notes] nvarchar(200) NULL,
        [ImageDocumentId] uniqueidentifier NULL,
        [TerminalIdentifier] nvarchar(100) NULL,
        [IPAddress] nvarchar(45) NULL,
        [IsOnline] bit NOT NULL,
        [LastSyncAt] datetime2 NULL,
        [LocationLatitude] decimal(18,6) NULL,
        [LocationLongitude] decimal(18,6) NULL,
        [CurrencyCode] nvarchar(3) NULL,
        [TimeZone] nvarchar(50) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_StorePoses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StorePoses_DocumentReferences_ImageDocumentId] FOREIGN KEY ([ImageDocumentId]) REFERENCES [DocumentReferences] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [StoreUserGroups] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Description] nvarchar(200) NULL,
        [Status] int NOT NULL,
        [LogoDocumentId] uniqueidentifier NULL,
        [ColorHex] nvarchar(7) NULL,
        [IsSystemGroup] bit NOT NULL,
        [IsDefault] bit NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_StoreUserGroups] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StoreUserGroups_DocumentReferences_LogoDocumentId] FOREIGN KEY ([LogoDocumentId]) REFERENCES [DocumentReferences] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [PriceLists] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [ValidFrom] datetime2 NULL,
        [ValidTo] datetime2 NULL,
        [Notes] nvarchar(1000) NOT NULL,
        [Status] int NOT NULL,
        [IsDefault] bit NOT NULL,
        [Priority] int NOT NULL,
        [EventId] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_PriceLists] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PriceLists_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [LicenseFeatures] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [DisplayName] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [Category] nvarchar(100) NOT NULL,
        [LicenseId] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_LicenseFeatures] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LicenseFeatures_Licenses_LicenseId] FOREIGN KEY ([LicenseId]) REFERENCES [Licenses] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [NotificationRecipients] (
        [Id] uniqueidentifier NOT NULL,
        [NotificationId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [ReadAt] datetime2 NULL,
        [AcknowledgedAt] datetime2 NULL,
        [SilencedAt] datetime2 NULL,
        [SilencedUntil] datetime2 NULL,
        [ArchivedAt] datetime2 NULL,
        [IsArchived] bit NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_NotificationRecipients] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_NotificationRecipients_Notifications_NotificationId] FOREIGN KEY ([NotificationId]) REFERENCES [Notifications] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [BusinessPartyAccountings] (
        [Id] uniqueidentifier NOT NULL,
        [BusinessPartyId] uniqueidentifier NOT NULL,
        [Iban] nvarchar(34) NULL,
        [BankId] uniqueidentifier NULL,
        [PaymentTermId] uniqueidentifier NULL,
        [CreditLimit] decimal(18,6) NULL,
        [Notes] nvarchar(100) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_BusinessPartyAccountings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BusinessPartyAccountings_Banks_BankId] FOREIGN KEY ([BankId]) REFERENCES [Banks] ([Id]),
        CONSTRAINT [FK_BusinessPartyAccountings_PaymentTerms_PaymentTermId] FOREIGN KEY ([PaymentTermId]) REFERENCES [PaymentTerms] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [PromotionRules] (
        [Id] uniqueidentifier NOT NULL,
        [PromotionId] uniqueidentifier NOT NULL,
        [RuleType] int NOT NULL,
        [DiscountPercentage] decimal(5,2) NULL,
        [DiscountAmount] decimal(18,6) NULL,
        [RequiredQuantity] int NULL,
        [FreeQuantity] int NULL,
        [FixedPrice] decimal(18,6) NULL,
        [MinOrderAmount] decimal(18,6) NULL,
        [CategoryIds] nvarchar(max) NULL,
        [CustomerGroupIds] nvarchar(max) NULL,
        [SalesChannels] nvarchar(max) NULL,
        [ValidDays] nvarchar(max) NULL,
        [StartTime] time NULL,
        [EndTime] time NULL,
        [IsCombinable] bit NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_PromotionRules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PromotionRules_Promotions_PromotionId] FOREIGN KEY ([PromotionId]) REFERENCES [Promotions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [RolePermissions] (
        [Id] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        [PermissionId] uniqueidentifier NOT NULL,
        [GrantedAt] datetime2 NOT NULL,
        [GrantedBy] nvarchar(100) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [SaleItems] (
        [Id] uniqueidentifier NOT NULL,
        [SaleSessionId] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [ProductCode] nvarchar(50) NULL,
        [ProductName] nvarchar(200) NOT NULL,
        [UnitPrice] decimal(18,6) NOT NULL,
        [Quantity] decimal(18,6) NOT NULL,
        [DiscountPercent] decimal(5,2) NOT NULL,
        [TotalAmount] decimal(18,6) NOT NULL,
        [TaxRate] decimal(5,2) NOT NULL,
        [TaxAmount] decimal(18,6) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [IsService] bit NOT NULL,
        [PromotionId] uniqueidentifier NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_SaleItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SaleItems_SaleSessions_SaleSessionId] FOREIGN KEY ([SaleSessionId]) REFERENCES [SaleSessions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [SalePayments] (
        [Id] uniqueidentifier NOT NULL,
        [SaleSessionId] uniqueidentifier NOT NULL,
        [PaymentMethodId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,6) NOT NULL,
        [Status] int NOT NULL,
        [TransactionReference] nvarchar(200) NULL,
        [PaymentDate] datetime2 NULL,
        [Notes] nvarchar(500) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_SalePayments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SalePayments_PaymentMethods_PaymentMethodId] FOREIGN KEY ([PaymentMethodId]) REFERENCES [PaymentMethods] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_SalePayments_SaleSessions_SaleSessionId] FOREIGN KEY ([SaleSessionId]) REFERENCES [SaleSessions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [SessionNotes] (
        [Id] uniqueidentifier NOT NULL,
        [SaleSessionId] uniqueidentifier NOT NULL,
        [NoteFlagId] uniqueidentifier NOT NULL,
        [Text] nvarchar(1000) NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_SessionNotes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SessionNotes_NoteFlags_NoteFlagId] FOREIGN KEY ([NoteFlagId]) REFERENCES [NoteFlags] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_SessionNotes_SaleSessions_SaleSessionId] FOREIGN KEY ([SaleSessionId]) REFERENCES [SaleSessions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [TableSessions] (
        [Id] uniqueidentifier NOT NULL,
        [TableNumber] nvarchar(50) NOT NULL,
        [TableName] nvarchar(100) NULL,
        [Capacity] int NOT NULL,
        [Status] int NOT NULL,
        [CurrentSaleSessionId] uniqueidentifier NULL,
        [Area] nvarchar(100) NULL,
        [PositionX] int NULL,
        [PositionY] int NULL,
        [IsActive] bit NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_TableSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TableSessions_SaleSessions_CurrentSaleSessionId] FOREIGN KEY ([CurrentSaleSessionId]) REFERENCES [SaleSessions] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [Printers] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Type] nvarchar(30) NOT NULL,
        [Model] nvarchar(50) NULL,
        [Location] nvarchar(50) NULL,
        [Address] nvarchar(100) NULL,
        [Status] int NOT NULL,
        [StationId] uniqueidentifier NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Printers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Printers_Stations_StationId] FOREIGN KEY ([StationId]) REFERENCES [Stations] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [DocumentTypes] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Code] nvarchar(10) NOT NULL,
        [IsStockIncrease] bit NOT NULL,
        [DefaultWarehouseId] uniqueidentifier NULL,
        [IsFiscal] bit NOT NULL,
        [RequiredPartyType] int NOT NULL,
        [Notes] nvarchar(200) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DocumentTypes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentTypes_StorageFacilities_DefaultWarehouseId] FOREIGN KEY ([DefaultWarehouseId]) REFERENCES [StorageFacilities] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [StorageLocations] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(30) NOT NULL,
        [Description] nvarchar(100) NULL,
        [WarehouseId] uniqueidentifier NOT NULL,
        [Capacity] int NULL,
        [Occupancy] int NULL,
        [LastInventoryDate] datetime2 NULL,
        [IsRefrigerated] bit NOT NULL,
        [Notes] nvarchar(200) NULL,
        [Zone] nvarchar(20) NULL,
        [Floor] nvarchar(10) NULL,
        [Row] nvarchar(10) NULL,
        [Column] nvarchar(10) NULL,
        [Level] nvarchar(10) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_StorageLocations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StorageLocations_StorageFacilities_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [StorageFacilities] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [TenantLicenses] (
        [Id] uniqueidentifier NOT NULL,
        [TargetTenantId] uniqueidentifier NOT NULL,
        [LicenseId] uniqueidentifier NOT NULL,
        [StartsAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NULL,
        [IsAssignmentActive] bit NOT NULL,
        [ApiCallsThisMonth] int NOT NULL,
        [ApiCallsResetAt] datetime2 NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_TenantLicenses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TenantLicenses_Licenses_LicenseId] FOREIGN KEY ([LicenseId]) REFERENCES [Licenses] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TenantLicenses_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [Username] nvarchar(100) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [PasswordSalt] nvarchar(max) NOT NULL,
        [MustChangePassword] bit NOT NULL,
        [PasswordChangedAt] datetime2 NULL,
        [FailedLoginAttempts] int NOT NULL,
        [LockedUntil] datetime2 NULL,
        [LastLoginAt] datetime2 NULL,
        [LastFailedLoginAt] datetime2 NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [VatRates] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Percentage] decimal(5,2) NOT NULL,
        [Status] int NOT NULL,
        [ValidFrom] datetime2 NULL,
        [ValidTo] datetime2 NULL,
        [Notes] nvarchar(200) NULL,
        [VatNatureId] uniqueidentifier NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_VatRates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VatRates_VatNatures_VatNatureId] FOREIGN KEY ([VatNatureId]) REFERENCES [VatNatures] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [Contacts] (
        [Id] uniqueidentifier NOT NULL,
        [OwnerId] uniqueidentifier NOT NULL,
        [OwnerType] nvarchar(50) NOT NULL,
        [ContactType] int NOT NULL,
        [Value] nvarchar(100) NOT NULL,
        [Purpose] int NOT NULL,
        [Relationship] nvarchar(50) NULL,
        [IsPrimary] bit NOT NULL,
        [Notes] nvarchar(100) NULL,
        [BankId] uniqueidentifier NULL,
        [BusinessPartyId] uniqueidentifier NULL,
        [ReferenceId] uniqueidentifier NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Contacts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Contacts_Banks_BankId] FOREIGN KEY ([BankId]) REFERENCES [Banks] ([Id]),
        CONSTRAINT [FK_Contacts_BusinessParties_BusinessPartyId] FOREIGN KEY ([BusinessPartyId]) REFERENCES [BusinessParties] ([Id]),
        CONSTRAINT [FK_Contacts_References_ReferenceId] FOREIGN KEY ([ReferenceId]) REFERENCES [References] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [MessageAttachments] (
        [Id] uniqueidentifier NOT NULL,
        [MessageId] uniqueidentifier NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [OriginalFileName] nvarchar(255) NULL,
        [FileSize] bigint NOT NULL,
        [ContentType] nvarchar(100) NOT NULL,
        [MediaType] int NOT NULL,
        [FileUrl] nvarchar(500) NULL,
        [ThumbnailUrl] nvarchar(500) NULL,
        [UploadedAt] datetime2 NOT NULL,
        [UploadedBy] uniqueidentifier NOT NULL,
        [MediaMetadataJson] nvarchar(max) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_MessageAttachments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MessageAttachments_ChatMessages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [ChatMessages] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [MessageReadReceipts] (
        [Id] uniqueidentifier NOT NULL,
        [MessageId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [ReadAt] datetime2 NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_MessageReadReceipts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MessageReadReceipts_ChatMessages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [ChatMessages] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [StoreUserGroupStoreUserPrivilege] (
        [GroupsId] uniqueidentifier NOT NULL,
        [PrivilegesId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_StoreUserGroupStoreUserPrivilege] PRIMARY KEY ([GroupsId], [PrivilegesId]),
        CONSTRAINT [FK_StoreUserGroupStoreUserPrivilege_StoreUserGroups_GroupsId] FOREIGN KEY ([GroupsId]) REFERENCES [StoreUserGroups] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_StoreUserGroupStoreUserPrivilege_StoreUserPrivileges_PrivilegesId] FOREIGN KEY ([PrivilegesId]) REFERENCES [StoreUserPrivileges] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [StoreUsers] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Username] nvarchar(50) NOT NULL,
        [Email] nvarchar(100) NULL,
        [PasswordHash] nvarchar(200) NULL,
        [Role] nvarchar(50) NULL,
        [Status] int NOT NULL,
        [LastLoginAt] datetime2 NULL,
        [Notes] nvarchar(200) NULL,
        [CashierGroupId] uniqueidentifier NULL,
        [PhotoDocumentId] uniqueidentifier NULL,
        [PhotoConsent] bit NOT NULL,
        [PhotoConsentAt] datetime2 NULL,
        [PhoneNumber] nvarchar(20) NULL,
        [LastPasswordChangedAt] datetime2 NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [ExternalId] nvarchar(max) NULL,
        [IsOnShift] bit NOT NULL,
        [ShiftId] uniqueidentifier NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_StoreUsers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StoreUsers_DocumentReferences_PhotoDocumentId] FOREIGN KEY ([PhotoDocumentId]) REFERENCES [DocumentReferences] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StoreUsers_StoreUserGroups_CashierGroupId] FOREIGN KEY ([CashierGroupId]) REFERENCES [StoreUserGroups] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [LicenseFeaturePermissions] (
        [Id] uniqueidentifier NOT NULL,
        [LicenseFeatureId] uniqueidentifier NOT NULL,
        [PermissionId] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_LicenseFeaturePermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LicenseFeaturePermissions_LicenseFeatures_LicenseFeatureId] FOREIGN KEY ([LicenseFeatureId]) REFERENCES [LicenseFeatures] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_LicenseFeaturePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [TableReservations] (
        [Id] uniqueidentifier NOT NULL,
        [TableId] uniqueidentifier NOT NULL,
        [CustomerName] nvarchar(200) NOT NULL,
        [PhoneNumber] nvarchar(20) NULL,
        [NumberOfGuests] int NOT NULL,
        [ReservationDateTime] datetime2 NOT NULL,
        [DurationMinutes] int NULL,
        [Status] int NOT NULL,
        [SpecialRequests] nvarchar(1000) NULL,
        [ConfirmedAt] datetime2 NULL,
        [ArrivedAt] datetime2 NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_TableReservations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TableReservations_TableSessions_TableId] FOREIGN KEY ([TableId]) REFERENCES [TableSessions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [DocumentCounters] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentTypeId] uniqueidentifier NOT NULL,
        [Series] nvarchar(10) NOT NULL,
        [CurrentValue] int NOT NULL,
        [Year] int NULL,
        [Prefix] nvarchar(10) NULL,
        [PaddingLength] int NOT NULL,
        [FormatPattern] nvarchar(50) NULL,
        [ResetOnYearChange] bit NOT NULL,
        [Notes] nvarchar(200) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DocumentCounters] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentCounters_DocumentTypes_DocumentTypeId] FOREIGN KEY ([DocumentTypeId]) REFERENCES [DocumentTypes] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [DocumentRetentionPolicies] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentTypeId] uniqueidentifier NOT NULL,
        [RetentionDays] int NULL,
        [AutoDeleteEnabled] bit NOT NULL,
        [GracePeriodDays] int NOT NULL,
        [ArchiveInsteadOfDelete] bit NOT NULL,
        [Notes] nvarchar(500) NULL,
        [Reason] nvarchar(200) NULL,
        [LastAppliedAt] datetime2 NULL,
        [DocumentsDeleted] int NOT NULL,
        [DocumentsArchived] int NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DocumentRetentionPolicies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentRetentionPolicies_DocumentTypes_DocumentTypeId] FOREIGN KEY ([DocumentTypeId]) REFERENCES [DocumentTypes] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [DocumentTemplates] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [DocumentTypeId] uniqueidentifier NOT NULL,
        [Category] nvarchar(50) NULL,
        [IsPublic] bit NOT NULL,
        [Owner] nvarchar(100) NULL,
        [TemplateConfiguration] nvarchar(max) NULL,
        [DefaultBusinessPartyId] uniqueidentifier NULL,
        [DefaultWarehouseId] uniqueidentifier NULL,
        [DefaultPaymentMethod] nvarchar(30) NULL,
        [DefaultDueDateDays] int NULL,
        [DefaultNotes] nvarchar(500) NULL,
        [UsageCount] int NOT NULL,
        [LastUsedAt] datetime2 NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DocumentTemplates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentTemplates_DocumentTypes_DocumentTypeId] FOREIGN KEY ([DocumentTypeId]) REFERENCES [DocumentTypes] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [DocumentWorkflows] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [DocumentTypeId] uniqueidentifier NULL,
        [Category] nvarchar(50) NULL,
        [Priority] int NOT NULL,
        [IsActive] bit NOT NULL,
        [IsDefault] bit NOT NULL,
        [WorkflowConfiguration] nvarchar(max) NULL,
        [TriggerConditions] nvarchar(2000) NULL,
        [MaxProcessingTimeHours] int NULL,
        [EscalationRules] nvarchar(1000) NULL,
        [NotificationSettings] nvarchar(1000) NULL,
        [AutoApprovalRules] nvarchar(1000) NULL,
        [WorkflowVersion] int NOT NULL,
        [UsageCount] int NOT NULL,
        [AverageCompletionTimeHours] decimal(18,6) NULL,
        [SuccessRate] decimal(18,6) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DocumentWorkflows] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentWorkflows_DocumentTypes_DocumentTypeId] FOREIGN KEY ([DocumentTypeId]) REFERENCES [DocumentTypes] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [ProjectOrders] (
        [Id] uniqueidentifier NOT NULL,
        [OrderNumber] nvarchar(50) NOT NULL,
        [ProjectName] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [CustomerId] uniqueidentifier NULL,
        [ProjectType] int NOT NULL,
        [Status] int NOT NULL,
        [Priority] int NOT NULL,
        [StartDate] datetime2 NULL,
        [PlannedEndDate] datetime2 NULL,
        [ActualEndDate] datetime2 NULL,
        [ProjectManager] nvarchar(100) NULL,
        [EstimatedBudget] decimal(18,6) NULL,
        [ActualCost] decimal(18,6) NULL,
        [EstimatedHours] decimal(18,6) NULL,
        [ActualHours] decimal(18,6) NULL,
        [ProgressPercentage] decimal(18,6) NOT NULL,
        [StorageLocationId] uniqueidentifier NULL,
        [DocumentId] uniqueidentifier NULL,
        [ExternalReference] nvarchar(100) NULL,
        [Notes] nvarchar(2000) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ProjectOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectOrders_BusinessParties_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [BusinessParties] ([Id]),
        CONSTRAINT [FK_ProjectOrders_StorageLocations_StorageLocationId] FOREIGN KEY ([StorageLocationId]) REFERENCES [StorageLocations] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [AdminTenants] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [ManagedTenantId] uniqueidentifier NOT NULL,
        [AccessLevel] int NOT NULL,
        [GrantedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_AdminTenants] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AdminTenants_Tenants_ManagedTenantId] FOREIGN KEY ([ManagedTenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AdminTenants_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [AuditTrails] (
        [Id] uniqueidentifier NOT NULL,
        [PerformedByUserId] uniqueidentifier NOT NULL,
        [OperationType] int NOT NULL,
        [SourceTenantId] uniqueidentifier NULL,
        [TargetTenantId] uniqueidentifier NULL,
        [TargetUserId] uniqueidentifier NULL,
        [SessionId] nvarchar(100) NULL,
        [IpAddress] nvarchar(45) NULL,
        [UserAgent] nvarchar(500) NULL,
        [Details] nvarchar(max) NULL,
        [WasSuccessful] bit NOT NULL,
        [ErrorMessage] nvarchar(1000) NULL,
        [PerformedAt] datetime2 NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_AuditTrails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditTrails_Tenants_SourceTenantId] FOREIGN KEY ([SourceTenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AuditTrails_Tenants_TargetTenantId] FOREIGN KEY ([TargetTenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AuditTrails_Users_PerformedByUserId] FOREIGN KEY ([PerformedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AuditTrails_Users_TargetUserId] FOREIGN KEY ([TargetUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [BackupOperations] (
        [Id] uniqueidentifier NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [ProgressPercentage] int NOT NULL,
        [CurrentOperation] nvarchar(200) NULL,
        [StartedAt] datetime2 NOT NULL,
        [CompletedAt] datetime2 NULL,
        [FilePath] nvarchar(500) NULL,
        [FileSizeBytes] bigint NULL,
        [ErrorMessage] nvarchar(1000) NULL,
        [StartedByUserId] uniqueidentifier NOT NULL,
        [Description] nvarchar(500) NULL,
        [IncludeAuditLogs] bit NOT NULL,
        [IncludeUserData] bit NOT NULL,
        [IncludeConfiguration] bit NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_BackupOperations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BackupOperations_Users_StartedByUserId] FOREIGN KEY ([StartedByUserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [LoginAudits] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NULL,
        [Username] nvarchar(100) NOT NULL,
        [EventType] nvarchar(50) NOT NULL,
        [IpAddress] nvarchar(45) NULL,
        [UserAgent] nvarchar(500) NULL,
        [EventTime] datetime2 NOT NULL,
        [Success] bit NOT NULL,
        [FailureReason] nvarchar(500) NULL,
        [SessionId] nvarchar(100) NULL,
        [SessionDuration] time NULL,
        [Metadata] nvarchar(max) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_LoginAudits] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LoginAudits_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [UserRoles] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        [GrantedAt] datetime2 NOT NULL,
        [GrantedBy] nvarchar(100) NULL,
        [ExpiresAt] datetime2 NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [ShortDescription] nvarchar(50) NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [Code] nvarchar(max) NOT NULL,
        [ImageUrl] nvarchar(500) NOT NULL,
        [ImageDocumentId] uniqueidentifier NULL,
        [Status] int NOT NULL,
        [IsVatIncluded] bit NOT NULL,
        [DefaultPrice] decimal(18,6) NULL,
        [VatRateId] uniqueidentifier NULL,
        [UnitOfMeasureId] uniqueidentifier NULL,
        [CategoryNodeId] uniqueidentifier NULL,
        [FamilyNodeId] uniqueidentifier NULL,
        [GroupNodeId] uniqueidentifier NULL,
        [StationId] uniqueidentifier NULL,
        [IsBundle] bit NOT NULL,
        [BrandId] uniqueidentifier NULL,
        [ModelId] uniqueidentifier NULL,
        [PreferredSupplierId] uniqueidentifier NULL,
        [ReorderPoint] decimal(18,6) NULL,
        [SafetyStock] decimal(18,6) NULL,
        [TargetStockLevel] decimal(18,6) NULL,
        [AverageDailyDemand] decimal(18,6) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Products_Brands_BrandId] FOREIGN KEY ([BrandId]) REFERENCES [Brands] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Products_ClassificationNodes_CategoryNodeId] FOREIGN KEY ([CategoryNodeId]) REFERENCES [ClassificationNodes] ([Id]),
        CONSTRAINT [FK_Products_ClassificationNodes_FamilyNodeId] FOREIGN KEY ([FamilyNodeId]) REFERENCES [ClassificationNodes] ([Id]),
        CONSTRAINT [FK_Products_ClassificationNodes_GroupNodeId] FOREIGN KEY ([GroupNodeId]) REFERENCES [ClassificationNodes] ([Id]),
        CONSTRAINT [FK_Products_DocumentReferences_ImageDocumentId] FOREIGN KEY ([ImageDocumentId]) REFERENCES [DocumentReferences] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Products_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [Models] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Products_Stations_StationId] FOREIGN KEY ([StationId]) REFERENCES [Stations] ([Id]),
        CONSTRAINT [FK_Products_UMs_UnitOfMeasureId] FOREIGN KEY ([UnitOfMeasureId]) REFERENCES [UMs] ([Id]),
        CONSTRAINT [FK_Products_VatRates_VatRateId] FOREIGN KEY ([VatRateId]) REFERENCES [VatRates] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [Teams] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [ShortDescription] nvarchar(200) NOT NULL,
        [LongDescription] nvarchar(1000) NOT NULL,
        [Email] nvarchar(100) NULL,
        [Status] int NOT NULL,
        [EventId] uniqueidentifier NOT NULL,
        [ClubCode] nvarchar(50) NULL,
        [FederationCode] nvarchar(50) NULL,
        [Category] nvarchar(50) NULL,
        [CoachContactId] uniqueidentifier NULL,
        [TeamLogoDocumentId] uniqueidentifier NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Teams] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Teams_Contacts_CoachContactId] FOREIGN KEY ([CoachContactId]) REFERENCES [Contacts] ([Id]),
        CONSTRAINT [FK_Teams_DocumentReferences_TeamLogoDocumentId] FOREIGN KEY ([TeamLogoDocumentId]) REFERENCES [DocumentReferences] ([Id]),
        CONSTRAINT [FK_Teams_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [DocumentRecurrences] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [TemplateId] uniqueidentifier NOT NULL,
        [Pattern] int NOT NULL,
        [Interval] int NOT NULL,
        [DaysOfWeek] nvarchar(50) NULL,
        [DayOfMonth] int NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [MaxOccurrences] int NULL,
        [NextExecutionDate] datetime2 NULL,
        [LastExecutionDate] datetime2 NULL,
        [ExecutionCount] int NOT NULL,
        [IsEnabled] bit NOT NULL,
        [Status] int NOT NULL,
        [BusinessPartyId] uniqueidentifier NULL,
        [WarehouseId] uniqueidentifier NULL,
        [LeadTimeDays] int NOT NULL,
        [NotificationSettings] nvarchar(1000) NULL,
        [AdditionalConfig] nvarchar(2000) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DocumentRecurrences] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentRecurrences_DocumentTemplates_TemplateId] FOREIGN KEY ([TemplateId]) REFERENCES [DocumentTemplates] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [DocumentWorkflowStepDefinition] (
        [Id] uniqueidentifier NOT NULL,
        [WorkflowId] uniqueidentifier NOT NULL,
        [StepOrder] int NOT NULL,
        [StepName] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [StepType] int NOT NULL,
        [RequiredRole] nvarchar(100) NULL,
        [AssignedUser] nvarchar(100) NULL,
        [TimeLimitHours] int NULL,
        [IsMandatory] bit NOT NULL,
        [RequiresMultipleApprovers] bit NOT NULL,
        [MinApprovers] int NULL,
        [Conditions] nvarchar(1000) NULL,
        [Actions] nvarchar(1000) NULL,
        [NextStepOnApproval] int NULL,
        [NextStepOnRejection] int NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DocumentWorkflowStepDefinition] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentWorkflowStepDefinition_DocumentWorkflows_WorkflowId] FOREIGN KEY ([WorkflowId]) REFERENCES [DocumentWorkflows] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [Lots] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [ProductionDate] datetime2 NULL,
        [ExpiryDate] datetime2 NULL,
        [SupplierId] uniqueidentifier NULL,
        [OriginalQuantity] decimal(18,6) NOT NULL,
        [AvailableQuantity] decimal(18,6) NOT NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(500) NULL,
        [QualityStatus] int NOT NULL,
        [Barcode] nvarchar(50) NULL,
        [CountryOfOrigin] nvarchar(50) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Lots] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Lots_BusinessParties_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [BusinessParties] ([Id]),
        CONSTRAINT [FK_Lots_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [PriceListEntries] (
        [Id] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [PriceListId] uniqueidentifier NOT NULL,
        [Price] decimal(18,6) NOT NULL,
        [Currency] nvarchar(3) NOT NULL,
        [Score] int NOT NULL,
        [IsEditableInFrontend] bit NOT NULL,
        [IsDiscountable] bit NOT NULL,
        [Status] int NOT NULL,
        [MinQuantity] int NOT NULL,
        [MaxQuantity] int NOT NULL,
        [Notes] nvarchar(500) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_PriceListEntries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PriceListEntries_PriceLists_PriceListId] FOREIGN KEY ([PriceListId]) REFERENCES [PriceLists] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PriceListEntries_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [ProductBundleItems] (
        [Id] uniqueidentifier NOT NULL,
        [BundleProductId] uniqueidentifier NOT NULL,
        [ComponentProductId] uniqueidentifier NOT NULL,
        [Quantity] int NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ProductBundleItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductBundleItems_Products_BundleProductId] FOREIGN KEY ([BundleProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductBundleItems_Products_ComponentProductId] FOREIGN KEY ([ComponentProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [ProductSuppliers] (
        [Id] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [SupplierId] uniqueidentifier NOT NULL,
        [SupplierProductCode] nvarchar(100) NULL,
        [PurchaseDescription] nvarchar(500) NULL,
        [UnitCost] decimal(18,6) NULL,
        [Currency] nvarchar(10) NULL,
        [MinOrderQty] int NULL,
        [IncrementQty] int NULL,
        [LeadTimeDays] int NULL,
        [LastPurchasePrice] decimal(18,6) NULL,
        [LastPurchaseDate] datetime2 NULL,
        [Preferred] bit NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ProductSuppliers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductSuppliers_BusinessParties_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [BusinessParties] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProductSuppliers_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [ProductUnits] (
        [Id] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [UnitOfMeasureId] uniqueidentifier NOT NULL,
        [ConversionFactor] decimal(18,6) NOT NULL,
        [UnitType] nvarchar(20) NOT NULL,
        [Description] nvarchar(100) NULL,
        [Status] int NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ProductUnits] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductUnits_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductUnits_UMs_UnitOfMeasureId] FOREIGN KEY ([UnitOfMeasureId]) REFERENCES [UMs] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [PromotionRuleProducts] (
        [Id] uniqueidentifier NOT NULL,
        [PromotionRuleId] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [Quantity] int NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_PromotionRuleProducts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PromotionRuleProducts_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PromotionRuleProducts_PromotionRules_PromotionRuleId] FOREIGN KEY ([PromotionRuleId]) REFERENCES [PromotionRules] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [TeamMembers] (
        [Id] uniqueidentifier NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Email] nvarchar(100) NULL,
        [Role] nvarchar(50) NULL,
        [DateOfBirth] datetime2 NULL,
        [Status] int NOT NULL,
        [TeamId] uniqueidentifier NOT NULL,
        [Position] nvarchar(50) NULL,
        [JerseyNumber] int NULL,
        [EligibilityStatus] int NOT NULL,
        [PhotoDocumentId] uniqueidentifier NULL,
        [PhotoConsent] bit NOT NULL,
        [PhotoConsentAt] datetime2 NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_TeamMembers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TeamMembers_DocumentReferences_PhotoDocumentId] FOREIGN KEY ([PhotoDocumentId]) REFERENCES [DocumentReferences] ([Id]),
        CONSTRAINT [FK_TeamMembers_Teams_TeamId] FOREIGN KEY ([TeamId]) REFERENCES [Teams] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [Serials] (
        [Id] uniqueidentifier NOT NULL,
        [SerialNumber] nvarchar(100) NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [LotId] uniqueidentifier NULL,
        [CurrentLocationId] uniqueidentifier NULL,
        [Status] int NOT NULL,
        [ManufacturingDate] datetime2 NULL,
        [WarrantyExpiry] datetime2 NULL,
        [OwnerId] uniqueidentifier NULL,
        [SaleDate] datetime2 NULL,
        [Notes] nvarchar(500) NULL,
        [Barcode] nvarchar(50) NULL,
        [RfidTag] nvarchar(50) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Serials] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Serials_BusinessParties_OwnerId] FOREIGN KEY ([OwnerId]) REFERENCES [BusinessParties] ([Id]),
        CONSTRAINT [FK_Serials_Lots_LotId] FOREIGN KEY ([LotId]) REFERENCES [Lots] ([Id]),
        CONSTRAINT [FK_Serials_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Serials_StorageLocations_CurrentLocationId] FOREIGN KEY ([CurrentLocationId]) REFERENCES [StorageLocations] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [Stocks] (
        [Id] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [StorageLocationId] uniqueidentifier NOT NULL,
        [LotId] uniqueidentifier NULL,
        [Quantity] decimal(18,6) NOT NULL,
        [ReservedQuantity] decimal(18,6) NOT NULL,
        [MinimumLevel] decimal(18,6) NULL,
        [MaximumLevel] decimal(18,6) NULL,
        [ReorderPoint] decimal(18,6) NULL,
        [ReorderQuantity] decimal(18,6) NULL,
        [LastMovementDate] datetime2 NULL,
        [UnitCost] decimal(18,6) NULL,
        [LastInventoryDate] datetime2 NULL,
        [Notes] nvarchar(200) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_Stocks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Stocks_Lots_LotId] FOREIGN KEY ([LotId]) REFERENCES [Lots] ([Id]),
        CONSTRAINT [FK_Stocks_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Stocks_StorageLocations_StorageLocationId] FOREIGN KEY ([StorageLocationId]) REFERENCES [StorageLocations] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [SustainabilityCertificates] (
        [Id] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NULL,
        [LotId] uniqueidentifier NULL,
        [CertificateType] int NOT NULL,
        [CertificateNumber] nvarchar(100) NOT NULL,
        [IssuingAuthority] nvarchar(200) NOT NULL,
        [IssueDate] datetime2 NOT NULL,
        [ExpiryDate] datetime2 NULL,
        [Status] int NOT NULL,
        [CountryOfOrigin] nvarchar(100) NULL,
        [CarbonFootprintKg] decimal(18,6) NULL,
        [WaterUsageLiters] decimal(18,6) NULL,
        [EnergyConsumptionKwh] decimal(18,6) NULL,
        [RecycledContentPercentage] decimal(18,6) NULL,
        [IsRecyclable] bit NOT NULL,
        [IsBiodegradable] bit NOT NULL,
        [IsOrganic] bit NOT NULL,
        [IsFairTrade] bit NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [DocumentId] uniqueidentifier NULL,
        [IsVerified] bit NOT NULL,
        [VerificationDate] datetime2 NULL,
        [VerifiedBy] nvarchar(100) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_SustainabilityCertificates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SustainabilityCertificates_Lots_LotId] FOREIGN KEY ([LotId]) REFERENCES [Lots] ([Id]),
        CONSTRAINT [FK_SustainabilityCertificates_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [ProductCodes] (
        [Id] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [ProductUnitId] uniqueidentifier NULL,
        [CodeType] nvarchar(30) NOT NULL,
        [Code] nvarchar(100) NOT NULL,
        [AlternativeDescription] nvarchar(200) NULL,
        [Status] int NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ProductCodes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductCodes_ProductUnits_ProductUnitId] FOREIGN KEY ([ProductUnitId]) REFERENCES [ProductUnits] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ProductCodes_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [InsurancePolicies] (
        [Id] uniqueidentifier NOT NULL,
        [TeamMemberId] uniqueidentifier NOT NULL,
        [Provider] nvarchar(100) NOT NULL,
        [PolicyNumber] nvarchar(50) NOT NULL,
        [ValidFrom] datetime2 NOT NULL,
        [ValidTo] datetime2 NOT NULL,
        [DocumentReferenceId] uniqueidentifier NULL,
        [CoverageType] nvarchar(100) NULL,
        [CoverageAmount] decimal(18,6) NULL,
        [Currency] nvarchar(3) NULL,
        [Notes] nvarchar(500) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_InsurancePolicies] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InsurancePolicies_DocumentReferences_DocumentReferenceId] FOREIGN KEY ([DocumentReferenceId]) REFERENCES [DocumentReferences] ([Id]),
        CONSTRAINT [FK_InsurancePolicies_TeamMembers_TeamMemberId] FOREIGN KEY ([TeamMemberId]) REFERENCES [TeamMembers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [MembershipCards] (
        [Id] uniqueidentifier NOT NULL,
        [TeamMemberId] uniqueidentifier NOT NULL,
        [CardNumber] nvarchar(50) NOT NULL,
        [Federation] nvarchar(100) NOT NULL,
        [ValidFrom] datetime2 NOT NULL,
        [ValidTo] datetime2 NOT NULL,
        [DocumentReferenceId] uniqueidentifier NULL,
        [Category] nvarchar(50) NULL,
        [Notes] nvarchar(500) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_MembershipCards] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MembershipCards_DocumentReferences_DocumentReferenceId] FOREIGN KEY ([DocumentReferenceId]) REFERENCES [DocumentReferences] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_MembershipCards_TeamMembers_TeamMemberId] FOREIGN KEY ([TeamMemberId]) REFERENCES [TeamMembers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [MaintenanceRecords] (
        [Id] uniqueidentifier NOT NULL,
        [SerialId] uniqueidentifier NOT NULL,
        [RecordNumber] nvarchar(50) NOT NULL,
        [MaintenanceType] int NOT NULL,
        [Status] int NOT NULL,
        [ScheduledDate] datetime2 NOT NULL,
        [StartedDate] datetime2 NULL,
        [CompletedDate] datetime2 NULL,
        [Technician] nvarchar(100) NULL,
        [Description] nvarchar(1000) NOT NULL,
        [PartsUsed] nvarchar(500) NULL,
        [Cost] decimal(18,6) NULL,
        [LaborHours] decimal(18,6) NULL,
        [NextMaintenanceDate] datetime2 NULL,
        [MaintenanceIntervalDays] int NULL,
        [IssuesFound] nvarchar(1000) NULL,
        [Recommendations] nvarchar(1000) NULL,
        [ServiceProvider] nvarchar(200) NULL,
        [WarrantyInfo] nvarchar(200) NULL,
        [Priority] int NOT NULL,
        [DocumentId] uniqueidentifier NULL,
        [Notes] nvarchar(1000) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_MaintenanceRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MaintenanceRecords_Serials_SerialId] FOREIGN KEY ([SerialId]) REFERENCES [Serials] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [QualityControls] (
        [Id] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [LotId] uniqueidentifier NULL,
        [SerialId] uniqueidentifier NULL,
        [ControlNumber] nvarchar(50) NOT NULL,
        [ControlType] int NOT NULL,
        [Status] int NOT NULL,
        [TestDate] datetime2 NOT NULL,
        [CompletionDate] datetime2 NULL,
        [Inspector] nvarchar(100) NOT NULL,
        [TestMethod] nvarchar(200) NULL,
        [SampleSize] decimal(18,6) NULL,
        [Results] nvarchar(1000) NULL,
        [Observations] nvarchar(1000) NULL,
        [Passed] bit NULL,
        [Defects] nvarchar(500) NULL,
        [CorrectiveActions] nvarchar(500) NULL,
        [DocumentId] uniqueidentifier NULL,
        [CertificateNumber] nvarchar(100) NULL,
        [ExpiryDate] datetime2 NULL,
        [NextControlDate] datetime2 NULL,
        [Cost] decimal(18,6) NULL,
        [ExternalLab] nvarchar(200) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_QualityControls] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QualityControls_Lots_LotId] FOREIGN KEY ([LotId]) REFERENCES [Lots] ([Id]),
        CONSTRAINT [FK_QualityControls_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_QualityControls_Serials_SerialId] FOREIGN KEY ([SerialId]) REFERENCES [Serials] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [WasteManagementRecords] (
        [Id] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NULL,
        [LotId] uniqueidentifier NULL,
        [SerialId] uniqueidentifier NULL,
        [StorageLocationId] uniqueidentifier NULL,
        [RecordNumber] nvarchar(50) NOT NULL,
        [WasteType] int NOT NULL,
        [Reason] int NOT NULL,
        [Quantity] decimal(18,6) NOT NULL,
        [WeightKg] decimal(18,6) NULL,
        [WasteDate] datetime2 NOT NULL,
        [DisposalMethod] int NOT NULL,
        [DisposalDate] datetime2 NULL,
        [DisposalCompany] nvarchar(200) NULL,
        [DisposalCost] decimal(18,6) NULL,
        [Status] int NOT NULL,
        [IsHazardous] bit NOT NULL,
        [HazardCode] nvarchar(50) NULL,
        [EnvironmentalImpact] nvarchar(1000) NULL,
        [RecyclingRatePercentage] decimal(5,2) NULL,
        [RecoveryValue] decimal(18,6) NULL,
        [ResponsiblePerson] nvarchar(100) NULL,
        [Notes] nvarchar(1000) NULL,
        [DocumentId] uniqueidentifier NULL,
        [CertificateNumber] nvarchar(100) NULL,
        [IsCompliant] bit NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_WasteManagementRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WasteManagementRecords_Lots_LotId] FOREIGN KEY ([LotId]) REFERENCES [Lots] ([Id]),
        CONSTRAINT [FK_WasteManagementRecords_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]),
        CONSTRAINT [FK_WasteManagementRecords_Serials_SerialId] FOREIGN KEY ([SerialId]) REFERENCES [Serials] ([Id]),
        CONSTRAINT [FK_WasteManagementRecords_StorageLocations_StorageLocationId] FOREIGN KEY ([StorageLocationId]) REFERENCES [StorageLocations] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [StockAlerts] (
        [Id] uniqueidentifier NOT NULL,
        [StockId] uniqueidentifier NOT NULL,
        [AlertType] int NOT NULL,
        [Severity] int NOT NULL,
        [CurrentLevel] decimal(18,6) NOT NULL,
        [Threshold] decimal(18,6) NOT NULL,
        [Message] nvarchar(500) NOT NULL,
        [Status] int NOT NULL,
        [TriggeredDate] datetime2 NOT NULL,
        [AcknowledgedDate] datetime2 NULL,
        [AcknowledgedBy] nvarchar(100) NULL,
        [ResolvedDate] datetime2 NULL,
        [ResolvedBy] nvarchar(100) NULL,
        [ResolutionNotes] nvarchar(500) NULL,
        [SendEmailNotifications] bit NOT NULL,
        [NotificationEmails] nvarchar(500) NULL,
        [LastNotificationDate] datetime2 NULL,
        [NotificationCount] int NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_StockAlerts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockAlerts_Stocks_StockId] FOREIGN KEY ([StockId]) REFERENCES [Stocks] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [DocumentAccessLogs] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentHeaderId] uniqueidentifier NULL,
        [UserId] nvarchar(256) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [AccessType] nvarchar(50) NOT NULL,
        [AccessedAt] datetime2 NOT NULL,
        [IpAddress] nvarchar(45) NULL,
        [UserAgent] nvarchar(500) NULL,
        [Result] nvarchar(50) NOT NULL,
        [Details] nvarchar(1000) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [SessionId] nvarchar(100) NULL,
        CONSTRAINT [PK_DocumentAccessLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [DocumentAnalytics] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentHeaderId] uniqueidentifier NOT NULL,
        [AnalyticsDate] datetime2 NOT NULL,
        [DocumentTypeId] uniqueidentifier NULL,
        [BusinessPartyId] uniqueidentifier NULL,
        [DocumentCreator] nvarchar(100) NULL,
        [Department] nvarchar(100) NULL,
        [TimeToFirstApprovalHours] decimal(18,6) NULL,
        [TimeToFinalApprovalHours] decimal(18,6) NULL,
        [TimeToClosureHours] decimal(18,6) NULL,
        [TotalProcessingTimeHours] decimal(18,6) NULL,
        [ApprovalStepsRequired] int NOT NULL,
        [ApprovalStepsCompleted] int NOT NULL,
        [ApprovalsReceived] int NOT NULL,
        [Rejections] int NOT NULL,
        [AverageApprovalTimeHours] decimal(18,6) NULL,
        [Escalations] int NOT NULL,
        [Revisions] int NOT NULL,
        [Errors] int NOT NULL,
        [CommentsCount] int NOT NULL,
        [AttachmentsCount] int NOT NULL,
        [VersionsCount] int NOT NULL,
        [DocumentValue] decimal(18,6) NULL,
        [Currency] nvarchar(3) NULL,
        [ProcessingCost] decimal(18,6) NULL,
        [EfficiencyScore] decimal(18,6) NULL,
        [FinalStatus] int NULL,
        [QualityScore] decimal(18,6) NULL,
        [ComplianceScore] decimal(18,6) NULL,
        [SatisfactionScore] decimal(18,6) NULL,
        [CompletedOnTime] bit NULL,
        [TimeVarianceHours] decimal(18,6) NULL,
        [PeakLoad] decimal(18,6) NULL,
        [UsersInvolved] int NOT NULL,
        [ExternalSystems] int NOT NULL,
        [NotificationsSent] int NOT NULL,
        [AdditionalData] nvarchar(max) NULL,
        [AnalyticsCategory] nvarchar(50) NULL,
        [Tags] nvarchar(500) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DocumentAnalytics] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentAnalytics_DocumentTypes_DocumentTypeId] FOREIGN KEY ([DocumentTypeId]) REFERENCES [DocumentTypes] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [DocumentAttachments] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentHeaderId] uniqueidentifier NULL,
        [DocumentRowId] uniqueidentifier NULL,
        [FileName] nvarchar(255) NOT NULL,
        [StoragePath] nvarchar(500) NOT NULL,
        [MimeType] nvarchar(100) NOT NULL,
        [FileSizeBytes] bigint NOT NULL,
        [Version] int NOT NULL,
        [PreviousVersionId] uniqueidentifier NULL,
        [Title] nvarchar(200) NULL,
        [Notes] nvarchar(500) NULL,
        [IsSigned] bit NOT NULL,
        [SignatureInfo] nvarchar(1000) NULL,
        [SignedAt] datetime2 NULL,
        [SignedBy] nvarchar(100) NULL,
        [IsCurrentVersion] bit NOT NULL,
        [Category] int NOT NULL,
        [AccessLevel] int NOT NULL,
        [StorageProvider] nvarchar(50) NULL,
        [ExternalReference] nvarchar(200) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DocumentAttachments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentAttachments_DocumentAttachments_PreviousVersionId] FOREIGN KEY ([PreviousVersionId]) REFERENCES [DocumentAttachments] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [DocumentComments] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentHeaderId] uniqueidentifier NULL,
        [DocumentRowId] uniqueidentifier NULL,
        [Content] nvarchar(2000) NOT NULL,
        [CommentType] int NOT NULL,
        [Priority] int NOT NULL,
        [Status] int NOT NULL,
        [ParentCommentId] uniqueidentifier NULL,
        [AssignedTo] nvarchar(100) NULL,
        [DueDate] datetime2 NULL,
        [ResolvedAt] datetime2 NULL,
        [ResolvedBy] nvarchar(100) NULL,
        [MentionedUsers] nvarchar(500) NULL,
        [IsPrivate] bit NOT NULL,
        [IsPinned] bit NOT NULL,
        [Visibility] int NOT NULL,
        [Tags] nvarchar(200) NULL,
        [Metadata] nvarchar(1000) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DocumentComments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentComments_DocumentComments_ParentCommentId] FOREIGN KEY ([ParentCommentId]) REFERENCES [DocumentComments] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [DocumentHeaders] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentTypeId] uniqueidentifier NOT NULL,
        [Series] nvarchar(10) NULL,
        [Number] nvarchar(30) NOT NULL,
        [Date] datetime2 NOT NULL,
        [BusinessPartyId] uniqueidentifier NOT NULL,
        [BusinessPartyAddressId] uniqueidentifier NULL,
        [CustomerName] nvarchar(100) NULL,
        [SourceWarehouseId] uniqueidentifier NULL,
        [DestinationWarehouseId] uniqueidentifier NULL,
        [ShippingDate] datetime2 NULL,
        [CarrierName] nvarchar(100) NULL,
        [TrackingNumber] nvarchar(50) NULL,
        [ShippingNotes] nvarchar(200) NULL,
        [TeamMemberId] uniqueidentifier NULL,
        [TeamId] uniqueidentifier NULL,
        [EventId] uniqueidentifier NULL,
        [CashRegisterId] uniqueidentifier NULL,
        [CashierId] uniqueidentifier NULL,
        [ExternalDocumentNumber] nvarchar(30) NULL,
        [ExternalDocumentSeries] nvarchar(10) NULL,
        [ExternalDocumentDate] datetime2 NULL,
        [DocumentReason] nvarchar(100) NULL,
        [IsProforma] bit NOT NULL,
        [IsFiscal] bit NOT NULL,
        [FiscalDocumentNumber] nvarchar(30) NULL,
        [FiscalDate] datetime2 NULL,
        [VatAmount] decimal(18,6) NOT NULL,
        [TotalNetAmount] decimal(18,6) NOT NULL,
        [TotalGrossAmount] decimal(18,6) NOT NULL,
        [Currency] nvarchar(3) NOT NULL,
        [ExchangeRate] decimal(18,6) NULL,
        [BaseCurrencyAmount] decimal(18,6) NULL,
        [DueDate] datetime2 NULL,
        [PaymentStatus] int NOT NULL,
        [AmountPaid] decimal(18,6) NOT NULL,
        [PaymentMethod] nvarchar(30) NULL,
        [PaymentReference] nvarchar(50) NULL,
        [TotalDiscount] decimal(18,6) NOT NULL,
        [TotalDiscountAmount] decimal(18,6) NOT NULL,
        [ApprovalStatus] int NOT NULL,
        [ApprovedBy] nvarchar(100) NULL,
        [ApprovedAt] datetime2 NULL,
        [ClosedAt] datetime2 NULL,
        [Status] int NOT NULL,
        [ReferenceDocumentId] uniqueidentifier NULL,
        [SourceTemplateId] uniqueidentifier NULL,
        [SourceRecurrenceId] uniqueidentifier NULL,
        [CurrentVersionNumber] int NOT NULL,
        [VersioningEnabled] bit NOT NULL,
        [CurrentWorkflowExecutionId] uniqueidentifier NULL,
        [Notes] nvarchar(500) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DocumentHeaders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentHeaders_Addresses_BusinessPartyAddressId] FOREIGN KEY ([BusinessPartyAddressId]) REFERENCES [Addresses] ([Id]),
        CONSTRAINT [FK_DocumentHeaders_BusinessParties_BusinessPartyId] FOREIGN KEY ([BusinessPartyId]) REFERENCES [BusinessParties] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocumentHeaders_DocumentHeaders_ReferenceDocumentId] FOREIGN KEY ([ReferenceDocumentId]) REFERENCES [DocumentHeaders] ([Id]),
        CONSTRAINT [FK_DocumentHeaders_DocumentRecurrences_SourceRecurrenceId] FOREIGN KEY ([SourceRecurrenceId]) REFERENCES [DocumentRecurrences] ([Id]),
        CONSTRAINT [FK_DocumentHeaders_DocumentTemplates_SourceTemplateId] FOREIGN KEY ([SourceTemplateId]) REFERENCES [DocumentTemplates] ([Id]),
        CONSTRAINT [FK_DocumentHeaders_DocumentTypes_DocumentTypeId] FOREIGN KEY ([DocumentTypeId]) REFERENCES [DocumentTypes] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocumentHeaders_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]),
        CONSTRAINT [FK_DocumentHeaders_StorageFacilities_DestinationWarehouseId] FOREIGN KEY ([DestinationWarehouseId]) REFERENCES [StorageFacilities] ([Id]),
        CONSTRAINT [FK_DocumentHeaders_StorageFacilities_SourceWarehouseId] FOREIGN KEY ([SourceWarehouseId]) REFERENCES [StorageFacilities] ([Id]),
        CONSTRAINT [FK_DocumentHeaders_StorePoses_CashRegisterId] FOREIGN KEY ([CashRegisterId]) REFERENCES [StorePoses] ([Id]),
        CONSTRAINT [FK_DocumentHeaders_StoreUsers_CashierId] FOREIGN KEY ([CashierId]) REFERENCES [StoreUsers] ([Id]),
        CONSTRAINT [FK_DocumentHeaders_TeamMembers_TeamMemberId] FOREIGN KEY ([TeamMemberId]) REFERENCES [TeamMembers] ([Id]),
        CONSTRAINT [FK_DocumentHeaders_Teams_TeamId] FOREIGN KEY ([TeamId]) REFERENCES [Teams] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [DocumentRows] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentHeaderId] uniqueidentifier NOT NULL,
        [RowType] int NOT NULL,
        [ParentRowId] uniqueidentifier NULL,
        [ProductCode] nvarchar(50) NULL,
        [ProductId] uniqueidentifier NULL,
        [LocationId] uniqueidentifier NULL,
        [Description] nvarchar(200) NOT NULL,
        [UnitOfMeasure] nvarchar(10) NULL,
        [UnitOfMeasureId] uniqueidentifier NULL,
        [UnitOfMeasureEntityId] uniqueidentifier NULL,
        [UnitPrice] decimal(18,6) NOT NULL,
        [BaseQuantity] decimal(18,4) NULL,
        [BaseUnitPrice] decimal(18,4) NULL,
        [BaseUnitOfMeasureId] uniqueidentifier NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [LineDiscount] decimal(5,2) NOT NULL,
        [LineDiscountValue] decimal(18,6) NOT NULL,
        [DiscountType] int NOT NULL,
        [VatRate] decimal(5,2) NOT NULL,
        [VatDescription] nvarchar(30) NULL,
        [IsGift] bit NOT NULL,
        [IsManual] bit NOT NULL,
        [SourceWarehouseId] uniqueidentifier NULL,
        [DestinationWarehouseId] uniqueidentifier NULL,
        [Notes] nvarchar(200) NULL,
        [SortOrder] int NOT NULL,
        [StationId] uniqueidentifier NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DocumentRows] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentRows_DocumentHeaders_DocumentHeaderId] FOREIGN KEY ([DocumentHeaderId]) REFERENCES [DocumentHeaders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocumentRows_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]),
        CONSTRAINT [FK_DocumentRows_Stations_StationId] FOREIGN KEY ([StationId]) REFERENCES [Stations] ([Id]),
        CONSTRAINT [FK_DocumentRows_StorageFacilities_DestinationWarehouseId] FOREIGN KEY ([DestinationWarehouseId]) REFERENCES [StorageFacilities] ([Id]),
        CONSTRAINT [FK_DocumentRows_StorageFacilities_SourceWarehouseId] FOREIGN KEY ([SourceWarehouseId]) REFERENCES [StorageFacilities] ([Id]),
        CONSTRAINT [FK_DocumentRows_StorageLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [StorageLocations] ([Id]),
        CONSTRAINT [FK_DocumentRows_UMs_UnitOfMeasureEntityId] FOREIGN KEY ([UnitOfMeasureEntityId]) REFERENCES [UMs] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [DocumentSchedules] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentHeaderId] uniqueidentifier NULL,
        [DocumentTypeId] uniqueidentifier NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [ScheduleType] int NOT NULL,
        [Category] nvarchar(50) NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [Frequency] int NOT NULL,
        [Interval] int NOT NULL,
        [SpecificDays] nvarchar(100) NULL,
        [TimeOfDay] time NULL,
        [Timezone] nvarchar(50) NULL,
        [Priority] int NOT NULL,
        [Status] int NOT NULL,
        [NextExecutionDate] datetime2 NULL,
        [LastExecutionDate] datetime2 NULL,
        [ExecutionCount] int NOT NULL,
        [Actions] nvarchar(2000) NULL,
        [Conditions] nvarchar(1000) NULL,
        [NotificationSettings] nvarchar(1000) NULL,
        [AutoRenewalSettings] nvarchar(1000) NULL,
        [IntegrationSettings] nvarchar(1000) NULL,
        [CustomData] nvarchar(2000) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DocumentSchedules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentSchedules_DocumentHeaders_DocumentHeaderId] FOREIGN KEY ([DocumentHeaderId]) REFERENCES [DocumentHeaders] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_DocumentSchedules_DocumentTypes_DocumentTypeId] FOREIGN KEY ([DocumentTypeId]) REFERENCES [DocumentTypes] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [DocumentSummaryLinks] (
        [Id] uniqueidentifier NOT NULL,
        [SummaryDocumentId] uniqueidentifier NOT NULL,
        [DetailedDocumentId] uniqueidentifier NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DocumentSummaryLinks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentSummaryLinks_DocumentHeaders_DetailedDocumentId] FOREIGN KEY ([DetailedDocumentId]) REFERENCES [DocumentHeaders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DocumentSummaryLinks_DocumentHeaders_SummaryDocumentId] FOREIGN KEY ([SummaryDocumentId]) REFERENCES [DocumentHeaders] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [DocumentVersions] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentHeaderId] uniqueidentifier NOT NULL,
        [VersionNumber] int NOT NULL,
        [VersionLabel] nvarchar(50) NULL,
        [ChangeDescription] nvarchar(1000) NULL,
        [IsCurrentVersion] bit NOT NULL,
        [DocumentSnapshot] nvarchar(max) NOT NULL,
        [RowsSnapshot] nvarchar(max) NULL,
        [DataSize] bigint NOT NULL,
        [Checksum] nvarchar(64) NULL,
        [VersionCreator] nvarchar(100) NULL,
        [VersionReason] nvarchar(200) NULL,
        [WorkflowState] int NULL,
        [ApprovalStatus] int NOT NULL,
        [ApprovedBy] nvarchar(100) NULL,
        [ApprovedAt] datetime2 NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DocumentVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentVersions_DocumentHeaders_DocumentHeaderId] FOREIGN KEY ([DocumentHeaderId]) REFERENCES [DocumentHeaders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [DocumentWorkflowExecutions] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentHeaderId] uniqueidentifier NOT NULL,
        [WorkflowId] uniqueidentifier NOT NULL,
        [CurrentState] int NOT NULL,
        [CurrentStepOrder] int NULL,
        [Status] int NOT NULL,
        [Priority] int NOT NULL,
        [StartedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [InitiatedBy] nvarchar(100) NULL,
        [InitiationReason] nvarchar(500) NULL,
        [ExpectedCompletionDate] datetime2 NULL,
        [ActualCompletionDate] datetime2 NULL,
        [ProcessingTimeHours] decimal(18,6) NULL,
        [IsEscalated] bit NOT NULL,
        [EscalationLevel] int NOT NULL,
        [LastEscalatedAt] datetime2 NULL,
        [ContextData] nvarchar(max) NULL,
        [FinalOutcome] nvarchar(200) NULL,
        [Notes] nvarchar(1000) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DocumentWorkflowExecutions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentWorkflowExecutions_DocumentHeaders_DocumentHeaderId] FOREIGN KEY ([DocumentHeaderId]) REFERENCES [DocumentHeaders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DocumentWorkflowExecutions_DocumentWorkflows_WorkflowId] FOREIGN KEY ([WorkflowId]) REFERENCES [DocumentWorkflows] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [StockMovementPlans] (
        [Id] uniqueidentifier NOT NULL,
        [MovementType] int NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [LotId] uniqueidentifier NULL,
        [FromLocationId] uniqueidentifier NULL,
        [ToLocationId] uniqueidentifier NULL,
        [PlannedQuantity] decimal(18,6) NOT NULL,
        [PlannedDate] datetime2 NOT NULL,
        [Priority] int NOT NULL,
        [Status] int NOT NULL,
        [Reason] int NOT NULL,
        [Notes] nvarchar(500) NULL,
        [DocumentHeaderId] uniqueidentifier NULL,
        [PlanCreator] nvarchar(100) NULL,
        [AssignedTo] nvarchar(100) NULL,
        [ApprovedDate] datetime2 NULL,
        [ApprovedBy] nvarchar(100) NULL,
        [ExecutedDate] datetime2 NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_StockMovementPlans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockMovementPlans_DocumentHeaders_DocumentHeaderId] FOREIGN KEY ([DocumentHeaderId]) REFERENCES [DocumentHeaders] ([Id]),
        CONSTRAINT [FK_StockMovementPlans_Lots_LotId] FOREIGN KEY ([LotId]) REFERENCES [Lots] ([Id]),
        CONSTRAINT [FK_StockMovementPlans_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_StockMovementPlans_StorageLocations_FromLocationId] FOREIGN KEY ([FromLocationId]) REFERENCES [StorageLocations] ([Id]),
        CONSTRAINT [FK_StockMovementPlans_StorageLocations_ToLocationId] FOREIGN KEY ([ToLocationId]) REFERENCES [StorageLocations] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [StationOrderQueueItems] (
        [Id] uniqueidentifier NOT NULL,
        [StationId] uniqueidentifier NOT NULL,
        [DocumentHeaderId] uniqueidentifier NOT NULL,
        [DocumentRowId] uniqueidentifier NULL,
        [TeamMemberId] uniqueidentifier NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [Quantity] int NOT NULL,
        [Status] int NOT NULL,
        [SortOrder] int NOT NULL,
        [AssignedAt] datetime2 NULL,
        [StartedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [Notes] nvarchar(200) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_StationOrderQueueItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StationOrderQueueItems_DocumentHeaders_DocumentHeaderId] FOREIGN KEY ([DocumentHeaderId]) REFERENCES [DocumentHeaders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_StationOrderQueueItems_DocumentRows_DocumentRowId] FOREIGN KEY ([DocumentRowId]) REFERENCES [DocumentRows] ([Id]),
        CONSTRAINT [FK_StationOrderQueueItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_StationOrderQueueItems_Stations_StationId] FOREIGN KEY ([StationId]) REFERENCES [Stations] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_StationOrderQueueItems_TeamMembers_TeamMemberId] FOREIGN KEY ([TeamMemberId]) REFERENCES [TeamMembers] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [DocumentReminders] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentHeaderId] uniqueidentifier NOT NULL,
        [ReminderType] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [TargetDate] datetime2 NOT NULL,
        [Priority] int NOT NULL,
        [Status] int NOT NULL,
        [IsRecurring] bit NOT NULL,
        [RecurrencePattern] int NULL,
        [RecurrenceInterval] int NULL,
        [RecurrenceEndDate] datetime2 NULL,
        [NotifyUsers] nvarchar(1000) NULL,
        [NotifyRoles] nvarchar(500) NULL,
        [NotifyEmails] nvarchar(1000) NULL,
        [NotificationMethods] nvarchar(200) NULL,
        [LeadTimeHours] int NOT NULL,
        [EscalationEnabled] bit NOT NULL,
        [EscalationRules] nvarchar(1000) NULL,
        [SnoozeSettings] nvarchar(500) NULL,
        [CustomData] nvarchar(2000) NULL,
        [LastNotifiedAt] datetime2 NULL,
        [NextNotificationAt] datetime2 NULL,
        [NotificationCount] int NOT NULL,
        [SnoozeCount] int NOT NULL,
        [CompletedAt] datetime2 NULL,
        [CompletedBy] nvarchar(100) NULL,
        [CompletionNotes] nvarchar(500) NULL,
        [DocumentScheduleId] uniqueidentifier NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DocumentReminders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentReminders_DocumentHeaders_DocumentHeaderId] FOREIGN KEY ([DocumentHeaderId]) REFERENCES [DocumentHeaders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocumentReminders_DocumentSchedules_DocumentScheduleId] FOREIGN KEY ([DocumentScheduleId]) REFERENCES [DocumentSchedules] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [DocumentVersionSignature] (
        [Id] uniqueidentifier NOT NULL,
        [DocumentVersionId] uniqueidentifier NOT NULL,
        [Signer] nvarchar(100) NOT NULL,
        [SignerRole] nvarchar(100) NULL,
        [SignatureData] nvarchar(max) NOT NULL,
        [SignatureAlgorithm] nvarchar(50) NULL,
        [CertificateInfo] nvarchar(500) NULL,
        [SignedAt] datetime2 NOT NULL,
        [SignerIpAddress] nvarchar(45) NULL,
        [UserAgent] nvarchar(200) NULL,
        [IsValid] bit NOT NULL,
        [Timestamp] nvarchar(1000) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DocumentVersionSignature] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentVersionSignature_DocumentVersions_DocumentVersionId] FOREIGN KEY ([DocumentVersionId]) REFERENCES [DocumentVersions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [DocumentWorkflowStep] (
        [Id] uniqueidentifier NOT NULL,
        [WorkflowExecutionId] uniqueidentifier NOT NULL,
        [StepDefinitionId] uniqueidentifier NULL,
        [DocumentVersionId] uniqueidentifier NULL,
        [StepOrder] int NOT NULL,
        [StepName] nvarchar(100) NOT NULL,
        [StepType] int NOT NULL,
        [Status] int NOT NULL,
        [AssignedUser] nvarchar(100) NULL,
        [ExecutedBy] nvarchar(100) NULL,
        [StartedAt] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [DueDate] datetime2 NULL,
        [ProcessingTimeHours] decimal(18,6) NULL,
        [Decision] nvarchar(50) NULL,
        [Comments] nvarchar(1000) NULL,
        [StepData] nvarchar(max) NULL,
        [Attachments] nvarchar(500) NULL,
        [ErrorMessage] nvarchar(500) NULL,
        [RetryCount] int NOT NULL,
        [NextStepOrder] int NULL,
        [IsEscalated] bit NOT NULL,
        [EscalatedAt] datetime2 NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DocumentWorkflowStep] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DocumentWorkflowStep_DocumentVersions_DocumentVersionId] FOREIGN KEY ([DocumentVersionId]) REFERENCES [DocumentVersions] ([Id]),
        CONSTRAINT [FK_DocumentWorkflowStep_DocumentWorkflowExecutions_WorkflowExecutionId] FOREIGN KEY ([WorkflowExecutionId]) REFERENCES [DocumentWorkflowExecutions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocumentWorkflowStep_DocumentWorkflowStepDefinition_StepDefinitionId] FOREIGN KEY ([StepDefinitionId]) REFERENCES [DocumentWorkflowStepDefinition] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [StockMovements] (
        [Id] uniqueidentifier NOT NULL,
        [MovementType] int NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [LotId] uniqueidentifier NULL,
        [SerialId] uniqueidentifier NULL,
        [FromLocationId] uniqueidentifier NULL,
        [ToLocationId] uniqueidentifier NULL,
        [Quantity] decimal(18,6) NOT NULL,
        [UnitCost] decimal(18,6) NULL,
        [MovementDate] datetime2 NOT NULL,
        [DocumentHeaderId] uniqueidentifier NULL,
        [DocumentRowId] uniqueidentifier NULL,
        [Reason] int NOT NULL,
        [Notes] nvarchar(500) NULL,
        [UserId] nvarchar(100) NULL,
        [Reference] nvarchar(50) NULL,
        [Status] int NOT NULL,
        [MovementPlanId] uniqueidentifier NULL,
        [ProjectOrderId] uniqueidentifier NULL,
        [StockId] uniqueidentifier NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_StockMovements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StockMovements_DocumentHeaders_DocumentHeaderId] FOREIGN KEY ([DocumentHeaderId]) REFERENCES [DocumentHeaders] ([Id]),
        CONSTRAINT [FK_StockMovements_DocumentRows_DocumentRowId] FOREIGN KEY ([DocumentRowId]) REFERENCES [DocumentRows] ([Id]),
        CONSTRAINT [FK_StockMovements_Lots_LotId] FOREIGN KEY ([LotId]) REFERENCES [Lots] ([Id]),
        CONSTRAINT [FK_StockMovements_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_StockMovements_ProjectOrders_ProjectOrderId] FOREIGN KEY ([ProjectOrderId]) REFERENCES [ProjectOrders] ([Id]),
        CONSTRAINT [FK_StockMovements_Serials_SerialId] FOREIGN KEY ([SerialId]) REFERENCES [Serials] ([Id]),
        CONSTRAINT [FK_StockMovements_StockMovementPlans_MovementPlanId] FOREIGN KEY ([MovementPlanId]) REFERENCES [StockMovementPlans] ([Id]),
        CONSTRAINT [FK_StockMovements_Stocks_StockId] FOREIGN KEY ([StockId]) REFERENCES [Stocks] ([Id]),
        CONSTRAINT [FK_StockMovements_StorageLocations_FromLocationId] FOREIGN KEY ([FromLocationId]) REFERENCES [StorageLocations] ([Id]),
        CONSTRAINT [FK_StockMovements_StorageLocations_ToLocationId] FOREIGN KEY ([ToLocationId]) REFERENCES [StorageLocations] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE TABLE [ProjectMaterialAllocations] (
        [Id] uniqueidentifier NOT NULL,
        [ProjectOrderId] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [LotId] uniqueidentifier NULL,
        [SerialId] uniqueidentifier NULL,
        [StorageLocationId] uniqueidentifier NULL,
        [PlannedQuantity] decimal(18,6) NOT NULL,
        [AllocatedQuantity] decimal(18,6) NOT NULL,
        [ConsumedQuantity] decimal(18,6) NOT NULL,
        [ReturnedQuantity] decimal(18,6) NOT NULL,
        [UnitOfMeasureId] uniqueidentifier NULL,
        [Status] int NOT NULL,
        [PlannedDate] datetime2 NULL,
        [AllocationDate] datetime2 NULL,
        [ConsumptionStartDate] datetime2 NULL,
        [ConsumptionEndDate] datetime2 NULL,
        [UnitCost] decimal(18,6) NULL,
        [TotalCost] decimal(18,6) NULL,
        [StockMovementId] uniqueidentifier NULL,
        [Purpose] nvarchar(200) NULL,
        [RequestedBy] nvarchar(100) NULL,
        [ApprovedBy] nvarchar(100) NULL,
        [Notes] nvarchar(1000) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_ProjectMaterialAllocations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectMaterialAllocations_Lots_LotId] FOREIGN KEY ([LotId]) REFERENCES [Lots] ([Id]),
        CONSTRAINT [FK_ProjectMaterialAllocations_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProjectMaterialAllocations_ProjectOrders_ProjectOrderId] FOREIGN KEY ([ProjectOrderId]) REFERENCES [ProjectOrders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProjectMaterialAllocations_Serials_SerialId] FOREIGN KEY ([SerialId]) REFERENCES [Serials] ([Id]),
        CONSTRAINT [FK_ProjectMaterialAllocations_StockMovements_StockMovementId] FOREIGN KEY ([StockMovementId]) REFERENCES [StockMovements] ([Id]),
        CONSTRAINT [FK_ProjectMaterialAllocations_StorageLocations_StorageLocationId] FOREIGN KEY ([StorageLocationId]) REFERENCES [StorageLocations] ([Id]),
        CONSTRAINT [FK_ProjectMaterialAllocations_UMs_UnitOfMeasureId] FOREIGN KEY ([UnitOfMeasureId]) REFERENCES [UMs] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Addresses_BankId] ON [Addresses] ([BankId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Addresses_BusinessPartyId] ON [Addresses] ([BusinessPartyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_AdminTenants_ManagedTenantId] ON [AdminTenants] ([ManagedTenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AdminTenants_UserId_ManagedTenantId] ON [AdminTenants] ([UserId], [ManagedTenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_AuditTrails_PerformedByUserId] ON [AuditTrails] ([PerformedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_AuditTrails_SourceTenantId] ON [AuditTrails] ([SourceTenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_AuditTrails_TargetTenantId] ON [AuditTrails] ([TargetTenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_AuditTrails_TargetUserId] ON [AuditTrails] ([TargetUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_BackupOperations_StartedByUserId] ON [BackupOperations] ([StartedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_BusinessPartyAccountings_BankId] ON [BusinessPartyAccountings] ([BankId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_BusinessPartyAccountings_PaymentTermId] ON [BusinessPartyAccountings] ([PaymentTermId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ChatMembers_ChatThreadId] ON [ChatMembers] ([ChatThreadId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ChatMembers_JoinedAt] ON [ChatMembers] ([JoinedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ChatMembers_LastSeenAt] ON [ChatMembers] ([LastSeenAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ChatMembers_Role] ON [ChatMembers] ([Role]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ChatMembers_TenantId] ON [ChatMembers] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ChatMembers_UserId] ON [ChatMembers] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_ChatThreadId] ON [ChatMessages] ([ChatThreadId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_IsDeleted] ON [ChatMessages] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_ReplyToMessageId] ON [ChatMessages] ([ReplyToMessageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_SenderId] ON [ChatMessages] ([SenderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_SentAt] ON [ChatMessages] ([SentAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_Status] ON [ChatMessages] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_TenantId] ON [ChatMessages] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ChatThreads_CreatedAt] ON [ChatThreads] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ChatThreads_IsPrivate] ON [ChatThreads] ([IsPrivate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ChatThreads_TenantId] ON [ChatThreads] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ChatThreads_Type] ON [ChatThreads] ([Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ChatThreads_UpdatedAt] ON [ChatThreads] ([UpdatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ClassificationNodes_ParentId] ON [ClassificationNodes] ([ParentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Contacts_BankId] ON [Contacts] ([BankId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Contacts_BusinessPartyId] ON [Contacts] ([BusinessPartyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Contacts_ReferenceId] ON [Contacts] ([ReferenceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentAccessLogs_DocumentHeaderId] ON [DocumentAccessLogs] ([DocumentHeaderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DocumentAnalytics_DocumentHeaderId] ON [DocumentAnalytics] ([DocumentHeaderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentAnalytics_DocumentTypeId] ON [DocumentAnalytics] ([DocumentTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentAttachments_DocumentHeaderId] ON [DocumentAttachments] ([DocumentHeaderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentAttachments_DocumentRowId] ON [DocumentAttachments] ([DocumentRowId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentAttachments_PreviousVersionId] ON [DocumentAttachments] ([PreviousVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentComments_DocumentHeaderId] ON [DocumentComments] ([DocumentHeaderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentComments_DocumentRowId] ON [DocumentComments] ([DocumentRowId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentComments_ParentCommentId] ON [DocumentComments] ([ParentCommentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DocumentCounters_DocumentTypeId_Series_Year] ON [DocumentCounters] ([DocumentTypeId], [Series], [Year]) WHERE [Year] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentHeaders_BusinessPartyAddressId] ON [DocumentHeaders] ([BusinessPartyAddressId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentHeaders_BusinessPartyId] ON [DocumentHeaders] ([BusinessPartyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentHeaders_CashierId] ON [DocumentHeaders] ([CashierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentHeaders_CashRegisterId] ON [DocumentHeaders] ([CashRegisterId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentHeaders_CurrentWorkflowExecutionId] ON [DocumentHeaders] ([CurrentWorkflowExecutionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentHeaders_DestinationWarehouseId] ON [DocumentHeaders] ([DestinationWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentHeaders_DocumentTypeId] ON [DocumentHeaders] ([DocumentTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentHeaders_EventId] ON [DocumentHeaders] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentHeaders_ReferenceDocumentId] ON [DocumentHeaders] ([ReferenceDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentHeaders_SourceRecurrenceId] ON [DocumentHeaders] ([SourceRecurrenceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentHeaders_SourceTemplateId] ON [DocumentHeaders] ([SourceTemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentHeaders_SourceWarehouseId] ON [DocumentHeaders] ([SourceWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentHeaders_TeamId] ON [DocumentHeaders] ([TeamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentHeaders_TeamMemberId] ON [DocumentHeaders] ([TeamMemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentRecurrences_TemplateId] ON [DocumentRecurrences] ([TemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentReminders_DocumentHeaderId] ON [DocumentReminders] ([DocumentHeaderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentReminders_DocumentScheduleId] ON [DocumentReminders] ([DocumentScheduleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentRetentionPolicies_DocumentTypeId] ON [DocumentRetentionPolicies] ([DocumentTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentRows_DestinationWarehouseId] ON [DocumentRows] ([DestinationWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentRows_DocumentHeaderId] ON [DocumentRows] ([DocumentHeaderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentRows_LocationId] ON [DocumentRows] ([LocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentRows_ProductId] ON [DocumentRows] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentRows_SourceWarehouseId] ON [DocumentRows] ([SourceWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentRows_StationId] ON [DocumentRows] ([StationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentRows_UnitOfMeasureEntityId] ON [DocumentRows] ([UnitOfMeasureEntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentSchedules_DocumentHeaderId] ON [DocumentSchedules] ([DocumentHeaderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentSchedules_DocumentTypeId] ON [DocumentSchedules] ([DocumentTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentSchedules_NextExecutionDate] ON [DocumentSchedules] ([NextExecutionDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentSummaryLinks_DetailedDocumentId] ON [DocumentSummaryLinks] ([DetailedDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentSummaryLinks_SummaryDocumentId] ON [DocumentSummaryLinks] ([SummaryDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentTemplates_DocumentTypeId] ON [DocumentTemplates] ([DocumentTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentTypes_DefaultWarehouseId] ON [DocumentTypes] ([DefaultWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentVersions_DocumentHeaderId] ON [DocumentVersions] ([DocumentHeaderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentVersionSignature_DocumentVersionId] ON [DocumentVersionSignature] ([DocumentVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentWorkflowExecutions_DocumentHeaderId] ON [DocumentWorkflowExecutions] ([DocumentHeaderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentWorkflowExecutions_WorkflowId] ON [DocumentWorkflowExecutions] ([WorkflowId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentWorkflows_DocumentTypeId] ON [DocumentWorkflows] ([DocumentTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentWorkflowStep_DocumentVersionId] ON [DocumentWorkflowStep] ([DocumentVersionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentWorkflowStep_StepDefinitionId] ON [DocumentWorkflowStep] ([StepDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentWorkflowStep_WorkflowExecutionId] ON [DocumentWorkflowStep] ([WorkflowExecutionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_DocumentWorkflowStepDefinition_WorkflowId] ON [DocumentWorkflowStepDefinition] ([WorkflowId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_InsurancePolicies_DocumentReferenceId] ON [InsurancePolicies] ([DocumentReferenceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_InsurancePolicies_TeamMemberId] ON [InsurancePolicies] ([TeamMemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_LicenseFeaturePermissions_LicenseFeatureId] ON [LicenseFeaturePermissions] ([LicenseFeatureId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_LicenseFeaturePermissions_PermissionId] ON [LicenseFeaturePermissions] ([PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_LicenseFeatures_LicenseId] ON [LicenseFeatures] ([LicenseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_LoginAudits_UserId] ON [LoginAudits] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Lots_ProductId] ON [Lots] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Lots_SupplierId] ON [Lots] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_MaintenanceRecords_SerialId] ON [MaintenanceRecords] ([SerialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_MembershipCards_DocumentReferenceId] ON [MembershipCards] ([DocumentReferenceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_MembershipCards_TeamMemberId] ON [MembershipCards] ([TeamMemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_MessageAttachments_MediaType] ON [MessageAttachments] ([MediaType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_MessageAttachments_MessageId] ON [MessageAttachments] ([MessageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_MessageAttachments_TenantId] ON [MessageAttachments] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_MessageAttachments_UploadedAt] ON [MessageAttachments] ([UploadedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_MessageAttachments_UploadedBy] ON [MessageAttachments] ([UploadedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_MessageReadReceipts_MessageId] ON [MessageReadReceipts] ([MessageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MessageReadReceipts_MessageId_UserId] ON [MessageReadReceipts] ([MessageId], [UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_MessageReadReceipts_ReadAt] ON [MessageReadReceipts] ([ReadAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_MessageReadReceipts_TenantId] ON [MessageReadReceipts] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_MessageReadReceipts_UserId] ON [MessageReadReceipts] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Models_BrandId] ON [Models] ([BrandId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_NotificationRecipients_NotificationId] ON [NotificationRecipients] ([NotificationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_NotificationRecipients_ReadAt] ON [NotificationRecipients] ([ReadAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_NotificationRecipients_Status] ON [NotificationRecipients] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_NotificationRecipients_TenantId] ON [NotificationRecipients] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_NotificationRecipients_UserId] ON [NotificationRecipients] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Notifications_CreatedAt] ON [Notifications] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Notifications_ExpiresAt] ON [Notifications] ([ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Notifications_IsArchived] ON [Notifications] ([IsArchived]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Notifications_Priority] ON [Notifications] ([Priority]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Notifications_Status] ON [Notifications] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Notifications_TenantId] ON [Notifications] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Notifications_Type] ON [Notifications] ([Type]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_PriceListEntries_PriceListId] ON [PriceListEntries] ([PriceListId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_PriceListEntries_ProductId] ON [PriceListEntries] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_PriceLists_EventId] ON [PriceLists] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Printers_StationId] ON [Printers] ([StationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ProductBundleItems_BundleProductId] ON [ProductBundleItems] ([BundleProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ProductBundleItems_ComponentProductId] ON [ProductBundleItems] ([ComponentProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ProductCodes_ProductId] ON [ProductCodes] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ProductCodes_ProductUnitId] ON [ProductCodes] ([ProductUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Product_BrandId] ON [Products] ([BrandId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Product_ImageDocumentId] ON [Products] ([ImageDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Product_ModelId] ON [Products] ([ModelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Product_PreferredSupplierId] ON [Products] ([PreferredSupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Products_CategoryNodeId] ON [Products] ([CategoryNodeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Products_FamilyNodeId] ON [Products] ([FamilyNodeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Products_GroupNodeId] ON [Products] ([GroupNodeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Products_StationId] ON [Products] ([StationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Products_UnitOfMeasureId] ON [Products] ([UnitOfMeasureId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Products_VatRateId] ON [Products] ([VatRateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ProductSupplier_ProductId] ON [ProductSuppliers] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ProductSupplier_ProductId_Preferred] ON [ProductSuppliers] ([ProductId], [Preferred]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ProductSupplier_SupplierId] ON [ProductSuppliers] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ProductUnits_ProductId] ON [ProductUnits] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ProductUnits_UnitOfMeasureId] ON [ProductUnits] ([UnitOfMeasureId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ProjectMaterialAllocations_LotId] ON [ProjectMaterialAllocations] ([LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ProjectMaterialAllocations_ProductId] ON [ProjectMaterialAllocations] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ProjectMaterialAllocations_ProjectOrderId] ON [ProjectMaterialAllocations] ([ProjectOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ProjectMaterialAllocations_SerialId] ON [ProjectMaterialAllocations] ([SerialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ProjectMaterialAllocations_StockMovementId] ON [ProjectMaterialAllocations] ([StockMovementId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ProjectMaterialAllocations_StorageLocationId] ON [ProjectMaterialAllocations] ([StorageLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ProjectMaterialAllocations_UnitOfMeasureId] ON [ProjectMaterialAllocations] ([UnitOfMeasureId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ProjectOrders_CustomerId] ON [ProjectOrders] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_ProjectOrders_StorageLocationId] ON [ProjectOrders] ([StorageLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_PromotionRuleProducts_ProductId] ON [PromotionRuleProducts] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_PromotionRuleProducts_PromotionRuleId] ON [PromotionRuleProducts] ([PromotionRuleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_PromotionRules_PromotionId] ON [PromotionRules] ([PromotionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_QualityControls_LotId] ON [QualityControls] ([LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_QualityControls_ProductId] ON [QualityControls] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_QualityControls_SerialId] ON [QualityControls] ([SerialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_References_BankId] ON [References] ([BankId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_References_BusinessPartyId] ON [References] ([BusinessPartyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_RoleId] ON [RolePermissions] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_SaleItems_SaleSessionId] ON [SaleItems] ([SaleSessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_SalePayments_PaymentMethodId] ON [SalePayments] ([PaymentMethodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_SalePayments_SaleSessionId] ON [SalePayments] ([SaleSessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Serials_CurrentLocationId] ON [Serials] ([CurrentLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Serials_LotId] ON [Serials] ([LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Serials_OwnerId] ON [Serials] ([OwnerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Serials_ProductId] ON [Serials] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_SessionNotes_NoteFlagId] ON [SessionNotes] ([NoteFlagId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_SessionNotes_SaleSessionId] ON [SessionNotes] ([SaleSessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StationOrderQueueItems_DocumentHeaderId] ON [StationOrderQueueItems] ([DocumentHeaderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StationOrderQueueItems_DocumentRowId] ON [StationOrderQueueItems] ([DocumentRowId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StationOrderQueueItems_ProductId] ON [StationOrderQueueItems] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StationOrderQueueItems_StationId] ON [StationOrderQueueItems] ([StationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StationOrderQueueItems_TeamMemberId] ON [StationOrderQueueItems] ([TeamMemberId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StockAlerts_StockId] ON [StockAlerts] ([StockId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StockMovementPlans_DocumentHeaderId] ON [StockMovementPlans] ([DocumentHeaderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StockMovementPlans_FromLocationId] ON [StockMovementPlans] ([FromLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StockMovementPlans_LotId] ON [StockMovementPlans] ([LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StockMovementPlans_ProductId] ON [StockMovementPlans] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StockMovementPlans_ToLocationId] ON [StockMovementPlans] ([ToLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StockMovements_DocumentHeaderId] ON [StockMovements] ([DocumentHeaderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StockMovements_DocumentRowId] ON [StockMovements] ([DocumentRowId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StockMovements_FromLocationId] ON [StockMovements] ([FromLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StockMovements_LotId] ON [StockMovements] ([LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StockMovements_MovementPlanId] ON [StockMovements] ([MovementPlanId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StockMovements_ProductId] ON [StockMovements] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StockMovements_ProjectOrderId] ON [StockMovements] ([ProjectOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StockMovements_SerialId] ON [StockMovements] ([SerialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StockMovements_StockId] ON [StockMovements] ([StockId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StockMovements_ToLocationId] ON [StockMovements] ([ToLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Stocks_LotId] ON [Stocks] ([LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Stocks_ProductId] ON [Stocks] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Stocks_StorageLocationId] ON [Stocks] ([StorageLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StorageLocations_WarehouseId] ON [StorageLocations] ([WarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StorePos_ImageDocumentId] ON [StorePoses] ([ImageDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StoreUserGroup_LogoDocumentId] ON [StoreUserGroups] ([LogoDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StoreUserGroupStoreUserPrivilege_PrivilegesId] ON [StoreUserGroupStoreUserPrivilege] ([PrivilegesId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StoreUser_PhotoDocumentId] ON [StoreUsers] ([PhotoDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_StoreUsers_CashierGroupId] ON [StoreUsers] ([CashierGroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_SustainabilityCertificates_LotId] ON [SustainabilityCertificates] ([LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_SustainabilityCertificates_ProductId] ON [SustainabilityCertificates] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_TableReservations_TableId] ON [TableReservations] ([TableId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_TableSessions_CurrentSaleSessionId] ON [TableSessions] ([CurrentSaleSessionId]) WHERE [CurrentSaleSessionId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_TeamMembers_PhotoDocumentId] ON [TeamMembers] ([PhotoDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_TeamMembers_TeamId] ON [TeamMembers] ([TeamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Teams_CoachContactId] ON [Teams] ([CoachContactId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Teams_EventId] ON [Teams] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Teams_TeamLogoDocumentId] ON [Teams] ([TeamLogoDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_TenantLicenses_LicenseId] ON [TenantLicenses] ([LicenseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_TenantLicenses_TenantId] ON [TenantLicenses] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_UserRoles_UserId] ON [UserRoles] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email_TenantId] ON [Users] ([Email], [TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_Users_TenantId] ON [Users] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Username_TenantId] ON [Users] ([Username], [TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_VatRates_VatNatureId] ON [VatRates] ([VatNatureId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_WasteManagementRecords_LotId] ON [WasteManagementRecords] ([LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_WasteManagementRecords_ProductId] ON [WasteManagementRecords] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_WasteManagementRecords_SerialId] ON [WasteManagementRecords] ([SerialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    CREATE INDEX [IX_WasteManagementRecords_StorageLocationId] ON [WasteManagementRecords] ([StorageLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    ALTER TABLE [DocumentAccessLogs] ADD CONSTRAINT [FK_DocumentAccessLogs_DocumentHeaders_DocumentHeaderId] FOREIGN KEY ([DocumentHeaderId]) REFERENCES [DocumentHeaders] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    ALTER TABLE [DocumentAnalytics] ADD CONSTRAINT [FK_DocumentAnalytics_DocumentHeaders_DocumentHeaderId] FOREIGN KEY ([DocumentHeaderId]) REFERENCES [DocumentHeaders] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    ALTER TABLE [DocumentAttachments] ADD CONSTRAINT [FK_DocumentAttachments_DocumentHeaders_DocumentHeaderId] FOREIGN KEY ([DocumentHeaderId]) REFERENCES [DocumentHeaders] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    ALTER TABLE [DocumentAttachments] ADD CONSTRAINT [FK_DocumentAttachments_DocumentRows_DocumentRowId] FOREIGN KEY ([DocumentRowId]) REFERENCES [DocumentRows] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    ALTER TABLE [DocumentComments] ADD CONSTRAINT [FK_DocumentComments_DocumentHeaders_DocumentHeaderId] FOREIGN KEY ([DocumentHeaderId]) REFERENCES [DocumentHeaders] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    ALTER TABLE [DocumentComments] ADD CONSTRAINT [FK_DocumentComments_DocumentRows_DocumentRowId] FOREIGN KEY ([DocumentRowId]) REFERENCES [DocumentRows] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    ALTER TABLE [DocumentHeaders] ADD CONSTRAINT [FK_DocumentHeaders_DocumentWorkflowExecutions_CurrentWorkflowExecutionId] FOREIGN KEY ([CurrentWorkflowExecutionId]) REFERENCES [DocumentWorkflowExecutions] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107081244_InitialCreation_20251107_0912'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251107081244_InitialCreation_20251107_0912', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251110132022_20251110_AUTOCODE_ART'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Code');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Products] ALTER COLUMN [Code] nvarchar(450) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251110132022_20251110_AUTOCODE_ART'
)
BEGIN
    CREATE TABLE [DailySequences] (
        [Date] date NOT NULL,
        [LastNumber] bigint NOT NULL,
        CONSTRAINT [PK_DailySequences] PRIMARY KEY ([Date])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251110132022_20251110_AUTOCODE_ART'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Products_Code] ON [Products] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251110132022_20251110_AUTOCODE_ART'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251110132022_20251110_AUTOCODE_ART', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251120101928_20251120'
)
BEGIN
    CREATE TABLE [DashboardConfigurations] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [EntityType] nvarchar(100) NOT NULL,
        [IsDefault] bit NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DashboardConfigurations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DashboardConfigurations_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251120101928_20251120'
)
BEGIN
    CREATE TABLE [DashboardMetricConfigs] (
        [Id] uniqueidentifier NOT NULL,
        [DashboardConfigurationId] uniqueidentifier NOT NULL,
        [Title] nvarchar(100) NOT NULL,
        [Type] int NOT NULL,
        [FieldName] nvarchar(100) NULL,
        [FilterCondition] nvarchar(500) NULL,
        [Format] nvarchar(20) NULL,
        [Icon] nvarchar(100) NULL,
        [Color] nvarchar(50) NULL,
        [Description] nvarchar(200) NULL,
        [Order] int NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_DashboardMetricConfigs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DashboardMetricConfigs_DashboardConfigurations_DashboardConfigurationId] FOREIGN KEY ([DashboardConfigurationId]) REFERENCES [DashboardConfigurations] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251120101928_20251120'
)
BEGIN
    CREATE INDEX [IX_DashboardConfigurations_UserId] ON [DashboardConfigurations] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251120101928_20251120'
)
BEGIN
    CREATE INDEX [IX_DashboardMetricConfigs_DashboardConfigurationId] ON [DashboardMetricConfigs] ([DashboardConfigurationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251120101928_20251120'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251120101928_20251120', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    ALTER TABLE [Users] ADD [AvatarDocumentId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    ALTER TABLE [Users] ADD [EmailNotificationsEnabled] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    ALTER TABLE [Users] ADD [InAppNotificationsEnabled] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    ALTER TABLE [Users] ADD [PhoneNumber] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    ALTER TABLE [Users] ADD [PreferredLanguage] nvarchar(10) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    ALTER TABLE [Users] ADD [PushNotificationsEnabled] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    ALTER TABLE [Users] ADD [TimeZone] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DashboardMetricConfigs]') AND [c].[name] = N'Icon');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [DashboardMetricConfigs] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [DashboardMetricConfigs] ALTER COLUMN [Icon] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE TABLE [AlertConfigurations] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] nvarchar(100) NOT NULL,
        [PriceIncreaseThresholdPercentage] decimal(18,6) NOT NULL,
        [PriceDecreaseThresholdPercentage] decimal(18,6) NOT NULL,
        [VolatilityThresholdPercentage] decimal(18,6) NOT NULL,
        [DaysWithoutUpdateThreshold] int NOT NULL,
        [EnableEmailNotifications] bit NOT NULL,
        [EnableBrowserNotifications] bit NOT NULL,
        [AlertOnPriceIncrease] bit NOT NULL,
        [AlertOnPriceDecrease] bit NOT NULL,
        [AlertOnBetterSupplier] bit NOT NULL,
        [AlertOnVolatility] bit NOT NULL,
        [NotificationFrequency] int NOT NULL,
        [LastDigestSentAt] datetime2 NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_AlertConfigurations] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE TABLE [SupplierPriceAlerts] (
        [Id] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NULL,
        [SupplierId] uniqueidentifier NULL,
        [AlertType] int NOT NULL,
        [Severity] int NOT NULL,
        [Status] int NOT NULL,
        [OldPrice] decimal(18,6) NULL,
        [NewPrice] decimal(18,6) NULL,
        [PriceChangePercentage] decimal(18,6) NULL,
        [Currency] nvarchar(10) NOT NULL,
        [PotentialSavings] decimal(18,6) NULL,
        [AlertTitle] nvarchar(200) NOT NULL,
        [AlertMessage] nvarchar(1000) NOT NULL,
        [RecommendedAction] nvarchar(500) NULL,
        [BetterSupplierSuggestionId] uniqueidentifier NULL,
        [AcknowledgedAt] datetime2 NULL,
        [AcknowledgedByUserId] nvarchar(100) NULL,
        [ResolvedAt] datetime2 NULL,
        [ResolvedByUserId] nvarchar(100) NULL,
        [ResolutionNotes] nvarchar(1000) NULL,
        [EmailSent] bit NOT NULL,
        [EmailSentAt] datetime2 NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_SupplierPriceAlerts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SupplierPriceAlerts_BusinessParties_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [BusinessParties] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SupplierPriceAlerts_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE TABLE [SupplierProductPriceHistories] (
        [Id] uniqueidentifier NOT NULL,
        [ProductSupplierId] uniqueidentifier NOT NULL,
        [SupplierId] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [OldUnitCost] decimal(18,6) NOT NULL,
        [NewUnitCost] decimal(18,6) NOT NULL,
        [PriceChange] decimal(18,6) NOT NULL,
        [PriceChangePercentage] decimal(18,6) NOT NULL,
        [Currency] nvarchar(10) NOT NULL,
        [OldLeadTimeDays] int NULL,
        [NewLeadTimeDays] int NULL,
        [ChangedAt] datetime2 NOT NULL,
        [ChangedByUserId] uniqueidentifier NOT NULL,
        [ChangeSource] nvarchar(50) NOT NULL,
        [ChangeReason] nvarchar(500) NULL,
        [Notes] nvarchar(1000) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_SupplierProductPriceHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SupplierProductPriceHistories_BusinessParties_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [BusinessParties] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SupplierProductPriceHistories_ProductSuppliers_ProductSupplierId] FOREIGN KEY ([ProductSupplierId]) REFERENCES [ProductSuppliers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_SupplierProductPriceHistories_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SupplierProductPriceHistories_Users_ChangedByUserId] FOREIGN KEY ([ChangedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE TABLE [TransferOrders] (
        [Id] uniqueidentifier NOT NULL,
        [Number] nvarchar(50) NOT NULL,
        [Series] nvarchar(20) NULL,
        [OrderDate] datetime2 NOT NULL,
        [SourceWarehouseId] uniqueidentifier NOT NULL,
        [DestinationWarehouseId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [ShipmentDate] datetime2 NULL,
        [ExpectedArrivalDate] datetime2 NULL,
        [ActualArrivalDate] datetime2 NULL,
        [Notes] nvarchar(1000) NULL,
        [ShippingReference] nvarchar(200) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_TransferOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TransferOrders_StorageFacilities_DestinationWarehouseId] FOREIGN KEY ([DestinationWarehouseId]) REFERENCES [StorageFacilities] ([Id]),
        CONSTRAINT [FK_TransferOrders_StorageFacilities_SourceWarehouseId] FOREIGN KEY ([SourceWarehouseId]) REFERENCES [StorageFacilities] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE TABLE [TransferOrderRows] (
        [Id] uniqueidentifier NOT NULL,
        [TransferOrderId] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [SourceLocationId] uniqueidentifier NOT NULL,
        [DestinationLocationId] uniqueidentifier NULL,
        [QuantityOrdered] decimal(18,6) NOT NULL,
        [QuantityShipped] decimal(18,6) NOT NULL,
        [QuantityReceived] decimal(18,6) NOT NULL,
        [LotId] uniqueidentifier NULL,
        [Notes] nvarchar(500) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_TransferOrderRows] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TransferOrderRows_Lots_LotId] FOREIGN KEY ([LotId]) REFERENCES [Lots] ([Id]),
        CONSTRAINT [FK_TransferOrderRows_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TransferOrderRows_StorageLocations_DestinationLocationId] FOREIGN KEY ([DestinationLocationId]) REFERENCES [StorageLocations] ([Id]),
        CONSTRAINT [FK_TransferOrderRows_StorageLocations_SourceLocationId] FOREIGN KEY ([SourceLocationId]) REFERENCES [StorageLocations] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TransferOrderRows_TransferOrders_TransferOrderId] FOREIGN KEY ([TransferOrderId]) REFERENCES [TransferOrders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_Users_AvatarDocumentId] ON [Users] ([AvatarDocumentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_AlertConfiguration_TenantId] ON [AlertConfigurations] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AlertConfiguration_TenantId_UserId] ON [AlertConfigurations] ([TenantId], [UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_SupplierPriceAlert_CreatedAt] ON [SupplierPriceAlerts] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_SupplierPriceAlert_ProductId] ON [SupplierPriceAlerts] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_SupplierPriceAlert_Status] ON [SupplierPriceAlerts] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_SupplierPriceAlert_SupplierId] ON [SupplierPriceAlerts] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_SupplierPriceAlert_TenantId] ON [SupplierPriceAlerts] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_SupplierPriceAlert_TenantId_Status] ON [SupplierPriceAlerts] ([TenantId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_SupplierPriceAlert_TenantId_Status_CreatedAt] ON [SupplierPriceAlerts] ([TenantId], [Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_SupplierProductPriceHistories_ChangedByUserId] ON [SupplierProductPriceHistories] ([ChangedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_SupplierProductPriceHistory_ChangedAt] ON [SupplierProductPriceHistories] ([ChangedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_SupplierProductPriceHistory_ProductId] ON [SupplierProductPriceHistories] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_SupplierProductPriceHistory_ProductId_ChangedAt] ON [SupplierProductPriceHistories] ([ProductId], [ChangedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_SupplierProductPriceHistory_ProductSupplierId] ON [SupplierProductPriceHistories] ([ProductSupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_SupplierProductPriceHistory_SupplierId] ON [SupplierProductPriceHistories] ([SupplierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_SupplierProductPriceHistory_SupplierId_ChangedAt] ON [SupplierProductPriceHistories] ([SupplierId], [ChangedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_TransferOrderRows_DestinationLocationId] ON [TransferOrderRows] ([DestinationLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_TransferOrderRows_LotId] ON [TransferOrderRows] ([LotId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_TransferOrderRows_ProductId] ON [TransferOrderRows] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_TransferOrderRows_SourceLocationId] ON [TransferOrderRows] ([SourceLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_TransferOrderRows_TransferOrderId] ON [TransferOrderRows] ([TransferOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_TransferOrders_DestinationWarehouseId] ON [TransferOrders] ([DestinationWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    CREATE INDEX [IX_TransferOrders_SourceWarehouseId] ON [TransferOrders] ([SourceWarehouseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_DocumentReferences_AvatarDocumentId] FOREIGN KEY ([AvatarDocumentId]) REFERENCES [DocumentReferences] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251202135352_20251202_1453'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251202135352_20251202_1453', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251203144734_BAUBAU'
)
BEGIN
    ALTER TABLE [EntityChangeLogs] ADD [TenantId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251203144734_BAUBAU'
)
BEGIN
    CREATE INDEX [IX_EntityChangeLogs_TenantId] ON [EntityChangeLogs] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251203144734_BAUBAU'
)
BEGIN
    CREATE INDEX [IX_EntityChangeLogs_TenantId_EntityName_ChangedAt] ON [EntityChangeLogs] ([TenantId], [EntityName], [ChangedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251203144734_BAUBAU'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251203144734_BAUBAU', N'10.0.0');
END;

COMMIT;
GO

