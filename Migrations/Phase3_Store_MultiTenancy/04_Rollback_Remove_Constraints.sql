-- =============================================
-- Phase 3: Multi-Tenancy Store Configuration
-- Script 04: Rollback - Remove Constraints and Indexes
-- Date: 2025-12-04
-- Description: Removes NOT NULL constraints and indexes if rollback is needed
-- WARNING: This should only be used if migration needs to be rolled back
-- =============================================

SET NOCOUNT ON;

PRINT '========================================';
PRINT 'Phase 3: Store Multi-Tenancy ROLLBACK';
PRINT 'Removing Constraints and Indexes';
PRINT '========================================';
PRINT '';
PRINT '⚠ WARNING: This will rollback the multi-tenancy constraints!';
PRINT '⚠ Ensure you understand the implications before proceeding.';
PRINT '';

BEGIN TRY
    -- =============================================
    -- STEP 1: Remove CHECK Constraints
    -- =============================================
    PRINT 'STEP 1: Removing CHECK constraints...';
    
    IF EXISTS (
        SELECT 1 FROM sys.check_constraints 
        WHERE parent_object_id = OBJECT_ID('[dbo].[StoreUserPrivileges]') 
        AND name = 'CK_StoreUserPrivileges_TenantId_NotEmpty'
    )
    BEGIN
        ALTER TABLE [dbo].[StoreUserPrivileges]
        DROP CONSTRAINT [CK_StoreUserPrivileges_TenantId_NotEmpty];
        PRINT '   ✓ Removed CHECK constraint from StoreUserPrivileges';
    END
    ELSE
    BEGIN
        PRINT '   - CHECK constraint not found for StoreUserPrivileges (skipping)';
    END
    
    IF EXISTS (
        SELECT 1 FROM sys.check_constraints 
        WHERE parent_object_id = OBJECT_ID('[dbo].[StoreUserGroups]') 
        AND name = 'CK_StoreUserGroups_TenantId_NotEmpty'
    )
    BEGIN
        ALTER TABLE [dbo].[StoreUserGroups]
        DROP CONSTRAINT [CK_StoreUserGroups_TenantId_NotEmpty];
        PRINT '   ✓ Removed CHECK constraint from StoreUserGroups';
    END
    ELSE
    BEGIN
        PRINT '   - CHECK constraint not found for StoreUserGroups (skipping)';
    END
    
    IF EXISTS (
        SELECT 1 FROM sys.check_constraints 
        WHERE parent_object_id = OBJECT_ID('[dbo].[StoreUsers]') 
        AND name = 'CK_StoreUsers_TenantId_NotEmpty'
    )
    BEGIN
        ALTER TABLE [dbo].[StoreUsers]
        DROP CONSTRAINT [CK_StoreUsers_TenantId_NotEmpty];
        PRINT '   ✓ Removed CHECK constraint from StoreUsers';
    END
    ELSE
    BEGIN
        PRINT '   - CHECK constraint not found for StoreUsers (skipping)';
    END
    
    IF EXISTS (
        SELECT 1 FROM sys.check_constraints 
        WHERE parent_object_id = OBJECT_ID('[dbo].[StorePoses]') 
        AND name = 'CK_StorePoses_TenantId_NotEmpty'
    )
    BEGIN
        ALTER TABLE [dbo].[StorePoses]
        DROP CONSTRAINT [CK_StorePoses_TenantId_NotEmpty];
        PRINT '   ✓ Removed CHECK constraint from StorePoses';
    END
    ELSE
    BEGIN
        PRINT '   - CHECK constraint not found for StorePoses (skipping)';
    END
    PRINT '';

    -- =============================================
    -- STEP 2: Remove Performance Indexes
    -- =============================================
    PRINT 'STEP 2: Removing performance indexes...';
    
    IF EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE object_id = OBJECT_ID('[dbo].[StoreUserPrivileges]') 
        AND name = 'IX_StoreUserPrivileges_TenantId'
    )
    BEGIN
        DROP INDEX [IX_StoreUserPrivileges_TenantId] ON [dbo].[StoreUserPrivileges];
        PRINT '   ✓ Removed index from StoreUserPrivileges';
    END
    ELSE
    BEGIN
        PRINT '   - Index not found for StoreUserPrivileges (skipping)';
    END
    
    IF EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE object_id = OBJECT_ID('[dbo].[StoreUserGroups]') 
        AND name = 'IX_StoreUserGroups_TenantId'
    )
    BEGIN
        DROP INDEX [IX_StoreUserGroups_TenantId] ON [dbo].[StoreUserGroups];
        PRINT '   ✓ Removed index from StoreUserGroups';
    END
    ELSE
    BEGIN
        PRINT '   - Index not found for StoreUserGroups (skipping)';
    END
    
    IF EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE object_id = OBJECT_ID('[dbo].[StoreUsers]') 
        AND name = 'IX_StoreUsers_TenantId'
    )
    BEGIN
        DROP INDEX [IX_StoreUsers_TenantId] ON [dbo].[StoreUsers];
        PRINT '   ✓ Removed index from StoreUsers';
    END
    ELSE
    BEGIN
        PRINT '   - Index not found for StoreUsers (skipping)';
    END
    
    IF EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE object_id = OBJECT_ID('[dbo].[StorePoses]') 
        AND name = 'IX_StorePoses_TenantId'
    )
    BEGIN
        DROP INDEX [IX_StorePoses_TenantId] ON [dbo].[StorePoses];
        PRINT '   ✓ Removed index from StorePoses';
    END
    ELSE
    BEGIN
        PRINT '   - Index not found for StorePoses (skipping)';
    END
    PRINT '';

    -- =============================================
    -- STEP 3: Remove NOT NULL Constraints
    -- =============================================
    PRINT 'STEP 3: Removing NOT NULL constraints...';
    PRINT '⚠ Note: TenantId columns will become nullable again';
    PRINT '';
    
    -- Make TenantId nullable in StoreUserPrivileges
    IF EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('[dbo].[StoreUserPrivileges]') 
        AND name = 'TenantId' 
        AND is_nullable = 0
    )
    BEGIN
        ALTER TABLE [dbo].[StoreUserPrivileges]
        ALTER COLUMN TenantId UNIQUEIDENTIFIER NULL;
        PRINT '   ✓ Made StoreUserPrivileges.TenantId nullable';
    END
    ELSE
    BEGIN
        PRINT '   - StoreUserPrivileges.TenantId already nullable (skipping)';
    END
    
    -- Make TenantId nullable in StoreUserGroups
    IF EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('[dbo].[StoreUserGroups]') 
        AND name = 'TenantId' 
        AND is_nullable = 0
    )
    BEGIN
        ALTER TABLE [dbo].[StoreUserGroups]
        ALTER COLUMN TenantId UNIQUEIDENTIFIER NULL;
        PRINT '   ✓ Made StoreUserGroups.TenantId nullable';
    END
    ELSE
    BEGIN
        PRINT '   - StoreUserGroups.TenantId already nullable (skipping)';
    END
    
    -- Make TenantId nullable in StoreUsers
    IF EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('[dbo].[StoreUsers]') 
        AND name = 'TenantId' 
        AND is_nullable = 0
    )
    BEGIN
        ALTER TABLE [dbo].[StoreUsers]
        ALTER COLUMN TenantId UNIQUEIDENTIFIER NULL;
        PRINT '   ✓ Made StoreUsers.TenantId nullable';
    END
    ELSE
    BEGIN
        PRINT '   - StoreUsers.TenantId already nullable (skipping)';
    END
    
    -- Make TenantId nullable in StorePoses
    IF EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('[dbo].[StorePoses]') 
        AND name = 'TenantId' 
        AND is_nullable = 0
    )
    BEGIN
        ALTER TABLE [dbo].[StorePoses]
        ALTER COLUMN TenantId UNIQUEIDENTIFIER NULL;
        PRINT '   ✓ Made StorePoses.TenantId nullable';
    END
    ELSE
    BEGIN
        PRINT '   - StorePoses.TenantId already nullable (skipping)';
    END
    PRINT '';

    -- =============================================
    -- VALIDATION
    -- =============================================
    PRINT '========================================';
    PRINT 'ROLLBACK VALIDATION';
    PRINT '========================================';
    
    DECLARE @RollbackComplete BIT = 1;
    
    -- Verify all constraints removed
    IF EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('[dbo].[StoreUserPrivileges]') 
        AND name = 'TenantId' 
        AND is_nullable = 0
    )
    BEGIN
        PRINT '   ✗ StoreUserPrivileges.TenantId still has NOT NULL constraint';
        SET @RollbackComplete = 0;
    END
    
    IF EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('[dbo].[StoreUserGroups]') 
        AND name = 'TenantId' 
        AND is_nullable = 0
    )
    BEGIN
        PRINT '   ✗ StoreUserGroups.TenantId still has NOT NULL constraint';
        SET @RollbackComplete = 0;
    END
    
    IF EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('[dbo].[StoreUsers]') 
        AND name = 'TenantId' 
        AND is_nullable = 0
    )
    BEGIN
        PRINT '   ✗ StoreUsers.TenantId still has NOT NULL constraint';
        SET @RollbackComplete = 0;
    END
    
    IF EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('[dbo].[StorePoses]') 
        AND name = 'TenantId' 
        AND is_nullable = 0
    )
    BEGIN
        PRINT '   ✗ StorePoses.TenantId still has NOT NULL constraint';
        SET @RollbackComplete = 0;
    END
    
    IF @RollbackComplete = 1
    BEGIN
        PRINT '   ✓ All constraints successfully removed';
        PRINT '';
        PRINT '========================================';
        PRINT '✓ ROLLBACK COMPLETE';
        PRINT '========================================';
        PRINT '';
        PRINT 'WARNING: Database is now in pre-migration state.';
        PRINT 'TenantId columns are nullable again and can accept NULL values.';
        PRINT '';
        PRINT 'NEXT STEPS:';
        PRINT '1. Restore data from backup if needed';
        PRINT '2. Investigate why rollback was necessary';
        PRINT '3. Fix any issues before re-attempting migration';
    END
    ELSE
    BEGIN
        PRINT '';
        PRINT '========================================';
        PRINT '⚠ WARNING: Rollback incomplete';
        PRINT '========================================';
        PRINT 'Some constraints could not be removed. Review errors above.';
    END
    
    PRINT '';
    PRINT '========================================';
    PRINT 'Rollback Script Complete';
    PRINT '========================================';

END TRY
BEGIN CATCH
    PRINT '';
    PRINT '========================================';
    PRINT '✗ ERROR OCCURRED DURING ROLLBACK';
    PRINT '========================================';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS VARCHAR(10));
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR(10));
    PRINT '';
    PRINT 'TROUBLESHOOTING:';
    PRINT '1. Review the error message above';
    PRINT '2. Check for active transactions or locks';
    PRINT '3. Manually remove constraints if needed';
    PRINT '4. Contact database administrator';
    PRINT '========================================';
    
    THROW;
END CATCH;
