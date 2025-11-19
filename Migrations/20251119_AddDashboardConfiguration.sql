-- Migration: Add Dashboard Configuration Tables
-- Date: 2025-11-19
-- Description: Add tables for storing user dashboard configurations and metric settings

-- Create DashboardConfigurations table
CREATE TABLE [dbo].[DashboardConfigurations] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [TenantId] UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [EntityType] NVARCHAR(100) NOT NULL,
    [IsDefault] BIT NOT NULL DEFAULT 0,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedAt] DATETIME2 NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [DeletedAt] DATETIME2 NULL,
    [DeletedBy] NVARCHAR(100) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [FK_DashboardConfigurations_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
);

-- Create index on UserId and EntityType for efficient lookup
CREATE NONCLUSTERED INDEX [IX_DashboardConfigurations_UserId_EntityType] 
ON [dbo].[DashboardConfigurations] ([UserId], [EntityType])
INCLUDE ([IsDefault], [IsDeleted]);

-- Create DashboardMetricConfigs table
CREATE TABLE [dbo].[DashboardMetricConfigs] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [TenantId] UNIQUEIDENTIFIER NOT NULL,
    [DashboardConfigurationId] UNIQUEIDENTIFIER NOT NULL,
    [Title] NVARCHAR(100) NOT NULL,
    [Type] INT NOT NULL, -- 0=Count, 1=Sum, 2=Average, 3=Min, 4=Max
    [FieldName] NVARCHAR(100) NULL,
    [FilterCondition] NVARCHAR(500) NULL,
    [Format] NVARCHAR(20) NULL,
    [Icon] NVARCHAR(100) NULL,
    [Color] NVARCHAR(50) NULL,
    [Description] NVARCHAR(200) NULL,
    [Order] INT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedAt] DATETIME2 NULL,
    [ModifiedBy] NVARCHAR(100) NULL,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [DeletedAt] DATETIME2 NULL,
    [DeletedBy] NVARCHAR(100) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [RowVersion] ROWVERSION NOT NULL,
    CONSTRAINT [FK_DashboardMetricConfigs_DashboardConfigurations_DashboardConfigurationId] 
        FOREIGN KEY ([DashboardConfigurationId]) REFERENCES [dbo].[DashboardConfigurations] ([Id]) ON DELETE CASCADE
);

-- Create index on DashboardConfigurationId for efficient lookup
CREATE NONCLUSTERED INDEX [IX_DashboardMetricConfigs_DashboardConfigurationId] 
ON [dbo].[DashboardMetricConfigs] ([DashboardConfigurationId])
INCLUDE ([Order], [IsDeleted]);

-- Add comment documentation
EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Stores user dashboard configurations for different entity types', 
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE', @level1name = 'DashboardConfigurations';

EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Stores metric configurations within a dashboard configuration', 
    @level0type = N'SCHEMA', @level0name = 'dbo',
    @level1type = N'TABLE', @level1name = 'DashboardMetricConfigs';
