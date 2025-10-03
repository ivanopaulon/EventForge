# Inventory Document Workflow - Implementation Guide

## Overview

This document explains the new inventory document workflow that allows creating a single inventory document and adding multiple rows to it as products are scanned and counted.

## Background

The previous inventory procedure created individual inventory entries for each product scan, without a unified document to track the entire inventory session. The new workflow addresses this by:

1. Creating a single inventory document at the start of the inventory session
2. Adding rows to this document as products are scanned
3. Finalizing the document when the inventory is complete

## API Endpoints

### 1. Start Inventory Document

**Endpoint:** `POST /api/v1/warehouse/inventory/document/start`

Creates a new inventory document to track the inventory session.

**Request Body:**
```json
{
  "warehouseId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "inventoryDate": "2025-01-15T10:00:00Z",
  "notes": "Q1 2025 Physical Inventory",
  "series": "INV",
  "number": "INV-001"
}
```

**Fields:**
- `warehouseId` (optional): Warehouse where the inventory is being conducted
- `inventoryDate` (required): Date of the inventory
- `notes` (optional): Notes about this inventory session
- `series` (optional): Document series for numbering
- `number` (optional): Document number (auto-generated if not provided)

**Response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "number": "INV-20250115-100000",
  "series": "INV",
  "inventoryDate": "2025-01-15T10:00:00Z",
  "warehouseId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "warehouseName": "Main Warehouse",
  "status": "Draft",
  "notes": "Q1 2025 Physical Inventory",
  "createdAt": "2025-01-15T10:00:00Z",
  "createdBy": "mario.rossi",
  "finalizedAt": null,
  "finalizedBy": null,
  "rows": [],
  "totalItems": 0
}
```

### 2. Add Row to Inventory Document

**Endpoint:** `POST /api/v1/warehouse/inventory/document/{documentId}/row`

Adds a product count row to an existing inventory document.

**Request Body:**
```json
{
  "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "locationId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "quantity": 95,
  "lotId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
  "notes": "Some items damaged"
}
```

**Fields:**
- `productId` (required): Product being counted
- `locationId` (required): Storage location where the product is located
- `quantity` (required): Counted quantity
- `lotId` (optional): Lot identifier if applicable
- `notes` (optional): Notes about this specific count

**Response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "number": "INV-20250115-100000",
  "series": "INV",
  "inventoryDate": "2025-01-15T10:00:00Z",
  "warehouseId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "warehouseName": "Main Warehouse",
  "status": "Draft",
  "notes": "Q1 2025 Physical Inventory",
  "createdAt": "2025-01-15T10:00:00Z",
  "createdBy": "mario.rossi",
  "rows": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa9",
      "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "productName": "Product XYZ",
      "productCode": "PRD-001",
      "locationId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
      "locationName": "A-01-01",
      "quantity": 95,
      "previousQuantity": 100,
      "adjustmentQuantity": -5,
      "lotId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
      "lotCode": "LOT-2025-001",
      "notes": "Some items damaged",
      "createdAt": "2025-01-15T10:05:00Z",
      "createdBy": "mario.rossi"
    }
  ],
  "totalItems": 1
}
```

### 3. Finalize Inventory Document

**Endpoint:** `POST /api/v1/warehouse/inventory/document/{documentId}/finalize`

Finalizes the inventory document and applies all stock adjustments.

**Response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "number": "INV-20250115-100000",
  "series": "INV",
  "inventoryDate": "2025-01-15T10:00:00Z",
  "warehouseId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "warehouseName": "Main Warehouse",
  "status": "Closed",
  "notes": "Q1 2025 Physical Inventory",
  "createdAt": "2025-01-15T10:00:00Z",
  "createdBy": "mario.rossi",
  "finalizedAt": "2025-01-15T11:00:00Z",
  "finalizedBy": "mario.rossi",
  "rows": [
    // ... all rows
  ],
  "totalItems": 25
}
```

## Workflow Example

### Step 1: Start Inventory Session

```bash
POST /api/v1/warehouse/inventory/document/start
Content-Type: application/json

