-- Rollback: Revert DailyClosureRecords.PrinterId to NOT NULL
-- Date: 2026-07-07
-- Companion to: 20260707_MakeDailyClosureRecordsPrinterIdNullable.sql
--
-- WARNING: This rollback will fail if any row has PrinterId IS NULL
-- (i.e. any "NonFiscale" no-printer closure has been recorded since the fix
-- was deployed). Either delete/reassign those rows or keep the column
-- nullable before re-applying this script.
--
-- VALIDATION QUERY (run before executing):
--   SELECT COUNT(*) FROM [dbo].[DailyClosureRecords] WHERE [PrinterId] IS NULL;
--   -- Must be 0 before rollback, otherwise the ALTER COLUMN will fail.

BEGIN TRANSACTION;

ALTER TABLE [dbo].[DailyClosureRecords] ALTER COLUMN [PrinterId] uniqueidentifier NOT NULL;

COMMIT;
