# Procedura di Inventario - Spiegazione Tecnica

## Domanda
**Analizziamo ora la procedura di inventario, verifica lato server: quando inseriremo una quantità cosa stiamo facendo di preciso? Creiamo un documento? Valorizziamo solo il magazzino?**

## Risposta Dettagliata

Quando si inserisce una quantità durante la procedura di inventario attraverso l'endpoint `POST /api/v1/warehouse/inventory`, il sistema esegue le seguenti operazioni:

### 1. **Recupero dello Stock Corrente**
```csharp
// Recupera il livello di stock attuale per il prodotto/ubicazione/lotto
var existingStocks = await _stockService.GetStockAsync(
    productId: createDto.ProductId,
    locationId: createDto.LocationId,
    lotId: createDto.LotId);
```

### 2. **Calcolo della Differenza di Inventario**
```csharp
var currentQuantity = existingStock?.Quantity ?? 0;
var countedQuantity = createDto.Quantity;  // Quantità contata fisicamente
var adjustmentQuantity = countedQuantity - currentQuantity;
```

Il sistema calcola automaticamente la differenza tra:
- **Quantità Corrente**: Quella registrata nel sistema
- **Quantità Contata**: Quella effettivamente rilevata durante l'inventario fisico

### 3. **Creazione del Documento di Movimento (StockMovement)**
```csharp
if (adjustmentQuantity != 0)
{
    await _stockMovementService.ProcessAdjustmentMovementAsync(
        productId: createDto.ProductId,
        locationId: createDto.LocationId,
        adjustmentQuantity: adjustmentQuantity,
        reason: adjustmentQuantity > 0 
            ? "Inventory Count - Found Additional Stock" 
            : "Inventory Count - Stock Shortage Detected",
        lotId: createDto.LotId,
        notes: createDto.Notes,
        currentUser: GetCurrentUser());
}
```

**Sì, viene creato un documento!** Specificamente:
- Viene creato un record `StockMovement` con tipo `Adjustment`
- Questo documento fornisce la completa tracciabilità dell'operazione di inventario
- Il documento include:
  - Prodotto, ubicazione, lotto (se applicabile)
  - Quantità di aggiustamento (positiva o negativa)
  - Motivo dell'aggiustamento (scorta aggiuntiva trovata o mancanza rilevata)
  - Data/ora dell'operazione
  - Utente che ha eseguito l'operazione
  - Note opzionali

### 4. **Valorizzazione del Magazzino (Stock)**
```csharp
var stock = await _stockService.CreateOrUpdateStockAsync(createStockDto, GetCurrentUser());
```

Il sistema aggiorna o crea il record di stock con:
- La nuova quantità contata (sostituisce la vecchia)
- Note associate all'inventario
- Metadati di auditing (chi ha modificato, quando)

### 5. **Aggiornamento della Data di Ultimo Inventario**
```csharp
await _stockService.UpdateLastInventoryDateAsync(stock.Id, DateTime.UtcNow);
```

Il campo `LastInventoryDate` viene aggiornato per tracciare quando è stato effettuato l'ultimo conteggio fisico.

## Riepilogo: Cosa Succede Esattamente

### ✅ **SI, viene creato un documento**
- Un record `StockMovement` di tipo `Adjustment` viene creato per tracciare la correzione
- Questo fornisce audit trail completo e tracciabilità

### ✅ **SI, viene valorizzato il magazzino**
- Il record `Stock` viene aggiornato con la quantità contata
- La quantità viene sostituita con quella rilevata fisicamente
- Il campo `LastInventoryDate` viene impostato

## Esempio Pratico

### Scenario: Inventario Prodotto XYZ in Ubicazione A

**Stato Iniziale:**
```
Stock.Quantity = 100 unità (nel sistema)
```

**Operazione di Inventario:**
```
Quantità contata fisicamente = 95 unità
```

**Risultato:**

1. **StockMovement creato:**
   ```
   MovementType: Adjustment
   Quantity: 5
   FromLocationId: A (movimento negativo)
   Reason: "Inventory Count - Stock Shortage Detected"
   Notes: "Inventario periodico Q1 2025"
   MovementDate: 2025-01-15 10:30:00
   CreatedBy: "mario.rossi"
   ```

2. **Stock aggiornato:**
   ```
   Stock.Quantity = 95 unità (aggiornato)
   Stock.LastInventoryDate = 2025-01-15 10:30:00
   Stock.ModifiedBy = "mario.rossi"
   Stock.ModifiedAt = 2025-01-15 10:30:00
   ```

## Vantaggi di Questo Approccio

1. **Tracciabilità Completa**: Ogni regolazione di inventario è documentata
2. **Audit Trail**: Si può risalire a chi, quando e perché è stata effettuata una modifica
3. **Storico Movimenti**: Tutti i movimenti di aggiustamento sono consultabili
4. **Conformità**: Supporta requisiti di conformità e controllo qualità
5. **Analisi**: Permette di analizzare discrepanze tra stock teorico e reale

## API Endpoint

```
POST /api/v1/warehouse/inventory
```

**Request Body:**
```json
{
  "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "locationId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "quantity": 95,
  "lotId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
  "notes": "Inventario periodico Q1 2025"
}
```

**Response:**
```json
{
  "id": "stock-id",
  "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "productName": "Prodotto XYZ",
  "productCode": "PRD-XYZ",
  "locationId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "locationName": "A-01-01",
  "quantity": 95,
  "lotId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
  "lotCode": "LOT-2025-001",
  "notes": "Inventario periodico Q1 2025",
  "createdAt": "2025-01-15T10:30:00Z",
  "createdBy": "mario.rossi"
}
```

## Entità Coinvolte

### 1. Stock (Entità Principale del Magazzino)
- Memorizza la quantità corrente per prodotto/ubicazione/lotto
- Include soglie minime/massime
- Traccia data ultimo inventario
- Gestisce quantità riservate vs disponibili

### 2. StockMovement (Documento di Movimento)
- Registra ogni movimento di magazzino
- Tipi: Inbound, Outbound, Transfer, **Adjustment**, Return, etc.
- Include quantità, ubicazioni, motivo, note
- Fornisce tracciabilità completa

### 3. AuditLog (Log di Audit Automatico)
- Traccia automaticamente le modifiche alle entità
- Chi, cosa, quando, valori prima/dopo

## Conclusione

**La procedura di inventario NON si limita a valorizzare il magazzino**, ma:

1. ✅ **Crea un documento formale** (StockMovement) per ogni aggiustamento
2. ✅ **Aggiorna il magazzino** con le quantità contate
3. ✅ **Registra la data di inventario** per tracking
4. ✅ **Fornisce audit trail completo** per conformità
5. ✅ **Permette analisi storiche** delle discrepanze

Questo approccio garantisce la massima trasparenza e tracciabilità delle operazioni di inventario, essenziale per la gestione professionale del magazzino e la conformità normativa.
