-- Rollback: 20260618_RemoveDocumentApprovalAndTwoStateStatus
-- Description:
--   Restores approval columns and re-expands status values best-effort.
--   ⚠️ WARNING: Status rollback is lossy because forward migration merged:
--     Draft(0) + Open(1) -> Active(1), and Closed(2) + Cancelled(3) -> Archived(4).
--   Rollback cannot distinguish original values inside each merged group.
--   Use only for emergency rollback.
-- Date: 2026-06-18

BEGIN TRANSACTION;

-- 1) Re-add approval columns on DocumentHeaders
ALTER TABLE "DocumentHeaders" ADD COLUMN IF NOT EXISTS "ApprovalStatus" integer NOT NULL DEFAULT 0;
ALTER TABLE "DocumentHeaders" ADD COLUMN IF NOT EXISTS "ApprovedBy" varchar(100) NULL;
ALTER TABLE "DocumentHeaders" ADD COLUMN IF NOT EXISTS "ApprovedAt" timestamp NULL;

-- 2) Re-add approval columns on DocumentVersions
ALTER TABLE "DocumentVersions" ADD COLUMN IF NOT EXISTS "ApprovalStatus" integer NOT NULL DEFAULT 0;
ALTER TABLE "DocumentVersions" ADD COLUMN IF NOT EXISTS "ApprovedBy" varchar(100) NULL;
ALTER TABLE "DocumentVersions" ADD COLUMN IF NOT EXISTS "ApprovedAt" timestamp NULL;

-- 3) Best-effort status rollback:
--    Active(1)   -> Draft(0)      [arbitrary inside former Draft/Open merged group]
--    Archived(4) -> Cancelled(3)  [arbitrary inside former Closed/Cancelled merged group]
--    NOTE: original distinctions cannot be reconstructed after the forward merge.
UPDATE "DocumentHeaders"
SET    "Status" = 0,
       "ModifiedAt" = NOW() AT TIME ZONE 'UTC',
       "ModifiedBy" = 'rollback_20260618'
WHERE  "Status" = 1
  AND  "IsDeleted" = FALSE;

UPDATE "DocumentHeaders"
SET    "Status" = 3,
       "ModifiedAt" = NOW() AT TIME ZONE 'UTC',
       "ModifiedBy" = 'rollback_20260618'
WHERE  "Status" = 4
  AND  "IsDeleted" = FALSE;

UPDATE "DocumentStatusHistories"
SET    "FromStatus" = 0
WHERE  "FromStatus" = 1;

UPDATE "DocumentStatusHistories"
SET    "FromStatus" = 3
WHERE  "FromStatus" = 4;

UPDATE "DocumentStatusHistories"
SET    "ToStatus" = 0
WHERE  "ToStatus" = 1;

UPDATE "DocumentStatusHistories"
SET    "ToStatus" = 3
WHERE  "ToStatus" = 4;

COMMIT;
