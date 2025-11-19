# Implementazione Drag & Drop Grouping per EFTable

## Panoramica

Il componente EFTable ora supporta il **raggruppamento delle colonne tramite drag & drop**, una funzionalità che permette agli utenti di trascinare le intestazioni delle colonne in una zona di raggruppamento visibile sopra la tabella.

## Come Funziona

### 1. Pannello di Raggruppamento

Sopra la tabella appare un pannello con bordo tratteggiato che indica dove trascinare le colonne:

```
┌─────────────────────────────────────────────────┐
│  🔀 Trascina qui un'intestazione di colonna     │
│     per raggruppare                              │
└─────────────────────────────────────────────────┘
```

Quando una colonna è raggruppata, il pannello mostra un chip con il nome della colonna e un pulsante per rimuovere il raggruppamento:

```
┌─────────────────────────────────────────────────┐
│  📁 Nome   [X]                                  │
└─────────────────────────────────────────────────┘
```

### 2. Intestazioni Colonne Draggable

Le intestazioni delle colonne sono rese draggable utilizzando il componente `EFTableColumnHeader`:

```razor
<EFTableColumnHeader TItem="VatRateDto" 
                     PropertyName="Name" 
                     OnDragStartCallback="@_efTable.HandleColumnDragStart">
    <MudTableSortLabel SortBy="@(new Func<VatRateDto, object>(x => x.Name))">
        Nome
    </MudTableSortLabel>
</EFTableColumnHeader>
```

Quando l'utente inizia a trascinare un'intestazione:
- Il cursore cambia in "grab"
- Il nome della proprietà viene memorizzato
- Il pannello di raggruppamento si attiva per ricevere il drop

### 3. Gestione del Drop

Quando l'utente rilascia l'intestazione sul pannello di raggruppamento:
1. La colonna viene impostata come colonna di raggruppamento
2. I dati vengono automaticamente raggruppati client-side
3. Le preferenze vengono salvate in localStorage
4. La tabella viene ri-renderizzata con i gruppi visibili

### 4. Rendering dei Gruppi

Quando i dati sono raggruppati, la tabella mostra:
- Una riga di intestazione del gruppo con sfondo diverso
- Il valore del gruppo (es. "Attivo", "Sospeso")
- Il conteggio degli elementi nel gruppo

```
┌─────────────────────────────────────────────────┐
│ 📁 Attivo  [5]                                   │
├─────────────────────────────────────────────────┤
│  Aliquota IVA 22%                                │
│  Aliquota IVA 10%                                │
│  ...                                             │
├─────────────────────────────────────────────────┤
│ 📁 Sospeso  [2]                                  │
├─────────────────────────────────────────────────┤
│  Aliquota IVA 4%                                 │
│  ...                                             │
└─────────────────────────────────────────────────┘
```

## Componenti Tecnici

### EFTable.razor

Il componente principale gestisce:
- Rendering del pannello di raggruppamento
- Stato del grouping (`_groupByProperty`)
- Eventi drag & drop
- Persistenza delle preferenze

Parametri chiave:
```csharp
[Parameter] public bool AllowDragDropGrouping { get; set; } = true;
```

Metodi chiave:
```csharp
public void HandleColumnDragStart(string propertyName) // Chiamato quando inizia il drag
private void HandleGroupPanelDragOver(DragEventArgs e)  // Permette il drop
private async Task HandleGroupPanelDrop(DragEventArgs e) // Gestisce il drop
private async Task RemoveGrouping()                     // Rimuove il grouping
```

### EFTableColumnHeader.razor

Componente helper per rendere le intestazioni draggable:
```razor
@typeparam TItem

<MudTh draggable="@IsDraggable"
       @ondragstart="@OnDragStart"
       Style="@_cursorStyle">
    @ChildContent
</MudTh>
```

Parametri:
- `TItem`: Tipo generico dell'item
- `PropertyName`: Nome della proprietà della colonna
- `IsDraggable`: Se la colonna può essere trascinata (default: true)
- `OnDragStartCallback`: Callback quando inizia il drag

### TablePreferences

Le preferenze salvate in localStorage includono ora:
```csharp
public class TablePreferences
{
    public Dictionary<string, int> ColumnOrders { get; set; }
    public Dictionary<string, bool> ColumnVisibility { get; set; }
    public string? GroupByProperty { get; set; }  // ← Nuova proprietà
}
```

## Limitazioni

1. **Solo Client-Side**: Il drag & drop grouping funziona solo con dati client-side (parametro `Items`). Non è disponibile con `ServerData` perché il raggruppamento richiede tutti i dati in memoria.

2. **Una Colonna alla Volta**: Al momento supporta solo il raggruppamento per una singola colonna. Il raggruppamento multi-livello potrebbe essere aggiunto in futuro.

3. **HTML5 Drag & Drop**: Utilizza le API HTML5 drag & drop, quindi funziona solo nei browser moderni.

## Utilizzo nell'Applicazione

### VatRateManagement.razor

Esempio di utilizzo completo:

```razor
<EFTable @ref="_efTable"
         T="VatRateDto"
         Items="_filteredVatRates"
         ComponentKey="VatRateManagement"
         InitialColumnConfigurations="_initialColumns"
         AllowDragDropGrouping="true">
    <HeaderContent Context="columnsContext">
        @foreach (var column in columnsContext.Where(c => c.IsVisible).OrderBy(c => c.Order))
        {
            <EFTableColumnHeader TItem="VatRateDto" 
                               PropertyName="@column.PropertyName" 
                               OnDragStartCallback="@_efTable.HandleColumnDragStart">
                <MudTableSortLabel>@column.DisplayName</MudTableSortLabel>
            </EFTableColumnHeader>
        }
    </HeaderContent>
    <RowTemplate>
        <!-- Template delle righe -->
    </RowTemplate>
</EFTable>
```

## Vantaggi

✅ **UX Intuitiva**: Drag & drop è un'interazione naturale e familiare  
✅ **Visuale Immediata**: Il pannello mostra chiaramente lo stato del grouping  
✅ **Persistenza**: Le preferenze vengono salvate automaticamente  
✅ **Flessibilità**: Facile aggiungere/rimuovere il raggruppamento al volo  
✅ **Performance**: Il raggruppamento avviene in memoria (client-side)  

## Sviluppi Futuri

Possibili miglioramenti:
- Supporto per raggruppamento multi-livello (drag di più colonne)
- Animazioni durante il drag & drop
- Supporto per touch devices (mobile)
- Raggruppamento server-side per grandi dataset
- Espansione/collasso dei gruppi

## Riferimenti

- [MudBlazor DataGrid Grouping](https://mudblazor.com/components/datagrid#grouping)
- [HTML5 Drag and Drop API](https://developer.mozilla.org/en-US/docs/Web/API/HTML_Drag_and_Drop_API)
- [Blazor Event Handling](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/event-handling)
