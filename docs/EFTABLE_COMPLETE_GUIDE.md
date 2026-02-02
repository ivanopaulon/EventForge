# EFTable Complete Guide - EventForge Pattern v2.0

**Last Updated**: February 2026  
**Issue**: [#1014](https://github.com/ivanopaulon/EventForge/issues/1014)  
**Status**: ✅ Production Ready

---

## 📚 Table of Contents

1. [Introduction](#1-introduction)
2. [Quick Start](#2-quick-start)
3. [Core Features](#3-core-features)
4. [Standard Pattern](#4-standard-pattern)
5. [Migration Guide](#5-migration-guide)
6. [API Reference](#6-api-reference)
7. [Examples](#7-examples)
8. [Best Practices](#8-best-practices)

---

## 1. Introduction

### What is EFTable?

EFTable is EventForge's enterprise-grade data table component providing:

✅ **Click-to-Detail Navigation** - Row click with Ctrl+Click support  
✅ **Configurable Search** - User-controlled searchable columns  
✅ **Advanced Export** - Column selection, filtered data, Excel/CSV  
✅ **QuickFilters** - Interactive chip-based filtering with counts  
✅ **Inline Filters** - Simple toolbar filters (max 2-3)  
✅ **Column Configuration** - Show/hide, reorder, persist preferences  
✅ **Drag-Drop Grouping** - Multi-level grouping (client-side)  
✅ **Responsive** - Mobile-friendly design  

### Implementation Status

**Issue #1014**: ✅ **Complete** (100%)  
**Pages Migrated**: 16/16 (100%)  
**Components**: EFTable, QuickFilters, ExportDialog  
**Documentation**: Comprehensive guides available  

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Management Page                       │
├─────────────────────────────────────────────────────────┤
│  QuickFilters Component                                 │
│  ┌──────────┬──────────┬──────────┬──────────┐         │
│  │ All (150)│Active(80)│Recent(25)│Custom    │         │
│  └──────────┴──────────┴──────────┴──────────┘         │
├─────────────────────────────────────────────────────────┤
│  EFTable Component                                      │
│  ┌────────────────────────────────────────────────┐    │
│  │ Toolbar                                        │    │
│  │ [Title] [Search] [Filters] [Export] [Add]     │    │
│  ├────────────────────────────────────────────────┤    │
│  │ Column Headers (Configurable, Sortable)       │    │
│  ├────────────────────────────────────────────────┤    │
│  │ Data Rows (Clickable, Multi-select)           │    │
│  │ • Row click → Navigate to detail              │    │
│  │ • Ctrl+Click → Open in new tab                │    │
│  ├────────────────────────────────────────────────┤    │
│  │ Footer (Pagination, Item Count)               │    │
│  └────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
```

---

## 2. Quick Start

### Minimal Setup

```razor
@page "/management/products"
@using EventForge.Client.Shared.Components
@inject NavigationManager NavigationManager
@inject IJSRuntime JSRuntime

<div class="management-page-root">
    <QuickFilters TItem="ProductDto"
                  Items="_allProducts"
                  Filters="_quickFilters"
                  OnFilterSelected="@HandleQuickFilter"
                  ShowCount="true" />

    <div class="eftable-wrapper">
        <EFTable TItem="ProductDto"
                 Items="_filteredProducts"
                 OnRowClick="@HandleRowClick"
                 ComponentKey="ProductManagement"
                 InitialColumnConfigurations="_columns">
            <ToolBarContent>
                <MudText Typo="Typo.h6">Products</MudText>
                <MudSpacer />
                <MudButton StartIcon="@Icons.Material.Outlined.Add" 
                          Color="Color.Primary" 
                          OnClick="@CreateProduct">
                    Add Product
                </MudButton>
            </ToolBarContent>
            
            <HeaderContent Context="columnConfigs">
                @foreach (var column in columnConfigs.Where(c => c.IsVisible).OrderBy(c => c.Order))
                {
                    @if (column.PropertyName == "Name")
                    {
                        <MudTh>Name</MudTh>
                    }
                    @if (column.PropertyName == "Code")
                    {
                        <MudTh>Code</MudTh>
                    }
                }
                <MudTh>Actions</MudTh>
            </HeaderContent>
            
            <RowTemplate>
                <MudTd DataLabel="Name">@context.Name</MudTd>
                <MudTd DataLabel="Code">@context.Code</MudTd>
                <MudTd>
                    <MudIconButton Icon="@Icons.Material.Outlined.Edit" 
                                  OnClick="@(() => EditProduct(context.Id))" />
                </MudTd>
            </RowTemplate>
        </EFTable>
    </div>
</div>

@code {
    private List<ProductDto> _allProducts = new();
    private List<EFTableColumnConfiguration> _columns = new()
    {
        new() { PropertyName = "Name", DisplayName = "Name", IsVisible = true, Order = 0, IsSearchable = true },
        new() { PropertyName = "Code", DisplayName = "Code", IsVisible = true, Order = 1, IsSearchable = true }
    };
    
    private List<QuickFilter<ProductDto>> _quickFilters = new()
    {
        new() { Id = "all", Label = "All", Predicate = _ => true },
        new() { Id = "active", Label = "Active", Predicate = p => p.IsActive, Color = Color.Success }
    };
    
    private QuickFilter<ProductDto>? _activeQuickFilter;
    
    private IEnumerable<ProductDto> _filteredProducts => 
        _allProducts.Where(p => _activeQuickFilter?.Predicate?.Invoke(p) ?? true);
    
    private void HandleQuickFilter(QuickFilter<ProductDto>? filter)
    {
        _activeQuickFilter = filter;
        StateHasChanged();
    }
    
    private void HandleRowClick(TableRowClickEventArgs<ProductDto> args)
    {
        if (args.MouseEventArgs.CtrlKey || args.MouseEventArgs.MetaKey)
        {
            JSRuntime.InvokeVoidAsync("open", $"/products/{args.Item.Id}", "_blank");
            return;
        }
        NavigationManager.NavigateTo($"/products/{args.Item.Id}");
    }
    
    private void CreateProduct() { }
    private void EditProduct(Guid id) { }
}
```

---

## 3. Core Features

### 3.1 Row Click Navigation

**Behavior:**
- Single click → Navigate to detail page (same tab)
- Ctrl+Click (Windows/Linux) or Cmd+Click (Mac) → Open in new tab

**Implementation:**
```csharp
private void HandleRowClick(TableRowClickEventArgs<TItem> args)
{
    // Check for modifier keys
    if (args.MouseEventArgs.CtrlKey || args.MouseEventArgs.MetaKey)
    {
        // Open in new tab
        JSRuntime.InvokeVoidAsync("open", $"/path/{args.Item.Id}", "_blank");
        return;
    }
    
    // Navigate in same tab
    NavigationManager.NavigateTo($"/path/{args.Item.Id}");
}
```

**CSS (Automatic):**
```css
/* Cursor pointer on rows */
.eftable-wrapper .mud-table-row {
    cursor: pointer;
}

/* Hover feedback */
.eftable-wrapper .mud-table-row:hover {
    background-color: var(--mud-palette-action-default-hover) !important;
}
```

### 3.2 Configurable Search

Users can configure which columns are searchable via the column configuration dialog.

**Setup:**
```csharp
private List<EFTableColumnConfiguration> _initialColumns = new()
{
    new() { 
        PropertyName = "Name", 
        DisplayName = "Nome", 
        IsVisible = true, 
        Order = 0, 
        IsSearchable = true  // User can toggle this
    },
    new() { 
        PropertyName = "Code", 
        DisplayName = "Codice", 
        IsVisible = true, 
        Order = 1, 
        IsSearchable = true  // User can toggle this
    },
    new() { 
        PropertyName = "Description", 
        DisplayName = "Descrizione", 
        IsVisible = true, 
        Order = 2, 
        IsSearchable = false  // Not searchable by default
    }
};
```

**Search Logic:**
```csharp
private bool FilterItem(TItem item)
{
    // Search filter
    if (!string.IsNullOrWhiteSpace(_searchTerm))
    {
        // Only search in columns marked as searchable
        var searchableColumns = _currentColumns.Where(c => c.IsSearchable).ToList();
        if (searchableColumns.Any())
        {
            var matchesSearch = false;
            foreach (var column in searchableColumns)
            {
                var value = GetPropertyValue(item, column.PropertyName);
                if (value?.ToString()?.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) == true)
                {
                    matchesSearch = true;
                    break;
                }
            }
            if (!matchesSearch) return false;
        }
    }
    
    return true;
}
```

### 3.3 QuickFilters Component

**Purpose:** Replace static dashboard metrics with interactive, clickable filters.

**Features:**
- Real-time count display
- Chip-based UI
- Single-select behavior (click to activate, click again to clear)
- Color and icon customization

**Full Example:**
```razor
<QuickFilters TItem="VatRateDto"
              Items="_allVatRates"
              Filters="_quickFilters"
              OnFilterSelected="@HandleQuickFilter"
              ShowCount="true" />

@code {
    private List<QuickFilter<VatRateDto>> _quickFilters = new()
    {
        new() 
        { 
            Id = "all", 
            Label = "Tutti",
            Predicate = _ => true,
            Description = "Mostra tutte le aliquote"
        },
        new() 
        { 
            Id = "standard", 
            Label = "Standard (22%)",
            Predicate = v => Math.Abs(v.Percentage - 22m) < 0.01m,
            Color = Color.Primary,
            Icon = Icons.Material.Outlined.CheckCircle
        },
        new() 
        { 
            Id = "reduced", 
            Label = "Ridotte",
            Predicate = v => v.Percentage < 22m && v.Percentage > 0m,
            Color = Color.Info,
            Icon = Icons.Material.Outlined.TrendingDown
        },
        new() 
        { 
            Id = "exempt", 
            Label = "Esenti",
            Predicate = v => v.Percentage == 0m,
            Color = Color.Success,
            Icon = Icons.Material.Outlined.Block
        }
    };
    
    private QuickFilter<VatRateDto>? _activeQuickFilter;
    
    private void HandleQuickFilter(QuickFilter<VatRateDto>? filter)
    {
        _activeQuickFilter = filter;
        StateHasChanged();
    }
}
```

### 3.4 Advanced Export

Export dialog with:
- Column selection (choose which columns to export)
- Format selection (CSV, Excel)
- Exports only filtered/visible data
- Respects current search and filters

**Setup:**
```razor
<EFTable TItem="ProductDto"
         Items="_filteredProducts"
         ShowExport="true"
         OnExport="@HandleExport"
         ExportFileName="Products"
         ComponentKey="ProductManagement">
```

**Export Handler:**
```csharp
private async Task HandleExport(ExportRequest request)
{
    var dataToExport = _filteredProducts.ToList();
    
    var columnsToExport = request.SelectedColumns
        .Where(c => c.IsSelected)
        .OrderBy(c => c.Order)
        .ToList();
    
    if (request.Format == "Excel")
    {
        var excelData = await ExportService.ExportToExcel(
            dataToExport, 
            columnsToExport,
            request.FileName ?? "Export"
        );
        await FileService.DownloadFile(excelData, $"{request.FileName}.xlsx");
    }
    else // CSV
    {
        var csvData = ExportService.ExportToCsv(dataToExport, columnsToExport);
        await FileService.DownloadFile(csvData, $"{request.FileName}.csv");
    }
}
```

### 3.5 Column Configuration

Users can:
- Show/hide columns
- Reorder columns via drag-drop
- Mark columns as searchable
- Save preferences (persisted in localStorage)

**Access:** Click the column configuration icon in the toolbar

**Persistence:** Automatically saved per `ComponentKey`

### 3.6 Drag-Drop Grouping

Enable multi-level grouping by dragging column headers to the grouping area.

**Setup:**
```razor
<EFTable TItem="ProductDto"
         Items="_products"
         AllowDragDropGrouping="true"
         ComponentKey="ProductManagement">
```

**User Experience:**
1. Drag column header to "Drop here to group" area
2. Multiple levels supported
3. Expand/collapse groups
4. Clear grouping with "Clear" button

---

## 4. Standard Pattern

### 4.1 CSS Structure

**New unified approach (as of PR #7):**

```css
/* Base class for all management pages */
.management-page-root {
    display: flex;
    flex-direction: column;
    gap: 1rem;
    padding: 1rem;
}

/* Legacy entity-specific classes still supported but deprecated */
.product-page-root,
.warehouse-page-root,
/* ... etc */ {
    /* Same styles for backward compatibility */
}

/* Standard wrapper for EFTable */
.eftable-wrapper {
    width: 100%;
    margin-top: 1rem;
    display: flex;
    flex-direction: column;
    min-height: 0;
}

/* Standard input styling */
.ef-input {
    max-width: 350px;
    min-width: 250px;
}

.ef-select {
    max-width: 250px;
    min-width: 150px;
}
```

**Usage:**
```razor
<div class="management-page-root">
    <!-- QuickFilters -->
    <QuickFilters ... />
    
    <!-- EFTable -->
    <div class="eftable-wrapper">
        <EFTable ... />
    </div>
</div>
```

### 4.2 Page Structure

**Standard Layout:**

```razor
@page "/management/entity"
@using Microsoft.AspNetCore.Authorization
@using EventForge.DTOs.YourNamespace
@using EventForge.DTOs.Common
@using EventForge.Client.Shared.Components
@attribute [Authorize]

@inject IAuthService AuthService
@inject IEntityService EntityService
@inject NavigationManager NavigationManager
@inject ISnackbar Snackbar
@inject IDialogService DialogService
@inject ITranslationService TranslationService
@inject IJSRuntime JSRuntime

<PageLoadingOverlay Visible="_isLoading" Message="..." />

@if (!_isLoading)
{
    <div class="management-page-root">
        <!-- QuickFilters -->
        <QuickFilters TItem="EntityDto"
                      Items="_allEntities"
                      Filters="_quickFilters"
                      OnFilterSelected="@HandleQuickFilter"
                      ShowCount="true" />
        
        <!-- EFTable -->
        <div class="eftable-wrapper">
            <EFTable TItem="EntityDto"
                     Items="_filteredEntities"
                     OnRowClick="@HandleRowClick"
                     MultiSelection="true"
                     SelectedItems="_selectedEntities"
                     SelectedItemsChanged="_selectedItemsChangedCallback"
                     IsLoading="_isLoadingEntities"
                     ComponentKey="EntityManagement"
                     InitialColumnConfigurations="_initialColumns"
                     AllowDragDropGrouping="true">
                
                <ToolBarContent>
                    <!-- Section 1: Title -->
                    <MudText Typo="Typo.h6">Entity Management</MudText>
                    <MudSpacer />
                    
                    <!-- Section 2: Search (optional inline filters) -->
                    <MudTextField @bind-Value="_searchTerm"
                                  Placeholder="Search..."
                                  Adornment="Adornment.Start"
                                  AdornmentIcon="@Icons.Material.Outlined.Search"
                                  Class="ef-input"
                                  Immediate="false"
                                  DebounceInterval="300" />
                    
                    <!-- Section 3: Actions -->
                    <MudButton StartIcon="@Icons.Material.Outlined.Add"
                              Color="Color.Primary"
                              OnClick="@CreateEntity">
                        Add
                    </MudButton>
                </ToolBarContent>
                
                <HeaderContent Context="columnConfigurations">
                    @foreach (var column in columnConfigurations.Where(c => c.IsVisible).OrderBy(c => c.Order))
                    {
                        @if (column.PropertyName == "Name")
                        {
                            <MudTh>Name</MudTh>
                        }
                        @if (column.PropertyName == "Code")
                        {
                            <MudTh>Code</MudTh>
                        }
                    }
                    <MudTh>Actions</MudTh>
                </HeaderContent>
                
                <RowTemplate>
                    <MudTd DataLabel="Name">@context.Name</MudTd>
                    <MudTd DataLabel="Code">@context.Code</MudTd>
                    <MudTd>
                        <MudIconButton Icon="@Icons.Material.Outlined.Edit"
                                      OnClick="@(() => EditEntity(context.Id))" />
                    </MudTd>
                </RowTemplate>
                
                <NoRecordsContent>
                    <MudText Typo="Typo.body1">No entities found</MudText>
                </NoRecordsContent>
            </EFTable>
        </div>
    </div>
}

@code {
    // Loading state
    private bool _isLoading = true;
    private bool _isLoadingEntities = false;
    
    // Data
    private List<EntityDto> _allEntities = new();
    private HashSet<EntityDto> _selectedEntities = new();
    
    // Quick filters
    private List<QuickFilter<EntityDto>> _quickFilters = new();
    private QuickFilter<EntityDto>? _activeQuickFilter;
    
    // Search and filters
    private string _searchTerm = string.Empty;
    
    // Column configuration
    private List<EFTableColumnConfiguration> _initialColumns = new();
    
    // Computed
    private IEnumerable<EntityDto> _filteredEntities => 
        _allEntities.Where(e => FilterItem(e));
    
    private EventCallback<HashSet<EntityDto>> _selectedItemsChangedCallback =>
        EventCallback.Factory.Create<HashSet<EntityDto>>(this, items => _selectedEntities = items);
    
    protected override async Task OnInitializedAsync()
    {
        await LoadData();
        _isLoading = false;
    }
    
    private async Task LoadData()
    {
        _isLoadingEntities = true;
        try
        {
            _allEntities = await EntityService.GetAllAsync();
        }
        finally
        {
            _isLoadingEntities = false;
        }
    }
    
    private bool FilterItem(EntityDto item)
    {
        // Quick filter
        if (_activeQuickFilter?.Predicate != null && !_activeQuickFilter.Predicate(item))
            return false;
        
        // Search filter
        if (!string.IsNullOrWhiteSpace(_searchTerm))
        {
            if (!item.Name.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) &&
                !item.Code.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        
        return true;
    }
    
    private void HandleQuickFilter(QuickFilter<EntityDto>? filter)
    {
        _activeQuickFilter = filter;
        StateHasChanged();
    }
    
    private void HandleRowClick(TableRowClickEventArgs<EntityDto> args)
    {
        if (args.MouseEventArgs.CtrlKey || args.MouseEventArgs.MetaKey)
        {
            JSRuntime.InvokeVoidAsync("open", $"/entity/{args.Item.Id}", "_blank");
            return;
        }
        NavigationManager.NavigateTo($"/entity/{args.Item.Id}");
    }
    
    private void CreateEntity() { }
    private void EditEntity(Guid id) { }
}
```

---

## 5. Migration Guide

### From ManagementDashboard to QuickFilters

**Step 1: Remove ManagementDashboard**

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

**Step 2: Add QuickFilters Definition**

```csharp
private List<QuickFilter<EntityDto>> _quickFilters = new()
{
    new() { Id = "all", Label = "All", Predicate = _ => true },
    new() { Id = "active", Label = "Active", Predicate = e => e.IsActive, Color = Color.Success }
};

private QuickFilter<EntityDto>? _activeQuickFilter;

private void HandleQuickFilter(QuickFilter<EntityDto>? filter)
{
    _activeQuickFilter = filter;
    StateHasChanged();
}
```

**Step 3: Update Filtering Logic**

```csharp
private IEnumerable<EntityDto> _filteredEntities => 
    _allEntities.Where(e => FilterItem(e));

private bool FilterItem(EntityDto item)
{
    if (_activeQuickFilter?.Predicate != null && !_activeQuickFilter.Predicate(item))
        return false;
    
    // Other filters...
    return true;
}
```

**Step 4: Remove Unused Code**

Remove:
- `_dashboardMetrics` field
- Dashboard configuration methods
- `@using EventForge.Client.Shared.Components.Dashboard` (if not used elsewhere)

**Step 5: Update CSS Class**

Change from entity-specific class to unified class:

**Before:**
```razor
<div class="product-page-root">
```

**After:**
```razor
<div class="management-page-root">
```

---

## 6. API Reference

### EFTable Parameters

#### Data & Display
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Items` | `IEnumerable<TItem>?` | `null` | Client-side data |
| `ServerData` | `Func<TableState, CancellationToken, Task<TableData<TItem>>>?` | `null` | Server-side data provider |
| `IsLoading` | `bool` | `false` | Shows loading indicator |
| `ComponentKey` | `string` | `""` | Unique key for saving preferences |

#### Selection
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `MultiSelection` | `bool` | `false` | Enable multi-row selection |
| `SelectedItems` | `HashSet<TItem>` | `new()` | Selected items |
| `SelectedItemsChanged` | `EventCallback<HashSet<TItem>>` | - | Selection change event |

#### Navigation
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `OnRowClick` | `EventCallback<TableRowClickEventArgs<TItem>>` | - | Row click handler |

#### Column Configuration
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `InitialColumnConfigurations` | `List<EFTableColumnConfiguration>` | `new()` | Initial column setup |
| `AllowDragDropGrouping` | `bool` | `false` | Enable drag-drop grouping |

#### Export
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ShowExport` | `bool` | `false` | Show export button |
| `OnExport` | `EventCallback<ExportRequest>` | - | Export handler |
| `ExportFileName` | `string?` | `null` | Default export filename |

#### Content Slots
| Slot | Description |
|------|-------------|
| `ToolBarContent` | Custom toolbar content |
| `HeaderContent` | Table header (use `Context="columnConfigurations"`) |
| `RowTemplate` | Row template (use `Context="item"`) |
| `NoRecordsContent` | Content shown when no data |

### QuickFilters Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Items` | `IEnumerable<TItem>` | `required` | All items for count calculation |
| `Filters` | `List<QuickFilter<TItem>>` | `required` | Filter definitions |
| `OnFilterSelected` | `EventCallback<QuickFilter<TItem>?>` | `required` | Filter selection handler |
| `ShowCount` | `bool` | `true` | Show item counts |

### QuickFilter Model

```csharp
public class QuickFilter<TItem>
{
    public string Id { get; set; }              // Unique identifier
    public string Label { get; set; }           // Display text
    public Func<TItem, bool>? Predicate { get; set; }  // Filter logic
    public Color Color { get; set; }            // MudBlazor color
    public string? Icon { get; set; }           // MudBlazor icon
    public string? Description { get; set; }    // Tooltip text
}
```

### EFTableColumnConfiguration Model

```csharp
public class EFTableColumnConfiguration
{
    public string PropertyName { get; set; }    // Property to display
    public string DisplayName { get; set; }     // Column header text
    public bool IsVisible { get; set; }         // Show/hide column
    public int Order { get; set; }              // Column order
    public bool IsSearchable { get; set; }      // Include in search
}
```

---

## 7. Examples

### Example 1: Simple Product List

```razor
<div class="management-page-root">
    <QuickFilters TItem="ProductDto"
                  Items="_products"
                  Filters="_filters"
                  OnFilterSelected="@(f => _activeFilter = f)"
                  ShowCount="true" />
    
    <div class="eftable-wrapper">
        <EFTable TItem="ProductDto"
                 Items="_filteredProducts"
                 OnRowClick="@HandleRowClick"
                 ComponentKey="Products">
            <ToolBarContent>
                <MudText Typo="Typo.h6">Products</MudText>
                <MudSpacer />
                <MudButton StartIcon="@Icons.Material.Outlined.Add" OnClick="@AddProduct">
                    Add
                </MudButton>
            </ToolBarContent>
            <HeaderContent>
                <MudTh>Name</MudTh>
                <MudTh>Price</MudTh>
            </HeaderContent>
            <RowTemplate>
                <MudTd>@context.Name</MudTd>
                <MudTd>@context.Price.ToString("C2")</MudTd>
            </RowTemplate>
        </EFTable>
    </div>
</div>
```

### Example 2: With Search and Inline Filters

```razor
<EFTable TItem="CustomerDto"
         Items="_filteredCustomers"
         ComponentKey="CustomerManagement">
    <ToolBarContent>
        <MudText Typo="Typo.h6">Customers</MudText>
        <MudSpacer />
        
        <!-- Search -->
        <MudTextField @bind-Value="_searchTerm"
                      Placeholder="Search customers..."
                      Adornment="Adornment.Start"
                      AdornmentIcon="@Icons.Material.Outlined.Search"
                      Class="ef-input"
                      DebounceInterval="300" />
        
        <!-- Inline Filter (max 2-3) -->
        <MudSelect @bind-Value="_typeFilter"
                   Placeholder="Type"
                   Class="ef-select ml-2">
            <MudSelectItem Value="@("")">All Types</MudSelectItem>
            <MudSelectItem Value="@("retail")">Retail</MudSelectItem>
            <MudSelectItem Value="@("wholesale")">Wholesale</MudSelectItem>
        </MudSelect>
        
        <MudButton StartIcon="@Icons.Material.Outlined.Add" OnClick="@AddCustomer">
            Add
        </MudButton>
    </ToolBarContent>
    <!-- ... -->
</EFTable>
```

### Example 3: With Export

```razor
<EFTable TItem="InvoiceDto"
         Items="_invoices"
         ShowExport="true"
         OnExport="@HandleExport"
         ExportFileName="Invoices"
         ComponentKey="InvoiceManagement">
    <!-- ... -->
</EFTable>

@code {
    private async Task HandleExport(ExportRequest request)
    {
        var columns = request.SelectedColumns.Where(c => c.IsSelected).ToList();
        
        if (request.Format == "Excel")
        {
            var excelData = await ExportService.ExportToExcel(_invoices, columns, "Invoices");
            await FileService.DownloadFile(excelData, "Invoices.xlsx");
        }
        else
        {
            var csvData = ExportService.ExportToCsv(_invoices, columns);
            await FileService.DownloadFile(csvData, "Invoices.csv");
        }
    }
}
```

---

## 8. Best Practices

### 8.1 Naming Conventions

✅ **DO:**
- Use `_allEntities` for unfiltered data
- Use `_filteredEntities` for filtered data (computed property)
- Use `_selectedEntities` for selected items
- Use `_isLoadingEntities` for loading state
- Use `_activeQuickFilter` for current quick filter
- Use `ComponentKey` matching page/entity name

❌ **DON'T:**
- Mix naming patterns (e.g., `items`, `_data`, `entities`)
- Use vague names like `_list`, `_items`

### 8.2 Filtering Strategy

**Recommended approach:**

```csharp
// Computed property for filtered data
private IEnumerable<EntityDto> _filteredEntities => 
    _allEntities.Where(e => FilterItem(e));

// Centralized filter logic
private bool FilterItem(EntityDto item)
{
    // 1. Quick filter (highest priority)
    if (_activeQuickFilter?.Predicate != null && !_activeQuickFilter.Predicate(item))
        return false;
    
    // 2. Search filter
    if (!string.IsNullOrWhiteSpace(_searchTerm))
    {
        if (!MatchesSearch(item, _searchTerm))
            return false;
    }
    
    // 3. Inline filters
    if (!string.IsNullOrEmpty(_typeFilter) && item.Type != _typeFilter)
        return false;
    
    return true;
}
```

### 8.3 QuickFilters Guidelines

**Keep it simple:**
- Maximum 5-6 filters
- Use clear, concise labels
- First filter should always be "All" or "Tutti"
- Add colors for visual distinction
- Use icons sparingly (only when they add value)

**Good Example:**
```csharp
new() { Id = "all", Label = "All", Predicate = _ => true },
new() { Id = "active", Label = "Active", Predicate = e => e.IsActive, Color = Color.Success },
new() { Id = "inactive", Label = "Inactive", Predicate = e => !e.IsActive, Color = Color.Default }
```

**Bad Example:**
```csharp
// Too many filters
new() { Id = "all", Label = "All items in the system", ... },
new() { Id = "active-today", Label = "Active today", ... },
new() { Id = "active-yesterday", Label = "Active yesterday", ... },
// ... 10 more filters
```

### 8.4 Inline Filters

**Guidelines:**
- Maximum 2-3 inline filters in toolbar
- Use for high-value, frequently-used filters
- Prefer dropdowns over text inputs
- Use consistent styling (`.ef-input`, `.ef-select`)

**When to use inline vs QuickFilters:**
- **QuickFilters**: Mutually exclusive options (status, type, category)
- **Inline**: Complementary filters (date range, search, type)

### 8.5 Column Configuration

**Best practices:**
```csharp
private List<EFTableColumnConfiguration> _initialColumns = new()
{
    // Mark key columns as searchable
    new() { PropertyName = "Name", DisplayName = "Nome", IsVisible = true, Order = 0, IsSearchable = true },
    new() { PropertyName = "Code", DisplayName = "Codice", IsVisible = true, Order = 1, IsSearchable = true },
    
    // Technical IDs usually not searchable
    new() { PropertyName = "Id", DisplayName = "ID", IsVisible = false, Order = 2, IsSearchable = false },
    
    // Dates searchable only if formatted as text
    new() { PropertyName = "CreatedAt", DisplayName = "Creato il", IsVisible = true, Order = 3, IsSearchable = false },
};
```

### 8.6 Performance

**Client-side filtering:**
- Suitable for up to ~1000 items
- Use computed properties (`IEnumerable<T>`)
- Avoid unnecessary `ToList()` calls

**Server-side filtering:**
- Use for large datasets (>1000 items)
- Implement `ServerData` parameter
- Return paginated results

### 8.7 Accessibility

**Always include:**
- `DataLabel` attribute in `MudTd` for mobile view
- Meaningful button labels (not just icons)
- ARIA labels for icon-only buttons
- Keyboard navigation support (built-in)

---

## Additional Resources

- **[Standard Pattern Guide](./EFTABLE_STANDARD_PATTERN.md)** - Detailed pattern specifications
- **[Migration Guide](./MIGRATION_GUIDE.md)** - Step-by-step migration instructions
- **[Dashboard to QuickFilters Migration](./DASHBOARD_TO_QUICKFILTERS_MIGRATION.md)** - Specific migration guide
- **[Component Documentation](./components/)** - Individual component docs
  - [EfTable.md](./components/EfTable.md)
  - [QuickFilters.md](./components/QuickFilters.md)
  - [Export.md](./components/Export.md)
- **[Issue #1014 Completion Report](./ISSUE_1014_COMPLETION_REPORT.md)** - Final implementation summary
- **[Archive](./archive/)** - Historical PR documentation

---

## Support

For questions or issues:
1. Check this guide and related documentation
2. Review existing implementations in `/Pages/Management/`
3. Consult the [archived PR summaries](./archive/) for historical context
4. Open a GitHub issue with the `documentation` or `enhancement` label

---

**Document Version**: 2.0  
**Last Review**: February 2026  
**Maintained By**: EventForge Development Team
