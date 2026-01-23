-- =====================================================
-- ROLLBACK PLAN (EMERGENCY ONLY - USE WITH CAUTION)
-- Migration: RemoveForcedPriceListIdOverride
-- Date: 2026-01-23
-- =====================================================

-- ⚠️  WARNING: This rollback will NOT restore migrated data!
-- Only use if forward migration failed mid-execution.
-- Always restore from backup for production data recovery.

BEGIN TRANSACTION;

PRINT '=== Starting ROLLBACK of ForcedPriceListIdOverride Removal ===';

-- Step 1: Re-add column
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('DocumentHeaders') 
      AND name = 'ForcedPriceListIdOverride'
)
BEGIN
    ALTER TABLE DocumentHeaders
    ADD ForcedPriceListIdOverride UNIQUEIDENTIFIER NULL;
    
    PRINT '✅ Re-added column ForcedPriceListIdOverride';
END
ELSE
BEGIN
    PRINT 'ℹ️  Column already exists';
END

-- Step 2: Re-create FK
IF NOT EXISTS (
    SELECT 1 
    FROM sys.foreign_keys 
    WHERE name = 'FK_DocumentHeaders_PriceLists_ForcedPriceListIdOverride'
      AND parent_object_id = OBJECT_ID('DocumentHeaders')
)
BEGIN
    ALTER TABLE DocumentHeaders
    ADD CONSTRAINT FK_DocumentHeaders_PriceLists_ForcedPriceListIdOverride
    FOREIGN KEY (ForcedPriceListIdOverride)
    REFERENCES PriceLists(Id)
    ON DELETE NO ACTION;
    
    PRINT '✅ Re-created FK_DocumentHeaders_PriceLists_ForcedPriceListIdOverride';
END
ELSE
BEGIN
    PRINT 'ℹ️  FK already exists';
END

-- Step 3: Re-create index (if it existed)
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_DocumentHeaders_ForcedPriceListIdOverride'
      AND object_id = OBJECT_ID('DocumentHeaders')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_DocumentHeaders_ForcedPriceListIdOverride
    ON DocumentHeaders(ForcedPriceListIdOverride);
    
    PRINT '✅ Re-created index IX_DocumentHeaders_ForcedPriceListIdOverride';
END
ELSE
BEGIN
    PRINT 'ℹ️  Index already exists';
END

COMMIT TRANSACTION;

PRINT '';
PRINT '=== Rollback completed ===';
PRINT '';
PRINT '⚠️  CRITICAL WARNINGS:';
PRINT '  1. Migrated data (ForcedPriceListIdOverride → PriceListId) was NOT restored';
PRINT '  2. Column is now empty - data loss occurred';
PRINT '  3. To recover data, restore from backup';
PRINT '  4. This rollback only recreates schema structure';
GO
