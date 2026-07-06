-- Rollback: Restore ImageUrl column on Products table
-- Date: 2026-07-06
-- Companion rollback for: 20260706_RemoveImageUrlFromProducts.sql
--
-- NOTE: This rollback restores the column structure only.
-- Data that existed in ImageUrl before the forward migration was dropped cannot
-- be recovered from this script alone — restore from a database backup taken
-- prior to running 20260706_RemoveImageUrlFromProducts.sql.

BEGIN TRANSACTION;

ALTER TABLE Products ADD ImageUrl NVARCHAR(500) NOT NULL DEFAULT '';

COMMIT;
