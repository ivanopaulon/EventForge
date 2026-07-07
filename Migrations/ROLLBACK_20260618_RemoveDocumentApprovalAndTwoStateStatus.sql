-- Rollback: 20260618_RemoveDocumentApprovalAndTwoStateStatus
-- Description:
--   Restores approval columns and re-expands status values best-effort.
--   ⚠️ WARNING: Status rollback is lossy because forward migration merged:
--     Draft(0) + Open(1) -> Active(1), and Closed(2) + Cancelled(3) -> Archived(4).
--   Rollback cannot distinguish original values inside each merged group.
--   Use only for emergency rollback.
-- Date: 2026-06-18
-- Syntax: SQL Server (T-SQL)

BEGIN TRANSACTION;

-- 1) Re-add approval columns on DocumentHeaders
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[DocumentHeaders]') AND name = 'ApprovalStatus')
    ALTER TABLE [DocumentHeaders] ADD [ApprovalStatus] int NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[DocumentHeaders]') AND name = 'ApprovedBy')
    ALTER TABLE [DocumentHeaders] ADD [ApprovedBy] nvarchar(100) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[DocumentHeaders]') AND name = 'ApprovedAt')
    ALTER TABLE [DocumentHeaders] ADD [ApprovedAt] datetime2 NULL;

-- 2) Re-add approval columns on DocumentVersions
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[DocumentVersions]') AND name = 'ApprovalStatus')
    ALTER TABLE [DocumentVersions] ADD [ApprovalStatus] int NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[DocumentVersions]') AND name = 'ApprovedBy')
    ALTER TABLE [DocumentVersions] ADD [ApprovedBy] nvarchar(100) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[DocumentVersions]') AND name = 'ApprovedAt')
    ALTER TABLE [DocumentVersions] ADD [ApprovedAt] datetime2 NULL;

-- 3) Best-effort status rollback:
--    Active(1)   -> Draft(0)      [arbitrary inside former Draft/Open merged group]
--    Archived(4) -> Cancelled(3)  [arbitrary inside former Closed/Cancelled merged group]
--    NOTE: original distinctions cannot be reconstructed after the forward merge.
UPDATE [DocumentHeaders]
SET    [Status] = 0,
       [ModifiedAt] = GETUTCDATE(),
       [ModifiedBy] = 'rollback_20260618'
WHERE  [Status] = 1
  AND  [IsDeleted] = 0;

UPDATE [DocumentHeaders]
SET    [Status] = 3,
       [ModifiedAt] = GETUTCDATE(),
       [ModifiedBy] = 'rollback_20260618'
WHERE  [Status] = 4
  AND  [IsDeleted] = 0;

UPDATE [DocumentStatusHistories]
SET    [FromStatus] = 0
WHERE  [FromStatus] = 1;

UPDATE [DocumentStatusHistories]
SET    [FromStatus] = 3
WHERE  [FromStatus] = 4;

UPDATE [DocumentStatusHistories]
SET    [ToStatus] = 0
WHERE  [ToStatus] = 1;

UPDATE [DocumentStatusHistories]
SET    [ToStatus] = 3
WHERE  [ToStatus] = 4;

COMMIT;
