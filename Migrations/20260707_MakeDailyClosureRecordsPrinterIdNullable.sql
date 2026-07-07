-- Migration: Make DailyClosureRecords.PrinterId nullable
-- Date: 2026-07-07
-- Related to: Fix FK_DailyClosureRecords_Printers_PrinterId violation on
--             POST /api/v1/fiscal-printing/daily-closure/execute-no-printer/{posId}
--
-- ROOT CAUSE:
--   ExecuteNoPrinterDailyClosureAsync (FiscalPrinterServiceRouter) inserted
--   DailyClosureRecord rows with PrinterId = Guid.Empty for POS terminals that have
--   no physical fiscal printer configured. Guid.Empty never exists in the Printers
--   table, so the FK_DailyClosureRecords_Printers_PrinterId constraint always
--   rejected the INSERT with SQL error 547, surfacing as an HTTP 500.
--
-- FIX:
--   DailyClosureRecord.PrinterId is now Guid? (nullable). No-printer ("NonFiscale")
--   closures are saved with PrinterId = NULL instead of Guid.Empty, which is valid
--   for the existing FK constraint (NULL FK values are not checked).
--
-- ROLLBACK: See ROLLBACK_20260707_MakeDailyClosureRecordsPrinterIdNullable.sql

BEGIN TRANSACTION;

ALTER TABLE [dbo].[DailyClosureRecords] ALTER COLUMN [PrinterId] uniqueidentifier NULL;

COMMIT;
