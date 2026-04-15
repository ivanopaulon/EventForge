-- Migration: Add ReportDefinitions and ReportDataSources tables for Bold Reports integration
-- Date: 2026-04-15
-- Description: Creates the tables backing EventForge.Server.Data.Entities.Reports.ReportDefinition
--              and ReportDataSource entities. Report definitions store RDLC XML content saved
--              by the Bold Reports JavaScript designer. Data sources declare the EF entity types
--              that back each report.

-- ── ReportDefinitions ──────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[ReportDefinitions]') AND type IN (N'U')
)
BEGIN
    CREATE TABLE [dbo].[ReportDefinitions]
    (
        [Id]            uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        [TenantId]      uniqueidentifier NOT NULL,
        [Name]          nvarchar(200)    NOT NULL,
        [Description]   nvarchar(1000)   NULL,
        [Category]      nvarchar(100)    NULL,
        [ReportContent] nvarchar(MAX)    NULL,   -- RDLC XML saved by the Bold Reports designer
        [IsPublic]      bit              NOT NULL DEFAULT 0,

        -- AuditableEntity columns
        [IsActive]      bit              NOT NULL DEFAULT 1,
        [IsDeleted]     bit              NOT NULL DEFAULT 0,
        [CreatedAt]     datetime2(7)     NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]     nvarchar(100)    NULL,
        [ModifiedAt]    datetime2(7)     NULL,
        [ModifiedBy]    nvarchar(100)    NULL,
        [DeletedAt]     datetime2(7)     NULL,
        [DeletedBy]     nvarchar(100)    NULL,
        [RowVersion]    rowversion       NULL,

        CONSTRAINT [PK_ReportDefinitions] PRIMARY KEY ([Id])
    );

    PRINT 'Created table ReportDefinitions';
END
GO

-- ── ReportDataSources ──────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[ReportDataSources]') AND type IN (N'U')
)
BEGIN
    CREATE TABLE [dbo].[ReportDataSources]
    (
        [Id]                   uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        [TenantId]             uniqueidentifier NOT NULL,
        [ReportDefinitionId]   uniqueidentifier NOT NULL,
        [DataSourceName]       nvarchar(100)    NOT NULL,
        [EntityType]           nvarchar(100)    NOT NULL,
        [Description]          nvarchar(500)    NULL,

        -- AuditableEntity columns
        [IsActive]             bit              NOT NULL DEFAULT 1,
        [IsDeleted]            bit              NOT NULL DEFAULT 0,
        [CreatedAt]            datetime2(7)     NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy]            nvarchar(100)    NULL,
        [ModifiedAt]           datetime2(7)     NULL,
        [ModifiedBy]           nvarchar(100)    NULL,
        [DeletedAt]            datetime2(7)     NULL,
        [DeletedBy]            nvarchar(100)    NULL,
        [RowVersion]           rowversion       NULL,

        CONSTRAINT [PK_ReportDataSources] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReportDataSources_ReportDefinitions]
            FOREIGN KEY ([ReportDefinitionId]) REFERENCES [dbo].[ReportDefinitions]([Id])
            ON DELETE CASCADE
    );

    PRINT 'Created table ReportDataSources';
END
GO

-- ── Indexes ────────────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'IX_ReportDefinitions_TenantId_Category_IsDeleted'
      AND object_id = OBJECT_ID('dbo.ReportDefinitions')
)
BEGIN
    CREATE INDEX [IX_ReportDefinitions_TenantId_Category_IsDeleted]
        ON [dbo].[ReportDefinitions] ([TenantId], [Category], [IsDeleted]);
    PRINT 'Created index IX_ReportDefinitions_TenantId_Category_IsDeleted';
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'IX_ReportDefinitions_TenantId_IsActive_IsDeleted'
      AND object_id = OBJECT_ID('dbo.ReportDefinitions')
)
BEGIN
    CREATE INDEX [IX_ReportDefinitions_TenantId_IsActive_IsDeleted]
        ON [dbo].[ReportDefinitions] ([TenantId], [IsActive], [IsDeleted]);
    PRINT 'Created index IX_ReportDefinitions_TenantId_IsActive_IsDeleted';
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'IX_ReportDataSources_ReportDefinitionId_DataSourceName'
      AND object_id = OBJECT_ID('dbo.ReportDataSources')
)
BEGIN
    CREATE UNIQUE INDEX [IX_ReportDataSources_ReportDefinitionId_DataSourceName]
        ON [dbo].[ReportDataSources] ([ReportDefinitionId], [DataSourceName])
        WHERE [IsDeleted] = 0;
    PRINT 'Created unique index IX_ReportDataSources_ReportDefinitionId_DataSourceName';
END
GO

-- ── Verification ───────────────────────────────────────────────────────────────────────────────

SELECT
    t.name AS TableName,
    p.rows AS RowCount
FROM sys.tables t
INNER JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0, 1)
WHERE t.name IN ('ReportDefinitions', 'ReportDataSources')
ORDER BY t.name;
GO

PRINT 'Migration 20260415_AddReportDefinitions completed successfully.';
GO
