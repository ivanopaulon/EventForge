-- Migration: Add Shape, Width, and Height columns to TableSessions
-- Date: 2026-07-07
-- Related to: Restaurant table floor-plan configurable table size/shape
--
-- PURPOSE:
--   Adds renderer configuration fields to TableSessions so each table can
--   store its floor-plan shape and pixel dimensions.
--
-- ROLLBACK: See ROLLBACK_20260707_AddShapeWidthHeightToTableSessions.sql

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TableSessions]') AND name = 'Shape')
BEGIN
    ALTER TABLE [dbo].[TableSessions]
    ADD [Shape] INT NOT NULL DEFAULT 0;

    PRINT 'Added Shape to TableSessions';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TableSessions]') AND name = 'Width')
BEGIN
    ALTER TABLE [dbo].[TableSessions]
    ADD [Width] INT NOT NULL DEFAULT 90;

    PRINT 'Added Width to TableSessions';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TableSessions]') AND name = 'Height')
BEGIN
    ALTER TABLE [dbo].[TableSessions]
    ADD [Height] INT NOT NULL DEFAULT 90;

    PRINT 'Added Height to TableSessions';
END

COMMIT;
