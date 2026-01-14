# Progressive Disclosure Implementation - AddDocumentRowDialog

## Obiettivo
Implementare il pattern Progressive Disclosure nel componente `AddDocumentRowDialog` per ridurre l'altezza del dialog da ~850-900px a ~500-600px e migliorare l'esperienza utente, specialmente su dispositivi mobili.

## Modifiche Implementate

### 1. Chiavi di Traduzione Aggiunte
Aggiunte 3 nuove chiavi di traduzione in `it.json` e `en.json`:

| Chiave | Italiano | English |
|--------|----------|---------|
| `documents.vatAndPricesSection` | IVA e Prezzi | VAT & Prices |
| `documents.discountsSection` | Sconti | Discounts |
| `documents.notesAndDetailsSection` | Note e Dettagli | Notes & Details |

### 2. Ristrutturazione Layout

#### PRIMA (Layout Tradizionale - ~850-900px)
```
┌─────────────────────────────────────────┐
│ ┌─────────────────────────────────────┐ │
│ │ 1. BARCODE SCANNER                  │ │
│ └─────────────────────────────────────┘ │
│                                           │
│ ┌──────────────┐ ┌──────────────────┐   │
│ │ 2. PRODOTTO  │ │ 2. QUANTITÀ + UM │   │
│ │              │ │                  │   │
│ └──────────────┘ └──────────────────┘   │
│                                           │
│ ┌──────┐ ┌──────┐ ┌──────────────┐      │
│ │PREZZI│ │  IVA │ │    SCONTI    │      │
│ │Netto │ │      │ │              │      │
│ │Lordo │ │      │ │              │      │
│ └──────┘ └──────┘ └──────────────┘      │
│                                           │
│ ┌─────────────────────────────────────┐ │
│ │ 4. NOTE (Full Width)                │ │
│ └─────────────────────────────────────┘ │
│                                           │
│ ┌─────────────────────────────────────┐ │
│ │ 5. RIEPILOGO RIGA                   │ │
│ │ (Sempre Visibile)                   │ │
│ └─────────────────────────────────────┘ │
└─────────────────────────────────────────┘
```

#### DOPO (Progressive Disclosure - ~500-600px)
```
┌─────────────────────────────────────────┐
│ ┌─────────────────────────────────────┐ │
│ │ 1. BARCODE SCANNER                  │ │
│ └─────────────────────────────────────┘ │
│                                           │
│ ┌──────────────┐ ┌──────────────────┐   │
│ │ 2. PRODOTTO  │ │ 2. QUANTITÀ + UM │   │
│ │              │ │                  │   │
│ └──────────────┘ └──────────────────┘   │
│                                           │
│ ┌─────────────────────────────────────┐ │
│ │ 3. PREZZO UNITARIO NETTO            │ │
│ └─────────────────────────────────────┘ │
│                                           │
│ ▼ IVA e Prezzi                          │ ← Collapsible
│ ▼ Sconti                                │ ← Collapsible
│ ▼ Note e Dettagli                       │ ← Collapsible
│                                           │
│ ┌─────────────────────────────────────┐ │
│ │ 4. RIEPILOGO RIGA                   │ │
│ │ (Sempre Visibile)                   │ │
│ └─────────────────────────────────────┘ │
└─────────────────────────────────────────┘
```

### 3. Dettagli dei Pannelli Espandibili

#### Panel 1: "IVA e Prezzi"
- **ID**: `vat-prices-panel`
- **Campi**:
  - Aliquota IVA % (select)
  - Importo IVA (calcolato, read-only)
  - Prezzo Unit. Lordo (calcolato, read-only)
- **Stato**: Chiuso per default

#### Panel 2: "Sconti"
- **ID**: `discounts-panel`
- **Campi**:
  - Sconto % (0-100)
  - Sconto € (valore assoluto)
- **Stato**: Chiuso per default

#### Panel 3: "Note e Dettagli"
- **ID**: `notes-details-panel`
- **Campi**:
  - Note (opzionali, 2 righe)
- **Stato**: Chiuso per default

### 4. Campi Sempre Visibili

