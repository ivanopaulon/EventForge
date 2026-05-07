-- Migration: 20260507_AddUniqueIndexProductBundleItem
-- Purpose: Add a filtered unique index on ProductBundleItems (BundleProductId, ComponentProductId)
--          to enforce at the database level that a component can only appear once per bundle
--          (among non-deleted rows). Application-level validation also enforces this rule.

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ProductBundleItems')
      AND name = N'UX_ProductBundleItem_BundleProduct_ComponentProduct'
)
BEGIN
    CREATE UNIQUE INDEX [UX_ProductBundleItem_BundleProduct_ComponentProduct]
        ON [dbo].[ProductBundleItems] ([BundleProductId], [ComponentProductId])
        WHERE [IsDeleted] = 0;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ProductBundleItems')
      AND name = N'IX_ProductBundleItem_BundleProductId'
)
BEGIN
    CREATE INDEX [IX_ProductBundleItem_BundleProductId]
        ON [dbo].[ProductBundleItems] ([BundleProductId]);
END
GO

PRINT 'Migration 20260507_AddUniqueIndexProductBundleItem completed.';
