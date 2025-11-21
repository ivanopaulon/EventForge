# Fix Completo: Calcolo Quantità Inventario con Unità Alternative

## Problema Originale

In PR #621 è stato fatto un tentativo di correggere il calcolo delle quantità quando si scansiona un barcode associato a un'unità alternativa (es. "Confezione da 6" con fattore di conversione 6). Il bug rimaneva nelle scansioni ripetute:

- **Prima scansione con fattore=6**: quantità = 1 × 6 = 6 ✓ (corretto)
- **Seconda scansione**: NewQuantity = 6 + 1 = 7, poi 7 × 6 = 42 ✗ (sbagliato! dovrebbe essere 12)

### Causa Principale

`InventoryFastService.HandleBarcodeScanned()` incrementava di 1 invece che del fattore di conversione, poi il chiamante moltiplicava il risultato causando un calcolo errato.

## Soluzione Implementata

**Principio fondamentale**: L'inventario deve SEMPRE usare quantità in UNITÀ BASE.

Quando si utilizza un'unità alternativa:
1. Convertire immediatamente in unità base moltiplicando per il fattore di conversione
2. Incrementare di `conversionFactor` unità nelle scansioni ripetute (non di 1)
3. Visualizzare sempre le quantità in unità base

## Modifiche Implementate

### 1. Procedura FAST (InventoryProcedureFast.razor)

**File modificati:**
- `IInventoryFastService.cs`: Aggiunto parametro `conversionFactor` al metodo `HandleBarcodeScanned()`
- `InventoryFastService.cs`:
  - Incrementa di `conversionFactor` invece di 1
  - Restituisce quantità sempre in unità base
- `InventoryProcedureFast.razor`:
  - Passa `_currentConversionFactor` al service
  - Rimossa moltiplicazione errata dopo la chiamata al service
  - Quantità già corretta dal service
- `InventoryFastServiceTests.cs`: Aggiunti 4 nuovi test per unità alternative

### 2. Procedura STANDARD (InventoryProcedure.razor)

**File modificati:**
- `InventoryProcedure.razor`:
  - Aggiunto campo `_currentConversionFactor`
  - Calcola il fattore di conversione dopo il lookup del prodotto
  - Passa il fattore di conversione a `InventoryEntryDialog`
  - Resetta il fattore di conversione in `ClearProductForm()`
  - Logga quando viene rilevata un'unità alternativa
- `InventoryEntryDialog.razor`:
  - Accetta parametro `ConversionFactor`
  - Pre-compila la quantità con il fattore di conversione (unità base)
  - Mostra alert di avviso quando viene rilevata unità alternativa
  - Aggiorna l'etichetta del campo quantità per indicare "(unità base)"

## Allineamento tra le Due Procedure

Entrambe le procedure ora hanno **funzionalità allineata** per le unità alternative:

### Somiglianze (Comportamenti Allineati)
- ✓ Entrambe usano `GetProductWithCodeByCodeAsync`
- ✓ Entrambe calcolano il fattore di conversione da ProductUnit
- ✓ Entrambe lavorano SEMPRE in unità base
- ✓ Entrambe pre-compilano le quantità già convertite
- ✓ Entrambe resettano il fattore di conversione alla pulizia

### Differenze (Solo nel Workflow UI)

**Procedura Standard:**
- Usa un dialog (`MudDialog`) per ogni inserimento
- Mostra alert nel dialog quando c'è unità alternativa
- L'utente può modificare la quantità prima di confermare
- Non gestisce scansioni ripetute

**Procedura Fast:**
- Usa form inline (nessun dialog)
- Gestisce scansioni ripetute tramite il service
- Auto-merge di righe esistenti (stesso prodotto + ubicazione)
- Se Fast Confirm è abilitato, conferma automaticamente

## Esempio di Comportamento

### Scenario: Confezione da 6 pezzi (ConversionFactor = 6)

**Prima scansione:**
- Standard: Dialog mostra "Quantità: 6 (unità base)" pre-compilato
- Fast: Form mostra "6" pre-compilato
- Risultato: Riga inventario con Quantity = 6 ✓

**Seconda scansione (solo Fast):**
- Service incrementa: 6 + 6 = 12
- Risultato: Riga inventario aggiornata con Quantity = 12 ✓

**Terza scansione (solo Fast):**
- Service incrementa: 12 + 6 = 18
- Risultato: Riga inventario aggiornata con Quantity = 18 ✓

### Scenario: Unità Base (ConversionFactor = 1)

Il comportamento rimane invariato:
- Prima scansione: Quantity = 1
- Seconda scansione (Fast): Quantity = 2
- Terza scansione (Fast): Quantity = 3

## Test Coverage

**Test Automatizzati:**
- 24 test in `InventoryFastServiceTests.cs`
- 4 nuovi test specifici per unità alternative:
  1. `HandleBarcodeScanned_WithAlternativeUnit_FirstScan_ConvertsToBaseUnits`
  2. `HandleBarcodeScanned_WithAlternativeUnit_RepeatedScan_IncrementsCorrectly`
  3. `HandleBarcodeScanned_WithAlternativeUnit_ThirdScan_IncrementsCorrectly`
  4. `HandleBarcodeScanned_WithBaseUnit_BehavesAsExpected`

**Risultati:**
```
Test Run Successful.
Total tests: 24
     Passed: 24
 Total time: 1.2595 Seconds
```

## Sicurezza e Compatibilità

- ✓ Tutte le modifiche sono **client-side** (come richiesto)
- ✓ **Nessuna modifica** al database o ai servizi server
- ✓ **Backward compatible** con unità base (factor = 1)
- ✓ **Nessun breaking change** per i dati esistenti
- ✓ Build completato con successo (0 errori)

## Conclusione

Il problema è stato risolto completamente in **entrambe** le procedure di inventario:

1. ✅ Calcolo corretto del fattore di conversione
2. ✅ Quantità sempre in unità base
3. ✅ Incremento corretto nelle scansioni ripetute (Fast)
4. ✅ Feedback visivo chiaro all'utente
5. ✅ Test completi e passanti
6. ✅ Procedure allineate nelle funzionalità core
7. ✅ Solo modifiche client-side

Le quantità vengono ora **sempre visualizzate e salvate in unità base**, come richiesto nel problema originale.
