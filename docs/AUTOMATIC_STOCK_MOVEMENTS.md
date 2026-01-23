# Automatic Stock Movement Creation

## Overview
EventForge automatically creates stock movements when purchase/sales documents (DDT, invoices) are approved or closed. This ensures warehouse stock quantities are always accurate without manual intervention.

## When Stock Movements Are Created

### 1. Document Approval
When `DocumentHeaderService.ApproveDocumentAsync()` is called:
- ✅ Creates stock movements for all document rows with products
- ✅ Only if movements don't already exist (idempotent)
- ✅ Uses document date for movement timestamp

### 2. Document Close
When `DocumentHeaderService.CloseDocumentAsync()` is called:
- ✅ Creates stock movements for all document rows with products
- ✅ Only if movements don't already exist (idempotent)
- ✅ Uses document date for movement timestamp

### 3. Row Operations on Approved/Closed Documents
When rows are added/modified/deleted on already approved or closed documents:
- ✅ **Add row**: Creates immediate stock movement
- ✅ **Update row quantity**: Creates compensating movement for the difference
- ✅ **Delete row**: Creates reverse compensating movement

## Document Lifecycle Diagram

```
Draft Document
    ↓
Add product rows (no movements yet)
    ↓
Approve Document → ✅ Stock movements created
    ↓
OR
    ↓
Close Document → ✅ Stock movements created
    ↓
Stock quantities updated automatically
```

## How It Works

### Stock Increase Documents (Purchases, Returns)
**Document Types**: `DDT_ACQ`, `FATT_ACQ`, etc.
**IsStockIncrease**: `true`

When document is approved/closed:
1. For each row with a ProductId:
   - Determines destination warehouse location
   - Creates **Inbound** stock movement
   - Increases stock quantity at location

**Example:**
```
Document: DDT_ACQ (Purchase Delivery Note)
Row: Product ABC, Quantity 100
Result: +100 units to warehouse stock
```

### Stock Decrease Documents (Sales, Deliveries)
**Document Types**: `DDT_VEND`, `FATT_VEND`, etc.
**IsStockIncrease**: `false`

When document is approved/closed:
1. For each row with a ProductId:
   - Determines source warehouse location
   - Creates **Outbound** stock movement
   - Decreases stock quantity from location

**Example:**
```
Document: DDT_VEND (Sales Delivery Note)
Row: Product XYZ, Quantity 50
Result: -50 units from warehouse stock
```

### Service Documents (No Stock Impact)
**Document Types**: Service invoices, consulting, etc.
**Rows without ProductId**: Service descriptions only

When document is approved/closed:
- ❌ No stock movements created
- Rows without ProductId are skipped

## Warehouse Location Resolution

The system determines the warehouse location in this priority order:

1. **Row Level**: `DocumentRow.LocationId` (if specified)
2. **Document Level**: `DocumentHeader.DestinationWarehouseId` (for purchases) or `DocumentHeader.SourceWarehouseId` (for sales)
3. **Document Type Default**: `DocumentType.DefaultWarehouseId`
4. **First Location in Warehouse**: Ordered by location code

If no location can be determined:
- ⚠️ Warning is logged
- ⚠️ Row is skipped (no movement created)

## Duplicate Prevention (Idempotency)

The system prevents duplicate stock movements:

```csharp
// Check if movements already exist for this document
var existingMovements = await _context.StockMovements
    .Where(sm => sm.DocumentHeaderId == documentHeader.Id && !sm.IsDeleted)
    .AnyAsync(cancellationToken);

if (existingMovements)
{
    _logger.LogInformation("Stock movements already exist for document {DocumentHeaderId}. Skipping.", documentHeader.Id);
    return;
}
```

**Benefits:**
- ✅ Safe to approve/close a document multiple times
- ✅ Safe to re-run processes
- ✅ No manual cleanup required

## Compensating Movements

When modifying approved/closed documents, the system creates compensating movements:

### Quantity Increase
**Original**: Product ABC, Quantity 100  
**Updated**: Product ABC, Quantity 120  
**Result**: Additional +20 movement created

### Quantity Decrease
**Original**: Product ABC, Quantity 100  
**Updated**: Product ABC, Quantity 80  
**Result**: Reverse -20 movement created

### Row Deletion
**Original**: Product ABC, Quantity 100  
**Deleted**: Row removed  
**Result**: Full reverse -100 movement created

## Logging

The system provides comprehensive logging for troubleshooting:

```
✅ Success
[Info] Created 5 stock movements for document DDT-ACQ-001 (ID: abc-123)

⚠️ Skip (already exists)
[Info] Stock movements already exist for document DDT-ACQ-001. Skipping.

⚠️ Warning (missing location)
[Warning] Cannot create stock movement for row xyz-456: unable to resolve storage location

❌ Error
[Error] Failed to create stock movements for document DDT-ACQ-001 (ID: abc-123)
```

## Examples

### Example 1: Purchase Order Flow
```
1. Create DDT_ACQ in Draft
   → Status: Draft, Stock: No change

2. Add 5 product rows
   → Status: Draft, Stock: No change

3. Close document
   → Status: Closed
   → ✅ 5 Inbound movements created
   → ✅ Stock increased by quantities

4. Stock quantities now correct!
```

