# Component Decomposition Guide

## Overview
This guide documents the process of extracting reusable components from large dialog components, using `AddDocumentRowDialog` as a case study. This approach improves maintainability, testability, and code reusability.

## Case Study: AddDocumentRowDialog Refactoring

### Before (Monolithic)
- **Main dialog**: 342 lines (razor) + 1,913 lines (cs)
- Multiple responsibilities mixed together
- Difficult to test individual features
- Hard to maintain and extend
- Product autocomplete broken due to incorrect pattern

### After (Modular)
- **Main dialog**: 350 lines (razor) + 1,928 lines (cs)
- **3 extracted components**:
  - `DocumentRowProductSelector` (recreated with working pattern)
  - `DocumentRowRecentTransactions` (new feature)
  - `DocumentRowNotesPanel` (extracted)
- Each component has single responsibility
- Easy to test in isolation
- Reusable across application
- Product autocomplete FIXED using proven pattern

## Extraction Process

### 1. Identify Candidate Components
Look for:
- Self-contained UI sections with clear boundaries
- Features with independent state
- Reusable patterns (search, selection, display)
- Broken or problematic code that needs refactoring

### 2. Define Component Interface
- Input parameters (data)
- Output events (callbacks)
- Keep interface minimal
- Use `EventCallback<T>` for two-way binding support

### 3. Extract Markup and Logic
- Move related HTML to `.razor` file
- Move related C# to `.razor.cs` file
- Keep original functionality intact
- Follow proven patterns from similar working components

### 4. Update Parent Component
- Replace inline markup with component tag
- Pass parameters and handle events
- Verify functionality unchanged
- Test integration thoroughly

## Component Patterns

### DocumentRowProductSelector

**Pattern**: Autocomplete with Quick Actions

**CRITICAL FIX**: This component was recreated from scratch using the EXACT pattern from `GenericDocumentProcedure.razor` (BusinessParty autocomplete) which is **KNOWN WORKING**.

**Previous Issues**:
- ❌ Used manual `Value` + `ValueChanged` instead of `@bind-Value`
- ❌ `CoerceText="true"` caused state conflicts with MudBlazor
- ❌ Search function not triggered correctly
- ❌ Product selection broken

**Solution - Use Working Pattern**:
```razor
<MudAutocomplete T="ProductDto"
                 @bind-Value="SelectedProduct"              ✅ Two-way binding
                 SearchFunc="@SearchProductsAsync"
                 ToStringFunc="@(p => p?.Name ?? string.Empty)"
                 MinCharacters="2"
                 DebounceInterval="300"
                 ShowProgressIndicator="true"
                 CoerceText="false"                         ✅ NO coercion
                 CoerceValue="false"                        ✅ NO coercion
                 ... />
```

**Codebehind Pattern**:
```csharp
[Parameter]
public ProductDto? SelectedProduct { get; set; }

[Parameter]
public EventCallback<ProductDto?> SelectedProductChanged { get; set; }

[Parameter, EditorRequired]
public Func<string, CancellationToken, Task<IEnumerable<ProductDto>>>? SearchFunc { get; set; }

// Wrapper method that delegates to parent's SearchFunc
private async Task<IEnumerable<ProductDto>> SearchProductsAsync(
    string searchTerm, CancellationToken cancellationToken)
{
    if (SearchFunc == null) return Array.Empty<ProductDto>();
    return await SearchFunc(searchTerm, cancellationToken);
}
```

**Key Features**:
- Rich ItemTemplate: Product Name + Code + Price
- NoItemsTemplate: "Nessun prodotto trovato"
- ProductQuickInfo card when product selected
- Quick Edit button → opens QuickCreateProductDialog
- ID-based change detection (avoid reference equality issues)

### DocumentRowRecentTransactions

**Pattern**: Data Display with Actions

**Features**:
- Loads data based on `ProductId` parameter
- Displays in table format with: Date, Price, UoM, Document, Party
- "Applica" button to apply selected price to current row
- Loading skeleton during fetch
- Empty state handling ("Nessuna transazione recente trovata")

**Component Interface**:
```csharp
[Parameter] public Guid? ProductId { get; set; }
[Parameter] public string? TransactionType { get; set; }
[Parameter] public Guid? PartyId { get; set; }
[Parameter] public EventCallback<decimal> OnPriceApplied { get; set; }
[Parameter] public int MaxTransactions { get; set; } = 3;
```

**Lifecycle Pattern**:
```csharp
protected override async Task OnParametersSetAsync()
{
    await base.OnParametersSetAsync();
    
    // Only reload if ProductId changed
    if (_previousProductId != ProductId)
    {
        _previousProductId = ProductId;
        if (ProductId.HasValue)
        {
            await LoadRecentTransactionsAsync();
        }
        else
        {
            RecentTransactions.Clear();
        }
    }
}
```

### DocumentRowNotesPanel

**Pattern**: Simple Input with Validation

**Features**:
- Single responsibility (notes input)
- Character limit validation (default: 200)
- Character counter display
- Consistent styling
- Helper text showing character count

