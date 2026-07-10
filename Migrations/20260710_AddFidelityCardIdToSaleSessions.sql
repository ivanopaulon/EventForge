-- Migration: Add FidelityCardId to SaleSessions
-- Date: 2026-07-10
-- Description: Propagates the fidelity card resolved for the customer to the sale session as soon
-- as it is known client-side (before payment), so fidelity-based discounts can be applied while the
-- cart is being built (see FidelityPointsRateService / calculate-preview endpoint for points).

IF COL_LENGTH('dbo.SaleSessions', 'FidelityCardId') IS NULL
BEGIN
    ALTER TABLE [dbo].[SaleSessions] ADD [FidelityCardId] uniqueidentifier NULL;
    PRINT 'FidelityCardId column added to SaleSessions.';
END
ELSE
BEGIN
    PRINT 'FidelityCardId column already exists on SaleSessions — skipping.';
END

IF NOT EXISTS (
    SELECT * FROM sys.foreign_keys
    WHERE name = 'FK_SaleSessions_FidelityCards_FidelityCardId'
)
BEGIN
    ALTER TABLE [dbo].[SaleSessions]
    ADD CONSTRAINT [FK_SaleSessions_FidelityCards_FidelityCardId]
        FOREIGN KEY ([FidelityCardId])
        REFERENCES [dbo].[FidelityCards] ([Id])
        ON DELETE SET NULL;
    PRINT 'FK_SaleSessions_FidelityCards_FidelityCardId constraint added.';
END
ELSE
BEGIN
    PRINT 'FK_SaleSessions_FidelityCards_FidelityCardId constraint already exists — skipping.';
END
