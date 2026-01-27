-- =============================================
-- Migration: Create BusinessPartyGroups and BusinessPartyGroupMembers tables
-- Date: 2026-01-27
-- Description: Create tables for Business Party Groups feature with proper ColorHex field size (nvarchar(7))
-- =============================================

USE [EventData];
GO

-- Create BusinessPartyGroups table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BusinessPartyGroups' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[BusinessPartyGroups] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(50) NULL,
        [Description] nvarchar(500) NULL,
        [GroupType] int NOT NULL,
        [ColorHex] nvarchar(7) NULL,
        [Icon] nvarchar(50) NULL,
        [Priority] int NOT NULL DEFAULT 50,
        [ValidFrom] datetime2 NULL,
        [ValidTo] datetime2 NULL,
        [MetadataJson] nvarchar(max) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT 0,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_BusinessPartyGroups] PRIMARY KEY ([Id])
    );

    PRINT 'Created table BusinessPartyGroups';
END
ELSE
BEGIN
    -- If table exists, ensure ColorHex column has correct size
    IF EXISTS (
        SELECT * FROM sys.columns c
        JOIN sys.tables t ON c.object_id = t.object_id
        JOIN sys.types ty ON c.user_type_id = ty.user_type_id
        WHERE t.name = 'BusinessPartyGroups' 
        AND c.name = 'ColorHex' 
        AND ty.name = 'nvarchar'
        AND c.max_length < 14  -- nvarchar(7) = 14 bytes (2 bytes per char)
    )
    BEGIN
        ALTER TABLE [dbo].[BusinessPartyGroups]
        ALTER COLUMN [ColorHex] nvarchar(7) NULL;
        
        PRINT 'Altered ColorHex column to nvarchar(7)';
    END
    ELSE
    BEGIN
        PRINT 'ColorHex column already has correct size or doesn''t exist';
    END
END
GO

-- Create BusinessPartyGroupMembers table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BusinessPartyGroupMembers' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[BusinessPartyGroupMembers] (
        [Id] uniqueidentifier NOT NULL,
        [BusinessPartyGroupId] uniqueidentifier NOT NULL,
        [BusinessPartyId] uniqueidentifier NOT NULL,
        [JoinedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [LeftDate] datetime2 NULL,
        [Notes] nvarchar(500) NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT 0,
        [DeletedAt] datetime2 NULL,
        [DeletedBy] nvarchar(100) NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_BusinessPartyGroupMembers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BusinessPartyGroupMembers_BusinessPartyGroups] FOREIGN KEY ([BusinessPartyGroupId]) 
            REFERENCES [dbo].[BusinessPartyGroups] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_BusinessPartyGroupMembers_BusinessParties] FOREIGN KEY ([BusinessPartyId]) 
            REFERENCES [dbo].[BusinessParties] ([Id]) ON DELETE CASCADE
    );

    -- Create indexes
    CREATE INDEX [IX_BusinessPartyGroupMembers_BusinessPartyGroupId] 
        ON [dbo].[BusinessPartyGroupMembers] ([BusinessPartyGroupId]);
    
    CREATE INDEX [IX_BusinessPartyGroupMembers_BusinessPartyId] 
        ON [dbo].[BusinessPartyGroupMembers] ([BusinessPartyId]);
    
    CREATE INDEX [IX_BusinessPartyGroupMembers_TenantId] 
        ON [dbo].[BusinessPartyGroupMembers] ([TenantId]);

    PRINT 'Created table BusinessPartyGroupMembers';
END
GO

-- Create index on BusinessPartyGroups if not exists
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BusinessPartyGroups_TenantId' AND object_id = OBJECT_ID('dbo.BusinessPartyGroups'))
BEGIN
    CREATE INDEX [IX_BusinessPartyGroups_TenantId] 
        ON [dbo].[BusinessPartyGroups] ([TenantId]);
    PRINT 'Created index IX_BusinessPartyGroups_TenantId';
END
GO

-- Add extended properties for documentation
IF NOT EXISTS (
    SELECT * FROM sys.extended_properties 
    WHERE major_id = OBJECT_ID('dbo.BusinessPartyGroups') 
    AND name = 'MS_Description' AND minor_id = 0
)
BEGIN
    EXEC sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Groups of Business Parties (customers, suppliers, or both) for promotions, price lists, or specific policies.', 
        @level0type = N'SCHEMA', @level0name = 'dbo',
        @level1type = N'TABLE', @level1name = 'BusinessPartyGroups';
END
GO

-- Add extended property for ColorHex column
DECLARE @ColorHexDesc NVARCHAR(500) = N'Hexadecimal color code in format #RRGGBB (7 characters) for UI badge display.';

IF EXISTS (
    SELECT * FROM sys.extended_properties 
    WHERE major_id = OBJECT_ID('dbo.BusinessPartyGroups') 
    AND name = 'MS_Description' 
    AND minor_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BusinessPartyGroups') AND name = 'ColorHex')
)
BEGIN
    EXEC sp_updateextendedproperty 
        @name = N'MS_Description', 
        @value = @ColorHexDesc, 
        @level0type = N'SCHEMA', @level0name = 'dbo',
        @level1type = N'TABLE', @level1name = 'BusinessPartyGroups',
        @level2type = N'COLUMN', @level2name = 'ColorHex';
END
ELSE
BEGIN
    EXEC sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = @ColorHexDesc, 
        @level0type = N'SCHEMA', @level0name = 'dbo',
        @level1type = N'TABLE', @level1name = 'BusinessPartyGroups',
        @level2type = N'COLUMN', @level2name = 'ColorHex';
END
GO

PRINT 'Migration 20260127_CreateBusinessPartyGroupsTable applied successfully.';
GO
