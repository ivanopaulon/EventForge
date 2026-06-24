-- Rollback: Remove FidelityCards and FidelityPointsTransactions tables
-- Date: 2026-06-23

IF EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[FidelityPointsTransactions]') AND type IN (N'U')
)
BEGIN
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_FidelityPointsTransactions_FidelityCards_FidelityCardId')
        ALTER TABLE [dbo].[FidelityPointsTransactions] DROP CONSTRAINT [FK_FidelityPointsTransactions_FidelityCards_FidelityCardId];

    DROP TABLE [dbo].[FidelityPointsTransactions];
    PRINT 'FidelityPointsTransactions table dropped successfully.';
END
ELSE
BEGIN
    PRINT 'FidelityPointsTransactions table does not exist — nothing to roll back.';
END

IF EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[FidelityCards]') AND type IN (N'U')
)
BEGIN
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_FidelityCards_BusinessParties_BusinessPartyId')
        ALTER TABLE [dbo].[FidelityCards] DROP CONSTRAINT [FK_FidelityCards_BusinessParties_BusinessPartyId];

    DROP TABLE [dbo].[FidelityCards];
    PRINT 'FidelityCards table dropped successfully.';
END
ELSE
BEGIN
    PRINT 'FidelityCards table does not exist — nothing to roll back.';
END
