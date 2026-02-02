# Migration Guide: Legacy → Modern EFTable Pattern

## Overview

This guide provides step-by-step instructions for migrating management pages from the legacy pattern (with ManagementDashboard) to the modern EFTable pattern (with QuickFilters, OnRowClick, and advanced export).

## Benefits of Migration

✅ **Consistent UX** - All management pages look and behave the same  
✅ **Better Performance** - QuickFilters are lighter than ManagementDashboard  
✅ **Enhanced Navigation** - Ctrl+Click to open in new tab  
✅ **Configurable Search** - Users can choose which columns to search  
✅ **Advanced Export** - Export dialog with column selection and format choice  
✅ **Cleaner Code** - Standardized naming conventions and patterns  

## Migration Steps

### Step 1: Add QuickFilters (Replace ManagementDashboard)

**Before:**
```razor
<div class="entity-top">
    <ManagementDashboard TItem="EntityDto"
                         Items="_filteredEntities"
                         Metrics="_dashboardMetrics"
                         EntityType="Entity"
                         AllowConfiguration="true"
                         UseServerSide="false" />
</div>
```

**After:**
```razor
<QuickFilters TItem="EntityDto"
              Items="_allEntities"
              Filters="_quickFilters"
              OnFilterSelected="@HandleQuickFilter"
              ShowCount="true" />
```

**Code-behind additions:**
```csharp
private List<QuickFilter<EntityDto>> _quickFilters = new()
{
    new() { Id = "all", Label = "Tutti", Predicate = _ => true },
    new() { Id = "active", Label = "Attivi", Predicate = e => e.IsActive, Color = Color.Success },
    new() { Id = "recent", Label = "Recenti", Predicate = e => e.CreatedAt >= DateTime.UtcNow.AddDays(-30) }
};

private QuickFilter<EntityDto>? _activeQuickFilter;

private void HandleQuickFilter(QuickFilter<EntityDto>? filter)
{
    _activeQuickFilter = filter;
    StateHasChanged();
}
```

### Step 2: Implement OnRowClick Navigation

**Add to EFTable:**
```razor
<EFTable ...
         OnRowClick="@HandleRowClick">
```

**Add IJSRuntime injection:**
```razor
@inject IJSRuntime JSRuntime
```

**Add using:**
```razor
@using EventForge.Client.Extensions
```

**Implement method:**
```csharp
private void HandleRowClick(TableRowClickEventArgs<EntityDto> args)
{
    // Ctrl+Click or Cmd+Click opens in new tab
    if (args.MouseEventArgs.CtrlKey || args.MouseEventArgs.MetaKey)
    {
        JSRuntime.InvokeVoidAsync("open", $"/path/to/entity/{args.Item.Id}", "_blank");
        return;
    }

    // Normal click navigates in same tab
    NavigationManager.NavigateTo($"/path/to/entity/{args.Item.Id}");
}
```

### Step 3: Add Configurable Search

**Update column configurations:**
```csharp
private List<EFTableColumnConfiguration> _initialColumns = new()
{
    new() { PropertyName = "Name", DisplayName = "Nome", IsVisible = true, Order = 0, IsSearchable = true },
    new() { PropertyName = "Code", DisplayName = "Codice", IsVisible = true, Order = 1, IsSearchable = true },
    new() { PropertyName = "CreatedAt", DisplayName = "Creato il", IsVisible = true, Order = 2, IsSearchable = false }
};
```

**Update filtering logic:**
```csharp
private bool FilterEntity(EntityDto entity)
{
    // Quick filter
    if (_activeQuickFilter?.Predicate != null && !_activeQuickFilter.Predicate(entity))
        return false;

    // Configurable multi-column search
    if (!entity.MatchesSearchInColumns(
        _searchTerm,
        _initialColumns.Where(c => c.IsSearchable).Select(c => c.PropertyName)))
        return false;

    return true;
}

private IEnumerable<EntityDto> _filteredEntities => _allEntities.Where(FilterEntity);
```

### Step 4: Add Advanced Export

**Add to EFTable:**
```razor
<EFTable ...
         ShowExport="true"
         ShowExportDialog="true"
         ExcelFileName="NomeFile"
         IsDataFiltered="@HasActiveFilters()"
         TotalItemsCount="@_allEntities.Count">
```

**Implement HasActiveFilters:**
```csharp
private bool HasActiveFilters()
{
    return !string.IsNullOrWhiteSpace(_searchTerm) || 
           _activeQuickFilter != null;
}
```

### Step 5: Clean Up CSS

**Remove from page-specific CSS file (e.g., entity.css):**
```css
/* Remove this: */
.entity-top {
    flex: 0 0 auto;
    padding: 12px;
}
```

**Keep page-root:**
```css
/* Keep this: */
.entity-page-root {
    display: flex;
    flex-direction: column;
    height: 100vh;
    overflow: hidden;
}
```

### Step 6: Update Naming Conventions

**Standard variable names:**
```csharp
// Data collections
private List<EntityDto> _entities = new();
private HashSet<EntityDto> _selectedEntities = new();

// Filtering
private string _searchTerm = string.Empty;
private QuickFilter<EntityDto>? _activeQuickFilter;

// State
private bool _isLoadingEntities = false;

// References
private EFTable<EntityDto>? _efTable;
```

