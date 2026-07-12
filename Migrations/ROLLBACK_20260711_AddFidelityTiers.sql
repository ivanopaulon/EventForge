-- Rollback: 20260711_AddFidelityTiers
-- Date: 2026-07-11
-- Description: Reverts the FidelityTiers/FidelityTierRules migration. Restores FidelityTierMultipliers.CardType
-- (repopulating from the seeded tiers' SortOrder), removes the TierId/TierEnteredAt columns from FidelityCards,
-- and drops the FidelityTiers/FidelityTierRules tables. The FidelityCards.Type enum column was never dropped by
-- the forward migration, so card levels are preserved.

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- =============================================
-- 1. Restore FidelityTierMultipliers.CardType from TierId
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[FidelityTierMultipliers]') AND name = 'CardType')
BEGIN
    ALTER TABLE [dbo].[FidelityTierMultipliers] ADD [CardType] int NOT NULL DEFAULT 0;
    PRINT 'CardType column restored on FidelityTierMultipliers.';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[FidelityTierMultipliers]') AND name = 'TierId')
BEGIN
    UPDATE m
    SET m.[CardType] = ft.[SortOrder]
    FROM [dbo].[FidelityTierMultipliers] m
    INNER JOIN [dbo].[FidelityTiers] ft ON ft.[Id] = m.[TierId];
END

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FidelityTierMultipliers_CampaignId_TierId' AND object_id = OBJECT_ID('dbo.FidelityTierMultipliers'))
BEGIN
    DROP INDEX [IX_FidelityTierMultipliers_CampaignId_TierId] ON [dbo].[FidelityTierMultipliers];
END

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_FidelityTierMultipliers_FidelityTiers_TierId')
BEGIN
    ALTER TABLE [dbo].[FidelityTierMultipliers] DROP CONSTRAINT [FK_FidelityTierMultipliers_FidelityTiers_TierId];
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[FidelityTierMultipliers]') AND name = 'TierId')
BEGIN
    ALTER TABLE [dbo].[FidelityTierMultipliers] DROP COLUMN [TierId];
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FidelityTierMultipliers_CampaignId_CardType' AND object_id = OBJECT_ID('dbo.FidelityTierMultipliers'))
BEGIN
    CREATE UNIQUE INDEX [IX_FidelityTierMultipliers_CampaignId_CardType]
        ON [dbo].[FidelityTierMultipliers] ([CampaignId], [CardType])
        WHERE [IsDeleted] = 0 AND [CampaignId] IS NOT NULL;
END

-- =============================================
-- 2. Remove TierId / TierEnteredAt from FidelityCards
-- =============================================
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FidelityCards_TierId' AND object_id = OBJECT_ID('dbo.FidelityCards'))
BEGIN
    DROP INDEX [IX_FidelityCards_TierId] ON [dbo].[FidelityCards];
END

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_FidelityCards_FidelityTiers_TierId')
BEGIN
    ALTER TABLE [dbo].[FidelityCards] DROP CONSTRAINT [FK_FidelityCards_FidelityTiers_TierId];
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[FidelityCards]') AND name = 'TierId')
BEGIN
    ALTER TABLE [dbo].[FidelityCards] DROP COLUMN [TierId];
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[FidelityCards]') AND name = 'TierEnteredAt')
BEGIN
    ALTER TABLE [dbo].[FidelityCards] DROP COLUMN [TierEnteredAt];
END

-- =============================================
-- 3. Drop FidelityTierRules and FidelityTiers tables
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FidelityTierRules]') AND type IN (N'U'))
BEGIN
    DROP TABLE [dbo].[FidelityTierRules];
    PRINT 'FidelityTierRules table dropped.';
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FidelityTiers]') AND type IN (N'U'))
BEGIN
    DROP TABLE [dbo].[FidelityTiers];
    PRINT 'FidelityTiers table dropped.';
END

COMMIT TRANSACTION;
PRINT 'Rollback 20260711_AddFidelityTiers applied successfully.';
