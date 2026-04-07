# QuickFilters Component

## Overview
`QuickFilters` is a generic, reusable Blazor component designed to provide fast, interactive filtering for EFTable components. It replaces static dashboard metrics with dynamic, clickable chip filters that show real-time counts and allow users to instantly filter table data.

## Features
- ✅ **Generic Type Support**: Works with any entity type `TItem`
- ✅ **Dynamic Count Display**: Shows real-time count of matching items for each filter
- ✅ **Interactive Chips**: Click to filter, click again to clear
- ✅ **Customizable Appearance**: Support for colors, icons, and tooltips
- ✅ **Predicate-Based Filtering**: Uses `Func<TItem, bool>` for flexible filtering logic
- ✅ **Seamless EFTable Integration**: Designed to work perfectly with EFTable components
- ✅ **Responsive Design**: Adapts to different screen sizes
- ✅ **Accessibility**: Full keyboard navigation and ARIA support

## Basic Usage

### 1. Import the Component
```razor
@using EventForge.Client.Shared.Components
```

### 2. Define Quick Filters in Your Code
```csharp
private List<QuickFilter<YourEntityDto>> _quickFilters = new()
{
    new() 
    { 
        Id = "all", 
        Label = "Tutti",
        Predicate = _ => true,
        Description = "Mostra tutti gli elementi"
    },
    new() 
    { 
        Id = "active", 
        Label = "Attivi",
        Predicate = e => e.IsActive,
        Color = Color.Success,
        Icon = Icons.Material.Outlined.CheckCircle,
        Description = "Solo elementi attivi"
    },
    new() 
    { 
        Id = "recent", 
        Label = "Recenti",
        Predicate = e => e.CreatedAt >= DateTime.Now.AddDays(-30),
        Color = Color.Info,
        Icon = Icons.Material.Outlined.NewReleases,
        Description = "Creati negli ultimi 30 giorni"
    }
};

private QuickFilter<YourEntityDto>? _activeQuickFilter;
```

### 3. Add Filter Handler
```csharp
private void HandleQuickFilter(QuickFilter<YourEntityDto>? filter)
{
    _activeQuickFilter = filter;
    StateHasChanged();
}
```

### 4. Update Filtering Logic
```csharp
private IEnumerable<YourEntityDto> _filteredItems => 
    _items.Where(item => FilterItem(item));

private bool FilterItem(YourEntityDto item)
{
    // Quick filter
    if (_activeQuickFilter != null && _activeQuickFilter.Predicate != null)
    {
        if (!_activeQuickFilter.Predicate(item))
            return false;
    }
    
    // Other filters (search, inline filters, etc.)
    // ...
    
    return true;
}
```

### 5. Add Component to Markup
```razor
<div class="your-page-root">
    <!-- Quick Filters -->
    <QuickFilters TItem="YourEntityDto"
                  Items="_items"
                  Filters="_quickFilters"
                  OnFilterSelected="@HandleQuickFilter"
                  ShowCount="true" />
    
    <!-- EFTable -->
    <EFTable TItem="YourEntityDto"
             Items="_filteredItems"
             ... />
</div>
```

## API Reference

### Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `TItem` | Generic Type | Yes | - | The type of items being filtered |
| `Items` | `IEnumerable<TItem>?` | No | `null` | Collection of items used for count calculation |
| `Filters` | `List<QuickFilter<TItem>>?` | No | `null` | List of quick filters to display |
| `OnFilterSelected` | `EventCallback<QuickFilter<TItem>?>` | Yes | - | Callback invoked when a filter is selected or cleared |
| `ShowCount` | `bool` | No | `true` | Whether to display item counts for each filter |

