# 🔄 Diagramma di Flusso: Assegnazione Codice durante Inventario

## 📊 Panoramica Generale

```
┌─────────────────────────────────────────────────────────────────────┐
│                    PROCEDURA DI INVENTARIO                          │
│                                                                     │
│  1. Avvia Sessione Inventario                                      │
│  2. Scansiona Codici e Conta Prodotti                             │
│  3. Gestisci Codici Non Trovati (QUESTO FLUSSO)                   │
│  4. Finalizza Inventario                                          │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 🎯 Flusso Principale: Scansione Codice

```
START
  │
  ├─> [Operatore Scansiona Codice]
  │          │
  │          v
  │   ┌──────────────────┐
  │   │ SearchBarcode()  │
  │   │ in Inventory     │
  │   │ Procedure        │
  │   └────────┬─────────┘
  │            │
  │            v
  │   ProductService.GetProductByCodeAsync()
  │            │
  │            v
  ├───────┬─────────┬──────────
  │       │         │
  v       v         v
FOUND   ERROR    NOT FOUND
  │       │         │
  │       │         v
  │       │    [QUESTO DOCUMENTO]
  │       │    ShowProductNotFoundDialog()
  │       │         │
  │       │         └─> Vedi dettaglio sotto
  │       │
  │       v
  │    [Error Handler]
  │       │
  │       └─> Snackbar Error
  │       └─> Log Error
  │
  v
[Show Inventory Entry Dialog]
  │
  └─> Operatore inserisce quantità
      │
      └─> Salva conteggio
          │
          └─> Continua con prossimo prodotto
```

---

## 🔍 Dettaglio: ProductNotFoundDialog

### A. Inizializzazione Dialog

```
┌────────────────────────────────────────────────────────┐
│ ShowProductNotFoundDialog()                            │
├────────────────────────────────────────────────────────┤
│                                                        │
│ 1. Crea DialogParameters:                             │
│    └─> Barcode: _scannedBarcode                       │
│    └─> IsInventoryContext: true  ← IMPORTANTE!        │
│                                                        │
│ 2. Crea DialogOptions:                                │
│    └─> CloseOnEscapeKey: true                         │
│    └─> MaxWidth: Medium                               │
│    └─> FullWidth: true                                │
│                                                        │
│ 3. Show Dialog:                                       │
│    └─> await DialogService.ShowAsync<...>()           │
│                                                        │
│ 4. Await Result                                       │
└────────────────────────────────────────────────────────┘
                         │
                         v
              [Dialog Mostrato a Schermo]
```

### B. Interfaccia Dialog (IsInventoryContext = true)

```
╔════════════════════════════════════════════════════════╗
║  ⚠️  Prodotto non trovato                              ║
╠════════════════════════════════════════════════════════╣
║                                                        ║
║  [!] Prodotto non trovato con il codice: ABC123       ║
║                                                        ║
║  ┌────────────────────────────────────────────────┐   ║
║  │ 📦 Codice da Assegnare                         │   ║
║  │ [  ABC123  ]                                   │   ║
║  └────────────────────────────────────────────────┘   ║
║                                                        ║
║  Cerca un prodotto esistente per assegnare questo     ║
║  codice, oppure crea un nuovo prodotto.               ║
║                                                        ║
║  ┌────────────────────────────────────────────────┐   ║
║  │ 🔍 Cerca Prodotto                              │   ║
║  │ ┌──────────────────────────────────────────┐  │   ║
║  │ │ [Digita per cercare...]                  │  │   ║
║  │ └──────────────────────────────────────────┘  │   ║
║  │ Cerca per codice o descrizione               │   ║
║  └────────────────────────────────────────────────┘   ║
║                                                        ║
║  ┌────────────────────────────────────────────────┐   ║
║  │ [ Salta ]  [ Annulla ]  [ Crea Nuovo Prodotto ]│   ║
║  └────────────────────────────────────────────────┘   ║
╚════════════════════════════════════════════════════════╝
                         │
        ┌────────────────┼────────────────┐
        v                v                v
     SALTA            ANNULLA           CREA
