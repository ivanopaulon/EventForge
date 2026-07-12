-- Migration: Add FidelityTiers and FidelityTierRules; migrate FidelityCardType enum to tier references
-- Date: 2026-07-11
-- Description: Replaces the fixed FidelityCardType enum (Bronze/Silver/Gold/Platinum) with manageable,
-- tenant-scoped FidelityTiers + FidelityTierRules. Adds TierId/TierEnteredAt to FidelityCards and
-- retypes FidelityTierMultipliers.CardType to TierId. The old FidelityCards.Type column is retained
-- for backward compatibility and is NOT dropped.

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- =============================================
-- 1. Create FidelityTiers table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FidelityTiers]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[FidelityTiers]
    (
        [Id]         uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        [TenantId]   uniqueidentifier NOT NULL,
        [Name]       nvarchar(50)     NOT NULL,
        [SortOrder]  int              NOT NULL DEFAULT 0,
        [Color]      nvarchar(50)     NULL,
        [Icon]       nvarchar(100)    NULL,
        [IsActive]   bit              NOT NULL DEFAULT 1,
        [IsDeleted]  bit              NOT NULL DEFAULT 0,
        [CreatedAt]  datetime2(7)     NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]  nvarchar(100)    NULL,
        [ModifiedAt] datetime2(7)     NULL,
        [ModifiedBy] nvarchar(100)    NULL,
        [DeletedAt]  datetime2(7)     NULL,
        [DeletedBy]  nvarchar(100)    NULL,
        [RowVersion] rowversion       NOT NULL,
        CONSTRAINT [PK_FidelityTiers] PRIMARY KEY ([Id])
    );
    PRINT 'FidelityTiers table created successfully.';
END
ELSE
    PRINT 'FidelityTiers table already exists — skipping creation.';

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FidelityTiers_TenantId_SortOrder' AND object_id = OBJECT_ID('dbo.FidelityTiers'))
BEGIN
    CREATE INDEX [IX_FidelityTiers_TenantId_SortOrder]
        ON [dbo].[FidelityTiers] ([TenantId], [SortOrder])
        WHERE [IsDeleted] = 0;
END

-- =============================================
-- 2. Create FidelityTierRules table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FidelityTierRules]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[FidelityTierRules]
    (
        [Id]                     uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        [TenantId]               uniqueidentifier NOT NULL,
        [TierId]                 uniqueidentifier NOT NULL,
        [MinimumSpendThreshold]  decimal(18,2)    NULL,
        [EvaluationPeriodMonths] int              NOT NULL DEFAULT 12,
        [IsActive]               bit              NOT NULL DEFAULT 1,
        [IsDeleted]              bit              NOT NULL DEFAULT 0,
        [CreatedAt]              datetime2(7)     NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]              nvarchar(100)    NULL,
        [ModifiedAt]             datetime2(7)     NULL,
        [ModifiedBy]             nvarchar(100)    NULL,
        [DeletedAt]              datetime2(7)     NULL,
        [DeletedBy]              nvarchar(100)    NULL,
        [RowVersion]             rowversion       NOT NULL,
        CONSTRAINT [PK_FidelityTierRules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FidelityTierRules_FidelityTiers_TierId]
            FOREIGN KEY ([TierId]) REFERENCES [dbo].[FidelityTiers] ([Id]) ON DELETE CASCADE
    );
    PRINT 'FidelityTierRules table created successfully.';
END
ELSE
    PRINT 'FidelityTierRules table already exists — skipping creation.';

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FidelityTierRules_TenantId_TierId' AND object_id = OBJECT_ID('dbo.FidelityTierRules'))
BEGIN
    CREATE INDEX [IX_FidelityTierRules_TenantId_TierId]
        ON [dbo].[FidelityTierRules] ([TenantId], [TierId])
        WHERE [IsDeleted] = 0;
END

