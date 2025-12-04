# Phase 3: Database Cleanup & Migration Multi-Tenancy - Store Configuration

## 📋 Overview

This migration completes the multi-tenancy refactoring for Store entities by:
1. Identifying and auditing orphan records (TenantId = NULL)
2. Cleaning up orphan data and their references
3. Adding NOT NULL constraints to TenantId columns
4. Adding performance indexes for tenant-filtered queries
5. Providing rollback capability if needed

## 🎯 Scope

### Store Entities Covered:
- **StoreUserPrivileges** - Operator privileges/permissions
- **StoreUserGroups** - Operator groups
- **StoreUsers** - Operators/Cashiers
- **StorePos** - Point of Sale terminals

### Related Tables Affected:
- **DocumentHeaders** - CashierId and CashRegisterId references
- **StoreUserGroupStoreUserPrivilege** - Many-to-many junction table

## 📁 Migration Scripts

### 01_PreMigration_Audit_OrphanRecords.sql
**Purpose**: Identifies all orphan records before cleanup  
**Safe to run**: ✅ Yes - Read-only, no data changes  
**Run when**: Before any migration to assess impact  

**What it does**:
- Counts orphan records in each Store table
- Shows sample orphan data for review
- Identifies dependent records (DocumentHeaders referencing orphans)
- Provides summary report

**Example output**:
```
========================================
AUDIT SUMMARY
========================================
Orphan Records by Entity:
   - StoreUserPrivileges: 0
   - StoreUserGroups: 2
   - StoreUsers: 5
   - StorePos: 1

Total Orphan Records: 8
```

### 02_Migration_Cleanup_OrphanData.sql
**Purpose**: Deletes all orphan records  
**Safe to run**: ⚠️ **NO - Deletes data permanently**  
**Run when**: After audit and backup  

**What it does**:
1. Nullifies foreign key references in DocumentHeaders
2. Nullifies CashierGroupId in StoreUsers referencing orphan groups
3. Deletes junction table records with orphan references
4. Deletes orphan StoreUsers
5. Deletes orphan StorePos
6. Deletes orphan StoreUserGroups
7. Deletes orphan StoreUserPrivileges
8. Validates no orphans remain
9. Commits transaction if successful

**Transaction safety**: All operations are wrapped in a transaction that rolls back on error.

### 03_Migration_Add_Constraints.sql
**Purpose**: Adds NOT NULL constraints and performance indexes  
**Safe to run**: ✅ Yes - But only after cleanup  
**Run when**: After successful cleanup migration  

**What it does**:
1. Pre-validates no orphan records exist
2. Adds NOT NULL constraint to TenantId in all Store tables
3. Creates performance indexes on TenantId columns
4. Adds CHECK constraints to prevent empty GUIDs
5. Validates all constraints are in place

**Indexes created**:
- `IX_StoreUserPrivileges_TenantId` with INCLUDE columns
- `IX_StoreUserGroups_TenantId` with INCLUDE columns
- `IX_StoreUsers_TenantId` with INCLUDE columns
- `IX_StorePoses_TenantId` with INCLUDE columns

### 04_Rollback_Remove_Constraints.sql
**Purpose**: Removes constraints if rollback is needed  
**Safe to run**: ⚠️ **Use only for rollback**  
**Run when**: Only if migration needs to be rolled back  

**What it does**:
1. Removes CHECK constraints
2. Removes performance indexes
3. Makes TenantId columns nullable again
4. Validates rollback completion

⚠️ **Note**: This does NOT restore deleted data. Use database backup for data restoration.

## 🚀 Execution Guide

### Prerequisites

1. **Phase 1 and Phase 2 completed**: Backend and frontend multi-tenancy refactoring
2. **SQL Server Management Studio** or similar tool
3. **Database Administrator access**
4. **Full database backup** before starting
5. **Maintenance window** (estimated 15-30 minutes depending on data volume)

### Step-by-Step Execution

#### Step 1: Pre-Migration Assessment

1. Create a full database backup:
```sql
BACKUP DATABASE [EventData] 
TO DISK = 'C:\Backups\EventData_PrePhase3_20251204.bak'
WITH COMPRESSION, INIT;
```

