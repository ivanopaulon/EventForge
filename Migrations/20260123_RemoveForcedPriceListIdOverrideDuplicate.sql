-- =====================================================
-- Migration: Remove ForcedPriceListIdOverride Duplicate
-- Date: 2026-01-23
-- Author: Copilot
-- Description: Cleanup duplicate field and consolidate to PriceListId
-- =====================================================

BEGIN TRANSACTION;

PRINT '=== Starting ForcedPriceListIdOverride Cleanup Migration ===';

-- =====================================================
-- STEP 1: Data Migration
-- =====================================================
PRINT 'Step 1: Migrating data from ForcedPriceListIdOverride to PriceListId...';

-- Log pre-migration statistics
DECLARE @TotalDocuments INT;
DECLARE @DocumentsWithForcedOverride INT;
DECLARE @DocumentsWithPriceList INT;
DECLARE @ConflictingDocuments INT;

SELECT @TotalDocuments = COUNT(*) FROM DocumentHeaders WHERE IsDeleted = 0;
SELECT @DocumentsWithForcedOverride = COUNT(*) FROM DocumentHeaders WHERE ForcedPriceListIdOverride IS NOT NULL AND IsDeleted = 0;
SELECT @DocumentsWithPriceList = COUNT(*) FROM DocumentHeaders WHERE PriceListId IS NOT NULL AND IsDeleted = 0;
SELECT @ConflictingDocuments = COUNT(*) 
FROM DocumentHeaders 
WHERE ForcedPriceListIdOverride IS NOT NULL 
  AND PriceListId IS NOT NULL
  AND ForcedPriceListIdOverride != PriceListId
  AND IsDeleted = 0;

PRINT 'Pre-migration stats:';
PRINT '  Total active documents: ' + CAST(@TotalDocuments AS VARCHAR);
PRINT '  Documents with ForcedPriceListIdOverride: ' + CAST(@DocumentsWithForcedOverride AS VARCHAR);
PRINT '  Documents with PriceListId: ' + CAST(@DocumentsWithPriceList AS VARCHAR);
PRINT '  Conflicting documents (different values): ' + CAST(@ConflictingDocuments AS VARCHAR);

-- Migrate data: Copy ForcedPriceListIdOverride → PriceListId where PriceListId is NULL
UPDATE DocumentHeaders
SET PriceListId = ForcedPriceListIdOverride
WHERE ForcedPriceListIdOverride IS NOT NULL
  AND PriceListId IS NULL
  AND IsDeleted = 0;

DECLARE @MigratedRows INT = @@ROWCOUNT;
PRINT 'Migrated ' + CAST(@MigratedRows AS VARCHAR) + ' records from ForcedPriceListIdOverride to PriceListId';

-- Log conflicts (documents with BOTH fields populated differently)
IF @ConflictingDocuments > 0
BEGIN
    PRINT '';
    PRINT '⚠️  WARNING: Found ' + CAST(@ConflictingDocuments AS VARCHAR) + ' documents with conflicting values:';
    
    SELECT TOP 10
        Id AS DocumentId, 
        DocumentNumber,
        PriceListId AS CurrentPriceListId, 
        ForcedPriceListIdOverride AS LegacyPriceListId,
        'PriceListId takes priority' AS Resolution,
        Date,
        BusinessPartyId
    FROM DocumentHeaders
    WHERE ForcedPriceListIdOverride IS NOT NULL 
      AND PriceListId IS NOT NULL
      AND ForcedPriceListIdOverride != PriceListId
      AND IsDeleted = 0
    ORDER BY Date DESC;
    
    PRINT '  → PriceListId will be kept (newer, correct field)';
    PRINT '  → ForcedPriceListIdOverride will be discarded';
    
    IF @ConflictingDocuments > 10
    BEGIN
        PRINT '  → Showing first 10 conflicts only. Total: ' + CAST(@ConflictingDocuments AS VARCHAR);
    END
END
ELSE
BEGIN
    PRINT '✅ No conflicts found - all documents have consistent data';
END

-- =====================================================
-- STEP 2: Drop Foreign Key Constraint
-- =====================================================
PRINT '';
PRINT 'Step 2: Dropping foreign key constraint...';

