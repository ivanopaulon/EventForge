# Phase 3 Migration - Execution Checklist

## 📋 Pre-Migration Preparation

### Infrastructure Readiness
- [ ] SQL Server Management Studio installed
- [ ] VPN/Network access to database server
- [ ] Database Administrator credentials available
- [ ] Sufficient disk space for backup (check size of EventData database)
- [ ] Maintenance window scheduled and communicated
- [ ] Stakeholders notified of downtime

### Documentation Review
- [ ] Read [README.md](README.md) completely
- [ ] Review [QUICK_START_GUIDE.md](QUICK_START_GUIDE.md)
- [ ] Understand rollback procedures
- [ ] Have emergency contacts ready

### Team Coordination
- [ ] DBA assigned and available
- [ ] Developer on standby for issues
- [ ] Communication channel established (Slack/Teams)
- [ ] Rollback authority identified

---

## 🎬 Execution Phase

### Phase 1: Backup (Critical - Do Not Skip!)

**Estimated Time**: 5-10 minutes  
**Blocking**: Yes  

- [ ] **1.1** Connect to SQL Server with SSMS
- [ ] **1.2** Verify database size: 
  ```sql
  USE EventData;
  EXEC sp_spaceused;
  ```
- [ ] **1.3** Create backup (⚠️ Note: Ensure C:\Backups\ directory exists or adjust path):
  ```sql
  BACKUP DATABASE [EventData] 
  TO DISK = 'C:\Backups\EventData_PrePhase3_20251204.bak'
  WITH COMPRESSION, INIT;
  ```
- [ ] **1.4** Verify backup (adjust path if needed):
  ```sql
  RESTORE VERIFYONLY 
  FROM DISK = 'C:\Backups\EventData_PrePhase3_20251204.bak';
  ```
- [ ] **1.5** Note backup location: ___________________
- [ ] **1.6** Note backup size: ___________________
- [ ] **1.7** Take screenshot of successful backup
- [ ] **1.8** Copy backup to secondary location (optional but recommended)

**✅ Backup Complete** - Time: _____

---

### Phase 2: Pre-Migration Audit

**Estimated Time**: 2-3 minutes  
**Blocking**: No  

- [ ] **2.1** Open `01_PreMigration_Audit_OrphanRecords.sql` in SSMS
- [ ] **2.2** Ensure correct database selected (EventData)
- [ ] **2.3** Execute script (F5)
- [ ] **2.4** Wait for completion
- [ ] **2.5** Review output:
  - Orphan StoreUserPrivileges: _____
  - Orphan StoreUserGroups: _____
  - Orphan StoreUsers: _____
  - Orphan StorePos: _____
  - Total orphans: _____

**Decision Point:**
- [ ] If **total orphans = 0**: Skip Phase 3, go to Phase 4
- [ ] If **orphans found**: Continue to Phase 3

**Optional - Export Orphan Data**
- [ ] Run export queries if manual review needed (see audit script output)
- [ ] Save results to CSV files
- [ ] Store in safe location: ___________________

**✅ Audit Complete** - Time: _____

---

### Phase 3: Cleanup Orphan Data (DELETES DATA!)

**Estimated Time**: 3-5 minutes  
**Blocking**: Yes  
**⚠️ WARNING**: This phase permanently deletes data!

**Pre-Cleanup Verification**
- [ ] **3.1** Backup verified and accessible: ✅
- [ ] **3.2** Audit results reviewed and acceptable
- [ ] **3.3** Team ready for potential issues
- [ ] **3.4** Application stopped/maintenance mode enabled

**Execute Cleanup**
- [ ] **3.5** Open `02_Migration_Cleanup_OrphanData.sql` in SSMS
- [ ] **3.6** Read the script header warnings
- [ ] **3.7** Ensure correct database selected (EventData)
- [ ] **3.8** Execute script (F5)
- [ ] **3.9** Monitor progress in Messages tab
- [ ] **3.10** Wait for completion (do not interrupt!)

**Verify Results**
- [ ] **3.11** Check for message: "SUCCESS: Transaction committed successfully!"
- [ ] **3.12** Review deletion summary:
  - StoreUserPrivileges deleted: _____
  - StoreUserGroups deleted: _____
  - StoreUsers deleted: _____
  - StorePos deleted: _____
  - Junction records deleted: _____
  - References nullified: _____
  - Total affected: _____

