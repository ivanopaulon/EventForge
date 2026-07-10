-- Rollback: Remove FidelityPointsBaseRates, FidelityTierMultipliers, and FidelityPointsCampaigns tables
-- Date: 2026-07-10

IF EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[FidelityPointsCampaigns]') AND type IN (N'U')
)
BEGIN
    DROP TABLE [dbo].[FidelityPointsCampaigns];
    PRINT 'FidelityPointsCampaigns table dropped successfully.';
END
ELSE
BEGIN
    PRINT 'FidelityPointsCampaigns table does not exist — nothing to roll back.';
END

IF EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[FidelityTierMultipliers]') AND type IN (N'U')
)
BEGIN
    DROP TABLE [dbo].[FidelityTierMultipliers];
    PRINT 'FidelityTierMultipliers table dropped successfully.';
END
ELSE
BEGIN
    PRINT 'FidelityTierMultipliers table does not exist — nothing to roll back.';
END

IF EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[FidelityPointsBaseRates]') AND type IN (N'U')
)
BEGIN
    DROP TABLE [dbo].[FidelityPointsBaseRates];
    PRINT 'FidelityPointsBaseRates table dropped successfully.';
END
ELSE
BEGIN
    PRINT 'FidelityPointsBaseRates table does not exist — nothing to roll back.';
END
