-- Migration: Add CampaignId to FidelityTierMultipliers, remove IgnoreTierMultiplier from FidelityPointsCampaigns
-- Date: 2026-07-11
-- Description: FidelityTierMultiplier becomes a per-campaign entity — each campaign configures its
-- own tier multipliers instead of sharing a single tenant-wide set. IgnoreTierMultiplier no longer
-- makes sense once multipliers are scoped per campaign (nothing shared left to "ignore").

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[FidelityTierMultipliers]') AND name = 'CampaignId')
BEGIN
    ALTER TABLE [dbo].[FidelityTierMultipliers] ADD [CampaignId] uniqueidentifier NULL;
    -- Nullable during migration to avoid breaking existing rows. These existing rows were the old
    -- tenant-wide multipliers and have no natural campaign to attach to.
    -- Run: SELECT COUNT(*) FROM FidelityTierMultipliers WHERE CampaignId IS NULL
    -- after this migration to see how many rows are now orphaned, and decide manually whether to
    -- delete them or leave them for cleanup — do not delete automatically here without confirmation.
    ALTER TABLE [dbo].[FidelityTierMultipliers]
        ADD CONSTRAINT [FK_FidelityTierMultipliers_FidelityPointsCampaigns_CampaignId]
        FOREIGN KEY ([CampaignId]) REFERENCES [dbo].[FidelityPointsCampaigns] ([Id]) ON DELETE CASCADE;
    PRINT 'CampaignId column added to FidelityTierMultipliers.';
END
ELSE
BEGIN
    PRINT 'CampaignId column already exists on FidelityTierMultipliers — skipping.';
END

-- The previous tenant-wide uniqueness (TenantId, CardType) no longer applies now that multipliers
-- are scoped per campaign; replace it with a per-campaign uniqueness constraint.
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FidelityTierMultipliers_TenantId_CardType' AND object_id = OBJECT_ID('dbo.FidelityTierMultipliers'))
BEGIN
    DROP INDEX [IX_FidelityTierMultipliers_TenantId_CardType] ON [dbo].[FidelityTierMultipliers];
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FidelityTierMultipliers_CampaignId_CardType' AND object_id = OBJECT_ID('dbo.FidelityTierMultipliers'))
BEGIN
    CREATE UNIQUE INDEX [IX_FidelityTierMultipliers_CampaignId_CardType]
        ON [dbo].[FidelityTierMultipliers] ([CampaignId], [CardType])
        WHERE [IsDeleted] = 0 AND [CampaignId] IS NOT NULL;
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[FidelityPointsCampaigns]') AND name = 'IgnoreTierMultiplier')
BEGIN
    ALTER TABLE [dbo].[FidelityPointsCampaigns] DROP COLUMN [IgnoreTierMultiplier];
    PRINT 'IgnoreTierMultiplier column dropped from FidelityPointsCampaigns.';
END
ELSE
BEGIN
    PRINT 'IgnoreTierMultiplier column already absent on FidelityPointsCampaigns — skipping.';
END

PRINT 'Migration 20260711_AddCampaignIdToFidelityTierMultipliers applied successfully.';
