-- Migration: Add ClosureType, FiscalClosurePending and PrinterErrors to DailyClosureRecords
-- Date: 2026-04-18
-- Description: Extends DailyClosureRecords to support closures performed without a fiscal printer
--              (DB-only or non-fiscal thermal), and to track when the hardware Z-report is still
--              pending because the printer was unreachable at closure time.

-- ── ClosureType (Fiscale | NonFiscale | SoloDatabase) ────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[DailyClosureRecords]')
      AND name = 'ClosureType'
)
BEGIN
    ALTER TABLE [dbo].[DailyClosureRecords]
        ADD [ClosureType] NVARCHAR(50) NOT NULL CONSTRAINT [DF_DailyClosureRecords_ClosureType] DEFAULT N'Fiscale';
END
GO

-- ── FiscalClosurePending ──────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[DailyClosureRecords]')
      AND name = 'FiscalClosurePending'
)
BEGIN
    ALTER TABLE [dbo].[DailyClosureRecords]
        ADD [FiscalClosurePending] BIT NOT NULL CONSTRAINT [DF_DailyClosureRecords_FiscalClosurePending] DEFAULT 0;
END
GO

-- ── PrinterErrors ─────────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[DailyClosureRecords]')
      AND name = 'PrinterErrors'
)
BEGIN
    ALTER TABLE [dbo].[DailyClosureRecords]
        ADD [PrinterErrors] NVARCHAR(500) NULL;
END
GO
