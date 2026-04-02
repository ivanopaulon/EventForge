-- ============================================================
-- Migration: 20260402_AddPromotionConcurrencyFields
-- Description: Adds CurrentUses and RowVersion columns to the
--              Promotions table to support usage tracking and
--              optimistic concurrency control (MaxUses enforcement).
-- Date: 2026-04-02
-- ============================================================

-- Add CurrentUses column to Promotions table
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Promotions]')
    AND name = 'CurrentUses'
)
BEGIN
    ALTER TABLE [dbo].[Promotions]
    ADD [CurrentUses] INT NOT NULL DEFAULT 0;

    PRINT 'Column CurrentUses added to Promotions.';
END
ELSE
BEGIN
    PRINT 'Column CurrentUses already exists in Promotions. Skipping.';
END
GO

-- Add RowVersion column to Promotions table (optimistic concurrency)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Promotions]')
    AND name = 'RowVersion'
)
BEGIN
    ALTER TABLE [dbo].[Promotions]
    ADD [RowVersion] ROWVERSION NOT NULL;

    PRINT 'Column RowVersion added to Promotions.';
END
ELSE
BEGIN
    PRINT 'Column RowVersion already exists in Promotions. Skipping.';
END
GO

PRINT 'Migration 20260402_AddPromotionConcurrencyFields completed successfully.';
GO
