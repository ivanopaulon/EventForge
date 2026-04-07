# Implementazione Conversione Drawer a Pagine Dedicate

## Panoramica

Questo documento fornisce le istruzioni dettagliate per completare la conversione delle rimanenti pagine di gestione da drawer a pagine dedicate, seguendo il pattern stabilito da ProductDetail.razor.

## Stato Attuale

### ✅ Completato
- **BrandManagement** → BrandDetail.razor
- **UnitOfMeasureManagement** → UnitOfMeasureDetail.razor

### 📋 Da Completare
1. **SupplierManagement** → SupplierDetail.razor (complesso - BusinessPartyDrawer)
2. **CustomerManagement** → CustomerDetail.razor (complesso - BusinessPartyDrawer)
3. **VatRateManagement** → VatRateDetail.razor
4. **VatNatureManagement** → VatNatureDetail.razor
5. **WarehouseManagement** → WarehouseDetail.razor (StorageFacilityDrawer)

## Pattern di Implementazione

### Fase 1: Creare la Pagina Detail

#### Template Base per Pagina Detail Semplice

```razor
@page "/route-base/new"
@page "/route-base/{EntityId:guid}"
@using Microsoft.AspNetCore.Authorization
@using EventForge.DTOs.[Namespace]
@using EventForge.DTOs.Common
@using EventForge.Client.Shared.Components
@attribute [Authorize]
@inject [IService] Service
@inject NavigationManager NavigationManager
@inject ISnackbar Snackbar
@inject IDialogService DialogService
@inject ITranslationService TranslationService
@inject ILogger<EntityDetail> Logger

<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    @if (_isLoading)
    {
        <MudProgressCircular Color="Color.Primary" Indeterminate="true" />
    }
    else if (_entity == null && !_isCreateMode)
    {
        <MudAlert Severity="Severity.Error">
            @TranslationService.GetTranslation("entity.notFound", "Entità non trovata")
        </MudAlert>
    }
    else
    {
        <!-- Page Header -->
        <MudPaper Elevation="2" Class="pa-4 mb-4">
            <div class="d-flex justify-space-between align-center">
                <div>
                    <div class="d-flex align-center gap-2 mb-2">
                        <MudIconButton Icon="@Icons.Material.Filled.ArrowBack" 
                                       Color="Color.Primary"
                                       OnClick="@(() => TryNavigateAway("/route-base"))"
                                       Size="Size.Small" />
                        <MudText Typo="Typo.h4">
                            <MudIcon Icon="@Icons.Material.Outlined.Icon" Class="mr-2" />
                            @(_isCreateMode 
                                ? TranslationService.GetTranslation("entity.create", "Crea") 
                                : (_entity?.Name ?? TranslationService.GetTranslation("entity.unknown", "Entità")))
                        </MudText>
                        
                        @if (HasUnsavedChanges())
                        {
                            <MudChip T="string" Size="Size.Small" Color="Color.Warning" Class="ml-2">
                                @TranslationService.GetTranslation("entity.unsavedChanges", "Modifiche non salvate")
                            </MudChip>
                        }
                    </div>
                </div>
                <div class="d-flex gap-2">
                    <MudButton Variant="Variant.Filled" 
                               Color="Color.Primary" 
                               StartIcon="@Icons.Material.Filled.Save"
                               OnClick="SaveEntityAsync"
                               Disabled="_isSaving"
                               Size="Size.Small">
                        @TranslationService.GetTranslation("common.save", "Salva")
                    </MudButton>
                </div>
            </div>
        </MudPaper>

        <!-- Form Section -->
        <MudPaper Elevation="2" Class="pa-4">
            <MudForm @ref="_form">
                <MudGrid>
                    <!-- Form fields here -->
                </MudGrid>
            </MudForm>
        </MudPaper>
    }
</MudContainer>

@code {
    [Parameter] public Guid? EntityId { get; set; }

    private EntityDto? _entity;
    private bool _isLoading = true;
    private bool _isCreateMode => EntityId == null || EntityId == Guid.Empty;
    private bool _isSaving = false;
    private MudForm? _form;

    private string _originalEntitySnapshot = string.Empty;
    private bool _hasLocalChanges = false;

    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadEntityAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_entity?.Id != EntityId)
            await LoadEntityAsync();
    }

    private async Task LoadEntityAsync()
    {
        _isLoading = true;
        try
        {
            if (_isCreateMode)
            {
                _entity = new EntityDto();
                _originalEntitySnapshot = SerializeEntity(_entity);
            }
            else if (EntityId.HasValue)
            {
                var entity = await Service.GetByIdAsync(EntityId.Value);
                _entity = entity;
                if (_entity != null)
                {
                    _originalEntitySnapshot = SerializeEntity(_entity);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading entity {EntityId}", EntityId);
            Snackbar.Add(TranslationService.GetTranslation("entity.loadError", "Errore nel caricamento"), Severity.Error);
        }
        finally
        {
            _isLoading = false;
            _hasLocalChanges = false;
        }
    }

    private async Task SaveEntityAsync()
    {
        if (_entity == null || _form == null) return;
        await _form.Validate();
        if (!_form.IsValid) return;

        _isSaving = true;
        try
        {
            if (_isCreateMode)
            {
                var createDto = new CreateEntityDto 
                { 
                    // Map properties from _entity to DTO
                    // Example: Name = _entity.Name,
                    //          Description = _entity.Description
                };
                var created = await Service.CreateAsync(createDto);
                Snackbar.Add(TranslationService.GetTranslation("entity.createSuccess", "Creato con successo"), Severity.Success);
                NavigationManager.NavigateTo($"/route-base/{created.Id}");
            }
            else if (EntityId.HasValue)
            {
                var updateDto = new UpdateEntityDto 
                { 
                    // Map properties from _entity to DTO
                    // Example: Name = _entity.Name,
                    //          Description = _entity.Description
                };
                await Service.UpdateAsync(EntityId.Value, updateDto);
                Snackbar.Add(TranslationService.GetTranslation("entity.updateSuccess", "Aggiornato con successo"), Severity.Success);
                await LoadEntityAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving entity");
            Snackbar.Add(TranslationService.GetTranslation("entity.saveError", "Errore nel salvataggio"), Severity.Error);
        }
        finally
        {
            _isSaving = false;
        }
    }

    private void MarkChanged()
    {
        _hasLocalChanges = true;
        StateHasChanged();
    }

    private string SerializeEntity(EntityDto? e)
    {
        if (e == null) return string.Empty;
        return System.Text.Json.JsonSerializer.Serialize(e, _jsonOptions);
    }

    private bool HasUnsavedChanges()
    {
        if (_entity == null) return false;
        if (_hasLocalChanges) return true;
        var current = SerializeEntity(_entity);
        return !string.Equals(current, _originalEntitySnapshot, StringComparison.Ordinal);
    }

    private async Task TryNavigateAway(string target)
    {
        if (!HasUnsavedChanges())
        {
            NavigationManager.NavigateTo(target);
            return;
        }

        var title = TranslationService.GetTranslation("common.confirm", "Conferma");
        var message = TranslationService.GetTranslation("entity.unsavedChangesConfirm", "Ci sono modifiche non salvate. Vuoi salvare prima di uscire?");
        var saveText = TranslationService.GetTranslation("common.save", "Salva");
        var discardText = TranslationService.GetTranslation("common.discard", "Non salvare");
        var cancelText = TranslationService.GetTranslation("common.cancel", "Annulla");

        var result = await DialogService.ShowMessageBox(title, message, yesText: saveText, noText: discardText, cancelText: cancelText);

        if (result == true)
        {
            await SaveEntityAsync();
            NavigationManager.NavigateTo(target);
        }
        else if (result == false)
        {
            NavigationManager.NavigateTo(target);
        }
    }
}
```