### QuickFilter<TItem> Model

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Id` | `string` | Yes | Unique identifier for the filter |
| `Label` | `string` | Yes | Display text shown in the chip |
| `Predicate` | `Func<TItem, bool>?` | No | Function to filter items |
| `Color` | `Color` | No | MudBlazor color for the chip (Default, Primary, Secondary, Success, Warning, Error, Info) |
| `Icon` | `string?` | No | Material icon to display in the chip |
| `Description` | `string?` | No | Tooltip text shown on hover |

## Advanced Examples

### Example 1: Business Party Management
```csharp
private List<QuickFilter<BusinessPartyDto>> _quickFilters = new()
{
    new() { Id = "all", Label = "Tutti", Predicate = _ => true },
    new() 
    { 
        Id = "customers", 
        Label = "Clienti",
        Predicate = bp => bp.Type == BusinessPartyType.Cliente || bp.Type == BusinessPartyType.Both,
        Color = Color.Primary,
        Icon = Icons.Material.Outlined.People
    },
    new() 
    { 
        Id = "suppliers", 
        Label = "Fornitori",
        Predicate = bp => bp.Type == BusinessPartyType.Supplier || bp.Type == BusinessPartyType.Both,
        Color = Color.Secondary,
        Icon = Icons.Material.Outlined.LocalShipping
    },
    new() 
    { 
        Id = "both", 
        Label = "Cliente + Fornitore",
        Predicate = bp => bp.Type == BusinessPartyType.Both,
        Color = Color.Info,
        Icon = Icons.Material.Outlined.Handshake
    }
};
```

### Example 2: Warehouse Management
```csharp
private List<QuickFilter<StorageFacilityDto>> _quickFilters = new()
{
    new() { Id = "all", Label = "Tutti", Predicate = _ => true },
    new() 
    { 
        Id = "fiscal", 
        Label = "Fiscali",
        Predicate = f => f.IsFiscal,
        Color = Color.Primary,
        Icon = Icons.Material.Outlined.AccountBalance
    },
    new() 
    { 
        Id = "refrigerated", 
        Label = "Refrigerati",
        Predicate = f => f.IsRefrigerated,
        Color = Color.Info,
        Icon = Icons.Material.Outlined.AcUnit
    },
    new() 
    { 
        Id = "both", 
        Label = "Fiscali + Refrigerati",
        Predicate = f => f.IsFiscal && f.IsRefrigerated,
        Color = Color.Success
    }
};
```

### Example 3: VAT Rate Management
```csharp
private List<QuickFilter<VatRateDto>> _quickFilters = new()
{
    new() { Id = "all", Label = "Tutte", Predicate = _ => true },
    new() 
    { 
        Id = "active", 
        Label = "Attive",
        Predicate = v => v.Status == VatRateStatus.Active,
        Color = Color.Success,
        Icon = Icons.Material.Outlined.CheckCircle
    },
    new() 
    { 
        Id = "standard", 
        Label = "Standard (≥ 20%)",
        Predicate = v => v.Percentage >= 20,
        Color = Color.Primary,
        Icon = Icons.Material.Outlined.Percent
    },
    new() 
    { 
        Id = "reduced", 
        Label = "Ridotte (< 20%)",
        Predicate = v => v.Percentage < 20,
        Color = Color.Info,
        Icon = Icons.Material.Outlined.Percent
    }
};
```

## Best Practices

### 1. **Always Include an "All" Filter**
```csharp
new() { Id = "all", Label = "Tutti", Predicate = _ => true }
```
This allows users to easily clear the filter and see all items.

### 2. **Keep Filter Count Reasonable (4-7 filters)**
Too many filters can overwhelm users. Aim for 4-7 meaningful filters that cover the most common use cases.

### 3. **Use Meaningful Icons and Colors**
- ✅ Active/Success → `Color.Success` + `CheckCircle` icon
- ⚠️ Warning/Suspended → `Color.Warning` + `Warning` icon
- ❌ Error/Deleted → `Color.Error` + `Error` icon
- ℹ️ Info/Special → `Color.Info` + relevant icon

### 4. **Provide Descriptive Tooltips**
Add a `Description` to explain what each filter does:
```csharp
Description = "Magazzini fiscali e refrigerati"
```

### 5. **Combine with Search and Inline Filters**
Quick Filters work best when combined with:
- **Search**: For free-text filtering
- **1-2 Inline Filters**: For simple, frequently-used filters (switches/dropdowns)

Example filtering order:
1. Quick Filter (primary categorization)
2. Search (text matching)
3. Inline Filters (additional refinement)

### 6. **Test Count Performance**
If you have many items (>1000), ensure count calculation is efficient. The component calls `Predicate` for each item on every render.

## Styling

The component uses the following CSS classes (defined in `app.css`):

```css
.quick-filters-wrapper {
    background-color: var(--mud-palette-background-grey);
    border-radius: 4px;
    padding: 8px;
    margin-bottom: 8px;
}

.quick-filters-wrapper .mud-chip {
    transition: all 0.2s ease;
}

.quick-filters-wrapper .mud-chip:hover {
    transform: translateY(-1px);
    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.quick-filters-wrapper .mud-chip-selected {
    font-weight: 600;
}
```

### Customization
You can override these styles in your component-specific CSS if needed.

## Performance Considerations

### Count Calculation
- Counts are calculated on every render by iterating through all items
- For large datasets (>5000 items), consider:
  - Disabling counts: `ShowCount="false"`
  - Pre-computing counts when data loads
  - Using server-side filtering with pre-computed counts

### Predicate Complexity
- Keep predicates simple and fast
- Avoid complex LINQ operations in predicates
- Pre-compute values if possible

## Accessibility

The component includes:
- **ARIA Labels**: For screen readers
- **Keyboard Navigation**: Full keyboard support via MudBlazor
- **Tooltips**: Descriptive text for each filter
- **Color Contrast**: Uses MudBlazor's accessible color palette

## Migration from Dashboard

If you're migrating from `ManagementDashboard`:

| Dashboard Metric Type | Quick Filter Equivalent |
|-----------------------|-------------------------|
| **Count (Total)** | Chip "Tutti" with count |
| **Count (with Filter)** | Chip with specific predicate |
| **Sum/Average/Min/Max** | NOT convertible - move to table footer |

See [Dashboard to QuickFilters Migration Guide](../DASHBOARD_TO_QUICKFILTERS_MIGRATION.md) for detailed migration steps.

## Troubleshooting

### Counts Not Updating
**Problem**: Counts don't update when items change.
**Solution**: Ensure `Items` parameter is updated when data changes.

### Filter Not Applying
**Problem**: Selected filter doesn't filter the table.
**Solution**: Verify `OnFilterSelected` callback is wired correctly and `_activeQuickFilter` is used in filtering logic.

### Icons Not Showing
**Problem**: Icons don't appear in chips.
**Solution**: Ensure you're using valid Material Icons from `Icons.Material.Outlined.*` or `Icons.Material.Filled.*`.

## See Also
- [EFTable Standard Pattern](../EFTABLE_STANDARD_PATTERN.md)
- [Dashboard to QuickFilters Migration Guide](../DASHBOARD_TO_QUICKFILTERS_MIGRATION.md)
- [MudBlazor Chip Documentation](https://mudblazor.com/components/chip)
- [MudBlazor Icons](https://mudblazor.com/features/icons)
