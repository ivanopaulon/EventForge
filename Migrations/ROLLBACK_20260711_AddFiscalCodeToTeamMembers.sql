-- Rollback: Remove FiscalCode column from TeamMembers
-- Date: 2026-07-11

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TeamMembers_FiscalCode' AND object_id = OBJECT_ID('dbo.TeamMembers'))
BEGIN
    DROP INDEX [IX_TeamMembers_FiscalCode] ON [dbo].[TeamMembers];
END

IF EXISTS (
    SELECT * FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[TeamMembers]') AND name = 'FiscalCode'
)
BEGIN
    ALTER TABLE [dbo].[TeamMembers] DROP COLUMN [FiscalCode];
    PRINT 'FiscalCode column dropped from TeamMembers.';
END
ELSE
BEGIN
    PRINT 'FiscalCode column does not exist on TeamMembers — nothing to roll back.';
END
