-- Rollback: Remove CashierShifts table
-- Date: 2026-04-16
-- Rolls back migration 20260416_AddCashierShifts.sql

IF EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[CashierShifts]') AND type IN (N'U')
)
BEGIN
    -- Drop FK constraints before dropping the table
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CashierShifts_StoreUsers_StoreUserId')
        ALTER TABLE [dbo].[CashierShifts] DROP CONSTRAINT [FK_CashierShifts_StoreUsers_StoreUserId];

    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_CashierShifts_StorePoses_PosId')
        ALTER TABLE [dbo].[CashierShifts] DROP CONSTRAINT [FK_CashierShifts_StorePoses_PosId];

    DROP TABLE [dbo].[CashierShifts];

    PRINT 'CashierShifts table dropped successfully.';
END
ELSE
BEGIN
    PRINT 'CashierShifts table does not exist — nothing to roll back.';
END
