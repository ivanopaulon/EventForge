-- Rollback: 20260618_AddMovesStockOnRowChangeToDocumentType
-- Description: Remove the "MovesStockOnRowChange" column from DocumentTypes.
-- Date: 2026-06-18

BEGIN TRANSACTION;

ALTER TABLE "DocumentTypes"
DROP COLUMN "MovesStockOnRowChange";

COMMIT;
