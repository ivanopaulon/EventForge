-- ============================================================
-- Migration: 20260402_AddAppliedPromotionsToDocumentRow
-- Description: Adds AppliedPromotionsJSON column to DocumentRows
--              for promotion traceability on document lines.
-- Date: 2026-04-02
-- ============================================================

-- Add AppliedPromotionsJSON column to DocumentRows
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[DocumentRows]') 
    AND name = 'AppliedPromotionsJSON'
)
BEGIN
    ALTER TABLE [dbo].[DocumentRows]
    ADD [AppliedPromotionsJSON] NVARCHAR(4000) NULL;
    
    PRINT 'Column AppliedPromotionsJSON added to DocumentRows.';
END
ELSE
BEGIN
    PRINT 'Column AppliedPromotionsJSON already exists in DocumentRows. Skipping.';
END

PRINT 'Migration 20260402_AddAppliedPromotionsToDocumentRow completed successfully.';
GO