{
  "warehouseId": "warehouse-guid",
  "inventoryDate": "2025-01-15T10:00:00Z",
  "notes": "Q1 2025 Physical Inventory"
}
```

**Result:** Returns inventory document with ID `doc-guid-123`

### Step 2: Scan and Count Products

For each product scanned:

```bash
POST /api/v1/warehouse/inventory/document/doc-guid-123/row
Content-Type: application/json

{
  "productId": "product-guid-1",
  "locationId": "location-guid-1",
  "quantity": 95
}
```

Repeat for each product. Each call adds a new row to the document.

### Step 3: Complete Inventory

When all products have been counted:

```bash
POST /api/v1/warehouse/inventory/document/doc-guid-123/finalize
```

This closes the document and processes all inventory adjustments.

## Technical Implementation Details

### Document Structure

The inventory document is implemented using the existing `DocumentHeader` and `DocumentRow` entities:

- **DocumentHeader**: Represents the inventory session
  - DocumentType: "INVENTORY" (auto-created)
  - Status: "Draft" → "Closed"
  - BusinessPartyId: System internal party (auto-created)

- **DocumentRow**: Represents each product count
  - ProductCode: Scanned product code
  - Description: Product name and location
  - Quantity: Counted quantity

### Auto-Created Entities

The system automatically creates:

1. **Inventory Document Type**: 
   - Code: "INVENTORY"
   - Name: "Inventory Document"
   - Created once per tenant on first use

2. **System Business Party**:
   - Name: "System Internal"
   - Type: Cliente (Customer)
   - Used for internal operations

### Benefits

1. **Single Document**: All inventory counts are tracked in one place
2. **Audit Trail**: Complete history of when and who performed the inventory
3. **Incremental Counting**: Add products one at a time as they are scanned
4. **Review Before Finalize**: Can review all counts before applying adjustments
5. **Document Management**: Leverages existing document infrastructure

## Comparison with Previous Approach

### Old Approach (Single Entry)
```
POST /api/v1/warehouse/inventory
→ Creates individual inventory entry
→ Immediately applies stock adjustment
→ No unified document
```

### New Approach (Document-Based)
```
1. POST /api/v1/warehouse/inventory/document/start
   → Creates inventory document

2. POST /api/v1/warehouse/inventory/document/{id}/row (multiple times)
   → Adds rows to document
   → Calculates adjustments but doesn't apply yet

3. POST /api/v1/warehouse/inventory/document/{id}/finalize
   → Closes document
   → Applies all stock adjustments at once
```

## Integration Notes

### For Frontend Development

1. **Start Session**: Create document when user begins inventory
2. **Scan Products**: Add rows as barcodes are scanned
3. **Display Progress**: Show document with all rows added
4. **Review**: Allow user to review counts before finalizing
5. **Complete**: Finalize document to apply adjustments

### Error Handling

- If document creation fails, no inventory data is lost
- If row addition fails, retry without affecting other rows
- If finalization fails, document remains in Draft status and can be retried

## Future Enhancements

Potential improvements to consider:

1. **Edit Rows**: Allow editing quantities before finalization
2. **Delete Rows**: Remove incorrectly scanned items
3. **Partial Finalization**: Apply adjustments for specific rows
4. **Document Templates**: Pre-configure inventory settings
5. **Barcode Integration**: Direct barcode scanning API
6. **Mobile App**: Dedicated mobile inventory app

## Related Documentation

- [INVENTORY_PROCEDURE_EXPLANATION.md](INVENTORY_PROCEDURE_EXPLANATION.md) - Previous inventory procedure
- [INVENTORY_PROCEDURE_TECHNICAL_SUMMARY.md](INVENTORY_PROCEDURE_TECHNICAL_SUMMARY.md) - Technical summary
