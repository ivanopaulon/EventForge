-- =============================================
-- Migration: Verify RowVersion columns for SaleSession and SaleItem
-- Date: 2025-12-09
-- Description: Verifies that ROWVERSION columns exist in SaleSessions and SaleItems tables
--              for optimistic concurrency control. These columns should already exist from
--              the AuditableEntity base class, but this migration adds them if missing.
--              This enables proper concurrency control for POS operations, preventing
--              conflicts when multiple terminals attempt to modify the same session.
-- =============================================

USE [EventData];
GO

-- Verify/Add RowVersion column to SaleSessions table
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[SaleSessions]') 
    AND name = 'RowVersion'
)
BEGIN
    ALTER TABLE [dbo].[SaleSessions]
    ADD [RowVersion] ROWVERSION NOT NULL;
    
    PRINT 'Added RowVersion column to SaleSessions table.';
    
    -- Add extended property for documentation
    EXEC sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Row version for optimistic concurrency control. Automatically updated by SQL Server on each row modification.', 
        @level0type = N'SCHEMA', @level0name = 'dbo',
        @level1type = N'TABLE', @level1name = 'SaleSessions',
        @level2type = N'COLUMN', @level2name = 'RowVersion';
END
ELSE
BEGIN
    PRINT 'RowVersion column already exists in SaleSessions table.';
END
GO

-- Verify/Add RowVersion column to SaleItems table
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[SaleItems]') 
    AND name = 'RowVersion'
)
BEGIN
    ALTER TABLE [dbo].[SaleItems]
    ADD [RowVersion] ROWVERSION NOT NULL;
    
    PRINT 'Added RowVersion column to SaleItems table.';
    
    -- Add extended property for documentation
    EXEC sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Row version for optimistic concurrency control. Automatically updated by SQL Server on each row modification.', 
        @level0type = N'SCHEMA', @level0name = 'dbo',
        @level1type = N'TABLE', @level1name = 'SaleItems',
        @level2type = N'COLUMN', @level2name = 'RowVersion';
END
ELSE
BEGIN
    PRINT 'RowVersion column already exists in SaleItems table.';
END
GO

PRINT 'Migration 20251209_AddRowVersionToSaleEntities completed successfully.';
GO