```

### C. Stato: Prodotto Selezionato

```
╔════════════════════════════════════════════════════════╗
║  ⚠️  Prodotto non trovato                              ║
╠════════════════════════════════════════════════════════╣
║                                                        ║
║  [!] Prodotto non trovato con il codice: ABC123       ║
║                                                        ║
║  ┌────────────────────────────────────────────────┐   ║
║  │ 📦 Codice da Assegnare: [ABC123]              │   ║
║  └────────────────────────────────────────────────┘   ║
║                                                        ║
║  🔍 [Sedia da Conferenza - CHAIR001]  ← Selezionato  ║
║                                                        ║
║  ┌────────────────────────────────────────────────┐   ║
║  │ ✅ Prodotto Selezionato                        │   ║
║  │                                                │   ║
║  │ Nome Prodotto:    Sedia da Conferenza         │   ║
║  │ Codice Prodotto:  CHAIR001                     │   ║
║  │ Descrizione:      Sedia ergonomica...          │   ║
║  └────────────────────────────────────────────────┘   ║
║                                                        ║
║  ┌────────────────────────────────────────────────┐   ║
║  │ Tipo Codice:  [Barcode ▼]                     │   ║
║  │ Codice:       [ABC123]                         │   ║
║  │ Descrizione   [_____________________________]  │   ║
║  │ Alternativa:                                   │   ║
║  └────────────────────────────────────────────────┘   ║
║                                                        ║
║  ┌────────────────────────────────────────────────┐   ║
║  │ [ Salta ]  [ Annulla ]  [ Assegna e Continua ]│   ║
║  └────────────────────────────────────────────────┘   ║
╚════════════════════════════════════════════════════════╝
                         │
        ┌────────────────┼────────────────┐
        v                v                v
     SALTA            ANNULLA          ASSEGNA
```

---

## 🎬 Scenario 1: SKIP - Salta Codice

```
╔═══════════════════════════════════════════════════════════════╗
║                    SCENARIO: SKIP                             ║
╚═══════════════════════════════════════════════════════════════╝

[Dialog Aperto]
     │
     v
[Operatore Click "Salta"]
     │
     v
┌─────────────────────────┐
│ SelectAction("skip")    │
└───────────┬─────────────┘
            │
            v
┌─────────────────────────────────────┐
│ MudDialog.Close(                    │
│   DialogResult.Ok("skip")           │
│ )                                   │
└───────────┬─────────────────────────┘
            │
            v
[Dialog Chiuso - Ritorno a InventoryProcedure]
            │
            v
┌─────────────────────────────────────────────────┐
│ result.Data is string action                    │
│ action == "skip" ✅                             │
├─────────────────────────────────────────────────┤
│ 1. Snackbar.Add(                                │
│      "ℹ️ Prodotto saltato: ABC123"              │
│    )                                            │
│                                                 │
│ 2. AddOperationLog(                             │
│      "Prodotto saltato",                        │
│      "Codice: ABC123",                          │
│      "Info"                                     │
│    )                                            │
│                                                 │
│ 3. ClearProductForm()                           │
│    └─> _scannedBarcode = ""                    │
│    └─> _currentProduct = null                  │
│    └─> Focus su input barcode                  │
└─────────────────────────────────────────────────┘
            │
            v
┌─────────────────────────────────────┐
│ ✅ COMPLETATO                       │
│                                     │
│ • Form pulito                       │
│ • Log aggiornato                    │
│ • Operatore pronto per prossima     │
│   scansione                         │
└─────────────────────────────────────┘

TIMELINE LOG:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⏰ 14:30:01  ℹ️  Ricerca prodotto - Codice: ABC123
⏰ 14:30:02  ⚠️  Prodotto non trovato - Codice: ABC123
⏰ 14:30:05  ℹ️  Prodotto saltato - Codice: ABC123
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

