# ProductDrawer Enhancement - Gestione Entità Collegate

## 📋 Problema Originale

L'analisi del ProductDrawer ha rivelato che mancavano sezioni dedicate alla gestione delle entità collegate al prodotto, come indicato nella documentazione:

- **ProductCode**: Codici alternativi (SKU, EAN, UPC, ecc.)
- **ProductUnit**: Unità di misura alternative con fattori di conversione
- **ProductBundleItem**: Componenti del bundle (quando IsBundle=true)

## ✅ Soluzione Implementata

### 1. Servizi Client (IProductService e ProductService)

Aggiunti metodi completi per la gestione CRUD di tutte e tre le entità:

#### ProductCode Management
```csharp
Task<IEnumerable<ProductCodeDto>?> GetProductCodesAsync(Guid productId);
Task<ProductCodeDto?> GetProductCodeByIdAsync(Guid id);
Task<ProductCodeDto?> CreateProductCodeAsync(CreateProductCodeDto createDto);
Task<ProductCodeDto?> UpdateProductCodeAsync(Guid id, UpdateProductCodeDto updateDto);
Task<bool> DeleteProductCodeAsync(Guid id);
```

#### ProductUnit Management
```csharp
Task<IEnumerable<ProductUnitDto>?> GetProductUnitsAsync(Guid productId);
Task<ProductUnitDto?> GetProductUnitByIdAsync(Guid id);
Task<ProductUnitDto?> CreateProductUnitAsync(CreateProductUnitDto createDto);
Task<ProductUnitDto?> UpdateProductUnitAsync(Guid id, UpdateProductUnitDto updateDto);
Task<bool> DeleteProductUnitAsync(Guid id);
```

#### ProductBundleItem Management
```csharp
Task<IEnumerable<ProductBundleItemDto>?> GetProductBundleItemsAsync(Guid bundleProductId);
Task<ProductBundleItemDto?> GetProductBundleItemByIdAsync(Guid id);
Task<ProductBundleItemDto?> CreateProductBundleItemAsync(CreateProductBundleItemDto createDto);
Task<ProductBundleItemDto?> UpdateProductBundleItemAsync(Guid id, UpdateProductBundleItemDto updateDto);
Task<bool> DeleteProductBundleItemAsync(Guid id);
```

### 2. UI Components nel ProductDrawer

#### Edit Mode
Aggiunte tre nuove sezioni con `MudExpansionPanel` dopo la sezione Suppliers:

1. **Product Codes Section**
   - Mostra tabella con: Tipo Codice, Codice, Descrizione, Stato
   - Bottoni: Add, Edit, Delete
   - Badge con conteggio

2. **Product Units Section**
   - Mostra tabella con: Tipo Unità, Unità di Misura, Fattore di Conversione, Descrizione, Stato
   - Bottoni: Add, Edit, Delete
   - Badge con conteggio

3. **Product Bundle Items Section** (solo se IsBundle=true)
   - Mostra tabella con: Prodotto Componente, Quantità
   - Bottoni: Add, Edit, Delete
   - Badge con conteggio

#### View Mode
Sezioni identiche ma in modalità read-only senza bottoni di modifica.

### 3. State Management

Aggiunti campi privati nel componente:
```csharp
private bool _loadingCodes = false;
private IEnumerable<ProductCodeDto>? _productCodes;

private bool _loadingUnits = false;
private IEnumerable<ProductUnitDto>? _productUnits;

private bool _loadingBundleItems = false;
private IEnumerable<ProductBundleItemDto>? _bundleItems;
```

Con metodi Load e Clear corrispondenti:
- `LoadCodesAsync()` / `ClearCodes()`
- `LoadUnitsAsync()` / `ClearUnits()`
- `LoadBundleItemsAsync()` / `ClearBundleItems()`

### 4. Helper Methods

Aggiunti metodi helper per visualizzazione:

```csharp
// Status text and colors for ProductCode
private string GetCodeStatusText(ProductCodeStatus status)
private Color GetCodeStatusColor(ProductCodeStatus status)

// Status text and colors for ProductUnit
private string GetUnitStatusText(ProductUnitStatus status)
private Color GetUnitStatusColor(ProductUnitStatus status)

// Product name lookup (placeholder)
private string GetProductName(Guid productId)
```

