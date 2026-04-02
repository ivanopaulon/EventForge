-- ============================================================
-- Rollback: 20260402_AddAppliedPromotionsToDocumentRow
-- Description: Removes AppliedPromotionsJSON column from DocumentRows
-- ============================================================

IF EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[DocumentRows]') 
    AND name = 'AppliedPromotionsJSON'
)
BEGIN
    ALTER TABLE [dbo].[DocumentRows]
    DROP COLUMN [AppliedPromotionsJSON];
    
    PRINT 'Column AppliedPromotionsJSON removed from DocumentRows.';
END
ELSE
BEGIN
    PRINT 'Column AppliedPromotionsJSON does not exist in DocumentRows. Skipping rollback.';
END

PRINT 'Rollback 20260402_AddAppliedPromotionsToDocumentRow completed successfully.';
GO
