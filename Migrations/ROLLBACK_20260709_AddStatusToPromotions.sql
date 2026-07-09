-- Rollback: Remove Status from Promotions

IF COL_LENGTH('dbo.Promotions', 'Status') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Promotions] DROP COLUMN [Status];
    PRINT 'Status column removed from Promotions.';
END
