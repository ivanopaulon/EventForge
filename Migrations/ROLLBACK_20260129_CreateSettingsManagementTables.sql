-- =============================================
-- Rollback Migration: Settings Management Tables
-- Date: 2026-01-29
-- Description: Rolls back the Settings Management Tables migration
-- =============================================

-- Drop SystemOperationLog table
IF OBJECT_ID('SystemOperationLog', 'U') IS NOT NULL
BEGIN
    PRINT 'Dropping SystemOperationLog table...';
    DROP TABLE SystemOperationLog;
END

-- Drop JwtKeyHistory table
IF OBJECT_ID('JwtKeyHistory', 'U') IS NOT NULL
BEGIN
    PRINT 'Dropping JwtKeyHistory table...';
    DROP TABLE JwtKeyHistory;
END

-- Remove added columns from SystemConfiguration
-- Note: We keep the table itself as it existed before this migration

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_SystemConfiguration_Key_Active' AND object_id = OBJECT_ID('SystemConfiguration'))
BEGIN
    PRINT 'Dropping UQ_SystemConfiguration_Key_Active constraint...';
    DROP INDEX UQ_SystemConfiguration_Key_Active ON SystemConfiguration;
END

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SystemConfiguration_Key_Version' AND object_id = OBJECT_ID('SystemConfiguration'))
BEGIN
    PRINT 'Dropping IX_SystemConfiguration_Key_Version index...';
    DROP INDEX IX_SystemConfiguration_Key_Version ON SystemConfiguration;
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SystemConfiguration') AND name = 'ModifiedAt')
BEGIN
    PRINT 'Removing ModifiedAt column from SystemConfiguration...';
    ALTER TABLE SystemConfiguration DROP COLUMN ModifiedAt;
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SystemConfiguration') AND name = 'ModifiedBy')
BEGIN
    PRINT 'Removing ModifiedBy column from SystemConfiguration...';
    ALTER TABLE SystemConfiguration DROP COLUMN ModifiedBy;
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SystemConfiguration') AND name = 'CreatedBy')
BEGIN
    PRINT 'Removing CreatedBy column from SystemConfiguration...';
    ALTER TABLE SystemConfiguration DROP COLUMN CreatedBy;
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SystemConfiguration') AND name = 'IsActive')
BEGIN
    PRINT 'Removing IsActive column from SystemConfiguration...';
    ALTER TABLE SystemConfiguration DROP COLUMN IsActive;
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SystemConfiguration') AND name = 'Version')
BEGIN
    PRINT 'Removing Version column from SystemConfiguration...';
    ALTER TABLE SystemConfiguration DROP COLUMN Version;
END

PRINT 'Settings Management Tables rollback completed successfully';
GO
