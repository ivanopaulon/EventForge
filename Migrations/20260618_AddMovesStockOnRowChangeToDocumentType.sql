-- Migration: 20260618_AddMovesStockOnRowChangeToDocumentType
-- Description:
--   Add the "MovesStockOnRowChange" flag to DocumentTypes.
--   When true, stock movements are created/updated/deleted immediately on every
--   document row change (add/update/delete), regardless of document status.
--   This replaces the bulk-on-archive generation for document types such as C3.
-- Date: 2026-06-18

BEGIN TRANSACTION;

ALTER TABLE "DocumentTypes"
ADD COLUMN "MovesStockOnRowChange" BOOLEAN NOT NULL DEFAULT TRUE;

COMMIT;
