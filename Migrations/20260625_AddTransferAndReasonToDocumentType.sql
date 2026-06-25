-- ============================================================
-- Migration: 20260625_AddTransferAndReasonToDocumentType
-- Adds two columns to DocumentTypes:
--   IsTransferDocument  – marks document types that represent
--                         a stock transfer between warehouses.
--   DefaultMovementReason – overrides the heuristic (Purchase/
--                         Sale) used by the rebuild procedure.
-- ============================================================

BEGIN TRANSACTION;

-- 1. IsTransferDocument (default false — no existing type is a transfer)
ALTER TABLE [DocumentTypes]
    ADD [IsTransferDocument] bit NOT NULL DEFAULT 0;

-- 2. DefaultMovementReason (nullable string, maps to StockMovementReason enum)
ALTER TABLE [DocumentTypes]
    ADD [DefaultMovementReason] nvarchar(50) NULL;

COMMIT TRANSACTION;
