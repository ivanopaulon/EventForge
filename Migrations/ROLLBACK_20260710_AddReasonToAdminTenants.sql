-- Rollback: Remove Reason column from AdminTenants
-- Date: 2026-07-10

IF EXISTS (
    SELECT * FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[AdminTenants]') AND name = 'Reason'
)
BEGIN
    ALTER TABLE [dbo].[AdminTenants] DROP COLUMN [Reason];
    PRINT 'Reason column dropped from AdminTenants.';
END
ELSE
BEGIN
    PRINT 'Reason column does not exist on AdminTenants — nothing to roll back.';
END