-- =============================================
-- 3. Seed 4 default tiers per existing tenant (idempotent by TenantId + SortOrder)
-- =============================================
;WITH DefaultTiers AS (
    SELECT * FROM (VALUES
        (N'Bronze',   0, N'#CD7F32', N'Circle'),
        (N'Silver',   1, N'#C0C0C0', N'Grade'),
        (N'Gold',     2, N'#FFD700', N'Star'),
        (N'Platinum', 3, N'#E5E4E2', N'Diamond')
    ) AS v([Name], [SortOrder], [Color], [Icon])
)
INSERT INTO [dbo].[FidelityTiers] ([Id], [TenantId], [Name], [SortOrder], [Color], [Icon], [IsActive], [IsDeleted], [CreatedAt], [CreatedBy])
SELECT NEWID(), t.[Id], d.[Name], d.[SortOrder], d.[Color], d.[Icon], 1, 0, GETUTCDATE(), N'System'
FROM [dbo].[Tenants] t
CROSS JOIN DefaultTiers d
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[FidelityTiers] ft
    WHERE ft.[TenantId] = t.[Id] AND ft.[SortOrder] = d.[SortOrder] AND ft.[IsDeleted] = 0
);
PRINT 'Default fidelity tiers seeded for existing tenants.';

-- =============================================
-- 4. Seed one rule per seeded tier (idempotent by TierId).
-- NOTE: MinimumSpendThreshold values below are ILLUSTRATIVE PLACEHOLDERS ONLY — deliberately round
-- numbers. Real per-tenant thresholds MUST be configured later via the Fidelity Tier management UI.
-- Bronze (base) has NULL threshold (no spend requirement).
-- =============================================
INSERT INTO [dbo].[FidelityTierRules] ([Id], [TenantId], [TierId], [MinimumSpendThreshold], [EvaluationPeriodMonths], [IsActive], [IsDeleted], [CreatedAt], [CreatedBy])
SELECT NEWID(), ft.[TenantId], ft.[Id],
    CASE ft.[SortOrder]
        WHEN 0 THEN NULL          -- Bronze: no spend requirement
        WHEN 1 THEN CAST(1000 AS decimal(18,2))   -- placeholder
        WHEN 2 THEN CAST(5000 AS decimal(18,2))   -- placeholder
        WHEN 3 THEN CAST(10000 AS decimal(18,2))  -- placeholder
        ELSE NULL
    END,
    12, 1, 0, GETUTCDATE(), N'System'
FROM [dbo].[FidelityTiers] ft
WHERE ft.[IsDeleted] = 0
  AND NOT EXISTS (SELECT 1 FROM [dbo].[FidelityTierRules] r WHERE r.[TierId] = ft.[Id] AND r.[IsDeleted] = 0);
PRINT 'Placeholder fidelity tier rules seeded (thresholds are examples only).';

-- =============================================
-- 5. Add TierId / TierEnteredAt columns to FidelityCards (do NOT drop Type)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[FidelityCards]') AND name = 'TierId')
BEGIN
    ALTER TABLE [dbo].[FidelityCards] ADD [TierId] uniqueidentifier NULL;
    PRINT 'TierId column added to FidelityCards.';
END
ELSE
    PRINT 'TierId column already exists on FidelityCards — skipping.';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[FidelityCards]') AND name = 'TierEnteredAt')
BEGIN
    ALTER TABLE [dbo].[FidelityCards] ADD [TierEnteredAt] datetime2(7) NULL;
    PRINT 'TierEnteredAt column added to FidelityCards.';
END
ELSE
    PRINT 'TierEnteredAt column already exists on FidelityCards — skipping.';

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_FidelityCards_FidelityTiers_TierId')
BEGIN
    ALTER TABLE [dbo].[FidelityCards]
        ADD CONSTRAINT [FK_FidelityCards_FidelityTiers_TierId]
        FOREIGN KEY ([TierId]) REFERENCES [dbo].[FidelityTiers] ([Id]) ON DELETE SET NULL;
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FidelityCards_TierId' AND object_id = OBJECT_ID('dbo.FidelityCards'))
BEGIN
    CREATE INDEX [IX_FidelityCards_TierId] ON [dbo].[FidelityCards] ([TierId]);
