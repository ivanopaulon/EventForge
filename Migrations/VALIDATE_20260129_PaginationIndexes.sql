-- =============================================
-- Validation Script: Pagination Performance Indexes
-- Purpose: Verify all indexes were created successfully
-- Run AFTER: 20260129_AddPaginationPerformanceIndexes.sql
-- =============================================

USE [EventData];
GO

PRINT '';
PRINT '=============================================';
PRINT 'VALIDATING PAGINATION PERFORMANCE INDEXES';
PRINT '=============================================';
PRINT '';

-- Check total count of indexes created
DECLARE @ExpectedCount INT = 59;
DECLARE @ActualCount INT;

SELECT @ActualCount = COUNT(*)
FROM sys.indexes
WHERE name LIKE 'IX_%TenantId%' 
   OR name LIKE 'IX_%Covering'
   OR name LIKE 'IX_ChatMessages_%'
   OR name LIKE 'IX_DocumentRows_%'
   OR name LIKE 'IX_SaleItems_%'
   OR name LIKE 'IX_Lots_%'
   OR name LIKE 'IX_StorageLocations_%'
   OR name LIKE 'IX_Models_BrandId'
   OR name LIKE 'IX_UserRoles_%'
   OR name LIKE 'IX_ClassificationNodes_ParentId';

PRINT 'Expected Indexes: ' + CAST(@ExpectedCount AS VARCHAR(10));
PRINT 'Actual Indexes:   ' + CAST(@ActualCount AS VARCHAR(10));
PRINT '';

IF @ActualCount >= @ExpectedCount
BEGIN
    PRINT '✓ SUCCESS: All expected indexes found!';
END
ELSE
BEGIN
    PRINT '✗ WARNING: Missing indexes detected!';
    PRINT '  Please review the migration output.';
END
PRINT '';

-- =============================================
-- Category 1: Core Pagination Indexes (26)
-- =============================================
PRINT 'Category 1: Core Pagination Indexes';
PRINT '------------------------------------';

SELECT 
    OBJECT_NAME(object_id) AS TableName,
    name AS IndexName,
    'Exists' AS Status
FROM sys.indexes
WHERE name IN (
    'IX_Addresses_TenantId_IsDeleted',
    'IX_Contacts_TenantId_IsDeleted',
    'IX_ClassificationNodes_TenantId_IsDeleted',
    'IX_VatRates_TenantId_IsDeleted',
    'IX_PaymentTerms_TenantId_IsDeleted',
    'IX_Banks_TenantId_IsDeleted',
    'IX_DocumentTypes_TenantId_IsDeleted',
    'IX_DocumentCounters_TenantId_IsDeleted',
    'IX_Lots_TenantId_IsDeleted',
    'IX_StorageLocations_TenantId_IsDeleted',
    'IX_Brands_TenantId_IsDeleted',
    'IX_Models_TenantId_IsDeleted',
    'IX_UMs_TenantId_IsDeleted',
    'IX_NoteFlags_TenantId_IsDeleted',
    'IX_PaymentMethods_TenantId_IsDeleted',
    'IX_SaleSessions_TenantId_IsDeleted',
    'IX_TableSessions_TenantId_IsDeleted',
    'IX_Events_TenantId_IsDeleted',
    'IX_ChatMessages_TenantId_IsDeleted',
    'IX_Notifications_TenantId_IsDeleted',
    'IX_Users_TenantId_IsDeleted',
    'IX_Tenants_IsDeleted',
    'IX_Products_TenantId_IsDeleted',
    'IX_StorageFacilities_TenantId_IsDeleted',
    'IX_DocumentHeaders_TenantId_IsDeleted',
    'IX_BusinessParties_TenantId_IsDeleted'
)
ORDER BY TableName, IndexName;

PRINT '';

-- =============================================
-- Category 2: DateTime Sorting Indexes (8)
-- =============================================
PRINT 'Category 2: DateTime Sorting Indexes';
PRINT '-------------------------------------';

SELECT 
    OBJECT_NAME(object_id) AS TableName,
    name AS IndexName,
    'Exists' AS Status
FROM sys.indexes
WHERE name IN (
    'IX_DocumentHeaders_TenantId_DocumentDate',
    'IX_Events_TenantId_EventDate',
    'IX_SaleSessions_TenantId_CreatedAt',
    'IX_AuditTrails_TenantId_Timestamp',
    'IX_LogEntries_TenantId_Timestamp',
    'IX_ChatMessages_TenantId_SentAt',
    'IX_Notifications_TenantId_CreatedAt',
    'IX_EntityChangeLogs_TenantId_Timestamp'
)
ORDER BY TableName, IndexName;

