# Pattern EntityManagementPage

## 1. Introduzione

`EntityManagementPage<TEntity>` è un componente Blazor generico che implementa il pattern standard per le pagine di gestione delle entità in EventForge. Fornisce out-of-the-box:

- Intestazione pagina con titolo, icona e breadcrumb
- Tabella dati (`EFTable`) con ricerca, ordinamento, paginazione, selezione multipla e export Excel
- Quick Filters per filtraggio rapido sull'intero dataset
- Bottoni di azione per riga (Edit, Delete, View, AuditLog, ToggleStatus) con gestione permessi
- Conferma eliminazione singola e bulk
- Gestione degli stati di caricamento con overlay
- Gestione errori e notifiche snackbar
- Integrazione con il sistema di autenticazione

**Quando usarlo:** per qualsiasi pagina di gestione di un'entità CRUD semplice o semi-complessa (VatRate, Brand, UnitOfMeasure, Operator, ecc.). Il pattern è pensato per entità che hanno una lista e una pagina di dettaglio/modifica separata.

**Quando NON usarlo:** vedere la sezione 8.

---

## 2. Struttura Minima — Wrapper Example

```razor
@page "/management/my-entities"
@inject IMyEntityService MyEntityService
@inject ITranslationService TranslationService

<EntityManagementPage TEntity="MyEntityDto"
                      Config="_config"
                      Service="_service">
    <HeaderContent Context="cols">
        <EFTableColumnHeader ... />
    </HeaderContent>
    <RowTemplate Context="ctx">
        <MudTd>@ctx.Item.Name</MudTd>
    </RowTemplate>
</EntityManagementPage>

@code {
    private MyEntityManagementService _service = null!;
    private EntityManagementConfig<MyEntityDto> _config = null!;

    protected override void OnInitialized()
    {
        _service = new MyEntityManagementService(MyEntityService);
        _config = new EntityManagementConfig<MyEntityDto>
        {
            ComponentKey = "my-entities",
            PageTitleKey = "myEntity.pageTitle",
            PageTitleDefault = "My Entities",
            EntityIcon = Icons.Material.Outlined.Category,
            PageRootCssClass = "management-page-root",
            BaseRoute = "/management/my-entities",
            EntityTypeName = "MyEntity",
            GetId = e => e.Id,
            GetDisplayName = e => e.Name,
            Columns = new List<EFTableColumnConfiguration>
            {
                new() { PropertyName = "Name", IsVisible = true, IsSearchable = true, Order = 0 }
            }
        };
    }
}
```

---

## 3. Proprietà di `EntityManagementConfig<TEntity>`