## 🔗 Scenario 2: ASSIGN - Assegna a Prodotto Esistente

```
╔═══════════════════════════════════════════════════════════════╗
║              SCENARIO: ASSIGN TO EXISTING                     ║
╚═══════════════════════════════════════════════════════════════╝

[Dialog Aperto]
     │
     v
[Operatore Cerca "Sedia"]
     │
     v
┌────────────────────────────────────────┐
│ SearchProducts("Sedia")                │
│                                        │
│ Filtra _allProducts dove:             │
│   • Name contains "Sedia" OR          │
│   • Code contains "Sedia" OR          │
│   • Description contains "Sedia"      │
│                                        │
│ Returns: List<ProductDto>             │
└──────────────┬─────────────────────────┘
               │
               v
┌────────────────────────────────────────┐
│ Autocomplete mostra risultati:        │
│ ┌────────────────────────────────┐   │
│ │ • Sedia da Conferenza          │   │
│ │   CHAIR001 - Sedia ergonomica  │   │
│ │                                │   │
│ │ • Sedia Pieghevole             │   │
│ │   CHAIR002 - Sedia eventi      │   │
│ └────────────────────────────────┘   │
└────────────────────────────────────────┘
               │
               v
[Operatore Seleziona "Sedia da Conferenza"]
               │
               v
┌────────────────────────────────────────┐
│ _selectedProduct = ProductDto          │
│ {                                      │
│   Id: <GUID>,                          │
│   Name: "Sedia da Conferenza",        │
│   Code: "CHAIR001",                    │
│   Description: "..."                   │
│ }                                      │
└──────────────┬─────────────────────────┘
               │
               v
┌────────────────────────────────────────┐
│ UI Aggiornata:                         │
│ • Dettagli prodotto visibili          │
│ • Form assegnazione mostrato:         │
│   └─> Tipo Codice: "Barcode"         │
│   └─> Codice: "ABC123"                │
│   └─> Descrizione Alt: ""             │
│ • Pulsante "Assegna" abilitato        │
└──────────────┬─────────────────────────┘
               │
               v
[Operatore Click "Assegna e Continua"]
               │
               v
┌────────────────────────────────────────────────┐
│ AssignBarcodeToProduct()                       │
├────────────────────────────────────────────────┤
│ 1. Validazione form                            │
│    └─> _isFormValid ✅                         │
│    └─> _selectedProduct != null ✅             │
│                                                │
│ 2. Prepara DTO:                                │
│    _createCodeDto.ProductId = <GUID>           │
│    {                                           │
│      ProductId: <GUID>,                        │
│      Code: "ABC123",                           │
│      CodeType: "Barcode",                      │
│      AlternativeDescription: "",               │
│      Status: Active                            │
│    }                                           │
│                                                │
│ 3. API Call:                                   │
│    result = await ProductService               │
│             .CreateProductCodeAsync(dto)       │
│    └─> HTTP POST /api/products/codes          │
│    └─> Body: JSON del DTO                     │
│                                                │
│ 4. Success Handler:                            │
│    └─> Snackbar.Add(                           │
│          "✅ Codice assegnato con successo"    │
│        )                                       │
│    └─> MudDialog.Close(                        │
│          DialogResult.Ok({                     │
│            action: "assigned",                 │
│            product: _selectedProduct           │
│          })                                    │
│        )                                       │
└────────────────┬───────────────────────────────┘
                 │
                 v
[Dialog Chiuso - Ritorno a InventoryProcedure]
                 │
                 v
┌─────────────────────────────────────────────────┐
│ result.Data is NOT string                       │
│ └─> else branch ✅                              │
├─────────────────────────────────────────────────┤
│ // Prodotto assegnato, cerca di nuovo          │
│ await SearchBarcode()                           │
└───────────────┬─────────────────────────────────┘
                │
                v
┌────────────────────────────────────────────────┐
│ SearchBarcode()                                │
├────────────────────────────────────────────────┤
│ _currentProduct = await ProductService         │
│   .GetProductByCodeAsync("ABC123")             │
│                                                │
│ Risultato: SUCCESS ✅                          │
│ └─> Prodotto "Sedia da Conferenza" trovato!   │
│                                                │
│ Snackbar: "✅ Prodotto trovato"                │
│ Log: "Prodotto trovato - Sedia (...)"         │
└───────────────┬────────────────────────────────┘
                │
                v
┌────────────────────────────────────────┐
│ ShowInventoryEntryDialog()             │
├────────────────────────────────────────┤
│ Mostra dialog per inserire quantità   │
└────────────────────────────────────────┘
                │
                v
┌────────────────────────────────────────┐
│ ✅ COMPLETATO                          │
│                                        │
│ • Codice ABC123 → CHAIR001             │
│ • Prodotto caricato                    │
│ • Pronto per conteggio                 │
└────────────────────────────────────────┘

TIMELINE LOG:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⏰ 14:30:01  ℹ️  Ricerca prodotto - Codice: ABC123
⏰ 14:30:02  ⚠️  Prodotto non trovato - Codice: ABC123
⏰ 14:30:15  ✅  Codice assegnato con successo
⏰ 14:30:16  ℹ️  Ricerca prodotto - Codice: ABC123
⏰ 14:30:17  ✅  Prodotto trovato - Sedia da Conferenza
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

DATABASE CHANGES:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
TABLE: ProductCodes
INSERT:
  Id: <NEW_GUID>
  ProductId: <CHAIR001_ID>
  Code: "ABC123"
  CodeType: "Barcode"
  AlternativeDescription: NULL
  Status: Active
  CreatedAt: 2025-01-XX 14:30:15
  CreatedBy: <USER_ID>
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

## ➕ Scenario 3: CREATE - Crea Nuovo Prodotto

```
╔═══════════════════════════════════════════════════════════════╗
║              SCENARIO: CREATE NEW PRODUCT                     ║
╚═══════════════════════════════════════════════════════════════╝

