# Inventory Procedure Flow Diagram

## Visual Flow: What Happens When Inserting Inventory Quantity

```
┌─────────────────────────────────────────────────────────────────────┐
│                  POST /api/v1/warehouse/inventory                   │
│                                                                     │
│  Request: {                                                         │
│    "productId": "guid",                                             │
│    "locationId": "guid",                                            │
│    "quantity": 95,  ← Physical count                                │
│    "lotId": "guid" (optional),                                      │
│    "notes": "Q1 2025 inventory"                                     │
│  }                                                                  │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│  STEP 1: Get Current Stock Level                                   │
│  ─────────────────────────────────────────────────────────────────  │
│  Query: Stock table WHERE                                           │
│         ProductId = guid AND                                        │
│         LocationId = guid AND                                       │
│         LotId = guid (if provided)                                  │
│                                                                     │
│  Result: Current Quantity = 100 units                               │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│  STEP 2: Calculate Adjustment                                       │
│  ─────────────────────────────────────────────────────────────────  │
│  Counted Quantity    = 95 units  (from request)                     │
│  Current Quantity    = 100 units (from database)                    │
│  Adjustment Quantity = -5 units  (calculated)                       │
│                                                                     │
│  Interpretation: Stock shortage detected                            │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│  STEP 3: Create StockMovement Document (if adjustment ≠ 0)         │
│  ─────────────────────────────────────────────────────────────────  │
│  ✅ NEW DOCUMENT CREATED!                                           │
│                                                                     │
│  INSERT INTO StockMovements:                                        │
│  {                                                                  │
│    "id": "new-guid",                                                │
│    "movementType": "Adjustment",                                    │
│    "productId": "guid",                                             │
│    "fromLocationId": "guid",  ← negative adjustment                 │
│    "quantity": 5,                                                   │
│    "reason": "Inventory Count - Stock Shortage Detected",           │
│    "notes": "Q1 2025 inventory",                                    │
│    "movementDate": "2025-01-15T10:30:00Z",                          │
│    "createdBy": "mario.rossi",                                      │
│    "status": "Completed"                                            │
│  }                                                                  │
│                                                                     │
│  Result: Permanent audit trail created ✅                           │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│  STEP 4: Update Stock Record                                        │
│  ─────────────────────────────────────────────────────────────────  │
│  ✅ WAREHOUSE UPDATED!                                              │
│                                                                     │
│  UPDATE Stock SET:                                                  │
│    Quantity = 95,           ← New counted value                     │
│    ModifiedBy = "mario.rossi",                                      │
│    ModifiedAt = "2025-01-15T10:30:00Z"                              │
│  WHERE id = stock-guid                                              │
│                                                                     │
│  Result: Stock quantity updated to match physical count ✅          │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│  STEP 5: Set Last Inventory Date                                    │
│  ─────────────────────────────────────────────────────────────────  │
│  ✅ TRACKING UPDATED!                                               │
│                                                                     │
│  UPDATE Stock SET:                                                  │
│    LastInventoryDate = "2025-01-15T10:30:00Z"                       │
│  WHERE id = stock-guid                                              │
│                                                                     │
│  Result: Last physical count date recorded ✅                       │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│  STEP 6: Return Response                                            │
│  ─────────────────────────────────────────────────────────────────  │
│  Response: {                                                        │
│    "id": "stock-guid",                                              │
│    "productId": "guid",                                             │
│    "productName": "Product XYZ",                                    │
│    "locationId": "guid",                                            │
│    "locationName": "A-01-01",                                       │
│    "quantity": 95,  ← Updated quantity                              │
│    "lotId": "guid",                                                 │
│    "notes": "Q1 2025 inventory",                                    │
│    "createdAt": "2025-01-15T10:30:00Z",                             │
│    "createdBy": "mario.rossi"                                       │
│  }                                                                  │
└─────────────────────────────────────────────────────────────────────┘
```

## Database State Changes

### Before Inventory Operation

**Stock Table:**
```
┌──────────────┬──────────┬────────────┬──────────────────────┐
│ ProductId    │ Location │ Quantity   │ LastInventoryDate    │
├──────────────┼──────────┼────────────┼──────────────────────┤
│ product-xyz  │ A-01-01  │ 100        │ 2024-12-01           │
└──────────────┴──────────┴────────────┴──────────────────────┘
```

**StockMovements Table:**
```
(No adjustment movement exists yet)
```

### After Inventory Operation

**Stock Table:**
```
┌──────────────┬──────────┬────────────┬──────────────────────┐
│ ProductId    │ Location │ Quantity   │ LastInventoryDate    │
├──────────────┼──────────┼────────────┼──────────────────────┤
│ product-xyz  │ A-01-01  │ 95 ✅      │ 2025-01-15 ✅        │
└──────────────┴──────────┴────────────┴──────────────────────┘
```

**StockMovements Table:**
```
┌────────────┬────────────┬──────────┬──────────┬────────────┬──────────────────────┐
│ Type       │ Product    │ Location │ Quantity │ Reason     │ Date                 │
├────────────┼────────────┼──────────┼──────────┼────────────┼──────────────────────┤
│ Adjustment │ product-xyz│ A-01-01  │ 5        │ Stock      │ 2025-01-15T10:30:00Z │
│            │            │          │          │ Shortage   │                      │
│            │            │          │          │ Detected   │                      │
└────────────┴────────────┴──────────┴──────────┴────────────┴──────────────────────┘
                                                              ↑ NEW DOCUMENT ✅
```

## Summary: What Gets Created/Updated

### ✅ Documents Created:
1. **StockMovement** (Adjustment type) - Permanent audit record
2. **AuditLog** entry - Automatic change tracking

### ✅ Records Updated:
1. **Stock.Quantity** - Updated to counted value
2. **Stock.LastInventoryDate** - Set to current date/time
3. **Stock.ModifiedBy** - Set to current user
4. **Stock.ModifiedAt** - Set to current timestamp

## Key Differences: Before vs After

| Aspect | Before Fix | After Fix |
|--------|-----------|-----------|
| **Document Creation** | ❌ None | ✅ StockMovement (Adjustment) |
| **Audit Trail** | ❌ Limited | ✅ Complete |
| **Traceability** | ❌ No | ✅ Full |
| **Discrepancy Tracking** | ❌ Not recorded | ✅ Documented |
| **Compliance** | ❌ Insufficient | ✅ Full support |
| **Historical Analysis** | ❌ Not possible | ✅ Enabled |
| **LastInventoryDate** | ❌ Not set | ✅ Updated |

## Compliance & Audit Benefits

### ✅ Questions That Can Now Be Answered:
- Who performed the inventory count?
- When was the count performed?
- What was the discrepancy?
- Why was the stock adjusted?
- What were the before/after values?
- What was the reason for the difference?

### ✅ Regulatory Requirements Supported:
- Financial reconciliation
- Quality control documentation
- Inventory accuracy metrics
- Shrinkage analysis
- Compliance audits
- Chain of custody

## Conclusion

**The inventory procedure now creates BOTH:**
1. ✅ **A formal document** (StockMovement) for audit trail
2. ✅ **Updates the warehouse** (Stock) with counted quantities

This is a **complete, professional-grade inventory management solution** with full traceability and compliance support.
