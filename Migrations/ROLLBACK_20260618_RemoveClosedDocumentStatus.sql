-- Rollback: 20260618_RemoveClosedDocumentStatus
-- Description: Reverts documents from Archived (4) back to Closed (2) for those that were
--              migrated by 20260618_RemoveClosedDocumentStatus. Note: this rollback cannot
--              distinguish documents that were already Archived before the migration from those
--              that were Closed. Use with caution and only if no new Archived documents have
--              been created after the migration.
-- Author: EventForge
-- Date: 2026-06-18
-- Syntax: SQL Server (T-SQL)

BEGIN TRANSACTION;

-- WARNING: This rollback moves ALL Archived documents back to Closed. If any documents were
-- legitimately archived after the forward migration, they will incorrectly revert to Closed.
-- Restore from backup instead if a precise rollback is required.

-- Step 1: Revert Archived (4) documents to Closed (2).
UPDATE [DocumentHeaders]
SET    [Status] = 2,                        -- Closed
       [ModifiedAt] = GETUTCDATE(),
       [ModifiedBy] = 'rollback_20260618'
WHERE  [Status] = 4                         -- Archived
  AND  [IsDeleted] = 0;

-- Step 2: Revert ToStatus Archived (4) → Closed (2) in status history.
UPDATE [DocumentStatusHistories]
SET    [ToStatus] = 2                       -- Closed
WHERE  [ToStatus] = 4;                      -- Archived

-- Step 3: Revert FromStatus Active (1) → Closed (2) in status history entries
--         where the next entry transitioned to Closed.
UPDATE [DocumentStatusHistories]
SET    [FromStatus] = 2                     -- Closed
WHERE  [FromStatus] = 1                     -- Active (was Closed before migration)
  AND  [ToStatus] = 4;                      -- entries that went to Archived

COMMIT;
