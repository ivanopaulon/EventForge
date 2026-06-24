-- ROLLBACK: 20260624_AddAIUsageLog
-- Removes the AIUsageLogs table.

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AIUsageLogs')
BEGIN
    DROP TABLE [dbo].[AIUsageLogs];
    PRINT 'Dropped table AIUsageLogs';
END
