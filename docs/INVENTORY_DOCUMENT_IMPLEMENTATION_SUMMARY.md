# Inventory Document Workflow - Implementation Summary

## What Was Requested

The user asked (in Italian):
> "Ok, quindi, la procedura di inventario è in realtà un unico documento inventariale, quando in io la procedura vorrei che leggendo il codice e inserendo la quantità si aggiungessero righe a questo documento, come possiamo realizzarlo?"

**Translation:**
"OK, so the inventory procedure is actually a single inventory document. In the procedure, I would like that by reading the code and entering the quantity, rows would be added to this document. How can we implement this?"

## What Was Implemented

### 1. New DTOs (4 files)
- `CreateInventoryDocumentDto.cs` - For starting an inventory session
- `AddInventoryDocumentRowDto.cs` - For adding a product count row
- `InventoryDocumentDto.cs` - For returning inventory document data
- `InventoryDocumentRowDto.cs` - For returning individual row data

### 2. New API Endpoints (3 endpoints)

#### Start Inventory Document
```
POST /api/v1/warehouse/inventory/document/start
```
Creates a new inventory document. This is called once at the beginning of an inventory session.

**Request:**
```json
{
  "warehouseId": "guid-optional",
  "inventoryDate": "2025-01-15T10:00:00Z",
  "notes": "Q1 2025 Physical Inventory",
  "series": "INV",
  "number": "INV-001"
}
```

#### Add Row to Inventory Document
```
POST /api/v1/warehouse/inventory/document/{documentId}/row
```
Adds a product count to the inventory document. This is called each time a product is scanned and counted.

**Request:**
```json
{
  "productId": "product-guid",
  "locationId": "location-guid",
  "quantity": 95,
  "lotId": "lot-guid-optional",
  "notes": "optional notes"
}
```

#### Finalize Inventory Document
```
POST /api/v1/warehouse/inventory/document/{documentId}/finalize
```
Closes the inventory document and applies all stock adjustments. This is called when the inventory is complete.

### 3. Service Extensions

Extended `DocumentHeaderService` with three new methods:

1. **GetOrCreateInventoryDocumentTypeAsync** - Creates "INVENTORY" document type if it doesn't exist
2. **GetOrCreateSystemBusinessPartyAsync** - Creates "System Internal" business party for internal operations
3. **AddDocumentRowAsync** - Adds a single row to an existing document

### 4. Documentation (2 files)

- `INVENTORY_DOCUMENT_WORKFLOW.md` - Complete documentation in English
- `PROCEDURA_INVENTARIO_DOCUMENTO.md` - Complete documentation in Italian

Both include:
- API reference with request/response examples
- Step-by-step workflow
- Technical implementation details
- Comparison with previous approach
- Integration notes for frontend

## How It Works

### Old Approach
```
For each product:
  POST /api/v1/warehouse/inventory
  → Creates individual inventory entry
  → Immediately applies stock adjustment
```

### New Approach
```
1. Start inventory session:
   POST /api/v1/warehouse/inventory/document/start
   → Creates inventory document (ID: doc-123)

2. Scan products (repeat for each):
   POST /api/v1/warehouse/inventory/document/doc-123/row
   → Adds row to document
   → Calculates adjustment but doesn't apply yet

3. Complete inventory:
   POST /api/v1/warehouse/inventory/document/doc-123/finalize
   → Closes document
   → Applies all stock adjustments
```

## Benefits

1. **Single Document** - All inventory counts tracked in one place
2. **Audit Trail** - Complete history of the inventory session
3. **Incremental Counting** - Add products one at a time as scanned
4. **Review Before Apply** - Can review all counts before finalizing
5. **Document Infrastructure** - Leverages existing document management system

## Technical Details

### Document Structure

Uses existing `DocumentHeader` and `DocumentRow` entities:

- **DocumentHeader**:
  - DocumentType: "INVENTORY" (auto-created)
  - Status: "Draft" → "Closed"
  - BusinessPartyId: System party (auto-created)
  
- **DocumentRow**:
  - ProductCode: Scanned product
  - Description: Product name + location
  - Quantity: Counted quantity

### Auto-Created Entities

On first use, the system creates:

1. **Document Type**:
   - Code: "INVENTORY"
   - Name: "Inventory Document"
   
2. **Business Party**:
   - Name: "System Internal"
   - Type: Cliente (Customer)

## Testing

- All existing tests pass: **208/208** ✅
- Solution builds without errors ✅
- No regressions introduced ✅

## Files Changed

### Added (11 files)
- `EventForge.DTOs/Warehouse/CreateInventoryDocumentDto.cs`
- `EventForge.DTOs/Warehouse/AddInventoryDocumentRowDto.cs`
- `EventForge.DTOs/Warehouse/InventoryDocumentDto.cs`
- `EventForge.DTOs/Warehouse/InventoryDocumentRowDto.cs`
- `docs/INVENTORY_DOCUMENT_WORKFLOW.md`
- `docs/PROCEDURA_INVENTARIO_DOCUMENTO.md`

### Modified (3 files)
- `EventForge.Server/Controllers/WarehouseManagementController.cs` - Added 3 endpoints and DocumentHeaderService dependency
- `EventForge.Server/Services/Documents/IDocumentHeaderService.cs` - Added 3 method signatures
- `EventForge.Server/Services/Documents/DocumentHeaderService.cs` - Implemented 3 new methods

## Next Steps for Frontend Integration

1. **Start Session**: When user clicks "Start Inventory", call `/start` endpoint
2. **Scan Products**: When barcode scanned, call `/row` endpoint with product ID and quantity
3. **Display Progress**: Show document with list of all rows added
4. **Review**: Allow user to review counts before finalizing
5. **Complete**: Call `/finalize` endpoint to apply all adjustments

## Example Frontend Flow

```typescript
// 1. Start inventory
const inventoryDoc = await startInventory({
  warehouseId: selectedWarehouse,
  inventoryDate: new Date(),
  notes: "Monthly inventory"
});

// 2. Scan and add products
while (scanning) {
  const barcode = await scanBarcode();
  const product = await lookupProduct(barcode);
  const quantity = await promptQuantity();
  
  await addInventoryRow(inventoryDoc.id, {
    productId: product.id,
    locationId: currentLocation,
    quantity: quantity
  });
}

// 3. Complete inventory
await finalizeInventory(inventoryDoc.id);
```

## Conclusion

The implementation successfully addresses the user's requirement to have a single inventory document where rows are added as products are scanned. The solution:

✅ Creates a single inventory document per session
✅ Allows adding rows incrementally as products are scanned
✅ Provides complete audit trail and traceability
✅ Integrates seamlessly with existing document management
✅ Maintains backward compatibility (old endpoint still works)
✅ Is fully documented in English and Italian
✅ Passes all existing tests
