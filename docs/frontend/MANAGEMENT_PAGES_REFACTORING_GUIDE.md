# Management Pages Refactoring Guide

## Overview
This guide documents the standardization of all management pages to use the VatRateManagement.razor template structure with EFTable and ManagementDashboard components.

## 📖 Comprehensive Documentation

For the complete, detailed EFTable standardization pattern including:
- Full HTML structure templates
- Standard toolbar 4-section layout
- Code-behind patterns and naming conventions
- CSS classes and responsive design
- Inline filters guidelines (max 2-3)
- Complete working examples
- Migration checklist

**See:** [`docs/EFTABLE_STANDARD_PATTERN.md`](/docs/EFTABLE_STANDARD_PATTERN.md)

This guide below provides a quick reference for the pattern.

## Completed Pages (3/11)
- ✅ VatNatureManagement.razor
- ✅ BrandManagement.razor
- ✅ UnitOfMeasureManagement.razor

## Remaining Pages (8/11)

### Business Management
1. **CustomerManagement.razor** (`/home/runner/work/EventForge/EventForge/EventForge.Client/Pages/Management/Business/CustomerManagement.razor`)
   - DTO: `BusinessPartyDto`
   - Icon: `Icons.Material.Outlined.People`
   - Metrics: Total Customers, Active, With VAT Number, Recent (last 30 days)

2. **SupplierManagement.razor** (`/home/runner/work/EventForge/EventForge/EventForge.Client/Pages/Management/Business/SupplierManagement.razor`)
   - DTO: `BusinessPartyDto`
   - Icon: `Icons.Material.Outlined.Business`
   - Metrics: Total Suppliers, Active, With VAT Number, Recent (last 30 days)

### Products Management
3. **ClassificationNodeManagement.razor** (`/home/runner/work/EventForge/EventForge/EventForge.Client/Pages/Management/Products/ClassificationNodeManagement.razor`)
   - DTO: `ClassificationNodeDto`
   - Icon: `Icons.Material.Outlined.AccountTree`
   - Metrics: Total Nodes, Root Nodes, Leaf Nodes, Recent (last 30 days)

5. **ProductManagement.razor** (`/home/runner/work/EventForge/EventForge/EventForge.Client/Pages/Management/Products/ProductManagement.razor`)
   - DTO: `ProductDto`
   - Icon: `Icons.Material.Outlined.Inventory`
   - Note: Already uses EFTable, only add ManagementDashboard
   - Metrics: Total Products, Active, With Images, Recent (last 30 days)

### Documents Management
5. **DocumentTypeManagement.razor** (`/home/runner/work/EventForge/EventForge/EventForge.Client/Pages/Management/Documents/DocumentTypeManagement.razor`)
   - DTO: `DocumentTypeDto`
   - Icon: `Icons.Material.Outlined.Category`
   - Metrics: Total Types, Fiscal Documents, Stock Increase Types, Recent (last 30 days)

6. **DocumentCounterManagement.razor** (`/home/runner/work/EventForge/EventForge/EventForge.Client/Pages/Management/Documents/DocumentCounterManagement.razor`)
   - DTO: `DocumentCounterDto` (needs verification)
   - Icon: `Icons.Material.Outlined.Numbers`
   - Metrics: Total Counters, Active Counters, Current Year, Recent (last 30 days)

### Warehouse Management
7. **WarehouseManagement.razor** (`/home/runner/work/EventForge/EventForge/EventForge.Client/Pages/Management/Warehouse/WarehouseManagement.razor`)
   - DTO: `StorageFacilityDto`
   - Icon: `Icons.Material.Outlined.Warehouse`
   - Metrics: Total Warehouses, Fiscal Warehouses, Refrigerated, Recent (last 30 days)

8. **LotManagement.razor** (`/home/runner/work/EventForge/EventForge/EventForge.Client/Pages/Management/Warehouse/LotManagement.razor`)
   - DTO: `LotDto` (needs verification)
   - Icon: `Icons.Material.Outlined.QrCode`
   - Metrics: Total Lots, Active Lots, Expiring Soon, Recent (last 30 days)

