# Procurement & Product Management Domain Documentation

## Overview

This document describes the procurement and product management domain model extensions implemented to support advanced product sourcing, supplier management, and inventory planning capabilities in EventForge.

## Entities

### Brand

Represents a product brand or manufacturer.

**Fields:**
- `Id` (Guid, PK): Unique identifier
- `Name` (string, required, max 200): Brand name
- `Description` (string, optional, max 1000): Brand description
- `Website` (string, optional, max 500): Brand website URL
- `Country` (string, optional, max 100): Country of origin or headquarters
- Audit fields (inherited from AuditableEntity): TenantId, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy, IsDeleted, DeletedAt, DeletedBy, IsActive, RowVersion

**Relationships:**
- One-to-Many with Model: A brand can have multiple models
- One-to-Many with Product: Products can be directly associated with a brand

**Indexes:**
- `IX_Brand_Name` on Name field for fast brand lookups

**Soft Delete:** ✅ Enabled (via AuditableEntity inheritance)

---

### Model

Represents a product model within a brand.

**Fields:**
- `Id` (Guid, PK): Unique identifier
- `BrandId` (Guid, FK, required): Reference to Brand
- `Name` (string, required, max 200): Model name
- `Description` (string, optional, max 1000): Model description
- `ManufacturerPartNumber` (string, optional, max 100): Manufacturer part number (MPN)
- Audit fields (inherited from AuditableEntity)

**Relationships:**
- Many-to-One with Brand: Each model belongs to a brand (with FK constraint, OnDelete: Restrict)
- One-to-Many with Product: Products can be associated with a specific model

**Indexes:**
- `IX_Model_BrandId_Name` composite index on (BrandId, Name) for fast model lookups within a brand

**Soft Delete:** ✅ Enabled (via AuditableEntity inheritance)

---

### ProductSupplier

Represents the relationship between a product and its suppliers, including supplier-specific information such as pricing, lead times, and ordering constraints.

**Fields:**
- `Id` (Guid, PK): Unique identifier
- `ProductId` (Guid, FK, required): Reference to Product
- `SupplierId` (Guid, FK, required): Reference to BusinessParty (must be Fornitore or ClienteFornitore)
- `SupplierProductCode` (string, optional, max 100): Supplier's product code/SKU
- `PurchaseDescription` (string, optional, max 500): Purchase description specific to this supplier
- `UnitCost` (decimal(18,6), optional): Unit cost from this supplier
- `Currency` (string, optional, max 10): Currency for the unit cost
- `MinOrderQty` (int, optional): Minimum order quantity
- `IncrementQty` (int, optional): Order quantity increment (must order in multiples)
- `LeadTimeDays` (int, optional): Lead time in days for delivery
- `LastPurchasePrice` (decimal(18,6), optional): Last purchase price
- `LastPurchaseDate` (DateTime, optional): Date of last purchase
- `Preferred` (bool): Indicates if this is the preferred supplier (default: false)
- `Notes` (string, optional, max 1000): Additional notes
- Audit fields (inherited from AuditableEntity)

**Relationships:**
- Many-to-One with Product (with FK constraint, OnDelete: Cascade)
- Many-to-One with BusinessParty as Supplier (with FK constraint, OnDelete: Restrict)

**Indexes:**
- `IX_ProductSupplier_ProductId` on ProductId
- `IX_ProductSupplier_SupplierId` on SupplierId
- `IX_ProductSupplier_ProductId_Preferred` composite index on (ProductId, Preferred) for fast preferred supplier queries

**Business Rules:**
- Only one ProductSupplier record per ProductId can have `Preferred = true`
- The SupplierId must reference a BusinessParty with PartyType = Fornitore or ClienteFornitore

**Soft Delete:** ✅ Enabled (via AuditableEntity inheritance)

---

### Product (Extended)

The existing Product entity has been extended with the following new fields:

**New Fields:**
- `BrandId` (Guid, optional): Reference to Brand
- `ModelId` (Guid, optional): Reference to Model
- `PreferredSupplierId` (Guid, optional): Reference to the preferred supplier's SupplierId
- `ReorderPoint` (decimal(18,6), optional): Inventory level at which to trigger reordering
- `SafetyStock` (decimal(18,6), optional): Minimum stock level to maintain as a buffer
- `TargetStockLevel` (decimal(18,6), optional): Desired inventory level for optimal stocking
- `AverageDailyDemand` (decimal(18,6), optional): Average daily demand for inventory planning calculations

