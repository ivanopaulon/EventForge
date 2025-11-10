# Miglioramenti Dialog di Creazione Rapida Prodotto

## Panoramica
Questo documento descrive i miglioramenti apportati al dialog `AdvancedQuickCreateProductDialog` per replicare la funzionalità di gestione delle unità di misura e dei codici a barre presenti nella pagina `ProductDetail`.

## Problema Originale
Nel dialog di aggiunta rapida di un prodotto utilizzato nella procedura di inventario, mancava la stessa sofisticazione presente in `ProductDetail` per la gestione della selezione dell'unità di misura base e alternative e dei codici a barre alternativi.

## Soluzione Implementata

### 1. Separazione Unità e Codici
**Prima**: Un singolo pannello espandibile gestiva sia codici che unità insieme.

**Dopo**: Due pannelli espandibili separati:
- **Unità Alternative**: Gestione dedicata delle unità di misura alternative
- **Codici Alternativi**: Gestione dedicata dei codici a barre alternativi

**Beneficio**: Migliore organizzazione che replica la struttura a tab di ProductDetail.

### 2. Tipo Unità con Dropdown
**Prima**: Campo di testo libero per il tipo di unità.

**Dopo**: Menu a tendina con valori predefiniti:
- Base
- Confezione (Pack)
- Scatola (Box)
- Pallet
- Container

**Beneficio**: Coerenza con ProductDetail, previene errori di battitura, garantisce qualità dei dati.

### 3. Associazione Codice-Unità
**Prima**: I codici avevano campi per unità ma senza associazione chiara.

**Dopo**: I codici possono essere esplicitamente associati a:
- Unità base del prodotto
- Qualsiasi unità alternativa configurata
- Nessuna unità (opzionale)

**Beneficio**: Relazione chiara tra codici a barre e unità di confezionamento (es. "questo codice a barre è per la confezione da 12 pezzi").

### 4. Gestione dello Stato
Sia le unità che i codici hanno ora campi di stato espliciti:
- Attivo
- Sospeso

Questo replica la gestione degli stati presente in ProductDetail.

### 5. Validazione Migliorata
- Verifica che tutte le unità abbiano un'unità di misura selezionata
- Verifica che i fattori di conversione siano >= 0.001
- Verifica che tutti i codici abbiano un valore
- Gestisce correttamente l'eliminazione di unità e aggiorna i codici associati

## Dettagli Tecnici

### File Modificato
- `EventForge.Client/Shared/Components/Dialogs/AdvancedQuickCreateProductDialog.razor`
  - 378 righe modificate: +308 aggiunte, -70 rimosse

### Nuove Classi Interne
```csharp
private class AlternativeUnitModel
{
    public string UnitType { get; set; } = "Pack";
    public Guid? UnitOfMeasureId { get; set; }
    public decimal ConversionFactor { get; set; } = 1m;
    public string? Description { get; set; }
    public string Status { get; set; } = "Active";
}

private class AlternativeCodeModel
{
    public string CodeType { get; set; } = "Barcode";
    public string Code { get; set; } = string.Empty;
    public string? AlternativeDescription { get; set; }
    public string Status { get; set; } = "Active";
    public int? AssociatedUnitIndex { get; set; }
}
```

### Nuovi Metodi
- `AddAlternativeUnit()`: Aggiunge una nuova unità alternativa
- `RemoveAlternativeUnit(int index)`: Rimuove un'unità e aggiorna le associazioni dei codici
- `AddAlternativeCode()`: Aggiunge un nuovo codice alternativo
- `RemoveAlternativeCode(int index)`: Rimuove un codice alternativo
- `GetUnitOfMeasureName(Guid? umId)`: Helper per visualizzare i nomi delle unità di misura

### Logica Avanzata
- Conversione intelligente da unità/codici separati al formato combinato `ProductCodeWithUnitDto`
- Generazione automatica di codici interni per unità senza codici espliciti
- Tracciamento delle associazioni basato su indice con aggiustamento automatico in caso di eliminazione

## Esempio di Utilizzo

### Scenario: Creazione Prodotto con Confezioni Multiple

1. **Prodotto Base**: Bottiglia d'acqua singola
   - Codice: WATER001
   - Unità Base: PZ (Pezzo)

2. **Unità Alternative**:
   - Confezione da 6: 
     - Tipo: Pack
     - Fattore conversione: 6
     - UoM: PZ
   - Scatola da 24:
     - Tipo: Box
     - Fattore conversione: 24
     - UoM: PZ

3. **Codici Alternativi**:
   - Barcode confezione da 6: 
     - Codice: 8012345678901
     - Tipo: EAN
     - Associato a: Pack (x6)
   - Barcode scatola da 24:
     - Codice: 8012345678918
     - Tipo: EAN
     - Associato a: Box (x24)

## Compatibilità
- ✅ Mantiene piena compatibilità con il flusso esistente della procedura di inventario
- ✅ Non introduce breaking changes
- ✅ Build compilato senza nuovi errori o warning
- ✅ Test esistenti passano correttamente

## Sicurezza
- ✅ Analisi CodeQL completata: nessuna vulnerabilità trovata
- ✅ Validazione corretta di tutti gli input utente
- ✅ Segue le pratiche di codifica sicura

## Conclusioni
I miglioramenti implementati portano il dialog di creazione rapida prodotto allo stesso livello di funzionalità della pagina ProductDetail, fornendo un'esperienza utente coerente e migliorando la qualità dei dati inseriti durante la procedura di inventario.
