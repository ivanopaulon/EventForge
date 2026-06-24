-- Migration: 20260624_AddSupplierGrossPriceToDocumentRow
-- Adds SupplierGrossPrice (nullable) to DocumentRows.
-- Stores the supplier catalogue price (before chained trade discounts) for purchase documents.
-- UnitPrice remains the net base price; SupplierGrossPrice is an audit/history field.

ALTER TABLE "DocumentRows"
    ADD COLUMN IF NOT EXISTS "SupplierGrossPrice" NUMERIC(18,6) NULL;

COMMENT ON COLUMN "DocumentRows"."SupplierGrossPrice"
    IS 'Supplier catalogue price before chained trade discounts (purchase documents only). Null for sales documents.';
