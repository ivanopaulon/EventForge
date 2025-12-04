-- =============================================
-- Phase 3: Multi-Tenancy Store Configuration
-- Script 02: Cleanup Orphan Data Migration
-- Date: 2025-12-04
-- Description: Deletes all records with NULL TenantId in Store tables
-- IMPORTANT: Execute 01_PreMigration_Audit_OrphanRecords.sql FIRST
-- IMPORTANT: Create full database backup BEFORE running this script
-- =============================================

-- Enable transaction for safety
SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT '========================================';
PRINT 'Phase 3: Store Multi-Tenancy Cleanup';
PRINT 'Deleting Orphan Records (TenantId = NULL)';
PRINT '========================================';
PRINT '';
PRINT '⚠ WARNING: This operation will DELETE data!';
PRINT '⚠ Ensure you have a full database backup before proceeding.';
PRINT '';

-- =============================================
-- Start Transaction
-- =============================================
BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @DeletedPrivileges INT = 0;
    DECLARE @DeletedGroups INT = 0;
    DECLARE @DeletedUsers INT = 0;
    DECLARE @DeletedPos INT = 0;
    DECLARE @DeletedJunctions INT = 0;
    DECLARE @SetNullReferences INT = 0;

    -- =============================================
    -- STEP 1: Handle Foreign Key References in DocumentHeaders
    -- =============================================
    PRINT 'STEP 1: Nullifying foreign key references in DocumentHeaders...';
    
    -- Set CashierId to NULL where it references orphan StoreUsers
    UPDATE dh
    SET CashierId = NULL
    FROM [dbo].[DocumentHeaders] dh
    INNER JOIN [dbo].[StoreUsers] su ON dh.CashierId = su.Id
    WHERE su.TenantId IS NULL;
    
    SET @SetNullReferences = @@ROWCOUNT;
    PRINT '   - CashierId references set to NULL: ' + CAST(@SetNullReferences AS VARCHAR(10));
    
    -- Set CashRegisterId to NULL where it references orphan StorePos
    UPDATE dh
    SET CashRegisterId = NULL
    FROM [dbo].[DocumentHeaders] dh
    INNER JOIN [dbo].[StorePoses] sp ON dh.CashRegisterId = sp.Id
    WHERE sp.TenantId IS NULL;
    
    SET @SetNullReferences = @SetNullReferences + @@ROWCOUNT;
    PRINT '   - CashRegisterId references set to NULL: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
    PRINT '   - Total references nullified: ' + CAST(@SetNullReferences AS VARCHAR(10));
    PRINT '';

    -- =============================================
    -- STEP 2: Handle Foreign Key References in StoreUsers
    -- =============================================
    PRINT 'STEP 2: Nullifying CashierGroupId in StoreUsers...';
    
    -- Set CashierGroupId to NULL where it references orphan StoreUserGroups
    UPDATE su
    SET CashierGroupId = NULL
    FROM [dbo].[StoreUsers] su
    INNER JOIN [dbo].[StoreUserGroups] sug ON su.CashierGroupId = sug.Id
    WHERE sug.TenantId IS NULL;
    
    PRINT '   - CashierGroupId references set to NULL: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
    PRINT '';

    -- =============================================
    -- STEP 3: Delete Junction Table Records
    -- =============================================
    PRINT 'STEP 3: Deleting orphan junction table records...';
    
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'StoreUserGroupStoreUserPrivilege')
    BEGIN
        -- Delete junction records referencing orphan groups
        DELETE FROM [dbo].[StoreUserGroupStoreUserPrivilege]
        WHERE GroupsId IN (
            SELECT Id FROM [dbo].[StoreUserGroups] WHERE TenantId IS NULL
        );
        
        SET @DeletedJunctions = @@ROWCOUNT;
        
        -- Delete junction records referencing orphan privileges
        DELETE FROM [dbo].[StoreUserGroupStoreUserPrivilege]
        WHERE PrivilegesId IN (
            SELECT Id FROM [dbo].[StoreUserPrivileges] WHERE TenantId IS NULL
        );
        
        SET @DeletedJunctions = @DeletedJunctions + @@ROWCOUNT;
        PRINT '   - Junction records deleted: ' + CAST(@DeletedJunctions AS VARCHAR(10));
    END
    ELSE
    BEGIN
        PRINT '   - Junction table not found (skipping)';
    END
    PRINT '';

    -- =============================================
    -- STEP 4: Delete Orphan StoreUsers
    -- =============================================
    PRINT 'STEP 4: Deleting orphan StoreUsers...';
    
    DELETE FROM [dbo].[StoreUsers]
    WHERE TenantId IS NULL;
    
    SET @DeletedUsers = @@ROWCOUNT;
    PRINT '   - StoreUsers deleted: ' + CAST(@DeletedUsers AS VARCHAR(10));
    PRINT '';

    -- =============================================
    -- STEP 5: Delete Orphan StorePos
    -- =============================================
    PRINT 'STEP 5: Deleting orphan StorePos...';
    
    DELETE FROM [dbo].[StorePoses]
    WHERE TenantId IS NULL;
    
    SET @DeletedPos = @@ROWCOUNT;
    PRINT '   - StorePos deleted: ' + CAST(@DeletedPos AS VARCHAR(10));
    PRINT '';

    -- =============================================
    -- STEP 6: Delete Orphan StoreUserGroups
    -- =============================================
    PRINT 'STEP 6: Deleting orphan StoreUserGroups...';
    
    DELETE FROM [dbo].[StoreUserGroups]
    WHERE TenantId IS NULL;
    
    SET @DeletedGroups = @@ROWCOUNT;
    PRINT '   - StoreUserGroups deleted: ' + CAST(@DeletedGroups AS VARCHAR(10));
    PRINT '';

    -- =============================================
    -- STEP 7: Delete Orphan StoreUserPrivileges
    -- =============================================
    PRINT 'STEP 7: Deleting orphan StoreUserPrivileges...';
    
    DELETE FROM [dbo].[StoreUserPrivileges]
    WHERE TenantId IS NULL;
    
    SET @DeletedPrivileges = @@ROWCOUNT;
    PRINT '   - StoreUserPrivileges deleted: ' + CAST(@DeletedPrivileges AS VARCHAR(10));
    PRINT '';

    -- =============================================
    -- VALIDATION: Verify No Orphans Remain
    -- =============================================
    PRINT 'VALIDATION: Checking for remaining orphan records...';
    
    DECLARE @RemainingOrphans INT = 0;
    
    SELECT @RemainingOrphans = 
        (SELECT COUNT(*) FROM [dbo].[StoreUserPrivileges] WHERE TenantId IS NULL) +
        (SELECT COUNT(*) FROM [dbo].[StoreUserGroups] WHERE TenantId IS NULL) +
        (SELECT COUNT(*) FROM [dbo].[StoreUsers] WHERE TenantId IS NULL) +
        (SELECT COUNT(*) FROM [dbo].[StorePoses] WHERE TenantId IS NULL);
    
    IF @RemainingOrphans > 0
    BEGIN
        PRINT '   ✗ VALIDATION FAILED: ' + CAST(@RemainingOrphans AS VARCHAR(10)) + ' orphan records still exist!';
        PRINT '   Rolling back transaction...';
        ROLLBACK TRANSACTION;
        RETURN;
    END
    
    PRINT '   ✓ VALIDATION PASSED: No orphan records remain.';
    PRINT '';

    -- =============================================
    -- SUMMARY
    -- =============================================
    PRINT '========================================';
    PRINT 'CLEANUP SUMMARY';
    PRINT '========================================';
    PRINT 'Records Deleted:';
    PRINT '   - StoreUserPrivileges: ' + CAST(@DeletedPrivileges AS VARCHAR(10));
    PRINT '   - StoreUserGroups: ' + CAST(@DeletedGroups AS VARCHAR(10));
    PRINT '   - StoreUsers: ' + CAST(@DeletedUsers AS VARCHAR(10));
    PRINT '   - StorePos: ' + CAST(@DeletedPos AS VARCHAR(10));
    PRINT '   - Junction Records: ' + CAST(@DeletedJunctions AS VARCHAR(10));
    PRINT '';
    PRINT 'References Nullified:';
    PRINT '   - DocumentHeaders: ' + CAST(@SetNullReferences AS VARCHAR(10));
    PRINT '';
    PRINT 'Total Records Affected: ' + CAST(
        @DeletedPrivileges + 
        @DeletedGroups + 
        @DeletedUsers + 
        @DeletedPos + 
        @DeletedJunctions +
        @SetNullReferences AS VARCHAR(10)
    );
    PRINT '';

    -- =============================================
    -- Commit Transaction
    -- =============================================
    COMMIT TRANSACTION;
    
    PRINT '✓ SUCCESS: Transaction committed successfully!';
    PRINT '';
    PRINT 'NEXT STEPS:';
    PRINT '1. Execute 03_Migration_Add_Constraints.sql to add NOT NULL constraints';
    PRINT '2. Verify application functionality after migration';
    PRINT '3. Monitor for any issues in production';
    PRINT '';
    PRINT '========================================';
    PRINT 'Cleanup Complete';
    PRINT '========================================';

END TRY
BEGIN CATCH
    -- =============================================
    -- Error Handling
    -- =============================================
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT '';
        PRINT '========================================';
        PRINT '✗ ERROR OCCURRED - Transaction Rolled Back';
        PRINT '========================================';
        PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS VARCHAR(10));
        PRINT 'Error Message: ' + ERROR_MESSAGE();
        PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR(10));
        PRINT '';
        PRINT 'TROUBLESHOOTING:';
        PRINT '1. Review the error message above';
        PRINT '2. Check if there are additional foreign key constraints';
        PRINT '3. Ensure 01_PreMigration_Audit_OrphanRecords.sql was executed';
        PRINT '4. Contact database administrator if issue persists';
        PRINT '========================================';
    END
    
    -- Re-throw error for logging
    THROW;
END CATCH;