[Dialog Aperto - Nessun prodotto selezionato]
     │
     v
[Operatore Click "Crea Nuovo Prodotto"]
     │
     v
┌─────────────────────────┐
│ SelectAction("create")  │
└───────────┬─────────────┘
            │
            v
┌─────────────────────────────────┐
│ MudDialog.Close(                │
│   DialogResult.Ok("create")     │
│ )                               │
└───────────┬─────────────────────┘
            │
            v
[Dialog Chiuso - Ritorno a InventoryProcedure]
            │
            v
┌─────────────────────────────────────────────────┐
│ result.Data is string action                    │
│ action == "create" ✅                           │
├─────────────────────────────────────────────────┤
│ CreateNewProduct()                              │
└───────────┬─────────────────────────────────────┘
            │
            v
┌────────────────────────────────────────────────┐
│ CreateNewProduct()                             │
├────────────────────────────────────────────────┤
│ _productForDrawer = new ProductDto             │
│ {                                              │
│   Code: "ABC123",  ← PRE-COMPILATO            │
│   Name: "",                                    │
│   Status: Active                               │
│ }                                              │
│                                                │
│ _productDrawerMode = EntityDrawerMode.Create   │
│ _productDrawerOpen = true                      │
└───────────┬────────────────────────────────────┘
            │
            v
┌─────────────────────────────────────────────────┐
│ ProductDrawer SI APRE                           │
├─────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────┐    │
│ │ 📝 Crea Nuovo Prodotto                  │    │
│ ├─────────────────────────────────────────┤    │
│ │                                         │    │
│ │ Codice: [ABC123] ← PRE-COMPILATO       │    │
│ │ Nome:   [___________________________]  │    │
│ │ Desc:   [___________________________]  │    │
│ │         [___________________________]  │    │
│ │ Prezzo: [________]                     │    │
│ │ Marca:  [_________▼]                   │    │
│ │ ...                                     │    │
│ │                                         │    │
│ │ [ Annulla ]  [ Salva ]                 │    │
│ └─────────────────────────────────────────┘    │
└─────────────────────────────────────────────────┘
            │
            v