### Fase 2: Aggiornare la Pagina Management

#### Modifiche da Applicare

1. **Rimuovere il riferimento al drawer**:
```razor
<!-- RIMUOVERE -->
<EntityDrawer @ref="_entityDrawer" OnEntitySaved="HandleEntitySaved" />

@code {
    private EntityDrawer _entityDrawer = default!;  // RIMUOVERE
    private bool _entityDrawerOpen = false;         // RIMUOVERE
    private EntityDrawerMode _entityDrawerMode = EntityDrawerMode.Create;  // RIMUOVERE
    private EntityDto? _selectedEntity;             // RIMUOVERE (se usato solo per drawer)
}
```

2. **Cambiare l'azione Create**:
```csharp
// PRIMA
private void OpenCreateEntityDrawer()
{
    _entityDrawer.OpenForCreate();
}

// DOPO
private void CreateEntity()
{
    NavigationManager.NavigateTo("/route-base/new");
}
```

3. **Cambiare l'azione Edit**:
```csharp
// PRIMA
private async Task EditEntity(Guid id)
{
    await _entityDrawer.OpenForEdit(id);
}

// DOPO
private void EditEntity(Guid id)
{
    NavigationManager.NavigateTo($"/route-base/{id}");
}
```

4. **Rimuovere l'azione View** (Edit ora serve entrambi gli scopi):
```razor
<!-- PRIMA -->
<ActionButtonGroup ShowView="true"
                   ShowEdit="true"
                   OnView="@(() => ViewEntity(context.Id))"
                   OnEdit="@(() => EditEntity(context.Id))" />

<!-- DOPO -->
<ActionButtonGroup ShowView="false"
                   ShowEdit="true"
                   OnEdit="@(() => EditEntity(context.Id))" />
```

5. **Aggiornare l'ActionButtonGroup nell'header**:
```razor
<!-- PRIMA -->
OnCreate="@OpenCreateEntityDrawer"

<!-- DOPO -->
OnCreate="@CreateEntity"
```

