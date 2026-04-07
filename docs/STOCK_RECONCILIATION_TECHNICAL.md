# Stock Reconciliation - Technical Documentation

## Overview

The Stock Reconciliation feature provides a comprehensive system to identify and correct discrepancies between recorded stock quantities and calculated quantities based on movement history.

## Architecture

### Component Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     Stock Reconciliation                     │
└─────────────────────────────────────────────────────────────┘
                              │
         ┌────────────────────┼────────────────────┐
         │                    │                    │
         ▼                    ▼                    ▼
┌─────────────────┐  ┌─────────────────┐  ┌──────────────────┐
│  Frontend (UI)  │  │  API Controller │  │  Service Layer   │
│  Blazor WASM    │  │  ASP.NET Core   │  │  Business Logic  │
└─────────────────┘  └─────────────────┘  └──────────────────┘
         │                    │                    │
         │                    │                    ▼
         │                    │           ┌──────────────────┐
         │                    │           │  Data Access     │
         │                    │           │  EF Core         │
         │                    │           └──────────────────┘
         │                    │                    │
         └────────────────────┴────────────────────┘
                              │
                              ▼
                     ┌─────────────────┐
                     │   Database      │
                     │   SQL Server    │
                     └─────────────────┘
```

## Data Flow

### Calculate Flow

```
1. User Input → UI Filters
2. UI → HTTP POST /api/v1/warehouse/stock-reconciliation/calculate
3. Controller → StockReconciliationService.CalculateReconciledStockAsync()
4. Service → Query Stock, DocumentRows, InventoryDocumentRows, StockMovements
5. Service → Calculate discrepancies
6. Service → Return StockReconciliationResultDto
7. Controller → Return 200 OK with results
8. UI → Display results table
```

### Apply Flow

```
1. User Selection → Selected Stock IDs + Reason
2. UI → Show Confirmation Dialog
3. User Confirms → HTTP POST /api/v1/warehouse/stock-reconciliation/apply
4. Controller → StockReconciliationService.ApplyReconciliationAsync()
5. Service → BEGIN TRANSACTION
6. Service → Update Stock.Quantity for each selected item
7. Service → Create StockMovement (type: Adjustment) if requested
8. Service → Create AuditLog entries
9. Service → COMMIT TRANSACTION
10. Service → Return StockReconciliationApplyResultDto
11. UI → Show success message with statistics
```

## Algorithm Explanation

### Calculation Algorithm

For each Stock record:

```csharp
// 1. Initialize calculated quantity
decimal calculatedQty = StartingQuantity ?? 0m;

// 2. Process Document Movements (if IncludeDocuments)
foreach (DocumentRow in period where ProductId and LocationId match)
{
    if (DocumentType.IsStockIncrease)
        calculatedQty += DocumentRow.Quantity;  // Inbound
    else
        calculatedQty -= DocumentRow.Quantity;  // Outbound
}

// 3. Process Inventory Movements (if IncludeInventories)
InventoryDocumentRow lastInventory = GetLastInventory(ProductId, LocationId, ToDate);
if (lastInventory != null)
{
    // Inventory REPLACES the calculated quantity
    calculatedQty = lastInventory.CountedQuantity;
    
    // Add documents AFTER the inventory
    foreach (DocumentRow after inventory date)
    {
        calculatedQty += (IsIncrease ? +Qty : -Qty);
    }
}

// 4. Process Manual Stock Movements
foreach (StockMovement in period)
{
    // ToLocationId matches → positive (inbound)
    // FromLocationId matches → negative (outbound)
    calculatedQty += (ToLocationId == locationId ? +Qty : -Qty);
}

// 5. Calculate discrepancy
decimal difference = calculatedQty - Stock.Quantity;
decimal diffPercentage = (calculatedQty != 0) 
    ? Math.Abs(difference) / Math.Abs(calculatedQty) * 100 
    : 0;

