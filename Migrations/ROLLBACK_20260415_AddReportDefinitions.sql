-- Rollback Migration: AddReportDefinitions
-- Date: 2026-04-15

-- Drop data sources first (FK dependency)
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReportDataSources]') AND type IN (N'U'))
BEGIN
    DROP TABLE [dbo].[ReportDataSources];
    PRINT 'Dropped table ReportDataSources';
END
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReportDefinitions]') AND type IN (N'U'))
BEGIN
    DROP TABLE [dbo].[ReportDefinitions];
    PRINT 'Dropped table ReportDefinitions';
END
GO

PRINT 'Rollback of 20260415_AddReportDefinitions completed.';
GO