## Template Structure Pattern

### 1. Page Template (HTML Section)

```razor
@page "/path/to/page"
@using Microsoft.AspNetCore.Authorization
@using EventForge.DTOs.[Namespace]
@using EventForge.DTOs.Common
@using EventForge.Client.Shared.Components
@using EventForge.Client.Shared.Components.Dashboard
@attribute [Authorize]
@inject IAuthService AuthService
@inject I[Entity]Service [Entity]Service
@inject NavigationManager NavigationManager
@inject IAuthenticationDialogService AuthenticationDialogService
@inject ISnackbar Snackbar
@inject IDialogService DialogService
@inject ITranslationService TranslationService
@inject ILogger<[Entity]Management> Logger

<PageLoadingOverlay Visible="_isLoading || _isLoading[Entities]"
                     Message="@(_isLoading ? TranslationService.GetTranslation("messages.loadingPage", "Caricamento pagina...") : TranslationService.GetTranslation("common.loading", "Caricamento..."))" />

@if (!_isLoading)
{
    <div class="[entity]-page-root">
        <div class="[entity]-top">
            <ManagementDashboard TItem="[EntityDto]"
                                 Items="_filtered[Entities]"
                                 Metrics="_dashboardMetrics"
                                 EntityType="[Entity]"
                                 AllowConfiguration="true"
                                 UseServerSide="false" />
        </div>

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
            <ToolBarContent>
                <!-- SECTION 1: Title -->
                <MudText Typo="Typo.h5">
                    @TranslationService.GetTranslation("[entity].management", "Gestione [Entities]")
                </MudText>
                <MudSpacer />
                
                <!-- SECTION 2: Search (if enabled) -->
                <MudTextField @bind-Value="_searchTerm"
                              @bind-Value:after="OnSearchChanged"
                              Label="@TranslationService.GetTranslation("[entity].search", "Cerca [entities]")"
                              Placeholder="@TranslationService.GetTranslation("[entity].searchPlaceholder", "Inserisci...")"
                              Variant="Variant.Outlined"
                              Adornment="Adornment.End"
                              AdornmentIcon="@Icons.Material.Outlined.Search"
                              Clearable="true"
                              Class="ef-input" />
                
                <!-- SECTION 3: Inline Filters (MAX 2-3, OPTIONAL) -->
                <!-- Example: MudSwitch for boolean toggles -->
                <!-- <MudSwitch @bind-Value="_showOnlyActive"
                           @bind-Value:after="OnFilterChanged"
                           Label="@TranslationService.GetTranslation("[entity].onlyActive", "Solo attivi")"
                           Color="Color.Primary"
                           Class="ml-2" /> -->
                
                <!-- Example: MudSelect for dropdown filters -->
                <!-- <MudSelect @bind-Value="_statusFilter"
                           @bind-Value:after="OnFilterChanged"
                           Label="@TranslationService.GetTranslation("[entity].status", "Stato")"
                           Variant="Variant.Outlined"
                           Class="ml-2"
                           Style="min-width: 150px;">
                    <MudSelectItem Value="@("all")">Tutti</MudSelectItem>
                    <MudSelectItem Value="@("active")">Attivi</MudSelectItem>
                </MudSelect> -->
                
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
            <HeaderContent Context="columnConfigurations">
                @foreach (var column in columnConfigurations.Where(c => c.IsVisible).OrderBy(c => c.Order))
                {
                    @if (column.PropertyName == "[PropertyName]")
                    {
                        <EFTableColumnHeader TItem="[EntityDto]" PropertyName="[PropertyName]" OnDragStartCallback="@_efTable.HandleColumnDragStart">
                            <MudTableSortLabel SortBy="@(new Func<[EntityDto], object>(x => x.[PropertyName]))">@TranslationService.GetTranslation("field.[propertyName]", "[Label]")</MudTableSortLabel>
                        </EFTableColumnHeader>
                    }
                    // Repeat for each column
                }
                <MudTh Class="text-center" Style="min-width:120px;">@TranslationService.GetTranslation("common.actions", "Azioni")</MudTh>
            </HeaderContent>

            <RowTemplate Context="item">
                @{
                    var visibleColumns = _efTable?.ColumnConfigurations?.Where(c => c.IsVisible).OrderBy(c => c.Order).ToList() ?? _initialColumns.Where(c => c.IsVisible).OrderBy(c => c.Order).ToList();
                }
                @foreach (var column in visibleColumns)
                {
                    @if (column.PropertyName == "[PropertyName]")
                    {
                        <MudTd DataLabel="@TranslationService.GetTranslation("field.[propertyName]", "[Label]")">
                            <!-- Column content -->
                        </MudTd>
                    }
                    // Repeat for each column
                }
                <MudTd DataLabel="@TranslationService.GetTranslation("common.actions", "Azioni")" Class="text-center">
                    <ActionButtonGroup EntityName="@item.Name"
                                       ItemDisplayName="@item.Name"
                                       ShowView="false"
                                       ShowEdit="true"
                                       ShowAuditLog="true"
                                       ShowToggleStatus="false"
                                       ShowDelete="true"
                                       OnEdit="@(() => Edit[Entity](item.Id))"
                                       OnAuditLog="@(() => View[Entity]AuditLog(item))"
                                       OnDelete="@(() => Delete[Entity](item))" />
                </MudTd>
            </RowTemplate>

            <NoRecordsContent>
                <div class="text-center pa-2 pa-sm-3 pa-md-4">
                    <MudIcon Icon="@Icons.Material.Outlined.[Icon]" Size="Size.Medium" Class="mb-4 mud-text-secondary" />
                    <MudText Typo="Typo.h6" Class="mb-2">
                        @(_[entities].Any() ?
                                            TranslationService.GetTranslation("[entity].no[Entities]MatchFilters", "Nessun [entity] corrisponde ai filtri applicati") :
                                            TranslationService.GetTranslation("[entity].no[Entities]Found", "Nessun [entity] trovato"))
                    </MudText>
                    @if (_[entities].Any())
                    {
                        <MudButton Variant="Variant.Text"
                                   Color="Color.Primary"
                                   StartIcon="@Icons.Material.Outlined.Clear"
                                   OnClick="@ClearFilters">
                            @TranslationService.GetTranslation("[entity].clearFilters", "Cancella filtri")
                        </MudButton>
                    }
                </div>
            </NoRecordsContent>
            </EFTable>
        </div>
    </div>
}
```

