-- Migration: 20260507_AddReconciliationAdjustmentFlagsToStockMovements
-- Purpose:
--   1) Add explicit structured metadata for technical reconciliation adjustments.
--   2) Backfill existing reconciliation-created movements using a conservative heuristic.

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'[dbo].[StockMovements]')
          AND name = N'IsReconciliationAdjustment'
    )
    BEGIN
        ALTER TABLE [dbo].[StockMovements]
            ADD [IsReconciliationAdjustment] BIT NOT NULL
                CONSTRAINT [DF_StockMovements_IsReconciliationAdjustment] DEFAULT (0);
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'[dbo].[StockMovements]')
          AND name = N'ReconciliationRunId'
    )
    BEGIN
        ALTER TABLE [dbo].[StockMovements]
            ADD [ReconciliationRunId] UNIQUEIDENTIFIER NULL;
    END

    -- Backfill legacy reconciliation adjustments (heuristic):
    -- Adjustment + reason=Adjustment + notes prefixed with "Stock Reconciliation -"
    UPDATE sm
    SET sm.[IsReconciliationAdjustment] = 1
    FROM [dbo].[StockMovements] sm
    WHERE sm.[IsDeleted] = 0
      AND sm.[MovementType] = 3
      AND sm.[Reason] = 3
      AND sm.[Notes] LIKE 'Stock Reconciliation -%';

    COMMIT TRANSACTION;
    PRINT 'Migration 20260507_AddReconciliationAdjustmentFlagsToStockMovements completed.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSev INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSev, @ErrState);
END CATCH
GO
