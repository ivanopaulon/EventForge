-- Migration: 20260624_AddLineDiscountStringToDocumentRow
-- Description: Adds LineDiscountString column to DocumentRows to preserve the original chained-discount
--              notation entered by the user (e.g. "10+5", "10+5+2").
--              LineDiscount (decimal) continues to hold the computed equivalent percentage used for calculations.
--              LineDiscountString is nullable: NULL means a simple single-value discount (backwards compatible).

ALTER TABLE DocumentRows
    ADD LineDiscountString NVARCHAR(50) NULL;