**If Errors Occur**
- [ ] Note error message: ___________________
- [ ] Check if transaction was rolled back (should be automatic)
- [ ] Review troubleshooting section in script
- [ ] Consult DBA before retrying
- [ ] Consider restoring from backup if critical

**✅ Cleanup Complete** - Time: _____

---

### Phase 4: Add Constraints and Indexes

**Estimated Time**: 3-5 minutes  
**Blocking**: Yes  

- [ ] **4.1** Open `03_Migration_Add_Constraints.sql` in SSMS
- [ ] **4.2** Ensure correct database selected (EventData)
- [ ] **4.3** Execute script (F5)
- [ ] **4.4** Monitor progress in Messages tab
- [ ] **4.5** Wait for completion

**Verify Results**
- [ ] **4.6** Check for message: "SUCCESS: All constraints and indexes applied!"
- [ ] **4.7** Confirm constraints added to:
  - [ ] StoreUserPrivileges.TenantId
  - [ ] StoreUserGroups.TenantId
  - [ ] StoreUsers.TenantId
  - [ ] StorePoses.TenantId
- [ ] **4.8** Confirm indexes created:
  - [ ] IX_StoreUserPrivileges_TenantId
  - [ ] IX_StoreUserGroups_TenantId
  - [ ] IX_StoreUsers_TenantId
  - [ ] IX_StorePoses_TenantId
- [ ] **4.9** Confirm CHECK constraints added (if applicable)

**If Errors Occur**
- [ ] Note error message: ___________________
- [ ] Check if some constraints were added
- [ ] Review validation section in script
- [ ] Retry individual ALTER statements if needed
- [ ] Consult DBA before proceeding

**✅ Constraints Added** - Time: _____

---

### Phase 5: Post-Migration Validation

**Estimated Time**: 2-3 minutes  
**Blocking**: No  

- [ ] **5.1** Open `05_PostMigration_Validation.sql` in SSMS
- [ ] **5.2** Ensure correct database selected (EventData)
- [ ] **5.3** Execute script (F5)
- [ ] **5.4** Monitor progress in Messages tab
- [ ] **5.5** Wait for completion

**Review Test Results**
- [ ] **TEST 1**: No orphan records - PASS/FAIL
- [ ] **TEST 2**: NOT NULL constraints - PASS/FAIL
- [ ] **TEST 3**: Performance indexes - PASS/FAIL
- [ ] **TEST 4**: CHECK constraints - PASS/FAIL
- [ ] **TEST 5**: Foreign key integrity - PASS/FAIL
- [ ] **TEST 6**: Data distribution - Reviewed
- [ ] **TEST 7**: Query performance - Reviewed
- [ ] **TEST 8**: Record counts - Reviewed

**Final Validation Status**
- [ ] **5.6** All critical tests PASSED
- [ ] **5.7** Message: "SUCCESS: ALL VALIDATION TESTS PASSED!"
- [ ] **5.8** Screenshot validation results

**If Validation Fails**
- [ ] Note which tests failed: ___________________
- [ ] Review error details in output
- [ ] Determine if errors are critical
- [ ] Consult DBA and development team
- [ ] Consider rollback if critical failures

**✅ Validation Complete** - Time: _____

---

### Phase 6: Application Testing

**Estimated Time**: 5-10 minutes  
**Blocking**: Yes  

**Start Application**
- [ ] **6.1** Start application server
- [ ] **6.2** Monitor startup logs
- [ ] **6.3** Check for database connection errors
- [ ] **6.4** Verify application accessible

**Functional Testing**
- [ ] **6.5** Login as tenant user
  - Username: ___________________
  - Tenant: ___________________
- [ ] **6.6** Navigate to Store Management section
- [ ] **6.7** Verify Store pages load correctly
  - [ ] StoreUsers (Operators) page
  - [ ] StoreUserGroups page
  - [ ] StorePoses page

**CRUD Operations Testing**
- [ ] **6.8** Create new StoreUser:
  - [ ] Form opens
  - [ ] Can fill in data
  - [ ] Save successful
  - [ ] TenantId automatically set
  - [ ] Only see own tenant's data
- [ ] **6.9** Create new StoreUserGroup:
  - [ ] Form opens
  - [ ] Can fill in data
  - [ ] Save successful
  - [ ] TenantId automatically set
