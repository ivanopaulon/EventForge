-- =============================================
-- Rollback: Remove ApplicableTo column from ClassificationNodes table
-- Date: 2026-05-06
-- =============================================

USE [EventData];
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = 'ClassificationNodes'
          AND COLUMN_NAME = 'ApplicableTo'
    )
    BEGIN
        ALTER TABLE [dbo].[ClassificationNodes]
            DROP COLUMN [ApplicableTo];

        PRINT 'Column ApplicableTo removed from ClassificationNodes.';
    END

    COMMIT TRANSACTION;
    PRINT 'Rollback 20260506_AddApplicableToClassificationNodes completed.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT           = ERROR_SEVERITY();
    DECLARE @ErrorState INT              = ERROR_STATE();
    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
GO
