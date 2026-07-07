-- Rollback: Remove QuickPinHash from StoreUsers
-- Date: 2026-07-07
-- Rolls back migration 20260707_AddQuickPinHashToStoreUsers.sql

IF COL_LENGTH('dbo.StoreUsers', 'QuickPinHash') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[StoreUsers]
        DROP COLUMN [QuickPinHash];

    PRINT 'QuickPinHash column removed from StoreUsers.';
END
ELSE
BEGIN
    PRINT 'QuickPinHash column does not exist on StoreUsers — nothing to roll back.';
END
