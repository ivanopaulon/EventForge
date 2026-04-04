-- ROLLBACK: 20260404_AddPromotionTargetingFields

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Promotions]') AND name = 'MaxTotalDiscountPercentage')
BEGIN
    ALTER TABLE [dbo].[Promotions] DROP COLUMN [MaxTotalDiscountPercentage];
    PRINT 'Column MaxTotalDiscountPercentage dropped from Promotions.';
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Promotions]') AND name = 'MaxUsesPerCustomer')
BEGIN
    ALTER TABLE [dbo].[Promotions] DROP COLUMN [MaxUsesPerCustomer];
    PRINT 'Column MaxUsesPerCustomer dropped from Promotions.';
END
GO
