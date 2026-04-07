# Database Migration: Decimal Quantities and Base Unit Fields

## Overview

This document describes the database migration required to support decimal quantities and base unit conversion in the DocumentRows table.

## Migration Required

The following schema changes must be applied to the database:

### 1. Alter Quantity Column

Change the `Quantity` column in `DocumentRows` table from `int` to `decimal(18,4)`:

```sql
-- Alter Quantity column to decimal(18,4)
ALTER TABLE DocumentRows
ALTER COLUMN Quantity decimal(18,4) NOT NULL;
```

### 2. Add New Columns

Add three new columns to support base unit conversion:

```sql
-- Add BaseQuantity column (nullable)
ALTER TABLE DocumentRows
ADD BaseQuantity decimal(18,4) NULL;

-- Add BaseUnitPrice column (nullable)
ALTER TABLE DocumentRows
ADD BaseUnitPrice decimal(18,4) NULL;

-- Add BaseUnitOfMeasureId column (nullable)
ALTER TABLE DocumentRows
ADD BaseUnitOfMeasureId uniqueidentifier NULL;
```

### 3. Create Index (Optional but Recommended)

Create an index on the new BaseUnitOfMeasureId column for better query performance:

```sql
-- Create index on BaseUnitOfMeasureId
CREATE INDEX IX_DocumentRows_BaseUnitOfMeasureId 
ON DocumentRows(BaseUnitOfMeasureId);
```

## Complete Migration Script

```sql
-- =============================================================================
-- Migration: Add Decimal Quantities and Base Unit Fields
-- Date: 2025-11-05
-- Description: Converts Quantity to decimal and adds base unit conversion fields
-- =============================================================================

-- IMPORTANT: Backup your database before running this script!

BEGIN TRANSACTION;

-- Step 1: Alter Quantity column
ALTER TABLE DocumentRows
ALTER COLUMN Quantity decimal(18,4) NOT NULL;

-- Step 2: Add new columns
ALTER TABLE DocumentRows
ADD BaseQuantity decimal(18,4) NULL;

ALTER TABLE DocumentRows
ADD BaseUnitPrice decimal(18,4) NULL;

ALTER TABLE DocumentRows
ADD BaseUnitOfMeasureId uniqueidentifier NULL;

-- Step 3: Create index
CREATE INDEX IX_DocumentRows_BaseUnitOfMeasureId 
ON DocumentRows(BaseUnitOfMeasureId);

-- Commit the transaction
COMMIT TRANSACTION;

PRINT 'Migration completed successfully!';
```

## Rollback Script

If you need to rollback the changes:

```sql
-- =============================================================================
-- Rollback: Decimal Quantities and Base Unit Fields
-- WARNING: This will truncate decimal values to integers and lose base unit data!
-- =============================================================================

BEGIN TRANSACTION;

-- Step 1: Drop index
DROP INDEX IF EXISTS IX_DocumentRows_BaseUnitOfMeasureId ON DocumentRows;

-- Step 2: Drop new columns
ALTER TABLE DocumentRows DROP COLUMN IF EXISTS BaseQuantity;
ALTER TABLE DocumentRows DROP COLUMN IF EXISTS BaseUnitPrice;
ALTER TABLE DocumentRows DROP COLUMN IF EXISTS BaseUnitOfMeasureId;

-- Step 3: Revert Quantity to int (WARNING: This truncates decimals!)
ALTER TABLE DocumentRows
ALTER COLUMN Quantity int NOT NULL;

COMMIT TRANSACTION;

PRINT 'Rollback completed!';
```

## Post-Migration Data Population (Optional)

After running the migration, you can populate the BaseQuantity for existing rows:

```sql
-- Populate BaseQuantity for existing rows with conversion factor information
UPDATE dr
SET 
    dr.BaseQuantity = dr.Quantity * pu.ConversionFactor,
    dr.BaseUnitPrice = CASE 
        WHEN pu.ConversionFactor > 0 THEN dr.UnitPrice / pu.ConversionFactor 
        ELSE dr.UnitPrice 
    END,
    dr.BaseUnitOfMeasureId = baseUnit.UnitOfMeasureId
FROM DocumentRows dr
INNER JOIN ProductUnits pu ON dr.ProductId = pu.ProductId 
    AND dr.UnitOfMeasureId = pu.UnitOfMeasureId
INNER JOIN ProductUnits baseUnit ON pu.ProductId = baseUnit.ProductId 
    AND baseUnit.ConversionFactor = 1.0 
    AND baseUnit.UnitType = 'Base'
WHERE dr.BaseQuantity IS NULL
    AND dr.IsDeleted = 0
    AND pu.IsDeleted = 0
    AND baseUnit.IsDeleted = 0;

PRINT 'BaseQuantity populated for ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows.';
```

