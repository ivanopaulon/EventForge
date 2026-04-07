# SuperAdmin Pages Standardization Guide

## Overview

This guide outlines the standardized pattern for all SuperAdmin pages in EventForge. The standardization effort aims to:

- **Consistency**: Ensure all SuperAdmin pages follow the same structure and UI patterns
- **Maintainability**: Make the codebase easier to understand and modify
- **User Experience**: Provide a uniform, intuitive interface for SuperAdmin users
- **Code Reusability**: Leverage shared components to reduce duplication

All SuperAdmin pages MUST follow the same pattern used in Management pages (ProductManagement, WarehouseManagement, etc.). This creates a unified experience across the entire application.

## The Standard Pattern

### Reference Files

These are the **PERFECT EXAMPLES** to follow:

- **Primary Reference**: `EventForge.Client/Pages/Management/Products/ProductManagement.razor`
- **Secondary Reference**: `EventForge.Client/Pages/Management/Warehouse/WarehouseManagement.razor`
- **SuperAdmin Examples** (Already Standardized):
  - `EventForge.Client/Pages/SuperAdmin/UserManagement.razor`
  - `EventForge.Client/Pages/SuperAdmin/TenantManagement.razor`
  - `EventForge.Client/Pages/SuperAdmin/LicenseManagement.razor`
  - `EventForge.Client/Pages/SuperAdmin/Configuration.razor`
  - `EventForge.Client/Pages/SuperAdmin/ClientLogManagement.razor`
  - `EventForge.Client/Pages/SuperAdmin/ChatModeration.razor`

### Mandatory HTML Structure