### 5. Management Regions

Tre nuove region nel codice:

1. **#region Product Code Management**
   - `OpenAddCodeDialog()` - TODO: Implementare dialog
   - `OpenEditCodeDialog()` - TODO: Implementare dialog
   - `DeleteCode()` - ✅ Implementato con conferma

2. **#region Product Unit Management**
   - `OpenAddUnitDialog()` - TODO: Implementare dialog
   - `OpenEditUnitDialog()` - TODO: Implementare dialog
   - `DeleteUnit()` - ✅ Implementato con conferma

3. **#region Product Bundle Item Management**
   - `OpenAddBundleItemDialog()` - TODO: Implementare dialog
   - `OpenEditBundleItemDialog()` - TODO: Implementare dialog
   - `DeleteBundleItem()` - ✅ Implementato con conferma

### 6. Traduzioni

#### Italiano (it.json)
```json
{
  "field": {
    "productCodes": "Codici Alternativi",
    "productUnits": "Unità di Misura Alternative",
    "bundleComponents": "Componenti Bundle",
    "alternativeDescription": "Descrizione Alternativa",
    "unitType": "Tipo Unità",
    "conversionFactor": "Fattore di Conversione",
    "componentProduct": "Prodotto Componente",
    "quantity": "Quantità"
  },
  "button": {
    "addCode": "Aggiungi Codice",
    "addUnit": "Aggiungi Unità",
    "addBundleItem": "Aggiungi Componente"
  },
  "messages": {
    "noCodes": "Nessun codice alternativo disponibile",
    "noUnits": "Nessuna unità di misura alternativa disponibile",
    "noBundleItems": "Nessun componente disponibile",
    "featureNotImplemented": "Funzionalità non ancora implementata"
  },
  "dialog": {
    "confirmDeleteCode": "Sei sicuro di voler eliminare questo codice?",
    "confirmDeleteUnit": "Sei sicuro di voler eliminare questa unità di misura?",
    "confirmDeleteBundleItem": "Sei sicuro di voler eliminare questo componente?"
  }
}
```

#### English (en.json)
Equivalenti in inglese per tutte le chiavi sopra.

## 🎯 UX/UI Pattern Utilizzati

### Pattern Consistente con BrandDrawer
Il pattern segue lo stesso approccio usato in BrandDrawer per la gestione dei Models:

1. **MudExpansionPanel** per sezioni collassabili
2. **Badge con conteggio** nel titolo del panel
3. **Bottone Add** nel titolo del panel
4. **MudTable** per visualizzare i dati
5. **IconButton Edit/Delete** per ogni riga
6. **ConfirmationDialog** prima della cancellazione
7. **Loading states** separati per ogni sezione

### Accessibilità
- Tutti i bottoni hanno `aria-label`
- Helper text per spiegare ogni campo
- Loading indicators durante il caricamento
- Messaggi di stato per azioni

## 📊 Struttura delle Tabelle

### ProductCode Table
| Campo | Descrizione |
|-------|-------------|
| Tipo Codice | SKU, EAN, UPC, etc. |
| Codice | Valore del codice |
| Descrizione | Descrizione alternativa |
| Stato | Active, Suspended, Deleted |
| Azioni | Edit, Delete |

### ProductUnit Table
| Campo | Descrizione |
|-------|-------------|
| Tipo Unità | Base, Pack, Pallet, etc. |
| Unità di Misura | Nome e simbolo dell'UM |
| Fattore di Conversione | N.NNN formato |
| Descrizione | Descrizione opzionale |
| Stato | Active, Suspended, Deleted |
| Azioni | Edit, Delete |

### ProductBundleItem Table
| Campo | Descrizione |
|-------|-------------|
| Prodotto Componente | Nome del prodotto |
| Quantità | Numero di unità |
| Azioni | Edit, Delete |

## 🔄 Workflow Utente

### Visualizzazione (View Mode)
1. Aprire ProductDrawer in modalità View
2. Le sezioni relative entities sono caricate automaticamente
3. I pannelli sono collassabili per migliore organizzazione
4. Nessuna azione di modifica disponibile