### 2. Code Section Pattern

```csharp
@code {
    // Dialog options for fullscreen audit dialog
    private readonly DialogOptions _auditDialogOptions = new() 
    { 
        FullScreen = true, 
        CloseButton = true,
        MaxWidth = MaxWidth.False
    };
    
    // UI State Management
    private bool _isLoading = true;
    private bool _isLoading[Entities] = false;

    // Filter and search state
    private string _searchTerm = string.Empty;
    private CancellationTokenSource? _searchDebounceCts;

    // Data collections
    private List<[EntityDto]> _[entities] = new();
    private HashSet<[EntityDto]> _selected[Entities] = new();

    // EFTable reference
    private EFTable<[EntityDto]> _efTable = null!;

    // Column configuration for EFTable
    private List<EFTableColumnConfiguration> _initialColumns = new()
    {
        new() { PropertyName = "[Property1]", DisplayName = "[Label1]", IsVisible = true, Order = 0 },
        new() { PropertyName = "[Property2]", DisplayName = "[Label2]", IsVisible = true, Order = 1 },
        // ... add all columns
    };

    // Dashboard configuration
    private List<DashboardMetric<[EntityDto]>> _dashboardMetrics = new()
    {
        new()
        {
            Title = "Totale [Entities]",
            Type = MetricType.Count,
            Icon = Icons.Material.Outlined.[Icon],
            Color = "primary",
            Description = "Numero totale di [entities]",
            Format = "N0"
        },
        new()
        {
            Title = "[Metric 2]",
            Type = MetricType.Count,
            Filter = x => /* condition */,
            Icon = Icons.Material.Outlined.[Icon2],
            Color = "success",
            Description = "[Description]",
            Format = "N0"
        },
        new()
        {
            Title = "[Metric 3]",
            Type = MetricType.Count,
            Filter = x => /* condition */,
            Icon = Icons.Material.Outlined.[Icon3],
            Color = "info",
            Description = "[Description]",
            Format = "N0"
        },
        new()
        {
            Title = "Ultimi Aggiunti",
            Type = MetricType.Count,
            Filter = x => x.CreatedAt >= DateTime.Now.AddDays(-30),
            Icon = Icons.Material.Outlined.NewReleases,
            Color = "warning",
            Description = "[Entities] aggiunti negli ultimi 30 giorni",
            Format = "N0"
        }
    };

    private EventCallback<HashSet<[EntityDto]>> _selectedItemsChangedCallback => EventCallback.Factory.Create<HashSet<[EntityDto]>>(this, OnSelectedItemsChanged);

    /// <summary>
    /// Computed property for filtered items based on search criteria.
    /// </summary>
    private IEnumerable<[EntityDto]> _filtered[Entities] => 
        _[entities].Where(x => 
            // Search filter logic
            (string.IsNullOrEmpty(_searchTerm) || 
             x.[Property].Contains(_searchTerm, StringComparison.OrdinalIgnoreCase)));

    /// <summary>
    /// Handles selection changes from EFTable
    /// </summary>
    private void OnSelectedItemsChanged(HashSet<[EntityDto]> items)
    {
        _selected[Entities] = items;
        StateHasChanged();
    }

    /// <summary>
    /// Component initialization.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Check authentication
            var isAuthenticated = await AuthService.IsAuthenticatedAsync();
            if (!isAuthenticated)
            {
                await ShowLoginDialogAsync();
                return;
            }

            // Load data
            await Load[Entities]Async();
        }
        catch (Exception ex)
        {
            Snackbar.Add(TranslationService.GetTranslation("[entity].loadingPageError", "Errore nel caricamento della pagina: {0}", ex.Message), Severity.Error);
            Logger.LogError(ex, "Error loading [entities] page");
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Loads data.
    /// </summary>
    private async Task Load[Entities]Async()
    {
        try
        {
            _isLoading[Entities] = true;
            var result = await [Entity]Service.Get[Entities]Async(/* params */);
            _[entities] = result.ToList(); // or result.Items?.ToList() depending on API
        }
        catch (Exception ex)
        {
            Snackbar.Add(TranslationService.GetTranslation("[entity].loading[Entities]Error", "Errore nel caricamento: {0}", ex.Message), Severity.Error);
            Logger.LogError(ex, "Error loading [entities]");
        }
        finally
        {
            _isLoading[Entities] = false;
        }
    }

    /// <summary>
    /// Clears all active filters.
    /// </summary>
    private void ClearFilters()
    {
        _searchTerm = string.Empty;
        StateHasChanged();
    }

    private async Task OnSearchChanged()
    {
        // Cancel previous debounce if any
        _searchDebounceCts?.Cancel();
        _searchDebounceCts = new CancellationTokenSource();
        var token = _searchDebounceCts.Token;
        
        try
        {
            await Task.Delay(300, token); // Debounce with cancellation
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

    private void Create[Entity]()
    {
        NavigationManager.NavigateTo("/path/to/[entities]/new");
    }

    private void Edit[Entity](Guid id)
    {
        NavigationManager.NavigateTo($"/path/to/[entities]/{id}");
    }

    private async Task Delete[Entity]([EntityDto] item)
    {
        var confirmTitle = TranslationService.GetTranslation("common.confirm", "Conferma");
        var confirmMessage = TranslationService.GetTranslation("[entity].confirmDelete", 
            "Sei sicuro di voler eliminare [entity] '{0}'? Questa azione non può essere annullata.", 
            item.Name);

        var confirm = await DialogService.ShowMessageBox(
            confirmTitle,
            confirmMessage,
            yesText: TranslationService.GetTranslation("common.delete", "Elimina"),
            cancelText: TranslationService.GetTranslation("common.cancel", "Annulla"));

        if (confirm == true)
        {
            try
            {
                await [Entity]Service.Delete[Entity]Async(item.Id);
                _[entities].Remove(item);
                Snackbar.Add(TranslationService.GetTranslation("[entity].[entity]Deleted", "[Entity] eliminato con successo!"), Severity.Success);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Snackbar.Add(TranslationService.GetTranslation("[entity].delete[Entity]Error", 
                    "Errore nell'eliminazione: {0}", ex.Message), Severity.Error);
                Logger.LogError(ex, "Error deleting [entity] {[Entity]Id}", item.Id);
            }
        }
    }

    /// <summary>
    /// Opens the audit log dialog.
    /// </summary>
    private async Task View[Entity]AuditLog([EntityDto] item)
    {
        var parameters = new DialogParameters<AuditHistoryDialog>
        {
            { x => x.EntityType, "[Entity]" },
            { x => x.EntityId, item.Id },
            { x => x.EntityName, item.Name }
        };

        await DialogService.ShowAsync<AuditHistoryDialog>(
            TranslationService.GetTranslation("audit.historyDialog.title", "Cronologia Modifiche"),
            parameters,
            _auditDialogOptions);
    }

    /// <summary>
    /// Deletes all selected items.
    /// </summary>
    private async Task DeleteSelected[Entities]()
    {
        if (_selected[Entities].Count == 0)
            return;

        var confirmTitle = TranslationService.GetTranslation("common.confirm", "Conferma");
        var confirmMessage = TranslationService.GetTranslationFormatted("[entity].confirmDeleteMultiple", 
            "Sei sicuro di voler eliminare {0} [entities] selezionati? Questa azione non può essere annullata.", 
            _selected[Entities].Count);

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
                
                foreach (var item in _selected[Entities].ToList())
                {
                    try
                    {
                        await [Entity]Service.Delete[Entity]Async(item.Id);
                        _[entities].RemoveAll(x => x.Id == item.Id);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        Logger.LogError(ex, "Error deleting [entity] {[Entity]Id}", item.Id);
                    }
                }
                
                _selected[Entities].Clear();
                
                if (failedCount == 0)
                {
                    Snackbar.Add(TranslationService.GetTranslationFormatted("[entity].deletedMultiple", 
                        "{0} [entities] eliminati con successo!", deletedCount), Severity.Success);
                }
                else
                {
                    Snackbar.Add(TranslationService.GetTranslationFormatted("[entity].deletedMultiplePartial", 
                        "{0} [entities] eliminati, {1} falliti", deletedCount, failedCount), Severity.Warning);
                }
                
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Snackbar.Add(TranslationService.GetTranslationFormatted("[entity].deleteError", 
                    "Errore nell'eliminazione: {0}", ex.Message), Severity.Error);
                Logger.LogError(ex, "Error deleting selected [entities]");
            }
        }
    }

    private async Task ShowLoginDialogAsync()
    {
        var result = await AuthenticationDialogService.ShowLoginDialogAsync();
        if (result)
        {
            // Reload the page after successful login
            await OnInitializedAsync();
        }
    }
}
```

## Key Points

1. **Remove MudContainer** - Use div wrappers instead
2. **Add IAuthenticationDialogService** - For login dialog support
3. **Use List<T>** instead of IEnumerable<T> for data collections
4. **Add EFTable reference** - `private EFTable<TItem> _efTable = null!;`
5. **Add column configurations** - InitialColumns list
6. **Add dashboard metrics** - 4 metrics recommended
7. **Add selected items callback** - EventCallback pattern
8. **Use computed property** for filtered items with LINQ Where
9. **Add ShowLoginDialogAsync** method
10. **Update audit log** to use DialogService with full-screen options

## Build and Test

After each page update:
```bash
cd /home/runner/work/EventForge/EventForge
dotnet build --no-incremental EventForge.Client/EventForge.Client.csproj
```

All changes should build without errors (warnings are acceptable if pre-existing).