```razor
@page "/superadmin/[page-name]"
@using Microsoft.AspNetCore.Authorization
@using EventForge.DTOs.[Namespace]
@using EventForge.DTOs.Common
@using EventForge.Client.Shared.Components
@using EventForge.Client.Shared.Components.Dashboard
@attribute [Authorize(Roles = "SuperAdmin")]
@inject IAuthService AuthService
@inject [IService] Service
@inject NavigationManager NavigationManager
@inject ISnackbar Snackbar
@inject IDialogService DialogService
@inject ITranslationService TranslationService

<PageTitle>@TranslationService.GetTranslation("superAdmin.pageTitle", "{0} - EventForge Super Admin", TranslationService.GetTranslation("[key]", "[Title]"))</PageTitle>

<PageLoadingOverlay Visible="_isLoading || _isLoadingEntities"
                     Message="@(_isLoading ? TranslationService.GetTranslation("messages.loadingPage", "Caricamento pagina...") : TranslationService.GetTranslation("common.loading", "Caricamento..."))" />

@if (!_isLoading)
{
    @if (!_isAuthorized)
    {
        <MudContainer MaxWidth="MaxWidth.Medium" Class="mt-8">
            <MudPaper Class="ef-tile">
                <MudGrid Justify="Justify.Center">
                    <MudItem xs="12">
                        <div class="d-flex flex-column align-center">
                            <MudIcon Icon="Icons.Material.Filled.Block" Color="Color.Error" Size="Size.Medium" Style="font-size: 48px;" />
                            <MudText Typo="Typo.h4" Align="Align.Center" Class="mb-4">@TranslationService.GetTranslation("error.accessDenied", "Accesso Negato")</MudText>
                            <MudText Typo="Typo.body1" Align="Align.Center" Class="mb-4">
                                @TranslationService.GetTranslation("superAdmin.superAdminRoleRequired", "Non hai i permessi per accedere a questa pagina. È richiesto il ruolo Super Amministratore.")
                            </MudText>
                            <MudButton Variant="Variant.Filled" 
                                       Color="Color.Primary" 
                                       StartIcon="Icons.Material.Filled.ArrowBack"
                                       OnClick="@(() => NavigationManager.NavigateTo("/"))">
                                @TranslationService.GetTranslation("superAdmin.returnToHome", "Torna alla Home")
                            </MudButton>
                        </div>
                    </MudItem>
                </MudGrid>
            </MudPaper>
        </MudContainer>
    }
    else
    {
        <div class="[entity]-page-root">
            <div class="[entity]-top">
                <ManagementDashboard TItem="[Dto]"
                                     Items="_filteredEntities"
                                     Metrics="_dashboardMetrics"
                                     EntityType="[Entity]"
                                     AllowConfiguration="true"
                                     UseServerSide="false" />
            </div>

            <div class="eftable-wrapper">
                <EFTable @ref="_efTable"
                         TItem="[Dto]"
                         Items="_filteredEntities"
                         MultiSelection="true"
                         SelectedItems="_selectedEntities"
                         SelectedItemsChanged="_selectedItemsChangedCallback"
                         IsLoading="_isLoadingEntities"
                         ComponentKey="[Entity]Management"
                         InitialColumnConfigurations="_initialColumns"
                         AllowDragDropGrouping="true">
                    <ToolBarContent>
                        <MudText Typo="Typo.h5">
                            @TranslationService.GetTranslation("[key]", "[Title]")
                        </MudText>
                        <MudSpacer />
                        <MudTextField @bind-Value="_searchTerm"
                                      @bind-Value:after="OnSearchChanged"
                                      Label="@TranslationService.GetTranslation("[searchKey]", "Cerca")"
                                      Placeholder="@TranslationService.GetTranslation("[placeholderKey]", "Inserisci testo...")"
                                      Variant="Variant.Outlined"
                                      Adornment="Adornment.End"
                                      AdornmentIcon="@Icons.Material.Outlined.Search"
                                      Clearable="true"
                                      Class="ef-input" />
                        <!-- MAX 2-3 inline filters here -->
                        <ManagementTableToolbar ShowRefresh="true"
                                                ShowCreate="true"
                                                ShowDelete="true"
                                                ShowSelectionBadge="true"
                                                SelectedCount="@_selectedEntities.Count"
                                                IsDisabled="_isLoadingEntities"
                                                OnRefresh="@LoadEntitiesAsync"
                                                OnCreate="@CreateEntity"
                                                OnDelete="@DeleteSelectedEntities">
                            <!-- Optional custom actions -->
                            <AdditionalActions>
                                <!-- Custom toolbar buttons here -->
                            </AdditionalActions>
                        </ManagementTableToolbar>
                    </ToolBarContent>
                    <HeaderContent Context="columnConfigurations">
                        @foreach (var column in columnConfigurations.Where(c => c.IsVisible).OrderBy(c => c.Order))
                        {
                            @if (column.PropertyName == "[ColumnName]")
                            {
                                <EFTableColumnHeader TItem="[Dto]" PropertyName="[ColumnName]" OnDragStartCallback="@_efTable.HandleColumnDragStart">
                                    <MudTableSortLabel SortBy="@(new Func<[Dto], object>(x => x.[Property]))">@TranslationService.GetTranslation("[key]", "[Label]")</MudTableSortLabel>
                                </EFTableColumnHeader>
                            }
                            <!-- More columns -->
                        }
                        <MudTh Class="text-center" Style="min-width:120px;">@TranslationService.GetTranslation("common.actions", "Azioni")</MudTh>
                    </HeaderContent>

                    <RowTemplate Context="item">
                        @{
                            var visibleColumns = _efTable?.ColumnConfigurations?.Where(c => c.IsVisible).OrderBy(c => c.Order).ToList() ?? _initialColumns.Where(c => c.IsVisible).OrderBy(c => c.Order).ToList();
                        }
                        @foreach (var column in visibleColumns)
                        {
                            <!-- Column rendering logic -->
                        }
                        <MudTd DataLabel="@TranslationService.GetTranslation("common.actions", "Azioni")" Class="text-center">
                            <ActionButtonGroup EntityName="@item.Name"
                                               ItemDisplayName="@item.Name"
                                               ShowView="false"
                                               ShowEdit="true"
                                               ShowAuditLog="true"
                                               ShowDelete="true"
                                               OnEdit="@(() => EditEntity(item.Id))"
                                               OnAuditLog="@(() => ViewAuditLog(item))"
                                               OnDelete="@(() => DeleteEntity(item))" />
                        </MudTd>
                    </RowTemplate>

                    <NoRecordsContent>
                        <div class="text-center pa-2 pa-sm-3 pa-md-4">
                            <MudIcon Icon="@Icons.Material.Outlined.[Icon]" Size="Size.Medium" Class="mb-4 mud-text-secondary" />
                            <MudText Typo="Typo.h6" Class="mb-2">
                                @(_entities.Any() ?
                                    TranslationService.GetTranslation("[noMatchKey]", "Nessun elemento corrisponde ai filtri") :
                                    TranslationService.GetTranslation("[noDataKey]", "Nessun elemento trovato"))
                            </MudText>
                            @if (_entities.Any())
                            {
                                <MudButton Variant="Variant.Text"
                                           Color="Color.Primary"
                                           StartIcon="@Icons.Material.Outlined.Clear"
                                           OnClick="@ClearFilters">
                                    @TranslationService.GetTranslation("action.clearFilters", "Cancella filtri")
                                </MudButton>
                            }
                        </div>
                    </NoRecordsContent>
                </EFTable>
            </div>
        </div>
    }
}
```

