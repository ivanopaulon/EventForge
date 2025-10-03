# Procedura di Inventario tramite Documento - Guida all'Implementazione

## Panoramica

Questo documento spiega la nuova procedura di inventario che consente di creare un unico documento inventariale e di aggiungere multiple righe man mano che i prodotti vengono scansionati e contati.

## Contesto

La procedura di inventario precedente creava singole voci di inventario per ogni scansione di prodotto, senza un documento unificato per tracciare l'intera sessione di inventario. La nuova procedura risolve questo problema:

1. Creando un unico documento di inventario all'inizio della sessione
2. Aggiungendo righe a questo documento man mano che i prodotti vengono scansionati
3. Finalizzando il documento quando l'inventario è completato

## Endpoint API

### 1. Avvio Documento di Inventario

**Endpoint:** `POST /api/v1/warehouse/inventory/document/start`

Crea un nuovo documento di inventario per tracciare la sessione di inventario.

**Request Body:**
```json
{
  "warehouseId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "inventoryDate": "2025-01-15T10:00:00Z",
  "notes": "Inventario Fisico Q1 2025",
  "series": "INV",
  "number": "INV-001"
}
```

**Campi:**
- `warehouseId` (opzionale): Magazzino dove viene condotto l'inventario
- `inventoryDate` (obbligatorio): Data dell'inventario
- `notes` (opzionale): Note su questa sessione di inventario
- `series` (opzionale): Serie del documento per la numerazione
- `number` (opzionale): Numero del documento (generato automaticamente se non fornito)

**Response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "number": "INV-20250115-100000",
  "series": "INV",
  "inventoryDate": "2025-01-15T10:00:00Z",
  "warehouseId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "warehouseName": "Magazzino Principale",
  "status": "Draft",
  "notes": "Inventario Fisico Q1 2025",
  "createdAt": "2025-01-15T10:00:00Z",
  "createdBy": "mario.rossi",
  "finalizedAt": null,
  "finalizedBy": null,
  "rows": [],
  "totalItems": 0
}
```

### 2. Aggiungi Riga al Documento di Inventario

**Endpoint:** `POST /api/v1/warehouse/inventory/document/{documentId}/row`

Aggiunge una riga di conteggio prodotto a un documento di inventario esistente.

**Request Body:**
```json
{
  "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "locationId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "quantity": 95,
  "lotId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
  "notes": "Alcuni articoli danneggiati"
}
```

**Campi:**
- `productId` (obbligatorio): Prodotto che viene contato
- `locationId` (obbligatorio): Ubicazione di magazzino dove si trova il prodotto
- `quantity` (obbligatorio): Quantità contata
- `lotId` (opzionale): Identificatore del lotto se applicabile
- `notes` (opzionale): Note su questo conteggio specifico

**Response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "number": "INV-20250115-100000",
  "series": "INV",
  "inventoryDate": "2025-01-15T10:00:00Z",
  "warehouseId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "warehouseName": "Magazzino Principale",
  "status": "Draft",
  "notes": "Inventario Fisico Q1 2025",
  "createdAt": "2025-01-15T10:00:00Z",
  "createdBy": "mario.rossi",
  "rows": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa9",
      "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "productName": "Prodotto XYZ",
      "productCode": "PRD-001",
      "locationId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
      "locationName": "A-01-01",
      "quantity": 95,
      "previousQuantity": 100,
      "adjustmentQuantity": -5,
      "lotId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
      "lotCode": "LOT-2025-001",
      "notes": "Alcuni articoli danneggiati",
      "createdAt": "2025-01-15T10:05:00Z",
      "createdBy": "mario.rossi"
    }
  ],
  "totalItems": 1
}
```

### 3. Finalizza Documento di Inventario

**Endpoint:** `POST /api/v1/warehouse/inventory/document/{documentId}/finalize`

Finalizza il documento di inventario e applica tutti gli aggiustamenti di stock.

**Response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "number": "INV-20250115-100000",
  "series": "INV",
  "inventoryDate": "2025-01-15T10:00:00Z",
  "warehouseId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "warehouseName": "Magazzino Principale",
  "status": "Closed",
  "notes": "Inventario Fisico Q1 2025",
  "createdAt": "2025-01-15T10:00:00Z",
  "createdBy": "mario.rossi",
  "finalizedAt": "2025-01-15T11:00:00Z",
  "finalizedBy": "mario.rossi",
  "rows": [
    // ... tutte le righe
  ],
  "totalItems": 25
}
```

## Esempio di Workflow

### Passo 1: Avvia Sessione di Inventario

```bash
POST /api/v1/warehouse/inventory/document/start
Content-Type: application/json