| Proprietà | Tipo | Default | Descrizione |
|---|---|---|---|
| `ComponentKey` | `string` (required) | — | Chiave univoca del componente per il salvataggio della configurazione colonne |
| `PageTitleKey` | `string` (required) | — | Chiave i18n per il titolo della pagina |
| `PageTitleDefault` | `string` (required) | — | Titolo di fallback se la chiave i18n non è disponibile |
| `EntityIcon` | `string` (required) | — | Icona MudBlazor per l'intestazione e il placeholder "no records" |
| `PageRootCssClass` | `string` (required) | — | Classe CSS radice del contenitore pagina (usare `"management-page-root"` come standard) |
| `BaseRoute` | `string` (required) | — | Rotta base per navigazione (es. `"/management/vat-rates"`). Usata per `{BaseRoute}/new` e `{BaseRoute}/{id}` |
| `EntityTypeName` | `string` (required) | — | Nome visualizzato del tipo entità per i log di errore (es. `"VatRate"`) |
| `GetId` | `Func<TEntity, Guid>` (required) | — | Selettore dell'ID dell'entità |
| `GetDisplayName` | `Func<TEntity, string>` (required) | — | Selettore del nome visualizzato dell'entità (usato in tooltip, messaggi di conferma) |
| `Columns` | `List<EFTableColumnConfiguration>` (required) | — | Configurazione delle colonne della tabella |
| `BreadcrumbItems` | `List<BreadcrumbItem>?` | `null` | Breadcrumb mostrato nell'intestazione. Sovrascrivibile tramite il parametro `BreadcrumbItems` del componente |
| `SearchPlaceholderKey` | `string` | `"common.search"` | Chiave i18n per il placeholder della ricerca |
| `SearchPlaceholderDefault` | `string` | `"Cerca..."` | Placeholder di fallback per la ricerca |
| `ExcelFileName` | `string` | `"Export"` | Nome del file Excel esportato |
| `ShowExport` | `bool` | `true` | Mostra il bottone di export |
| `DefaultPageSize` | `int` | `20` | Dimensione di pagina predefinita |
| `QuickFilters` | `List<QuickFilter<TEntity>>` | `[]` | Lista di quick filter visualizzati sopra la tabella |
| `ShowEdit` | `bool` | `true` | Mostra il bottone Edit per ogni riga |
| `ShowDelete` | `bool` | `true` | Mostra il bottone Delete per ogni riga |
| `ShowAuditLog` | `bool` | `true` | Mostra il bottone AuditLog per ogni riga |
| `ShowToggleStatus` | `bool` | `false` | Mostra il bottone ToggleStatus per ogni riga |
| `ShowView` | `bool` | `false` | Mostra il bottone View per ogni riga (placeholder per Fase 3) |
| `GetStatusColor` | `Func<TEntity, Color>?` | `null` | Selettore del colore dello status chip nell'intestazione |
| `GetStatusIcon` | `Func<TEntity, string>?` | `null` | Selettore dell'icona dello status chip |
| `GetStatusText` | `Func<TEntity, string>?` | `null` | Selettore del testo dello status chip |
| `GetIsActive` | `Func<TEntity, bool>?` | `null` | Selettore dello stato attivo/inattivo (usato da ToggleStatus e per l'icona del bottone) |
| `CanDelete` | `Func<TEntity, bool>?` | `null` | Predicato opzionale per bloccare l'eliminazione di entità specifiche. Quando restituisce `false`, l'eliminazione è bloccata con un messaggio di warning **e il bottone Delete è disabilitato visivamente** |
| `CanEdit` | `Func<TEntity, bool>?` | `null` | Predicato opzionale per disabilitare l'edit di entità specifiche. Quando restituisce `false`, il bottone Edit è disabilitato visivamente. Non blocca la navigazione server-side |
| `CannotDeleteMessageKey` | `string` | `"common.cannotDelete"` | Chiave i18n per il messaggio di warning quando `CanDelete` restituisce `false` |
| `EditTooltipKey` | `string` | `"common.edit"` | Chiave i18n per il tooltip del bottone Edit |
| `DeleteTooltipKey` | `string` | `"common.delete"` | Chiave i18n per il tooltip del bottone Delete |
| `ViewTooltipKey` | `string` | `"common.view"` | Chiave i18n per il tooltip del bottone View |
| `AuditLogTooltipKey` | `string` | `"common.auditLog"` | Chiave i18n per il tooltip del bottone AuditLog |
| `CustomLoadingContent` | `RenderFragment?` | `null` | Override del contenuto di loading. Quando `null`, usa il `PageLoadingOverlay` standard |
| `CreateTooltip` | `string` | `"common.createNew"` | Testo del tooltip del bottone Crea (stringa diretta, non chiave i18n) |
| `DeleteConfirmMessageKey` | `string` | `"common.confirmDelete"` | Chiave i18n per il messaggio di conferma eliminazione |
| `DeleteSuccessMessageKey` | `string` | `"common.deleteSuccess"` | Chiave i18n per il messaggio di successo eliminazione |
| `DeleteErrorMessageKey` | `string` | `"common.deleteError"` | Chiave i18n per il messaggio di errore eliminazione |
| `BulkDeleteConfirmMessageKey` | `string` | `"common.confirmBulkDelete"` | Chiave i18n per la conferma eliminazione bulk |
| `BulkDeleteSuccessMessageKey` | `string` | `"common.bulkDeleteSuccess"` | Chiave i18n per il successo eliminazione bulk |
| `LoadErrorMessageKey` | `string` | `"common.loadError"` | Chiave i18n per gli errori di caricamento |

---

## 4. Parametri di `EntityManagementPage<TEntity>`

| Parametro | Tipo | Descrizione |
|---|---|---|
| `Config` | `EntityManagementConfig<TEntity>` (required) | Configurazione completa della pagina |
| `Service` | `IEntityManagementService<TEntity>` (required) | Adapter del servizio per operazioni CRUD |
| `PageHeader` | `RenderFragment?` | Override completo dell'intestazione pagina. Quando fornito, sostituisce il `ManagementPageHeader` predefinito |
| `BreadcrumbItems` | `List<BreadcrumbItem>?` | Breadcrumb iniettato dall'esterno, ha priorità su `Config.BreadcrumbItems` |
| `HeaderContent` | `RenderFragment<List<EFTableColumnConfiguration>>?` | Slot per le intestazioni delle colonne della tabella. Riceve la lista di `EFTableColumnConfiguration` visibili |
| `RowTemplate` | `RenderFragment<(TEntity Item, List<EFTableColumnConfiguration> Cols)>?` | Slot per il contenuto delle celle della riga. Riceve l'entità e le colonne visibili |
| `BelowTableContent` | `RenderFragment?` | Slot per contenuto aggiuntivo sotto la tabella (es. pannelli informativi, legenda) |
| `AdditionalRowActions` | `RenderFragment<TEntity>?` | Slot per azioni aggiuntive per riga iniettate dopo i bottoni standard di `ActionButtonGroup` (es. bottone "Duplica", "Esporta singolo") |

---

## 5. Esempi di Override

### 5.1 — `CanDelete`: bloccare eliminazione su entità in uso

```csharp
_config = new EntityManagementConfig<VatRateDto>
{
    // ...
    CanDelete = vr => !vr.IsInUse,
    CannotDeleteMessageKey = "vatRate.cannotDeleteInUse",
};
```

Quando `CanDelete` restituisce `false`:
- Il bottone Delete è **disabilitato visivamente** nella riga
- Se l'eliminazione viene tentata comunque (es. via bulk delete), appare il messaggio di warning

### 5.2 — `CanEdit`: disabilitare edit su entità readonly

```csharp
_config = new EntityManagementConfig<DocumentTypeDto>
{
    // ...
    CanEdit = dt => !dt.IsSystem,
};
```

Quando `CanEdit` restituisce `false`:
- Il bottone Edit è **disabilitato visivamente** nella riga
- La navigazione verso la pagina di dettaglio rimane comunque possibile tramite altri mezzi
- Il blocco server-side è responsabilità del service/controller

### 5.3 — `AdditionalRowActions`: aggiungere un bottone custom per riga

```razor
<EntityManagementPage TEntity="PriceListDto"
                      Config="_config"
                      Service="_service">
    <AdditionalRowActions Context="item">
        <MudTooltip Text="Duplica">
            <MudIconButton Icon="@Icons.Material.Outlined.ContentCopy"
                           OnClick="@(() => DuplicatePriceList(item.Id))"
                           Class="ef-row-actionbutton" />
        </MudTooltip>
    </AdditionalRowActions>
</EntityManagementPage>
```

### 5.4 — `BelowTableContent`: contenuto sotto la tabella

```razor
<EntityManagementPage TEntity="VatRateDto"
                      Config="_config"
                      Service="_service">
    <BelowTableContent>
        <MudAlert Severity="Severity.Info" Class="mt-2">
            Le aliquote IVA contrassegnate come "in uso" non possono essere eliminate.
        </MudAlert>
    </BelowTableContent>
</EntityManagementPage>
```

### 5.5 — `AdditionalToolbarContent` (via EFTable): filtri aggiuntivi in toolbar

Per aggiungere filtri o azioni nella toolbar della tabella, usare il componente `EFTable` direttamente tramite il parametro `AdditionalToolbarContent`. Poiché `EntityManagementPage` non espone direttamente questo slot, le pagine più complesse con toolbar ricca possono:

1. Usare `AdditionalRowActions` per azioni per-riga
2. Usare `BelowTableContent` per filtri aggiuntivi sotto la tabella
3. Per toolbar completamente custom, considerare di non usare `EntityManagementPage` e costruire la pagina direttamente con `EFTable`

---

## 6. Pattern QuickFilters

I `QuickFilter<TEntity>` vengono visualizzati come chip cliccabili sopra la tabella e filtrano il dataset in memoria.

```csharp
_config = new EntityManagementConfig<VatRateDto>
{
    // ...
    QuickFilters = new List<QuickFilter<VatRateDto>>
    {
        new()
        {
            LabelKey = "common.all",
            LabelDefault = "Tutti",
            Predicate = null // null = nessun filtro, mostra tutti
        },
        new()
        {
            LabelKey = "common.active",
            LabelDefault = "Attivi",
            Predicate = e => e.IsActive
        },
        new()
        {
            LabelKey = "common.inactive",
            LabelDefault = "Inattivi",
            Predicate = e => !e.IsActive
        }
    }
};
```

**Comportamento:**
- Un solo quick filter è attivo alla volta
- I quick filter si combinano con la ricerca testuale
- Il filtro attivo viene evidenziato visivamente
- `ShowCount = true` mostra il numero di elementi per ogni filtro

---

## 7. Pattern Colonne

Le colonne sono configurate tramite `EFTableColumnConfiguration`:

```csharp
Columns = new List<EFTableColumnConfiguration>
{
    new()
    {
        PropertyName = "Code",       // Nome della proprietà (usato per ricerca)
        IsVisible = true,            // Visibile per default
        IsSearchable = true,         // Inclusa nella ricerca testuale
        Order = 0,                   // Ordine di visualizzazione
    },
    new()
    {
        PropertyName = "Description",
        IsVisible = true,
        IsSearchable = true,
        Order = 1,
    },
    new()
    {
        PropertyName = "Rate",
        IsVisible = true,
        IsSearchable = false,        // I numeri di solito non si cercano
        Order = 2,
    },
    new()
    {
        PropertyName = "IsActive",
        IsVisible = false,           // Nascosta per default ma configurabile dall'utente
        IsSearchable = false,
        Order = 3,
    }
}
```

**Note:**
- `IsSearchable = true` include la proprietà nella ricerca testuale full-text del componente
- L'utente può mostrare/nascondere le colonne tramite il pannello di configurazione colonne di `EFTable`
- L'ordine delle colonne è persistito nel `ComponentKey`
- Il drag-drop per il raggruppamento funziona automaticamente quando `AllowDragDropGrouping = true`

---

## 8. Casi Non Supportati

**NON usare `EntityManagementPage` quando:**

| Caso | Soluzione alternativa |
|---|---|
| Entità con tab multipli nella pagina di lista (es. ordini con tab "In corso" / "Completati") | Costruire la pagina direttamente con `EFTable` e gestire i tab manualmente |
| Workflow multi-step (es. approvazione, spedizione) con stati complessi | Pagina custom con `EFTable` e gestione stati dedicata |
| Pagine con due o più tabelle distinte in parallelo | Pagina custom con due istanze di `EFTable` |
| Gerarchie ad albero (es. `ClassificationNodeManagement` in tree view) | Componente tree custom — `EntityManagementPage` supporta solo lista piatta |
| Pagine con azioni bulk specifiche di dominio (es. attiva N listini, genera prezzi da template) | Usare `AdditionalToolbarContent` di `EFTable` direttamente, o pagina custom |
| Entità con visualizzazione inline detail/drawer (Fase 3) | In attesa di implementazione Fase 3 — per ora usare navigazione standard |

---

## 9. Roadmap — Fase 3

Le seguenti funzionalità sono pianificate per la Fase 3 del refactoring:

- **Tab custom per riga**: slot `InlineDetailContent` per aprire un pannello/drawer inline al click sulla riga (in alternativa alla navigazione)
- **`ViewEntity`**: implementazione del bottone View per aprire un dettaglio read-only
- **`RowClickBehavior`**: enum per configurare il comportamento al click sulla riga (`None`, `SelectRow`, `OpenInlineDetail`)
- **`GetDetailRoute`**: override della rotta di dettaglio per casi in cui la destinazione è diversa da `{BaseRoute}/{id}`

Fino all'implementazione della Fase 3, il comportamento al click sulla riga è disabilitato per default (nessuna navigazione automatica).

---

## 10. Estensioni Hook e Server-Side Paging

### Nuovi parametri `[Parameter]`

| Parametro | Tipo | Default | Descrizione |
|---|---|---|---|
| `OnCustomLoad` | `Func<Task>?` | `null` | Sostituisce la chiamata standard a `Service.GetPagedAsync`. Utile per pagine che caricano dati da più servizi. |
| `OnAfterInitialized` | `Func<Task>?` | `null` | Callback invocato dopo `OnInitializedAsync`. Utile per logica post-init (es. calcoli di riconciliazione). |
| `OnQuickFilterChanged` | `Func<QuickFilter<TEntity>?, Task>?` | `null` | Sostituisce il filtro client-side quando un QuickFilter viene selezionato. Utile per filtri server-side. |
| `OnClearFilters` | `Func<Task>?` | `null` | Sostituisce il comportamento di `ClearFilters`. Utile per reset server-side. |
| `IsEmbedded` | `bool` | `true` | Quando `false`, redirige a `EmbeddedRedirectUrl`. |
| `EmbeddedRedirectUrl` | `string?` | `null` | URL di redirect quando `IsEmbedded = false`. |
| `EFTableRef` | `EFTable<TEntity>?` | (computed) | Espone il riferimento interno alla tabella per manipolazioni avanzate delle colonne. |

### Server-Side Paging

Aggiungere `UseServerSidePaging = true` nella `EntityManagementConfig` per delegare il paging al server:

```csharp
_config = new EntityManagementConfig<MyEntityDto>
{
    // ...
    UseServerSidePaging = true,
    DefaultPageSize = 50,
    ShowLoadingProgressLog = true,
    LoadingProgressMessages = new[] { "Caricamento dati...", "Applicazione filtri...", "Preparazione tabella..." },
    ShowLoadingElapsedTime = true,
};
```

In modalità server-side, `LoadEntitiesAsync` passa `searchTerm`, `DefaultPageSize` e il token di cancellazione a `GetPagedAsync`. Gli adapter esistenti ignorano i nuovi parametri (backward compatible).

### Pagine Complesse con `OnCustomLoad`

Per pagine che caricano dati da più servizi (es. `StockOverview`):

```razor
<EntityManagementPage TEntity="StockLocationDetail"
                      Config="_config"
                      Service="_service"
                      OnCustomLoad="@LoadAllDataAsync"
                      OnAfterInitialized="@CalculateReconciliationAsync"
                      IsEmbedded="@IsEmbedded"
                      EmbeddedRedirectUrl="/warehouse/stock-management"
                      OnQuickFilterChanged="@HandleQuickFilterServerSide"
                      OnClearFilters="@ClearFiltersAndReload">
    ...
</EntityManagementPage>
```