### Mandatory Code-Behind Structure

```csharp
@code {
    // UI State Management
    private bool _isLoading = true;
    private bool _isAuthorized = false;
    private bool _isLoadingEntities = false;

    // Filter and search state
    private string _searchTerm = string.Empty;
    private string _selectedFilter1 = string.Empty; // Optional, MAX 2-3 filters
    private CancellationTokenSource? _searchDebounceCts;

    // Data collections
    private List<EntityDto> _entities = new();
    private HashSet<EntityDto> _selectedEntities = new();

    // EFTable reference
    private EFTable<EntityDto> _efTable = null!;

    // Column configuration for EFTable
    private List<EFTableColumnConfiguration> _initialColumns = new()
    {
        new() { PropertyName = "Column1", DisplayName = "Label1", IsVisible = true, Order = 0 },
        new() { PropertyName = "Column2", DisplayName = "Label2", IsVisible = true, Order = 1 },
        // ... more columns
    };

    // Dashboard configuration - EXACTLY 4 metrics
    private List<DashboardMetric<EntityDto>> _dashboardMetrics = new()
    {
        new()
        {
            Title = "Metric 1",
            Type = MetricType.Count, // or Sum, Average, Min, Max
            Icon = Icons.Material.Outlined.Icon1,
            Color = "primary", // or "success", "warning", "error", "info"
            Description = "Description of metric",
            Format = "N0" // or "C2", "P1", etc.
        },
        new()
        {
            Title = "Metric 2",
            Type = MetricType.Count,
            Filter = e => e.Property == Value, // Optional filter
            Icon = Icons.Material.Outlined.Icon2,
            Color = "success",
            Description = "Description",
            Format = "N0"
        },
        new()
        {
            Title = "Metric 3",
            Type = MetricType.Count,
            Filter = e => e.Date >= DateTime.Now.AddDays(-30),
            Icon = Icons.Material.Outlined.Icon3,
            Color = "warning",
            Description = "Description",
            Format = "N0"
        },
        new()
        {
            Title = "Metric 4",
            Type = MetricType.Count,
            Filter = e => e.Status == Status.Value,
            Icon = Icons.Material.Outlined.Icon4,
            Color = "info",
            Description = "Description",
            Format = "N0"
        }
    };

    private EventCallback<HashSet<EntityDto>> _selectedItemsChangedCallback => 
        EventCallback.Factory.Create<HashSet<EntityDto>>(this, OnSelectedItemsChanged);

    /// <summary>
    /// Computed property for filtered entities based on search criteria.
    /// </summary>
    private IEnumerable<EntityDto> _filteredEntities => 
        _entities.Where(e => 
            // Search filter
            (string.IsNullOrEmpty(_searchTerm) || 
             e.Property1.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) ||
             e.Property2.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase)) &&
            // Additional filters (optional)
            (string.IsNullOrEmpty(_selectedFilter1) || e.FilterProperty == _selectedFilter1)
        );

    /// <summary>
    /// Component initialization.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        try
        {
            _isAuthorized = await AuthService.IsSuperAdminAsync();
            
            if (_isAuthorized)
            {
                await LoadEntitiesAsync();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add(TranslationService.GetTranslation("error", "Errore: {0}", ex.Message), Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Load entities from service.
    /// </summary>
    private async Task LoadEntitiesAsync()
    {
        try
        {
            _isLoadingEntities = true;
            _entities = await Service.GetEntitiesAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add(TranslationService.GetTranslation("error", "Errore: {0}", ex.Message), Severity.Error);
        }
        finally
        {
            _isLoadingEntities = false;
        }
    }

    /// <summary>
    /// Clear all active filters.
    /// </summary>
    private void ClearFilters()
    {
        _searchTerm = string.Empty;
        _selectedFilter1 = string.Empty;
        StateHasChanged();
    }

    /// <summary>
    /// Handle search term changes with debouncing.
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

    // ... CRUD methods (Create, Edit, Delete, etc.)
}
```