**Component Interface**:
```csharp
[Parameter] public string? Notes { get; set; }
[Parameter] public EventCallback<string?> NotesChanged { get; set; }
[Parameter] public int MaxLength { get; set; } = 200;
[Parameter] public bool Disabled { get; set; } = false;
```

**Validation Pattern**:
```csharp
private async Task OnNotesChanged(string? value)
{
    // Enforce max length
    if (!string.IsNullOrEmpty(value) && value.Length > MaxLength)
    {
        value = value.Substring(0, MaxLength);
    }
    
    if (NotesChanged.HasDelegate)
    {
        await NotesChanged.InvokeAsync(value);
    }
}
```

## Best Practices

### 1. Single Responsibility
Each component should do **ONE** thing well. Don't mix multiple concerns in a single component.

### 2. Clear Interface
- Minimal parameters
- Clear event names
- Use `[EditorRequired]` for critical parameters
- Document parameter purpose in XML comments

### 3. Self-Contained
Component should manage its own state when possible:
- Loading states
- Validation states
- UI state (expanded/collapsed, etc.)

### 4. Consistent Styling
- Use `DialogStyleConstants` for dialog-related styling
- Apply CSS classes from central stylesheets
- Follow MudBlazor component patterns

### 5. Follow Proven Patterns
**CRITICAL**: When fixing broken components, copy from working implementations:
- ✅ `GenericDocumentProcedure.razor` → BusinessParty autocomplete (WORKING)
- ❌ Old `DocumentRowProductSelector` → broken autocomplete (DON'T COPY)

### 6. Always Use @bind-Value for MudAutocomplete
**DO**:
```razor
<MudAutocomplete T="ProductDto"
                 @bind-Value="SelectedProduct"
                 SearchFunc="@SearchProductsAsync"
                 CoerceText="false"
                 CoerceValue="false" />
```

**DON'T**:
```razor
<MudAutocomplete T="ProductDto"
                 Value="_selectedProduct"
                 ValueChanged="@OnProductSelected"
                 SearchFunc="@SearchProductsAsync"
                 CoerceText="true" />  ❌ This breaks autocomplete!
```

### 7. Documentation
Document:
- Why component was extracted
- What problem it solves
- How to use it
- Integration examples

## Testing Guidelines

### 1. Isolation Testing
Test component independently before integration:
- Mock required services
- Test all parameter combinations
- Test all event callbacks
- Test edge cases

### 2. Comparison Testing
For refactored components, compare with source pattern:
- `GenericDocumentProcedure` BusinessParty autocomplete → `DocumentRowProductSelector`
- Behavior should be identical
- Search should trigger at same point
- Selection should work the same way

### 3. Event Testing
Verify all `EventCallback`s fire correctly:
- `SelectedProductChanged` when product selected
- `OnPriceApplied` when price button clicked
- `NotesChanged` when notes updated

### 4. Edge Cases
- Empty states (no data)
- Loading states (async operations)
- Error states (API failures)
- Null values
- Invalid inputs

### 5. Integration Testing
Test in parent component after extraction:
- Component renders correctly
- Data flows correctly (parent → component)
- Events flow correctly (component → parent)
- No regressions in functionality

## Troubleshooting

### MudAutocomplete Not Searching
**Symptom**: Typing in autocomplete doesn't trigger search

**Cause**: Using manual `Value` + `ValueChanged` instead of `@bind-Value`

**Fix**: Use `@bind-Value` and set `CoerceText="false"`, `CoerceValue="false"`

### Component Not Updating
**Symptom**: Component doesn't update when parent changes parameters

**Cause**: Not implementing `OnParametersSetAsync` properly

**Fix**: Override `OnParametersSetAsync` and check for parameter changes

### Reference Equality Issues
**Symptom**: Component detects change even though object is same

**Cause**: Comparing object references instead of IDs

**Fix**: Use ID comparison: `if (_previousProductId != ProductId)`

## Migration Checklist

When extracting a component:

- [ ] Identify boundaries and responsibilities
- [ ] Define component interface (parameters + events)
- [ ] Create `.razor` file with markup
- [ ] Create `.razor.cs` file with logic
- [ ] Extract state management
- [ ] Extract event handlers
- [ ] Update parent component usage
- [ ] Build and fix compilation errors
- [ ] Test component in isolation
- [ ] Test component in parent
- [ ] Verify no regressions
- [ ] Document component purpose
- [ ] Commit with clear message

## References

- **Working Pattern Source**: `EventForge.Client/Pages/Management/Documents/GenericDocumentProcedure.razor` (lines 194-211)
- **MudBlazor Autocomplete**: https://mudblazor.com/components/autocomplete
- **Blazor Component Parameters**: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/
- **Previous Fix Documentation**: `PRODUCT_AUTOCOMPLETE_FIX_SUMMARY.md`
- **Dialog Styling**: `EventForge.Client/Shared/Components/Dialogs/DialogStyleConstants.cs`

## Conclusion

Component decomposition is essential for maintaining large Blazor applications. By following proven patterns and extracting components with single responsibilities, we create more maintainable, testable, and reusable code. Always prefer copying from working implementations rather than creating custom solutions that may have hidden issues.
