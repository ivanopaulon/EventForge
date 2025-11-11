# Fix: Alternative Unit Barcode Quantity Multiplication

## Issue Description (Italian)
"Sto controllando cosa succede se richiamo un articolo nella procedura di inventario con il codice a barre di un UM alternativa con fattore 6, la riga mi viene creata ma con la quantità non moltiplicata per il fattore, puoi verifica come mai per favore e correggere?"

## Translation
When recalling an article in the inventory procedure using a barcode for an alternative unit of measure (UM) with a factor of 6, the row is created but with the quantity not multiplied by the factor.

## Problem Analysis

### Root Cause
When scanning a barcode in the Fast Inventory Procedure:
1. The system called `GetProductByCodeAsync` which returns only the `ProductDto`
2. The ProductCode entity has an optional `ProductUnitId` that links to a ProductUnit with a `ConversionFactor`
3. Without retrieving the ProductCode information, the conversion factor was never accessed
4. Quantities were always set to 1 or incremented by 1, ignoring the conversion factor

### Example Scenario
- Product: "Water Bottles"
- Base Unit: "Piece" (1 bottle)
- Alternative Unit: "Pack of 6" with ConversionFactor = 6
- Barcode "PACK001" is associated with the "Pack of 6" unit

**Before the fix:**
- Scan "PACK001" → Quantity = 1 (incorrect - should be 6)
- Scan again → Quantity = 2 (incorrect - should be 12)

**After the fix:**
- Scan "PACK001" → Quantity = 6 (correct - 1 pack × factor 6)
- Scan again → Quantity = 12 (correct - 2 packs × factor 6)

## Solution Implementation

### Changes Made
Modified `EventForge.Client/Pages/Management/Warehouse/InventoryProcedureFast.razor`:

1. **Added State Variables** (lines 214-215):
   - `_currentProductCode`: Stores the ProductCode information
   - `_currentConversionFactor`: Stores the conversion factor (default: 1)

2. **Changed Product Lookup** (lines 559-580):
   - Replaced `GetProductByCodeAsync` with `GetProductWithCodeByCodeAsync`
   - Retrieves ProductCode with ProductUnitId information
   - Fetches ProductUnit to get ConversionFactor when ProductUnitId is present
   - Applies conversion factor to initial quantity: `_quantity = 1m * _currentConversionFactor`

3. **Updated Repeated Scan Logic** (lines 540, 548):
   - When incrementing quantity on repeated scans, multiply by conversion factor
   - `_quantity = scanResult.NewQuantity * _currentConversionFactor`

4. **Added State Cleanup** (lines 607-609, 811-812):
   - Reset `_currentProductCode` and `_currentConversionFactor` when clearing product form
   - Ensures clean state for next scan

### Code Flow

```
1. User scans barcode "PACK001"
   ↓
2. GetProductWithCodeByCodeAsync("PACK001")
   ↓
3. Returns ProductWithCodeDto { Product, Code { ProductUnitId } }
   ↓
4. If ProductUnitId exists:
   ↓
5. GetProductUnitByIdAsync(ProductUnitId)
   ↓
6. Get ConversionFactor (e.g., 6)
   ↓
7. Set _quantity = 1 × 6 = 6
   ↓
8. Display product entry form with quantity pre-filled to 6
   ↓
9. User confirms → Inventory row created with quantity 6
```

### Repeated Scan Flow

```
1. Product already loaded with ConversionFactor = 6
   ↓
2. User scans same barcode again
   ↓
3. HandleBarcodeScanned detects repeated scan
   ↓
4. NewQuantity = 2 (increment from 1 to 2)
   ↓
5. Apply factor: _quantity = 2 × 6 = 12
   ↓
6. Auto-confirm or focus quantity field with 12
```

## Testing

### Existing Tests
- All 20 InventoryFastService tests pass ✓
- Build successful with no compilation errors ✓
- No security vulnerabilities detected ✓

### Manual Testing Required
To fully validate the fix:

1. **Setup Test Data:**
   - Create a product with a base unit (e.g., "Piece")
   - Create an alternative unit (e.g., "Pack") with ConversionFactor = 6
   - Create a ProductCode (barcode) associated with the alternative unit

2. **Test Single Scan:**
   - Open Fast Inventory Procedure
   - Scan the alternative unit barcode
   - Verify quantity is pre-filled with 6 (not 1)
   - Confirm the row
   - Verify inventory row shows quantity 6

3. **Test Repeated Scan:**
   - With same product and location selected
   - Scan the same barcode again
   - Verify quantity increments to 12 (not 2)
   - Confirm the row
   - Verify inventory row is updated to quantity 12

4. **Test Base Unit:**
   - Scan a barcode for the base unit
   - Verify quantity is 1 (no conversion factor)
   - Behavior should remain unchanged

## Impact Assessment

### Minimal Changes
- Only 1 file modified: `InventoryProcedureFast.razor`
- 31 lines added, 6 lines removed
- No changes to services, DTOs, or database entities
- Backward compatible - works with existing data

### Benefits
- Correctly handles alternative unit barcodes in inventory procedure
- Maintains accurate inventory counts when using pack/case/pallet units
- Improves user experience by pre-filling correct quantities
- Reduces data entry errors

### No Breaking Changes
- Base units (ConversionFactor = 1) work exactly as before
- Barcodes without ProductUnitId work exactly as before
- All existing functionality preserved

## Related Code

### Entities
- `EventForge.Server/Data/Entities/Products/ProductCode.cs`
  - Line 24-27: ProductUnitId property (optional)
- `EventForge.Server/Data/Entities/Products/ProductUnit.cs`
  - Line 36-40: ConversionFactor property

### DTOs
- `EventForge.DTOs/Products/ProductCodeDto.cs`
  - Line 22-24: ProductUnitId property
- `EventForge.DTOs/Products/ProductUnitDto.cs`
  - Line 27-29: ConversionFactor property
- `EventForge.DTOs/Products/ProductWithCodeDto.cs`
  - Line 8-21: Combines Product with matched ProductCode

### Services
- `EventForge.Server/Services/Products/ProductService.cs`
  - Line 665-692: `GetProductWithCodeByCodeAsync` method
- `EventForge.Client/Services/IProductService.cs`
  - Line 16: `GetProductWithCodeByCodeAsync` interface
  - Line 53: `GetProductUnitByIdAsync` interface

## Future Enhancements

1. **Display Unit Information:**
   - Show the unit name/symbol in the product entry form
   - Example: "Quantity: 6 (1 Pack × 6 Pieces)"

2. **Configurable Behavior:**
   - Allow users to choose between auto-applying factor or manual entry
   - Could be a setting per warehouse or user preference

3. **Audit Trail:**
   - Log when conversion factors are applied
   - Helps with troubleshooting and validation

4. **Validation:**
   - Warn if user changes pre-filled quantity
   - Helps prevent accidental incorrect entries

## Conclusion

The fix successfully resolves the issue where alternative unit barcodes were not applying their conversion factors in the inventory procedure. The implementation is minimal, focused, and maintains backward compatibility while providing the correct behavior for all unit types.

## Files Changed
- `EventForge.Client/Pages/Management/Warehouse/InventoryProcedureFast.razor`

## Verification Checklist
- [x] Code compiles without errors
- [x] Existing tests pass
- [x] No security vulnerabilities introduced
- [ ] Manual testing with alternative unit barcodes (requires test environment)
- [ ] User acceptance testing (requires stakeholder validation)
