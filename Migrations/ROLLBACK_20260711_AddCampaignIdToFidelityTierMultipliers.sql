-- Rollback: Remove CampaignId from FidelityTierMultipliers, restore IgnoreTierMultiplier on FidelityPointsCampaigns
-- Date: 2026-07-11

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FidelityTierMultipliers_CampaignId_CardType' AND object_id = OBJECT_ID('dbo.FidelityTierMultipliers'))
BEGIN
    DROP INDEX [IX_FidelityTierMultipliers_CampaignId_CardType] ON [dbo].[FidelityTierMultipliers];
END

IF EXISTS (SELECT * FROM sys.default_constraints dc JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[FidelityPointsCampaigns]') AND c.name = 'IgnoreTierMultiplier')
BEGIN
    DECLARE @constraintName nvarchar(200);
    SELECT @constraintName = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[FidelityPointsCampaigns]') AND c.name = 'IgnoreTierMultiplier';
    IF @constraintName IS NOT NULL
        EXEC('ALTER TABLE [dbo].[FidelityPointsCampaigns] DROP CONSTRAINT [' + @constraintName + ']');
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[FidelityPointsCampaigns]') AND name = 'IgnoreTierMultiplier')
BEGIN
    ALTER TABLE [dbo].[FidelityPointsCampaigns] ADD [IgnoreTierMultiplier] bit NOT NULL DEFAULT 0;
    PRINT 'IgnoreTierMultiplier column restored on FidelityPointsCampaigns.';
END

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_FidelityTierMultipliers_FidelityPointsCampaigns_CampaignId')
BEGIN
    ALTER TABLE [dbo].[FidelityTierMultipliers] DROP CONSTRAINT [FK_FidelityTierMultipliers_FidelityPointsCampaigns_CampaignId];
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[FidelityTierMultipliers]') AND name = 'CampaignId')
BEGIN
    ALTER TABLE [dbo].[FidelityTierMultipliers] DROP COLUMN [CampaignId];
    PRINT 'CampaignId column dropped from FidelityTierMultipliers.';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FidelityTierMultipliers_TenantId_CardType' AND object_id = OBJECT_ID('dbo.FidelityTierMultipliers'))
BEGIN
    CREATE UNIQUE INDEX [IX_FidelityTierMultipliers_TenantId_CardType]
        ON [dbo].[FidelityTierMultipliers] ([TenantId], [CardType])
        WHERE [IsDeleted] = 0;
END

PRINT 'Rollback 20260711_AddCampaignIdToFidelityTierMultipliers applied successfully.';