END

-- =============================================
-- 6. Migrate existing FidelityCards.Type (enum int) -> TierId (per-tenant tier of matching SortOrder)
-- =============================================
UPDATE fc
SET fc.[TierId] = ft.[Id],
    fc.[TierEnteredAt] = COALESCE(fc.[TierEnteredAt], fc.[CreatedAt], GETUTCDATE())
FROM [dbo].[FidelityCards] fc
INNER JOIN [dbo].[FidelityTiers] ft
    ON ft.[TenantId] = fc.[TenantId] AND ft.[SortOrder] = fc.[Type] AND ft.[IsDeleted] = 0
WHERE fc.[TierId] IS NULL;
PRINT 'Existing fidelity cards migrated from Type enum to TierId.';

-- =============================================
-- 7. Retype FidelityTierMultipliers.CardType -> TierId
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[FidelityTierMultipliers]') AND name = 'TierId')
BEGIN
    ALTER TABLE [dbo].[FidelityTierMultipliers] ADD [TierId] uniqueidentifier NULL;
    PRINT 'TierId column added to FidelityTierMultipliers.';
END
ELSE
    PRINT 'TierId column already exists on FidelityTierMultipliers — skipping.';

-- Migrate CardType (enum int) -> TierId using tenant + SortOrder match (only if CardType still present)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[FidelityTierMultipliers]') AND name = 'CardType')
BEGIN
    UPDATE m
    SET m.[TierId] = ft.[Id]
    FROM [dbo].[FidelityTierMultipliers] m
    INNER JOIN [dbo].[FidelityTiers] ft
        ON ft.[TenantId] = m.[TenantId] AND ft.[SortOrder] = m.[CardType] AND ft.[IsDeleted] = 0
    WHERE m.[TierId] IS NULL;
    PRINT 'FidelityTierMultipliers migrated from CardType to TierId.';
END

-- Drop the old campaign-scoped unique index on CardType, replace with one on TierId
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FidelityTierMultipliers_CampaignId_CardType' AND object_id = OBJECT_ID('dbo.FidelityTierMultipliers'))
BEGIN
    DROP INDEX [IX_FidelityTierMultipliers_CampaignId_CardType] ON [dbo].[FidelityTierMultipliers];
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_FidelityTierMultipliers_FidelityTiers_TierId')
BEGIN
    ALTER TABLE [dbo].[FidelityTierMultipliers]
        ADD CONSTRAINT [FK_FidelityTierMultipliers_FidelityTiers_TierId]
        FOREIGN KEY ([TierId]) REFERENCES [dbo].[FidelityTiers] ([Id]);
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FidelityTierMultipliers_CampaignId_TierId' AND object_id = OBJECT_ID('dbo.FidelityTierMultipliers'))
BEGIN
    CREATE UNIQUE INDEX [IX_FidelityTierMultipliers_CampaignId_TierId]
        ON [dbo].[FidelityTierMultipliers] ([CampaignId], [TierId])
        WHERE [IsDeleted] = 0 AND [CampaignId] IS NOT NULL AND [TierId] IS NOT NULL;
END

-- Drop the now-unused CardType column (the entity no longer maps it; CampaignId FK is untouched)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[FidelityTierMultipliers]') AND name = 'CardType')
BEGIN
    DECLARE @cardTypeDefault nvarchar(200);
    SELECT @cardTypeDefault = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[FidelityTierMultipliers]') AND c.name = 'CardType';
    IF @cardTypeDefault IS NOT NULL
        EXEC('ALTER TABLE [dbo].[FidelityTierMultipliers] DROP CONSTRAINT [' + @cardTypeDefault + ']');
    ALTER TABLE [dbo].[FidelityTierMultipliers] DROP COLUMN [CardType];
    PRINT 'CardType column dropped from FidelityTierMultipliers.';
END

COMMIT TRANSACTION;
PRINT 'Migration 20260711_AddFidelityTiers applied successfully.';
