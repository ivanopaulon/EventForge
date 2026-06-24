-- Migration: 20260624_AddSupplierGrossPriceToDocumentRow
-- Adds SupplierGrossPrice (nullable) to DocumentRows.
-- Stores the supplier catalogue price (before chained trade discounts) for purchase documents.
-- UnitPrice remains the net base price; SupplierGrossPrice is an audit/history field.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[DocumentRows]') AND name = N'SupplierGrossPrice'
)
BEGIN
    ALTER TABLE [DocumentRows]
        ADD [SupplierGrossPrice] DECIMAL(18,6) NULL;
END