IF EXISTS (
    SELECT 1 
    FROM sys.foreign_keys 
    WHERE name = 'FK_DocumentHeaders_PriceLists_ForcedPriceListIdOverride'
      AND parent_object_id = OBJECT_ID('DocumentHeaders')
)
BEGIN
    ALTER TABLE DocumentHeaders
    DROP CONSTRAINT FK_DocumentHeaders_PriceLists_ForcedPriceListIdOverride;
    
    PRINT '✅ Dropped FK_DocumentHeaders_PriceLists_ForcedPriceListIdOverride';
END
ELSE
BEGIN
    PRINT 'ℹ️  FK constraint already removed or never existed';
END

-- =====================================================
-- STEP 3: Drop Index (if exists)
-- =====================================================
PRINT '';
PRINT 'Step 3: Dropping index on ForcedPriceListIdOverride...';

IF EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_DocumentHeaders_ForcedPriceListIdOverride'
      AND object_id = OBJECT_ID('DocumentHeaders')
)
BEGIN
    DROP INDEX IX_DocumentHeaders_ForcedPriceListIdOverride ON DocumentHeaders;
    PRINT '✅ Dropped index IX_DocumentHeaders_ForcedPriceListIdOverride';
END
ELSE
BEGIN
    PRINT 'ℹ️  Index already removed or never existed';
END

-- =====================================================
-- STEP 4: Drop Column
-- =====================================================
PRINT '';
PRINT 'Step 4: Dropping column ForcedPriceListIdOverride...';

IF EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('DocumentHeaders') 
      AND name = 'ForcedPriceListIdOverride'
)
BEGIN
    ALTER TABLE DocumentHeaders
    DROP COLUMN ForcedPriceListIdOverride;
    
    PRINT '✅ Dropped column ForcedPriceListIdOverride from DocumentHeaders';
END
ELSE
BEGIN
    PRINT 'ℹ️  Column already removed';
END

-- =====================================================
-- STEP 5: Verification
-- =====================================================
PRINT '';
PRINT 'Step 5: Verifying cleanup...';

-- Verify column removal
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('DocumentHeaders') 
      AND name = 'ForcedPriceListIdOverride'
)
BEGIN
    PRINT '✅ SUCCESS: Column ForcedPriceListIdOverride removed successfully';
END
ELSE
BEGIN
    PRINT '❌ ERROR: Column ForcedPriceListIdOverride still exists!';
    ROLLBACK TRANSACTION;
    RAISERROR('Migration failed: Column not removed', 16, 1);
    RETURN;
END

-- Verify FK removal
IF NOT EXISTS (
    SELECT 1 
    FROM sys.foreign_keys 
    WHERE name = 'FK_DocumentHeaders_PriceLists_ForcedPriceListIdOverride'
)
BEGIN
    PRINT '✅ SUCCESS: Foreign key removed successfully';
END
ELSE
BEGIN
    PRINT '❌ ERROR: Foreign key still exists!';
    ROLLBACK TRANSACTION;
    RAISERROR('Migration failed: FK not removed', 16, 1);
    RETURN;
END

-- Post-migration statistics
SELECT @DocumentsWithPriceList = COUNT(*) FROM DocumentHeaders WHERE PriceListId IS NOT NULL AND IsDeleted = 0;

PRINT '';
PRINT 'Post-migration stats:';
PRINT '  Documents with PriceListId: ' + CAST(@DocumentsWithPriceList AS VARCHAR);
PRINT '  Net increase: ' + CAST(@MigratedRows AS VARCHAR);

COMMIT TRANSACTION;

PRINT '';
PRINT '=== Migration completed successfully ===';
PRINT 'Summary:';
PRINT '  - Migrated records: ' + CAST(@MigratedRows AS VARCHAR);
PRINT '  - Conflicting records (logged): ' + CAST(@ConflictingDocuments AS VARCHAR);
PRINT '  - Column removed: ForcedPriceListIdOverride';
PRINT '  - FK removed: FK_DocumentHeaders_PriceLists_ForcedPriceListIdOverride';
GO
