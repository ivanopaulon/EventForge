-- ============================================================
-- Migration: 20260624_AddBusinessPartyIdToStockMovements
-- Adds BusinessPartyId to StockMovements as the single source
-- of truth for supplier/customer purchase price history,
-- replacing the now-removed SupplierProductPriceHistories table.
-- ============================================================

BEGIN TRANSACTION;

-- 1. Add BusinessPartyId column (nullable)
ALTER TABLE [StockMovements]
    ADD [BusinessPartyId] uniqueidentifier NULL;

-- 2. Add foreign key constraint
ALTER TABLE [StockMovements]
    ADD CONSTRAINT [FK_StockMovements_BusinessParties_BusinessPartyId]
        FOREIGN KEY ([BusinessPartyId])
        REFERENCES [BusinessParties] ([Id])
        ON DELETE NO ACTION;

-- 3. Add indexes for price-history queries
CREATE INDEX [IX_StockMovements_BusinessPartyId]
    ON [StockMovements] ([BusinessPartyId])
    WHERE [BusinessPartyId] IS NOT NULL;

CREATE INDEX [IX_StockMovements_ProductId_BusinessPartyId_MovementDate]
    ON [StockMovements] ([ProductId], [BusinessPartyId], [MovementDate])
    WHERE [BusinessPartyId] IS NOT NULL;

-- 4. Backfill BusinessPartyId from the linked DocumentHeader
--    (only for Inbound/Purchase movements that have a document reference)
UPDATE sm
SET sm.[BusinessPartyId] = dh.[BusinessPartyId]
FROM [StockMovements] sm
INNER JOIN [DocumentHeaders] dh ON dh.[Id] = sm.[DocumentHeaderId]
WHERE sm.[BusinessPartyId] IS NULL
  AND sm.[DocumentHeaderId] IS NOT NULL
  AND dh.[BusinessPartyId] IS NOT NULL;

-- 5. Drop SupplierProductPriceHistories table (now superseded)
--    Safe to drop only after confirming no active FK references.
IF OBJECT_ID('dbo.SupplierProductPriceHistories', 'U') IS NOT NULL
BEGIN
    -- Remove FKs from the table first
    DECLARE @fk NVARCHAR(256);
    DECLARE fk_cur CURSOR FOR
        SELECT name FROM sys.foreign_keys
        WHERE parent_object_id = OBJECT_ID('dbo.SupplierProductPriceHistories');
    OPEN fk_cur;
    FETCH NEXT FROM fk_cur INTO @fk;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC('ALTER TABLE [SupplierProductPriceHistories] DROP CONSTRAINT [' + @fk + ']');
        FETCH NEXT FROM fk_cur INTO @fk;
    END;
    CLOSE fk_cur;
    DEALLOCATE fk_cur;

    DROP TABLE [SupplierProductPriceHistories];
END;

COMMIT TRANSACTION;
