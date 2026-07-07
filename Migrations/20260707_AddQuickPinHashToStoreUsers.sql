-- Migration: Add QuickPinHash to StoreUsers
-- Date: 2026-07-07
-- Description: Adds nullable QuickPinHash column for fast operator PIN validation in POS.

IF COL_LENGTH('dbo.StoreUsers', 'QuickPinHash') IS NULL
BEGIN
    ALTER TABLE [dbo].[StoreUsers]
        ADD [QuickPinHash] nvarchar(200) NULL;

    PRINT 'QuickPinHash column added to StoreUsers.';
END
ELSE
BEGIN
    PRINT 'QuickPinHash column already exists on StoreUsers — skipping.';
END
