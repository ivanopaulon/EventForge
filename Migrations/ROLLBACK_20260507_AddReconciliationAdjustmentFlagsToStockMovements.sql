-- Rollback: 20260507_AddReconciliationAdjustmentFlagsToStockMovements
-- Purpose: remove IsReconciliationAdjustment and ReconciliationRunId columns.

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'[dbo].[StockMovements]')
          AND name = N'IsReconciliationAdjustment'
    )
    BEGIN
        DECLARE @ConstraintName NVARCHAR(256);
        SELECT @ConstraintName = dc.[name]
        FROM sys.default_constraints dc
        JOIN sys.columns c
            ON c.object_id = dc.parent_object_id
           AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[StockMovements]')
          AND c.[name] = N'IsReconciliationAdjustment';

        IF @ConstraintName IS NOT NULL
            EXEC('ALTER TABLE [dbo].[StockMovements] DROP CONSTRAINT [' + @ConstraintName + ']');

        ALTER TABLE [dbo].[StockMovements] DROP COLUMN [IsReconciliationAdjustment];
    END

    IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'[dbo].[StockMovements]')
          AND name = N'ReconciliationRunId'
    )
    BEGIN
        ALTER TABLE [dbo].[StockMovements] DROP COLUMN [ReconciliationRunId];
    END

    COMMIT TRANSACTION;
    PRINT 'Rollback 20260507_AddReconciliationAdjustmentFlagsToStockMovements completed.';
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
