-- =============================================
-- Phase 3: Multi-Tenancy Store Configuration
-- Script 01: Pre-Migration Audit - Identify Orphan Records
-- Date: 2025-12-04
-- Description: Audits and identifies all records with NULL TenantId in Store tables
-- =============================================

PRINT '========================================';
PRINT 'Phase 3: Store Multi-Tenancy Audit';
PRINT 'Identifying Orphan Records (TenantId = NULL)';
PRINT '========================================';
PRINT '';

-- =============================================
-- 1. AUDIT: StoreUserPrivileges (Privileges)
-- =============================================
PRINT '1. Auditing StoreUserPrivileges...';
DECLARE @OrphanStoreUserPrivileges INT;
SELECT @OrphanStoreUserPrivileges = COUNT(*) 
FROM [dbo].[StoreUserPrivileges] 
WHERE TenantId IS NULL;

PRINT '   - Orphan records found: ' + CAST(@OrphanStoreUserPrivileges AS VARCHAR(10));

IF @OrphanStoreUserPrivileges > 0
BEGIN
    PRINT '   - Details of orphan StoreUserPrivileges:';
    SELECT TOP 10
        Id,
        Code,
        Name,
        Category,
        Status,
        CreatedAt,
        CreatedBy,
        IsSystemPrivilege
    FROM [dbo].[StoreUserPrivileges]
    WHERE TenantId IS NULL
    ORDER BY CreatedAt DESC;
END
PRINT '';

-- =============================================
-- 2. AUDIT: StoreUserGroups (Operator Groups)
-- =============================================
PRINT '2. Auditing StoreUserGroups...';
DECLARE @OrphanStoreUserGroups INT;
SELECT @OrphanStoreUserGroups = COUNT(*) 
FROM [dbo].[StoreUserGroups] 
WHERE TenantId IS NULL;

PRINT '   - Orphan records found: ' + CAST(@OrphanStoreUserGroups AS VARCHAR(10));

IF @OrphanStoreUserGroups > 0
BEGIN
    PRINT '   - Details of orphan StoreUserGroups:';
    SELECT TOP 10
        Id,
        Code,
        Name,
        Description,
        Status,
        CreatedAt,
        CreatedBy,
        IsSystemGroup,
        IsDefault
    FROM [dbo].[StoreUserGroups]
    WHERE TenantId IS NULL
    ORDER BY CreatedAt DESC;

    -- Check if any StoreUsers reference these orphan groups
    PRINT '   - StoreUsers referencing orphan groups:';
    SELECT COUNT(*) AS ReferencingUsers
    FROM [dbo].[StoreUsers] su
    INNER JOIN [dbo].[StoreUserGroups] sug ON su.CashierGroupId = sug.Id
    WHERE sug.TenantId IS NULL;
END
PRINT '';

-- =============================================
-- 3. AUDIT: StoreUsers (Operators)
-- =============================================
PRINT '3. Auditing StoreUsers...';
DECLARE @OrphanStoreUsers INT;
SELECT @OrphanStoreUsers = COUNT(*) 
FROM [dbo].[StoreUsers] 
WHERE TenantId IS NULL;

PRINT '   - Orphan records found: ' + CAST(@OrphanStoreUsers AS VARCHAR(10));

IF @OrphanStoreUsers > 0
BEGIN
    PRINT '   - Details of orphan StoreUsers:';
    SELECT TOP 10
        Id,
        Name,
        Username,
        Email,
        Role,
        Status,
        CashierGroupId,
        CreatedAt,
        CreatedBy,
        LastLoginAt
    FROM [dbo].[StoreUsers]
    WHERE TenantId IS NULL
    ORDER BY CreatedAt DESC;

    -- Check if any DocumentHeaders reference these orphan users
    PRINT '   - DocumentHeaders referencing orphan StoreUsers:';
    SELECT COUNT(*) AS ReferencingDocuments
    FROM [dbo].[DocumentHeaders] dh
    INNER JOIN [dbo].[StoreUsers] su ON dh.CashierId = su.Id
    WHERE su.TenantId IS NULL;
END
PRINT '';

