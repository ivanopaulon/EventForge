-- =============================================
-- Phase 3: Multi-Tenancy Store Configuration
-- Script 05: Post-Migration Validation
-- Date: 2025-12-04
-- Description: Comprehensive validation of migration success
-- Safe to run: YES - Read-only validation queries
-- =============================================

SET NOCOUNT ON;

PRINT '========================================';
PRINT 'Phase 3: Post-Migration Validation';
PRINT 'Verifying Migration Success';
PRINT '========================================';
PRINT '';

DECLARE @ValidationErrors INT = 0;

-- =============================================
-- TEST 1: Verify No Orphan Records Exist
-- =============================================
PRINT 'TEST 1: Checking for orphan records...';

DECLARE @OrphanPrivileges INT, @OrphanGroups INT, @OrphanUsers INT, @OrphanPos INT;

SELECT @OrphanPrivileges = COUNT(*) FROM [dbo].[StoreUserPrivileges] WHERE TenantId IS NULL;
SELECT @OrphanGroups = COUNT(*) FROM [dbo].[StoreUserGroups] WHERE TenantId IS NULL;
SELECT @OrphanUsers = COUNT(*) FROM [dbo].[StoreUsers] WHERE TenantId IS NULL;
SELECT @OrphanPos = COUNT(*) FROM [dbo].[StorePoses] WHERE TenantId IS NULL;

IF (@OrphanPrivileges + @OrphanGroups + @OrphanUsers + @OrphanPos) = 0
BEGIN
    PRINT '   ✓ PASS: No orphan records found';
END
ELSE
BEGIN
    PRINT '   ✗ FAIL: Orphan records still exist!';
    PRINT '      - StoreUserPrivileges: ' + CAST(@OrphanPrivileges AS VARCHAR(10));
    PRINT '      - StoreUserGroups: ' + CAST(@OrphanGroups AS VARCHAR(10));
    PRINT '      - StoreUsers: ' + CAST(@OrphanUsers AS VARCHAR(10));
    PRINT '      - StorePoses: ' + CAST(@OrphanPos AS VARCHAR(10));
    SET @ValidationErrors = @ValidationErrors + 1;
END
PRINT '';

-- =============================================
-- TEST 2: Verify NOT NULL Constraints
-- =============================================
PRINT 'TEST 2: Checking NOT NULL constraints...';

DECLARE @ConstraintCount INT = 0;

SELECT @ConstraintCount = COUNT(*)
FROM sys.columns c
INNER JOIN sys.tables t ON c.object_id = t.object_id
WHERE t.name IN ('StoreUserPrivileges', 'StoreUserGroups', 'StoreUsers', 'StorePoses')
AND c.name = 'TenantId'
AND c.is_nullable = 0;

IF @ConstraintCount = 4
BEGIN
    PRINT '   ✓ PASS: All 4 TenantId columns have NOT NULL constraint';
END
ELSE
BEGIN
    PRINT '   ✗ FAIL: Only ' + CAST(@ConstraintCount AS VARCHAR(10)) + ' of 4 constraints found';
    
    -- Show which are missing
    SELECT 
        t.name AS TableName,
        c.name AS ColumnName,
        CASE WHEN c.is_nullable = 1 THEN 'NULLABLE (MISSING CONSTRAINT)' ELSE 'NOT NULL' END AS Status
    FROM sys.columns c
    INNER JOIN sys.tables t ON c.object_id = t.object_id
    WHERE t.name IN ('StoreUserPrivileges', 'StoreUserGroups', 'StoreUsers', 'StorePoses')
    AND c.name = 'TenantId';
    
    SET @ValidationErrors = @ValidationErrors + 1;
END
PRINT '';

-- =============================================
-- TEST 3: Verify Performance Indexes
-- =============================================
PRINT 'TEST 3: Checking performance indexes...';

DECLARE @IndexCount INT = 0;

