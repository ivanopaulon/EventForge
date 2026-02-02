# Dashboard to QuickFilters Migration Guide

## Overview
This guide helps you migrate management pages from the old `ManagementDashboard` component to the modern `QuickFilters` component.

## Why Migrate?

### Problems with ManagementDashboard
- ❌ **Takes up ~150px of vertical space** → wastes screen real estate
- ❌ **Static, read-only metrics** → no interaction
- ❌ **Always calculates metrics** → performance overhead
- ❌ **Redundant information** → data already visible in table
- ❌ **Complex configuration** → difficult to maintain

### Benefits of QuickFilters
- ✅ **Compact design (~40px height)** → more space for data
- ✅ **Interactive filtering** → click to filter
- ✅ **On-demand calculation** → better performance
- ✅ **Actionable UI** → users can filter, not just view
- ✅ **Simple configuration** → easy to maintain

## Migration Steps

### Step 1: Remove Dashboard Import
```diff
@using EventForge.Client.Shared.Components
- @using EventForge.Client.Shared.Components.Dashboard
```

### Step 2: Remove Dashboard Markup
```diff
<div class="your-page-root">
-   <div class="your-page-top">
-       <ManagementDashboard TItem="YourEntityDto"
-                            Items="_filteredItems"
-                            Metrics="_dashboardMetrics"
-                            EntityType="YourEntity"
-                            AllowConfiguration="true"
-                            UseServerSide="false" />
-   </div>
+   <!-- Quick Filters -->
+   <QuickFilters TItem="YourEntityDto"
+                 Items="_items"
+                 Filters="_quickFilters"
+                 OnFilterSelected="@HandleQuickFilter"
+                 ShowCount="true" />
    
    <div class="eftable-wrapper">
        <EFTable ... />
    </div>
</div>
```

### Step 3: Convert Dashboard Metrics to Quick Filters

#### Before (Dashboard):
```csharp
private List<DashboardMetric<StorageFacilityDto>> _dashboardMetrics = new()
{
    new()
    {
        Title = "Totale Magazzini",
        Type = MetricType.Count,
        Icon = Icons.Material.Outlined.Warehouse,
        Color = "primary",
        Description = "Numero totale di magazzini",
        Format = "N0"
    },
    new()
    {
        Title = "Attivi",
        Type = MetricType.Count,
        Filter = w => w.IsActive,
        Icon = Icons.Material.Outlined.CheckCircle,
        Color = "success",
        Description = "Magazzini attivi",
        Format = "N0"
    },
    new()
    {
        Title = "Fiscali",
        Type = MetricType.Count,
        Filter = w => w.IsFiscal,
        Icon = Icons.Material.Outlined.AccountBalance,
        Color = "primary",
        Description = "Magazzini fiscali",
        Format = "N0"
    }
};
```

#### After (Quick Filters):
```csharp
private List<QuickFilter<StorageFacilityDto>> _quickFilters = new()
{
    new() 
    { 
        Id = "all", 
        Label = "Tutti",
        Predicate = _ => true,
        Description = "Mostra tutti i magazzini"
    },
    new() 
    { 
        Id = "active", 
        Label = "Attivi",
        Predicate = f => f.IsActive,
        Color = Color.Success,
        Icon = Icons.Material.Outlined.CheckCircle,
        Description = "Magazzini attivi"
    },
    new() 
    { 
        Id = "fiscal", 
        Label = "Fiscali",
        Predicate = f => f.IsFiscal,
        Color = Color.Primary,
        Icon = Icons.Material.Outlined.AccountBalance,
        Description = "Magazzini fiscali"
    }
};

private QuickFilter<StorageFacilityDto>? _activeQuickFilter;
```

### Conversion Table

| Dashboard Property | Quick Filter Property | Notes |
|-------------------|----------------------|-------|
| `Title` | `Label` | Keep short and clear |
| `Type: MetricType.Count` | `Predicate` | Convert to filter function |
| `Filter` | `Predicate` | Direct mapping |
| `Icon` | `Icon` | Same icons, different format |
| `Color` (string) | `Color` (Color enum) | "primary" → `Color.Primary` |
| `Description` | `Description` | Same |
| `Format` | N/A | Count is auto-formatted |

### Step 4: Add Filter Handler
```csharp
private void HandleQuickFilter(QuickFilter<YourEntityDto>? filter)
{
    _activeQuickFilter = filter;
    StateHasChanged();
}
```

### Step 5: Update Filtering Logic
```csharp
private IEnumerable<YourEntityDto> _filteredItems => 
    _items.Where(item => FilterItem(item));

private bool FilterItem(YourEntityDto item)
{
    // Quick filter (ADD THIS FIRST)
    if (_activeQuickFilter != null && _activeQuickFilter.Predicate != null)
    {
        if (!_activeQuickFilter.Predicate(item))
            return false;
    }
    
    // Existing filters (search, inline filters, etc.)
    // ...
    
    return true;
}
```

### Step 6: Update ClearFilters (if exists)
```diff
private void ClearFilters()
{
    _searchTerm = string.Empty;
+   _activeQuickFilter = null;
    // ... other filter resets
    StateHasChanged();
}
```

## Metric Type Mapping

### Count Metrics → Quick Filters
✅ **Easy to convert**

```csharp
// Dashboard
new() {
    Title = "Attivi",
    Type = MetricType.Count,
    Filter = x => x.IsActive,
    ...
}

// Quick Filter
new() {
    Id = "active",
    Label = "Attivi",
    Predicate = x => x.IsActive,
    ...
}
```

