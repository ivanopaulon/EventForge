-- =============================================
-- Migration: Create Settings Management Tables
-- Date: 2026-01-29
-- Description: Creates tables for advanced settings management including:
--              - Enhanced SystemConfiguration with versioning
--              - JwtKeyHistory for key rotation
--              - SystemOperationLog for audit trail
-- =============================================

-- Drop tables if they exist (for clean reinstall)
IF OBJECT_ID('SystemOperationLog', 'U') IS NOT NULL
    DROP TABLE SystemOperationLog;

IF OBJECT_ID('JwtKeyHistory', 'U') IS NOT NULL
    DROP TABLE JwtKeyHistory;

-- Note: SystemConfiguration already exists, we'll extend it instead
-- =============================================
-- 1. Extend SystemConfiguration table with versioning
-- =============================================

-- Add new columns to existing SystemConfiguration table if they don't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SystemConfiguration') AND name = 'Version')
BEGIN
    ALTER TABLE SystemConfiguration ADD Version INT NOT NULL DEFAULT 1;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SystemConfiguration') AND name = 'IsActive')
BEGIN
    ALTER TABLE SystemConfiguration ADD IsActive BIT NOT NULL DEFAULT 1;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SystemConfiguration') AND name = 'ModifiedBy')
BEGIN
    ALTER TABLE SystemConfiguration ADD ModifiedBy NVARCHAR(100) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SystemConfiguration') AND name = 'ModifiedAt')
BEGIN
    ALTER TABLE SystemConfiguration ADD ModifiedAt DATETIME2 NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SystemConfiguration') AND name = 'CreatedBy')
BEGIN
    ALTER TABLE SystemConfiguration ADD CreatedBy NVARCHAR(100) NOT NULL DEFAULT 'System';
END

-- Create index for versioning queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SystemConfiguration_Key_Version' AND object_id = OBJECT_ID('SystemConfiguration'))
BEGIN
    CREATE INDEX IX_SystemConfiguration_Key_Version ON SystemConfiguration([Key], Version);
END

-- Create unique constraint for active configurations
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_SystemConfiguration_Key_Active' AND object_id = OBJECT_ID('SystemConfiguration'))
BEGIN
    CREATE UNIQUE INDEX UQ_SystemConfiguration_Key_Active ON SystemConfiguration([Key]) 
    WHERE IsActive = 1;
END

-- =============================================
-- 2. Create JwtKeyHistory table for key rotation
-- =============================================
CREATE TABLE JwtKeyHistory (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    KeyIdentifier NVARCHAR(50) NOT NULL UNIQUE,
    EncryptedKey NVARCHAR(MAX) NOT NULL, -- AES-256 encrypted
    IsActive BIT NOT NULL DEFAULT 1,
    ValidFrom DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ValidUntil DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy NVARCHAR(100) NOT NULL,
    
    CONSTRAINT CHK_JwtKeyHistory_ValidDates CHECK (ValidUntil IS NULL OR ValidUntil > ValidFrom)
);

-- Create index for active key lookups
CREATE INDEX IX_JwtKeyHistory_IsActive_ValidFrom ON JwtKeyHistory(IsActive, ValidFrom);

-- Create index for key identifier lookups
CREATE INDEX IX_JwtKeyHistory_KeyIdentifier ON JwtKeyHistory(KeyIdentifier);

-- =============================================
-- 3. Create SystemOperationLog table for audit trail
-- =============================================
CREATE TABLE SystemOperationLog (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    OperationType NVARCHAR(50) NOT NULL, -- ConfigChange, Migration, Restart, Bootstrap, KeyRotation, Export, Import
    EntityType NVARCHAR(100) NULL, -- Configuration key, Migration name, etc.
    EntityId NVARCHAR(200) NULL,
    Action NVARCHAR(50) NOT NULL, -- Create, Update, Delete, Apply, Rollback, etc.
    Description NVARCHAR(500) NOT NULL,
    OldValue NVARCHAR(MAX) NULL, -- For config changes
    NewValue NVARCHAR(MAX) NULL,
    Details NVARCHAR(MAX) NULL, -- JSON with additional metadata
    Success BIT NOT NULL DEFAULT 1,
    ErrorMessage NVARCHAR(MAX) NULL,
    ExecutedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExecutedBy NVARCHAR(100) NOT NULL,
    IpAddress NVARCHAR(50) NULL,
    UserAgent NVARCHAR(500) NULL,
    
    CONSTRAINT CHK_SystemOperationLog_OperationType CHECK (
        OperationType IN ('ConfigChange', 'Migration', 'Restart', 'Bootstrap', 'KeyRotation', 'Export', 'Import', 'DatabaseOperation')
    ),
    CONSTRAINT CHK_SystemOperationLog_Action CHECK (
        Action IN ('Create', 'Update', 'Delete', 'Apply', 'Rollback', 'Rotate', 'Revoke', 'Reset', 'Import', 'Export', 'Backup', 'Restore', 'Restart')
    )
);

-- Create indexes for performance
CREATE INDEX IX_SystemOperationLog_OperationType_ExecutedAt ON SystemOperationLog(OperationType, ExecutedAt DESC);
CREATE INDEX IX_SystemOperationLog_EntityType_EntityId ON SystemOperationLog(EntityType, EntityId);
CREATE INDEX IX_SystemOperationLog_ExecutedBy ON SystemOperationLog(ExecutedBy);
CREATE INDEX IX_SystemOperationLog_ExecutedAt ON SystemOperationLog(ExecutedAt DESC);

-- =============================================
-- 4. Seed initial JWT key if none exists
-- SECURITY WARNING: The placeholder key MUST be replaced
-- before production use. See application startup validation.
-- =============================================
IF NOT EXISTS (SELECT 1 FROM JwtKeyHistory WHERE IsActive = 1)
BEGIN
    -- Generate a placeholder key identifier
    DECLARE @keyId NVARCHAR(50) = 'key_' + CONVERT(NVARCHAR(36), NEWID());
    
    -- PLACEHOLDER KEY - MUST BE REPLACED BEFORE PRODUCTION USE
    INSERT INTO JwtKeyHistory (KeyIdentifier, EncryptedKey, IsActive, ValidFrom, CreatedBy)
    VALUES (@keyId, 'PLACEHOLDER_KEY_TO_BE_REPLACED_ON_FIRST_USE', 1, GETUTCDATE(), 'System.Migration');
    
    -- Log the operation
    INSERT INTO SystemOperationLog (OperationType, Action, Description, Success, ExecutedBy)
    VALUES ('Bootstrap', 'Create', 'Initial JWT key placeholder created during migration - MUST BE REPLACED', 1, 'System.Migration');
END

-- =============================================
-- 5. Log migration completion
-- =============================================
INSERT INTO SystemOperationLog (OperationType, Action, Description, Success, ExecutedBy)
VALUES ('Migration', 'Apply', 'Settings Management Tables migration applied successfully', 1, 'System.Migration');

PRINT 'Settings Management Tables migration completed successfully';
GO