SELECT @IndexCount = COUNT(*)
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name IN ('StoreUserPrivileges', 'StoreUserGroups', 'StoreUsers', 'StorePoses')
AND i.name LIKE 'IX_%_TenantId';

IF @IndexCount = 4
BEGIN
    PRINT '   ✓ PASS: All 4 TenantId indexes exist';
    
    -- Show index details
    SELECT 
        t.name AS TableName,
        i.name AS IndexName,
        i.type_desc AS IndexType,
        STUFF((
            SELECT ', ' + c.name
            FROM sys.index_columns ic
            INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
            WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
            AND ic.is_included_column = 1
            FOR XML PATH('')
        ), 1, 2, '') AS IncludedColumns
    FROM sys.indexes i
    INNER JOIN sys.tables t ON i.object_id = t.object_id
    WHERE t.name IN ('StoreUserPrivileges', 'StoreUserGroups', 'StoreUsers', 'StorePoses')
    AND i.name LIKE 'IX_%_TenantId';
END
ELSE
BEGIN
    PRINT '   ✗ FAIL: Only ' + CAST(@IndexCount AS VARCHAR(10)) + ' of 4 indexes found';
    SET @ValidationErrors = @ValidationErrors + 1;
END
PRINT '';

-- =============================================
-- TEST 4: Verify CHECK Constraints
-- =============================================
PRINT 'TEST 4: Checking CHECK constraints (optional)...';

DECLARE @CheckConstraintCount INT = 0;

SELECT @CheckConstraintCount = COUNT(*)
FROM sys.check_constraints cc
INNER JOIN sys.tables t ON cc.parent_object_id = t.object_id
WHERE t.name IN ('StoreUserPrivileges', 'StoreUserGroups', 'StoreUsers', 'StorePoses')
AND cc.name LIKE 'CK_%_TenantId_NotEmpty';

IF @CheckConstraintCount = 4
BEGIN
    PRINT '   ✓ PASS: All 4 CHECK constraints exist';
END
ELSE IF @CheckConstraintCount = 0
BEGIN
    PRINT '   ⚠ INFO: No CHECK constraints found (optional feature)';
END
ELSE
BEGIN
    PRINT '   ⚠ WARNING: Only ' + CAST(@CheckConstraintCount AS VARCHAR(10)) + ' of 4 CHECK constraints found';
END
PRINT '';

-- =============================================
-- TEST 5: Verify Foreign Key Integrity
-- =============================================
PRINT 'TEST 5: Checking foreign key integrity...';

-- Check DocumentHeaders references
DECLARE @InvalidCashierRefs INT = 0;
DECLARE @InvalidPosRefs INT = 0;
DECLARE @InvalidGroupRefs INT = 0;

-- Invalid CashierId references
SELECT @InvalidCashierRefs = COUNT(*)
FROM [dbo].[DocumentHeaders] dh
WHERE dh.CashierId IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM [dbo].[StoreUsers] su WHERE su.Id = dh.CashierId);

-- Invalid CashRegisterId references
SELECT @InvalidPosRefs = COUNT(*)
FROM [dbo].[DocumentHeaders] dh
WHERE dh.CashRegisterId IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM [dbo].[StorePoses] sp WHERE sp.Id = dh.CashRegisterId);

-- Invalid CashierGroupId references
SELECT @InvalidGroupRefs = COUNT(*)
FROM [dbo].[StoreUsers] su
WHERE su.CashierGroupId IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM [dbo].[StoreUserGroups] sug WHERE sug.Id = su.CashierGroupId);

IF (@InvalidCashierRefs + @InvalidPosRefs + @InvalidGroupRefs) = 0
BEGIN
    PRINT '   ✓ PASS: All foreign key references are valid';
