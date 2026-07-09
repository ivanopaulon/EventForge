-- Migration: Add Status to Promotions
-- Date: 2026-07-09
-- Description: Adds Status column (Draft/Active/Suspended/Archived) to Promotions, replacing the pure date-based activity check.

IF COL_LENGTH('dbo.Promotions', 'Status') IS NULL
BEGIN
    ALTER TABLE [dbo].[Promotions]
        ADD [Status] int NOT NULL DEFAULT 1; -- 1 = Active, per allineare le promozioni esistenti allo stato attuale

    PRINT 'Status column added to Promotions.';
END
ELSE
BEGIN
    PRINT 'Status column already exists on Promotions — skipping.';
END
