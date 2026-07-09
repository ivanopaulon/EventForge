-- Migration: Add PriceListId, PriceListName, AppliedPromotionsJSON to SaleItems
-- Date: 2026-07-09
-- Description: Traccia quale listino ha determinato il prezzo di una riga vendita e il dettaglio
-- di tutte le promozioni applicate (non solo la prima), per mostrarli nello scontrino POS2026.

IF COL_LENGTH('dbo.SaleItems', 'PriceListId') IS NULL
BEGIN
    ALTER TABLE [dbo].[SaleItems] ADD [PriceListId] uniqueidentifier NULL;
    PRINT 'PriceListId column added to SaleItems.';
END
ELSE
BEGIN
    PRINT 'PriceListId column already exists on SaleItems — skipping.';
END

IF COL_LENGTH('dbo.SaleItems', 'PriceListName') IS NULL
BEGIN
    ALTER TABLE [dbo].[SaleItems] ADD [PriceListName] nvarchar(100) NULL;
    PRINT 'PriceListName column added to SaleItems.';
END
ELSE
BEGIN
    PRINT 'PriceListName column already exists on SaleItems — skipping.';
END

IF COL_LENGTH('dbo.SaleItems', 'AppliedPromotionsJSON') IS NULL
BEGIN
    ALTER TABLE [dbo].[SaleItems] ADD [AppliedPromotionsJSON] nvarchar(max) NULL;
    PRINT 'AppliedPromotionsJSON column added to SaleItems.';
END
ELSE
BEGIN
    PRINT 'AppliedPromotionsJSON column already exists on SaleItems — skipping.';
END
