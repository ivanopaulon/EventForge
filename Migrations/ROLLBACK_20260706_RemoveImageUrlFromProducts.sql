-- Rollback: Restore ImageUrl column on Products table
-- Date: 2026-07-06
-- Companion rollback for: 20260706_RemoveImageUrlFromProducts.sql

BEGIN TRANSACTION;

ALTER TABLE Products ADD ImageUrl NVARCHAR(500) NOT NULL DEFAULT '';

COMMIT;