2. Run audit script:
```sql
-- Execute in SSMS
USE [EventData];
GO

-- Run the audit
-- File: 01_PreMigration_Audit_OrphanRecords.sql
```

3. Review audit results:
   - Note the number of orphan records
   - Check if any critical data will be lost
   - Verify DocumentHeaders references (will be set to NULL)
   - Export orphan data if manual review is needed

4. **DECISION POINT**: 
   - If orphan count is 0: Skip to Step 3 (Constraint Addition)
   - If orphan records exist: Continue to Step 2

#### Step 2: Cleanup Orphan Data

⚠️ **CRITICAL**: This step deletes data permanently!

1. **Double-check backup exists and is valid**

2. **Optional**: Export orphan data for archival:
```sql
-- Export orphan StoreUsers to CSV
SELECT * FROM [dbo].[StoreUsers] 
WHERE TenantId IS NULL;

-- Export orphan StoreUserGroups to CSV
SELECT * FROM [dbo].[StoreUserGroups] 
WHERE TenantId IS NULL;

-- Export orphan StorePos to CSV
SELECT * FROM [dbo].[StorePoses] 
WHERE TenantId IS NULL;

-- Export orphan StoreUserPrivileges to CSV
SELECT * FROM [dbo].[StoreUserPrivileges] 
WHERE TenantId IS NULL;
```

3. Run cleanup script:
```sql
-- Execute in SSMS
USE [EventData];
GO

-- Run the cleanup (THIS DELETES DATA!)
-- File: 02_Migration_Cleanup_OrphanData.sql
```

4. Verify cleanup success:
   - Check for "SUCCESS: Transaction committed successfully!"
   - Review summary of deleted records
   - If error occurs, transaction is automatically rolled back

5. **Re-run audit** to confirm zero orphans:
```sql
-- File: 01_PreMigration_Audit_OrphanRecords.sql
```

#### Step 3: Add Constraints and Indexes

1. Run constraint script:
```sql
-- Execute in SSMS
USE [EventData];
GO

-- Add constraints and indexes
-- File: 03_Migration_Add_Constraints.sql
```

2. Verify success:
   - Check for "SUCCESS: All constraints and indexes applied!"
   - Confirm all 4 entities have NOT NULL constraints
   - Confirm all 4 performance indexes created

3. Test application:
   - Start application server
   - Verify Store management pages work
   - Test creating new operators, groups, and POS
   - Verify TenantId is automatically set

#### Step 4: Post-Migration Validation

1. **Application smoke tests**:
   - Login as tenant user
   - Navigate to Store Management
   - Create a new StoreUser → Should succeed with TenantId set
   - Create a new StoreUserGroup → Should succeed with TenantId set
   - Create a new StorePos → Should succeed with TenantId set
   - Attempt to query Store entities → Should only see tenant's data

2. **Database validation queries**:
```sql
-- Verify no NULL TenantIds exist
SELECT 
    (SELECT COUNT(*) FROM [dbo].[StoreUserPrivileges] WHERE TenantId IS NULL) AS OrphanPrivileges,
    (SELECT COUNT(*) FROM [dbo].[StoreUserGroups] WHERE TenantId IS NULL) AS OrphanGroups,
    (SELECT COUNT(*) FROM [dbo].[StoreUsers] WHERE TenantId IS NULL) AS OrphanUsers,
    (SELECT COUNT(*) FROM [dbo].[StorePoses] WHERE TenantId IS NULL) AS OrphanPos;

-- Should return all zeros

-- Verify indexes exist
SELECT 
    t.name AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE i.name LIKE 'IX_Store%_TenantId'
ORDER BY t.name;

-- Should return 4 indexes

-- Test query performance with new indexes
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

DECLARE @TestTenantId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM Tenants WHERE Id <> '00000000-0000-0000-0000-000000000000');

SELECT * FROM [dbo].[StoreUsers] WHERE TenantId = @TestTenantId;
SELECT * FROM [dbo].[StoreUserGroups] WHERE TenantId = @TestTenantId;
SELECT * FROM [dbo].[StorePoses] WHERE TenantId = @TestTenantId;
SELECT * FROM [dbo].[StoreUserPrivileges] WHERE TenantId = @TestTenantId;

SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
```