-- =============================================
-- 4. AUDIT: StorePos (Point of Sale)
-- =============================================
PRINT '4. Auditing StorePos...';
DECLARE @OrphanStorePos INT;
SELECT @OrphanStorePos = COUNT(*) 
FROM [dbo].[StorePoses] 
WHERE TenantId IS NULL;

PRINT '   - Orphan records found: ' + CAST(@OrphanStorePos AS VARCHAR(10));

IF @OrphanStorePos > 0
BEGIN
    PRINT '   - Details of orphan StorePos:';
    SELECT TOP 10
        Id,
        Name,
        Description,
        Status,
        Location,
        TerminalIdentifier,
        IPAddress,
        CreatedAt,
        CreatedBy,
        LastOpenedAt
    FROM [dbo].[StorePoses]
    WHERE TenantId IS NULL
    ORDER BY CreatedAt DESC;

    -- Check if any DocumentHeaders reference these orphan POS
    PRINT '   - DocumentHeaders referencing orphan StorePos:';
    SELECT COUNT(*) AS ReferencingDocuments
    FROM [dbo].[DocumentHeaders] dh
    INNER JOIN [dbo].[StorePoses] sp ON dh.CashRegisterId = sp.Id
    WHERE sp.TenantId IS NULL;
END
PRINT '';

-- =============================================
-- 5. AUDIT: Many-to-Many Junction Table
-- =============================================
PRINT '5. Auditing StoreUserGroupStoreUserPrivilege junction table...';
DECLARE @OrphanJunctions INT;

-- Check if junction table exists
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'StoreUserGroupStoreUserPrivilege')
BEGIN
    SELECT @OrphanJunctions = COUNT(*)
    FROM [dbo].[StoreUserGroupStoreUserPrivilege] j
    WHERE EXISTS (
        SELECT 1 FROM [dbo].[StoreUserGroups] g 
        WHERE g.Id = j.GroupsId AND g.TenantId IS NULL
    )
    OR EXISTS (
        SELECT 1 FROM [dbo].[StoreUserPrivileges] p 
        WHERE p.Id = j.PrivilegesId AND p.TenantId IS NULL
    );

    PRINT '   - Junction records with orphan references: ' + CAST(@OrphanJunctions AS VARCHAR(10));
END
ELSE
BEGIN
    PRINT '   - Junction table not found (check for alternative naming)';
END
PRINT '';

-- =============================================
-- SUMMARY
-- =============================================
PRINT '========================================';
PRINT 'AUDIT SUMMARY';
PRINT '========================================';
PRINT 'Orphan Records by Entity:';
PRINT '   - StoreUserPrivileges: ' + CAST(@OrphanStoreUserPrivileges AS VARCHAR(10));
PRINT '   - StoreUserGroups: ' + CAST(@OrphanStoreUserGroups AS VARCHAR(10));
PRINT '   - StoreUsers: ' + CAST(@OrphanStoreUsers AS VARCHAR(10));
PRINT '   - StorePos: ' + CAST(@OrphanStorePos AS VARCHAR(10));
PRINT '';
PRINT 'Total Orphan Records: ' + CAST(
    @OrphanStoreUserPrivileges + 
    @OrphanStoreUserGroups + 
    @OrphanStoreUsers + 
    @OrphanStorePos AS VARCHAR(10)
);
PRINT '';

IF (@OrphanStoreUserPrivileges + @OrphanStoreUserGroups + @OrphanStoreUsers + @OrphanStorePos) = 0
BEGIN
    PRINT '✓ SUCCESS: No orphan records found. Database is clean!';
END
ELSE
BEGIN
    PRINT '⚠ WARNING: Orphan records detected. Review above details before cleanup.';
    PRINT '';
    PRINT 'RECOMMENDED ACTIONS:';
    PRINT '1. Export orphan data for analysis if needed';
    PRINT '2. Verify no critical business data will be lost';
    PRINT '3. Create full database backup';
    PRINT '4. Execute cleanup migration (02_Migration_Cleanup_OrphanData.sql)';
END

PRINT '========================================';
PRINT 'Audit Complete';
PRINT '========================================';