## Components to Use

### ✅ Required Components

1. **PageLoadingOverlay**
   - Shows loading state during page initialization
   - Displays custom message
   - Always place at the top of the page

2. **ManagementDashboard**
   - Displays metrics/statistics
   - Exactly 4 metrics per page
   - Client-side or server-side calculation
   - Configurable by users

3. **EFTable**
   - Main data grid component
   - Supports sorting, filtering, grouping
   - Column customization
   - Drag-drop reordering
   - Multi-selection support

4. **ManagementTableToolbar**
   - Standard toolbar with refresh, create, delete buttons
   - Selection badge
   - Custom actions support

5. **ActionButtonGroup**
   - Consistent action buttons for each row
   - View, Edit, AuditLog, Delete, Toggle Status

6. **EFTableColumnHeader**
   - Sortable column headers
   - Drag-drop support

### ❌ Deprecated Components

**DO NOT USE** these components in new or refactored pages:

1. **SuperAdminPageLayout** - Replaced by standard structure
2. **SuperAdminCollapsibleSection** - Replaced by ManagementDashboard
3. **MudTable** - Replaced by EFTable
4. **MudDataGrid** - Replaced by EFTable

## Dashboard Metrics Rules

### Configuration

- **Exactly 4 metrics** per page
- Each metric MUST have:
  - `Title`: Short, descriptive name
  - `Type`: Count, Sum, Average, Min, or Max
  - `Icon`: Material icon
  - `Color`: "primary", "success", "warning", "error", or "info"
  - `Description`: Tooltip text explaining the metric
  - `Format`: Number format string (e.g., "N0", "C2", "P1")

### Metric Types

```csharp
MetricType.Count    // Count of items matching filter
MetricType.Sum      // Sum of a numeric property
MetricType.Average  // Average of a numeric property
MetricType.Min      // Minimum value
MetricType.Max      // Maximum value
```

### Color Guidelines

- **primary**: Total/overall counts
- **success**: Positive metrics (active, completed, approved)
- **warning**: Attention items (pending, warnings)
- **error**: Problem metrics (errors, blocked, failed)
- **info**: Informational metrics (recent, with features)

### Example Metrics

```csharp
// Total count - primary
new()
{
    Title = "Total Items",
    Type = MetricType.Count,
    Icon = Icons.Material.Outlined.Dashboard,
    Color = "primary",
    Description = "Total number of items",
    Format = "N0"
}

// With filter - success
new()
{
    Title = "Active Items",
    Type = MetricType.Count,
    Filter = i => i.Status == Status.Active,
    Icon = Icons.Material.Outlined.CheckCircle,
    Color = "success",
    Description = "Items with active status",
    Format = "N0"
}

// Date-based filter - warning
new()
{
    Title = "Recent Items",
    Type = MetricType.Count,
    Filter = i => i.CreatedAt >= DateTime.Now.AddDays(-30),
    Icon = Icons.Material.Outlined.NewReleases,
    Color = "warning",
    Description = "Items created in last 30 days",
    Format = "N0"
}
```

