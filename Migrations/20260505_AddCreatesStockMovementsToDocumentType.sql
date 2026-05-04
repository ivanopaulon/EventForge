-- =============================================
-- Migration: Add CreatesStockMovements column to DocumentTypes table
-- Date: 2026-05-05
-- Description:
--   Adds CreatesStockMovements (BIT NOT NULL DEFAULT 1) to DocumentTypes.
--   Inventory document types (IsInventoryDocument = 1) are set to 0 so that
--   approving or closing an inventory document does NOT generate warehouse
--   stock movements automatically.
--
--   Also cleans up any erroneous StockMovements that were already generated
--   for inventory documents (i.e., documents whose type has IsInventoryDocument = 1).
--   Those movements are soft-deleted and the affected Stock.Quantity values are
--   corrected in the same atomic transaction.
-- =============================================

USE [EventData];
GO

BEGIN TRY
    BEGIN TRANSACTION;

    -- ─────────────────────────────────────────────────────────────────────────
    -- Step 1: Add the CreatesStockMovements column (idempotent)
    -- ─────────────────────────────────────────────────────────────────────────
    IF NOT EXISTS (
        SELECT 1
        FROM   sys.columns
        WHERE  object_id = OBJECT_ID(N'[dbo].[DocumentTypes]')
          AND  name = 'CreatesStockMovements'
    )
    BEGIN
        ALTER TABLE [dbo].[DocumentTypes]
        ADD [CreatesStockMovements] BIT NOT NULL DEFAULT 1;

        PRINT 'Column CreatesStockMovements added to DocumentTypes.';
    END
    ELSE
    BEGIN
        PRINT 'Column CreatesStockMovements already exists in DocumentTypes — skipping ALTER.';
    END

    -- ─────────────────────────────────────────────────────────────────────────
    -- Step 2: Set CreatesStockMovements = 0 for all inventory document types
    -- ─────────────────────────────────────────────────────────────────────────
    UPDATE [dbo].[DocumentTypes]
    SET    [CreatesStockMovements] = 0
    WHERE  [IsInventoryDocument] = 1
      AND  [IsDeleted] = 0;

    PRINT CONCAT('Set CreatesStockMovements = 0 for ', @@ROWCOUNT, ' inventory document type(s).');

    -- ─────────────────────────────────────────────────────────────────────────
    -- Step 3: Ensure well-known INVENTORY code is flagged correctly
    -- ─────────────────────────────────────────────────────────────────────────
    UPDATE [dbo].[DocumentTypes]
    SET    [IsInventoryDocument]   = 1,
           [CreatesStockMovements] = 0
    WHERE  [Code] IN ('INVENTORY', 'INV-COUNT', 'STOCK-COUNT', 'INVENT', 'INV', 'INVFIS', 'PHY-INV')
      AND  [IsDeleted] = 0
      AND  ([IsInventoryDocument] = 0 OR [CreatesStockMovements] = 1);

    PRINT CONCAT('Updated ', @@ROWCOUNT, ' well-known inventory document type(s) (Code-based guard).');

    -- ─────────────────────────────────────────────────────────────────────────
    -- Step 4: Soft-delete erroneous StockMovements generated from inventory docs
    --
    -- Erroneous = movements whose DocumentHeaderId links to a DocumentHeader
    -- whose DocumentType has IsInventoryDocument = 1.
    -- ─────────────────────────────────────────────────────────────────────────

    -- 4a. Collect the IDs of erroneous movements into a temp table
    SELECT sm.[Id],
           sm.[ProductId],
           sm.[FromLocationId],
           sm.[ToLocationId],
           sm.[Quantity],
           sm.[TenantId]
    INTO   #ErroneousMovements
    FROM   [dbo].[StockMovements]   sm
    JOIN   [dbo].[DocumentHeaders]  dh  ON dh.[Id]   = sm.[DocumentHeaderId]
    JOIN   [dbo].[DocumentTypes]    dt  ON dt.[Id]   = dh.[DocumentTypeId]
    WHERE  sm.[IsDeleted] = 0
      AND  dt.[IsInventoryDocument] = 1;

    PRINT CONCAT('Identified ', (SELECT COUNT(1) FROM #ErroneousMovements), ' erroneous movement(s) to clean up.');

    -- 4b. Restore Stock.Quantity for each erroneous movement
    --     An Inbound/positive-adjustment (ToLocationId set) incorrectly increased stock → subtract it back.
    --     An Outbound/negative-adjustment (FromLocationId set) incorrectly decreased stock → add it back.

    -- Reverse inbound/positive-adjustment contributions (they added to stock)
    UPDATE s
    SET    s.[Quantity]     = s.[Quantity] - em.[Quantity],
           s.[ModifiedAt]   = GETUTCDATE(),
           s.[ModifiedBy]   = 'Migration_20260505'
    FROM   [dbo].[Stocks] s
    JOIN   #ErroneousMovements em
           ON  em.[ToLocationId] IS NOT NULL
           AND em.[TenantId]     = s.[TenantId]
           AND em.[ProductId]    = s.[ProductId]
           AND em.[ToLocationId] = s.[StorageLocationId]
    WHERE  s.[IsDeleted] = 0;

    PRINT CONCAT('Reversed inbound contributions for ', @@ROWCOUNT, ' stock record(s).');

    -- Reverse outbound/negative-adjustment contributions (they reduced stock)
    UPDATE s
    SET    s.[Quantity]     = s.[Quantity] + em.[Quantity],
           s.[ModifiedAt]   = GETUTCDATE(),
           s.[ModifiedBy]   = 'Migration_20260505'
    FROM   [dbo].[Stocks] s
    JOIN   #ErroneousMovements em
           ON  em.[FromLocationId] IS NOT NULL
           AND em.[TenantId]       = s.[TenantId]
           AND em.[ProductId]      = s.[ProductId]
           AND em.[FromLocationId] = s.[StorageLocationId]
    WHERE  s.[IsDeleted] = 0;

    PRINT CONCAT('Reversed outbound contributions for ', @@ROWCOUNT, ' stock record(s).');

    -- 4c. Soft-delete the erroneous movements
    UPDATE sm
    SET    sm.[IsDeleted]  = 1,
           sm.[DeletedAt]  = GETUTCDATE(),
           sm.[DeletedBy]  = 'Migration_20260505',
           sm.[ModifiedAt] = GETUTCDATE(),
           sm.[ModifiedBy] = 'Migration_20260505'
    FROM   [dbo].[StockMovements] sm
    JOIN   #ErroneousMovements em ON em.[Id] = sm.[Id];

    PRINT CONCAT('Soft-deleted ', @@ROWCOUNT, ' erroneous movement(s).');

    DROP TABLE #ErroneousMovements;

    COMMIT TRANSACTION;
    PRINT 'Migration 20260505_AddCreatesStockMovementsToDocumentType completed successfully.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg   NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSev   INT            = ERROR_SEVERITY();
    DECLARE @ErrState INT            = ERROR_STATE();

    IF OBJECT_ID('tempdb..#ErroneousMovements') IS NOT NULL
        DROP TABLE #ErroneousMovements;

    PRINT CONCAT('Migration FAILED: ', @ErrMsg);
    RAISERROR(@ErrMsg, @ErrSev, @ErrState);
END CATCH
GO
