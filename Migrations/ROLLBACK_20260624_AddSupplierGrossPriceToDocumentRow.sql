-- ROLLBACK: 20260624_AddSupplierGrossPriceToDocumentRow
-- Removes the SupplierGrossPrice column added by 20260624_AddSupplierGrossPriceToDocumentRow.sql.

ALTER TABLE "DocumentRows"
    DROP COLUMN IF EXISTS "SupplierGrossPrice";