## EFTable Column Rules

### Column Configuration

```csharp
private List<EFTableColumnConfiguration> _initialColumns = new()
{
    new() 
    { 
        PropertyName = "PropertyName",  // Must match DTO property
        DisplayName = "Display Label",  // Translated label
        IsVisible = true,               // Show by default
        Order = 0                       // Display order
    },
    // ... more columns
};
```

### Display Patterns

#### Text Column
```razor
<MudTd DataLabel="@TranslationService.GetTranslation("field.name", "Nome")">
    <MudText Typo="Typo.body2">@item.Name</MudText>
</MudTd>
```

#### Truncated Text
```razor
<MudTd DataLabel="@TranslationService.GetTranslation("field.description", "Descrizione")">
    <MudText Typo="Typo.body2" Class="text-truncate" Style="max-width:400px;">
        @(string.IsNullOrEmpty(item.Description) ? "-" : item.Description)
    </MudText>
</MudTd>
```

#### Chip Column (Status/Level)
```razor
<MudTd DataLabel="@TranslationService.GetTranslation("field.status", "Stato")">
    <MudChip T="string" Size="Size.Small" Color="@GetStatusColor(item.Status)">
        @GetStatusText(item.Status)
    </MudChip>
</MudTd>
```

#### Code/ID Chip
```razor
<MudTd DataLabel="@TranslationService.GetTranslation("field.code", "Codice")">
    <MudChip T="string" Size="Size.Small" Color="Color.Primary" Variant="Variant.Outlined">
        @item.Code
    </MudChip>
</MudTd>
```

#### Avatar Column
```razor
<MudTd DataLabel="@TranslationService.GetTranslation("field.name", "Nome")">
    <div class="d-flex align-center">
        <MudAvatar Color="Color.Primary" Size="Size.Small" Class="mr-2">
            <MudIcon Icon="@Icons.Material.Outlined.Person" />
        </MudAvatar>
        <div>
            <MudText Typo="Typo.body2">@item.Name</MudText>
            <MudText Typo="Typo.caption" Class="mud-text-secondary">
                ID: @(item.Id.ToString()[..Math.Min(8, item.Id.ToString().Length)])...
            </MudText>
        </div>
    </div>
</MudTd>
```

#### Date Column
```razor
<MudTd DataLabel="@TranslationService.GetTranslation("field.createdAt", "Creato il")">
    <MudText Typo="Typo.body2">@item.CreatedAt.ToString("dd/MM/yyyy HH:mm")</MudText>
</MudTd>
```

#### Currency Column
```razor
<MudTd DataLabel="@TranslationService.GetTranslation("field.price", "Prezzo")" Style="text-align:right;">
    @if (item.Price.HasValue)
    {
        <MudText Typo="Typo.body2" Style="font-weight:600;">@item.Price.Value.ToString("C2")</MudText>
    }
    else
    {
        <MudText Typo="Typo.body2">-</MudText>
    }
</MudTd>
```

## Filtering Rules

### Guidelines

- **ALWAYS** include search field as first filter
- **MAXIMUM** 2-3 inline filters (search + 2 dropdowns)
- More complex filters should be in a separate filter panel/dialog
- All filters are debounced (300ms)
- Filters are computed client-side via `_filteredEntities` property

### Search Implementation

```csharp
private string _searchTerm = string.Empty;
private CancellationTokenSource? _searchDebounceCts;

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
```

### Filter Property

```csharp
private IEnumerable<EntityDto> _filteredEntities => 
    _entities.Where(e => 
        // Search filter - check multiple properties
        (string.IsNullOrEmpty(_searchTerm) || 
         e.Name.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) ||
         e.Description.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase)) &&
        // Additional filters
        (string.IsNullOrEmpty(_selectedStatus) || e.Status == _selectedStatus) &&
        (string.IsNullOrEmpty(_selectedCategory) || e.Category == _selectedCategory)
    ).OrderByDescending(e => e.CreatedAt); // Default sorting
```

## ManagementTableToolbar Configuration

### Standard Configuration

