# Recent Product Transaction Suggestions Feature - Implementation Summary

## Overview
This feature integrates price suggestions from recent transactions (purchases or sales) directly into the `AddDocumentRowDialog` component. When a user selects a product while adding or editing a document row, the system displays the last 3 relevant transactions with their effective prices, allowing users to quickly apply these prices with a single click.

## Implementation Details

### Server-Side Changes

#### 1. New DTO: `RecentProductTransactionDto`
**Location:** `EventForge.DTOs/Products/RecentProductTransactionDto.cs`

Contains information about recent product transactions:
- Document header and row identification
- Business party information (supplier/customer)
- Normalized quantity (BaseQuantity if available, otherwise Quantity)
- Effective unit price (after discount, normalized to base unit)
- Raw unit price and base unit price
- Currency, unit of measure, discount details

#### 2. Server Service Implementation
**Location:** `EventForge.Server/Services/Products/ProductService.cs`

Added `GetRecentProductTransactionsAsync` method that:
- Queries document rows filtered by product, tenant, and approval status
- Filters by document type (IsStockIncrease) based on transaction type parameter
- Optionally filters by business party ID
- Orders results by document date (descending) and creation date
- Calculates effective unit price:
  - For percentage discounts: `unitPrice * (1 - discount/100)`
  - For value discounts: `unitPrice - (discountValue / quantity)`
  - Clamps discount to not exceed unit price
- Returns top N transactions (default: 3, max: 10)

**Key Features:**
- Efficient LINQ queries with proper includes and tenant filtering
- Handles both percentage and value-based discounts
- Works with base unit normalization when available
- Uses constant `DefaultCurrency = "EUR"` for extensibility

#### 3. Controller Endpoint
**Location:** `EventForge.Server/Controllers/ProductManagementController.cs`

New endpoint: `GET /api/v1/product-management/products/{productId}/recent-transactions`

Query parameters:
- `type`: "purchase" or "sale" (required, defaults to "purchase")
- `partyId`: Optional GUID to filter by business party
- `top`: Number of results (1-10, defaults to 3)

Returns: `IEnumerable<RecentProductTransactionDto>`

Includes validation for:
- Transaction type parameter
- Top parameter bounds
- Product existence
- Tenant access

### Client-Side Changes

#### 1. Client Service Extension
**Location:** `EventForge.Client/Services/ProductService.cs`

Added `GetRecentProductTransactionsAsync` method that:
- Constructs API URL with query parameters
- Calls the server endpoint
- Returns null on error (with logging)

#### 2. Dialog Enhancement
**Location:** `EventForge.Client/Shared/Components/Dialogs/Documents/AddDocumentRowDialog.razor`

**State Management:**
- Added `_documentHeader` to store document context
- Added `_recentTransactions` list
- Added `_loadingTransactions` flag
- Added constants `PurchaseKeywords` and `SaleKeywords` for document type detection

**Initialization:**
- Loads document header on initialization to get business party and document type context

**Product Selection:**
- Triggers `LoadRecentTransactions` when a product is selected
- Determines transaction type based on document type name keywords
- Optionally filters by current business party

**UI Components:**
- Displays suggestions in a styled `MudPaper` component with info color border
- Shows loading indicator while fetching transactions
- Each transaction displays:
  - Business party name
  - Document number and date
  - Quantity
  - Effective unit price (highlighted in green)
  - "Applica" (Apply) button
- Compact, mobile-friendly layout using MudBlazor's Grid system

**Price Application:**
- `ApplySuggestion` method applies the suggested price to `_model.UnitPrice`
- Applies `BaseUnitPrice` if available
- Clears discount fields (since price is already net)
- Shows success snackbar notification
- Future enhancement: unit conversion for different units of measure

### Testing

#### Server Tests
**Location:** `EventForge.Tests/Services/Products/ProductRecentTransactionsTests.cs`

Comprehensive test suite covering:
1. **Purchase transaction retrieval** - Verifies top N results ordered by date
2. **Sale transaction retrieval** - Validates filtering by document type
3. **Business party filtering** - Tests party-specific results
4. **Non-existent product handling** - Returns empty list gracefully
5. **Value discount calculation** - Ensures correct price calculation

**Test Infrastructure:**
- Uses in-memory database for isolation
- Seeds test data with purchases and sales
- Validates effective price calculations with different discount types
- All 5 tests passing

### Security Considerations

1. **Tenant Isolation:** All queries filter by tenant ID to prevent cross-tenant data access
2. **Authorization:** Endpoint requires authentication via `[Authorize]` attribute
3. **Input Validation:** 
   - Transaction type limited to "purchase" or "sale"
   - Top parameter bounded between 1-10
   - Product existence verified before query