**New Relationships:**
- Many-to-One with Brand (with FK constraint, OnDelete: Restrict)
- Many-to-One with Model (with FK constraint, OnDelete: Restrict)
- One-to-Many with ProductSupplier: A product can have multiple suppliers

**New Indexes:**
- `IX_Product_BrandId` on BrandId
- `IX_Product_ModelId` on ModelId
- `IX_Product_PreferredSupplierId` on PreferredSupplierId

**Business Rules:**
- If `IsBundle = true`, the product cannot have suppliers (ProductSupplier records)
- This constraint must be enforced at the service/validation layer

---

## Database Schema

### Tables Created

1. **Brands**
   - Primary Key: Id
   - Index: IX_Brand_Name

2. **Models**
   - Primary Key: Id
   - Foreign Key: BrandId → Brands(Id)
   - Index: IX_Model_BrandId_Name

3. **ProductSuppliers**
   - Primary Key: Id
   - Foreign Key: ProductId → Products(Id) with CASCADE DELETE
   - Foreign Key: SupplierId → BusinessParties(Id) with RESTRICT DELETE
   - Indexes: IX_ProductSupplier_ProductId, IX_ProductSupplier_SupplierId, IX_ProductSupplier_ProductId_Preferred

### Tables Modified

1. **Products**
   - Added columns: BrandId, ModelId, PreferredSupplierId, ReorderPoint, SafetyStock, TargetStockLevel, AverageDailyDemand
   - Foreign Keys: BrandId → Brands(Id), ModelId → Models(Id)
   - Indexes: IX_Product_BrandId, IX_Product_ModelId, IX_Product_PreferredSupplierId

---

## Data Transfer Objects (DTOs)

### Brand DTOs
- `BrandDto`: Output DTO with Id, Name, Description, Website, Country, audit info
- `CreateBrandDto`: Input DTO for creating a new brand
- `UpdateBrandDto`: Input DTO for updating an existing brand

### Model DTOs
- `ModelDto`: Output DTO with Id, BrandId, BrandName, Name, Description, ManufacturerPartNumber, audit info
- `CreateModelDto`: Input DTO for creating a new model (requires BrandId)
- `UpdateModelDto`: Input DTO for updating an existing model

### ProductSupplier DTOs
- `ProductSupplierDto`: Output DTO with all supplier relationship information including ProductName and SupplierName
- `CreateProductSupplierDto`: Input DTO for creating a new product-supplier relationship
- `UpdateProductSupplierDto`: Input DTO for updating an existing product-supplier relationship

### Updated Product DTOs
- `CreateProductDto`: Extended with BrandId, ModelId, PreferredSupplierId, and inventory reorder fields
- `UpdateProductDto`: Extended with the same new fields
- `ProductDetailDto`: Extended to include BrandName, ModelName, PreferredSupplierName, inventory fields, and a Suppliers collection

---

## Business Logic & Validation Rules

### Preferred Supplier Constraint

**Rule:** Only one ProductSupplier per Product can have `Preferred = true`.

**Implementation Strategy:**
1. Service layer validation: Before setting a supplier as preferred, check if another supplier is already marked as preferred for the same product
2. If another preferred supplier exists, either:
   - Reject the operation with a validation error, OR
   - Automatically set the existing preferred supplier to `Preferred = false` before setting the new one

**Recommended Approach:** Automatic reset of previous preferred supplier for better UX.

### Bundle Cannot Have Suppliers

**Rule:** If a Product has `IsBundle = true`, it cannot have associated ProductSupplier records.

**Implementation Strategy:**
1. Service layer validation: When creating/updating a ProductSupplier, verify that the associated Product has `IsBundle = false`
2. When setting `IsBundle = true` on a Product, verify that it has no associated ProductSupplier records
3. Reject operations that violate this constraint with appropriate validation errors

### Supplier BusinessParty Validation

**Rule:** ProductSupplier.SupplierId must reference a BusinessParty with `PartyType = Fornitore` or `PartyType = ClienteFornitore`.

**Implementation Strategy:**
1. Service layer validation: When creating/updating a ProductSupplier, verify the BusinessParty has the correct PartyType
2. Reject operations with invalid supplier types with appropriate validation errors

---

## Migration

**Migration Name:** `AddBrandModelProductSupplierEntities`

**Migration Actions:**
1. Create Brands table
2. Create Models table with FK to Brands
3. Create ProductSuppliers table with FKs to Products and BusinessParties
4. Alter Products table to add new columns (BrandId, ModelId, PreferredSupplierId, inventory fields)
5. Add Foreign Keys from Products to Brands and Models
6. Create all required indexes

