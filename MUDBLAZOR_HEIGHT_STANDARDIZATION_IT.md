# Standardizzazione Altezza Componenti MudBlazor

## Contesto

Dopo la PR #531 che ha sistemato l'altezza dei controlli `MudSelect`, questa modifica estende la stessa logica a tutti i componenti di input MudBlazor per garantire un'altezza uniforme di 40px per tutti i controlli non multilinea.

## Obiettivo

Uniformare lo stile di tutti i componenti MudBlazor nel file CSS dedicato (`mud-components.css`) in modo che tutti i controlli, se non sono multilinea, abbiano la stessa altezza del MudSelect.

## Componenti Standardizzati

I seguenti componenti ora hanno un'altezza uniforme quando si utilizza la classe CSS appropriata:

| Componente | Classe CSS | Altezza |
|------------|-----------|---------|
| MudTextField | `ef-input` | 40px (min-height) |
| MudSelect | `ef-select` | 40px (min-height) |
| MudNumericField | `ef-numeric` | 40px (min-height) |
| MudDatePicker | `ef-datepicker` | 40px (min-height) |
| MudTimePicker | `ef-timepicker` | 40px (min-height) |
| MudAutocomplete | `ef-autocomplete` | 40px (min-height) |

**Nota:** I componenti multilinea come `MudTextField` con `Lines > 1` non sono interessati da questa standardizzazione e mantengono il loro comportamento naturale.

## Modifiche Tecniche

### 1. Normalizzazione dell'Altezza (`.mud-input-root`)

```css
.ef-input .mud-input-root,
.ef-select .mud-input-root,
.ef-numeric .mud-input-root,
.ef-datepicker .mud-input-root,
.ef-timepicker .mud-input-root,
.ef-autocomplete .mud-input-root {
    min-height: var(--ef-field-height) !important;  /* 40px */
    box-sizing: border-box !important;
}
```

**Perché `min-height` invece di `height`:**
- Permette ai componenti di espandersi se necessario (es. errori di validazione)
- Garantisce comunque una baseline uniforme
- Evita problemi di overflow del contenuto

### 2. Elementi Interni (`.mud-input-slot`)

```css
.ef-input .mud-input-slot,
.ef-select .mud-input-slot,
.ef-numeric .mud-input-slot,
.ef-datepicker .mud-input-slot,
.ef-timepicker .mud-input-slot,
.ef-autocomplete .mud-input-slot {
    min-height: unset !important;
    height: 100%;
}
```

Questo assicura che gli elementi interni si adattino al contenitore senza aggiungere altezza extra.

### 3. Controlli di Input Specifici

Per ogni tipo di componente, sono state aggiunte regole specifiche per i controlli interni:

```css
/* Numeric field */
.ef-numeric .mud-input-input-control {
    height: auto;
    padding: 6px 0;
    display: flex;
    align-items: center;
    box-sizing: border-box !important;
}

/* DatePicker e TimePicker */
.ef-datepicker .mud-input-input-control,
.ef-timepicker .mud-input-input-control {
    height: auto;
    padding: 6px 0;
    display: flex;
    align-items: center;
    box-sizing: border-box !important;
}

/* Autocomplete */
.ef-autocomplete .mud-input-control {
    height: auto;
    padding: 6px 0;
    display: flex;
    align-items: center;
    box-sizing: border-box !important;
}
```

**Caratteristiche comuni:**
- `height: auto` - dimensionamento naturale basato sul contenuto
- `padding: 6px 0` - spaziatura verticale per il testo
- `display: flex` con `align-items: center` - centratura verticale
- `box-sizing: border-box` - calcolo dimensioni coerente

### 4. Allineamento Adornments/Icons

```css
.ef-input .mud-input-adornment,
.ef-select .mud-input-adornment,
.ef-numeric .mud-input-adornment,
.ef-datepicker .mud-input-adornment,
.ef-timepicker .mud-input-adornment,
.ef-autocomplete .mud-input-adornment {
    height: 100%;
    display: inline-flex;
    align-items: center;
}
```

Garantisce che le icone e gli adornment siano correttamente allineati verticalmente in tutti i tipi di input.

## Come Utilizzare

### Esempio Base

```razor
<!-- TextField con altezza standardizzata -->
<MudTextField @bind-Value="model.Name"
              Label="Nome"
              Variant="Variant.Outlined"
              Class="ef-input" />

<!-- NumericField con altezza standardizzata -->
<MudNumericField @bind-Value="model.Price"
                 Label="Prezzo"
                 Variant="Variant.Outlined"
                 Class="ef-numeric" />

<!-- DatePicker con altezza standardizzata -->
<MudDatePicker @bind-Date="model.Date"
               Label="Data"
               Variant="Variant.Outlined"
               Class="ef-datepicker" />

<!-- Select con altezza standardizzata -->
<MudSelect @bind-Value="model.Category"
           Label="Categoria"
           Variant="Variant.Outlined"
           Class="ef-select">
    <MudSelectItem Value="@("A")">Categoria A</MudSelectItem>
    <MudSelectItem Value="@("B")">Categoria B</MudSelectItem>
</MudSelect>

<!-- Autocomplete con altezza standardizzata -->
<MudAutocomplete T="string"
                 @bind-Value="model.City"
                 Label="Città"
                 Variant="Variant.Outlined"
                 Class="ef-autocomplete"
                 SearchFunc="SearchCities" />
```

### Esempio Form Completo

