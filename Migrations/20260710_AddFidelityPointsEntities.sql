-- Migration: Add FidelityPointsBaseRates, FidelityTierMultipliers, and FidelityPointsCampaigns tables
-- Date: 2026-07-10
-- Description: Creates fidelity points entities backed by EF instead of SystemConfigurations.

IF NOT EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[FidelityPointsBaseRates]') AND type IN (N'U')
)
BEGIN
    CREATE TABLE [dbo].[FidelityPointsBaseRates]
    (
        [Id]            uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        [TenantId]      uniqueidentifier NOT NULL,
        [Rate]          decimal(18,6)    NOT NULL DEFAULT 1.0,
        [RoundingMode]  int              NOT NULL DEFAULT 0,
        [EffectiveFrom] datetime2(7)     NOT NULL,
        [EffectiveTo]   datetime2(7)     NULL,
        [IsActive]      bit              NOT NULL DEFAULT 1,
        [IsDeleted]     bit              NOT NULL DEFAULT 0,
        [CreatedAt]     datetime2(7)     NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]     nvarchar(100)    NULL,
        [ModifiedAt]    datetime2(7)     NULL,
        [ModifiedBy]    nvarchar(100)    NULL,
        [DeletedAt]     datetime2(7)     NULL,
        [DeletedBy]     nvarchar(100)    NULL,
        [RowVersion]    rowversion       NOT NULL,
        CONSTRAINT [PK_FidelityPointsBaseRates] PRIMARY KEY ([Id])
    );

    PRINT 'FidelityPointsBaseRates table created successfully.';
END
ELSE
BEGIN
    PRINT 'FidelityPointsBaseRates table already exists — skipping creation.';
END

IF NOT EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[FidelityTierMultipliers]') AND type IN (N'U')
)
BEGIN
    CREATE TABLE [dbo].[FidelityTierMultipliers]
    (
        [Id]         uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        [TenantId]   uniqueidentifier NOT NULL,
        [CardType]   int              NOT NULL DEFAULT 0,
        [Multiplier] decimal(18,6)    NOT NULL DEFAULT 1.0,
        [IsActive]   bit              NOT NULL DEFAULT 1,
        [IsDeleted]  bit              NOT NULL DEFAULT 0,
        [CreatedAt]  datetime2(7)     NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]  nvarchar(100)    NULL,
        [ModifiedAt] datetime2(7)     NULL,
        [ModifiedBy] nvarchar(100)    NULL,
        [DeletedAt]  datetime2(7)     NULL,
        [DeletedBy]  nvarchar(100)    NULL,
        [RowVersion] rowversion       NOT NULL,
        CONSTRAINT [PK_FidelityTierMultipliers] PRIMARY KEY ([Id])
    );

    PRINT 'FidelityTierMultipliers table created successfully.';
END
ELSE
BEGIN
    PRINT 'FidelityTierMultipliers table already exists — skipping creation.';
END

IF NOT EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[FidelityPointsCampaigns]') AND type IN (N'U')
)
BEGIN
    CREATE TABLE [dbo].[FidelityPointsCampaigns]
    (
        [Id]                   uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        [TenantId]             uniqueidentifier NOT NULL,
        [Name]                 nvarchar(200)    NOT NULL,
        [StartDate]            datetime2(7)     NOT NULL,
        [EndDate]              datetime2(7)     NOT NULL,
        [Multiplier]           decimal(18,6)    NOT NULL DEFAULT 1.0,
        [RoundingMode]         int              NOT NULL DEFAULT 0,
        [IgnoreTierMultiplier] bit              NOT NULL DEFAULT 0,
        [ProductIdsJSON]       nvarchar(max)    NULL,
        [CategoryIdsJSON]      nvarchar(max)    NULL,
        [IsActive]             bit              NOT NULL DEFAULT 1,
        [IsDeleted]            bit              NOT NULL DEFAULT 0,
        [CreatedAt]            datetime2(7)     NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]            nvarchar(100)    NULL,
        [ModifiedAt]           datetime2(7)     NULL,
        [ModifiedBy]           nvarchar(100)    NULL,
        [DeletedAt]            datetime2(7)     NULL,
        [DeletedBy]            nvarchar(100)    NULL,
        [RowVersion]           rowversion       NOT NULL,
        CONSTRAINT [PK_FidelityPointsCampaigns] PRIMARY KEY ([Id])
    );

    PRINT 'FidelityPointsCampaigns table created successfully.';
END
ELSE
BEGIN
    PRINT 'FidelityPointsCampaigns table already exists — skipping creation.';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FidelityPointsBaseRates_TenantId_EffectiveFrom' AND object_id = OBJECT_ID('dbo.FidelityPointsBaseRates'))
BEGIN
    CREATE INDEX [IX_FidelityPointsBaseRates_TenantId_EffectiveFrom]
        ON [dbo].[FidelityPointsBaseRates] ([TenantId], [EffectiveFrom])
        WHERE [IsDeleted] = 0;
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FidelityTierMultipliers_TenantId_CardType' AND object_id = OBJECT_ID('dbo.FidelityTierMultipliers'))
BEGIN
    CREATE UNIQUE INDEX [IX_FidelityTierMultipliers_TenantId_CardType]
        ON [dbo].[FidelityTierMultipliers] ([TenantId], [CardType])
        WHERE [IsDeleted] = 0;
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FidelityPointsCampaigns_TenantId_StartDate_EndDate' AND object_id = OBJECT_ID('dbo.FidelityPointsCampaigns'))
BEGIN
    CREATE INDEX [IX_FidelityPointsCampaigns_TenantId_StartDate_EndDate]
        ON [dbo].[FidelityPointsCampaigns] ([TenantId], [StartDate], [EndDate])
        WHERE [IsDeleted] = 0;
END

PRINT 'Migration 20260710_AddFidelityPointsEntities applied successfully.';
