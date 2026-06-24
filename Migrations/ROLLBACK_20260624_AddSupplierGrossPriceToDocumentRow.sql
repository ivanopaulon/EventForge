-- ROLLBACK: 20260624_AddSupplierGrossPriceToDocumentRow
-- Removes the SupplierGrossPrice column added by 20260624_AddSupplierGrossPriceToDocumentRow.sql.

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[DocumentRows]') AND name = N'SupplierGrossPrice'
)
BEGIN
    ALTER TABLE [DocumentRows]
        DROP COLUMN [SupplierGrossPrice];
END