## Using EF Core Migrations (Alternative)

If you prefer to use EF Core migrations:

1. Ensure the migration file is created (it's in the Migrations folder but may be gitignored)
2. Run the migration using dotnet ef tools:

```bash
cd EventForge.Server
dotnet ef database update
```

Or generate a SQL script:

```bash
dotnet ef migrations script --output migration.sql
```

## Verification Steps

After running the migration:

### 1. Verify Schema
```sql
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    NUMERIC_PRECISION, 
    NUMERIC_SCALE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'DocumentRows'
    AND COLUMN_NAME IN ('Quantity', 'BaseQuantity', 'BaseUnitPrice', 'BaseUnitOfMeasureId')
ORDER BY COLUMN_NAME;
```

Expected results:
| COLUMN_NAME | DATA_TYPE | NUMERIC_PRECISION | NUMERIC_SCALE | IS_NULLABLE |
|-------------|-----------|-------------------|---------------|-------------|
| BaseQuantity | decimal | 18 | 4 | YES |
| BaseUnitOfMeasureId | uniqueidentifier | NULL | NULL | YES |
| BaseUnitPrice | decimal | 18 | 4 | YES |
| Quantity | decimal | 18 | 4 | NO |

### 2. Test Document Row Creation
```sql
-- Create a test document row with decimal quantity
INSERT INTO DocumentRows (
    Id, DocumentHeaderId, Description, Quantity, UnitPrice, 
    TenantId, CreatedAt, CreatedBy, IsDeleted
)
VALUES (
    NEWID(), @TestDocumentHeaderId, 'Test Product', 2.5, 10.00,
    @TestTenantId, GETUTCDATE(), 'test-migration', 0
);

-- Verify
SELECT Id, Description, Quantity, BaseQuantity, BaseUnitPrice
FROM DocumentRows
WHERE CreatedBy = 'test-migration';

-- Clean up
DELETE FROM DocumentRows WHERE CreatedBy = 'test-migration';
```

## Pre-Migration Checklist

- [ ] Backup the database
- [ ] Test the migration script in a development/staging environment
- [ ] Review existing DocumentRows data for any issues
- [ ] Notify users of potential downtime
- [ ] Ensure application is updated to the version that supports decimal quantities

## Post-Migration Checklist

- [ ] Run verification queries
- [ ] Test document row creation with decimal quantities
- [ ] Test unit conversion functionality
- [ ] Verify inventory operations work correctly
- [ ] Monitor application logs for any errors
- [ ] Run application tests

## Breaking Changes

This migration introduces the following breaking changes:

1. **Quantity Type Change**: Client applications must update to handle decimal quantities instead of integers
2. **API Contracts**: DTOs now expect `decimal` type for Quantity field
3. **Calculations**: Any code that cast Quantity to int must be updated

All these changes have been implemented in this PR.

## Support

If you encounter issues:

1. Check application logs for detailed error messages
2. Verify the schema changes were applied correctly
3. Run the verification queries
4. Contact the development team with specific error details

## Related Changes in This PR

### Server-Side
- `EventForge.Server/Data/Entities/Documents/DocumentRow.cs` - Entity model updated
- `EventForge.Server/Data/EventForgeDbContext.cs` - DbContext configuration updated
- `EventForge.Server/Services/Documents/DocumentHeaderService.cs` - Business logic updated
- `EventForge.Server/Controllers/WarehouseManagementController.cs` - Controller updated

### DTOs
- `EventForge.DTOs/Documents/CreateDocumentRowDto.cs`
- `EventForge.DTOs/Documents/UpdateDocumentRowDto.cs`
- `EventForge.DTOs/Documents/DocumentRowDto.cs`

### Client-Side
- `EventForge.Client/Shared/Components/Dialogs/Documents/AddDocumentRowDialog.razor`
- `EventForge.Client/Pages/Management/Documents/GenericDocumentProcedure.razor`

### Tests
- `EventForge.Tests/Services/Documents/DocumentHeaderStockMovementTests.cs`
- `EventForge.Tests/Services/Documents/DocumentRowMergeTests.cs`
