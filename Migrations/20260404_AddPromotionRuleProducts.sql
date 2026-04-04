-- ============================================================
-- Migration: 20260404_AddPromotionRuleProducts
-- Description: Creates the PromotionRuleProducts join table and
--              adds BusinessPartyGroupIds and IsCombinable columns
--              to the PromotionRules table.
--              BusinessPartyGroupIds replaces the deprecated
--              CustomerGroupIds field and allows targeting rules
--              at specific Business Party Groups.
--              IsCombinable controls whether a rule can be stacked
--              with others in the same promotion.
-- Date: 2026-04-04
-- ============================================================

-- 1. Create PromotionRuleProducts table
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PromotionRuleProducts]') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[PromotionRuleProducts] (
        [Id]             UNIQUEIDENTIFIER NOT NULL,
        [PromotionRuleId] UNIQUEIDENTIFIER NOT NULL,
        [ProductId]      UNIQUEIDENTIFIER NOT NULL,
        [Quantity]       INT              NULL,
        [TenantId]       UNIQUEIDENTIFIER NOT NULL,
        [CreatedAt]      DATETIME2        NOT NULL,
        [CreatedBy]      NVARCHAR(100)    NULL,
        [ModifiedAt]     DATETIME2        NULL,
        [ModifiedBy]     NVARCHAR(100)    NULL,
        [IsDeleted]      BIT              NOT NULL DEFAULT 0,
        [DeletedAt]      DATETIME2        NULL,
        [DeletedBy]      NVARCHAR(100)    NULL,
        [IsActive]       BIT              NOT NULL DEFAULT 1,
        [RowVersion]     ROWVERSION       NULL,
        CONSTRAINT [PK_PromotionRuleProducts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PromotionRuleProducts_Products_ProductId]
            FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PromotionRuleProducts_PromotionRules_PromotionRuleId]
            FOREIGN KEY ([PromotionRuleId]) REFERENCES [dbo].[PromotionRules] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_PromotionRuleProducts_ProductId]      ON [dbo].[PromotionRuleProducts] ([ProductId]);
    CREATE INDEX [IX_PromotionRuleProducts_PromotionRuleId] ON [dbo].[PromotionRuleProducts] ([PromotionRuleId]);

    PRINT 'Table PromotionRuleProducts created.';
END
ELSE
BEGIN
    PRINT 'Table PromotionRuleProducts already exists. Skipping creation.';
END
GO

-- 2. Add BusinessPartyGroupIds column to PromotionRules
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[PromotionRules]')
      AND name = 'BusinessPartyGroupIds'
)
BEGIN
    ALTER TABLE [dbo].[PromotionRules]
    ADD [BusinessPartyGroupIds] NVARCHAR(MAX) NULL;

    PRINT 'Column BusinessPartyGroupIds added to PromotionRules.';
END
ELSE
BEGIN
    PRINT 'Column BusinessPartyGroupIds already exists in PromotionRules. Skipping.';
END
GO

-- 3. Add IsCombinable column to PromotionRules
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[PromotionRules]')
      AND name = 'IsCombinable'
)
BEGIN
    ALTER TABLE [dbo].[PromotionRules]
    ADD [IsCombinable] BIT NOT NULL DEFAULT 1;

    PRINT 'Column IsCombinable added to PromotionRules.';
END
ELSE
BEGIN
    PRINT 'Column IsCombinable already exists in PromotionRules. Skipping.';
END
GO

PRINT 'Migration 20260404_AddPromotionRuleProducts completed successfully.';
GO