```razor
<ManagementTableToolbar ShowRefresh="true"
                        ShowCreate="true"
                        ShowDelete="true"
                        ShowSelectionBadge="true"
                        SelectedCount="@_selectedEntities.Count"
                        IsDisabled="_isLoadingEntities"
                        OnRefresh="@LoadEntitiesAsync"
                        OnCreate="@CreateEntity"
                        OnDelete="@DeleteSelectedEntities" />
```

### With Custom Actions

```razor
<ManagementTableToolbar ShowRefresh="true"
                        ShowCreate="false"
                        ShowDelete="false"
                        ShowSelectionBadge="false"
                        IsDisabled="_isLoading"
                        OnRefresh="@RefreshAsync">
    <AdditionalActions>
        <MudTooltip Text="@TranslationService.GetTranslation("action.export", "Esporta")">
            <MudIconButton Icon="@Icons.Material.Filled.Download" 
                           OnClick="@ExportAsync"
                           Variant="Variant.Outlined"
                           Size="Size.Small"
                           Color="Color.Info" />
        </MudTooltip>
        <MudTooltip Text="@TranslationService.GetTranslation("action.import", "Importa")">
            <MudIconButton Icon="@Icons.Material.Filled.Upload" 
                           OnClick="@ImportAsync"
                           Variant="Variant.Outlined"
                           Size="Size.Small"
                           Color="Color.Secondary" />
        </MudTooltip>
    </AdditionalActions>
</ManagementTableToolbar>
```

### ClientLogManagement Example (Custom Actions)

```razor
<ManagementTableToolbar ShowRefresh="true"
                        ShowCreate="false"
                        ShowDelete="false"
                        ShowSelectionBadge="false"
                        IsDisabled="_isLoadingLogs"
                        OnRefresh="@RefreshLogsAsync">
    <AdditionalActions>
        <MudTooltip Text="Flush to Server">
            <MudIconButton Icon="@Icons.Material.Filled.Send" 
                           OnClick="@FlushLogsToServerAsync"
                           Disabled="_isFlushingLogs"
                           Variant="Variant.Outlined"
                           Size="Size.Small"
                           Color="Color.Secondary" />
        </MudTooltip>
        <MudTooltip Text="Export JSON">
            <MudIconButton Icon="@Icons.Material.Filled.Download" 
                           OnClick="@ExportLogsAsync"
                           Variant="Variant.Outlined"
                           Size="Size.Small"
                           Color="Color.Info" />
        </MudTooltip>
        <MudTooltip Text="Clear Local Logs">
            <MudIconButton Icon="@Icons.Material.Outlined.Clear" 
                           OnClick="@ClearLocalLogsAsync"
                           Variant="Variant.Outlined"
                           Size="Size.Small"
                           Color="Color.Warning" />
        </MudTooltip>
        <MudTooltip Text="Test Log">
            <MudIconButton Icon="@Icons.Material.Filled.BugReport" 
                           OnClick="@TestLogAsync"
                           Variant="Variant.Outlined"
                           Size="Size.Small"
                           Color="Color.Tertiary" />
        </MudTooltip>
    </AdditionalActions>
</ManagementTableToolbar>
```

## Migration Checklist

Use this checklist when migrating an existing page to the standard pattern:

### Phase 1: Remove Old Components
- [ ] Remove `<SuperAdminPageLayout>` wrapper
- [ ] Remove all `<SuperAdminCollapsibleSection>` components
- [ ] Remove `<MudTable>` or `<MudDataGrid>` components
- [ ] Remove mock charts or non-functional UI elements
- [ ] Remove inline settings sections (move to separate pages)

### Phase 2: Add New Structure
- [ ] Add `<PageLoadingOverlay>` at the top
- [ ] Add `@if (!_isLoading)` wrapper
- [ ] Add `@if (!_isAuthorized)` access denied UI
- [ ] Add `<div class="[entity]-page-root">` container
- [ ] Add `<div class="[entity]-top">` for dashboard
- [ ] Add `<div class="eftable-wrapper">` for table