PRINT '';

-- =============================================
-- Category 3: Foreign Key Indexes (15)
-- =============================================
PRINT 'Category 3: Foreign Key Indexes';
PRINT '--------------------------------';

SELECT 
    OBJECT_NAME(object_id) AS TableName,
    name AS IndexName,
    'Exists' AS Status
FROM sys.indexes
WHERE name IN (
    'IX_Products_CategoryId',
    'IX_Products_SupplierId',
    'IX_Products_UnitOfMeasureId',
    'IX_Products_BrandId',
    'IX_Models_BrandId',
    'IX_DocumentRows_DocumentHeaderId',
    'IX_DocumentRows_ProductId',
    'IX_SaleItems_SaleSessionId',
    'IX_SaleItems_ProductId',
    'IX_Lots_ProductId',
    'IX_Lots_StorageLocationId',
    'IX_StorageLocations_StorageFacilityId',
    'IX_ClassificationNodes_ParentId',
    'IX_ChatMessages_ThreadId',
    'IX_ChatMessages_SenderId'
)
ORDER BY TableName, IndexName;

PRINT '';

-- =============================================
-- Category 4: Composite Filter Indexes (8)
-- =============================================
PRINT 'Category 4: Composite Filter Indexes';
PRINT '-------------------------------------';

SELECT 
    OBJECT_NAME(object_id) AS TableName,
    name AS IndexName,
    'Exists' AS Status
FROM sys.indexes
WHERE name IN (
    'IX_SaleSessions_TenantId_OpenSessions',
    'IX_TableSessions_TenantId_Available',
    'IX_Products_TenantId_Active',
    'IX_AuditTrails_TenantId_EntityType_Timestamp',
    'IX_AuditTrails_TenantId_UserId_Timestamp',
    'IX_LogEntries_TenantId_Level_Timestamp',
    'IX_Notifications_TenantId_Unread',
    'IX_UserRoles_RoleName_UserId'
)
ORDER BY TableName, IndexName;

PRINT '';

-- =============================================
-- Category 5: Covering Indexes (2)
-- =============================================
PRINT 'Category 5: Covering Indexes';
PRINT '-----------------------------';

SELECT 
    OBJECT_NAME(object_id) AS TableName,
    name AS IndexName,
    'Exists' AS Status
FROM sys.indexes
WHERE name IN (
    'IX_Products_Covering',
    'IX_DocumentHeaders_Covering'
)
ORDER BY TableName, IndexName;

PRINT '';

-- =============================================
-- Index Size Analysis
-- =============================================
PRINT 'Index Size Analysis';
PRINT '-------------------';

SELECT TOP 10
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    SUM(ps.used_page_count) * 8 / 1024 AS SizeMB,
    SUM(ps.row_count) AS RowCount
FROM sys.indexes i
INNER JOIN sys.dm_db_partition_stats ps 
    ON i.object_id = ps.object_id 
    AND i.index_id = ps.index_id
WHERE i.name LIKE 'IX_%'
    AND i.name LIKE '%TenantId%' OR i.name LIKE '%Covering'
GROUP BY i.object_id, i.name
ORDER BY SUM(ps.used_page_count) DESC;

PRINT '';

-- =============================================
-- Check for Missing Indexes (should be minimal)
-- =============================================
PRINT 'Missing Index Analysis';
PRINT '----------------------';

SELECT TOP 5
    ROUND(migs.avg_total_user_cost * migs.avg_user_impact * (migs.user_seeks + migs.user_scans), 0) AS improvement_measure,
    DB_NAME(mid.database_id) AS database_name,
    OBJECT_NAME(mid.object_id, mid.database_id) AS table_name,
    mid.equality_columns,
    mid.inequality_columns,
    mid.included_columns
FROM sys.dm_db_missing_index_groups mig
INNER JOIN sys.dm_db_missing_index_group_stats migs 
    ON mig.index_group_handle = migs.group_handle
INNER JOIN sys.dm_db_missing_index_details mid 
    ON mig.index_handle = mid.index_handle
WHERE migs.avg_user_impact > 50
ORDER BY improvement_measure DESC;

PRINT '';
PRINT '=============================================';
PRINT 'VALIDATION COMPLETE';
PRINT '=============================================';
PRINT '';
PRINT 'Next Steps:';
PRINT '1. Review the index counts above';
PRINT '2. Check execution plans for sample queries';
PRINT '3. Monitor performance metrics';
PRINT '4. Schedule index maintenance if needed';
PRINT '';
GO
