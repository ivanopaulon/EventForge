-- Migration: 20260507_IncreaseEntityDisplayNameLength
-- Purpose: increase EntityChangeLogs.EntityDisplayName length to avoid truncation during
--          bulk reconciliation and other verbose audit operations.

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.EntityChangeLogs')
      AND name = N'EntityDisplayName'
)
BEGIN
    ALTER TABLE dbo.EntityChangeLogs
        ALTER COLUMN EntityDisplayName NVARCHAR(500) NULL;
END
GO

PRINT 'Migration 20260507_IncreaseEntityDisplayNameLength completed.';
