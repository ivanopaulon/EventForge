# EFTable Standard Pattern - EventForge

## Overview

This document defines the **standard pattern** for all management pages using EFTable in EventForge. Following this pattern ensures consistency, maintainability, and a unified user experience across all management interfaces.

### Goals
- ✅ **Uniform toolbar structure** across all management pages
- ✅ **Consistent HTML/CSS** structure and naming conventions  
- ✅ **Standard code-behind patterns** for state management
- ✅ **Clear filter guidelines** (max 2-3 inline filters)
- ✅ **Reusable templates** for rapid development

---

## Table of Contents
1. [HTML Structure](#html-structure)
2. [Toolbar Structure](#toolbar-structure)
3. [Code-Behind Pattern](#code-behind-pattern)
4. [CSS Classes](#css-classes)
5. [Filters Guidelines](#filters-guidelines)
6. [Complete Example](#complete-example)
7. [Migration Checklist](#migration-checklist)

---

## HTML Structure

### Standard Page Layout

All management pages MUST follow this exact HTML structure:

```razor
@page "/path/to/page"
@using Microsoft.AspNetCore.Authorization
@using EventForge.DTOs.[Namespace]
@using EventForge.DTOs.Common
@using EventForge.Client.Shared.Components
@using EventForge.Client.Shared.Components.Dashboard
@attribute [Authorize]

<!-- Standard service injections -->
@inject IAuthService AuthService
@inject I[Entity]Service [Entity]Service
@inject NavigationManager NavigationManager
@inject IAuthenticationDialogService AuthenticationDialogService
@inject ISnackbar Snackbar
@inject IDialogService DialogService
@inject ITranslationService TranslationService
@inject ILogger<[Entity]Management> Logger

<!-- Loading overlay -->
<PageLoadingOverlay Visible="_isLoading || _isLoading[Entities]"
                     Message="@(_isLoading ? TranslationService.GetTranslation("messages.loadingPage", "Caricamento pagina...") : TranslationService.GetTranslation("common.loading", "Caricamento..."))" />

@if (!_isLoading)
{
    <!-- Page root wrapper with entity-specific class -->
    <div class="[entity]-page-root">
        
        <!-- Dashboard section -->
        <div class="[entity]-top">
            <ManagementDashboard TItem="[EntityDto]"
                                 Items="_filtered[Entities]"
                                 Metrics="_dashboardMetrics"
                                 EntityType="[Entity]"
                                 AllowConfiguration="true"
                                 UseServerSide="false" />
        </div>

        <!-- Table section -->
        <div class="eftable-wrapper">
            <EFTable @ref="_efTable"
                     TItem="[EntityDto]"
                     Items="_filtered[Entities]"
                     MultiSelection="true"
                     SelectedItems="_selected[Entities]"
                     SelectedItemsChanged="_selectedItemsChangedCallback"
                     IsLoading="_isLoading[Entities]"
                     ComponentKey="[Entity]Management"
                     InitialColumnConfigurations="_initialColumns"
                     AllowDragDropGrouping="true">
                
                <!-- Toolbar content (see Toolbar Structure section) -->
                <ToolBarContent>
                    <!-- Content here -->
                </ToolBarContent>

                <!-- Header content with column configurations -->
                <HeaderContent Context="columnConfigurations">
                    <!-- Column headers here -->
                </HeaderContent>

                <!-- Row template -->
                <RowTemplate Context="item">
                    <!-- Row cells here -->
                </RowTemplate>

                <!-- No records content -->
                <NoRecordsContent>
                    <!-- Empty state here -->
                </NoRecordsContent>
            </EFTable>
        </div>
    </div>
}
```

### Naming Conventions

| Element | Pattern | Example |
|---------|---------|---------|
| **Page root class** | `[entity]-page-root` | `warehouse-page-root`, `vat-rate-page-root` |
| **Dashboard wrapper** | `[entity]-top` | `warehouse-top`, `vat-rate-top` |
| **Table wrapper** | `eftable-wrapper` | Always `eftable-wrapper` (consistent) |
| **Component key** | `[Entity]Management` | `WarehouseManagement`, `VatRateManagement` |

---

## Toolbar Structure

### Standard 4-Section Layout

The toolbar MUST be organized into **exactly 4 sections** in this order:

```razor
<ToolBarContent>
    <!-- SECTION 1: Title -->
    <MudText Typo="Typo.h5">
        @TranslationService.GetTranslation("[entity].[titleKey]", "Default Title")
    </MudText>
    <MudSpacer />
    
    <!-- SECTION 2: Search (if enabled) -->
    <MudTextField @bind-Value="_searchTerm"
                  @bind-Value:after="OnSearchChanged"
                  Label="@TranslationService.GetTranslation("[entity].search[Entities]", "Search...")"
                  Placeholder="@TranslationService.GetTranslation("[entity].searchPlaceholder", "Enter search term...")"
                  Variant="Variant.Outlined"
                  Adornment="Adornment.End"
                  AdornmentIcon="@Icons.Material.Outlined.Search"
                  Clearable="true"
                  Class="ef-input" />
    
    <!-- SECTION 3: Inline Filters (MAX 2-3) -->
    <!-- Example: MudSwitch for boolean filters -->
    <MudSwitch @bind-Value="_showOnlyActive"
               @bind-Value:after="OnFilterChanged"
               Label="@TranslationService.GetTranslation("[entity].onlyActive", "Only Active")"
               Color="Color.Primary"
               Class="ml-2" />
    
    <!-- Example: MudSelect for dropdown filters -->
    <MudSelect @bind-Value="_statusFilter"
               @bind-Value:after="OnFilterChanged"
               Label="@TranslationService.GetTranslation("[entity].status", "Status")"
               Variant="Variant.Outlined"
               Class="ml-2"
               Style="min-width: 150px;">
        <MudSelectItem Value="@("all")">All</MudSelectItem>
        <MudSelectItem Value="@("active")">Active</MudSelectItem>
        <MudSelectItem Value="@("inactive")">Inactive</MudSelectItem>
    </MudSelect>
    
    <!-- SECTION 4: Toolbar Actions -->
    <ManagementTableToolbar ShowSelectionBadge="true"
                            SelectedCount="@_selected[Entities].Count"
                            ShowRefresh="true"
                            ShowCreate="true"
                            ShowDelete="true"
                            CreateTooltip="[entity].createNew"
                            IsDisabled="_isLoading[Entities]"
                            OnRefresh="@Load[Entities]Async"
                            OnCreate="@Create[Entity]"
                            OnDelete="@DeleteSelected[Entities]" />
</ToolBarContent>
```

### Section Details

#### Section 1: Title
- **Always** use `MudText` with `Typo="Typo.h5"`
- **Always** use `TranslationService.GetTranslation()` with key and default
- Followed by `<MudSpacer />` to push content right

#### Section 2: Search (Optional)
- Use only if search is enabled
- **Always** use class `ef-input` for consistent styling
- **Always** use `Variant="Outlined"` for consistency
- **Always** include `Clearable="true"` for better UX
- **Always** bind to `OnSearchChanged` with debounce

#### Section 3: Inline Filters
- **MAXIMUM 2-3 filters** allowed in toolbar
- Allowed filter types:
  - ✅ `MudSwitch` for boolean toggles (fiscal/non-fiscal, active/inactive)
  - ✅ `MudSelect` for simple dropdowns (status, type, category)
- **NOT** allowed in toolbar:
  - ❌ Complex date pickers
  - ❌ Multiple text inputs
  - ❌ Range selectors
  - ❌ More than 3 filters total
- Use `Class="ml-2"` for consistent spacing

#### Section 4: Toolbar Actions
- **Always** use `ManagementTableToolbar` component
- Standard parameters:
  - `ShowSelectionBadge="true"` - show selected count
  - `SelectedCount` - bind to selected collection count
  - `ShowRefresh="true"` - enable refresh button
  - `ShowCreate="true"` - enable create button
  - `ShowDelete="true"` - enable delete button
  - `CreateTooltip` - translation key for create button
  - `IsDisabled` - bind to loading state
  - Event handlers: `OnRefresh`, `OnCreate`, `OnDelete`

---

## Code-Behind Pattern

### Standard Variable Naming

All management pages MUST use these exact naming conventions:

```csharp
@code {
    // ============================================
    // DIALOG OPTIONS
    // ============================================
    private readonly DialogOptions _auditDialogOptions = new() 
    { 
        FullScreen = true, 
        CloseButton = true,
        MaxWidth = MaxWidth.False
    };
    
    // ============================================
    // UI STATE MANAGEMENT
    // ============================================
    private bool _isLoading = true;
    private bool _isLoading[Entities] = false;

    // ============================================
    // FILTER AND SEARCH STATE
    // ============================================
    private string _searchTerm = string.Empty;
    private CancellationTokenSource? _searchDebounceCts;
    
    // Additional filters (max 2-3)
    private bool _showOnlyActive = false;
    private string _statusFilter = "all";

    // ============================================
    // DATA COLLECTIONS
    // ============================================
    private List<[EntityDto]> _[entities] = new();
    private HashSet<[EntityDto]> _selected[Entities] = new();

    // ============================================
    // COMPONENT REFERENCES
    // ============================================
    private EFTable<[EntityDto]> _efTable = null!;

    // ============================================
    // COLUMN CONFIGURATION
    // ============================================
    private List<EFTableColumnConfiguration> _initialColumns = new()
    {
        new() { PropertyName = "[Property1]", DisplayName = "[Display1]", IsVisible = true, Order = 0 },
        new() { PropertyName = "[Property2]", DisplayName = "[Display2]", IsVisible = true, Order = 1 },
        // ... more columns
    };

    // ============================================
    // DASHBOARD CONFIGURATION
    // ============================================
    private List<DashboardMetric<[EntityDto]>> _dashboardMetrics = new()
    {
        new()
        {
            Title = "Total [Entities]",
            Type = MetricType.Count,
            Icon = Icons.Material.Outlined.[Icon],
            Color = "primary",
            Description = "Total number of [entities]",
            Format = "N0"
        },
        // Minimum 4 metrics recommended
    };

    // ============================================
    // EVENT CALLBACKS
    // ============================================
    private EventCallback<HashSet<[EntityDto]>> _selectedItemsChangedCallback => 
        EventCallback.Factory.Create<HashSet<[EntityDto]>>(this, OnSelectedItemsChanged);

    // ============================================
    // COMPUTED PROPERTIES
    // ============================================
    /// <summary>
    /// Computed property for filtered [entities] based on search and filter criteria.
    /// </summary>
    private IEnumerable<[EntityDto]> _filtered[Entities] => 
        _[entities].Where(e => 
            // Search filter
            (string.IsNullOrEmpty(_searchTerm) || 
             e.[SearchProperty].Contains(_searchTerm, StringComparison.OrdinalIgnoreCase)) &&
            // Additional filters
            (!_showOnlyActive || e.IsActive));
}
```

### Standard Methods

Every management page MUST implement these methods:

```csharp
/// <summary>
/// Component initialization.
/// </summary>
protected override async Task OnInitializedAsync()
{
    try
    {
        var isAuthenticated = await AuthService.IsAuthenticatedAsync();
        if (!isAuthenticated)
        {
            await ShowLoginDialogAsync();
            return;
        }

        await Load[Entities]Async();
    }
    catch (Exception ex)
    {
        Snackbar.Add(TranslationService.GetTranslation("[entity].loadingPageError", 
            "Error loading page: {0}", ex.Message), Severity.Error);
        Logger.LogError(ex, "Error loading [entities] page");
    }
    finally
    {
        _isLoading = false;
    }
}

/// <summary>
/// Loads [entity] data.
/// </summary>
private async Task Load[Entities]Async()
{
    try
    {
        _isLoading[Entities] = true;
        var result = await [Entity]Service.Get[Entities]Async();
        if (result != null)
        {
            _[entities] = result.Items.ToList();
        }
    }
    catch (Exception ex)
    {
        Snackbar.Add(TranslationService.GetTranslation("[entity].loadingError", 
            "Error loading [entities]: {0}", ex.Message), Severity.Error);
        Logger.LogError(ex, "Error loading [entities]");
    }
    finally
    {
        _isLoading[Entities] = false;
    }
}

/// <summary>
/// Handles selection changes from EFTable.
/// </summary>
private void OnSelectedItemsChanged(HashSet<[EntityDto]> items)
{
    _selected[Entities] = items;
    StateHasChanged();
}

/// <summary>
/// Handles search term changes with debounce.
/// </summary>
private async Task OnSearchChanged()
{
    _searchDebounceCts?.Cancel();
    _searchDebounceCts = new CancellationTokenSource();
    var token = _searchDebounceCts.Token;
    
    try
    {
        await Task.Delay(300, token);
        if (!token.IsCancellationRequested)
        {
            StateHasChanged();
        }
    }
    catch (OperationCanceledException)
    {
        // Swallow cancellation
    }
}

/// <summary>
/// Handles filter changes.
/// </summary>
private void OnFilterChanged()
{
    StateHasChanged();
}

/// <summary>
/// Clears all active filters.
/// </summary>
private void ClearFilters()
{
    _searchTerm = string.Empty;
    _showOnlyActive = false;
    _statusFilter = "all";
    StateHasChanged();
}
```

---

## CSS Classes

### Standard Wrapper Classes

All management pages use these standard CSS classes:

```css
/* Page root wrapper - entity-specific */
.[entity]-page-root {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    padding: 1rem;
}

/* Dashboard wrapper - entity-specific */
.[entity]-top {
    width: 100%;
}

/* EFTable wrapper - ALWAYS THE SAME */
.eftable-wrapper {
    width: 100%;
    margin-top: 1rem;
}

/* Search input - standard styling */
.ef-input {
    max-width: 350px;
    min-width: 250px;
}
```

### Responsive Breakpoints

Standard responsive behavior for all management pages:

```css
/* Mobile (< 600px) */
@media (max-width: 599px) {
    .[entity]-page-root {
        padding: 0.5rem;
        gap: 0.5rem;
    }
    
    .ef-input {
        max-width: 100%;
        min-width: 200px;
    }
}

/* Tablet (600px - 959px) */
@media (min-width: 600px) and (max-width: 959px) {
    .[entity]-page-root {
        padding: 0.75rem;
        gap: 0.75rem;
    }
}

/* Desktop (>= 960px) */
@media (min-width: 960px) {
    .[entity]-page-root {
        padding: 1rem;
        gap: 1rem;
    }
}
```

### Spacing Standards

Use MudBlazor spacing classes consistently:

- `pa-2` / `pa-sm-3` / `pa-md-4` - Padding (responsive)
- `ma-2` / `ma-sm-3` / `ma-md-4` - Margin (responsive)
- `ml-2`, `mr-2`, `mt-2`, `mb-2` - Directional margin/padding
- `gap-1`, `gap-2`, `gap-4` - Flexbox gap

---

## Filters Guidelines

### The 2-3 Filter Rule

**CRITICAL:** Management pages MUST have **maximum 2-3 inline filters** in the toolbar.

### Allowed Inline Filters

✅ **MudSwitch** - For binary toggles:
```razor
<MudSwitch @bind-Value="_showOnlyFiscal"
           @bind-Value:after="OnFilterChanged"
           Label="@TranslationService.GetTranslation("warehouse.onlyFiscal", "Only Fiscal")"
           Color="Color.Primary"
           Class="ml-2" />
```

Examples:
- Only Active / Inactive
- Only Fiscal / Non-fiscal
- Only Refrigerated
- With VAT Number
- Has Images

✅ **MudSelect** - For simple dropdowns (3-5 options max):
```razor
<MudSelect @bind-Value="_typeFilter"
           @bind-Value:after="OnFilterChanged"
           Label="@TranslationService.GetTranslation("entity.type", "Type")"
           Variant="Variant.Outlined"
           Class="ml-2"
           Style="min-width: 150px;">
    <MudSelectItem Value="@("all")">All</MudSelectItem>
    <MudSelectItem Value="@("type1")">Type 1</MudSelectItem>
    <MudSelectItem Value="@("type2")">Type 2</MudSelectItem>
</MudSelect>
```

Examples:
- Status (Active, Suspended, Deleted)
- Type (Customer, Supplier, Both)
- Category (A, B, C)
- Priority (High, Medium, Low)

### NOT Allowed Inline

❌ **Date pickers** - too complex for toolbar
❌ **Range selectors** - use quick filters (future PR)
❌ **Multiple text inputs** - confusing UX
❌ **More than 3 filters** - toolbar becomes cluttered
❌ **Nested filters** - too complex

### Complex Filters → Future PR

If you need more than 2-3 filters, they will be handled in **PR #3 (Quick Filters)**:
- Advanced date ranges
- Multi-select dropdowns
- Numeric ranges
- Custom filter combinations
- Saved filter presets

---

## Complete Example

### WarehouseManagement.razor (Reference Implementation)

This is the **gold standard** example to follow:

```razor
@page "/warehouse/facilities"
@using Microsoft.AspNetCore.Authorization
@using EventForge.DTOs.Warehouse
@using EventForge.DTOs.Common
@using EventForge.Client.Shared.Components
@using EventForge.Client.Shared.Components.Dashboard
@attribute [Authorize]
@inject IAuthService AuthService
@inject IWarehouseService WarehouseService
@inject NavigationManager NavigationManager
@inject IAuthenticationDialogService AuthenticationDialogService
@inject ISnackbar Snackbar
@inject IDialogService DialogService
@inject ITranslationService TranslationService
@inject ILogger<WarehouseManagement> Logger

<PageLoadingOverlay Visible="_isLoading || _isLoadingFacilities"
                     Message="@(_isLoading ? TranslationService.GetTranslation("messages.loadingPage", "Caricamento pagina...") : TranslationService.GetTranslation("common.loading", "Caricamento..."))" />

@if (!_isLoading)
{
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
            <EFTable @ref="_efTable"
                 TItem="StorageFacilityDto"
                 Items="_filteredFacilities"
                 MultiSelection="true"
                 SelectedItems="_selectedFacilities"
                 SelectedItemsChanged="_selectedItemsChangedCallback"
                 IsLoading="_isLoadingFacilities"
                 ComponentKey="WarehouseManagement"
                 InitialColumnConfigurations="_initialColumns"
                 AllowDragDropGrouping="true">
            <ToolBarContent>
                <!-- SECTION 1: Title -->
                <MudText Typo="Typo.h5">
                    @TranslationService.GetTranslation("warehouse.facilityManagement", "Gestione Magazzini")
                </MudText>
                <MudSpacer />
                
                <!-- SECTION 2: Search -->
                <MudTextField @bind-Value="_searchTerm"
                              @bind-Value:after="OnSearchChanged"
                              Label="@TranslationService.GetTranslation("warehouse.searchFacilities", "Cerca magazzini")"
                              Placeholder="@TranslationService.GetTranslation("warehouse.searchPlaceholder", "Inserisci nome o codice...")"
                              Variant="Variant.Outlined"
                              Adornment="Adornment.End"
                              AdornmentIcon="@Icons.Material.Outlined.Search"
                              Clearable="true"
                              Class="ef-input" />
                
                <!-- SECTION 3: Inline Filters (2 switches - PERFECT) -->
                <MudSwitch @bind-Value="_showOnlyFiscal"
                           @bind-Value:after="OnFilterChanged"
                           Label="@TranslationService.GetTranslation("warehouse.onlyFiscal", "Solo fiscali")"
                           Color="Color.Primary"
                           Class="ml-2" />
                <MudSwitch @bind-Value="_showOnlyRefrigerated"
                           @bind-Value:after="OnFilterChanged"
                           Label="@TranslationService.GetTranslation("warehouse.onlyRefrigerated", "Solo refrigerati")"
                           Color="Color.Info"
                           Class="ml-2" />
                
                <!-- SECTION 4: Toolbar Actions -->
                <ManagementTableToolbar ShowSelectionBadge="true"
                                        SelectedCount="@_selectedFacilities.Count"
                                        ShowRefresh="true"
                                        ShowCreate="true"
                                        ShowDelete="true"
                                        CreateTooltip="warehouse.createNewFacility"
                                        IsDisabled="_isLoadingFacilities"
                                        OnRefresh="@LoadStorageFacilitiesAsync"
                                        OnCreate="@CreateStorageFacility"
                                        OnDelete="@DeleteSelectedFacilities" />
            </ToolBarContent>
            
            <!-- HeaderContent, RowTemplate, NoRecordsContent omitted for brevity -->
            </EFTable>
        </div>
    </div>
}

@code {
    // All standard variables and methods as documented above
}
```

**Why this is the gold standard:**
- ✅ Perfect 4-section toolbar structure
- ✅ Exactly 2 inline filters (not too many)
- ✅ Correct naming conventions throughout
- ✅ ManagementDashboard + EFTable properly integrated
- ✅ All translations use TranslationService
- ✅ Clean, readable, maintainable code

---

## Migration Checklist

Use this checklist when migrating an existing management page to the standard pattern:

### 1. HTML Structure
- [ ] Page uses `[entity]-page-root` wrapper class
- [ ] Dashboard in `[entity]-top` wrapper
- [ ] EFTable in `eftable-wrapper` wrapper
- [ ] No `MudContainer` usage (use div wrappers instead)

### 2. Toolbar
- [ ] Section 1: Title with `MudText Typo="Typo.h5"`
- [ ] `<MudSpacer />` after title
- [ ] Section 2: Search with `Class="ef-input"`
- [ ] Section 3: Maximum 2-3 inline filters
- [ ] Section 4: `ManagementTableToolbar` with standard parameters

### 3. Code-Behind
- [ ] Variables follow naming conventions
  - [ ] `_isLoading` and `_isLoading[Entities]`
  - [ ] `_searchTerm` with `_searchDebounceCts`
  - [ ] `_[entities]` and `_selected[Entities]`
  - [ ] `_efTable` reference
  - [ ] `_initialColumns` configuration
  - [ ] `_dashboardMetrics` (minimum 4 metrics)
- [ ] `_filtered[Entities]` computed property
- [ ] `_selectedItemsChangedCallback` event callback
- [ ] All standard methods implemented

### 4. Filters
- [ ] No more than 2-3 inline filters
- [ ] Only `MudSwitch` or `MudSelect` (no complex filters)
- [ ] All filters use `@bind-Value:after="OnFilterChanged"`
- [ ] `ClearFilters()` method resets all filters

### 5. Translations
- [ ] All user-facing text uses `TranslationService.GetTranslation()`
- [ ] Translation keys follow pattern: `[entity].[key]`
- [ ] Default text provided as fallback

### 6. CSS
- [ ] Correct wrapper classes used
- [ ] `ef-input` class on search field
- [ ] Responsive spacing with MudBlazor classes
- [ ] No inline styles (except minimal width adjustments)

### 7. Dashboard
- [ ] Minimum 4 metrics configured
- [ ] Metrics have icons, colors, descriptions
- [ ] `AllowConfiguration="true"` enabled
- [ ] Metrics use `TranslationService` for titles

### 8. Testing
- [ ] Page builds without errors
- [ ] No regressions in functionality
- [ ] Toolbar looks consistent with other pages
- [ ] Filters work correctly
- [ ] Dashboard displays properly
- [ ] Search and selection work
- [ ] Responsive on mobile/tablet/desktop

---

## Summary

### Key Takeaways

1. **Always use the 4-section toolbar structure** - no exceptions
2. **Maximum 2-3 inline filters** - complex filters go elsewhere
3. **Follow naming conventions exactly** - consistency is key
4. **Use standard wrapper classes** - `[entity]-page-root`, `[entity]-top`, `eftable-wrapper`
5. **Minimum 4 dashboard metrics** - provides good overview
6. **Always use TranslationService** - never hardcode strings
7. **Reference WarehouseManagement** - when in doubt, copy this pattern

### Benefits

✅ **Consistency** - All pages look and feel the same  
✅ **Maintainability** - Easy to update all pages at once  
✅ **Onboarding** - New developers learn one pattern  
✅ **Quality** - Less bugs through standardization  
✅ **UX** - Users know what to expect everywhere  

### Next Steps

After implementing this pattern:
- **PR #2** - Row click + Configurable search
- **PR #3** - Advanced filters (quick filters panel)
- **PR #4** - Advanced export features
- **PR #5** - Rollout to all remaining pages

---

**Document Version:** 1.0  
**Last Updated:** 2026-02-02  
**Author:** EventForge Team  
**Status:** ✅ Active Standard

## Row Click Navigation Pattern

All management pages should implement row click navigation for quick access to detail pages.

### Implementation

```csharp
// 1. Inject JSRuntime (if not already injected)
@inject IJSRuntime JSRuntime

// 2. Add OnRowClick parameter to EFTable
<EFTable ...
         OnRowClick="@HandleRowClick">
</EFTable>

// 3. Implement handler method
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

### Requirements

- ✅ Must support Ctrl+Click to open in new tab
- ✅ Must not interfere with checkbox selection
- ✅ Must not interfere with action buttons in row
- ✅ Cursor should change to pointer on hover (handled by CSS)

## Configurable Search Pattern

Implement searchable column configuration for better UX and performance.

### Step 1: Add using statement

```csharp
@using EventForge.Client.Extensions
```

### Step 2: Define searchable columns

```csharp
private List<EFTableColumnConfiguration> _initialColumns = new()
{
    new() { PropertyName = "Name", DisplayName = "Name", IsVisible = true, Order = 0, IsSearchable = true },
    new() { PropertyName = "Code", DisplayName = "Code", IsVisible = true, Order = 1, IsSearchable = true },
    new() { PropertyName = "Price", DisplayName = "Price", IsVisible = true, Order = 2, IsSearchable = false },
    new() { PropertyName = "Stock", DisplayName = "Stock", IsVisible = true, Order = 3, IsSearchable = false }
};
```

### Step 3: Implement filter using extension method

```csharp
private IEnumerable<EntityDto> _filteredItems => 
    _allItems.Where(item => FilterItem(item));

private bool FilterItem(EntityDto item)
{
    // Configurable multi-column search
    if (!item.MatchesSearchInColumns(
        _searchTerm,
        _initialColumns.Where(c => c.IsSearchable).Select(c => c.PropertyName)))
        return false;
    
    // Additional filters...
    if (_statusFilter.HasValue && item.Status != _statusFilter)
        return false;
    
    return true;
}
```

### Benefits

1. **User control**: Users can toggle searchability per column via configuration dialog
2. **Performance**: Avoids searching non-text fields (dates, numbers, IDs)
3. **Persistence**: Configuration saved in user preferences
4. **Clean code**: Extension method keeps filter logic readable

### Guidelines

- Mark **text fields** as searchable: Name, Code, Description, Email, Address
- Mark **non-text fields** as not searchable: Dates, Numbers, Booleans, IDs
- Mark **reference fields** as not searchable: Foreign keys, enums (unless display text)
- Consider **performance**: Don't search large text fields unnecessarily

