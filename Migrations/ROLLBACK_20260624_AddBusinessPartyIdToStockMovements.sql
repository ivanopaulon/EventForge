-- ============================================================
-- ROLLBACK: 20260624_AddBusinessPartyIdToStockMovements
-- ============================================================

BEGIN TRANSACTION;

-- Remove indexes
DROP INDEX IF EXISTS [IX_StockMovements_ProductId_BusinessPartyId_MovementDate] ON [StockMovements];
DROP INDEX IF EXISTS [IX_StockMovements_BusinessPartyId] ON [StockMovements];

-- Remove FK
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_StockMovements_BusinessParties_BusinessPartyId'
      AND parent_object_id = OBJECT_ID('dbo.StockMovements'))
BEGIN
    ALTER TABLE [StockMovements] DROP CONSTRAINT [FK_StockMovements_BusinessParties_BusinessPartyId];
END;

-- Remove column
ALTER TABLE [StockMovements] DROP COLUMN [BusinessPartyId];

-- NOTE: SupplierProductPriceHistories table is NOT recreated here.
-- Restore from backup if a full rollback of data history is required.

COMMIT TRANSACTION;
