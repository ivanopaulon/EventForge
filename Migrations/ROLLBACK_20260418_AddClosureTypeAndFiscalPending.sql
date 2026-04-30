-- Rollback Migration: AddClosureTypeAndFiscalPending
-- Date: 2026-04-18

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[DailyClosureRecords]') AND name = 'PrinterErrors'
)
    ALTER TABLE [dbo].[DailyClosureRecords] DROP COLUMN [PrinterErrors];
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[DailyClosureRecords]') AND name = 'FiscalClosurePending'
)
BEGIN
    ALTER TABLE [dbo].[DailyClosureRecords] DROP CONSTRAINT [DF_DailyClosureRecords_FiscalClosurePending];
    ALTER TABLE [dbo].[DailyClosureRecords] DROP COLUMN [FiscalClosurePending];
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[DailyClosureRecords]') AND name = 'ClosureType'
)
BEGIN
    ALTER TABLE [dbo].[DailyClosureRecords] DROP CONSTRAINT [DF_DailyClosureRecords_ClosureType];
    ALTER TABLE [dbo].[DailyClosureRecords] DROP COLUMN [ClosureType];
END
GO
