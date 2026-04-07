-- Migration: Add Branding Configuration
-- Date: 2026-02-02
-- Description: Add branding configuration settings and tenant-specific branding overrides

-- =============================================
-- 1. Add Branding Configurations to SystemConfigurations table
-- =============================================

-- Check if configurations already exist before inserting
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemConfigurations] WHERE [Key] = 'Branding:LogoUrl')
BEGIN
    INSERT INTO [dbo].[SystemConfigurations] 
        ([Id], [Key], [Value], [Category], [Description], [IsEncrypted], [RequiresRestart], [IsReadOnly], [DefaultValue], [Version], [IsActive], [CreatedBy], [CreatedAt])
    VALUES
        (NEWID(), 'Branding:LogoUrl', '/eventforgetitle.svg', 'Branding', 'Default logo URL for the application', 0, 0, 0, '/eventforgetitle.svg', 1, 1, 'System', GETUTCDATE());
    
    PRINT 'Added configuration: Branding:LogoUrl';
END
ELSE
    PRINT 'Configuration Branding:LogoUrl already exists, skipping.';

IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemConfigurations] WHERE [Key] = 'Branding:LogoHeight')
BEGIN
    INSERT INTO [dbo].[SystemConfigurations] 
        ([Id], [Key], [Value], [Category], [Description], [IsEncrypted], [RequiresRestart], [IsReadOnly], [DefaultValue], [Version], [IsActive], [CreatedBy], [CreatedAt])
    VALUES
        (NEWID(), 'Branding:LogoHeight', '40', 'Branding', 'Default logo height in pixels', 0, 0, 0, '40', 1, 1, 'System', GETUTCDATE());
    
    PRINT 'Added configuration: Branding:LogoHeight';
END
ELSE
    PRINT 'Configuration Branding:LogoHeight already exists, skipping.';

IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemConfigurations] WHERE [Key] = 'Branding:ApplicationName')
BEGIN
    INSERT INTO [dbo].[SystemConfigurations] 
        ([Id], [Key], [Value], [Category], [Description], [IsEncrypted], [RequiresRestart], [IsReadOnly], [DefaultValue], [Version], [IsActive], [CreatedBy], [CreatedAt])
    VALUES
        (NEWID(), 'Branding:ApplicationName', 'EventForge', 'Branding', 'Application name displayed in UI', 0, 0, 0, 'EventForge', 1, 1, 'System', GETUTCDATE());
    
    PRINT 'Added configuration: Branding:ApplicationName';
END
ELSE
    PRINT 'Configuration Branding:ApplicationName already exists, skipping.';

IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemConfigurations] WHERE [Key] = 'Branding:FaviconUrl')
BEGIN
    INSERT INTO [dbo].[SystemConfigurations] 
        ([Id], [Key], [Value], [Category], [Description], [IsEncrypted], [RequiresRestart], [IsReadOnly], [DefaultValue], [Version], [IsActive], [CreatedBy], [CreatedAt])
    VALUES
        (NEWID(), 'Branding:FaviconUrl', '/trace.svg', 'Branding', 'Favicon URL for the application', 0, 0, 0, '/trace.svg', 1, 1, 'System', GETUTCDATE());
    
    PRINT 'Added configuration: Branding:FaviconUrl';
END
ELSE
    PRINT 'Configuration Branding:FaviconUrl already exists, skipping.';

IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemConfigurations] WHERE [Key] = 'Branding:AllowTenantOverride')
BEGIN
    INSERT INTO [dbo].[SystemConfigurations] 
        ([Id], [Key], [Value], [Category], [Description], [IsEncrypted], [RequiresRestart], [IsReadOnly], [DefaultValue], [Version], [IsActive], [CreatedBy], [CreatedAt])
    VALUES
        (NEWID(), 'Branding:AllowTenantOverride', 'true', 'Branding', 'Allow tenants to override global branding settings', 0, 0, 0, 'true', 1, 1, 'System', GETUTCDATE());
    
    PRINT 'Added configuration: Branding:AllowTenantOverride';
END
ELSE
    PRINT 'Configuration Branding:AllowTenantOverride already exists, skipping.';

