-- ============================================================
-- ROLLBACK: 20260404_AddPromotionRuleProducts
-- Description: Reverses the migration that created PromotionRuleProducts
--              and added BusinessPartyGroupIds / IsCombinable to
--              PromotionRules.
-- Date: 2026-04-04
-- WARNING: Dropping PromotionRuleProducts removes all product
--          associations stored for promotion rules.
-- ============================================================

-- 1. Drop PromotionRuleProducts table (and its indexes / FKs)
IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PromotionRuleProducts]') AND type = 'U')
BEGIN
    DROP TABLE [dbo].[PromotionRuleProducts];
    PRINT 'Table PromotionRuleProducts dropped.';
END
ELSE
BEGIN
    PRINT 'Table PromotionRuleProducts does not exist. Skipping.';
END
GO

-- 2. Drop BusinessPartyGroupIds column from PromotionRules
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[PromotionRules]')
      AND name = 'BusinessPartyGroupIds'
)
BEGIN
    ALTER TABLE [dbo].[PromotionRules]
    DROP COLUMN [BusinessPartyGroupIds];

    PRINT 'Column BusinessPartyGroupIds dropped from PromotionRules.';
END
ELSE
BEGIN
    PRINT 'Column BusinessPartyGroupIds does not exist in PromotionRules. Skipping.';
END
GO

-- 3. Drop IsCombinable column from PromotionRules
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[PromotionRules]')
      AND name = 'IsCombinable'
)
BEGIN
    ALTER TABLE [dbo].[PromotionRules]
    DROP COLUMN [IsCombinable];

    PRINT 'Column IsCombinable dropped from PromotionRules.';
END
ELSE
BEGIN
    PRINT 'Column IsCombinable does not exist in PromotionRules. Skipping.';
END
GO

PRINT 'Rollback ROLLBACK_20260404_AddPromotionRuleProducts completed successfully.';
GO