// 6. Determine severity
ReconciliationSeverity severity = DetermineSeverity(
    Stock.Quantity, 
    calculatedQty, 
    diffPercentage
);
```

### Severity Determination

```csharp
if (currentQuantity == 0 && calculatedQuantity > 0)
    return ReconciliationSeverity.Missing;  // 🔴 Critical

if (currentQuantity == calculatedQuantity)
    return ReconciliationSeverity.Correct;   // ✅ OK

if (differencePercentage > 10)
    return ReconciliationSeverity.Major;     // ❌ High priority

return ReconciliationSeverity.Minor;         // ⚠️ Low priority
```

## Database Schema Impact

### Tables Involved

```sql
-- Read Operations
Stock (ProductId, StorageLocationId, Quantity)
DocumentRow (ProductId, LocationId, Quantity)
DocumentHeader (DocumentTypeId, Date)
DocumentType (IsStockIncrease)
InventoryDocumentRow (ProductId, LocationId, CountedQuantity)
StockMovement (ProductId, FromLocationId, ToLocationId, Quantity)

-- Write Operations (Apply)
Stock.Quantity = UPDATE
StockMovement = INSERT (type: Adjustment)
EntityChangeLog = INSERT (audit trail)
```

### Entity Relationships

```
Stock
├─ Product (M:1)
├─ StorageLocation (M:1)
│  └─ Warehouse (M:1)
└─ Lot (M:1, optional)

DocumentRow
├─ DocumentHeader (M:1)
│  └─ DocumentType (M:1)
├─ Product (M:1)
└─ Location (M:1)

StockMovement
├─ Product (M:1)
├─ FromLocation (M:1, optional)
├─ ToLocation (M:1, optional)
└─ Lot (M:1, optional)
```

## API Reference

### POST /api/v1/warehouse/stock-reconciliation/calculate

**Request Body:**
```json
{
  "fromDate": "2024-01-01T00:00:00Z",
  "toDate": "2024-12-31T23:59:59Z",
  "warehouseId": "guid-optional",
  "locationId": "guid-optional",
  "productId": "guid-optional",
  "includeDocuments": true,
  "includeInventories": true,
  "onlyWithDiscrepancies": false,
  "startingQuantity": 0
}
```

**Response:** `200 OK`
```json
{
  "items": [
    {
      "stockId": "guid",
      "productId": "guid",
      "productCode": "PROD001",
      "productName": "Product Name",
      "warehouseName": "Main Warehouse",
      "locationCode": "A-01",
      "currentQuantity": 100,
      "calculatedQuantity": 120,
      "difference": 20,
      "differencePercentage": 16.67,
      "severity": 2,
      "sourceMovements": [...],
      "totalDocuments": 5,
      "totalInventories": 1,
      "totalManualMovements": 2
    }
  ],
  "summary": {
    "totalProducts": 100,
    "correctCount": 80,
    "minorDiscrepancyCount": 15,
    "majorDiscrepancyCount": 5,
    "missingCount": 0,
    "totalDifferenceValue": 150.50
  }
}
```

### POST /api/v1/warehouse/stock-reconciliation/apply

**Request Body:**
```json
{
  "itemsToApply": ["stock-guid-1", "stock-guid-2"],
  "reason": "Monthly reconciliation - Q4 2024",
  "createAdjustmentMovements": true
}
```

**Response:** `200 OK`
```json
{
  "updatedCount": 2,
  "movementsCreated": 2,
  "totalAdjustmentValue": 35.00,
  "updatedStockIds": ["stock-guid-1", "stock-guid-2"],
  "success": true,
  "errorMessage": null
}
```

### GET /api/v1/warehouse/stock-reconciliation/export

**Query Parameters:** Same as calculate endpoint

**Response:** `200 OK`
- Content-Type: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- File: `StockReconciliation_YYYYMMDD_HHmmss.xlsx`

## Performance Considerations

### Optimization Strategies

1. **Query Optimization:**
   - Use `.AsNoTracking()` for read-only calculate phase
   - Use `.Include()` for eager loading related entities
   - Batch queries to avoid N+1 problems

2. **Pagination:**
   - Current implementation loads all matching stocks
   - TODO: Implement server-side pagination for large datasets

3. **Indexes Required:**
   ```sql
   -- DocumentRow
   CREATE INDEX IX_DocumentRow_ProductId_LocationId_Date 
   ON DocumentRow(ProductId, LocationId, CreatedAt);
   
   -- StockMovement
   CREATE INDEX IX_StockMovement_ProductId_Date
   ON StockMovement(ProductId, MovementDate);
   ```

4. **Caching:**
   - Warehouse and Location names cached in DTO to avoid N+1
   - Consider caching DocumentType.IsStockIncrease lookups

### Scalability

- **Small datasets** (<1000 products): Instant
- **Medium datasets** (1000-10000 products): 5-30 seconds
- **Large datasets** (>10000 products): Consider pagination + background processing

## Security Considerations

### Authorization

- Required Roles: `SuperAdmin`, `Admin`, `Manager`
- License Feature: `WarehouseManagement`
- Tenant Isolation: All queries filtered by `TenantId`

### Audit Trail

Every reconciliation application creates:
1. `EntityChangeLog` entry for each Stock update
2. `StockMovement` (type: Adjustment) for traceability
3. Operation logged with reason and user

### Validation

- `ItemsToApply`: Minimum 1 item required
- `Reason`: Required, max 500 characters
- All Stock IDs validated to exist and belong to current tenant

## Error Handling

### Common Errors

1. **Missing Product/Location References:**
   - Error: DocumentRows with null ProductId or LocationId
   - Solution: Run InventoryDiagnosticService first

2. **Concurrent Modifications:**
   - Error: Stock quantity changed during apply
   - Mitigation: Transaction isolation + optimistic locking (TBD)

3. **Calculation Mismatch:**
   - Error: Results change between calculate and apply
   - Solution: Recalculate immediately before apply

## Testing Strategy

### Unit Tests Required

```csharp
// Service Tests
- Calculate_WithOnlyDocuments_ReturnsCorrectQuantity()
- Calculate_WithInventory_ReplacesQuantity()
- Calculate_WithMixedSources_AggregatesCorrectly()
- DetermineSeverity_CorrectClassification()
- Apply_WithTransaction_AtomicOperation()
- Apply_CreatesAdjustmentMovements()
- Apply_LogsAuditTrail()