### Sum/Average/Min/Max Metrics → Table Footer
⚠️ **Cannot convert to Quick Filters**

These metrics should be moved to the table footer or a summary section:

```razor
<!-- Add below table -->
<MudPaper Class="pa-2 mt-2">
    <MudText Typo="Typo.caption">
        Totale Importo: @_filteredItems.Sum(x => x.Amount).ToString("C2")
    </MudText>
</MudPaper>
```

## Complete Example: WarehouseManagement

### Before
```razor
@using EventForge.Client.Shared.Components.Dashboard

<div class="warehouse-page-root">
    <div class="warehouse-top">
        <ManagementDashboard TItem="StorageFacilityDto"
                             Items="_filteredFacilities"
                             Metrics="_dashboardMetrics"
                             EntityType="Warehouse"
                             AllowConfiguration="true"
                             UseServerSide="false" />
    </div>
    
    <div class="eftable-wrapper">
        <EFTable ... />
    </div>
</div>

@code {
    private List<DashboardMetric<StorageFacilityDto>> _dashboardMetrics = new()
    {
        new() { Title = "Totale", Type = MetricType.Count, ... },
        new() { Title = "Attivi", Type = MetricType.Count, Filter = f => f.IsActive, ... },
        new() { Title = "Fiscali", Type = MetricType.Count, Filter = f => f.IsFiscal, ... }
    };
}
```

### After
```razor
<div class="warehouse-page-root">
    <!-- Quick Filters -->
    <QuickFilters TItem="StorageFacilityDto"
                  Items="_storageFacilities"
                  Filters="_quickFilters"
                  OnFilterSelected="@HandleQuickFilter"
                  ShowCount="true" />
    
    <div class="eftable-wrapper">
        <EFTable TItem="StorageFacilityDto"
                 Items="_filteredFacilities"
                 ... />
    </div>
</div>

@code {
    private List<QuickFilter<StorageFacilityDto>> _quickFilters = new()
    {
        new() { Id = "all", Label = "Tutti", Predicate = _ => true },
        new() { Id = "active", Label = "Attivi", Predicate = f => f.IsActive, Color = Color.Success },
        new() { Id = "fiscal", Label = "Fiscali", Predicate = f => f.IsFiscal, Color = Color.Primary }
    };
    
    private QuickFilter<StorageFacilityDto>? _activeQuickFilter;
    
    private void HandleQuickFilter(QuickFilter<StorageFacilityDto>? filter)
    {
        _activeQuickFilter = filter;
        StateHasChanged();
    }
    
    private IEnumerable<StorageFacilityDto> _filteredFacilities => 
        _storageFacilities.Where(f => FilterFacility(f));
    
    private bool FilterFacility(StorageFacilityDto facility)
    {
        // Quick filter
        if (_activeQuickFilter != null && _activeQuickFilter.Predicate != null)
        {
            if (!_activeQuickFilter.Predicate(facility))
                return false;
        }
        
        // Other filters...
        
        return true;
    }
}
```

## Inline Filters Consolidation

While migrating, also consolidate inline filters to MAX 2-3:

### Before (Too Many)
```razor
<MudTextField @bind-Value="_searchTerm" />
<MudSelect @bind-Value="_typeFilter" />
<MudSelect @bind-Value="_statusFilter" />
<MudSwitch @bind-Value="_showOnlyActive" />
<MudSwitch @bind-Value="_hideExpired" />
<!-- TOTAL: 5 filters → TOO MANY! -->
```

### After (Optimized)
```razor
<!-- Search -->
<MudTextField @bind-Value="_searchTerm" Class="ef-input" />

<!-- 1-2 simple inline filters -->
<MudSwitch @bind-Value="_onlyDefault" Label="Solo Default" />
```

**Move type/status filters to Quick Filters:**
```csharp
new() { Id = "type_a", Label = "Tipo A", Predicate = x => x.Type == Type.A, ... },
new() { Id = "active", Label = "Attivi", Predicate = x => x.Status == Status.Active, ... }
```

## Testing Checklist

After migration, verify:

- [ ] Dashboard removed (no import, no component, no metrics variable)
- [ ] Quick filters visible above table
- [ ] Count correct for each chip
- [ ] Click chip → filters table
- [ ] Click chip again (or select different chip) → updates filter
- [ ] Search + quick filter + inline filters work together
- [ ] Clear filters resets everything
- [ ] No console errors
- [ ] Layout looks good (responsive)

## Common Issues

### Issue: Counts show 0
**Cause**: `Items` parameter not bound to data source.
**Fix**: Ensure `Items="_items"` points to full dataset, not filtered.

### Issue: Filter doesn't apply
**Cause**: `_activeQuickFilter` not used in filtering logic.
**Fix**: Add quick filter check at start of filter method.

### Issue: Multiple filters active
**Cause**: MudChipSet allows multiple selections.
**Fix**: Already handled - component uses `Filter="true"` and `Mandatory="false"`.

## Performance Comparison

| Aspect | Dashboard | QuickFilters |
|--------|-----------|--------------|
| **Height** | ~150px | ~40px |
| **Calculation** | Always | On-demand |
| **Interaction** | None | Click to filter |
| **Updates** | On data change | On selection change |
| **Complexity** | High (metrics config) | Low (simple predicates) |

## See Also
- [QuickFilters Component Documentation](components/QuickFilters.md)
- [EFTable Standard Pattern](EFTABLE_STANDARD_PATTERN.md)
- [PR #3 Implementation Summary](../PR3_IMPLEMENTATION_SUMMARY.md)
