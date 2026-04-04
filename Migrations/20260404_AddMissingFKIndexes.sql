-- Migration: 20260404_AddMissingFKIndexes
-- Purpose: Add missing FK indexes for foreign-key columns that lacked explicit index definitions.
--          Improves query performance on JOINs and lookups by FK.
--          All indexes are created WITH (ONLINE = ON) to avoid blocking production reads/writes.
-- Safe to run in production: CREATE INDEX … IF NOT EXISTS is not supported by SQL Server,
-- so we guard each with an EXISTS check on sys.indexes.

-- ── DocumentHeaders ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentHeaders_BusinessPartyId' AND object_id = OBJECT_ID('DocumentHeaders'))
    CREATE NONCLUSTERED INDEX IX_DocumentHeaders_BusinessPartyId
        ON DocumentHeaders (BusinessPartyId)
        WITH (ONLINE = ON);
GO

-- ── PriceListEntries ─────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PriceListEntries_PriceListId' AND object_id = OBJECT_ID('PriceListEntries'))
    CREATE NONCLUSTERED INDEX IX_PriceListEntries_PriceListId
        ON PriceListEntries (PriceListId)
        WITH (ONLINE = ON);
GO

-- ── PromotionRules ───────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PromotionRules_PromotionId' AND object_id = OBJECT_ID('PromotionRules'))
    CREATE NONCLUSTERED INDEX IX_PromotionRules_PromotionId
        ON PromotionRules (PromotionId)
        WITH (ONLINE = ON);
GO

-- ── PromotionRuleProducts ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PromotionRuleProducts_PromotionRuleId' AND object_id = OBJECT_ID('PromotionRuleProducts'))
    CREATE NONCLUSTERED INDEX IX_PromotionRuleProducts_PromotionRuleId
        ON PromotionRuleProducts (PromotionRuleId)
        WITH (ONLINE = ON);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PromotionRuleProducts_ProductId' AND object_id = OBJECT_ID('PromotionRuleProducts'))
    CREATE NONCLUSTERED INDEX IX_PromotionRuleProducts_ProductId
        ON PromotionRuleProducts (ProductId)
        WITH (ONLINE = ON);
GO

-- ── UserRoles ────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserRoles_UserId' AND object_id = OBJECT_ID('UserRoles'))
    CREATE NONCLUSTERED INDEX IX_UserRoles_UserId
        ON UserRoles (UserId)
        WITH (ONLINE = ON);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserRoles_RoleId' AND object_id = OBJECT_ID('UserRoles'))
    CREATE NONCLUSTERED INDEX IX_UserRoles_RoleId
        ON UserRoles (RoleId)
        WITH (ONLINE = ON);
GO

-- ── RolePermissions ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RolePermissions_RoleId' AND object_id = OBJECT_ID('RolePermissions'))
    CREATE NONCLUSTERED INDEX IX_RolePermissions_RoleId
        ON RolePermissions (RoleId)
        WITH (ONLINE = ON);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RolePermissions_PermissionId' AND object_id = OBJECT_ID('RolePermissions'))
    CREATE NONCLUSTERED INDEX IX_RolePermissions_PermissionId
        ON RolePermissions (PermissionId)
        WITH (ONLINE = ON);
GO

-- ── LoginAudits ──────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LoginAudits_UserId' AND object_id = OBJECT_ID('LoginAudits'))
    CREATE NONCLUSTERED INDEX IX_LoginAudits_UserId
        ON LoginAudits (UserId)
        WITH (ONLINE = ON);
GO

-- ── NotificationRecipients ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_NotificationRecipients_NotificationId' AND object_id = OBJECT_ID('NotificationRecipients'))
    CREATE NONCLUSTERED INDEX IX_NotificationRecipients_NotificationId
        ON NotificationRecipients (NotificationId)
        WITH (ONLINE = ON);
GO

-- ── SaleItems ────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SaleItems_SaleSessionId' AND object_id = OBJECT_ID('SaleItems'))
    CREATE NONCLUSTERED INDEX IX_SaleItems_SaleSessionId
        ON SaleItems (SaleSessionId)
        WITH (ONLINE = ON);
GO

-- ── SalePayments ─────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SalePayments_SaleSessionId' AND object_id = OBJECT_ID('SalePayments'))
    CREATE NONCLUSTERED INDEX IX_SalePayments_SaleSessionId
        ON SalePayments (SaleSessionId)
        WITH (ONLINE = ON);
GO

PRINT 'Migration 20260404_AddMissingFKIndexes completed successfully.';
