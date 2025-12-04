# Phase 3 Migration - Quick Start Guide

## 🚀 5-Minute Migration Guide

### Prerequisites Checklist
- [ ] SQL Server Management Studio installed
- [ ] Database Administrator access
- [ ] Application can be stopped for maintenance
- [ ] Backup location has sufficient space

### Execution Steps

#### 1️⃣ Backup (5 minutes)
```sql
USE master;
GO

BACKUP DATABASE [EventData] 
TO DISK = 'C:\Backups\EventData_PrePhase3_' + CONVERT(VARCHAR(8), GETDATE(), 112) + '.bak'
WITH COMPRESSION, INIT;
GO

-- Verify backup
RESTORE VERIFYONLY 
FROM DISK = 'C:\Backups\EventData_PrePhase3_' + CONVERT(VARCHAR(8), GETDATE(), 112) + '.bak';
```

#### 2️⃣ Audit (2 minutes)
```sql
USE [EventData];
GO

-- Run: 01_PreMigration_Audit_OrphanRecords.sql
-- Review output - note number of orphan records
```

**Decision Point:**
- If **0 orphans**: Skip step 3, go to step 4
- If **orphans found**: Continue to step 3

#### 3️⃣ Cleanup (5 minutes) - ⚠️ DELETES DATA
```sql
USE [EventData];
GO

-- Run: 02_Migration_Cleanup_OrphanData.sql
-- Wait for "SUCCESS: Transaction committed"
```

#### 4️⃣ Add Constraints (3 minutes)
```sql
USE [EventData];
GO

-- Run: 03_Migration_Add_Constraints.sql
-- Wait for "SUCCESS: All constraints and indexes applied"
```

#### 5️⃣ Validate (2 minutes)
```sql
USE [EventData];
GO

-- Run: 05_PostMigration_Validation.sql
-- Look for "SUCCESS: ALL VALIDATION TESTS PASSED"
```

#### 6️⃣ Test Application (5 minutes)
- Start application
- Login as tenant user
- Go to Store Management
- Test creating StoreUser, StoreUserGroup, StorePos
- Verify operations work correctly

### Total Time: ~20-25 minutes

---

## 🔴 If Something Goes Wrong

### Option A: Rollback Constraints Only
```sql
USE [EventData];
GO

-- Run: 04_Rollback_Remove_Constraints.sql
-- Removes constraints but keeps existing data
```

### Option B: Full Database Restore
```sql
USE master;
GO

-- Stop database access
ALTER DATABASE [EventData] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

-- Restore from backup
RESTORE DATABASE [EventData] 
FROM DISK = 'C:\Backups\EventData_PrePhase3_YYYYMMDD.bak'
WITH REPLACE;

-- Resume access
ALTER DATABASE [EventData] SET MULTI_USER;
```

---

## 📊 Quick Validation Queries

### Check for orphan records
```sql
SELECT 
    'StoreUserPrivileges' AS Entity, COUNT(*) AS Orphans
FROM [dbo].[StoreUserPrivileges] 
WHERE TenantId IS NULL
UNION ALL
SELECT 'StoreUserGroups', COUNT(*) FROM [dbo].[StoreUserGroups] WHERE TenantId IS NULL
UNION ALL
SELECT 'StoreUsers', COUNT(*) FROM [dbo].[StoreUsers] WHERE TenantId IS NULL
UNION ALL
SELECT 'StorePoses', COUNT(*) FROM [dbo].[StorePoses] WHERE TenantId IS NULL;
-- All should return 0
```

### Check constraints
```sql
SELECT 
    t.name AS TableName,
    c.name AS ColumnName,
    CASE WHEN c.is_nullable = 0 THEN 'NOT NULL ✓' ELSE 'NULLABLE ✗' END AS Status
FROM sys.columns c
INNER JOIN sys.tables t ON c.object_id = t.object_id
WHERE t.name IN ('StoreUserPrivileges', 'StoreUserGroups', 'StoreUsers', 'StorePoses')
AND c.name = 'TenantId';
-- All should show 'NOT NULL ✓'
```

### Check indexes
```sql
SELECT 
    t.name AS TableName,
    i.name AS IndexName
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name IN ('StoreUserPrivileges', 'StoreUserGroups', 'StoreUsers', 'StorePoses')
AND i.name LIKE 'IX_%_TenantId';
-- Should return 4 rows
```

---

## 📞 Support Contacts

| Issue Type | Contact |
|------------|---------|
| Database errors | DBA Team |
| Application errors | Development Team |
| Migration questions | Check full README.md |
| Critical issues | Escalate to IT Manager |

---

## ✅ Success Indicators

You're done when:
- ✅ All validation tests pass
- ✅ Application starts without errors
- ✅ Store management pages work
- ✅ Can create new Store entities
- ✅ Only see tenant's own data

---

## 📋 Post-Migration Checklist

- [ ] Backup verified and stored safely
- [ ] All migration scripts executed successfully
- [ ] Validation script shows all tests passing
- [ ] Application tested and working
- [ ] Team notified of completion
- [ ] Documentation updated
- [ ] Monitoring set up for first 24 hours

---

## 🔗 Full Documentation

For detailed information, see [README.md](README.md) in this directory.
