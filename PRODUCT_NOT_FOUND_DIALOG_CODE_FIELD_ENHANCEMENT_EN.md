# ProductNotFoundDialog - Code Field Enhancement

## Summary
Added a visible and editable **Code field** to the `ProductNotFoundDialog` component to improve UX when assigning barcodes to existing products.

## Problem
When assigning a scanned barcode to an existing product, users could not:
- ❌ See the actual code value being assigned
- ❌ Verify the code before assignment
- ❌ Correct scanning errors

## Solution
Added a new `MudTextField` component in the assignment form that:
- ✅ Displays the code value (pre-filled with scanned barcode)
- ✅ Allows editing before assignment
- ✅ Includes validation (required, max 100 chars)
- ✅ Shows character counter and helper text

## Changes

### 1. ProductNotFoundDialog.razor
Added Code field between Code Type selector and Alternative Description:

```razor
<MudTextField @bind-Value="_createCodeDto.Code"
              Label="@TranslationService.GetTranslation("field.code", "Codice")"
              Variant="Variant.Outlined"
              Required="true"
              MaxLength="100"
              Counter="100"
              HelperText="@TranslationService.GetTranslation("products.codeHelper", "Codice SKU o simile")" />
```

### 2. Translation Files
- Added `"field.code": "Code"` to `en.json`
- `"field.code": "Codice"` already existed in `it.json`

### 3. Tests
- Added test cases for `field.code` translation key in both IT and EN

## Results
- ✅ Build: SUCCESS (0 errors)
- ✅ Tests: 211/211 PASSED (2 new tests added)

## Visual Comparison

### Before
```
[Code Type ▼]
[Alternative Description...]
```

### After
```
[Code Type ▼]
[Code *]                     ← NEW!
 ABC123              (0/100)
 ℹ️ SKU code or similar
[Alternative Description...]
```

## Benefits
1. **Visibility**: Users can see what code will be assigned
2. **Control**: Users can correct scan errors before assignment
3. **Validation**: Real-time feedback with required field and character counter
4. **Consistency**: Aligns with the CreateProductCodeDto validation rules

## Files Modified
- `EventForge.Client/Shared/Components/ProductNotFoundDialog.razor`
- `EventForge.Client/wwwroot/i18n/en.json`
- `EventForge.Tests/Services/Translation/TranslationServiceTests.cs`

---

**Implementation Date**: 2024-10-03  
**Status**: ✅ COMPLETED
