# Linee Guida PageLoadingOverlay

## Sommario

Il componente `PageLoadingOverlay` è un overlay di caricamento standardizzato per EventForge. Questo documento fornisce le linee guida su quando e come utilizzare correttamente il componente, pattern consigliati, esempi pratici e best practice.

## Indice

- [Quando Usare PageLoadingOverlay](#quando-usare-pageloadingoverlay)
- [Quando NON Usare PageLoadingOverlay](#quando-non-usare-pageloadingoverlay)
- [Pattern Consigliati](#pattern-consigliati)
- [Esempi di Implementazione](#esempi-di-implementazione)
- [Best Practice](#best-practice)
- [Accessibilità](#accessibilità)
- [Troubleshooting](#troubleshooting)

---

## Quando Usare PageLoadingOverlay

### ✅ Utilizzare PageLoadingOverlay per:

1. **Caricamento Iniziale della Pagina**
   - Quando la pagina sta caricando dati critici necessari per la visualizzazione
   - Durante l'inizializzazione di componenti complessi
   - Quando si attendono più chiamate API simultanee

2. **Operazioni di Salvataggio/Aggiornamento**
   - Quando si salvano modifiche che richiedono tempo (es. upload di file)
   - Durante operazioni batch che modificano più record
   - Quando l'utente deve attendere la conferma dell'operazione

3. **Operazioni di Eliminazione Multiple**
   - Quando si eliminano più elementi contemporaneamente
   - Durante operazioni che richiedono conferme dal server

4. **Transizioni di Stato Complesse**
   - Durante il cambio di contesto tenant
   - Quando si passa da una modalità all'altra (es. da modifica a visualizzazione)

---

## Quando NON Usare PageLoadingOverlay

### ❌ NON utilizzare PageLoadingOverlay per:

1. **Operazioni Rapide (< 500ms)**
   - Filtraggio locale di dati già caricati
   - Ordinamento di tabelle
   - Espansione/collasso di sezioni UI

2. **Caricamenti Parziali**
   - Caricamento di dati in background mentre l'utente può interagire
   - Aggiornamenti incrementali di liste o tabelle
   - **Alternativa**: Usare `MudProgressLinear` o indicatori localizzati

3. **Feedback Immediato**
   - Click su pulsanti che aprono dialog
   - Navigazione tra tab o sezioni
   - **Alternativa**: Usare feedback visivo sul componente stesso

4. **Operazioni Inline**
   - Modifica diretta in tabella
   - Toggle di switch/checkbox
   - **Alternativa**: Disabilitare temporaneamente il controllo

---

## Pattern Consigliati

### Pattern 1: Caricamento Iniziale della Pagina

```razor
@page "/product-management"
@inject IProductService ProductService

<PageLoadingOverlay IsVisible="_isLoading"
                    Message="@TranslationService.GetTranslation("messages.loadingPage", "Caricamento pagina...")" />

@if (!_isLoading)
{
    <!-- Contenuto della pagina -->
}

@code {
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _isLoading = true;
            await LoadDataAsync();
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }
}
```

### Pattern 2: Operazioni di Salvataggio

```razor
<PageLoadingOverlay IsVisible="_isSaving"
                    Message="@TranslationService.GetTranslation("messages.saving", "Salvataggio in corso...")" />

@code {
    private bool _isSaving = false;

    private async Task SaveAsync()
    {
        try
        {
            _isSaving = true;
            StateHasChanged();
            
            await ProductService.UpdateAsync(product);
            
            Snackbar.Add("Salvato con successo", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Errore: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }
}
```

### Pattern 3: Operazioni Multiple con Overlay Condizionale

```razor
<PageLoadingOverlay IsVisible="_isLoadingBrands || _isSaving"
                    Message="@GetLoadingMessage()" />

@code {
    private bool _isLoadingBrands = false;
    private bool _isSaving = false;

    private string GetLoadingMessage()
    {
        if (_isSaving)
            return TranslationService.GetTranslation("messages.saving", "Salvataggio...");
        if (_isLoadingBrands)
            return TranslationService.GetTranslation("messages.loading", "Caricamento...");
        return string.Empty;
    }
}
```

### Pattern 4: Overlay in Dialog

```razor
<MudDialog>
    <DialogContent>
        <PageLoadingOverlay IsVisible="_isProcessing"
                            Message="@TranslationService.GetTranslation("messages.processing", "Elaborazione...")" />
        
        @if (!_isProcessing)
        {
            <!-- Contenuto del dialog -->
        }
    </DialogContent>
</MudDialog>
```

---

## Esempi di Implementazione

### Esempio Completo: Management Page

```razor
@page "/brand-management"
@using EventForge.DTOs.Products
@inject IBrandService BrandService
@inject ITranslationService TranslationService
@inject ISnackbar Snackbar

<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    <PageLoadingOverlay IsVisible="_isLoading || _isDeleting"
                        Message="@GetLoadingMessage()" />

    @if (_isLoading)
    {
        <!-- Mostra solo overlay durante il caricamento iniziale -->
    }
    else
    {
        <MudPaper Elevation="2" Class="pa-4 mb-4">
            <div class="d-flex justify-space-between align-center mb-4">
                <MudText Typo="Typo.h4">
                    @TranslationService.GetTranslation("brand.management", "Gestione Marchi")
                </MudText>
            </div>

            <!-- Toolbar -->
            <ManagementTableToolbar ShowSelectionBadge="true"
                                    SelectedCount="_selectedBrands.Count"
                                    ShowRefresh="true"
                                    ShowCreate="true"
                                    ShowDelete="true"
                                    IsDisabled="_isDeleting"
                                    OnRefresh="@LoadBrandsAsync"
                                    OnCreate="@CreateBrand"
                                    OnDelete="@DeleteSelectedBrandsAsync" />

            <!-- Tabella dati -->
            <MudTable T="BrandDto" 
                      Items="_brands" 
                      @bind-SelectedItems="_selectedBrands"
                      MultiSelection="true"
                      Hover="true">
                <!-- Definizione colonne -->
            </MudTable>
        </MudPaper>
    }
</MudContainer>

@code {
    private bool _isLoading = true;
    private bool _isDeleting = false;
    private List<BrandDto> _brands = new();
    private HashSet<BrandDto> _selectedBrands = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadBrandsAsync();
    }

    private async Task LoadBrandsAsync()
    {
        try
        {
            _isLoading = true;
            StateHasChanged();
            
            _brands = await BrandService.GetAllAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Errore nel caricamento: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task DeleteSelectedBrandsAsync()
    {
        if (!_selectedBrands.Any())
            return;

        try
        {
            _isDeleting = true;
            StateHasChanged();
            
            foreach (var brand in _selectedBrands)
            {
                await BrandService.DeleteAsync(brand.Id);
            }
            
            Snackbar.Add("Eliminazione completata", Severity.Success);
            _selectedBrands.Clear();
            await LoadBrandsAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Errore nell'eliminazione: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isDeleting = false;
            StateHasChanged();
        }
    }

    private string GetLoadingMessage()
    {
        if (_isDeleting)
            return TranslationService.GetTranslation("messages.deleting", "Eliminazione in corso...");
        if (_isLoading)
            return TranslationService.GetTranslation("messages.loadingPage", "Caricamento pagina...");
        return string.Empty;
    }

    private void CreateBrand()
    {
        // Implementazione creazione
    }
}
```

---

## Best Practice

### 1. Gestione dello Stato

**✅ FARE:**
```csharp
try
{
    _isLoading = true;
    StateHasChanged(); // Forza il render immediato
    
    await LoadDataAsync();
}
finally
{
    _isLoading = false;
    StateHasChanged(); // Forza il render finale
}
```

**❌ NON FARE:**
```csharp
_isLoading = true;
await LoadDataAsync();
_isLoading = false;
// Manca StateHasChanged() - l'overlay potrebbe non aggiornarsi
```

### 2. Messaggi Descrittivi

**✅ FARE:**
```razor
Message="@TranslationService.GetTranslation("messages.savingProduct", "Salvataggio prodotto...")"
```

**❌ NON FARE:**
```razor
Message="Caricamento..." <!-- Messaggio hardcoded, non localizzato -->
```

### 3. Operazioni Lunghe

Per operazioni che richiedono più di 3 secondi, considera:

```csharp
private async Task LongRunningOperationAsync()
{
    _isLoading = true;
    _loadingMessage = "Inizializzazione...";
    StateHasChanged();
    
    await Task.Delay(1000);
    
    _loadingMessage = "Elaborazione dati...";
    StateHasChanged();
    
    await ProcessDataAsync();
    
    _loadingMessage = "Finalizzazione...";
    StateHasChanged();
    
    await FinalizeAsync();
    
    _isLoading = false;
    StateHasChanged();
}
```

### 4. Combinazione con Altri Indicatori

**Per tabelle con refresh incrementale:**
```razor
<MudTable Loading="_isRefreshing" LoadingProgressColor="Color.Info">
    <!-- Usa MudTable.Loading per refresh parziali -->
</MudTable>

<PageLoadingOverlay IsVisible="_isInitialLoad" />
<!-- Usa PageLoadingOverlay solo per il caricamento iniziale completo -->
```

### 5. Z-Index e Sovrapposizione

Il PageLoadingOverlay ha `z-index: 9998` per garantire che:
- Sia visibile sopra il contenuto della pagina
- Sia sotto i dialog MudBlazor (`z-index: 9999`)

**Non modificare il z-index** senza valutare l'impatto su:
- Dialog e drawer esistenti
- Menu a tendina
- Tooltip e popover

---

## Accessibilità

Il componente PageLoadingOverlay è progettato con l'accessibilità in mente:

### Attributi ARIA

```html
<div class="page-loading-overlay" 
     role="status" 
     aria-live="polite" 
     aria-label="Caricamento in corso">
```

- **`role="status"`**: Identifica l'area come indicatore di stato
- **`aria-live="polite"`**: Notifica gli screen reader senza interrompere
- **`aria-label`**: Fornisce descrizione per utenti con screen reader

### Riduzione del Movimento

```css
@media (prefers-reduced-motion: reduce) {
  .page-loading-content { transition: none; }
}
```

Rispetta le preferenze utente per il movimento ridotto, importante per utenti con:
- Disturbi vestibolari
- Sensibilità alle animazioni
- Preferenze di accessibilità del sistema operativo

---

## Troubleshooting

### Problema: L'overlay non si mostra

**Causa**: Mancata chiamata a `StateHasChanged()`

**Soluzione**:
```csharp
_isLoading = true;
StateHasChanged(); // Aggiungi questa chiamata
await LoadDataAsync();
```

### Problema: L'overlay rimane visibile

**Causa**: Eccezione non gestita nel blocco try

**Soluzione**:
```csharp
try
{
    _isLoading = true;
    await LoadDataAsync();
}
finally // Usa always finally
{
    _isLoading = false;
    StateHasChanged();
}
```

### Problema: Flickering dell'overlay

**Causa**: Operazione troppo veloce (< 200ms)

**Soluzione**: Aggiungere un delay minimo
```csharp
var startTime = DateTime.Now;
await LoadDataAsync();
var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
if (elapsed < 200)
    await Task.Delay(200 - (int)elapsed);
```

### Problema: Overlay copre i dialog

**Causa**: Z-index errato

**Soluzione**: Il PageLoadingOverlay ha z-index 9998, i dialog MudBlazor hanno 9999. Non modificare questi valori. Se necessario, posiziona l'overlay all'interno del dialog invece che a livello di pagina.

---

## Performance

### Ottimizzazione del Rendering

1. **Evitare render inutili**:
```csharp
protected override bool ShouldRender()
{
    // Solo se lo stato di loading è cambiato
    return _previousLoadingState != _isLoading;
}
```

2. **Debouncing per operazioni frequenti**:
```csharp
private System.Timers.Timer? _debounceTimer;

private void OnSearchChanged()
{
    _debounceTimer?.Stop();
    _debounceTimer = new System.Timers.Timer(300);
    _debounceTimer.Elapsed += async (s, e) => await SearchAsync();
    _debounceTimer.Start();
}
```

---

## Conclusione

Seguendo queste linee guida, garantirai:
- **Esperienza utente coerente** in tutta l'applicazione
- **Accessibilità** per tutti gli utenti
- **Performance ottimali** riducendo rendering inutili
- **Manutenibilità** del codice con pattern standardizzati

Per domande o suggerimenti di miglioramento, contattare il team di sviluppo frontend.

---

**Versione documento**: 1.0  
**Data**: 29 Ottobre 2025  
**Autore**: EventForge Development Team
