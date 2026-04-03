-- =============================================================================
-- ROLLBACK: Sprint 4 — Performance Indexes (Fase 6 Optimization)
-- Date: 2026-04-03
-- Description: Drops the composite and filtered indexes added in
--              20260403_Sprint4_PerformanceIndexes.sql
-- =============================================================================

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentHeaders_TenantId_Date'           AND object_id = OBJECT_ID('DocumentHeaders')) DROP INDEX [IX_DocumentHeaders_TenantId_Date]           ON [dbo].[DocumentHeaders];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentRows_TenantId_DocumentHeaderId'  AND object_id = OBJECT_ID('DocumentRows'))    DROP INDEX [IX_DocumentRows_TenantId_DocumentHeaderId]  ON [dbo].[DocumentRows];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentRows_TenantId_ProductId'         AND object_id = OBJECT_ID('DocumentRows'))    DROP INDEX [IX_DocumentRows_TenantId_ProductId]         ON [dbo].[DocumentRows];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentRows_IsPriceManual'              AND object_id = OBJECT_ID('DocumentRows'))    DROP INDEX [IX_DocumentRows_IsPriceManual]              ON [dbo].[DocumentRows];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentRows_AppliedPromotionsJSON_NotNull' AND object_id = OBJECT_ID('DocumentRows')) DROP INDEX [IX_DocumentRows_AppliedPromotionsJSON_NotNull] ON [dbo].[DocumentRows];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Promotions_TenantId_IsActive'            AND object_id = OBJECT_ID('Promotions'))     DROP INDEX [IX_Promotions_TenantId_IsActive]            ON [dbo].[Promotions];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Promotions_TenantId_StartDate_EndDate'   AND object_id = OBJECT_ID('Promotions'))     DROP INDEX [IX_Promotions_TenantId_StartDate_EndDate]   ON [dbo].[Promotions];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PriceLists_TenantId_Priority'            AND object_id = OBJECT_ID('PriceLists'))     DROP INDEX [IX_PriceLists_TenantId_Priority]            ON [dbo].[PriceLists];
GO