END
ELSE
BEGIN
    PRINT '   ✗ FAIL: Invalid foreign key references found!';
    PRINT '      - Invalid CashierId in DocumentHeaders: ' + CAST(@InvalidCashierRefs AS VARCHAR(10));
    PRINT '      - Invalid CashRegisterId in DocumentHeaders: ' + CAST(@InvalidPosRefs AS VARCHAR(10));
    PRINT '      - Invalid CashierGroupId in StoreUsers: ' + CAST(@InvalidGroupRefs AS VARCHAR(10));
    SET @ValidationErrors = @ValidationErrors + 1;
END
PRINT '';

-- =============================================
-- TEST 6: Verify Data Distribution by Tenant
-- =============================================
PRINT 'TEST 6: Analyzing data distribution by tenant...';

PRINT '   StoreUserPrivileges by Tenant:';
SELECT 
    COALESCE(CAST(TenantId AS VARCHAR(36)), 'NULL') AS TenantId,
    COUNT(*) AS RecordCount
FROM [dbo].[StoreUserPrivileges]
GROUP BY TenantId
ORDER BY COUNT(*) DESC;

PRINT '';
PRINT '   StoreUserGroups by Tenant:';
SELECT 
    COALESCE(CAST(TenantId AS VARCHAR(36)), 'NULL') AS TenantId,
    COUNT(*) AS RecordCount
FROM [dbo].[StoreUserGroups]
GROUP BY TenantId
ORDER BY COUNT(*) DESC;

PRINT '';
PRINT '   StoreUsers by Tenant:';
SELECT 
    COALESCE(CAST(TenantId AS VARCHAR(36)), 'NULL') AS TenantId,
    COUNT(*) AS RecordCount
FROM [dbo].[StoreUsers]
GROUP BY TenantId
ORDER BY COUNT(*) DESC;

PRINT '';
PRINT '   StorePoses by Tenant:';
SELECT 
    COALESCE(CAST(TenantId AS VARCHAR(36)), 'NULL') AS TenantId,
    COUNT(*) AS RecordCount
FROM [dbo].[StorePoses]
GROUP BY TenantId
ORDER BY COUNT(*) DESC;

PRINT '';

-- =============================================
-- TEST 7: Test Query Performance
-- =============================================
PRINT 'TEST 7: Testing query performance with indexes...';

-- Get a sample tenant ID
DECLARE @SampleTenantId UNIQUEIDENTIFIER;
SELECT TOP 1 @SampleTenantId = Id 
FROM [dbo].[Tenants] 
WHERE Id <> '00000000-0000-0000-0000-000000000000'
ORDER BY NEWID();

IF @SampleTenantId IS NOT NULL
BEGIN
    PRINT '   Testing with TenantId: ' + CAST(@SampleTenantId AS VARCHAR(36));
    
    DECLARE @StartTime DATETIME2, @EndTime DATETIME2, @Duration INT;
    
    -- Test StoreUsers query
    SET @StartTime = SYSDATETIME();
    SELECT COUNT(*) FROM [dbo].[StoreUsers] WHERE TenantId = @SampleTenantId;
    SET @EndTime = SYSDATETIME();
    SET @Duration = DATEDIFF(MILLISECOND, @StartTime, @EndTime);
    PRINT '   - StoreUsers query: ' + CAST(@Duration AS VARCHAR(10)) + ' ms';
    
    -- Test StoreUserGroups query
    SET @StartTime = SYSDATETIME();
    SELECT COUNT(*) FROM [dbo].[StoreUserGroups] WHERE TenantId = @SampleTenantId;
    SET @EndTime = SYSDATETIME();
    SET @Duration = DATEDIFF(MILLISECOND, @StartTime, @EndTime);
    PRINT '   - StoreUserGroups query: ' + CAST(@Duration AS VARCHAR(10)) + ' ms';
    
    -- Test StorePoses query
    SET @StartTime = SYSDATETIME();
    SELECT COUNT(*) FROM [dbo].[StorePoses] WHERE TenantId = @SampleTenantId;
    SET @EndTime = SYSDATETIME();
    SET @Duration = DATEDIFF(MILLISECOND, @StartTime, @EndTime);
    PRINT '   - StorePoses query: ' + CAST(@Duration AS VARCHAR(10)) + ' ms';
    
    -- Test StoreUserPrivileges query
    SET @StartTime = SYSDATETIME();
    SELECT COUNT(*) FROM [dbo].[StoreUserPrivileges] WHERE TenantId = @SampleTenantId;
    SET @EndTime = SYSDATETIME();
    SET @Duration = DATEDIFF(MILLISECOND, @StartTime, @EndTime);
    PRINT '   - StoreUserPrivileges query: ' + CAST(@Duration AS VARCHAR(10)) + ' ms';
    
    PRINT '   ✓ Performance test completed';