**Standard method names:**
```csharp
private async Task LoadEntitiesAsync()
private void HandleRowClick(TableRowClickEventArgs<EntityDto> args)
private void HandleQuickFilter(QuickFilter<EntityDto>? filter)
private bool HasActiveFilters()
private void OnSearchChanged()
```

## Special Cases

### Server-Side Pagination (e.g., StockOverview)

For pages with server-side pagination:

1. **Conditional Dashboard** - Keep dashboard only when not paginated:
```razor
@if (_totalPages <= 1)
{
    <div class="entity-top">
        <ManagementDashboard ... />
    </div>
}
```

2. **QuickFilters with API** - Update server filters on selection:
```csharp
private void HandleQuickFilter(QuickFilter<EntityDto>? filter)
{
    _activeQuickFilter = filter;
    
    // Update server-side filters
    if (filter?.Id == "in_stock")
        _filters.MinQuantity = 1;
    else if (filter?.Id == "low_stock")
        _filters.MaxQuantity = 10;
    
    // Reload from server
    _currentPage = 1;
    await LoadEntitiesAsync();
}
```

## Complete Migration Checklist

Use this checklist for each page you migrate:

- [ ] **Step 1: QuickFilters**
  - [ ] Remove ManagementDashboard section
  - [ ] Add QuickFilters component
  - [ ] Define _quickFilters list
  - [ ] Add _activeQuickFilter field
  - [ ] Implement HandleQuickFilter method
  - [ ] Remove _dashboardMetrics

- [ ] **Step 2: OnRowClick**
  - [ ] Add @inject IJSRuntime
  - [ ] Add @using EventForge.Client.Extensions
  - [ ] Add OnRowClick="@HandleRowClick" to EFTable
  - [ ] Implement HandleRowClick with Ctrl+Click support

- [ ] **Step 3: Configurable Search**
  - [ ] Add IsSearchable property to all columns in _initialColumns
  - [ ] Update filtering to use MatchesSearchInColumns extension
  - [ ] Create dedicated FilterEntity method

- [ ] **Step 4: Advanced Export**
  - [ ] Add ShowExport="true"
  - [ ] Add ShowExportDialog="true"
  - [ ] Add ExcelFileName
  - [ ] Add IsDataFiltered="@HasActiveFilters()"
  - [ ] Add TotalItemsCount
  - [ ] Implement HasActiveFilters method

- [ ] **Step 5: CSS Cleanup**
  - [ ] Remove .entity-top from CSS file
  - [ ] Remove responsive rules for .entity-top
  - [ ] Verify .eftable-wrapper still exists

- [ ] **Step 6: Code Quality**
  - [ ] Use DateTime.UtcNow (not DateTime.Now)
  - [ ] Follow standard naming conventions
  - [ ] Remove unused code/imports
  - [ ] Test build succeeds

## Common Issues

### Issue: QuickFilters don't show counts

**Solution:** Ensure you're passing the full collection to `Items`:
```razor
<QuickFilters Items="_allEntities"  <!-- NOT _filteredEntities -->
              Filters="_quickFilters" />
```

### Issue: Search doesn't work on some columns

**Solution:** Check that IsSearchable is set correctly:
```csharp
new() { PropertyName = "Name", IsSearchable = true },  // ✅
new() { PropertyName = "CreatedAt", IsSearchable = false }  // ✅ Don't search dates
```

### Issue: Export shows wrong count

**Solution:** Ensure IsDataFiltered and TotalItemsCount are set:
```razor
IsDataFiltered="@HasActiveFilters()"
TotalItemsCount="@_allEntities.Count"
```

### Issue: Row click doesn't work

**Solution:** Check that you injected IJSRuntime and added the HandleRowClick method.

## Before/After Comparison

### Before (Legacy Pattern)
- ❌ ManagementDashboard component (heavy, metrics)
- ❌ No row click navigation
- ❌ Search on all columns always
- ❌ Basic export (Excel only, all data)
- ❌ Inconsistent naming
- ❌ Page-specific CSS classes

### After (Modern Pattern)
- ✅ QuickFilters (lightweight, interactive)
- ✅ OnRowClick with Ctrl+Click support
- ✅ Configurable search per column
- ✅ Advanced export (dialog, column selection, filtered data)
- ✅ Standardized naming conventions
- ✅ Consolidated CSS

## Migration Timeline

For EventForge, we migrated pages in 5 PRs:

- **PR #1** - Standardized toolbar structure
- **PR #2** - Added OnRowClick navigation + configurable search
- **PR #3** - Added QuickFilters, removed ManagementDashboard
- **PR #4** - Added advanced export functionality
- **PR #5** - Rollout to remaining pages + cleanup

Each page takes approximately **30-60 minutes** to migrate following this guide.

## Need Help?

See also:
- [EFTABLE_STANDARD_PATTERN.md](./EFTABLE_STANDARD_PATTERN.md) - Complete reference pattern
- [Example: WarehouseManagement.razor](../EventForge.Client/Pages/Management/Warehouse/WarehouseManagement.razor) - Fully modernized
- [Example: ProductManagement.razor](../EventForge.Client/Pages/Management/Products/ProductManagement.razor) - Fully modernized

---

**Last Updated:** February 2026  
**Pattern Version:** 5.0 (Post PR #5)
