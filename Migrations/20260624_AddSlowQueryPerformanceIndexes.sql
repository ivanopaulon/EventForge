-- =============================================================================
-- Migration: Slow-Query Performance Indexes
-- Date: 2026-06-24
-- Description: Adds composite indexes that eliminate the most impactful slow
--              queries identified in production logs:
--
--   • Printers (IsDeleted, IsFiscalPrinter)
--       → fiscal-printer polling query was running a full table scan (~5500 ms)
--
--   • BusinessParties (TenantId, IsDeleted, Name)
--       → paginated list + search endpoints filter/order on these three columns
--
--   • SystemOperationLogs (Severity, ExecutedAt)
--       → monitoring-dashboard reads errors ordered by time
--
--   • UserRoles (UserId, ExpiresAt)
--       → RefreshTokenAsync filtered-include on expiry date
-- =============================================================================

-- ------------------------------------------------
-- Printers: composite index (IsDeleted, IsFiscalPrinter)
-- ------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Printers_IsDeleted_IsFiscalPrinter'
      AND object_id = OBJECT_ID('Printers')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Printers_IsDeleted_IsFiscalPrinter]
        ON [dbo].[Printers] ([IsDeleted], [IsFiscalPrinter])
        INCLUDE ([Name]);
END;
GO

-- ------------------------------------------------
-- BusinessParties: composite index (TenantId, IsDeleted, Name)
-- ------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BusinessParties_TenantId_IsDeleted_Name'
      AND object_id = OBJECT_ID('BusinessParties')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_BusinessParties_TenantId_IsDeleted_Name]
        ON [dbo].[BusinessParties] ([TenantId], [IsDeleted], [Name]);
END;
GO

-- ------------------------------------------------
-- SystemOperationLogs: composite index (Severity, ExecutedAt)
-- ------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_SystemOperationLogs_Severity_ExecutedAt'
      AND object_id = OBJECT_ID('SystemOperationLogs')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SystemOperationLogs_Severity_ExecutedAt]
        ON [dbo].[SystemOperationLogs] ([Severity], [ExecutedAt] DESC);
END;
GO

-- ------------------------------------------------
-- UserRoles: composite index (UserId, ExpiresAt)
-- ------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_UserRoles_UserId_ExpiresAt'
      AND object_id = OBJECT_ID('UserRoles')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UserRoles_UserId_ExpiresAt]
        ON [dbo].[UserRoles] ([UserId], [ExpiresAt]);
END;
GO