#### Sezione Essenziale (in alto)
1. **Barcode Scanner** - Scansione rapida prodotti
2. **Prodotto** (colonna sinistra):
   - Autocomplete prodotto
   - Descrizione
   - Checkbox "Somma quantità se già presente"
3. **Quantità e Unità** (colonna destra):
   - Quantità numerica
   - Unità di misura (select)
4. **Prezzo Unitario Netto** - Campo principale per il prezzo

#### Riepilogo (in basso - sempre visibile)
- Subtotale Netto
- Imposta IVA
- Sconto Totale
- **TOTALE RIGA** (evidenziato)

## Benefici dell'Implementazione

### 1. Riduzione Altezza Dialog
- **Prima**: ~850-900px
- **Dopo**: ~500-600px  
- **Riduzione**: ~40-50%

### 2. Miglioramenti UX
- ✅ Meno scrolling richiesto
- ✅ Focus sui campi essenziali
- ✅ Interfaccia più pulita
- ✅ Migliore esperienza mobile
- ✅ Accesso rapido alle funzioni avanzate quando necessario

### 3. Accessibilità
- ID univoci su tutti i pannelli
- Controllo programmatico dello stato dei pannelli
- Supporto keyboard navigation (MudBlazor nativo)
- Screen reader friendly

## Modifiche Tecniche

### File Modificati
1. **AddDocumentRowDialog.razor**
   - Rimossi `@inject` directives (ora in .razor.cs)
   - Rimosso `@code` block (ora in .razor.cs)
   - Implementati MudExpansionPanels
   - Ridotto da ~1225 a ~293 righe

2. **AddDocumentRowDialog.razor.cs**
   - Aggiunto `using EventForge.Client.Services.Documents;`
   - Aggiunto `IDocumentRowCalculationService` injection
   - Aggiunte variabili di stato per pannelli:
     - `_vatPanelExpanded`
     - `_discountsPanelExpanded`
     - `_notesPanelExpanded`

3. **Translation Files**
   - `it.json`: +3 chiavi
   - `en.json`: +3 chiavi

### Pattern Utilizzati
- **Progressive Disclosure**: Nascondi complessità fino a quando necessaria
- **Code-Behind**: Separazione markup/logica
- **Component Composition**: MudExpansionPanels per UI modulare

## Testing Checklist

### Funzionalità Core
- [ ] Barcode scanner funziona correttamente
- [ ] Autocomplete prodotto carica risultati
- [ ] Selezione prodotto popola campi
- [ ] Quantità e unità di misura funzionano
- [ ] Prezzo unitario accetta input

### Pannelli Espandibili
- [ ] Panel "IVA e Prezzi" si apre/chiude
- [ ] Calcoli IVA funzionano correttamente
- [ ] Panel "Sconti" si apre/chiude
- [ ] Calcoli sconti funzionano
- [ ] Panel "Note" si apre/chiude
- [ ] Note vengono salvate

### Calcoli e Riepilogo
- [ ] Subtotale calcolato correttamente
- [ ] IVA calcolata correttamente
- [ ] Sconti applicati correttamente
- [ ] Totale finale corretto

### Responsive Design
- [ ] Dialog responsivo su desktop
- [ ] Dialog responsivo su tablet
- [ ] Dialog responsivo su mobile
- [ ] Pannelli funzionano su tutti i dispositivi

### Accessibilità
- [ ] Navigazione da tastiera funziona
- [ ] Tab order corretto
- [ ] Screen reader compatibile
- [ ] ID univoci presenti

## Note per il Testing
- L'altezza effettiva del dialog dipende dal contenuto dei pannelli quando espansi
- Con tutti i pannelli chiusi, l'altezza dovrebbe essere ~500-600px
- Il riepilogo rimane sempre visibile indipendentemente dallo stato dei pannelli
- I calcoli automatici (IVA, lordo, sconti) devono aggiornarsi in real-time

## Compatibilità
- ✅ .NET 10.0
- ✅ MudBlazor 7.x
- ✅ Browser moderni (Chrome, Firefox, Edge, Safari)
- ✅ Mobile browsers (iOS Safari, Chrome Mobile)