// Controller Tests
- Calculate_UnauthorizedUser_Returns401()
- Calculate_ValidRequest_Returns200()
- Apply_InvalidRequest_Returns400()
- Export_ReturnsExcelFile()
```

### Integration Tests

```csharp
- FullWorkflow_CalculateApply_UpdatesStock()
- ConcurrentReconciliation_HandlesCorrectly()
- LargeDataset_PerformanceAcceptable()
```

## Known Limitations

1. **Inventory Model:**
   - TODO: Implement InventoryDocumentRow retrieval (currently placeholder)
   - Need to clarify inventory data model structure

2. **Excel Export:**
   - TODO: Implement using EPPlus or ClosedXML
   - Currently returns empty array

3. **Pagination:**
   - No server-side pagination for calculate results
   - May cause performance issues with 10,000+ products

4. **Concurrent Modifications:**
   - No optimistic locking on Stock table
   - Possible race conditions if multiple users reconcile same stock

## Future Enhancements

- [ ] Background job for large reconciliations
- [ ] Scheduled automatic reconciliations
- [ ] Email notifications for severe discrepancies
- [ ] Reconciliation history tracking
- [ ] Undo last reconciliation feature
- [ ] AI-powered discrepancy analysis

## Migration Guide

### From Manual Corrections

If currently using manual stock adjustments:

1. Run reconciliation in preview mode first
2. Compare with manual adjustments
3. Apply reconciliation
4. Deprecate manual adjustment process

### Backward Compatibility

- No breaking changes to existing Stock table
- StockMovement type "Adjustment" is new
- All existing stock operations continue to work

## Support

For technical support or bug reports:
- GitHub Issues: https://github.com/ivanopaulon/EventForge/issues
- Documentation: `/docs/STOCK_RECONCILIATION_GUIDE.md`
