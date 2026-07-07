-- Migration: 20260618_RemoveDocumentApprovalAndTwoStateStatus
-- Description:
--   1) Remove document approval model (ApprovalStatus, ApprovedBy, ApprovedAt)
--   2) Normalize document lifecycle to Active(1) / Archived(4)
--      - Draft(0)     -> Active(1)
--      - Open(1)      -> Active(1) [already aligned, unchanged]
--      - Closed(2)    -> Archived(4)
--      - Cancelled(3) -> Archived(4)
-- Date: 2026-06-18
-- Syntax: SQL Server (T-SQL)

BEGIN TRANSACTION;

-- 1) Normalize DocumentHeaders status values to 2-state model
UPDATE [DocumentHeaders]
SET    [Status] = 1,
       [ModifiedAt] = GETUTCDATE(),
       [ModifiedBy] = 'migration_20260618'
WHERE  [Status] = 0                       -- Draft -> Active
  AND  [IsDeleted] = 0;

UPDATE [DocumentHeaders]
SET    [Status] = 4,
       [ModifiedAt] = GETUTCDATE(),
       [ModifiedBy] = 'migration_20260618'
WHERE  [Status] = 2                       -- Closed -> Archived
  AND  [IsDeleted] = 0;

UPDATE [DocumentHeaders]
SET    [Status] = 4,
       [ModifiedAt] = GETUTCDATE(),
       [ModifiedBy] = 'migration_20260618'
WHERE  [Status] = 3                       -- Cancelled -> Archived
  AND  [IsDeleted] = 0;

-- 2) Normalize status history values
UPDATE [DocumentStatusHistories]
SET    [FromStatus] = 1
WHERE  [FromStatus] = 0;                  -- Draft -> Active

UPDATE [DocumentStatusHistories]
SET    [FromStatus] = 4
WHERE  [FromStatus] = 2;                  -- Closed -> Archived

UPDATE [DocumentStatusHistories]
SET    [FromStatus] = 4
WHERE  [FromStatus] = 3;                  -- Cancelled -> Archived

UPDATE [DocumentStatusHistories]
SET    [ToStatus] = 1
WHERE  [ToStatus] = 0;                    -- Draft -> Active

UPDATE [DocumentStatusHistories]
SET    [ToStatus] = 4
WHERE  [ToStatus] = 2;                    -- Closed -> Archived

UPDATE [DocumentStatusHistories]
SET    [ToStatus] = 4
WHERE  [ToStatus] = 3;                    -- Cancelled -> Archived

-- 3) Drop approval columns from DocumentHeaders
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[DocumentHeaders]') AND name = 'ApprovalStatus')
    ALTER TABLE [DocumentHeaders] DROP COLUMN [ApprovalStatus];

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[DocumentHeaders]') AND name = 'ApprovedBy')
    ALTER TABLE [DocumentHeaders] DROP COLUMN [ApprovedBy];

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[DocumentHeaders]') AND name = 'ApprovedAt')
    ALTER TABLE [DocumentHeaders] DROP COLUMN [ApprovedAt];

-- 4) Drop approval columns from DocumentVersions
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[DocumentVersions]') AND name = 'ApprovalStatus')
    ALTER TABLE [DocumentVersions] DROP COLUMN [ApprovalStatus];

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[DocumentVersions]') AND name = 'ApprovedBy')
    ALTER TABLE [DocumentVersions] DROP COLUMN [ApprovedBy];

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[DocumentVersions]') AND name = 'ApprovedAt')
    ALTER TABLE [DocumentVersions] DROP COLUMN [ApprovedAt];

COMMIT;
