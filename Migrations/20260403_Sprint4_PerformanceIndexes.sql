-- =============================================================================
-- Migration: Sprint 4 — Performance Indexes (Fase 6 Optimization)
-- Date: 2026-04-03
-- Description: Adds composite and filtered indexes on DocumentHeaders,
--              DocumentRows, Promotions and PriceLists to accelerate
--              multi-tenant pricing and promotion query paths.
-- =============================================================================

-- ------------------------------------------------
-- DocumentHeaders: composite index (TenantId, Date)
-- ------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_DocumentHeaders_TenantId_Date'
      AND object_id = OBJECT_ID('DocumentHeaders')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_DocumentHeaders_TenantId_Date]
        ON [dbo].[DocumentHeaders] ([TenantId], [Date])
        WHERE [IsDeleted] = 0;
END;
GO

-- ------------------------------------------------
-- DocumentRows: composite index (TenantId, DocumentHeaderId)
-- ------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_DocumentRows_TenantId_DocumentHeaderId'
      AND object_id = OBJECT_ID('DocumentRows')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_DocumentRows_TenantId_DocumentHeaderId]
        ON [dbo].[DocumentRows] ([TenantId], [DocumentHeaderId])
        WHERE [IsDeleted] = 0;
END;
GO

-- ------------------------------------------------
-- DocumentRows: composite index (TenantId, ProductId)
-- ------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_DocumentRows_TenantId_ProductId'
      AND object_id = OBJECT_ID('DocumentRows')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_DocumentRows_TenantId_ProductId]
        ON [dbo].[DocumentRows] ([TenantId], [ProductId])
        WHERE [IsDeleted] = 0;
END;
GO

-- ------------------------------------------------
-- DocumentRows: index on IsPriceManual
-- ------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_DocumentRows_IsPriceManual'
      AND object_id = OBJECT_ID('DocumentRows')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_DocumentRows_IsPriceManual]
        ON [dbo].[DocumentRows] ([IsPriceManual])
        WHERE [IsDeleted] = 0;
END;
GO

-- ------------------------------------------------
-- DocumentRows: filtered index — AppliedPromotionsJSON IS NOT NULL
-- ------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_DocumentRows_AppliedPromotionsJSON_NotNull'
      AND object_id = OBJECT_ID('DocumentRows')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_DocumentRows_AppliedPromotionsJSON_NotNull]
        ON [dbo].[DocumentRows] ([TenantId], [DocumentHeaderId])
        WHERE [AppliedPromotionsJSON] IS NOT NULL
          AND [IsDeleted] = 0;
END;
GO

-- ------------------------------------------------
-- Promotions: composite index (TenantId, IsActive)
-- ------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Promotions_TenantId_IsActive'
      AND object_id = OBJECT_ID('Promotions')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Promotions_TenantId_IsActive]
        ON [dbo].[Promotions] ([TenantId], [IsActive])
        WHERE [IsDeleted] = 0;
END;
GO

-- ------------------------------------------------
-- Promotions: composite index (TenantId, StartDate, EndDate)
-- ------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Promotions_TenantId_StartDate_EndDate'
      AND object_id = OBJECT_ID('Promotions')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Promotions_TenantId_StartDate_EndDate]
        ON [dbo].[Promotions] ([TenantId], [StartDate], [EndDate])
        WHERE [IsDeleted] = 0;
END;
GO

-- ------------------------------------------------
-- PriceLists: composite index (TenantId, Priority)
-- ------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_PriceLists_TenantId_Priority'
      AND object_id = OBJECT_ID('PriceLists')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PriceLists_TenantId_Priority]
        ON [dbo].[PriceLists] ([TenantId], [Priority])
        WHERE [IsDeleted] = 0;
END;
GO