**Rollback:** The migration includes a Down method that reverses all changes.

---

## Usage Examples

### Creating a Brand and Model

```csharp
// Create a brand
var brandDto = new CreateBrandDto
{
    Name = "Samsung",
    Description = "South Korean multinational electronics corporation",
    Website = "https://www.samsung.com",
    Country = "South Korea"
};
var brand = await brandService.CreateAsync(brandDto);

// Create a model for the brand
var modelDto = new CreateModelDto
{
    BrandId = brand.Id,
    Name = "Galaxy S23",
    Description = "Flagship smartphone series",
    ManufacturerPartNumber = "SM-S911"
};
var model = await modelService.CreateAsync(modelDto);
```

### Associating a Product with Brand and Model

```csharp
var productDto = new CreateProductDto
{
    Name = "Samsung Galaxy S23 Ultra 256GB",
    Code = "SGS23U-256",
    BrandId = brand.Id,
    ModelId = model.Id,
    DefaultPrice = 1199.99m,
    // ... other fields
};
var product = await productService.CreateAsync(productDto);
```

### Adding Suppliers to a Product

```csharp
// Add primary supplier
var supplierDto = new CreateProductSupplierDto
{
    ProductId = product.Id,
    SupplierId = primarySupplier.Id,
    SupplierProductCode = "SAMS-GS23U-256",
    UnitCost = 899.99m,
    Currency = "EUR",
    MinOrderQty = 10,
    LeadTimeDays = 7,
    Preferred = true
};
await productSupplierService.CreateAsync(supplierDto);

// Add alternative supplier
var altSupplierDto = new CreateProductSupplierDto
{
    ProductId = product.Id,
    SupplierId = alternativeSupplier.Id,
    UnitCost = 919.99m,
    Currency = "EUR",
    MinOrderQty = 5,
    LeadTimeDays = 14,
    Preferred = false // Not preferred
};
await productSupplierService.CreateAsync(altSupplierDto);
```

### Setting Inventory Reorder Parameters

```csharp
var updateDto = new UpdateProductDto
{
    // ... existing fields
    ReorderPoint = 20m,      // Reorder when stock falls below 20 units
    SafetyStock = 10m,        // Always maintain at least 10 units
    TargetStockLevel = 50m,   // Optimal stock level is 50 units
    AverageDailyDemand = 5m   // Sell approximately 5 units per day
};
await productService.UpdateAsync(productId, updateDto);
```

---

## Future Enhancements

Potential future improvements to the procurement domain:

1. **Purchase Order Management**: Create purchase orders based on reorder points and supplier information
2. **Supplier Performance Tracking**: Track on-time delivery, quality metrics, and pricing trends
3. **Automatic Reorder Suggestions**: Algorithm to suggest when to reorder based on demand patterns and lead times
4. **Multi-Currency Support**: Enhanced currency conversion and pricing in multiple currencies
5. **Supplier Contracts**: Track contract terms, volume discounts, and special pricing agreements
6. **RFQ/RFP Management**: Request for Quote/Proposal workflows for competitive bidding
7. **Supplier Ratings & Reviews**: Internal rating system for supplier performance
8. **Cost History Tracking**: Track cost changes over time for better financial analysis

---

## Implementation Checklist

- [x] Create Brand entity
- [x] Create Model entity
- [x] Create ProductSupplier entity
- [x] Extend Product entity with new fields
- [x] Update DbContext with new DbSets
- [x] Configure entity relationships in OnModelCreating
- [x] Add indexes for performance
- [x] Configure decimal precision
- [x] Create EF Core migration
- [x] Create Brand DTOs
- [x] Create Model DTOs
- [x] Create ProductSupplier DTOs
- [x] Update Product DTOs
- [x] Document entities and relationships
- [ ] Implement business validation rules (preferred supplier constraint)
- [ ] Implement business validation rules (bundle cannot have suppliers)
- [ ] Implement business validation rules (supplier PartyType validation)
- [ ] Create service layer for Brand CRUD operations
- [ ] Create service layer for Model CRUD operations
- [ ] Create service layer for ProductSupplier CRUD operations
- [ ] Create API controllers for new entities
- [ ] Add unit tests for validation rules
- [ ] Add integration tests for CRUD operations
- [ ] Update API documentation

---

*Document Version: 1.0*  
*Last Updated: September 30, 2024*  
*Related Issue: #353*