### Example 2: Sales Order Flow
```
1. Create DDT_VEND in Draft
   → Status: Draft, Stock: No change

2. Add 3 product rows
   → Status: Draft, Stock: No change

3. Approve document
   → Status: Open, ApprovalStatus: Approved
   → ✅ 3 Outbound movements created
   → ✅ Stock decreased by quantities

4. Close document
   → Status: Closed
   → ℹ️ Movements already exist, skipped

5. Stock quantities now correct!
```

### Example 3: Modify Approved Document
```
1. Document approved, movements created
   → Product A: 100 units

2. Update row quantity to 120
   → ✅ Compensating +20 movement created
   → ✅ Stock increased by 20

3. Stock quantities still correct!
```

## Backward Compatibility

### For Existing Documents
Documents created before this feature:
- ❌ Will NOT have movements created automatically
- ✅ Use Stock Reconciliation feature to fix
- ✅ Optionally run migration script (if provided)

### Migration Strategy
1. Run Stock Reconciliation for all closed documents
2. Review discrepancies
3. Approve reconciliation to create adjustment movements

## Testing

Comprehensive tests ensure correctness:

### Unit Tests
- ✅ Purchase document creates inbound movements
- ✅ Sales document creates outbound movements
- ✅ Service document (no products) creates no movements
- ✅ Duplicate prevention works correctly
- ✅ Compensating movements work for updates
- ✅ Row deletion creates reverse movements

### Integration Tests
- ✅ Full document lifecycle (Draft → Add rows → Close → Verify stock)
- ✅ Concurrent document operations don't create duplicates
- ✅ Large documents (100+ rows) process correctly

## Troubleshooting

### Issue: Stock movements not created
**Check:**
1. Is DocumentType loaded? (must include DocumentType in query)
2. Do rows have ProductId? (service rows are skipped)
3. Is warehouse location resolvable? (check logs for warnings)
4. Are movements already created? (check StockMovements table)

### Issue: Duplicate movements
**Check:**
1. Look for `existingMovements` log entries
2. Verify idempotency check is working
3. Check for concurrent operations

### Issue: Incorrect stock quantities
**Check:**
1. Review all movements for the document
2. Verify movement types (Inbound/Outbound)
3. Check for compensating movements from row updates
4. Run Stock Reconciliation to identify discrepancies

## Performance Considerations

- ✅ Batch processes rows (no additional queries per row)
- ✅ Uses `.Include()` for eager loading
- ✅ Transaction safety (all-or-nothing)
- ✅ Scales well up to 1000+ rows per document

## Configuration

No configuration required - feature is always enabled.

To disable for specific document types:
- Set `DocumentType.IsStockIncrease` to `false`
- Ensure rows don't have ProductId

## Related Features

- **Stock Reconciliation**: Fix discrepancies for historical documents
- **Inventory Management**: Physical inventory counts
- **Transfer Orders**: Move stock between warehouses
- **Stock Alerts**: Low stock notifications

## API Endpoints

This is an internal feature - no public API endpoints.

Automatic creation happens within:
- `POST /api/documentheaders/{id}/approve`
- `POST /api/documentheaders/{id}/close`
- `PUT /api/documentrows/{id}` (when document is approved)

## Database Schema

### StockMovements Table
```sql
StockMovements (
    Id,
    DocumentHeaderId,  -- Links to source document
    DocumentRowId,     -- Links to specific row
    ProductId,
    FromLocationId,    -- For outbound/transfers
    ToLocationId,      -- For inbound/transfers
    Quantity,
    UnitCost,
    MovementType,      -- Inbound, Outbound, Transfer, Adjustment
    MovementDate,      -- Uses document date
    Notes,             -- "Auto-generated from document {Number}"
    TenantId,
    IsDeleted
)
```

## Future Enhancements

Potential improvements:
- ⏱️ Batch creation for very large documents (1000+ rows)
- 📊 Performance metrics dashboard
- 🔔 Real-time notifications on movement creation
- 📝 Detailed movement audit trail
- 🎯 Smart location selection based on stock levels

## FAQ

**Q: Do I need to approve AND close documents?**  
A: No. Stock movements are created on either action. Once created, subsequent actions skip creation.

**Q: What happens if I modify a row after approval?**  
A: A compensating movement is created for the difference.

**Q: Can I undo a document after closing?**  
A: No. Closed documents are immutable. Create a credit note/return document instead.

**Q: How do I fix old documents without movements?**  
A: Use the Stock Reconciliation feature to identify and fix discrepancies.

**Q: What if a product doesn't exist in the location?**  
A: For inbound, stock is created. For outbound, movement is created even if stock goes negative (warning logged).

**Q: Are movements created in a transaction?**  
A: Yes. If document close/approve fails, movements are rolled back.

## Summary

The automatic stock movement creation feature:
- ✅ Eliminates manual stock reconciliation
- ✅ Ensures real-time stock accuracy
- ✅ Works seamlessly with existing workflows
- ✅ Prevents duplicate movements
- ✅ Scales to large documents
- ✅ Provides clear audit trail
- ✅ Backward compatible

No configuration or manual intervention required - it just works! 🎉