4. **Approval Status:** Only approved documents are included in results
5. **Soft Delete Handling:** Excludes deleted rows and headers

### Performance Optimizations

1. **Query Efficiency:**
   - Single database query with proper includes
   - Limited result set (top N)
   - Indexes assumed on common query fields (ProductId, TenantId, ApprovalStatus)
2. **Client-Side:**
   - Suggestions loaded asynchronously
   - Loading indicator for UX
   - Failed requests don't break the dialog

### Extensibility Points

1. **Multi-Currency Support:** 
   - Currency field in DTO (currently defaults to EUR)
   - Can be extended by reading from configuration or document header
2. **Unit Conversion:**
   - DTO includes BaseUnitPrice for future conversion logic
   - Placeholder in `ApplySuggestion` for unit-aware price application
3. **Configurable Keywords:**
   - Document type detection keywords extracted as constants
   - Can be moved to configuration/localization files
4. **Suggestion Count:**
   - `top` parameter allows flexibility (1-10)
   - Can be made configurable per tenant/user

### User Experience

**Benefits:**
- **Time Savings:** One-click price application eliminates manual entry
- **Consistency:** Uses historical pricing for similar transactions
- **Transparency:** Shows source document and context
- **Context-Aware:** Filters by document type and optionally by business party

**UX Flow:**
1. User opens AddDocumentRowDialog
2. User selects or scans a product
3. System loads recent transactions (with loading indicator)
4. Suggestions appear below unit price field
5. User reviews suggestions (party, date, price)
6. User clicks "Applica" to use suggested price
7. Price and discounts are automatically set

### Localization Support

Translation keys used:
- `documents.recentPurchases`: "Ultimi Acquisti per questo Prodotto"
- `documents.recentSales`: "Ultime Vendite per questo Prodotto"
- `documents.applyPrice`: "Applica"
- `documents.netPriceAfterDiscount`: "Prezzo netto (dopo sconto)"
- `documents.loadingTransactions`: "Caricamento suggerimenti prezzo..."
- `documents.priceApplied`: "Prezzo applicato: {0:C2}"
- `documents.priceApplyError`: "Errore nell'applicazione del prezzo"

### Future Enhancements

1. **Unit Conversion Logic:**
   - Automatic conversion when selected unit differs from transaction unit
   - Use `IUnitConversionService` or conversion factors from ProductUnits
2. **"Open Document" Action:**
   - Add button to open source document in read-only view
   - Use `DocumentViewerDialog`
3. **Supplier Selection:**
   - For purchases, allow setting supplier from suggestion
   - Useful when assigning products to suppliers
4. **Price Trend Indicator:**
   - Show trend (↑↓) if price changed compared to previous transaction
   - Color-code suggestions based on price variance
5. **Caching:**
   - Cache recent transactions per product (1-5 minute TTL)
   - Reduce database load for frequently accessed products
6. **Advanced Filtering:**
   - Date range filtering
   - Warehouse/location-specific suggestions
   - Exclude specific document types

### Documentation References

Related documentation files:
- `DOCUMENT_ROW_DIALOG_IMPROVEMENTS_SUMMARY.md` - Previous dialog enhancements
- `PRODUCT_MANAGEMENT_IMPLEMENTATION_SUMMARY.md` - Product management context
- This implementation follows patterns established in existing document management code

### Breaking Changes

**None** - This is a purely additive feature:
- New endpoint (no changes to existing endpoints)
- New DTO (no changes to existing DTOs)
- Client UI enhancement (gracefully degrades if endpoint unavailable)
- Backward compatible with existing workflows

### Deployment Notes

1. **Database:** No schema changes required (uses existing tables)
2. **Configuration:** No new configuration settings needed
3. **Dependencies:** No new dependencies added
4. **API Versioning:** Uses existing `v1` API route structure
5. **Feature Flags:** Can be hidden behind `ProductManagement` license feature flag (already applied)

### Testing Results

**Server Tests:**
- 5 new tests added
- All passing
- Coverage includes happy path, edge cases, and error scenarios

**Integration:**
- Full solution builds successfully
- No existing tests broken
- Total: 275 passing tests (was 270), 3 pre-existing failures unchanged

### Code Quality

**Code Review Feedback Addressed:**
- ✅ Extracted hard-coded currency to constant
- ✅ Extracted keyword arrays to class-level constants
- ✅ Removed unnecessary discard operator
- ✅ Added comprehensive XML documentation
- ✅ Follows existing code patterns and conventions

### Conclusion

This feature successfully implements contextual price suggestions in the document row dialog, improving user productivity and data consistency. The implementation is production-ready, well-tested, and designed for future extensibility. The feature gracefully integrates with existing workflows without introducing breaking changes.

**Status:** ✅ Complete and Ready for Merge
