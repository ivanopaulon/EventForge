-- =============================================
-- Phase 3: Multi-Tenancy Store Configuration
-- Script 03: Add NOT NULL Constraints and Indexes
-- Date: 2025-12-04
-- Description: Adds NOT NULL constraints on TenantId and performance indexes
-- IMPORTANT: Execute 02_Migration_Cleanup_OrphanData.sql FIRST
-- =============================================

SET NOCOUNT ON;

PRINT '========================================';
PRINT 'Phase 3: Store Multi-Tenancy Constraints';
PRINT 'Adding NOT NULL Constraints and Indexes';
PRINT '========================================';
PRINT '';

BEGIN TRY
    -- =============================================
    -- STEP 1: Verify No Orphan Records Exist
    -- =============================================
    PRINT 'STEP 1: Pre-validation - Checking for orphan records...';
    
    DECLARE @OrphanCount INT = 0;
    
    SELECT @OrphanCount = 
        (SELECT COUNT(*) FROM [dbo].[StoreUserPrivileges] WHERE TenantId IS NULL) +
        (SELECT COUNT(*) FROM [dbo].[StoreUserGroups] WHERE TenantId IS NULL) +
        (SELECT COUNT(*) FROM [dbo].[StoreUsers] WHERE TenantId IS NULL) +
        (SELECT COUNT(*) FROM [dbo].[StorePoses] WHERE TenantId IS NULL);
    
    IF @OrphanCount > 0
    BEGIN
        PRINT '   ✗ VALIDATION FAILED: ' + CAST(@OrphanCount AS VARCHAR(10)) + ' orphan records found!';
        PRINT '   Please execute 02_Migration_Cleanup_OrphanData.sql first.';
        RETURN;
    END
    
    PRINT '   ✓ VALIDATION PASSED: No orphan records found.';
    PRINT '';

    -- =============================================
    -- STEP 2: Add NOT NULL Constraint to StoreUserPrivileges
    -- =============================================
    PRINT 'STEP 2: Adding NOT NULL constraint to StoreUserPrivileges.TenantId...';
    
    -- Check if constraint already exists
    IF EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('[dbo].[StoreUserPrivileges]') 
        AND name = 'TenantId' 
        AND is_nullable = 0
    )
    BEGIN
        PRINT '   - Constraint already exists (skipping)';
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[StoreUserPrivileges]
        ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL;
        
        PRINT '   ✓ Constraint added successfully';
    END
    PRINT '';

    -- =============================================
    -- STEP 3: Add NOT NULL Constraint to StoreUserGroups
    -- =============================================
    PRINT 'STEP 3: Adding NOT NULL constraint to StoreUserGroups.TenantId...';
    
    IF EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('[dbo].[StoreUserGroups]') 
        AND name = 'TenantId' 
        AND is_nullable = 0
    )
    BEGIN
        PRINT '   - Constraint already exists (skipping)';
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[StoreUserGroups]
        ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL;
        
        PRINT '   ✓ Constraint added successfully';
    END
    PRINT '';

    -- =============================================
    -- STEP 4: Add NOT NULL Constraint to StoreUsers
    -- =============================================
    PRINT 'STEP 4: Adding NOT NULL constraint to StoreUsers.TenantId...';
    
    IF EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('[dbo].[StoreUsers]') 
        AND name = 'TenantId' 
        AND is_nullable = 0
    )
    BEGIN
        PRINT '   - Constraint already exists (skipping)';
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[StoreUsers]
        ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL;
        
        PRINT '   ✓ Constraint added successfully';
    END
    PRINT '';

    -- =============================================
    -- STEP 5: Add NOT NULL Constraint to StorePos
    -- =============================================
    PRINT 'STEP 5: Adding NOT NULL constraint to StorePoses.TenantId...';
    
    IF EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('[dbo].[StorePoses]') 
        AND name = 'TenantId' 
        AND is_nullable = 0
    )
    BEGIN
        PRINT '   - Constraint already exists (skipping)';
    END
    ELSE
    BEGIN
        ALTER TABLE [dbo].[StorePoses]
        ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL;
        
        PRINT '   ✓ Constraint added successfully';
    END
    PRINT '';

    -- =============================================
    -- STEP 6: Add Performance Index to StoreUserPrivileges
    -- =============================================
    PRINT 'STEP 6: Adding performance index to StoreUserPrivileges.TenantId...';
    
    IF EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE object_id = OBJECT_ID('[dbo].[StoreUserPrivileges]') 
        AND name = 'IX_StoreUserPrivileges_TenantId'
    )
    BEGIN
        PRINT '   - Index already exists (skipping)';
    END
    ELSE
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_StoreUserPrivileges_TenantId]
        ON [dbo].[StoreUserPrivileges] ([TenantId])
        INCLUDE ([Code], [Name], [Status], [IsActive], [IsDeleted]);
        
        PRINT '   ✓ Index created successfully';
    END
    PRINT '';

    -- =============================================
    -- STEP 7: Add Performance Index to StoreUserGroups
    -- =============================================
    PRINT 'STEP 7: Adding performance index to StoreUserGroups.TenantId...';
    
    IF EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE object_id = OBJECT_ID('[dbo].[StoreUserGroups]') 
        AND name = 'IX_StoreUserGroups_TenantId'
    )
    BEGIN
        PRINT '   - Index already exists (skipping)';
    END
    ELSE
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_StoreUserGroups_TenantId]
        ON [dbo].[StoreUserGroups] ([TenantId])
        INCLUDE ([Code], [Name], [Status], [IsActive], [IsDeleted]);
        
        PRINT '   ✓ Index created successfully';
    END
    PRINT '';

    -- =============================================
    -- STEP 8: Add Performance Index to StoreUsers
    -- =============================================
    PRINT 'STEP 8: Adding performance index to StoreUsers.TenantId...';
    
    IF EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE object_id = OBJECT_ID('[dbo].[StoreUsers]') 
        AND name = 'IX_StoreUsers_TenantId'
    )
    BEGIN
        PRINT '   - Index already exists (skipping)';
    END
    ELSE
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_StoreUsers_TenantId]
        ON [dbo].[StoreUsers] ([TenantId])
        INCLUDE ([Username], [Name], [Status], [IsActive], [IsDeleted]);
        
        PRINT '   ✓ Index created successfully';
    END
    PRINT '';

    -- =============================================
    -- STEP 9: Add Performance Index to StorePos
    -- =============================================
    PRINT 'STEP 9: Adding performance index to StorePoses.TenantId...';
    
    IF EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE object_id = OBJECT_ID('[dbo].[StorePoses]') 
        AND name = 'IX_StorePoses_TenantId'
    )
    BEGIN
        PRINT '   - Index already exists (skipping)';
    END
    ELSE
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_StorePoses_TenantId]
        ON [dbo].[StorePoses] ([TenantId])
        INCLUDE ([Name], [Status], [IsActive], [IsDeleted]);
        
        PRINT '   ✓ Index created successfully';
    END
    PRINT '';

    -- =============================================
    -- STEP 10: Add CHECK Constraint (TenantId > 0) - Optional
    -- =============================================
    PRINT 'STEP 10: Adding CHECK constraint for TenantId validation...';
    
    -- Check constraint for StoreUserPrivileges
    IF NOT EXISTS (
        SELECT 1 FROM sys.check_constraints 
        WHERE parent_object_id = OBJECT_ID('[dbo].[StoreUserPrivileges]') 
        AND name = 'CK_StoreUserPrivileges_TenantId_NotEmpty'
    )
    BEGIN
        ALTER TABLE [dbo].[StoreUserPrivileges]
        ADD CONSTRAINT [CK_StoreUserPrivileges_TenantId_NotEmpty] 
        CHECK (TenantId <> '00000000-0000-0000-0000-000000000000');
        
        PRINT '   ✓ CHECK constraint added to StoreUserPrivileges';
    END
    ELSE
    BEGIN
        PRINT '   - CHECK constraint already exists for StoreUserPrivileges';
    END
    
    -- Check constraint for StoreUserGroups
    IF NOT EXISTS (
        SELECT 1 FROM sys.check_constraints 
        WHERE parent_object_id = OBJECT_ID('[dbo].[StoreUserGroups]') 
        AND name = 'CK_StoreUserGroups_TenantId_NotEmpty'
    )
    BEGIN
        ALTER TABLE [dbo].[StoreUserGroups]
        ADD CONSTRAINT [CK_StoreUserGroups_TenantId_NotEmpty] 
        CHECK (TenantId <> '00000000-0000-0000-0000-000000000000');
        
        PRINT '   ✓ CHECK constraint added to StoreUserGroups';
    END
    ELSE
    BEGIN
        PRINT '   - CHECK constraint already exists for StoreUserGroups';
    END
    
    -- Check constraint for StoreUsers
    IF NOT EXISTS (
        SELECT 1 FROM sys.check_constraints 
        WHERE parent_object_id = OBJECT_ID('[dbo].[StoreUsers]') 
        AND name = 'CK_StoreUsers_TenantId_NotEmpty'
    )
    BEGIN
        ALTER TABLE [dbo].[StoreUsers]
        ADD CONSTRAINT [CK_StoreUsers_TenantId_NotEmpty] 
        CHECK (TenantId <> '00000000-0000-0000-0000-000000000000');
        
        PRINT '   ✓ CHECK constraint added to StoreUsers';
    END
    ELSE
    BEGIN
        PRINT '   - CHECK constraint already exists for StoreUsers';
    END
    
    -- Check constraint for StorePoses
    IF NOT EXISTS (
        SELECT 1 FROM sys.check_constraints 
        WHERE parent_object_id = OBJECT_ID('[dbo].[StorePoses]') 
        AND name = 'CK_StorePoses_TenantId_NotEmpty'
    )
    BEGIN
        ALTER TABLE [dbo].[StorePoses]
        ADD CONSTRAINT [CK_StorePoses_TenantId_NotEmpty] 
        CHECK (TenantId <> '00000000-0000-0000-0000-000000000000');
        
        PRINT '   ✓ CHECK constraint added to StorePoses';
    END
    ELSE
    BEGIN
        PRINT '   - CHECK constraint already exists for StorePoses';
    END
    PRINT '';

    -- =============================================
    -- FINAL VALIDATION
    -- =============================================
    PRINT '========================================';
    PRINT 'FINAL VALIDATION';
    PRINT '========================================';
    
    -- Verify all constraints are in place
    DECLARE @ConstraintsValid BIT = 1;
    
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('[dbo].[StoreUserPrivileges]') 
        AND name = 'TenantId' 
        AND is_nullable = 0
    )
    BEGIN
        PRINT '   ✗ StoreUserPrivileges.TenantId NOT NULL constraint missing';
        SET @ConstraintsValid = 0;
    END
    
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('[dbo].[StoreUserGroups]') 
        AND name = 'TenantId' 
        AND is_nullable = 0
    )
    BEGIN
        PRINT '   ✗ StoreUserGroups.TenantId NOT NULL constraint missing';
        SET @ConstraintsValid = 0;
    END
    
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('[dbo].[StoreUsers]') 
        AND name = 'TenantId' 
        AND is_nullable = 0
    )
    BEGIN
        PRINT '   ✗ StoreUsers.TenantId NOT NULL constraint missing';
        SET @ConstraintsValid = 0;
    END
    
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('[dbo].[StorePoses]') 
        AND name = 'TenantId' 
        AND is_nullable = 0
    )
    BEGIN
        PRINT '   ✗ StorePoses.TenantId NOT NULL constraint missing';
        SET @ConstraintsValid = 0;
    END
    
    IF @ConstraintsValid = 1
    BEGIN
        PRINT '   ✓ All NOT NULL constraints verified';
    END
    PRINT '';
    
    -- Verify indexes exist
    DECLARE @IndexesValid BIT = 1;
    
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE object_id = OBJECT_ID('[dbo].[StoreUserPrivileges]') 
        AND name = 'IX_StoreUserPrivileges_TenantId'
    )
    BEGIN
        PRINT '   ✗ Index IX_StoreUserPrivileges_TenantId missing';
        SET @IndexesValid = 0;
    END
    
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE object_id = OBJECT_ID('[dbo].[StoreUserGroups]') 
        AND name = 'IX_StoreUserGroups_TenantId'
    )
    BEGIN
        PRINT '   ✗ Index IX_StoreUserGroups_TenantId missing';
        SET @IndexesValid = 0;
    END
    
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE object_id = OBJECT_ID('[dbo].[StoreUsers]') 
        AND name = 'IX_StoreUsers_TenantId'
    )
    BEGIN
        PRINT '   ✗ Index IX_StoreUsers_TenantId missing';
        SET @IndexesValid = 0;
    END
    
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE object_id = OBJECT_ID('[dbo].[StorePoses]') 
        AND name = 'IX_StorePoses_TenantId'
    )
    BEGIN
        PRINT '   ✗ Index IX_StorePoses_TenantId missing';
        SET @IndexesValid = 0;
    END
    
    IF @IndexesValid = 1
    BEGIN
        PRINT '   ✓ All performance indexes verified';
    END
    PRINT '';

    IF @ConstraintsValid = 1 AND @IndexesValid = 1
    BEGIN
        PRINT '========================================';
        PRINT '✓ SUCCESS: All constraints and indexes applied!';
        PRINT '========================================';
        PRINT '';
        PRINT 'Store entities are now fully compliant with multi-tenancy:';
        PRINT '   • StoreUserPrivileges';
        PRINT '   • StoreUserGroups';
        PRINT '   • StoreUsers';
        PRINT '   • StorePoses';
        PRINT '';
        PRINT 'NEXT STEPS:';
        PRINT '1. Test application functionality';
        PRINT '2. Monitor query performance with new indexes';
        PRINT '3. Update application documentation';
        PRINT '4. Deploy to staging/production environments';
    END
    ELSE
    BEGIN
        PRINT '========================================';
        PRINT '⚠ WARNING: Some constraints or indexes failed';
        PRINT '========================================';
        PRINT 'Review the errors above and retry as needed.';
    END
    
    PRINT '';
    PRINT '========================================';
    PRINT 'Constraint Migration Complete';
    PRINT '========================================';

END TRY
BEGIN CATCH
    PRINT '';
    PRINT '========================================';
    PRINT '✗ ERROR OCCURRED';
    PRINT '========================================';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS VARCHAR(10));
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR(10));
    PRINT '';
    PRINT 'TROUBLESHOOTING:';
    PRINT '1. Review the error message above';
    PRINT '2. Ensure 02_Migration_Cleanup_OrphanData.sql was executed successfully';
    PRINT '3. Check for any active transactions or locks on these tables';
    PRINT '4. Contact database administrator if issue persists';
    PRINT '========================================';
    
    THROW;
END CATCH;
