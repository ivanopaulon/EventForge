-- Migration: Remove ImageUrl column from Products table
-- Date: 2026-07-06
-- Related to: PIANO_CORREZIONE.md P11 (Task E1 — Milestone 4)
--
-- PREREQUISITE CHECKLIST (human checkpoint required before executing):
--   1. Confirm no client external to the application reads Products.ImageUrl via API
--   2. Confirm all frontend consumers use ThumbnailUrl/ImageDocumentId (completed in Milestone 4 PR)
--   3. Confirm no seed/bootstrap scripts set ImageUrl
--   4. Run validation query below and check result count is acceptable
--   5. Deploy the application code changes BEFORE running this migration
--
-- VALIDATION QUERY (run before executing DROP):
--   SELECT COUNT(*) FROM Products WHERE ImageUrl IS NOT NULL AND ImageUrl != '';
--   -- If count > 0, consider whether the data needs to be migrated to DocumentReference first.
--
-- ROLLBACK: See ROLLBACK_20260706_RemoveImageUrlFromProducts.sql

BEGIN TRANSACTION;

ALTER TABLE Products DROP COLUMN ImageUrl;

COMMIT;
