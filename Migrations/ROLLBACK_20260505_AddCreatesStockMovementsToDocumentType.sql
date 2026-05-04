-- =============================================
-- ROLLBACK: 20260505_AddCreatesStockMovementsToDocumentType
-- Date: 2026-05-05
-- Description:
--   Removes the CreatesStockMovements column from DocumentTypes.
--
--   IMPORTANT: This rollback does NOT restore erroneous StockMovements that
--   were soft-deleted by the forward migration, because those movements were
--   incorrect by definition. Restoring them would re-introduce corrupted stock
--   data.  If you need to restore them for any reason, do so manually using the
--   DeletedBy = 'Migration_20260505' marker.
-- =============================================

USE [EventData];
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (
        SELECT 1
        FROM   sys.columns
        WHERE  object_id = OBJECT_ID(N'[dbo].[DocumentTypes]')
          AND  name = 'CreatesStockMovements'
    )
    BEGIN
        ALTER TABLE [dbo].[DocumentTypes]
        DROP COLUMN [CreatesStockMovements];

        PRINT 'Column CreatesStockMovements removed from DocumentTypes.';
    END
    ELSE
    BEGIN
        PRINT 'Column CreatesStockMovements does not exist — nothing to roll back.';
    END

    COMMIT TRANSACTION;
    PRINT 'Rollback of 20260505_AddCreatesStockMovementsToDocumentType completed.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg   NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSev   INT            = ERROR_SEVERITY();
    DECLARE @ErrState INT            = ERROR_STATE();

    PRINT CONCAT('Rollback FAILED: ', @ErrMsg);
    RAISERROR(@ErrMsg, @ErrSev, @ErrState);
END CATCH
GO