```razor
<MudGrid>
    <MudItem xs="12" md="6">
        <MudTextField @bind-Value="product.Code"
                      Label="Codice"
                      Variant="Variant.Outlined"
                      Class="ef-input" />
    </MudItem>
    <MudItem xs="12" md="6">
        <MudSelect @bind-Value="product.CategoryId"
                   Label="Categoria"
                   Variant="Variant.Outlined"
                   Class="ef-select">
            @foreach (var cat in categories)
            {
                <MudSelectItem Value="@cat.Id">@cat.Name</MudSelectItem>
            }
        </MudSelect>
    </MudItem>
    <MudItem xs="12" md="6">
        <MudNumericField @bind-Value="product.Price"
                         Label="Prezzo"
                         Variant="Variant.Outlined"
                         Format="N2"
                         Class="ef-numeric" />
    </MudItem>
    <MudItem xs="12" md="6">
        <MudDatePicker @bind-Date="product.ReleaseDate"
                       Label="Data Rilascio"
                       Variant="Variant.Outlined"
                       Class="ef-datepicker" />
    </MudItem>
</MudGrid>
```

Tutti i campi in questo form avranno la stessa altezza di 40px, creando un aspetto visivamente uniforme e professionale.

## Responsive Design

Il sistema include breakpoint responsive per schermi più piccoli:

```css
@media (max-width:768px) {
    :root { 
        --ef-input-height: 36px; 
        --ef-button-height: 36px; 
        --ef-field-height: 36px; 
    }
}
```

**Nota:** Questi valori devono essere mantenuti sincronizzati con le dichiarazioni principali nel selettore `:root` (linee 18-26) per evitare inconsistenze. Su dispositivi mobili, l'altezza si riduce a 36px per una migliore usabilità touch.

## Pattern Stabilito (da PR #531)

Questo lavoro segue il pattern stabilito nella PR #531 per MudSelect:

### Prima (Approccio Errato)
```css
.ef-select .mud-input-root {
    height: 40px !important;  /* Altezza fissa - problematico */
}
.ef-select .mud-select-input {
    height: 100%;  /* Non si adatta bene */
}
```

**Problemi:**
- Altezza forzata causava overflow
- Testo compresso
- Box model non coerente

### Dopo (Approccio Corretto)
```css
.ef-select .mud-input-root {
    min-height: 40px !important;  /* Baseline flessibile */
    box-sizing: border-box !important;
}
.ef-select .mud-select-input {
    height: auto;  /* Dimensionamento naturale */
    padding: 6px 0;  /* Spaziatura appropriata */
    box-sizing: border-box !important;
}
```

**Vantaggi:**
- Altezza minima garantita ma flessibile
- Spaziatura interna appropriata
- Box model coerente
- Miglior centratura del testo

## Compatibilità

- **Retrocompatibile:** I componenti senza classi `ef-*` continuano a funzionare normalmente
- **Opt-in:** Gli sviluppatori devono aggiungere esplicitamente le classi per ottenere la standardizzazione
- **Non invasivo:** Non modifica il comportamento predefinito di MudBlazor

## Testing

### Verifiche Eseguite

1. ✅ Build del progetto completato con successo (0 warnings, 0 errors)
2. ✅ Nessun problema di sicurezza rilevato da CodeQL
3. ✅ File CSS valido e ben formattato
4. ✅ Tutti i selettori CSS sono specifici e non creano conflitti

### Testing Manuale Raccomandato

Per verificare la corretta implementazione:

1. **Test Visivo Base**
   - Creare una form con tutti i tipi di input
   - Applicare le classi `ef-*` appropriate
   - Verificare che tutti gli input abbiano la stessa altezza visiva

2. **Test Varianti**
   - Testare con `Variant.Outlined`
   - Testare con `Variant.Filled`
   - Testare con `Variant.Text`

3. **Test Stati**
   - Input normale
   - Input con focus
   - Input con errori di validazione
   - Input disabled

4. **Test Responsive**
   - Desktop (altezza 40px)
   - Tablet (altezza 40px)
   - Mobile (altezza 36px)

## File Modificati

- `EventForge.Client/wwwroot/css/components/mud-components.css`
  - +53 linee aggiunte
  - -7 linee rimosse
  - Totale netto: +46 linee

## Benefici

1. **Coerenza Visiva:** Tutti i controlli form hanno la stessa altezza baseline
2. **Manutenibilità:** Centralizzazione delle regole CSS in un unico file
3. **Scalabilità:** Facile aggiungere nuovi componenti seguendo lo stesso pattern
4. **Flessibilità:** Uso di `min-height` permette adattamento quando necessario
5. **Accessibilità:** Altezze appropriate per target touch e mouse
6. **Professionalità:** Interfaccia più pulita e professionale

## Prossimi Passi (Opzionali)

Per completare l'adozione di questo sistema:

1. **Aggiornare i componenti esistenti:**
   - Aggiungere le classi `ef-*` ai componenti in ProductDrawer
   - Aggiungere le classi `ef-*` ai componenti in form di documenti
   - Aggiungere le classi `ef-*` ai componenti in drawers

2. **Documentazione:**
   - Aggiungere esempi alla guida di stile del progetto
   - Creare screenshot prima/dopo per la documentazione

3. **Linee guida:**
   - Stabilire come standard per tutti i nuovi componenti
   - Aggiornare le linee guida di sviluppo

## Riferimenti

- PR #531: Fix MudSelect height and alignment with proper CSS box model
- File CSS: `EventForge.Client/wwwroot/css/components/mud-components.css`
- Documentazione MudBlazor: https://mudblazor.com/

## Riepilogo

Questa modifica completa il lavoro iniziato nella PR #531, estendendo il pattern di altezza uniforme a tutti i componenti di input MudBlazor. Il risultato è un sistema coeso e flessibile che garantisce coerenza visiva in tutta l'applicazione, mantenendo al contempo la flessibilità necessaria per casi d'uso specifici.
