-- ============================================================
-- Migration: 20260404_AddPromotionTargetingFields
-- Description: Adds MaxTotalDiscountPercentage and MaxUsesPerCustomer
--              columns to the Promotions table.
-- Date: 2026-04-04
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Promotions]')
    AND name = 'MaxTotalDiscountPercentage'
)
BEGIN
    ALTER TABLE [dbo].[Promotions]
    ADD [MaxTotalDiscountPercentage] DECIMAL(5, 2) NULL;
    PRINT 'Column MaxTotalDiscountPercentage added to Promotions.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[Promotions]')
    AND name = 'MaxUsesPerCustomer'
)
BEGIN
    ALTER TABLE [dbo].[Promotions]
    ADD [MaxUsesPerCustomer] INT NULL;
    PRINT 'Column MaxUsesPerCustomer added to Promotions.';
END
GO

PRINT 'Migration 20260404_AddPromotionTargetingFields completed successfully.';
GO
