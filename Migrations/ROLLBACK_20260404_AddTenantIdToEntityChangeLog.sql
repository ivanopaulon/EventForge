-- ============================================================
-- Rollback: 20260404_AddTenantIdToEntityChangeLog
-- Description: Removes TenantId column and index from EntityChangeLogs.
-- Date: 2026-04-04
-- ============================================================

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[EntityChangeLogs]')
    AND name = 'IX_EntityChangeLogs_TenantId'
)
BEGIN
    DROP INDEX [IX_EntityChangeLogs_TenantId] ON [dbo].[EntityChangeLogs];
    PRINT 'Index IX_EntityChangeLogs_TenantId dropped.';
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[EntityChangeLogs]')
    AND name = 'TenantId'
)
BEGIN
    ALTER TABLE [dbo].[EntityChangeLogs]
    DROP COLUMN [TenantId];
    PRINT 'Column TenantId dropped from EntityChangeLogs.';
END
GO