END
ELSE
BEGIN
    PRINT '   ⚠ SKIP: No tenant data found for performance testing';
END
PRINT '';

-- =============================================
-- TEST 8: Verify Record Counts
-- =============================================
PRINT 'TEST 8: Record count summary...';

DECLARE @TotalPrivileges INT, @TotalGroups INT, @TotalUsers INT, @TotalPos INT;

SELECT @TotalPrivileges = COUNT(*) FROM [dbo].[StoreUserPrivileges];
SELECT @TotalGroups = COUNT(*) FROM [dbo].[StoreUserGroups];
SELECT @TotalUsers = COUNT(*) FROM [dbo].[StoreUsers];
SELECT @TotalPos = COUNT(*) FROM [dbo].[StorePoses];

PRINT '   Total Records:';
PRINT '      - StoreUserPrivileges: ' + CAST(@TotalPrivileges AS VARCHAR(10));
PRINT '      - StoreUserGroups: ' + CAST(@TotalGroups AS VARCHAR(10));
PRINT '      - StoreUsers: ' + CAST(@TotalUsers AS VARCHAR(10));
PRINT '      - StorePoses: ' + CAST(@TotalPos AS VARCHAR(10));
PRINT '      - Total: ' + CAST(@TotalPrivileges + @TotalGroups + @TotalUsers + @TotalPos AS VARCHAR(10));
PRINT '';

-- =============================================
-- FINAL SUMMARY
-- =============================================
PRINT '========================================';
PRINT 'VALIDATION SUMMARY';
PRINT '========================================';

IF @ValidationErrors = 0
BEGIN
    PRINT '';
    PRINT '✓✓✓ SUCCESS: ALL VALIDATION TESTS PASSED! ✓✓✓';
    PRINT '';
    PRINT 'Migration completed successfully:';
    PRINT '   • Zero orphan records';
    PRINT '   • All NOT NULL constraints active';
    PRINT '   • All performance indexes created';
    PRINT '   • Foreign key integrity maintained';
    PRINT '   • Data properly distributed by tenant';
    PRINT '';
    PRINT 'Store entities are now fully compliant with multi-tenancy!';
    PRINT '';
    PRINT 'NEXT STEPS:';
    PRINT '1. ✓ Test application functionality';
    PRINT '2. ✓ Monitor query performance';
    PRINT '3. ✓ Update operational documentation';
    PRINT '4. ✓ Deploy to production if staging tests pass';
END
ELSE
BEGIN
    PRINT '';
    PRINT '✗✗✗ VALIDATION FAILED ✗✗✗';
    PRINT '';
    PRINT 'Number of failed tests: ' + CAST(@ValidationErrors AS VARCHAR(10));
    PRINT '';
    PRINT 'REQUIRED ACTIONS:';
    PRINT '1. Review failed tests above';
    PRINT '2. Re-run appropriate migration scripts';
    PRINT '3. Contact database administrator if issues persist';
    PRINT '4. DO NOT deploy to production until all tests pass';
END

PRINT '';
PRINT '========================================';
PRINT 'Validation Complete';
PRINT '========================================';
