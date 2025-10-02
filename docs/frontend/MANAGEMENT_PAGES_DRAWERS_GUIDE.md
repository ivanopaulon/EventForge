# EventForge Management Pages & Drawers - Creation Guide

## Overview
This guide provides the standard patterns and best practices for creating management pages and drawer components in EventForge. Following these patterns ensures UI consistency, proper data flow, and maintainability.

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Management Page Pattern](#management-page-pattern)
3. [Drawer Component Pattern](#drawer-component-pattern)
4. [Data Flow](#data-flow)
5. [Creating a New Management Page](#creating-a-new-management-page)
6. [Creating a New Drawer](#creating-a-new-drawer)
7. [Localization](#localization)
8. [Examples](#examples)
9. [Common Patterns](#common-patterns)
10. [Checklist](#checklist)

---

## Architecture Overview

### Component Hierarchy
```
Management Page (.razor)
    ├── SuperAdminPageLayout (optional, for SuperAdmin pages)
    ├── MudDataGrid<T>
    │   ├── Toolbar (Search, Filters, Actions)
    │   ├── Columns
    │   └── Row Actions (ActionButtonGroup)
    └── EntityDrawer Component
        ├── MudDrawer
        ├── Form Fields
        └── Action Buttons
```

### Key Components

1. **Management Page**: Lists entities with search, filters, and CRUD actions
2. **Drawer Component**: Side panel for creating/editing/viewing entities
3. **Service**: Handles API communication
4. **DTOs**: Data transfer objects for communication

---

## Management Page Pattern

### File Structure
```
EventForge.Client/Pages/Management/
└── MyEntityManagement.razor
```

### Standard Template

```razor
@page "/management/my-entities"
@using EventForge.DTOs.MyEntity
@using EventForge.DTOs.Common
@using EventForge.Client.Services
@inject IMyEntityService MyEntityService
@inject ISnackbar Snackbar
@inject IDialogService DialogService
@inject ITranslationService TranslationService
@inject ILogger<MyEntityManagement> Logger
@attribute [Authorize]

<PageTitle>@TranslationService.GetTranslation("nav.myEntityManagement", "Gestione Entità")</PageTitle>

<MudContainer MaxWidth="MaxWidth.ExtraExtraLarge" Class="mt-4">
    <MudText Typo="Typo.h4" Class="mb-4">
        <MudIcon Icon="@Icons.Material.Filled.Category" Class="mr-2" />
        @TranslationService.GetTranslation("myEntity.management", "Gestione Entità")
    </MudText>
    
    <MudText Typo="Typo.body1" Class="mb-4">
        @TranslationService.GetTranslation("myEntity.managementDescription", "Gestisci le entità della tua organizzazione")
    </MudText>

    @* Search and Filters *@
    <MudPaper Class="pa-4 mb-4" Elevation="2">
        <MudGrid>
            <MudItem xs="12" sm="6" md="4">
                <MudTextField @bind-Value="_searchTerm"
                              Label="@TranslationService.GetTranslation("myEntity.search", "Cerca")"
                              Placeholder="@TranslationService.GetTranslation("myEntity.searchPlaceholder", "Inserisci nome...")"
                              Adornment="Adornment.Start"
                              AdornmentIcon="@Icons.Material.Filled.Search"
                              Immediate="true"
                              DebounceInterval="300"
                              OnDebounceIntervalElapsed="OnSearchChanged"
                              Class="mb-2" />
            </MudItem>
            <MudItem xs="12" sm="6" md="8" Class="d-flex align-center gap-2">
                <MudSwitch @bind-Value="_showActiveOnly"
                           Label="@TranslationService.GetTranslation("filter.activeOnly", "Solo attivi")"
                           Color="Color.Primary"
                           T="bool" />
            </MudItem>
        </MudGrid>
    </MudPaper>

    @* Data Grid *@
    <MudPaper Elevation="2" Class="pa-4">
        <div class="d-flex justify-space-between align-center mb-4">
            <MudText Typo="Typo.h6">
                @TranslationService.GetTranslation("myEntity.list", "Lista Entità")
                @if (_filteredEntities != null)
                {
                    <MudChip Size="Size.Small" Class="ml-2">
                        @_filteredEntities.Count() @TranslationService.GetTranslation("common.itemsFound", "elementi trovati")
                    </MudChip>
                }
            </MudText>
            
            <div class="d-flex gap-2">
                <MudButton Variant="Variant.Filled"
                           Color="Color.Primary"
                           StartIcon="@Icons.Material.Filled.Refresh"
                           OnClick="LoadEntitiesAsync"
                           Size="Size.Small">
                    @TranslationService.GetTranslation("action.refresh", "Aggiorna")
                </MudButton>
                
                <MudButton Variant="Variant.Filled"
                           Color="Color.Success"
                           StartIcon="@Icons.Material.Filled.Add"
                           OnClick="CreateNewEntity"
                           Size="Size.Small">
                    @TranslationService.GetTranslation("myEntity.createNew", "Crea nuovo")
                </MudButton>
            </div>
        </div>

        <MudDataGrid T="MyEntityDto"
                     Items="@_filteredEntities"
                     Loading="@_isLoading"
                     Hover="true"
                     Dense="true"
                     Striped="true"
                     ReadOnly="true"
                     SortMode="SortMode.Multiple">
            <Columns>
                <PropertyColumn Property="x => x.Name"
                                Title="@TranslationService.GetTranslation("field.name", "Nome")"
                                Sortable="true" />
                
                <PropertyColumn Property="x => x.Code"
                                Title="@TranslationService.GetTranslation("field.code", "Codice")"
                                Sortable="true" />
                
                <PropertyColumn Property="x => x.IsActive"
                                Title="@TranslationService.GetTranslation("field.status", "Stato")">
                    <CellTemplate>
                        @if (context.Item.IsActive)
                        {
                            <MudChip Size="Size.Small" Color="Color.Success">
                                @TranslationService.GetTranslation("status.active", "Attivo")
                            </MudChip>
                        }
                        else
                        {
                            <MudChip Size="Size.Small" Color="Color.Default">
                                @TranslationService.GetTranslation("status.inactive", "Non attivo")
                            </MudChip>
                        }
                    </CellTemplate>
                </PropertyColumn>
                
                <TemplateColumn Title="@TranslationService.GetTranslation("action.actions", "Azioni")"
                                Sortable="false">
                    <CellTemplate>
                        <ActionButtonGroup EntityName="@context.Item.Name"
                                         ItemDisplayName="@context.Item.Name"
                                         ShowView="true"
                                         ShowEdit="true"
                                         ShowDelete="true"
                                         OnView="@(() => ViewEntity(context.Item))"
                                         OnEdit="@(() => EditEntity(context.Item))"
                                         OnDelete="@(() => DeleteEntity(context.Item))" />
                    </CellTemplate>
                </TemplateColumn>
            </Columns>
            
            <NoRecordsContent>
                <div class="pa-4 text-center">
                    <MudIcon Icon="@Icons.Material.Outlined.SearchOff" Size="Size.Large" Class="mb-2" />
                    <MudText Typo="Typo.body1">
                        @TranslationService.GetTranslation("myEntity.noEntitiesFound", "Nessuna entità trovata")
                    </MudText>
                    @if (!string.IsNullOrWhiteSpace(_searchTerm) || _showActiveOnly)
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
        </MudDataGrid>
    </MudPaper>
</MudContainer>

@* Entity Drawer *@
<MyEntityDrawer @ref="_entityDrawer"
                OnEntitySaved="HandleEntitySaved" />

@code {
    private MyEntityDrawer _entityDrawer = default!;
    private IEnumerable<MyEntityDto>? _entities;
    private IEnumerable<MyEntityDto>? _filteredEntities;
    private bool _isLoading = true;
    private string _searchTerm = string.Empty;
    private bool _showActiveOnly = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadEntitiesAsync();
    }

    private async Task LoadEntitiesAsync()
    {
        try
        {
            _isLoading = true;
            StateHasChanged();

            var result = await MyEntityService.GetEntitiesAsync();
            _entities = result.Items;
            ApplyFilters();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading entities");
            Snackbar.Add(
                TranslationService.GetTranslation("myEntity.loadError", "Errore nel caricamento"),
                Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private void ApplyFilters()
    {
        if (_entities == null)
        {
            _filteredEntities = null;
            return;
        }

        _filteredEntities = _entities;

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(_searchTerm))
        {
            var searchLower = _searchTerm.ToLower();
            _filteredEntities = _filteredEntities.Where(e =>
                (e.Name?.ToLower().Contains(searchLower) ?? false) ||
                (e.Code?.ToLower().Contains(searchLower) ?? false));
        }

        // Apply active filter
        if (_showActiveOnly)
        {
            _filteredEntities = _filteredEntities.Where(e => e.IsActive);
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
        _showActiveOnly = false;
        ApplyFilters();
        StateHasChanged();
    }

    private void CreateNewEntity()
    {
        _entityDrawer.OpenForCreate();
    }

    private void ViewEntity(MyEntityDto entity)
    {
        _entityDrawer.OpenForView(entity.Id);
    }

    private void EditEntity(MyEntityDto entity)
    {
        _entityDrawer.OpenForEdit(entity.Id);
    }

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

### Example 1: Simple Entity (Categories)

#### CategoryManagement.razor
```razor
@page "/management/categories"
@inject ICategoryService CategoryService
@* ... standard setup ... *@

<MudDataGrid T="CategoryDto" Items="@_filteredCategories">
    <Columns>
        <PropertyColumn Property="x => x.Name" />
        <PropertyColumn Property="x => x.Code" />
        <TemplateColumn Title="Actions">
            <CellTemplate>
                <ActionButtonGroup EntityName="@context.Item.Name"
                                 OnView="@(() => ViewCategory(context.Item))"
                                 OnEdit="@(() => EditCategory(context.Item))"
                                 OnDelete="@(() => DeleteCategory(context.Item))" />
            </CellTemplate>
        </TemplateColumn>
    </Columns>
</MudDataGrid>

<CategoryDrawer @ref="_categoryDrawer" OnEntitySaved="HandleCategorySaved" />
```

### Example 2: Complex Entity with Relationships (Suppliers)

#### SupplierManagement.razor
```razor
@page "/management/suppliers"
@inject IBusinessPartyService BusinessPartyService
@* ... *@

<MudDataGrid T="BusinessPartyDto" Items="@_filteredSuppliers">
    <Columns>
        <PropertyColumn Property="x => x.CompanyName" Title="Ragione Sociale" />
        <PropertyColumn Property="x => x.VatNumber" Title="P.IVA" />
        <PropertyColumn Property="x => x.TaxCode" Title="C.F." />
        <PropertyColumn Property="x => x.Email" />
        <PropertyColumn Property="x => x.Phone" Title="Telefono" />
        <TemplateColumn Title="Indirizzi">
            <CellTemplate>
                <MudChip Size="Size.Small">
                    @context.Item.Addresses?.Count() ?? 0
                </MudChip>
            </CellTemplate>
        </TemplateColumn>
        <TemplateColumn Title="Contatti">
            <CellTemplate>
                <MudChip Size="Size.Small">
                    @context.Item.Contacts?.Count() ?? 0
                </MudChip>
            </CellTemplate>
        </TemplateColumn>
        <TemplateColumn Title="Azioni">
            <CellTemplate>
                <ActionButtonGroup EntityName="@context.Item.CompanyName"
                                 OnView="@(() => ViewSupplier(context.Item))"
                                 OnEdit="@(() => EditSupplier(context.Item))"
                                 OnDelete="@(() => DeleteSupplier(context.Item))" />
            </CellTemplate>
        </TemplateColumn>
    </Columns>
</MudDataGrid>

<BusinessPartyDrawer @ref="_supplierDrawer"
                     DefaultPartyType="BusinessPartyType.Supplier"
                     OnEntitySaved="HandleSupplierSaved" />
```

### Example 3: Entity with Status Indicators (Warehouses)

#### WarehouseManagement.razor
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

### Pattern 1: Debounced Search
```razor
<MudTextField @bind-Value="_searchTerm"
              Immediate="true"
              DebounceInterval="300"
              OnDebounceIntervalElapsed="OnSearchChanged" />
```

### Pattern 2: Filter Application
```csharp
private void ApplyFilters()
{
    _filteredEntities = _entities;
    
    if (!string.IsNullOrWhiteSpace(_searchTerm))
    {
        var searchLower = _searchTerm.ToLower();
        _filteredEntities = _filteredEntities.Where(e => 
            e.Name.ToLower().Contains(searchLower));
    }
    
    if (_showActiveOnly)
    {
        _filteredEntities = _filteredEntities.Where(e => e.IsActive);
    }
}
```

### Pattern 3: Confirmation Dialog
```csharp
private async Task DeleteEntity(MyEntityDto entity)
{
    var confirmed = await DialogService.ShowMessageBox(
        "Conferma",
        $"Sei sicuro di voler eliminare '{entity.Name}'?",
        yesText: "Elimina",
        cancelText: "Annulla");

    if (confirmed == true)
    {
        await MyEntityService.DeleteEntityAsync(entity.Id);
        await LoadEntitiesAsync();
    }
}
```

### Pattern 4: Loading States
```razor
@if (_isLoading)
{
    <MudProgressLinear Indeterminate="true" />
}
else
{
    <MudDataGrid Items="@_filteredEntities" />
}
```

### Pattern 5: ActionButtonGroup Usage
```razor
<ActionButtonGroup EntityName="@item.Name"
                   ItemDisplayName="@item.Name"
                   ShowView="true"
                   ShowEdit="true"
                   ShowDelete="true"
                   ShowPrint="false"
                   OnView="@(() => ViewItem(item))"
                   OnEdit="@(() => EditItem(item))"
                   OnDelete="@(() => DeleteItem(item))" />
```

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

Good examples to follow:
- ✅ `SupplierManagement.razor` + `BusinessPartyDrawer.razor` - Complex entity with relationships
- ✅ `VatRateManagement.razor` + `VatRateDrawer.razor` - Simple entity with status
- ✅ `WarehouseManagement.razor` + `StorageFacilityDrawer.razor` - Entity with properties
- ✅ `ClassificationNodeManagement.razor` - Hierarchical entities

---

For more information, see:
- [Service Creation Guide](./SERVICE_CREATION_GUIDE.md)
- [HttpClient Best Practices](./HTTPCLIENT_BEST_PRACTICES.md)
