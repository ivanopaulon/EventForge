-- Migration: Rename ClosedAt to ArchivedAt in DocumentHeaders
-- Date: 2026-06-23
-- Description: Renames the DocumentHeaders.ClosedAt column to ArchivedAt to reflect
--              the updated domain language (archiving instead of closing).

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[DocumentHeaders]')
      AND name = N'ClosedAt'
)
BEGIN
    EXEC sp_rename '[dbo].[DocumentHeaders].[ClosedAt]', 'ArchivedAt', 'COLUMN';
    PRINT 'Column DocumentHeaders.ClosedAt renamed to ArchivedAt.';
END
ELSE
BEGIN
    PRINT 'Column DocumentHeaders.ClosedAt does not exist — skipping rename.';
END
