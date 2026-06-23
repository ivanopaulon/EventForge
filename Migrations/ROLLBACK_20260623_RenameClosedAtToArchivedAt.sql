-- Rollback: Rename ArchivedAt back to ClosedAt in DocumentHeaders
-- Date: 2026-06-23

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[DocumentHeaders]')
      AND name = N'ArchivedAt'
)
BEGIN
    EXEC sp_rename '[dbo].[DocumentHeaders].[ArchivedAt]', 'ClosedAt', 'COLUMN';
    PRINT 'Column DocumentHeaders.ArchivedAt rolled back to ClosedAt.';
END
ELSE
BEGIN
    PRINT 'Column DocumentHeaders.ArchivedAt does not exist — skipping rollback.';
END