[Operatore Compila Dati]
   • Nome: "Nuovo Articolo"
   • Descrizione: "Articolo per evento"
   • Prezzo: 25.00
   • Marca: <seleziona>
            │
            v
[Operatore Click "Salva"]
            │
            v
┌────────────────────────────────────────────────┐
│ ProductDrawer - OnSaveClick()                  │
├────────────────────────────────────────────────┤
│ 1. Validazione form                            │
│    └─> _isValid ✅                             │
│                                                │
│ 2. API Call:                                   │
│    result = await ProductService               │
│             .CreateProductAsync(product)       │
│    └─> HTTP POST /api/products                │
│    └─> Body: JSON del ProductDto              │
│                                                │
│ 3. Success Handler:                            │
│    └─> Snackbar: "✅ Prodotto creato"         │
│    └─> OnProductSaved.InvokeAsync(result)     │
│    └─> Drawer chiuso                           │
└───────────┬────────────────────────────────────┘
            │
            v
[Ritorno a InventoryProcedure]
            │
            v
┌─────────────────────────────────────────────────┐
│ HandleProductCreated(createdProduct)            │
├─────────────────────────────────────────────────┤
│ await SearchBarcode()                           │
│ └─> Ricerca prodotto con codice "ABC123"       │
└───────────┬─────────────────────────────────────┘
            │
            v
┌────────────────────────────────────────────────┐
│ SearchBarcode()                                │
├────────────────────────────────────────────────┤
│ _currentProduct = await ProductService         │
│   .GetProductByCodeAsync("ABC123")             │
│                                                │
│ Risultato: SUCCESS ✅                          │
│ └─> Prodotto "Nuovo Articolo" trovato!        │
│                                                │
│ Snackbar: "✅ Prodotto trovato"                │
│ Log: "Prodotto trovato - Nuovo Articolo"      │
└───────────┬────────────────────────────────────┘
            │
            v
┌────────────────────────────────────────┐
│ ShowInventoryEntryDialog()             │
├────────────────────────────────────────┤
│ Mostra dialog per inserire quantità   │
└────────────────────────────────────────┘
            │
            v
┌────────────────────────────────────────┐
│ ✅ COMPLETATO                          │
│                                        │
│ • Nuovo prodotto creato                │
│ • Codice ABC123 assegnato              │
│ • Pronto per conteggio                 │
└────────────────────────────────────────┘

TIMELINE LOG:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⏰ 14:30:01  ℹ️  Ricerca prodotto - Codice: ABC123
⏰ 14:30:02  ⚠️  Prodotto non trovato - Codice: ABC123
⏰ 14:30:45  ✅  Prodotto creato - Nuovo Articolo
⏰ 14:30:46  ℹ️  Ricerca prodotto - Codice: ABC123
⏰ 14:30:47  ✅  Prodotto trovato - Nuovo Articolo
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

DATABASE CHANGES:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
TABLE: Products
INSERT:
  Id: <NEW_GUID>
  Code: "ABC123"
  Name: "Nuovo Articolo"
  Description: "Articolo per evento"
  Price: 25.00
  Status: Active
  CreatedAt: 2025-01-XX 14:30:45
  CreatedBy: <USER_ID>
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

## 🚫 Scenario 4: CANCEL - Annulla

