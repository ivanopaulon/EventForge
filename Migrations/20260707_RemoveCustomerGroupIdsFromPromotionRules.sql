-- Migration: Remove deprecated CustomerGroupIds column from PromotionRules
-- Issue: P08 — PromotionRule.CustomerGroupIds obsolete field cleanup
-- Date: 2026-07-07
--
-- PREREQUISITE: Run the data-safety check below before executing on production.
-- If COUNT(*) > 0, first verify that BusinessPartyGroupIds already contains the
-- migrated data; otherwise run the data migration step.
--
-- ============================================================
-- STEP 0: Data-safety check (run manually before proceeding)
-- ============================================================
-- SELECT COUNT(*) AS RowsWithLegacyData
-- FROM PromotionRules
-- WHERE CustomerGroupIds IS NOT NULL
--   AND CustomerGroupIds NOT IN ('[]', 'null', '');
--
-- ============================================================
-- STEP 1: Data migration (run only if STEP 0 count > 0)
-- ============================================================
-- UPDATE PromotionRules
-- SET BusinessPartyGroupIds = CustomerGroupIds
-- WHERE BusinessPartyGroupIds IS NULL
--   AND CustomerGroupIds IS NOT NULL
--   AND CustomerGroupIds NOT IN ('[]', 'null', '');
--
-- ============================================================
-- STEP 2: Drop the deprecated column (run after STEP 0/1)
-- ============================================================
BEGIN TRANSACTION;

ALTER TABLE PromotionRules DROP COLUMN CustomerGroupIds;

COMMIT;
