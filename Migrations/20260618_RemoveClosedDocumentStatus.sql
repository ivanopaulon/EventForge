-- Migration: 20260618_RemoveClosedDocumentStatus
-- Description: Removes the Closed (2) document status by migrating existing Closed documents
--              to Archived (4). This aligns with the new document lifecycle:
--              Draft (0) → Active (1) → Archived (4), with Cancelled (3) as a dead end.
--              The Open (1) enum value is renamed to Active (1) — no DB change required.
-- Author: EventForge
-- Date: 2026-06-18

BEGIN TRANSACTION;

-- Step 1: Migrate all Closed (2) documents to Archived (4).
-- Rationale: "Closed" was the finalized state before archiving; Archived is the closest
-- equivalent in the new model and is semantically appropriate.
UPDATE "DocumentHeaders"
SET    "Status" = 4,                        -- Archived
       "ModifiedAt" = NOW() AT TIME ZONE 'UTC',
       "ModifiedBy" = 'migration_20260618'
WHERE  "Status" = 2                         -- Closed
  AND  "IsDeleted" = FALSE;

-- Step 2: Migrate Closed (2) entries in DocumentStatusHistory to Archived (4)
--         for the ToStatus column.
UPDATE "DocumentStatusHistories"
SET    "ToStatus" = 4                       -- Archived
WHERE  "ToStatus" = 2;                      -- Closed

-- Step 3: Migrate Closed (2) entries in DocumentStatusHistory to Active (1)
--         for the FromStatus column (Closed was reached from Open/Active).
UPDATE "DocumentStatusHistories"
SET    "FromStatus" = 1                     -- Active
WHERE  "FromStatus" = 2;                    -- Closed

COMMIT;
