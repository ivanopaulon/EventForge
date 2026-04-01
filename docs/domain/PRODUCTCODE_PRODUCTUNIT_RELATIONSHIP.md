# ProductCode-ProductUnit Relationship Implementation

## Problem Statement

Il problema identificato era che un prodotto può avere più codici a barre, ognuno associato a un'unità di misura diversa. Ad esempio:
- Un prodotto può essere venduto singolarmente con un codice a barre (unità base)
- Lo stesso prodotto può essere venduto in confezione da 6 pezzi con un altro codice a barre (unità confezione)

Precedentemente, `ProductCode` (codici a barre/SKU) e `ProductUnit` (unità di misura con fattori di conversione) erano entità separate senza collegamento diretto.

## Solution Implemented

### Database Changes

1. **ProductCode Entity**: Aggiunto campo opzionale `ProductUnitId` (Guid?)
   - Foreign key verso `ProductUnit`
   - Relazione opzionale (nullable)
   - Delete behavior: `SetNull` (se l'unità viene eliminata, il codice rimane ma il link viene rimosso)

2. **Migration**: `20251005233556_AddProductUnitIdToProductCode`
   - Aggiunge colonna `ProductUnitId` alla tabella `ProductCodes`
   - Crea indice per la foreign key
   - Configura la relazione con `ON DELETE SET NULL`

### DTOs Updated

- `ProductCodeDto`: Aggiunto `ProductUnitId` (Guid?)
- `CreateProductCodeDto`: Aggiunto `ProductUnitId` (Guid?)
- `UpdateProductCodeDto`: Aggiunto `ProductUnitId` (Guid?)

### Server-Side Changes

#### ProductService
- `AddProductCodeAsync`: Ora include `ProductUnitId` durante la creazione
- `UpdateProductCodeAsync`: Ora include `ProductUnitId` durante l'aggiornamento
- `MapToProductCodeDto`: Include `ProductUnitId` e `Status` nella mappatura

#### ProductManagementController
Aggiunti nuovi endpoint per la gestione di ProductUnit:

```
GET    /api/v1/product-management/products/{productId}/units
POST   /api/v1/product-management/products/{productId}/units
PUT    /api/v1/product-management/products/units/{id}
DELETE /api/v1/product-management/products/units/{id}
```

Questi endpoint erano mancanti e necessari per la gestione completa delle unità di misura prodotto.

### Client-Side Changes

#### ProductService
- Aggiornati endpoint per `ProductUnit` per corrispondere ai nuovi percorsi del controller
- `CreateProductUnitAsync`, `UpdateProductUnitAsync`, `DeleteProductUnitAsync` ora usano i percorsi corretti

#### ProductDrawer Component
- Aggiunta colonna "Unità di Misura" nelle tabelle ProductCode (modalità edit e view)
- Nuovo metodo helper `GetProductUnitDescription()` che mostra:
  - Tipo unità (Base, Pack, Pallet, etc.)
  - Unità di misura con simbolo
  - Fattore di conversione
  - Formato: "Pack (PZ (pz) x 6.00)"

## Use Cases

### Scenario 1: Prodotto con Unità Base
```
Product: Bottiglia d'Acqua
ProductCode: "8001234567890" (EAN-13)
ProductUnit: Base (PZ x 1.00)
```

### Scenario 2: Stesso Prodotto in Confezione
```
Product: Bottiglia d'Acqua
ProductCode: "8001234567999" (EAN-13)
ProductUnit: Pack (CF (conf) x 6.00)
```

### Scenario 3: Stesso Prodotto su Pallet
```
Product: Bottiglia d'Acqua
ProductCode: "18001234567897" (EAN-14)
ProductUnit: Pallet (PLT (plt) x 144.00)
```

## Benefits

1. **Scansione Automatica**: Quando si scansiona un codice a barre, il sistema può:
   - Identificare il prodotto
   - Determinare automaticamente l'unità di misura corretta
   - Applicare il fattore di conversione appropriato

2. **Gestione Magazzino**: Facilita la gestione di:
   - Carichi merce in pallet
   - Vendite al dettaglio di pezzi singoli
   - Vendite di confezioni multiple

3. **Flessibilità**: Il collegamento è opzionale:
   - Un codice può non avere un'unità specifica (usa l'unità base del prodotto)
   - Un'unità può esistere senza codici specifici
   - Più codici possono riferirsi alla stessa unità

## Technical Notes

- Il campo `ProductUnitId` è nullable per mantenere la retrocompatibilità
- La relazione usa `DeleteBehavior.SetNull` per preservare i codici anche se l'unità viene eliminata
- Le conversioni tra enum status sono gestite esplicitamente nel service layer
- I dialog per aggiungere/modificare ProductCode sono ancora da implementare (TODO)

## Testing Recommendations

1. Verificare che un ProductCode possa essere creato con e senza ProductUnitId
2. Testare la visualizzazione nella UI quando ProductUnitId è null vs. quando è valorizzato
3. Verificare il comportamento di cascata quando si elimina un ProductUnit collegato
4. Testare la sincronizzazione tra ProductUnits caricati e ProductCodes nel ProductDrawer