{
  "warehouseId": "warehouse-guid",
  "inventoryDate": "2025-01-15T10:00:00Z",
  "notes": "Inventario Fisico Q1 2025"
}
```

**Risultato:** Ritorna documento di inventario con ID `doc-guid-123`

### Passo 2: Scansiona e Conta i Prodotti

Per ogni prodotto scansionato:

```bash
POST /api/v1/warehouse/inventory/document/doc-guid-123/row
Content-Type: application/json

{
  "productId": "product-guid-1",
  "locationId": "location-guid-1",
  "quantity": 95
}
```

Ripeti per ogni prodotto. Ogni chiamata aggiunge una nuova riga al documento.

### Passo 3: Completa l'Inventario

Quando tutti i prodotti sono stati contati:

```bash
POST /api/v1/warehouse/inventory/document/doc-guid-123/finalize
```

Questo chiude il documento e processa tutti gli aggiustamenti di inventario.

## Dettagli Tecnici di Implementazione

### Struttura del Documento

Il documento di inventario è implementato usando le entità `DocumentHeader` e `DocumentRow` esistenti:

- **DocumentHeader**: Rappresenta la sessione di inventario
  - DocumentType: "INVENTORY" (creato automaticamente)
  - Status: "Draft" → "Closed"
  - BusinessPartyId: Parte aziendale di sistema (creata automaticamente)

- **DocumentRow**: Rappresenta ogni conteggio di prodotto
  - ProductCode: Codice prodotto scansionato
  - Description: Nome prodotto e ubicazione
  - Quantity: Quantità contata

### Entità Create Automaticamente

Il sistema crea automaticamente:

1. **Tipo Documento Inventario**: 
   - Code: "INVENTORY"
   - Name: "Inventory Document"
   - Creato una volta per tenant al primo utilizzo

2. **Business Party di Sistema**:
   - Name: "System Internal"
   - Type: Cliente (Customer)
   - Usato per operazioni interne

### Vantaggi

1. **Documento Unico**: Tutti i conteggi di inventario sono tracciati in un unico posto
2. **Audit Trail**: Storico completo di quando e chi ha eseguito l'inventario
3. **Conteggio Incrementale**: Aggiungi prodotti uno alla volta man mano che vengono scansionati
4. **Revisione Prima della Finalizzazione**: Possibilità di rivedere tutti i conteggi prima di applicare gli aggiustamenti
5. **Gestione Documentale**: Sfrutta l'infrastruttura documentale esistente

## Confronto con l'Approccio Precedente

### Vecchio Approccio (Singola Voce)
```
POST /api/v1/warehouse/inventory
→ Crea una singola voce di inventario
→ Applica immediatamente l'aggiustamento di stock
→ Nessun documento unificato
```

### Nuovo Approccio (Basato su Documento)
```
1. POST /api/v1/warehouse/inventory/document/start
   → Crea il documento di inventario

2. POST /api/v1/warehouse/inventory/document/{id}/row (più volte)
   → Aggiunge righe al documento
   → Calcola gli aggiustamenti ma non li applica ancora

3. POST /api/v1/warehouse/inventory/document/{id}/finalize
   → Chiude il documento
   → Applica tutti gli aggiustamenti di stock in una volta
```

## Note di Integrazione

### Per lo Sviluppo Frontend

1. **Avvia Sessione**: Crea il documento quando l'utente inizia l'inventario
2. **Scansiona Prodotti**: Aggiungi righe man mano che i codici a barre vengono scansionati
3. **Visualizza Progresso**: Mostra il documento con tutte le righe aggiunte
4. **Revisiona**: Permetti all'utente di rivedere i conteggi prima di finalizzare
5. **Completa**: Finalizza il documento per applicare gli aggiustamenti

### Gestione degli Errori

- Se la creazione del documento fallisce, nessun dato di inventario viene perso
- Se l'aggiunta di una riga fallisce, si può ritentare senza influenzare le altre righe
- Se la finalizzazione fallisce, il documento rimane in stato Draft e può essere ritentata

## Miglioramenti Futuri

Possibili miglioramenti da considerare:

1. **Modifica Righe**: Permettere la modifica delle quantità prima della finalizzazione
2. **Elimina Righe**: Rimuovere articoli scansionati per errore
3. **Finalizzazione Parziale**: Applicare aggiustamenti per righe specifiche
4. **Template di Documento**: Pre-configurare le impostazioni di inventario
5. **Integrazione Barcode**: API diretta per scansione codici a barre
6. **App Mobile**: App mobile dedicata per l'inventario

## Documentazione Correlata

- [INVENTORY_PROCEDURE_EXPLANATION.md](INVENTORY_PROCEDURE_EXPLANATION.md) - Procedura di inventario precedente
- [INVENTORY_PROCEDURE_TECHNICAL_SUMMARY.md](INVENTORY_PROCEDURE_TECHNICAL_SUMMARY.md) - Riepilogo tecnico
- [INVENTORY_DOCUMENT_WORKFLOW.md](INVENTORY_DOCUMENT_WORKFLOW.md) - Versione inglese di questo documento