-- =============================================
-- 2. Add Custom Branding Columns to Tenants Table
-- =============================================

-- Add CustomLogoUrl column if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Tenants]') AND name = 'CustomLogoUrl')
BEGIN
    ALTER TABLE [dbo].[Tenants]
    ADD [CustomLogoUrl] NVARCHAR(500) NULL;
    
    PRINT 'Added column: Tenants.CustomLogoUrl';
END
ELSE
    PRINT 'Column Tenants.CustomLogoUrl already exists, skipping.';

-- Add CustomApplicationName column if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Tenants]') AND name = 'CustomApplicationName')
BEGIN
    ALTER TABLE [dbo].[Tenants]
    ADD [CustomApplicationName] NVARCHAR(100) NULL;
    
    PRINT 'Added column: Tenants.CustomApplicationName';
END
ELSE
    PRINT 'Column Tenants.CustomApplicationName already exists, skipping.';

-- Add CustomFaviconUrl column if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Tenants]') AND name = 'CustomFaviconUrl')
BEGIN
    ALTER TABLE [dbo].[Tenants]
    ADD [CustomFaviconUrl] NVARCHAR(500) NULL;
    
    PRINT 'Added column: Tenants.CustomFaviconUrl';
END
ELSE
    PRINT 'Column Tenants.CustomFaviconUrl already exists, skipping.';

-- =============================================
-- 3. Create Performance Index for Custom Branding
-- =============================================

-- Create index on custom branding columns for efficient lookup
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tenants_CustomBranding' AND object_id = OBJECT_ID(N'[dbo].[Tenants]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Tenants_CustomBranding] 
    ON [dbo].[Tenants] ([Id])
    INCLUDE ([CustomLogoUrl], [CustomApplicationName], [CustomFaviconUrl]);
    
    PRINT 'Created index: IX_Tenants_CustomBranding';
END
ELSE
    PRINT 'Index IX_Tenants_CustomBranding already exists, skipping.';

-- =============================================
-- 4. Add Extended Properties for Documentation
-- =============================================

-- Add descriptions to the new columns
IF NOT EXISTS (
    SELECT 1 FROM sys.extended_properties 
    WHERE major_id = OBJECT_ID(N'[dbo].[Tenants]') 
    AND minor_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Tenants]') AND name = 'CustomLogoUrl')
    AND name = 'MS_Description'
)
BEGIN
    EXEC sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Custom logo URL override for this tenant', 
        @level0type = N'SCHEMA', @level0name = 'dbo',
        @level1type = N'TABLE', @level1name = 'Tenants',
        @level2type = N'COLUMN', @level2name = 'CustomLogoUrl';
    
    PRINT 'Added description for Tenants.CustomLogoUrl';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.extended_properties 
    WHERE major_id = OBJECT_ID(N'[dbo].[Tenants]') 
    AND minor_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Tenants]') AND name = 'CustomApplicationName')
    AND name = 'MS_Description'
)
BEGIN
    EXEC sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Custom application name override for this tenant', 
        @level0type = N'SCHEMA', @level0name = 'dbo',
        @level1type = N'TABLE', @level1name = 'Tenants',
        @level2type = N'COLUMN', @level2name = 'CustomApplicationName';
    
    PRINT 'Added description for Tenants.CustomApplicationName';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.extended_properties 
    WHERE major_id = OBJECT_ID(N'[dbo].[Tenants]') 
    AND minor_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Tenants]') AND name = 'CustomFaviconUrl')
    AND name = 'MS_Description'
)
BEGIN
    EXEC sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'Custom favicon URL override for this tenant', 
        @level0type = N'SCHEMA', @level0name = 'dbo',
        @level1type = N'TABLE', @level1name = 'Tenants',
        @level2type = N'COLUMN', @level2name = 'CustomFaviconUrl';
    
    PRINT 'Added description for Tenants.CustomFaviconUrl';
END

-- =============================================
-- Migration Complete
-- =============================================
PRINT '';
PRINT '==============================================';
PRINT 'Migration 20260202_AddBrandingConfiguration completed successfully!';
PRINT '==============================================';
GO
