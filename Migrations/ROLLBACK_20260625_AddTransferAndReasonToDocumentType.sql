-- ============================================================
-- ROLLBACK: 20260625_AddTransferAndReasonToDocumentType
-- ============================================================

BEGIN TRANSACTION;

ALTER TABLE [DocumentTypes] DROP COLUMN [DefaultMovementReason];
ALTER TABLE [DocumentTypes] DROP COLUMN [IsTransferDocument];

COMMIT TRANSACTION;
