-- =============================================================================
-- ROLLBACK: Slow-Query Performance Indexes
-- Date: 2026-06-24
-- Description: Drops the indexes added by 20260624_AddSlowQueryPerformanceIndexes.sql
-- =============================================================================

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Printers_IsDeleted_IsFiscalPrinter' AND object_id = OBJECT_ID('Printers'))
    DROP INDEX [IX_Printers_IsDeleted_IsFiscalPrinter] ON [dbo].[Printers];
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_BusinessParties_TenantId_IsDeleted_Name' AND object_id = OBJECT_ID('BusinessParties'))
    DROP INDEX [IX_BusinessParties_TenantId_IsDeleted_Name] ON [dbo].[BusinessParties];
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SystemOperationLogs_Severity_ExecutedAt' AND object_id = OBJECT_ID('SystemOperationLogs'))
    DROP INDEX [IX_SystemOperationLogs_Severity_ExecutedAt] ON [dbo].[SystemOperationLogs];
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserRoles_UserId_ExpiresAt' AND object_id = OBJECT_ID('UserRoles'))
    DROP INDEX [IX_UserRoles_UserId_ExpiresAt] ON [dbo].[UserRoles];
GO
