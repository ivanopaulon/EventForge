-- Migration: Add ManualDiscountPercent, PromotionDiscountPercent to SaleItems
-- Date: 2026-07-09
-- Description: Separa lo sconto manuale (cassiere) da quello promozionale, oggi condivisi nello
-- stesso campo DiscountPercent — causa del bug per cui una promozione non si disattiva mai quando
-- la riga smette di soddisfarne i requisiti (il ricalcolo aggiornava DiscountPercent solo verso l'alto).
-- DiscountPercent resta il campo totale usato per i calcoli esistenti; i due nuovi campi ne tracciano
-- la composizione.

IF COL_LENGTH('dbo.SaleItems', 'ManualDiscountPercent') IS NULL
BEGIN
    ALTER TABLE [dbo].[SaleItems] ADD [ManualDiscountPercent] decimal(5,2) NOT NULL DEFAULT 0;
    PRINT 'ManualDiscountPercent column added to SaleItems.';
END
ELSE
BEGIN
    PRINT 'ManualDiscountPercent column already exists on SaleItems — skipping.';
END

IF COL_LENGTH('dbo.SaleItems', 'PromotionDiscountPercent') IS NULL
BEGIN
    ALTER TABLE [dbo].[SaleItems] ADD [PromotionDiscountPercent] decimal(5,2) NOT NULL DEFAULT 0;
    PRINT 'PromotionDiscountPercent column added to SaleItems.';
END
ELSE
BEGIN
    PRINT 'PromotionDiscountPercent column already exists on SaleItems — skipping.';
END

-- Backfill: per le righe esistenti con uno sconto già applicato e un PromotionId valorizzato,
-- assumiamo (ragionevolmente, ma è un'euristica) che tutto lo sconto attuale sia di origine
-- promozionale, dato che prima di questa migrazione non esisteva altro modo di distinguerlo.
UPDATE [dbo].[SaleItems]
SET [PromotionDiscountPercent] = [DiscountPercent]
WHERE [PromotionId] IS NOT NULL AND [DiscountPercent] > 0;
