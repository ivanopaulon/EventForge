-- Rollback: 20260507_IncreaseEntityDisplayNameLength
-- Purpose: revert EntityChangeLogs.EntityDisplayName back to NVARCHAR(100).
-- WARNING: ensure no values exceed 100 chars before running this rollback.

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.EntityChangeLogs')
      AND name = N'EntityDisplayName'
)
BEGIN
    ALTER TABLE dbo.EntityChangeLogs
        ALTER COLUMN EntityDisplayName NVARCHAR(100) NULL;
END
GO

PRINT 'Rollback 20260507_IncreaseEntityDisplayNameLength completed.';
