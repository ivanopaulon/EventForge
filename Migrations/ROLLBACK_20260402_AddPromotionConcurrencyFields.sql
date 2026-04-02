-- ============================================================
-- Rollback: ROLLBACK_20260402_AddPromotionConcurrencyFields
-- Description: Reverts the AddPromotionConcurrencyFields migration
--              by removing the CurrentUses and RowVersion columns
--              from the Promotions table.
-- Date: 2026-04-02
-- ============================================================

-- Remove RowVersion column from Promotions table
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Promotions]')
    AND name = 'RowVersion'
)
BEGIN
    ALTER TABLE [dbo].[Promotions]
    DROP COLUMN [RowVersion];

    PRINT 'Column RowVersion removed from Promotions.';
END
ELSE
BEGIN
    PRINT 'Column RowVersion does not exist in Promotions. Skipping.';
END
GO

-- Remove CurrentUses column from Promotions table
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Promotions]')
    AND name = 'CurrentUses'
)
BEGIN
    ALTER TABLE [dbo].[Promotions]
    DROP COLUMN [CurrentUses];

    PRINT 'Column CurrentUses removed from Promotions.';
END
ELSE
BEGIN
    PRINT 'Column CurrentUses does not exist in Promotions. Skipping.';
END
GO

PRINT 'Rollback ROLLBACK_20260402_AddPromotionConcurrencyFields completed successfully.';
GO