```
╔═══════════════════════════════════════════════════════════════╗
║                  SCENARIO: CANCEL                             ║
╚═══════════════════════════════════════════════════════════════╝

[Dialog Aperto]
     │
     v
[Operatore Click "Annulla" O Preme ESC]
     │
     v
┌─────────────────────────┐
│ Cancel()                │
│ MudDialog.Cancel()      │
└───────────┬─────────────┘
            │
            v
[Dialog Chiuso - Ritorno a InventoryProcedure]
            │
            v
┌─────────────────────────────────────────────────┐
│ result.Canceled == true                         │
│ └─> Nessuna azione eseguita                     │
└─────────────────────────────────────────────────┘
            │
            v
┌────────────────────────────────────┐
│ ✅ COMPLETATO                      │
│                                    │
│ • Nessuna modifica effettuata     │
│ • Form rimane nello stato corrente │
│ • Operatore può ri-scansionare     │
└────────────────────────────────────┘

TIMELINE LOG:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⏰ 14:30:01  ℹ️  Ricerca prodotto - Codice: ABC123
⏰ 14:30:02  ⚠️  Prodotto non trovato - Codice: ABC123
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
(Nessuna altra azione loggata)
```

---

## 📊 Matrice Decisionale

```
┌─────────────────────────────────────────────────────────────┐
│ QUANDO USARE OGNI AZIONE                                    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ ⏭️  SALTA                                                   │
│   ├─ Codice sconosciuto/temporaneo                         │
│   ├─ Non serve tracciarlo nell'inventario                  │
│   ├─ Si vuole continuare velocemente                       │
│   └─ Esempio: Codice danneggiato, campione promozionale    │
│                                                             │
│ 🔗 ASSEGNA A ESISTENTE                                      │
│   ├─ Prodotto esiste ma con codice diverso                 │
│   ├─ Nuovo codice a barre/EAN per prodotto noto            │
│   ├─ Vuoi associare codice fornitore a tuo codice interno  │
│   └─ Esempio: Sedia CHAIR001 ha nuovo EAN 1234567890       │
│                                                             │
│ ➕ CREA NUOVO PRODOTTO                                      │
│   ├─ Prodotto veramente nuovo                              │
│   ├─ Prima volta che lo vedi                               │
│   ├─ Devi tracciarlo in magazzino                          │
│   └─ Esempio: Nuovo articolo da fornitore                  │
│                                                             │
│ ❌ ANNULLA                                                  │
│   ├─ Hai scansionato per errore                            │
│   ├─ Vuoi ri-scansionare                                   │
│   ├─ Devi verificare qualcosa prima                        │
│   └─ Esempio: Errore di lettura barcode                    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## ⚙️ Componenti Tecnici Coinvolti

```
┌──────────────────────────────────────────────────────────────┐
│                      STACK TECNOLOGICO                       │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│ 🎨 UI Layer                                                  │
│   ├─ InventoryProcedure.razor                               │
│   └─ ProductNotFoundDialog.razor                            │
│                                                              │
│ 🧩 Components                                                │
│   ├─ MudDialog                                              │
│   ├─ MudAutocomplete                                        │
│   ├─ MudForm                                                │
│   ├─ MudSnackbar                                            │
│   └─ EntityDrawer (ProductDrawer)                           │
│                                                              │
│ 🔧 Services                                                  │
│   ├─ IProductService                                        │
│   │   ├─ GetProductByCodeAsync()                           │
│   │   ├─ GetProductsAsync()                                │
│   │   ├─ CreateProductAsync()                              │
│   │   └─ CreateProductCodeAsync()                          │
│   │                                                          │
│   ├─ ITranslationService                                    │
│   │   └─ GetTranslation()                                  │
│   │                                                          │
│   ├─ ISnackbar                                              │
│   │   └─ Add()                                              │
│   │                                                          │
│   └─ ILogger<T>                                             │
│       └─ LogError()                                         │
│                                                              │
│ 📦 DTOs                                                      │
│   ├─ ProductDto                                             │
│   └─ CreateProductCodeDto                                   │
│                                                              │
│ 🌐 API                                                       │
│   ├─ GET  /api/products/by-code/{code}                     │
│   ├─ GET  /api/products?page={}&pageSize={}                │
│   ├─ POST /api/products                                     │
│   └─ POST /api/products/codes                              │
│                                                              │
│ 💾 Database                                                  │
│   ├─ Products Table                                         │
│   └─ ProductCodes Table                                     │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

## 🎯 Punti Chiave dell'Implementazione

