# EventForge Management Pages & Drawers - Creation Guide

## Overview
This guide provides the standard patterns and best practices for creating management pages and drawer components in EventForge. Following these patterns ensures UI consistency, proper data flow, and maintainability.

**Last Updated:** January 2025  
**Recent Implementations:** Brand Management, Model Management, Product Management, VatRate Management

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Management Page Pattern](#management-page-pattern)
3. [Drawer Component Pattern](#drawer-component-pattern)
4. [Advanced Drawer Patterns](#advanced-drawer-patterns)
5. [Data Flow](#data-flow)
6. [Creating a New Management Page](#creating-a-new-management-page)
7. [Creating a New Drawer](#creating-a-new-drawer)
8. [Localization](#localization)
9. [Examples](#examples)
10. [Common Patterns](#common-patterns)
11. [Checklist](#checklist)

---

## Architecture Overview

### Component Hierarchy
```
Management Page (.razor)
    ├── SuperAdminPageLayout (optional, for SuperAdmin pages)
    ├── MudTable<T> or MudDataGrid<T>
    │   ├── Filters Section (Search, Dropdowns, Switches)
    │   ├── ActionButtonGroup (Toolbar mode)
    │   ├── Columns with sorting
    │   └── Row Actions (ActionButtonGroup per row)
    └── EntityDrawer Component (using base EntityDrawer)
        ├── FormContent (for Create/Edit modes)
        │   ├── Form Fields (MudTextField, MudSelect, etc.)
        │   └── Nested Entity Management (optional)
        ├── ViewContent (for View mode)
        │   └── Read-only fields
        └── Built-in Action Buttons (Save, Cancel, Close)
```

### Key Components

1. **Management Page**: Lists entities with search, filters, and CRUD actions
2. **EntityDrawer Base Component**: Provides consistent drawer behavior with three modes (Create, Edit, View)
3. **Custom Drawer Component**: Extends EntityDrawer with entity-specific fields
4. **Service**: Handles API communication using IHttpClientService
5. **DTOs**: Data transfer objects for communication (Create, Update, Read)
6. **ActionButtonGroup**: Reusable component for entity actions (View, Edit, Delete, etc.)

---

## Management Page Pattern

### File Structure
```
EventForge.Client/Pages/Management/
└── MyEntityManagement.razor
```

### Modern Template (Using MudTable + ActionButtonGroup)

Recent implementations use this cleaner pattern with MudTable instead of MudDataGrid:

```razor
@page "/management/my-entities"
@using Microsoft.AspNetCore.Authorization
@using EventForge.DTOs.MyEntity
@using EventForge.DTOs.Common
@using EventForge.Client.Shared.Components
@attribute [Authorize]
@inject IAuthService AuthService
@inject IMyEntityService MyEntityService
@inject NavigationManager NavigationManager
@inject ISnackbar Snackbar
@inject IDialogService DialogService
@inject ITranslationService TranslationService
@inject ILogger<MyEntityManagement> Logger

<MudContainer MaxWidth="MaxWidth.False" Class="mt-4">
    @if (_isLoading)
    {
        <MudProgressCircular Color="Color.Primary" Indeterminate="true" />
    }
    else
    {
        <MudPaper Elevation="2" Class="pa-4 mb-4">
            <!-- Page Header -->
            <div class="d-flex justify-space-between align-center mb-4">
                <div>
                    <MudText Typo="Typo.h4">
                        <MudIcon Icon="@Icons.Material.Outlined.Category" Class="mr-2" />
                        @TranslationService.GetTranslation("myEntity.management", "Gestione Entità")
                    </MudText>
                    <MudText Typo="Typo.body2" Class="mud-text-secondary mt-2">
                        @TranslationService.GetTranslation("myEntity.managementDescription", "Gestisci le entità della tua organizzazione")
                    </MudText>
                </div>
            </div>

            <!-- Filters Section -->
            <MudPaper Elevation="0" Class="pa-3 mb-4" Style="background-color: var(--mud-palette-background-grey);">
                <div class="d-flex gap-4 align-center flex-wrap">
                    <MudTextField @bind-Value="_searchTerm"
                                  @bind-Value:after="OnSearchChanged"
                                  Label="@TranslationService.GetTranslation("myEntity.search", "Cerca entità")"
                                  Placeholder="@TranslationService.GetTranslation("myEntity.searchPlaceholder", "Inserisci nome...")"
                                  Variant="Variant.Outlined"
                                  Adornment="Adornment.End"
                                  AdornmentIcon="@Icons.Material.Outlined.Search"
                                  Clearable="true"
                                  Style="flex: 2;" />
                </div>
            </MudPaper>

            <!-- Data Table -->
            <MudPaper Elevation="1" Class="border-rounded">
                <MudCardHeader Class="pa-2">
                    <CardHeaderContent>
                        <MudText Typo="Typo.h6">
                            <MudIcon Icon="@Icons.Material.Outlined.List" Class="mr-2" />
                            @TranslationService.GetTranslation("myEntity.list", "Lista Entità")
                            <MudText Typo="Typo.body2" Class="mud-text-secondary ml-2">
                                (@_filteredEntities.Count() @TranslationService.GetTranslation("common.itemsFound", "elementi trovati"))
                            </MudText>
                        </MudText>
                    </CardHeaderContent>
                    <CardHeaderActions>
                        <ActionButtonGroup Mode="ActionButtonGroupMode.Toolbar"
                                           ShowRefresh="true"
                                           ShowExport="false" 
                                           ShowCreate="true"
                                           ShowAuditLog="false"
                                           CreateIcon="@Icons.Material.Outlined.Add"
                                           CreateTooltip="@TranslationService.GetTranslation("myEntity.createNew", "Crea nuova entità")"
                                           IsDisabled="_isLoadingEntities"
                                           OnRefresh="@LoadEntitiesAsync"
                                           OnCreate="@OpenCreateDrawer" />
                    </CardHeaderActions>
                </MudCardHeader>
                <MudCardContent Class="pa-1">
                    @if (_isLoadingEntities)
                    {
                        <MudProgressLinear Color="Color.Primary" Indeterminate="true" Class="mt-4" />
                    }
                    else
                    {
                        <MudTable T="MyEntityDto" 
                                  Items="_filteredEntities" 
                                  Hover="true" 
                                  Striped="true"
                                  Loading="_isLoadingEntities"
                                  LoadingProgressColor="Color.Info"
                                  SortLabel="@TranslationService.GetTranslation("tooltip.sortColumn", "Ordina colonna")"
                                  AllowUnsorted="false"
                                  FixedHeader="true"
                                  Height="60vh">
                            <HeaderContent>
                                <MudTh>
                                    <MudTableSortLabel SortBy="new Func<MyEntityDto, object>(x => x.Name)">
                                        @TranslationService.GetTranslation("field.name", "Nome")
                                    </MudTableSortLabel>
                                </MudTh>
                                <MudTh>@TranslationService.GetTranslation("field.description", "Descrizione")</MudTh>
                                <MudTh>
                                    <MudTableSortLabel SortBy="new Func<MyEntityDto, object>(x => x.CreatedAt)">
                                        @TranslationService.GetTranslation("field.createdAt", "Creato il")
                                    </MudTableSortLabel>
                                </MudTh>
                                <MudTh Style="text-align: right;">
                                    @TranslationService.GetTranslation("field.actions", "Azioni")
                                </MudTh>
                            </HeaderContent>
                            <RowTemplate>
                                <MudTd DataLabel="@TranslationService.GetTranslation("field.name", "Nome")">
                                    <strong>@context.Name</strong>
                                </MudTd>
                                <MudTd DataLabel="@TranslationService.GetTranslation("field.description", "Descrizione")">
                                    @if (!string.IsNullOrWhiteSpace(context.Description))
                                    {
                                        <span>@(context.Description.Length > 50 ? context.Description.Substring(0, 50) + "..." : context.Description)</span>
                                    }
                                    else
                                    {
                                        <span class="mud-text-secondary">-</span>
                                    }
                                </MudTd>
                                <MudTd DataLabel="@TranslationService.GetTranslation("field.createdAt", "Creato il")">
                                    @context.CreatedAt.ToString("dd/MM/yyyy")
                                </MudTd>
                                <MudTd DataLabel="@TranslationService.GetTranslation("field.actions", "Azioni")" Style="text-align: right;">
                                    <ActionButtonGroup EntityName="@context.Name"
                                                       ItemDisplayName="@context.Name"
                                                       ShowView="true"
                                                       ShowEdit="true"
                                                       ShowAuditLog="true"
                                                       ShowDelete="true"
                                                       OnView="@(() => ViewEntity(context.Id))"
                                                       OnEdit="@(() => EditEntity(context.Id))"
                                                       OnAuditLog="@(() => ViewEntityAuditLog(context))"
                                                       OnDelete="@(() => DeleteEntity(context))" />
                                </MudTd>
                            </RowTemplate>
                            <NoRecordsContent>
                                <div class="pa-4 text-center">
                                    <MudIcon Icon="@Icons.Material.Outlined.SearchOff" Size="Size.Large" Class="mb-2" />
                                    <MudText Typo="Typo.body1">
                                        @TranslationService.GetTranslation("myEntity.noEntitiesFound", "Nessuna entità trovata")
                                    </MudText>
                                    @if (!string.IsNullOrWhiteSpace(_searchTerm))
                                    {
                                        <MudButton Variant="Variant.Text"
                                                   Color="Color.Primary"
                                                   OnClick="ClearFilters"
                                                   Class="mt-2">
                                            @TranslationService.GetTranslation("action.clearFilters", "Cancella filtri")
                                        </MudButton>
                                    }
                                </div>
                            </NoRecordsContent>
                        </MudTable>
                    }
                </MudCardContent>
            </MudPaper>
        </MudPaper>
    }
</MudContainer>

<MyEntityDrawer @ref="_entityDrawer" OnEntitySaved="HandleEntitySaved" />

<!-- Audit History Drawer (optional) -->
<AuditHistoryDrawer @bind-IsOpen="_auditDrawerOpen"
                    EntityType="MyEntity"
                    EntityId="@_selectedEntityForAudit?.Id"
                    EntityName="@_selectedEntityForAudit?.Name" />

@code {
    private MyEntityDrawer _entityDrawer = default!;
    private IEnumerable<MyEntityDto> _entities = new List<MyEntityDto>();
    private IEnumerable<MyEntityDto> _filteredEntities = new List<MyEntityDto>();
    private bool _isLoading = true;
    private bool _isLoadingEntities = false;
    private bool _auditDrawerOpen = false;
    private string _searchTerm = string.Empty;
    private MyEntityDto? _selectedEntityForAudit;

    protected override async Task OnInitializedAsync()
    {
        await LoadEntitiesAsync();
        _isLoading = false;
    }

    private async Task LoadEntitiesAsync()
    {
        try
        {
            _isLoadingEntities = true;
            StateHasChanged();

            var result = await MyEntityService.GetEntitiesAsync(page: 1, pageSize: 1000);
            _entities = result.Items ?? new List<MyEntityDto>();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading entities");
            Snackbar.Add(
                TranslationService.GetTranslation("myEntity.loadError", "Errore nel caricamento delle entità"),
                Severity.Error);
        }
        finally
        {
            _isLoadingEntities = false;
            StateHasChanged();
        }
    }

    private void ApplyFilters()
    {
        _filteredEntities = _entities;

        if (!string.IsNullOrWhiteSpace(_searchTerm))
        {
            var searchLower = _searchTerm.ToLower();
            _filteredEntities = _filteredEntities.Where(e =>
                (e.Name?.ToLower().Contains(searchLower) ?? false) ||
                (e.Description?.ToLower().Contains(searchLower) ?? false));
        }
    }

    private void OnSearchChanged()
    {
        ApplyFilters();
        StateHasChanged();
    }

    private void ClearFilters()
    {
        _searchTerm = string.Empty;
        ApplyFilters();
        StateHasChanged();
    }

    private void OpenCreateDrawer()
    {
        _entityDrawer.OpenForCreate();
    }

    private void ViewEntity(Guid id)
    {
        _entityDrawer.OpenForView(id);
    }

    private void EditEntity(Guid id)
    {
        _entityDrawer.OpenForEdit(id);
    }

    private async Task DeleteEntity(MyEntityDto entity)
    {
        var confirmed = await DialogService.ShowMessageBox(
            TranslationService.GetTranslation("action.confirm", "Conferma"),
            TranslationService.GetTranslation("myEntity.confirmDelete", $"Sei sicuro di voler eliminare '{entity.Name}'?"),
            yesText: TranslationService.GetTranslation("action.delete", "Elimina"),
            cancelText: TranslationService.GetTranslation("action.cancel", "Annulla"));

        if (confirmed == true)
        {
            try
            {
                await MyEntityService.DeleteEntityAsync(entity.Id);
                Snackbar.Add(
                    TranslationService.GetTranslation("myEntity.deleteSuccess", "Entità eliminata con successo"),
                    Severity.Success);
                await LoadEntitiesAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting entity {EntityId}", entity.Id);
                Snackbar.Add(
                    TranslationService.GetTranslation("myEntity.deleteError", "Errore nell'eliminazione dell'entità"),
                    Severity.Error);
            }
        }
    }

    private async Task HandleEntitySaved()
    {
        await LoadEntitiesAsync();
    }
    
    private async Task ViewEntityAuditLog(MyEntityDto entity)
    {
        _selectedEntityForAudit = entity;
        _auditDrawerOpen = true;
        await Task.CompletedTask;
    }
}
```

### Key Improvements in Modern Pattern

1. **ActionButtonGroup in Toolbar Mode**: Clean refresh and create buttons
2. **MudTable instead of MudDataGrid**: Better performance and simpler API
3. **Separate loading states**: `_isLoading` for initial load, `_isLoadingEntities` for refresh
4. **Filter section styling**: Uses background-grey for visual separation
5. **NoRecordsContent**: Provides clear empty state with option to clear filters
6. **Height limitation**: `Height="60vh"` with `FixedHeader="true"` for better UX
7. **Responsive columns**: Uses `DataLabel` for mobile view
8. **Audit log integration**: Optional AuditHistoryDrawer for tracking changes

### ActionButtonGroup Usage

#### Toolbar Mode (for page-level actions)
```razor
<ActionButtonGroup Mode="ActionButtonGroupMode.Toolbar"
                   ShowRefresh="true"
                   ShowCreate="true"
                   ShowExport="false" 
                   ShowAuditLog="false"
                   CreateIcon="@Icons.Material.Outlined.Add"
                   CreateTooltip="@TranslationService.GetTranslation("myEntity.createNew", "Crea nuovo")"
                   IsDisabled="_isLoadingEntities"
                   OnRefresh="@LoadEntitiesAsync"
                   OnCreate="@OpenCreateDrawer" />
```

#### Row Mode (for per-entity actions)
```razor
<ActionButtonGroup EntityName="@entity.Name"
                   ItemDisplayName="@entity.Name"
                   ShowView="true"
                   ShowEdit="true"
                   ShowDelete="true"
                   ShowAuditLog="true"
                   OnView="@(() => ViewEntity(entity.Id))"
                   OnEdit="@(() => EditEntity(entity.Id))"
                   OnDelete="@(() => DeleteEntity(entity))"
                   OnAuditLog="@(() => ViewAuditLog(entity))" />
```

    private async Task DeleteEntity(MyEntityDto entity)
    {
        var confirmed = await DialogService.ShowMessageBox(
            TranslationService.GetTranslation("action.confirm", "Conferma"),
            TranslationService.GetTranslation("myEntity.confirmDelete", 
                $"Sei sicuro di voler eliminare '{entity.Name}'?"),
            yesText: TranslationService.GetTranslation("action.delete", "Elimina"),
            cancelText: TranslationService.GetTranslation("action.cancel", "Annulla"));

        if (confirmed == true)
        {
            try
            {
                await MyEntityService.DeleteEntityAsync(entity.Id);
                Snackbar.Add(
                    TranslationService.GetTranslation("myEntity.deleteSuccess", "Entità eliminata con successo"),
                    Severity.Success);
                await LoadEntitiesAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting entity {EntityId}", entity.Id);
                Snackbar.Add(
                    TranslationService.GetTranslation("myEntity.deleteError", "Errore nell'eliminazione"),
                    Severity.Error);
            }
        }
    }

    private async Task HandleEntitySaved()
    {
        await LoadEntitiesAsync();
        Snackbar.Add(
            TranslationService.GetTranslation("myEntity.saveSuccess", "Entità salvata con successo"),
            Severity.Success);
    }
}
```

---

## Advanced Drawer Patterns

### Pattern 1: EntityDrawer Base Component (Modern Approach)

**Recent implementations use the `EntityDrawer` base component** which provides:
- Consistent drawer behavior across all entities
- Built-in mode management (Create, Edit, View)
- Standardized layout and styling
- Accessibility support
- Responsive width handling

#### EntityDrawer Structure

```razor
<EntityDrawer @bind-IsOpen="@IsOpen"
              @bind-Mode="@Mode"
              EntityName="@TranslationService.GetTranslation("field.brand", "Marchio")"
              Model="@_model"
              OnSave="@HandleSave"
              OnCancel="@HandleCancel"
              OnClose="@HandleClose"
              CustomTitle="@_customTitle"
              AllowEdit="@AllowEdit"
              Width="50%">
    
    <FormContent>
        <!-- Form fields for Create/Edit modes -->
    </FormContent>
    
    <ViewContent>
        <!-- Read-only fields for View mode -->
    </ViewContent>
</EntityDrawer>
```

#### Key Properties

- `IsOpen`: Controls drawer visibility
- `Mode`: EntityDrawerMode enum (Create, Edit, View)
- `EntityName`: Used in default title if CustomTitle not provided
- `Model`: The entity data model being edited
- `OnSave`, `OnCancel`, `OnClose`: Event callbacks
- `CustomTitle`: Override default title (e.g., "Modifica Marchio: Nike")
- `AllowEdit`: Controls if edit mode is permitted
- `Width`: Drawer width ("50%", "60%", "800px", etc.)

#### Code-Behind Pattern

```csharp
@code {
    [Parameter]
    public EventCallback OnEntitySaved { get; set; }
    
    [Parameter]
    public bool AllowEdit { get; set; } = true;
    
    public bool IsOpen { get; set; }
    public EntityDrawerMode Mode { get; set; } = EntityDrawerMode.View;
    public BrandDto? OriginalBrand { get; private set; }
    
    private BrandDto _model = new();
    private string? _customTitle;
    
    public void OpenForCreate()
    {
        Mode = EntityDrawerMode.Create;
        OriginalBrand = null;
        _model = new BrandDto();
        _customTitle = TranslationService.GetTranslation("drawer.title.creaBrand", "Crea Nuovo Marchio");
        IsOpen = true;
        StateHasChanged();
    }
    
    public async Task OpenForEdit(Guid id)
    {
        try
        {
            Mode = EntityDrawerMode.Edit;
            var brand = await BrandService.GetBrandByIdAsync(id);
            
            if (brand == null)
            {
                Snackbar.Add(
                    TranslationService.GetTranslation("brand.notFound", "Marchio non trovato"),
                    Severity.Warning);
                return;
            }
            
            OriginalBrand = brand;
            _model = new BrandDto
            {
                Id = brand.Id,
                Name = brand.Name,
                Description = brand.Description
                // ... other fields
            };
            
            _customTitle = $"{TranslationService.GetTranslation("drawer.title.modificaBrand", "Modifica Marchio")}: {brand.Name}";
            IsOpen = true;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading brand for edit");
            Snackbar.Add(
                TranslationService.GetTranslation("brand.loadError", "Errore nel caricamento del marchio"),
                Severity.Error);
        }
    }
    
    public async Task OpenForView(Guid id)
    {
        // Similar to OpenForEdit but with Mode = EntityDrawerMode.View
    }
    
    private async Task HandleSave()
    {
        try
        {
            if (Mode == EntityDrawerMode.Create)
            {
                var createDto = new CreateBrandDto
                {
                    Name = _model.Name,
                    Description = _model.Description
                    // ... map other fields
                };
                
                await BrandService.CreateBrandAsync(createDto);
                Snackbar.Add(
                    TranslationService.GetTranslation("brand.createSuccess", "Marchio creato con successo"),
                    Severity.Success);
            }
            else if (Mode == EntityDrawerMode.Edit && OriginalBrand != null)
            {
                var updateDto = new UpdateBrandDto
                {
                    Name = _model.Name,
                    Description = _model.Description
                    // ... map other fields
                };
                
                await BrandService.UpdateBrandAsync(OriginalBrand.Id, updateDto);
                Snackbar.Add(
                    TranslationService.GetTranslation("brand.updateSuccess", "Marchio aggiornato con successo"),
                    Severity.Success);
            }
            
            await OnEntitySaved.InvokeAsync();
            HandleClose();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving brand");
            Snackbar.Add(
                TranslationService.GetTranslation("brand.saveError", "Errore nel salvataggio del marchio"),
                Severity.Error);
        }
    }
    
    private void HandleCancel()
    {
        HandleClose();
    }
    
    private void HandleClose()
    {
        IsOpen = false;
        _model = new BrandDto();
        OriginalBrand = null;
        _customTitle = null;
        StateHasChanged();
    }
}
```

### Pattern 2: Nested Entity Management in Drawers

**Example: BrandDrawer with embedded Model management**

When editing a Brand, you can display and manage related Models directly within the drawer.

#### Implementation

```razor
<EntityDrawer @bind-IsOpen="@IsOpen" ...>
    <FormContent>
        <!-- Brand fields -->
        <MudItem xs="12">
            <MudTextField @bind-Value="_model.Name" ... />
        </MudItem>
        
        @if (Mode == EntityDrawerMode.Edit && OriginalBrand != null)
        {
            <!-- Models Section with inline CRUD -->
            @if (_loadingModels)
            {
                <MudItem xs="12" Class="mt-4">
                    <MudProgressLinear Color="Color.Primary" Indeterminate="true" />
                </MudItem>
            }
            else
            {
                <MudItem xs="12" Class="mt-4">
                    <MudExpansionPanels>
                        <MudExpansionPanel>
                            <TitleContent>
                                <div class="d-flex justify-space-between align-center" style="width: 100%;">
                                    <MudText>@($"{TranslationService.GetTranslation("field.models", "Modelli")} ({_models?.Count() ?? 0})")</MudText>
                                    <MudIconButton Icon="@Icons.Material.Filled.Add" 
                                                  Color="Color.Primary" 
                                                  Size="Size.Small"
                                                  OnClick="@(() => OpenAddModelDialog())"
                                                  aria-label="@TranslationService.GetTranslation("button.addModel", "Aggiungi modello")" />
                                </div>
                            </TitleContent>
                            <ChildContent>
                                @if (_models?.Any() == true)
                                {
                                    <MudTable Items="_models" Hover="true" Dense="true" Striped="true">
                                        <HeaderContent>
                                            <MudTh>@TranslationService.GetTranslation("field.name", "Nome")</MudTh>
                                            <MudTh>@TranslationService.GetTranslation("field.description", "Descrizione")</MudTh>
                                            <MudTh>@TranslationService.GetTranslation("common.actions", "Azioni")</MudTh>
                                        </HeaderContent>
                                        <RowTemplate>
                                            <MudTd>@context.Name</MudTd>
                                            <MudTd>@(context.Description?.Length > 30 ? context.Description.Substring(0, 30) + "..." : context.Description)</MudTd>
                                            <MudTd>
                                                <MudIconButton Icon="@Icons.Material.Filled.Edit" 
                                                              Color="Color.Primary" 
                                                              Size="Size.Small"
                                                              OnClick="@(() => OpenEditModelDialog(context))" />
                                                <MudIconButton Icon="@Icons.Material.Filled.Delete" 
                                                              Color="Color.Error" 
                                                              Size="Size.Small"
                                                              OnClick="@(() => DeleteModel(context.Id))" />
                                            </MudTd>
                                        </RowTemplate>
                                    </MudTable>
                                }
                                else
                                {
                                    <MudText Typo="Typo.body2" Class="pa-4">
                                        @TranslationService.GetTranslation("messages.noModels", "Nessun modello disponibile")
                                    </MudText>
                                }
                            </ChildContent>
                        </MudExpansionPanel>
                    </MudExpansionPanels>
                </MudItem>
            }
        }
    </FormContent>
</EntityDrawer>

@code {
    private bool _loadingModels = false;
    private IEnumerable<ModelDto>? _models;
    
    private async Task LoadModelsAsync()
    {
        if (OriginalBrand == null) return;
        
        try
        {
            _loadingModels = true;
            StateHasChanged();
            
            var result = await ModelService.GetModelsByBrandIdAsync(OriginalBrand.Id, page: 1, pageSize: 1000);
            _models = result.Items ?? new List<ModelDto>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load models for brand {BrandId}", OriginalBrand.Id);
            Snackbar.Add(
                TranslationService.GetTranslation("messages.loadFailed", "Errore durante il caricamento"), 
                Severity.Warning);
        }
        finally
        {
            _loadingModels = false;
            StateHasChanged();
        }
    }
    
    private async Task OpenAddModelDialog()
    {
        if (OriginalBrand == null) return;
        
        var parameters = new DialogParameters
        {
            ["BrandId"] = OriginalBrand.Id,
            ["BrandName"] = OriginalBrand.Name
        };
        
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
        var dialog = await DialogService.ShowAsync<AddModelDialog>(
            TranslationService.GetTranslation("dialog.addModel", "Aggiungi Modello"),
            parameters,
            options
        );
        
        var result = await dialog.Result;
        if (!result.Canceled)
        {
            await LoadModelsAsync();
        }
    }
}
```

**Key Benefits:**
- User can manage related entities without leaving the parent drawer
- Maintains context (user stays focused on the brand)
- Uses MudExpansionPanels to keep UI clean and organized
- Inline CRUD operations with dialogs for add/edit

### Pattern 3: Autocomplete Selection with Relationships

**Example: ModelDrawer with Brand selection**

When a child entity references a parent entity, use MudAutocomplete for selection.

```razor
<EntityDrawer ...>
    <FormContent>
        <MudGrid>
            <MudItem xs="12">
                <MudAutocomplete T="BrandDto"
                                 @bind-Value="_selectedBrand"
                                 Label="@($"{TranslationService.GetTranslation("drawer.field.brand", "Marchio")} *")"
                                 Variant="Variant.Outlined"
                                 SearchFunc="@SearchBrands"
                                 ToStringFunc="@(b => b?.Name ?? "")"
                                 Required="true"
                                 ResetValueOnEmptyText="true"
                                 CoerceText="true"
                                 CoerceValue="true"
                                 aria-describedby="brand-help">
                    <ItemTemplate Context="brand">
                        <MudText>@brand.Name</MudText>
                        @if (!string.IsNullOrEmpty(brand.Country))
                        {
                            <MudText Typo="Typo.caption">@brand.Country</MudText>
                        }
                    </ItemTemplate>
                </MudAutocomplete>
                <MudText id="brand-help" Typo="Typo.caption" Class="mud-input-helper-text">
                    @TranslationService.GetTranslation("drawer.helperText.brand", "Seleziona il marchio del modello")
                </MudText>
            </MudItem>
            
            <MudItem xs="12">
                <MudTextField @bind-Value="_model.Name"
                              Label="@($"{TranslationService.GetTranslation("drawer.field.nomeModello", "Nome Modello")} *")"
                              ... />
            </MudItem>
        </MudGrid>
    </FormContent>
</EntityDrawer>

@code {
    private BrandDto? _selectedBrand;
    private IEnumerable<BrandDto> _brands = new List<BrandDto>();
    
    protected override async Task OnInitializedAsync()
    {
        await LoadBrandsAsync();
    }
    
    private async Task LoadBrandsAsync()
    {
        try
        {
            var result = await BrandService.GetBrandsAsync(page: 1, pageSize: 1000);
            _brands = result.Items ?? new List<BrandDto>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading brands");
        }
    }
    
    private async Task<IEnumerable<BrandDto>> SearchBrands(string value, CancellationToken token)
    {
        await Task.CompletedTask;
        if (string.IsNullOrEmpty(value))
            return _brands;
        
        return _brands.Where(b => b.Name.Contains(value, StringComparison.OrdinalIgnoreCase));
    }
    
    private async Task HandleSave()
    {
        if (_selectedBrand == null)
        {
            Snackbar.Add(
                TranslationService.GetTranslation("model.brandRequired", "Il marchio è obbligatorio"),
                Severity.Warning);
            return;
        }
        
        // ... save logic using _selectedBrand.Id
    }
}
```

**Key Features:**
- Type-safe autocomplete with generic type
- Custom item template for rich display
- Search function for filtering
- Required validation
- Helper text for guidance

### Pattern 4: Complex Field Types

#### Date Pickers

```razor
<MudItem xs="12" md="6">
    <MudDatePicker @bind-Date="_validFrom"
                   Label="@TranslationService.GetTranslation("drawer.field.validoDa", "Valido Da")"
                   Variant="Variant.Outlined"
                   aria-describedby="validFrom-help" />
    <MudText id="validFrom-help" Typo="Typo.caption" Class="mud-input-helper-text">
        @TranslationService.GetTranslation("drawer.helperText.validoDa", "Data inizio validità")
    </MudText>
</MudItem>
```

#### Numeric Fields with Validation

```razor
<MudItem xs="12" md="6">
    <MudNumericField @bind-Value="_model.Percentage"
                     Label="@($"{TranslationService.GetTranslation("drawer.field.percentuale", "Percentuale")} *")"
                     Variant="Variant.Outlined"
                     Min="0"
                     Max="100"
                     Required="true"
                     Immediate="true"
                     aria-describedby="percentage-help" />
    <MudText id="percentage-help" Typo="Typo.caption" Class="mud-input-helper-text">
        @TranslationService.GetTranslation("drawer.helperText.percentuale", "Valore da 0 a 100")
    </MudText>
</MudItem>
```

#### Select with Enum Values

```razor
<MudItem xs="12" md="6">
    <MudSelect @bind-Value="_model.Status"
               Label="@($"{TranslationService.GetTranslation("drawer.field.stato", "Stato")} *")"
               Variant="Variant.Outlined"
               Required="true"
               aria-describedby="status-help">
        <MudSelectItem Value="@VatRateStatus.Active">
            @TranslationService.GetTranslation("drawer.status.attivo", "Attivo")
        </MudSelectItem>
        <MudSelectItem Value="@VatRateStatus.Suspended">
            @TranslationService.GetTranslation("drawer.status.sospeso", "Sospeso")
        </MudSelectItem>
        <MudSelectItem Value="@VatRateStatus.Deleted">
            @TranslationService.GetTranslation("drawer.status.eliminato", "Eliminato")
        </MudSelectItem>
    </MudSelect>
    <MudText id="status-help" Typo="Typo.caption" Class="mud-input-helper-text">
        @TranslationService.GetTranslation("drawer.helperText.stato", "Stato dell'aliquota")
    </MudText>
</MudItem>
```

### Pattern 5: Read-Only Display in View Mode

```razor
<ViewContent>
    <MudGrid>
        <MudItem xs="12">
            <MudTextField Value="@(OriginalBrand?.Name ?? "")"
                          Label="@TranslationService.GetTranslation("drawer.field.nomeBrand", "Nome Marchio")"
                          Variant="Variant.Outlined"
                          ReadOnly="true"
                          aria-label="@TranslationService.GetTranslation("drawer.field.nomeBrand", "Nome Marchio")" />
        </MudItem>
        
        @if (OriginalBrand != null)
        {
            <MudItem xs="12" md="6">
                <MudTextField Value="@OriginalBrand.Id.ToString()"
                              Label="@TranslationService.GetTranslation("drawer.field.id", "ID")"
                              Variant="Variant.Outlined"
                              ReadOnly="true" />
            </MudItem>
            
            <MudItem xs="12" md="6">
                <MudTextField Value="@OriginalBrand.CreatedAt.ToString("dd/MM/yyyy HH:mm")"
                              Label="@TranslationService.GetTranslation("drawer.field.dataCreazione", "Data Creazione")"
                              Variant="Variant.Outlined"
                              ReadOnly="true" />
            </MudItem>
            
            @if (!string.IsNullOrWhiteSpace(OriginalBrand.CreatedBy))
            {
                <MudItem xs="12">
                    <MudTextField Value="@OriginalBrand.CreatedBy"
                                  Label="@TranslationService.GetTranslation("drawer.field.creatoDa", "Creato Da")"
                                  Variant="Variant.Outlined"
                                  ReadOnly="true" />
                </MudItem>
            }
        }
    </MudGrid>
</ViewContent>
```

**Best Practices for View Mode:**
- Use Value instead of @bind-Value for read-only fields
- Set ReadOnly="true" on all fields
- Display metadata (ID, CreatedAt, CreatedBy, UpdatedAt, etc.)
- Show related entities in expansion panels (read-only tables)

---

## Drawer Component Pattern

### File Structure
```
EventForge.Client/Shared/Components/
└── MyEntityDrawer.razor
```

### Standard Template

```razor
@using EventForge.DTOs.MyEntity
@using EventForge.Client.Services
@inject IMyEntityService MyEntityService
@inject ISnackbar Snackbar
@inject ITranslationService TranslationService
@inject ILogger<MyEntityDrawer> Logger

<MudDrawer @bind-Open="_isOpen"
           Anchor="Anchor.Right"
           Elevation="1"
           Variant="@DrawerVariant.Temporary"
           Width="600px">
    <MudDrawerHeader>
        <MudText Typo="Typo.h6">
            @if (_mode == DrawerMode.Create)
            {
                @TranslationService.GetTranslation("myEntity.createTitle", "Crea Nuova Entità")
            }
            else if (_mode == DrawerMode.Edit)
            {
                @TranslationService.GetTranslation("myEntity.editTitle", $"Modifica: {_entity?.Name}")
            }
            else
            {
                @TranslationService.GetTranslation("myEntity.viewTitle", $"Visualizza: {_entity?.Name}")
            }
        </MudText>
    </MudDrawerHeader>
    
    <MudDivider />
    
    <MudDrawerContent Class="pa-4">
        @if (_isLoading)
        {
            <div class="d-flex justify-center align-center" style="height: 200px;">
                <MudProgressCircular Indeterminate="true" />
            </div>
        }
        else if (_entity != null)
        {
            <MudForm @ref="_form" @bind-IsValid="_isFormValid">
                <MudGrid>
                    @* Name Field *@
                    <MudItem xs="12">
                        <MudTextField @bind-Value="_entity.Name"
                                      Label="@TranslationService.GetTranslation("field.name", "Nome")"
                                      Required="true"
                                      RequiredError="@TranslationService.GetTranslation("validation.required", "Campo obbligatorio")"
                                      ReadOnly="@(_mode == DrawerMode.View)"
                                      HelperText="@TranslationService.GetTranslation("myEntity.nameHelper", "Inserisci il nome dell'entità")" />
                    </MudItem>

                    @* Code Field *@
                    <MudItem xs="12">
                        <MudTextField @bind-Value="_entity.Code"
                                      Label="@TranslationService.GetTranslation("field.code", "Codice")"
                                      Required="true"
                                      RequiredError="@TranslationService.GetTranslation("validation.required", "Campo obbligatorio")"
                                      ReadOnly="@(_mode == DrawerMode.View)"
                                      HelperText="@TranslationService.GetTranslation("myEntity.codeHelper", "Codice univoco")" />
                    </MudItem>

                    @* Description Field *@
                    <MudItem xs="12">
                        <MudTextField @bind-Value="_entity.Description"
                                      Label="@TranslationService.GetTranslation("field.description", "Descrizione")"
                                      Lines="3"
                                      ReadOnly="@(_mode == DrawerMode.View)"
                                      HelperText="@TranslationService.GetTranslation("myEntity.descriptionHelper", "Descrizione opzionale")" />
                    </MudItem>

                    @* Active Checkbox *@
                    <MudItem xs="12">
                        <MudCheckBox @bind-Value="_entity.IsActive"
                                     Label="@TranslationService.GetTranslation("field.isActive", "Attivo")"
                                     ReadOnly="@(_mode == DrawerMode.View)"
                                     Color="Color.Primary" />
                    </MudItem>

                    @* Notes Field *@
                    <MudItem xs="12">
                        <MudTextField @bind-Value="_entity.Notes"
                                      Label="@TranslationService.GetTranslation("field.notes", "Note")"
                                      Lines="5"
                                      ReadOnly="@(_mode == DrawerMode.View)"
                                      HelperText="@TranslationService.GetTranslation("common.notesHelper", "Note aggiuntive")" />
                    </MudItem>
                </MudGrid>
            </MudForm>
        }
    </MudDrawerContent>
    
    <MudDivider />
    
    <MudDrawerFooter Class="pa-4">
        <div class="d-flex justify-space-between" style="width: 100%;">
            <MudButton Variant="Variant.Outlined"
                       Color="Color.Default"
                       OnClick="Close">
                @TranslationService.GetTranslation("action.close", "Chiudi")
            </MudButton>
            
            @if (_mode != DrawerMode.View)
            {
                <MudButton Variant="Variant.Filled"
                           Color="Color.Primary"
                           OnClick="SaveEntity"
                           Disabled="@(!_isFormValid || _isSaving)">
                    @if (_isSaving)
                    {
                        <MudProgressCircular Size="Size.Small" Indeterminate="true" />
                    }
                    else
                    {
                        @TranslationService.GetTranslation("action.save", "Salva")
                    }
                </MudButton>
            }
        </div>
    </MudDrawerFooter>
</MudDrawer>

@code {
    [Parameter]
    public EventCallback OnEntitySaved { get; set; }

    private enum DrawerMode { Create, Edit, View }

    private MudForm _form = default!;
    private bool _isOpen;
    private bool _isLoading;
    private bool _isSaving;
    private bool _isFormValid;
    private DrawerMode _mode;
    private MyEntityDto? _entity;
    private Guid? _entityId;

    public void OpenForCreate()
    {
        _mode = DrawerMode.Create;
        _entityId = null;
        _entity = new MyEntityDto
        {
            IsActive = true
        };
        _isOpen = true;
        StateHasChanged();
    }

    public async Task OpenForEdit(Guid id)
    {
        _mode = DrawerMode.Edit;
        _entityId = id;
        await LoadEntityAsync(id);
        _isOpen = true;
        StateHasChanged();
    }

    public async Task OpenForView(Guid id)
    {
        _mode = DrawerMode.View;
        _entityId = id;
        await LoadEntityAsync(id);
        _isOpen = true;
        StateHasChanged();
    }

    private async Task LoadEntityAsync(Guid id)
    {
        try
        {
            _isLoading = true;
            StateHasChanged();

            _entity = await MyEntityService.GetEntityAsync(id);
            
            if (_entity == null)
            {
                Snackbar.Add(
                    TranslationService.GetTranslation("myEntity.notFound", "Entità non trovata"),
                    Severity.Warning);
                Close();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading entity {EntityId}", id);
            Snackbar.Add(
                TranslationService.GetTranslation("myEntity.loadError", "Errore nel caricamento"),
                Severity.Error);
            Close();
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task SaveEntity()
    {
        if (_entity == null || !_isFormValid)
            return;

        try
        {
            _isSaving = true;
            StateHasChanged();

            if (_mode == DrawerMode.Create)
            {
                var createDto = new CreateMyEntityDto
                {
                    Name = _entity.Name,
                    Code = _entity.Code,
                    Description = _entity.Description,
                    IsActive = _entity.IsActive,
                    Notes = _entity.Notes
                };
                
                await MyEntityService.CreateEntityAsync(createDto);
            }
            else if (_mode == DrawerMode.Edit && _entityId.HasValue)
            {
                var updateDto = new UpdateMyEntityDto
                {
                    Name = _entity.Name,
                    Code = _entity.Code,
                    Description = _entity.Description,
                    IsActive = _entity.IsActive,
                    Notes = _entity.Notes
                };
                
                await MyEntityService.UpdateEntityAsync(_entityId.Value, updateDto);
            }

            await OnEntitySaved.InvokeAsync();
            Close();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving entity");
            Snackbar.Add(
                TranslationService.GetTranslation("myEntity.saveError", "Errore nel salvataggio"),
                Severity.Error);
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }

    private void Close()
    {
        _isOpen = false;
        _entity = null;
        _entityId = null;
        StateHasChanged();
    }
}
```

---

## Data Flow

### Management Page → Drawer → Service → API

```
User Action (Create/Edit/View)
    ↓
Management Page calls Drawer method
    ↓
Drawer opens and loads data (if Edit/View)
    ↓
Service fetches data from API
    ↓
Drawer displays form
    ↓
User saves changes
    ↓
Drawer calls Service to save
    ↓
Service posts to API
    ↓
Drawer invokes OnEntitySaved callback
    ↓
Management Page reloads data
    ↓
Grid updates with new data
```

---

## Creating a New Management Page

### Step-by-Step Guide

#### 1. Create the Service
Follow the [Service Creation Guide](./SERVICE_CREATION_GUIDE.md)

#### 2. Create the Drawer Component
```bash
EventForge.Client/Shared/Components/MyEntityDrawer.razor
```

Use the [Drawer Template](#drawer-component-pattern) as starting point.

#### 3. Create the Management Page
```bash
EventForge.Client/Pages/Management/MyEntityManagement.razor
```

Use the [Management Page Template](#management-page-pattern) as starting point.

#### 4. Add Navigation Menu Item

Edit `EventForge.Client/Layout/NavMenu.razor`:

```razor
@* Add to Administration section *@
<MudNavLink Href="/management/my-entities"
            Icon="@Icons.Material.Filled.Category"
            Match="NavLinkMatch.Prefix">
    @TranslationService.GetTranslation("nav.myEntityManagement", "Gestione Entità")
</MudNavLink>
```

#### 5. Add Translations

Edit `EventForge.Client/wwwroot/i18n/it.json`:

```json
{
  "nav.myEntityManagement": "Gestione Entità",
  "myEntity.management": "Gestione Entità",
  "myEntity.managementDescription": "Gestisci le entità della tua organizzazione",
  "myEntity.search": "Cerca entità",
  "myEntity.searchPlaceholder": "Inserisci nome...",
  "myEntity.list": "Lista Entità",
  "myEntity.createNew": "Crea nuova entità",
  "myEntity.createTitle": "Crea Nuova Entità",
  "myEntity.editTitle": "Modifica Entità",
  "myEntity.viewTitle": "Visualizza Entità",
  "myEntity.confirmDelete": "Sei sicuro di voler eliminare '{0}'?",
  "myEntity.deleteSuccess": "Entità eliminata con successo",
  "myEntity.deleteError": "Errore nell'eliminazione",
  "myEntity.saveSuccess": "Entità salvata con successo",
  "myEntity.saveError": "Errore nel salvataggio",
  "myEntity.loadError": "Errore nel caricamento",
  "myEntity.notFound": "Entità non trovata",
  "myEntity.noEntitiesFound": "Nessuna entità trovata",
  "myEntity.nameHelper": "Inserisci il nome dell'entità",
  "myEntity.codeHelper": "Codice univoco dell'entità"
}
```

#### 6. Test the Implementation

1. Run the application
2. Navigate to the page
3. Test Create, Edit, View, Delete operations
4. Verify search and filters
5. Check translations
6. Test error scenarios

---

## Creating a New Drawer

### Essential Elements

#### 1. Three Modes
```csharp
private enum DrawerMode { Create, Edit, View }
```

#### 2. Public Methods
```csharp
public void OpenForCreate() { }
public async Task OpenForEdit(Guid id) { }
public async Task OpenForView(Guid id) { }
```

#### 3. Callback Event
```csharp
[Parameter]
public EventCallback OnEntitySaved { get; set; }
```

#### 4. Form Validation
```razor
<MudForm @ref="_form" @bind-IsValid="_isFormValid">
    @* Form fields *@
</MudForm>
```

#### 5. Save Logic
```csharp
private async Task SaveEntity()
{
    if (_mode == DrawerMode.Create)
    {
        await MyEntityService.CreateEntityAsync(createDto);
    }
    else if (_mode == DrawerMode.Edit)
    {
        await MyEntityService.UpdateEntityAsync(id, updateDto);
    }
    
    await OnEntitySaved.InvokeAsync();
    Close();
}
```

---

## Localization

### Translation Keys Naming Convention

#### Navigation
```
nav.{entityName}Management
```
Example: `nav.supplierManagement`

#### Page Titles and Descriptions
```
{entityName}.management
{entityName}.managementDescription
```

#### Fields
```
field.{fieldName}
drawer.field.{specificField}
```

#### Actions
```
action.{actionName}
```
Examples: `action.save`, `action.delete`, `action.refresh`

#### Status
```
status.{statusName}
```
Examples: `status.active`, `status.inactive`

#### Messages
```
{entityName}.{messageType}
```
Examples:
- `supplier.saveSuccess`
- `supplier.deleteError`
- `supplier.notFound`

### Full Translation Example

```json
{
  "nav.supplierManagement": "Gestione Fornitori",
  "supplier.management": "Gestione Fornitori",
  "supplier.managementDescription": "Gestisci i fornitori della tua organizzazione",
  "supplier.search": "Cerca fornitori",
  "supplier.searchPlaceholder": "Inserisci nome o codice...",
  "supplier.list": "Lista Fornitori",
  "supplier.createNew": "Crea nuovo fornitore",
  "supplier.createTitle": "Crea Nuovo Fornitore",
  "supplier.editTitle": "Modifica Fornitore: {0}",
  "supplier.viewTitle": "Visualizza Fornitore: {0}",
  "supplier.confirmDelete": "Sei sicuro di voler eliminare il fornitore '{0}'?",
  "supplier.deleteSuccess": "Fornitore eliminato con successo",
  "supplier.deleteError": "Errore nell'eliminazione del fornitore",
  "supplier.saveSuccess": "Fornitore salvato con successo",
  "supplier.saveError": "Errore nel salvataggio del fornitore",
  "supplier.loadError": "Errore nel caricamento dei fornitori",
  "supplier.notFound": "Fornitore non trovato",
  "supplier.noSuppliersFound": "Nessun fornitore trovato",
  "field.supplierName": "Nome Fornitore",
  "field.supplierCode": "Codice Fornitore",
  "field.vatNumber": "Partita IVA",
  "field.taxCode": "Codice Fiscale",
  "status.active": "Attivo",
  "status.inactive": "Non attivo"
}
```

---

## Examples

### Example 1: Simple Entity (VatRate)

**Reference:** `VatRateManagement.razor` + `VatRateDrawer.razor`

Simple entity with status enum, date fields, and numeric validation.

**Key Features:**
- Status dropdown with enum values
- Percentage field with Min/Max validation
- Optional date range (ValidFrom, ValidTo)
- Multi-line notes field

**Drawer Fields:**
- Name (required, max 50 chars)
- Percentage (required, 0-100)
- Status (required: Active/Suspended/Deleted)
- ValidFrom (optional date)
- ValidTo (optional date)
- Notes (optional, max 200 chars)

---

### Example 2: Entity with Parent Relationship (Model)

**Reference:** `ModelManagement.razor` + `ModelDrawer.razor`

Child entity that references a parent entity (Brand) using autocomplete.

**Key Features:**
- MudAutocomplete for Brand selection
- Rich item template showing brand name and country
- Search functionality for filtering brands
- Required validation for parent reference

**Implementation Highlights:**
```razor
<MudAutocomplete T="BrandDto"
                 @bind-Value="_selectedBrand"
                 SearchFunc="@SearchBrands"
                 ToStringFunc="@(b => b?.Name ?? "")"
                 Required="true">
    <ItemTemplate Context="brand">
        <MudText>@brand.Name</MudText>
        <MudText Typo="Typo.caption">@brand.Country</MudText>
    </ItemTemplate>
</MudAutocomplete>
```

---

### Example 3: Entity with Nested Management (Brand)

**Reference:** `BrandManagement.razor` + `BrandDrawer.razor`

Parent entity that allows managing child entities (Models) within its drawer.

**Key Features:**
- **MudExpansionPanels** to show related Models
- Inline CRUD operations for Models (Add, Edit, Delete)
- Loading state while fetching Models
- Dialog-based add/edit for child entities
- Count badge showing number of related entities

**Implementation Pattern:**
```razor
<!-- In Edit mode only -->
@if (Mode == EntityDrawerMode.Edit && OriginalBrand != null)
{
    <MudExpansionPanel>
        <TitleContent>
            <div class="d-flex justify-space-between align-center">
                <MudText>Models (@_models?.Count() ?? 0)</MudText>
                <MudIconButton Icon="@Icons.Material.Filled.Add" 
                              OnClick="@OpenAddModelDialog" />
            </div>
        </TitleContent>
        <ChildContent>
            <MudTable Items="_models">
                <!-- Model rows with inline edit/delete -->
            </MudTable>
        </ChildContent>
    </MudExpansionPanel>
}
```

**Benefits:**
- User stays in context (Brand drawer)
- Immediate feedback on related entities
- Efficient workflow for managing relationships

---

### Example 4: Complex Entity with Multiple Relationships (Product)

**Reference:** `ProductManagement.razor` + `ProductDrawer.razor`

Complex entity with multiple one-to-many relationships (Suppliers).

**Key Features:**
- Similar nested management pattern as Brand
- Expansion panel for Suppliers
- Rich supplier information (name, code, cost, lead time)
- Preferred supplier indicator
- Dialog-based management for complex child entities

**Drawer Fields:**
- Name (required)
- Code (required)
- Description (optional, multi-line)
- Suppliers section (in Edit mode):
  - Supplier name
  - Supplier product code
  - Unit cost with currency
  - Lead time in days
  - Preferred flag
  - Actions (Edit, Delete)

---

### Example 5: Entity with Properties/Flags (Warehouse)

**Reference:** `WarehouseManagement.razor` + `StorageFacilityDrawer.razor`
```razor
@page "/management/warehouses"
@inject IWarehouseService WarehouseService
@* ... *@

<MudDataGrid T="StorageFacilityDto" Items="@_filteredWarehouses">
    <Columns>
        <PropertyColumn Property="x => x.Name" />
        <PropertyColumn Property="x => x.Code" />
        <TemplateColumn Title="Proprietà">
            <CellTemplate>
                <div class="d-flex gap-1">
                    @if (context.Item.IsFiscal)
                    {
                        <MudChip Size="Size.Small" Color="Color.Info">Fiscale</MudChip>
                    }
                    @if (context.Item.IsRefrigerated)
                    {
                        <MudChip Size="Size.Small" Color="Color.Tertiary">Refrigerato</MudChip>
                    }
                </div>
            </CellTemplate>
        </TemplateColumn>
        <PropertyColumn Property="x => x.TotalLocations" Title="Ubicazioni" />
        <TemplateColumn Title="Azioni">
            <CellTemplate>
                <ActionButtonGroup EntityName="@context.Item.Name"
                                 OnView="@(() => ViewWarehouse(context.Item))"
                                 OnEdit="@(() => EditWarehouse(context.Item))"
                                 OnDelete="@(() => DeleteWarehouse(context.Item))" />
            </CellTemplate>
        </TemplateColumn>
    </Columns>
</MudDataGrid>
```

---

## Common Patterns

### Pattern 1: MudTable with Sorting
```razor
<MudTable T="MyEntityDto" 
          Items="_filteredEntities" 
          Hover="true" 
          Striped="true"
          FixedHeader="true"
          Height="60vh">
    <HeaderContent>
        <MudTh>
            <MudTableSortLabel SortBy="new Func<MyEntityDto, object>(x => x.Name)">
                @TranslationService.GetTranslation("field.name", "Nome")
            </MudTableSortLabel>
        </MudTh>
        <MudTh>
            <MudTableSortLabel SortBy="new Func<MyEntityDto, object>(x => x.CreatedAt)">
                @TranslationService.GetTranslation("field.createdAt", "Creato il")
            </MudTableSortLabel>
        </MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="Nome">@context.Name</MudTd>
        <MudTd DataLabel="Creato il">@context.CreatedAt.ToString("dd/MM/yyyy")</MudTd>
    </RowTemplate>
</MudTable>
```

### Pattern 2: @bind-Value:after for Search
Modern approach for handling search changes:
```razor
<MudTextField @bind-Value="_searchTerm"
              @bind-Value:after="OnSearchChanged"
              Label="Cerca"
              Clearable="true" />

@code {
    private string _searchTerm = string.Empty;
    
    private void OnSearchChanged()
    {
        ApplyFilters();
        StateHasChanged();
    }
}
```

### Pattern 3: Filter Application
```csharp
private void ApplyFilters()
{
    _filteredEntities = _entities;
    
    if (!string.IsNullOrWhiteSpace(_searchTerm))
    {
        var searchLower = _searchTerm.ToLower();
        _filteredEntities = _filteredEntities.Where(e => 
            (e.Name?.ToLower().Contains(searchLower) ?? false) ||
            (e.Description?.ToLower().Contains(searchLower) ?? false));
    }
}
```

### Pattern 4: Confirmation Dialog
```csharp
private async Task DeleteEntity(MyEntityDto entity)
{
    var confirmed = await DialogService.ShowMessageBox(
        TranslationService.GetTranslation("action.confirm", "Conferma"),
        TranslationService.GetTranslation("myEntity.confirmDelete", 
            $"Sei sicuro di voler eliminare '{entity.Name}'?"),
        yesText: TranslationService.GetTranslation("action.delete", "Elimina"),
        cancelText: TranslationService.GetTranslation("action.cancel", "Annulla"));

    if (confirmed == true)
    {
        await MyEntityService.DeleteEntityAsync(entity.Id);
        Snackbar.Add("Eliminato con successo", Severity.Success);
        await LoadEntitiesAsync();
    }
}
```

### Pattern 5: Loading States
```razor
@if (_isLoading)
{
    <MudProgressCircular Color="Color.Primary" Indeterminate="true" />
}
else if (_isLoadingEntities)
{
    <MudProgressLinear Color="Color.Primary" Indeterminate="true" />
}
else
{
    <MudTable Items="@_filteredEntities" />
}
```

**Best Practice:** Use separate loading states for initial load and refresh operations.

### Pattern 6: ActionButtonGroup Usage
```razor
<!-- Toolbar mode (page level) -->
<ActionButtonGroup Mode="ActionButtonGroupMode.Toolbar"
                   ShowRefresh="true"
                   ShowCreate="true"
                   OnRefresh="@LoadEntitiesAsync"
                   OnCreate="@OpenCreateDrawer" />

<!-- Row mode (per entity) -->
<ActionButtonGroup EntityName="@item.Name"
                   ShowView="true"
                   ShowEdit="true"
                   ShowDelete="true"
                   ShowAuditLog="true"
                   OnView="@(() => ViewItem(item.Id))"
                   OnEdit="@(() => EditItem(item.Id))"
                   OnDelete="@(() => DeleteItem(item))"
                   OnAuditLog="@(() => ViewAuditLog(item))" />
```

### Pattern 7: Expansion Panels for Related Entities
```razor
<MudExpansionPanels>
    <MudExpansionPanel>
        <TitleContent>
            <div class="d-flex justify-space-between align-center" style="width: 100%;">
                <MudText>Related Items (@_items?.Count() ?? 0)</MudText>
                <MudIconButton Icon="@Icons.Material.Filled.Add" 
                              Color="Color.Primary" 
                              Size="Size.Small"
                              OnClick="@OpenAddDialog" />
            </div>
        </TitleContent>
        <ChildContent>
            @if (_items?.Any() == true)
            {
                <MudTable Items="_items" Dense="true" Hover="true" Striped="true">
                    <!-- Table content -->
                </MudTable>
            }
            else
            {
                <MudText Class="pa-4">Nessun elemento disponibile</MudText>
            }
        </ChildContent>
    </MudExpansionPanel>
</MudExpansionPanels>
```

### Pattern 8: Truncated Text Display
```razor
<MudTd DataLabel="Descrizione">
    @if (!string.IsNullOrWhiteSpace(context.Description))
    {
        <span>@(context.Description.Length > 50 ? context.Description.Substring(0, 50) + "..." : context.Description)</span>
    }
    else
    {
        <span class="mud-text-secondary">-</span>
    }
</MudTd>
```

### Pattern 9: Helper Text with ARIA Support
```razor
<MudTextField @bind-Value="_model.Name"
              Label="Nome *"
              Required="true"
              aria-describedby="name-help" />
<MudText id="name-help" Typo="Typo.caption" Class="mud-input-helper-text">
    @TranslationService.GetTranslation("drawer.helperText.nome", "Inserisci il nome")
</MudText>
```

**Key Benefits:**
- Accessibility compliance (WCAG/EAA)
- Clear guidance for users
- Consistent styling across all forms

---

## Checklist

### Management Page Checklist

- [ ] Page route defined (`@page "/management/my-entities"`)
- [ ] Authorization attribute (`@attribute [Authorize]`)
- [ ] All required services injected
- [ ] PageTitle set with translation
- [ ] Search field with debounce
- [ ] Appropriate filters (switches, dropdowns)
- [ ] Refresh button
- [ ] Create button
- [ ] MudDataGrid with appropriate columns
- [ ] ActionButtonGroup in actions column
- [ ] NoRecordsContent with clear filters option
- [ ] Drawer component referenced
- [ ] OnEntitySaved callback handled
- [ ] Error handling with try-catch
- [ ] Snackbar notifications
- [ ] All translations added

### Drawer Checklist

- [ ] Three modes defined (Create, Edit, View)
- [ ] Public methods for opening (OpenForCreate, OpenForEdit, OpenForView)
- [ ] OnEntitySaved callback parameter
- [ ] Form validation with MudForm
- [ ] All required fields marked as required
- [ ] ReadOnly bound to mode
- [ ] Loading state handled
- [ ] Save button disabled appropriately
- [ ] Close button present
- [ ] DTO mapping for Create and Update
- [ ] Error handling with try-catch
- [ ] Snackbar notifications
- [ ] All translations added
- [ ] Helper text for fields

### Service Checklist

- [ ] Interface defined
- [ ] Implementation uses IHttpClientService
- [ ] All CRUD operations implemented
- [ ] Proper error handling
- [ ] Logging for errors
- [ ] Registered in Program.cs
- [ ] Server endpoints verified
- [ ] DTOs correctly used

### Translations Checklist

- [ ] Navigation translation
- [ ] Page title and description
- [ ] Search placeholder
- [ ] All field labels
- [ ] All helper texts
- [ ] All status labels
- [ ] All action labels
- [ ] Success messages
- [ ] Error messages
- [ ] Confirmation messages
- [ ] No records messages

---

## Summary

### Key Principles

1. **Consistency**: Follow the same patterns across all pages
2. **Reusability**: Use shared components (ActionButtonGroup, SuperAdminPageLayout)
3. **User Experience**: Clear feedback, loading states, error handling
4. **Localization**: All text translatable
5. **Maintainability**: Clean code, proper separation of concerns

### Remember

- Always use `IHttpClientService` for API calls
- Always use `ActionButtonGroup` for table actions
- Always provide translations for all text
- Always handle loading and error states
- Always validate forms before saving
- Always provide user feedback (Snackbar)
- Always log errors for debugging

### Reference Pages

**Recent implementations (January 2025) - Follow these patterns:**
- ✅ **BrandManagement.razor** + **BrandDrawer.razor** - Parent entity with nested child management
- ✅ **ModelManagement.razor** + **ModelDrawer.razor** - Child entity with parent selection (autocomplete)
- ✅ **ProductManagement.razor** + **ProductDrawer.razor** - Complex entity with multiple relationships
- ✅ **VatRateManagement.razor** + **VatRateDrawer.razor** - Simple entity with status and dates

**Earlier implementations - Still valid but older patterns:**
- ✅ `SupplierManagement.razor` + `BusinessPartyDrawer.razor` - Complex entity with addresses/contacts
- ✅ `WarehouseManagement.razor` + `StorageFacilityDrawer.razor` - Entity with properties and locations
- ✅ `ClassificationNodeManagement.razor` - Hierarchical entities

**Key Differentiator:**
Recent implementations use the **EntityDrawer base component** with `<FormContent>` and `<ViewContent>` sections, providing more consistent behavior and better code reuse.

---

For more information, see:
- [Service Creation Guide](./SERVICE_CREATION_GUIDE.md)
- [Drawer Implementation Guide](./DRAWER_IMPLEMENTATION_GUIDE.md)
- [HttpClient Best Practices](./HTTPCLIENT_BEST_PRACTICES.md)
