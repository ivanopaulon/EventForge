-- Rollback: Remove Shape, Width, and Height columns from TableSessions
-- Date: 2026-07-07
-- Companion to: 20260707_AddShapeWidthHeightToTableSessions.sql
--
-- WARNING:
--   This rollback removes persisted floor-plan renderer configuration for
--   all restaurant tables.

BEGIN TRANSACTION;

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TableSessions]') AND name = 'Height')
BEGIN
    ALTER TABLE [dbo].[TableSessions] DROP COLUMN [Height];
    PRINT 'Dropped Height from TableSessions';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TableSessions]') AND name = 'Width')
BEGIN
    ALTER TABLE [dbo].[TableSessions] DROP COLUMN [Width];
    PRINT 'Dropped Width from TableSessions';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TableSessions]') AND name = 'Shape')
BEGIN
    ALTER TABLE [dbo].[TableSessions] DROP COLUMN [Shape];
    PRINT 'Dropped Shape from TableSessions';
END

COMMIT;