## Dettagli Implementazione per Entità Specifiche

### VatRateDetail.razor

**Route**: `/financial/vat-rates/new` e `/financial/vat-rates/{VatRateId:guid}`

**Campi principali**:
- Name (obbligatorio)
- Percentage (0-100, obbligatorio)
- Status (Active/Suspended/Deleted)
- ValidFrom (DatePicker, opzionale)
- ValidTo (DatePicker, opzionale)
- VatNatureId (Select da lista VatNatures, opzionale)
- Notes (multilinea, opzionale)

**Riferimento**: EventForge.Client/Shared/Components/VatRateDrawer.razor (linee 1-150)

### VatNatureDetail.razor

**Route**: `/financial/vat-natures/new` e `/financial/vat-natures/{VatNatureId:guid}`

**Campi principali**:
- Code (obbligatorio, es. N1, N2, N3)
- Name (obbligatorio)
- Description (multilinea, opzionale)

**Nota**: Backend service potrebbe non essere completo (vedi TODO nel drawer originale)

**Riferimento**: EventForge.Client/Shared/Components/VatNatureDrawer.razor

### WarehouseDetail.razor (StorageFacilityDetail)

**Route**: `/warehouse/storage-facilities/new` e `/warehouse/storage-facilities/{FacilityId:guid}`

**Campi principali** (da verificare nel StorageFacilityDrawer):
- Nome magazzino
- Codice
- Tipo
- Indirizzo
- Capacità

**Riferimento**: EventForge.Client/Shared/Components/StorageFacilityDrawer.razor

### SupplierDetail.razor / CustomerDetail.razor (BusinessPartyDetail)

**Complessità**: Alta - BusinessPartyDrawer ha 1001 righe

**Route**: 
- `/business/suppliers/new` e `/business/suppliers/{PartyId:guid}`
- `/business/customers/new` e `/business/customers/{PartyId:guid}`

**Strategia consigliata**: 
1. Creare una pagina BusinessPartyDetail.razor unificata
2. Usare un parametro per distinguere il tipo (Supplier/Customer/Both)
3. Organizzare in tabs:
   - General Info (dati anagrafici)
   - Addresses (indirizzi)
   - Contacts (contatti)
   - References (riferimenti)
   - Financial (dati finanziari)

**Campi principali**:
- PartyType (Cliente/Fornitore/Entrambi)
- Name (obbligatorio)
- TaxCode, VatNumber
- SdiCode, PEC
- Notes

**Relazioni complesse**:
- Indirizzi multipli
- Contatti multipli
- Riferimenti multipli
- Prodotti forniti (per fornitori)

**Riferimento**: EventForge.Client/Shared/Components/BusinessPartyDrawer.razor

## Checklist per Ogni Conversione

- [ ] Creare il file *Detail.razor con le route appropriate
- [ ] Implementare il pattern di header con back button
- [ ] Implementare il form o tabs per i campi
- [ ] Aggiungere validazione con MudForm
- [ ] Implementare SaveEntityAsync
- [ ] Implementare il meccanismo di unsaved changes
- [ ] Implementare TryNavigateAway con dialog di conferma
- [ ] Aggiornare il file *Management.razor
- [ ] Rimuovere riferimenti al drawer
- [ ] Cambiare CreateEntity per navigare
- [ ] Cambiare EditEntity per navigare
- [ ] Rimuovere ViewEntity
- [ ] Testare build: `dotnet build`
- [ ] Testare navigazione create/edit

## Note di Testing

Dopo ogni conversione:

1. **Build**: `dotnet build` - deve compilare senza errori
2. **Navigazione Create**: Verificare che il pulsante "Crea" navighi a /route/new
3. **Navigazione Edit**: Verificare che il pulsante "Modifica" navighi a /route/{id}
4. **Save**: Verificare che il salvataggio funzioni
5. **Back Navigation**: Verificare che il pulsante indietro funzioni
6. **Unsaved Changes**: Verificare che il dialog appaia su modifiche non salvate

## Priorità di Implementazione

1. **Alta priorità** (semplici):
   - VatRateDetail.razor
   - VatNatureDetail.razor
   - WarehouseDetail.razor

2. **Media priorità** (complessi):
   - BusinessPartyDetail.razor (per Supplier e Customer)

## Supporto

Per domande o problemi durante l'implementazione:
- Riferimento pattern: BrandDetail.razor e UnitOfMeasureDetail.razor
- Riferimento originale: ProductDetail.razor
- Drawer originali in: EventForge.Client/Shared/Components/

## Esempi Completati

### BrandDetail.razor
- Route: `/product-management/brands/new` e `/product-management/brands/{BrandId:guid}`
- Tabs: General Info, Models
- Gestione modelli associati al brand

### UnitOfMeasureDetail.razor
- Route: `/settings/unit-of-measures/new` e `/settings/unit-of-measures/{UnitId:guid}`
- Form semplice con validazione
- Campo Symbol non modificabile dopo la creazione