### Phase 3: Add Dashboard
- [ ] Add `<ManagementDashboard>` component
- [ ] Define `_dashboardMetrics` with exactly 4 metrics
- [ ] Configure metrics with proper colors and icons
- [ ] Add descriptions and formats

### Phase 4: Add Table
- [ ] Replace table with `<EFTable>`
- [ ] Define `_initialColumns` configuration
- [ ] Add `ToolBarContent` with search and filters
- [ ] Add `<ManagementTableToolbar>` with appropriate settings
- [ ] Add `HeaderContent` with `<EFTableColumnHeader>` for each column
- [ ] Add `RowTemplate` with proper cell rendering
- [ ] Add `NoRecordsContent` with appropriate empty state

### Phase 5: Move Filters
- [ ] Move search field to toolbar (inline)
- [ ] Keep MAX 2-3 inline filters
- [ ] Remove complex filter sections
- [ ] Implement debounced search
- [ ] Create `_filteredEntities` computed property

### Phase 6: Update Code-Behind
- [ ] Add `_isLoadingEntities` state variable
- [ ] Add `_searchTerm` and filter variables
- [ ] Add `_entities` data collection
- [ ] Add `_efTable` reference
- [ ] Update `OnInitializedAsync` to check authorization
- [ ] Implement `OnSearchChanged` with debouncing
- [ ] Implement `ClearFilters` method
- [ ] Add helper methods (color getters, text formatters)

### Phase 7: Test
- [ ] Page loads without errors
- [ ] Dashboard displays correct metrics
- [ ] Table displays data correctly
- [ ] Search works with debouncing
- [ ] Filters work correctly
- [ ] Sorting works on all columns
- [ ] Actions buttons work
- [ ] Column reordering works (drag-drop)
- [ ] Selection works (if enabled)
- [ ] Empty state displays correctly
- [ ] Authorization check works

## Testing Requirements

### UI/UX Tests
- [ ] Page loads within 2 seconds
- [ ] Loading overlay displays correctly
- [ ] Dashboard metrics are readable and accurate
- [ ] Table renders properly on mobile, tablet, desktop
- [ ] Search debouncing works (no lag)
- [ ] Filters apply immediately after selection
- [ ] Empty state is clear and helpful
- [ ] Action buttons are accessible and have tooltips
- [ ] Column headers are sortable with visual feedback
- [ ] Selected rows are visually distinct

### Functional Tests
- [ ] Authorization check prevents unauthorized access
- [ ] Data loads from service correctly
- [ ] Search filters data client-side
- [ ] Dropdown filters work correctly
- [ ] CRUD operations work (Create, Read, Update, Delete)
- [ ] Multi-selection works (if enabled)
- [ ] Bulk delete works (if enabled)
- [ ] Column customization persists
- [ ] Dashboard configuration persists (if enabled)

## Common Patterns

### Helper Method: Get Color by Status

```csharp
private Color GetStatusColor(string status) => status?.ToLower() switch
{
    "active" => Color.Success,
    "inactive" => Color.Default,
    "pending" => Color.Warning,
    "blocked" => Color.Error,
    "deleted" => Color.Error,
    _ => Color.Default
};
```

### Helper Method: Get Status Text

```csharp
private string GetStatusText(string status) => status?.ToLower() switch
{
    "active" => TranslationService.GetTranslation("status.active", "Attivo"),
    "inactive" => TranslationService.GetTranslation("status.inactive", "Inattivo"),
    "pending" => TranslationService.GetTranslation("status.pending", "In Attesa"),
    "blocked" => TranslationService.GetTranslation("status.blocked", "Bloccato"),
    _ => status
};
```

### Helper Method: Truncate Text

```csharp
private string TruncateText(string text, int maxLength = 100)
{
    if (string.IsNullOrEmpty(text)) return "-";
    return text.Length <= maxLength ? text : text[..maxLength] + "...";
}
```

### Pattern: Confirmation Dialog

