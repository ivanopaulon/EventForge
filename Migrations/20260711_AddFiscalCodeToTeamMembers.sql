-- Migration: Add FiscalCode to TeamMembers
-- Date: 2026-07-11
-- Description: Adds a nullable FiscalCode column to TeamMembers. Nullable because not all
-- historical members have one recorded and we do not want to force retroactive data entry.
-- Used only to power a non-blocking multi-team warning (never a save-blocking validation).

IF NOT EXISTS (
    SELECT * FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[TeamMembers]') AND name = 'FiscalCode'
)
BEGIN
    ALTER TABLE [dbo].[TeamMembers]
        ADD [FiscalCode] nvarchar(16) NULL;

    PRINT 'FiscalCode column added to TeamMembers.';
END
ELSE
BEGIN
    PRINT 'FiscalCode column already exists on TeamMembers — skipping.';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_TeamMembers_FiscalCode' AND object_id = OBJECT_ID('dbo.TeamMembers'))
BEGIN
    CREATE INDEX [IX_TeamMembers_FiscalCode]
        ON [dbo].[TeamMembers] ([FiscalCode])
        WHERE [FiscalCode] IS NOT NULL AND [IsDeleted] = 0;
END

PRINT 'Migration 20260711_AddFiscalCodeToTeamMembers applied successfully.';
