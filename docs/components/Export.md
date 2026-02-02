# Advanced Export System

## Overview

Sistema di export avanzato per EFTable con selezione colonne e supporto dati filtrati.

**File:** `EventForge.Client/Shared/Components/Dialogs/ExportDialog.razor`  
**Models:** `EventForge.Client/Shared/Components/Export/ExportModels.cs`  
**Versione:** 1.0 (PR #4)

## Features

✅ **Dialog interattivo per selezione colonne**  
✅ **Export su dati filtrati** (non tutti i dati)  
✅ **Supporto Excel (.xlsx) e CSV (.csv)**  
✅ **Progress indicator durante export**  
✅ **Info pre-export** (count righe, colonne)  
✅ **Default intelligente** (solo colonne visibili)

## Basic Usage

### 1. Abilitare Export in EFTable

```razor
<EFTable @ref="_efTable"
         TItem="BusinessPartyDto"
         Items="_filteredBusinessParties"
         ShowExport="true"
         ShowExportDialog="true"
         ExcelFileName="AnagraficheAziendali"
         IsDataFiltered="@HasActiveFilters()"
         TotalItemsCount="@_businessParties.Count"
         ...>
</EFTable>
```

### 2. Implementare HasActiveFilters

```csharp
private bool HasActiveFilters()
{
    return !string.IsNullOrWhiteSpace(_searchTerm) 
        || _activeQuickFilter != null
        || _selectedFilter.HasValue;
}
```

## Export Dialog

Il dialog di export mostra:

1. **Selezione Colonne**
   - Checkbox per ogni colonna visibile
   - Toggle "Tutte le colonne" per selezionare/deselezionare tutte

2. **Info Export**
   - Numero righe da esportare
   - Messaggio "Filtrate da X totali" se ci sono filtri attivi
   - Numero colonne selezionate

3. **Selezione Formato**
   - Excel (.xlsx) - Consigliato
   - CSV (.csv)

## API Reference

### EFTable Parameters

```csharp
/// <summary>
/// Se true, mostra pulsante export nella toolbar.
/// </summary>
[Parameter] public bool ShowExport { get; set; } = false;

/// <summary>
/// Se true, mostra dialog di selezione colonne prima dell'export.
/// Default: true (export avanzato).
/// </summary>
[Parameter] public bool ShowExportDialog { get; set; } = true;

/// <summary>
/// Nome file per export Excel (senza estensione).
/// </summary>
[Parameter] public string ExcelFileName { get; set; } = "Export";

/// <summary>
/// Indica se i dati visualizzati sono filtrati rispetto al totale.
/// Usato per mostrare informazioni corrette nel dialog export.
/// </summary>
[Parameter] public bool IsDataFiltered { get; set; } = false;

/// <summary>
/// Numero totale di items disponibili (prima dei filtri).
/// Usato per mostrare "X filtrate da Y totali".
/// </summary>
[Parameter] public int TotalItemsCount { get; set; } = 0;

/// <summary>
/// Callback per export avanzato con configurazione colonne.
/// Se implementato, sovrascrive la logica built-in.
/// </summary>
[Parameter] public EventCallback<ExportRequest> OnExportAdvanced { get; set; }
```

### Export Models

```csharp
// Configurazione colonna
public class ExportColumnConfig
{
    public string PropertyName { get; set; }
    public string DisplayName { get; set; }
    public bool IncludeInExport { get; set; } = true;
    public string? NumberFormat { get; set; }
    public int Order { get; set; }
}

// Formati supportati
public enum ExportFormat
{
    Excel,
    Csv
}

// Risultato dialog
public class ExportDialogResult
{
    public ExportFormat Format { get; set; }
    public List<ExportColumnConfig> Columns { get; set; }
}

// Richiesta export
public class ExportRequest
{
    public List<ExportColumnConfig> Columns { get; set; }
    public ExportFormat Format { get; set; }
    public string FileName { get; set; }
}
```

## Custom Export Handler

Per implementare logica custom:

```razor
<EFTable ShowExport="true"
         ShowExportDialog="true"
         OnExportAdvanced="@HandleExportAdvanced"
         ...>
</EFTable>

@code {
    private async Task HandleExportAdvanced(ExportRequest request)
    {
        // Logica custom per export
        var data = GetFilteredData();
        var selectedColumns = request.Columns.Where(c => c.IncludeInExport);
        
        if (request.Format == ExportFormat.Excel)
        {
            await ExportToExcel(data, selectedColumns, request.FileName);
        }
        else if (request.Format == ExportFormat.Csv)
        {
            await ExportToCsv(data, selectedColumns, request.FileName);
        }
    }
}
```

## Export su Dati Filtrati

**IMPORTANTE:** L'export lavora **SOLO sui dati filtrati** visualizzati nella tabella.

Il metodo `GetDisplayItems()` in EFTable ritorna i dati dopo:
- Ricerca testuale
- Filtri inline
- Quick filters
- Altri filtri custom

```csharp
// ✅ CORRETTO - Export dati filtrati
Items="_filteredBusinessParties"
IsDataFiltered="@HasActiveFilters()"
TotalItemsCount="@_businessParties.Count"

// ❌ SBAGLIATO - Export tutti i dati
Items="_businessParties"  // Non usa filtri!
```

## Translation Keys

### Italiano (it.json)

```json
{
  "export": {
    "dialogTitle": "Esporta dati",
    "selectColumns": "Seleziona colonne da esportare",
    "allColumns": "Tutte le colonne",
    "recordsToExport": "righe verranno esportate",
    "filteredFrom": "Filtrate da {0} righe totali",
    "selectedColumns": "{0} colonne selezionate",
    "selectFormat": "Seleziona formato",
    "recommended": "Consigliato",
    "confirm": "Esporta",
    "noData": "Nessun dato da esportare",
    "success": "Export completato con successo!",
    "failed": "Export fallito",
    "error": "Errore durante l'export: {0}",
    "inProgress": "Export in corso..."
  },
  "tooltip": {
    "export": "Esporta dati"
  }
}
```

### Inglese (en.json)

```json
{
  "export": {
    "dialogTitle": "Export data",
    "selectColumns": "Select columns to export",
    "allColumns": "All columns",
    "recordsToExport": "rows will be exported",
    "filteredFrom": "Filtered from {0} total rows",
    "selectedColumns": "{0} columns selected",
    "selectFormat": "Select format",
    "recommended": "Recommended",
    "confirm": "Export",
    "noData": "No data to export",
    "success": "Export completed successfully!",
    "failed": "Export failed",
    "error": "Error during export: {0}",
    "inProgress": "Export in progress..."
  },
  "tooltip": {
    "export": "Export data"
  }
}
```

## Best Practices

### 1. Sempre Specificare IsDataFiltered

```csharp
// ✅ CORRETTO
IsDataFiltered="@HasActiveFilters()"

private bool HasActiveFilters()
{
    return !string.IsNullOrWhiteSpace(_searchTerm) 
        || _activeFilter != null;
}
```

### 2. Fornire TotalItemsCount

```csharp
// ✅ CORRETTO
TotalItemsCount="@_allItems.Count"

// Questo permette di mostrare "150 filtrate da 500 totali"
```

### 3. Nome File Significativo

```csharp
// ✅ CORRETTO
ExcelFileName="AnagraficheAziendali"  // Descrittivo
ExcelFileName="Magazzini"
ExcelFileName="Listini"

// ❌ EVITARE
ExcelFileName="Export"  // Generico
ExcelFileName="Data"
```

### 4. Legacy Mode

Per mantenere comportamento legacy (export diretto senza dialog):

```csharp
ShowExport="true"
ShowExportDialog="false"  // Salta dialog
```

## Examples

### Esempio 1: Warehouse Management

```razor
<EFTable @ref="_efTable"
         TItem="StorageFacilityDto"
         Items="_filteredFacilities"
         ShowExport="true"
         ShowExportDialog="true"
         ExcelFileName="Magazzini"
         IsDataFiltered="@HasActiveFilters()"
         TotalItemsCount="@_storageFacilities.Count"
         ...>
</EFTable>

@code {
    private IEnumerable<StorageFacilityDto> _filteredFacilities => 
        _storageFacilities.Where(f => FilterFacility(f));
    
    private bool HasActiveFilters()
    {
        return !string.IsNullOrWhiteSpace(_searchTerm) 
            || _showOnlyFiscal 
            || _showOnlyRefrigerated
            || _activeQuickFilter != null;
    }
}
```

### Esempio 2: Custom Export Logic

```razor
<EFTable ShowExport="true"
         ShowExportDialog="true"
         OnExportAdvanced="@CustomExport"
         ...>
</EFTable>

@code {
    private async Task CustomExport(ExportRequest request)
    {
        // Custom logic: aggiungi metadati
        var metadata = new
        {
            ExportedAt = DateTime.Now,
            ExportedBy = _currentUser.Name,
            FiltersApplied = HasActiveFilters()
        };
        
        // Usa built-in con metadati extra
        await MyCustomExportService.ExportWithMetadata(
            request.Columns, 
            request.Format, 
            GetFilteredData(),
            metadata);
    }
}
```

## Troubleshooting

### Export Mostra Tutti i Dati (Non Filtrati)

**Problema:** L'export include dati che non sono visibili nella tabella.

**Soluzione:** Assicurati di passare `Items` con i dati filtrati, non tutti i dati.

```csharp
// ✅ CORRETTO
Items="_filteredBusinessParties"

// ❌ SBAGLIATO
Items="_businessParties"
```

### Dialog Non Mostra "Filtrate da X totali"

**Problema:** Il messaggio non appare anche se ci sono filtri attivi.

**Soluzione:** Imposta `IsDataFiltered="true"` e fornisci `TotalItemsCount`.

```csharp
// ✅ CORRETTO
IsDataFiltered="@HasActiveFilters()"
TotalItemsCount="@_allItems.Count"
```

### Nessuna Colonna Selezionata

**Problema:** Il dialog mostra "0 colonne selezionate".

**Soluzione:** Le colonne vengono automaticamente selezionate se sono visibili. Verifica che `InitialColumnConfigurations` abbia colonne con `IsVisible=true`.

## Technical Notes

- **CSV Encoding:** UTF-8 con BOM
- **CSV Delimiter:** Virgola (`,`)
- **CSV Escape:** Doppio quote (`""`) per quote nei valori
- **Excel Format:** `.xlsx` (OpenXML)
- **Excel Formatting:** Header colorato, alternate rows, auto-filter
- **Progress Overlay:** Modale con spinner durante export
- **File Naming:** `{ExcelFileName}_{yyyyMMdd_HHmmss}.{ext}`

## Related Documentation

- [EFTable.md](./EfTable.md) - Documentazione completa EFTable
- [QuickFilters.md](./QuickFilters.md) - Sistema Quick Filters
- [EFTABLE_STANDARD_PATTERN.md](../EFTABLE_STANDARD_PATTERN.md) - Pattern standard implementazione

## Version History

- **v1.0** (PR #4): Implementazione iniziale con selezione colonne e dati filtrati
