-- Migration: 20260404_OptimizeEntityChangeLogs
-- Purpose: Repair the EntityChangeLogs table whose index space greatly exceeds data space
--          because the clustered PK was built on random Guids (Guid.NewGuid()) causing
--          constant B-tree page splits and ~50% page density.
--
-- Changes:
--   1. REBUILD the existing clustered PK index with FILLFACTOR=95 (reduces wasted space)
--      ONLINE=ON ensures no blocking reads/writes during the operation.
--   2. ALTER the Id column default to NEWSEQUENTIALID() so all future inserts use sequential
--      Guids — no more page splits going forward.
--   3. CREATE three non-clustered indexes for the most common query patterns.
--   4. ALTER OldValue / NewValue columns from NVARCHAR(MAX) to NVARCHAR(2000).
--      *** IMPORTANT: run a pre-check query first (see section 0) to verify no row has
--          values longer than 2000 chars. If any do, review before running section 4. ***
--
-- Safe for production: all index operations use ONLINE = ON.
-- Estimated downtime: ZERO (online operations only).

-- ─── 0. PRE-CHECK: verify no value exceeds 2000 characters ───────────────────────────────
-- Run this SELECT before section 4. If it returns rows, investigate and optionally truncate
-- those values manually before altering the columns.
--
-- SELECT TOP 100 Id, EntityName, PropertyName, LEN(OldValue) AS OldLen, LEN(NewValue) AS NewLen
-- FROM EntityChangeLogs
-- WHERE LEN(OldValue) > 2000 OR LEN(NewValue) > 2000
-- ORDER BY LEN(OldValue) + LEN(NewValue) DESC;

-- ─── 1. REBUILD clustered PK index (reduces fragmentation on existing data) ──────────────
ALTER INDEX PK_EntityChangeLogs ON EntityChangeLogs
    REBUILD WITH (ONLINE = ON, FILLFACTOR = 95, SORT_IN_TEMPDB = ON);
GO

-- ─── 2. Apply NEWSEQUENTIALID() default to the Id column ────────────────────────────────
-- Drop the existing default constraint if any (SQL Server may have created one implicitly).
DECLARE @constraintName NVARCHAR(256);
SELECT @constraintName = dc.name
FROM   sys.default_constraints dc
JOIN   sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE  c.object_id = OBJECT_ID('EntityChangeLogs') AND c.name = 'Id';

IF @constraintName IS NOT NULL
    EXEC('ALTER TABLE EntityChangeLogs DROP CONSTRAINT ' + @constraintName);
GO

ALTER TABLE EntityChangeLogs
    ADD DEFAULT NEWSEQUENTIALID() FOR Id;
GO

-- ─── 3. CREATE non-clustered indexes (ONLINE = ON, no blocking) ──────────────────────────

-- Most common pattern: audit history for a specific entity in a tenant
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_EntityChangeLogs_TenantId_EntityId_ChangedAt'
      AND object_id = OBJECT_ID('EntityChangeLogs'))
    CREATE NONCLUSTERED INDEX IX_EntityChangeLogs_TenantId_EntityId_ChangedAt
        ON EntityChangeLogs (TenantId, EntityId, ChangedAt DESC)
        INCLUDE (EntityName, PropertyName, OperationType, ChangedBy)
        WITH (ONLINE = ON, FILLFACTOR = 90);
GO

-- Tenant-wide audit timeline
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_EntityChangeLogs_TenantId_ChangedAt'
      AND object_id = OBJECT_ID('EntityChangeLogs'))
    CREATE NONCLUSTERED INDEX IX_EntityChangeLogs_TenantId_ChangedAt
        ON EntityChangeLogs (TenantId, ChangedAt DESC)
        INCLUDE (EntityName, EntityId, OperationType, ChangedBy)
        WITH (ONLINE = ON, FILLFACTOR = 90);
GO

-- Per-entity-type queries (e.g. all changes to Products today)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_EntityChangeLogs_EntityName_TenantId_ChangedAt'
      AND object_id = OBJECT_ID('EntityChangeLogs'))
    CREATE NONCLUSTERED INDEX IX_EntityChangeLogs_EntityName_TenantId_ChangedAt
        ON EntityChangeLogs (EntityName, TenantId, ChangedAt DESC)
        INCLUDE (EntityId, OperationType, ChangedBy)
        WITH (ONLINE = ON, FILLFACTOR = 90);
GO

-- ─── 4. ALTER OldValue / NewValue to NVARCHAR(2000) ──────────────────────────────────────
-- PREREQUISITE: run the pre-check query in section 0 first.
-- NOTE: ALTER COLUMN from NVARCHAR(MAX) to bounded requires an OFFLINE table scan.
--       Schedule this step during a maintenance window or low-traffic period.
--       It cannot be done ONLINE. Estimated time: proportional to table size.

-- UNCOMMENT ONLY after verifying the pre-check returns 0 rows:
-- ALTER TABLE EntityChangeLogs ALTER COLUMN OldValue NVARCHAR(2000) NULL;
-- GO
-- ALTER TABLE EntityChangeLogs ALTER COLUMN NewValue NVARCHAR(2000) NULL;
-- GO

PRINT 'Migration 20260404_OptimizeEntityChangeLogs completed. Check output for any errors.';