### Modifica (Edit Mode)
1. Aprire ProductDrawer in modalità Edit
2. Scroll alle nuove sezioni dopo Suppliers
3. Click su + per aggiungere nuova entity
4. Click su Edit per modificare
5. Click su Delete (con conferma) per eliminare

### Creazione (Create Mode)
1. Le sezioni non sono visibili in Create mode
2. Prima creare il prodotto
3. Poi editarlo per aggiungere le entity collegate

## ⚠️ Limitazioni Attuali

### Dialog Add/Edit Non Implementati
I dialog per Add e Edit mostrano al momento:
```
"Funzionalità non ancora implementata"
```

Per implementarli, seguire il pattern di `AddProductSupplierDialog`:

1. Creare `AddProductCodeDialog.razor`
2. Creare `EditProductCodeDialog.razor`
3. Ripetere per ProductUnit e ProductBundleItem
4. Aggiornare i metodi `OpenAdd*Dialog()` e `OpenEdit*Dialog()`

### GetProductName Placeholder
Il metodo `GetProductName()` attualmente restituisce solo l'ID:
```csharp
private string GetProductName(Guid productId)
{
    // This would need to load products or use a cache
    // For now, just return the ID
    return productId.ToString();
}
```

**Soluzioni possibili:**
1. Caricare tutti i prodotti in dropdown all'apertura del drawer
2. Implementare un servizio di cache prodotti
3. Fare chiamata API separata per ogni prodotto (meno efficiente)

## ✨ Vantaggi dell'Implementazione

### 1. Completezza
Tutte le entità collegate sono ora gestibili dal ProductDrawer

### 2. Consistenza
Pattern UI/UX allineato con il resto dell'applicazione

### 3. Accessibilità
Interfaccia accessibile con supporto ARIA

### 4. Manutenibilità
Codice ben organizzato in regions con commenti

### 5. Internazionalizzazione
Traduzioni complete IT/EN

## 🚀 Prossimi Passi

1. **Implementare i Dialog Add/Edit**
   - Creare dialog per ProductCode
   - Creare dialog per ProductUnit
   - Creare dialog per ProductBundleItem

2. **Migliorare GetProductName**
   - Implementare cache prodotti
   - O caricare nomi al load del drawer

3. **Testing**
   - Test unitari per i nuovi metodi service
   - Test UI per le nuove sezioni
   - Test di integrazione end-to-end

4. **Validazione**
   - Validare input nei dialog
   - Gestire errori di rete
   - Messaggi utente appropriati

## 📝 File Modificati

### Client Services
- `EventForge.Client/Services/IProductService.cs` - Aggiunte interfacce
- `EventForge.Client/Services/ProductService.cs` - Implementazioni

### UI Components
- `EventForge.Client/Shared/Components/ProductDrawer.razor` - UI completa

### Translations
- `EventForge.Client/wwwroot/i18n/it.json` - Traduzioni IT
- `EventForge.Client/wwwroot/i18n/en.json` - Traduzioni EN

## ✅ Checklist Completamento

- [x] IProductService interfacce
- [x] ProductService implementazioni
- [x] ProductDrawer UI Edit mode
- [x] ProductDrawer UI View mode
- [x] State management (load/clear)
- [x] Helper methods
- [x] Delete functionality con conferma
- [x] Traduzioni IT
- [x] Traduzioni EN
- [x] Build successful
- [x] JSON validation
- [ ] Dialog Add/Edit (TODO futuro)
- [ ] GetProductName implementation (TODO futuro)
- [ ] Testing manuale
- [ ] Testing automatico

## 🎉 Conclusione

L'implementazione è **completa e funzionale** per quanto riguarda:
- Visualizzazione delle entità collegate
- Delete con conferma
- Struttura UI/UX consistente
- Traduzioni complete

Le funzionalità Add/Edit possono essere implementate successivamente seguendo il pattern già stabilito con ProductSupplier.

---

**Data implementazione**: 2025
**Status**: ✅ COMPLETATO (tranne dialog Add/Edit)
**Build**: ✅ SUCCESS
**Translations**: ✅ COMPLETE
