-- ============================================================
-- Migration: 20260407_AddStorePosAssignments
-- Adds CashierGroupId (FK → StoreUserGroups) to StorePoses.
-- DefaultFiscalPrinterId FK was already present in the schema;
-- this migration adds the FK constraint if missing and creates
-- the covering index.
-- ============================================================

-- 1. Add CashierGroupId column (nullable)
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'StorePoses' AND COLUMN_NAME = 'CashierGroupId'
)
BEGIN
    ALTER TABLE [StorePoses]
        ADD [CashierGroupId] uniqueidentifier NULL;
END
GO

-- 2. Add FK constraint for CashierGroupId → StoreUserGroups
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_StorePoses_StoreUserGroups_CashierGroupId'
)
BEGIN
    ALTER TABLE [StorePoses]
        ADD CONSTRAINT [FK_StorePoses_StoreUserGroups_CashierGroupId]
        FOREIGN KEY ([CashierGroupId]) REFERENCES [StoreUserGroups] ([Id])
        ON DELETE SET NULL;
END
GO

-- 3. Index on CashierGroupId
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_StorePos_CashierGroupId' AND object_id = OBJECT_ID('StorePoses')
)
BEGIN
    CREATE INDEX [IX_StorePos_CashierGroupId] ON [StorePoses] ([CashierGroupId]);
END
GO

-- 4. Ensure FK constraint exists for DefaultFiscalPrinterId → Printers
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_StorePoses_Printers_DefaultFiscalPrinterId'
)
BEGIN
    ALTER TABLE [StorePoses]
        ADD CONSTRAINT [FK_StorePoses_Printers_DefaultFiscalPrinterId]
        FOREIGN KEY ([DefaultFiscalPrinterId]) REFERENCES [Printers] ([Id])
        ON DELETE SET NULL;
END
GO

-- 5. Index on DefaultFiscalPrinterId
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_StorePos_DefaultFiscalPrinterId' AND object_id = OBJECT_ID('StorePoses')
)
BEGIN
    CREATE INDEX [IX_StorePos_DefaultFiscalPrinterId] ON [StorePoses] ([DefaultFiscalPrinterId]);
END
GO
