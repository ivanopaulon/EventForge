-- Rollback: Remove FidelityCardId from SaleSessions
-- Date: 2026-07-10
-- Rolls back migration 20260710_AddFidelityCardIdToSaleSessions.sql

IF EXISTS (
    SELECT * FROM sys.foreign_keys
    WHERE name = 'FK_SaleSessions_FidelityCards_FidelityCardId'
)
BEGIN
    ALTER TABLE [dbo].[SaleSessions] DROP CONSTRAINT [FK_SaleSessions_FidelityCards_FidelityCardId];
    PRINT 'FK_SaleSessions_FidelityCards_FidelityCardId constraint removed.';
END
ELSE
BEGIN
    PRINT 'FK_SaleSessions_FidelityCards_FidelityCardId constraint does not exist — nothing to roll back.';
END

IF COL_LENGTH('dbo.SaleSessions', 'FidelityCardId') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[SaleSessions] DROP COLUMN [FidelityCardId];
    PRINT 'FidelityCardId column removed from SaleSessions.';
END
ELSE
BEGIN
    PRINT 'FidelityCardId column does not exist on SaleSessions — nothing to roll back.';
END