```csharp
private async Task DeleteEntity(EntityDto entity)
{
    var confirmTitle = TranslationService.GetTranslation("common.confirm", "Conferma");
    var confirmMessage = TranslationService.GetTranslation("entity.confirmDelete", 
        "Sei sicuro di voler eliminare '{0}'?", entity.Name);

    var confirm = await DialogService.ShowMessageBox(
        confirmTitle,
        confirmMessage,
        yesText: TranslationService.GetTranslation("common.delete", "Elimina"),
        cancelText: TranslationService.GetTranslation("common.cancel", "Annulla"));

    if (confirm == true)
    {
        try
        {
            await Service.DeleteAsync(entity.Id);
            _entities.Remove(entity);
            Snackbar.Add(TranslationService.GetTranslation("entity.deleted", "Elemento eliminato con successo!"), Severity.Success);
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Snackbar.Add(TranslationService.GetTranslation("entity.deleteError", "Errore nell'eliminazione: {0}", ex.Message), Severity.Error);
        }
    }
}
```

### Pattern: Bulk Delete

```csharp
private async Task DeleteSelectedEntities()
{
    if (_selectedEntities.Count == 0)
        return;

    var confirmTitle = TranslationService.GetTranslation("common.confirm", "Conferma");
    var confirmMessage = TranslationService.GetTranslationFormatted("entity.confirmDeleteMultiple", 
        "Sei sicuro di voler eliminare {0} elementi?", _selectedEntities.Count);

    var confirm = await DialogService.ShowMessageBox(
        confirmTitle,
        confirmMessage,
        yesText: TranslationService.GetTranslation("common.delete", "Elimina"),
        cancelText: TranslationService.GetTranslation("common.cancel", "Annulla"));

    if (confirm == true)
    {
        try
        {
            var deletedCount = 0;
            var failedCount = 0;
            
            foreach (var entity in _selectedEntities.ToList())
            {
                try
                {
                    await Service.DeleteAsync(entity.Id);
                    _entities.RemoveAll(e => e.Id == entity.Id);
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    failedCount++;
                }
            }
            
            _selectedEntities.Clear();
            
            if (failedCount == 0)
            {
                Snackbar.Add(TranslationService.GetTranslationFormatted("entity.deletedMultiple", 
                    "{0} elementi eliminati con successo!", deletedCount), Severity.Success);
            }
            else
            {
                Snackbar.Add(TranslationService.GetTranslationFormatted("entity.deletedMultiplePartial", 
                    "{0} elementi eliminati, {1} falliti", deletedCount, failedCount), Severity.Warning);
            }
            
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Snackbar.Add(TranslationService.GetTranslationFormatted("entity.deleteError", 
                "Errore nell'eliminazione: {0}", ex.Message), Severity.Error);
        }
    }
}
```

## Examples

All standardized SuperAdmin pages follow this pattern:

### Fully Standardized Pages
1. **UserManagement.razor** - User management with tenant filtering
2. **TenantManagement.razor** - Tenant management with status tracking
3. **LicenseManagement.razor** - License management with expiration tracking
4. **Configuration.razor** - System configuration management
5. **ClientLogManagement.razor** - Client-side log management with custom actions
6. **ChatModeration.razor** - Chat moderation with mock data warning

### Key Differences

Despite these entity-specific features, all pages follow the exact same fundamental pattern: PageLoadingOverlay → ManagementDashboard → EFTable with standardized toolbar and columns. The differences below are purely in the specific features and data being displayed:

- **ClientLogManagement**: Uses custom toolbar actions for log-specific operations (Flush, Export, Clear, Test)
- **ChatModeration**: Displays warning alert about mock data, pending message actions conditional on status
- **UserManagement**: Tenant dropdown filter, role chips, status toggle
- **LicenseManagement**: Expiration date tracking, license type badges
- **TenantManagement**: Subscription status, user count metrics

All pages share the same core structure while accommodating entity-specific features.

## Conclusion

Following this standardization guide ensures:
- **Consistency**: All pages look and behave the same way
- **Maintainability**: Easy to understand and modify
- **Scalability**: New pages can be created quickly
- **User Experience**: Predictable, intuitive interface

When in doubt, refer to **ProductManagement.razor** as the gold standard.

For questions or clarifications, consult this guide or the reference implementations.
