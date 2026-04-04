-- ============================================================
-- Migration: 20260404_AddTenantIdToEntityChangeLog
-- Description: Adds TenantId column to EntityChangeLogs table for
--              multi-tenant isolation of audit log entries.
--              Nullable for backward-compatibility with existing rows.
-- Date: 2026-04-04
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[EntityChangeLogs]')
    AND name = 'TenantId'
)
BEGIN
    ALTER TABLE [dbo].[EntityChangeLogs]
    ADD [TenantId] UNIQUEIDENTIFIER NULL;
    PRINT 'Column TenantId added to EntityChangeLogs.';
END
GO

-- Index to support per-tenant audit log queries
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[EntityChangeLogs]')
    AND name = 'IX_EntityChangeLogs_TenantId'
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EntityChangeLogs_TenantId]
    ON [dbo].[EntityChangeLogs] ([TenantId])
    WHERE [TenantId] IS NOT NULL;
    PRINT 'Index IX_EntityChangeLogs_TenantId created.';
END
GO