### 1. Contesto Condizionale
```csharp
[Parameter]
public bool IsInventoryContext { get; set; } = false;
```
- Determina quale UI mostrare
- Durante inventario: mostra "Salta"
- Fuori inventario: mostra "Crea" come prima opzione

### 2. Ricerca Integrata
```csharp
private async Task<IEnumerable<ProductDto>> SearchProducts(string value)
{
    return _allProducts
        .Where(p => p.Name.Contains(value) ||
                   p.Code.Contains(value) ||
                   p.Description.Contains(value))
        .Take(20);
}
```
- Ricerca real-time
- Cerca in tutti i campi rilevanti
- Limite 20 risultati per performance

### 3. Assegnazione Atomica
```csharp
var result = await ProductService.CreateProductCodeAsync(_createCodeDto);
if (result != null)
{
    // Success
    MudDialog.Close(DialogResult.Ok(new { action = "assigned", ... }));
}
```
- Singola chiamata API
- Transazione database
- Feedback immediato

### 4. Re-search Pattern
```csharp
// Dopo assegnazione o creazione
await SearchBarcode();
```
- Verifica che codice sia ora trovato
- Carica prodotto fresco da DB
- Continua workflow inventario

---

## 📈 Performance e Ottimizzazioni

### Caricamento Prodotti
```
┌──────────────────────────────┐
│ OnInitializedAsync()         │
│ └─> LoadProducts()           │
│     └─> Get 100 prodotti     │ ← Cache locale
│         └─> _allProducts     │
└──────────────────────────────┘
```
- Caricamento iniziale di 100 prodotti
- Cache locale per autocomplete veloce
- Evita chiamate API ripetute durante ricerca

### Ricerca Client-Side
```
User digita → Filtra _allProducts → Mostra risultati
            ← Nessuna chiamata API! ←
```
- Ricerca istantanea
- Nessun network overhead
- UX fluida

---

## 🎓 Apprendimenti e Best Practices

### ✅ Cosa Funziona Bene

1. **Single Dialog Workflow**
   - Non serve navigare tra più dialog
   - Tutto visibile in una schermata
   - Riduce cognitive load

2. **Context-Aware UI**
   - `IsInventoryContext` permette customizzazione
   - Stesso componente, usi diversi
   - Codice riutilizzabile

3. **Immediate Feedback**
   - Snackbar per ogni azione
   - Log timeline visibile
   - Utente sempre informato

4. **Error Handling Robusto**
   - Try-catch su tutte le API calls
   - Logging dettagliato
   - Messaggi user-friendly

5. **Re-search Pattern**
   - Dopo assegnazione → ricerca automatica
   - Workflow continua senza interruzioni
   - Verifica che operazione sia riuscita

### 🎯 Pattern Applicati

```
┌──────────────────────────────────────────────┐
│ PATTERN: DIALOG RESULT                       │
├──────────────────────────────────────────────┤
│ • Dialog ritorna risultato tipizzato         │
│ • Chiamante decide come gestirlo             │
│ • Separazione responsabilità                 │
└──────────────────────────────────────────────┘

┌──────────────────────────────────────────────┐
│ PATTERN: SEARCH-THEN-ACT                     │
├──────────────────────────────────────────────┤
│ 1. Cerca prodotto                            │
│ 2. Trovato → entry dialog                    │
│ 3. Non trovato → gestione caso               │
│ 4. Dopo azione → ri-cerca                    │
└──────────────────────────────────────────────┘

┌──────────────────────────────────────────────┐
│ PATTERN: PROGRESSIVE DISCLOSURE              │
├──────────────────────────────────────────────┤
│ • Form assegnazione appare solo se serve     │
│ • Dettagli prodotto solo se selezionato     │
│ • UI si adatta al contesto                   │
└──────────────────────────────────────────────┘
```

---

**DOCUMENTO COMPLETATO** ✅

*Questo documento illustra tutti i flussi possibili per l'assegnazione di un codice durante la procedura di inventario.*
