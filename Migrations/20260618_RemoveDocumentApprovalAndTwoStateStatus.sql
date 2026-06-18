-- Migration: 20260618_RemoveDocumentApprovalAndTwoStateStatus
-- Description:
--   1) Remove document approval model (ApprovalStatus, ApprovedBy, ApprovedAt)
--   2) Normalize document lifecycle to Active(1) / Archived(4)
--      - Draft(0)     -> Active(1)
--      - Open(1)      -> Active(1) [already aligned, unchanged]
--      - Closed(2)    -> Archived(4)
--      - Cancelled(3) -> Archived(4)
-- Date: 2026-06-18

BEGIN TRANSACTION;

-- 1) Normalize DocumentHeaders status values to 2-state model
UPDATE "DocumentHeaders"
SET    "Status" = 1,
       "ModifiedAt" = NOW() AT TIME ZONE 'UTC',
       "ModifiedBy" = 'migration_20260618'
WHERE  "Status" = 0                       -- Draft -> Active
  AND  "IsDeleted" = FALSE;

UPDATE "DocumentHeaders"
SET    "Status" = 4,
       "ModifiedAt" = NOW() AT TIME ZONE 'UTC',
       "ModifiedBy" = 'migration_20260618'
WHERE  "Status" = 2                       -- Closed -> Archived
  AND  "IsDeleted" = FALSE;

UPDATE "DocumentHeaders"
SET    "Status" = 4,
       "ModifiedAt" = NOW() AT TIME ZONE 'UTC',
       "ModifiedBy" = 'migration_20260618'
WHERE  "Status" = 3                       -- Cancelled -> Archived
  AND  "IsDeleted" = FALSE;

-- 2) Normalize status history values
UPDATE "DocumentStatusHistories"
SET    "FromStatus" = 1
WHERE  "FromStatus" = 0;                  -- Draft -> Active

UPDATE "DocumentStatusHistories"
SET    "FromStatus" = 4
WHERE  "FromStatus" = 2;                  -- Closed -> Archived

UPDATE "DocumentStatusHistories"
SET    "FromStatus" = 4
WHERE  "FromStatus" = 3;                  -- Cancelled -> Archived

UPDATE "DocumentStatusHistories"
SET    "ToStatus" = 1
WHERE  "ToStatus" = 0;                    -- Draft -> Active

UPDATE "DocumentStatusHistories"
SET    "ToStatus" = 4
WHERE  "ToStatus" = 2;                    -- Closed -> Archived

UPDATE "DocumentStatusHistories"
SET    "ToStatus" = 4
WHERE  "ToStatus" = 3;                    -- Cancelled -> Archived

-- 3) Drop approval columns from DocumentHeaders
ALTER TABLE "DocumentHeaders" DROP COLUMN IF EXISTS "ApprovalStatus";
ALTER TABLE "DocumentHeaders" DROP COLUMN IF EXISTS "ApprovedBy";
ALTER TABLE "DocumentHeaders" DROP COLUMN IF EXISTS "ApprovedAt";

-- 4) Drop approval columns from DocumentVersions
ALTER TABLE "DocumentVersions" DROP COLUMN IF EXISTS "ApprovalStatus";
ALTER TABLE "DocumentVersions" DROP COLUMN IF EXISTS "ApprovedBy";
ALTER TABLE "DocumentVersions" DROP COLUMN IF EXISTS "ApprovedAt";

COMMIT;
