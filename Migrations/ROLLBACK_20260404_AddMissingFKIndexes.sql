-- ROLLBACK: 20260404_AddMissingFKIndexes
-- Removes all indexes added by the forward migration.
-- Safe to run: each DROP is guarded by an EXISTS check.

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentHeaders_BusinessPartyId' AND object_id = OBJECT_ID('DocumentHeaders'))
    DROP INDEX IX_DocumentHeaders_BusinessPartyId ON DocumentHeaders;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PriceListEntries_PriceListId' AND object_id = OBJECT_ID('PriceListEntries'))
    DROP INDEX IX_PriceListEntries_PriceListId ON PriceListEntries;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PromotionRules_PromotionId' AND object_id = OBJECT_ID('PromotionRules'))
    DROP INDEX IX_PromotionRules_PromotionId ON PromotionRules;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PromotionRuleProducts_PromotionRuleId' AND object_id = OBJECT_ID('PromotionRuleProducts'))
    DROP INDEX IX_PromotionRuleProducts_PromotionRuleId ON PromotionRuleProducts;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PromotionRuleProducts_ProductId' AND object_id = OBJECT_ID('PromotionRuleProducts'))
    DROP INDEX IX_PromotionRuleProducts_ProductId ON PromotionRuleProducts;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserRoles_UserId' AND object_id = OBJECT_ID('UserRoles'))
    DROP INDEX IX_UserRoles_UserId ON UserRoles;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserRoles_RoleId' AND object_id = OBJECT_ID('UserRoles'))
    DROP INDEX IX_UserRoles_RoleId ON UserRoles;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RolePermissions_RoleId' AND object_id = OBJECT_ID('RolePermissions'))
    DROP INDEX IX_RolePermissions_RoleId ON RolePermissions;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RolePermissions_PermissionId' AND object_id = OBJECT_ID('RolePermissions'))
    DROP INDEX IX_RolePermissions_PermissionId ON RolePermissions;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LoginAudits_UserId' AND object_id = OBJECT_ID('LoginAudits'))
    DROP INDEX IX_LoginAudits_UserId ON LoginAudits;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_NotificationRecipients_NotificationId' AND object_id = OBJECT_ID('NotificationRecipients'))
    DROP INDEX IX_NotificationRecipients_NotificationId ON NotificationRecipients;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SaleItems_SaleSessionId' AND object_id = OBJECT_ID('SaleItems'))
    DROP INDEX IX_SaleItems_SaleSessionId ON SaleItems;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SalePayments_SaleSessionId' AND object_id = OBJECT_ID('SalePayments'))
    DROP INDEX IX_SalePayments_SaleSessionId ON SalePayments;
GO

PRINT 'Rollback 20260404_AddMissingFKIndexes completed.';
