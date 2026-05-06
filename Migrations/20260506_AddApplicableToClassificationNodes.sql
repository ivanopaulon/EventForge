-- =============================================
-- Migration: Add ApplicableTo column to ClassificationNodes table
-- Date: 2026-05-06
-- Description:
--   Adds ApplicableTo (INT NOT NULL DEFAULT 0) to ClassificationNodes.
--   Values correspond to the ClassificationApplicableTo enum:
--     0 = Products (default)
--     1 = BusinessParties
--     2 = Both
--     3 = All
-- =============================================

USE [EventData];
GO

BEGIN TRY
    BEGIN TRANSACTION;

    -- Add ApplicableTo column if it does not already exist
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = 'ClassificationNodes'
          AND COLUMN_NAME = 'ApplicableTo'
    )
    BEGIN
        ALTER TABLE [dbo].[ClassificationNodes]
            ADD [ApplicableTo] INT NOT NULL DEFAULT 0;

        PRINT 'Column ApplicableTo added to ClassificationNodes.';
    END
    ELSE
    BEGIN
        PRINT 'Column ApplicableTo already exists in ClassificationNodes. Skipping.';
    END

    COMMIT TRANSACTION;
    PRINT 'Migration 20260506_AddApplicableToClassificationNodes completed successfully.';
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
