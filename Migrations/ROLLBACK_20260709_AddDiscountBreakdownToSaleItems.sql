-- Rollback: Remove ManualDiscountPercent, PromotionDiscountPercent from SaleItems
-- Date: 2026-07-09
-- Rolls back migration 20260709_AddDiscountBreakdownToSaleItems.sql
-- Nessun bisogno di invertire il backfill: la rimozione delle colonne lo rende irrilevante.

IF COL_LENGTH('dbo.SaleItems', 'ManualDiscountPercent') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[SaleItems] DROP COLUMN [ManualDiscountPercent];
    PRINT 'ManualDiscountPercent column removed from SaleItems.';
END
ELSE
BEGIN
    PRINT 'ManualDiscountPercent column does not exist on SaleItems — nothing to roll back.';
END

IF COL_LENGTH('dbo.SaleItems', 'PromotionDiscountPercent') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[SaleItems] DROP COLUMN [PromotionDiscountPercent];
    PRINT 'PromotionDiscountPercent column removed from SaleItems.';
END
ELSE
BEGIN
    PRINT 'PromotionDiscountPercent column does not exist on SaleItems — nothing to roll back.';
END
