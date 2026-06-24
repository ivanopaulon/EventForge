-- Rollback: 20260624_AddLineDiscountStringToDocumentRow
-- Removes the LineDiscountString column added to DocumentRows.

ALTER TABLE DocumentRows
    DROP COLUMN LineDiscountString;