3. **Monitor for issues**:
   - Check application logs for errors
   - Monitor query performance
   - Verify no data integrity issues

## 🔄 Rollback Procedure

### When to Rollback

Consider rollback if:
- Application errors occur after migration
- Data integrity issues discovered
- Performance degradation observed
- Business requirements change

### Rollback Steps

⚠️ **WARNING**: Rollback does NOT restore deleted data. Use database backup for that.

1. **Stop the application** to prevent new data

2. **Restore database from backup** (if data needs to be restored):
```sql
-- Stop database access
ALTER DATABASE [EventData] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

-- Restore from backup
RESTORE DATABASE [EventData] 
FROM DISK = 'C:\Backups\EventData_PrePhase3_20251204.bak'
WITH REPLACE;

-- Resume access
ALTER DATABASE [EventData] SET MULTI_USER;
```

3. **OR run rollback script** (if only removing constraints):
```sql
-- Execute in SSMS
USE [EventData];
GO

-- Remove constraints only (does not restore data)
-- File: 04_Rollback_Remove_Constraints.sql
```

4. **Verify rollback**:
```sql
-- Check TenantId is nullable again
SELECT 
    c.name AS ColumnName,
    t.name AS TableName,
    c.is_nullable
FROM sys.columns c
INNER JOIN sys.tables t ON c.object_id = t.object_id
WHERE t.name IN ('StoreUsers', 'StoreUserGroups', 'StorePoses', 'StoreUserPrivileges')
AND c.name = 'TenantId';

-- is_nullable should be 1 (true) after rollback
```

5. **Restart application** and verify functionality

6. **Investigate root cause** before re-attempting migration

## ⚠️ Important Notes

### Data Loss Warning

- **02_Migration_Cleanup_OrphanData.sql** permanently deletes orphan records
- Once deleted, data can only be restored from backup
- Ensure backup is valid before running cleanup
- Export orphan data if manual review is needed

### Foreign Key References

The cleanup script handles these references automatically:
- `DocumentHeaders.CashierId` → Set to NULL if referencing orphan StoreUser
- `DocumentHeaders.CashRegisterId` → Set to NULL if referencing orphan StorePos
- `StoreUsers.CashierGroupId` → Set to NULL if referencing orphan StoreUserGroup

### Transaction Safety

- Cleanup script uses transaction with XACT_ABORT ON
- Any error causes automatic rollback
- All-or-nothing: either all cleanup succeeds or none

### Performance Impact

- Index creation may take a few seconds per table
- Minimal impact on running application
- Indexes improve query performance for tenant-filtered queries

## ✅ Success Criteria

Migration is successful when:

1. ✅ **Zero orphan records**: All Store entities have valid TenantId
2. ✅ **Constraints active**: NOT NULL constraints on all TenantId columns
3. ✅ **Indexes created**: Performance indexes on all TenantId columns
4. ✅ **Application functional**: Store management works without errors
5. ✅ **Data integrity**: No broken foreign key references
6. ✅ **Performance**: Query performance meets expectations

## 📊 Business Impact

### Security
- **Enforced tenant isolation**: Impossible to create/maintain data without tenant
- **Data integrity**: Database-level enforcement prevents orphan records

### Compliance
- **Multi-tenancy guaranteed**: Data separation at database level
- **Audit trail**: All changes logged in cleanup scripts

### Performance
- **Faster queries**: Indexes optimize tenant-filtered queries
- **Better scalability**: Efficient data access patterns

### Maintainability
- **Clean codebase**: Consistent multi-tenant implementation
- **Reduced bugs**: Database constraints prevent data issues

## 🔗 Related Documentation

- **Phase 1**: Backend multi-tenancy refactoring (COMPLETED)
- **Phase 2**: Frontend multi-tenancy fixes (COMPLETED - PR #792)
- **Phase 3**: This migration (DATABASE CLEANUP)

## 📞 Support

For issues or questions:
1. Check troubleshooting sections in each script
2. Review application logs for errors
3. Verify prerequisites are met
4. Contact database administrator if needed

## 📝 Change Log

| Date | Version | Changes |
|------|---------|---------|
| 2025-12-04 | 1.0 | Initial migration scripts and documentation |