- [ ] **6.10** Create new StorePos:
  - [ ] Form opens
  - [ ] Can fill in data
  - [ ] Save successful
  - [ ] TenantId automatically set

**Data Isolation Testing**
- [ ] **6.11** Verify can only see own tenant's Store data
- [ ] **6.12** Try querying with different tenant (if multi-tenant test available)
- [ ] **6.13** Verify DocumentHeader operations still work

**Performance Testing**
- [ ] **6.14** Note page load times: ___________________
- [ ] **6.15** Compare with expected performance
- [ ] **6.16** No significant degradation observed

**Error Checking**
- [ ] **6.17** Check browser console for JavaScript errors
- [ ] **6.18** Check server logs for exceptions
- [ ] **6.19** Check database logs for query errors

**✅ Application Testing Complete** - Time: _____

---

## ✅ Post-Migration Activities

### Documentation
- [ ] Update this checklist with actual times
- [ ] Note any issues encountered: ___________________
- [ ] Document any deviations from plan: ___________________
- [ ] Save all script output messages
- [ ] Take screenshots of key validation points

### Communication
- [ ] Notify team of successful completion
- [ ] Update status in project tracker
- [ ] Send completion email to stakeholders
- [ ] Document lessons learned

### Monitoring
- [ ] Set up monitoring for next 24 hours
- [ ] Watch for unusual errors in logs
- [ ] Monitor query performance
- [ ] Track user feedback

### Cleanup
- [ ] Archive backup file with proper naming
- [ ] Store audit results
- [ ] File documentation in appropriate location
- [ ] Update runbook if needed

---

## 🔴 Emergency Rollback Procedure

### When to Rollback
Rollback if:
- [ ] Multiple validation tests fail
- [ ] Application crashes or has critical errors
- [ ] Data integrity issues discovered
- [ ] Performance severely degraded
- [ ] Business decision to abort

### Rollback Option A: Remove Constraints Only

**Use when**: Data is OK but constraints cause issues

- [ ] **R.1** Stop application
- [ ] **R.2** Open `04_Rollback_Remove_Constraints.sql`
- [ ] **R.3** Execute script
- [ ] **R.4** Verify constraints removed
- [ ] **R.5** Test application
- [ ] **R.6** Investigate root cause

### Rollback Option B: Full Database Restore

**Use when**: Data was corrupted or need complete rollback

- [ ] **R.7** Stop application and block all database access
- [ ] **R.8** Run restore command:
  ```sql
  ALTER DATABASE [EventData] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
  RESTORE DATABASE [EventData] 
  FROM DISK = 'C:\Backups\EventData_PrePhase3_20251204.bak'
  WITH REPLACE;
  ALTER DATABASE [EventData] SET MULTI_USER;
  ```
- [ ] **R.9** Verify restore successful
- [ ] **R.10** Test database connectivity
- [ ] **R.11** Start application
- [ ] **R.12** Verify application works
- [ ] **R.13** Investigate root cause before retry

### Post-Rollback
- [ ] Document why rollback was necessary
- [ ] Analyze what went wrong
- [ ] Update migration plan
- [ ] Schedule re-attempt if appropriate

---

## 📊 Migration Summary

**Execution Date**: ___________________  
**Start Time**: ___________________  
**End Time**: ___________________  
**Total Duration**: ___________________  
**Executed By**: ___________________  
**Status**: ✅ Success / ⚠️ Partial / ❌ Failed / 🔄 Rolled Back

**Orphan Records Cleaned**:
- StoreUserPrivileges: _____
- StoreUserGroups: _____
- StoreUsers: _____
- StorePoses: _____
- Total: _____

**Issues Encountered**: 
___________________
___________________
___________________

**Resolution**:
___________________
___________________
___________________

**Sign-Off**:
- DBA: ___________________  
- Developer: ___________________  
- Manager: ___________________  

---

## 📞 Emergency Contacts

| Role | Name | Contact |
|------|------|---------|
| Primary DBA | _____________ | _____________ |
| Backup DBA | _____________ | _____________ |
| Lead Developer | _____________ | _____________ |
| DevOps Engineer | _____________ | _____________ |
| IT Manager | _____________ | _____________ |

---

**Remember**: Better to abort and rollback than to force through with errors!

**Good luck with the migration! 🚀**
