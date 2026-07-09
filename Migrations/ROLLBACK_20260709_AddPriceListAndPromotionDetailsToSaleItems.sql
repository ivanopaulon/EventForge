-- Rollback: Remove PriceListId, PriceListName, AppliedPromotionsJSON from SaleItems
-- Date: 2026-07-09
-- Rolls back migration 20260709_AddPriceListAndPromotionDetailsToSaleItems.sql

IF COL_LENGTH('dbo.SaleItems', 'PriceListId') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[SaleItems] DROP COLUMN [PriceListId];
    PRINT 'PriceListId column removed from SaleItems.';
END
ELSE
BEGIN
    PRINT 'PriceListId column does not exist on SaleItems — nothing to roll back.';
END

IF COL_LENGTH('dbo.SaleItems', 'PriceListName') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[SaleItems] DROP COLUMN [PriceListName];
    PRINT 'PriceListName column removed from SaleItems.';
END
ELSE
BEGIN
    PRINT 'PriceListName column does not exist on SaleItems — nothing to roll back.';
END

IF COL_LENGTH('dbo.SaleItems', 'AppliedPromotionsJSON') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[SaleItems] DROP COLUMN [AppliedPromotionsJSON];
    PRINT 'AppliedPromotionsJSON column removed from SaleItems.';
END
ELSE
BEGIN
    PRINT 'AppliedPromotionsJSON column does not exist on SaleItems — nothing to roll back.';
END
