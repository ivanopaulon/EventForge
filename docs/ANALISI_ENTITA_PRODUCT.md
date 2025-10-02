# Analisi dell'Entità Product e delle Entità Collegate

## Indice
1. [Panoramica](#panoramica)
2. [Entità Product](#entità-product)
3. [Entità Collegate Direttamente](#entità-collegate-direttamente)
4. [Entità di Supporto](#entità-di-supporto)
5. [Relazioni tra Entità](#relazioni-tra-entità)
6. [Regole di Business](#regole-di-business)
7. [Diagramma delle Relazioni](#diagramma-delle-relazioni)

---

## Panoramica

Il sistema EventForge implementa un modello dati completo per la gestione dei prodotti. L'entità centrale `Product` è connessa a diverse entità correlate che forniscono funzionalità estese per la gestione dell'inventario, classificazione, prezzi, fornitori e composizione dei prodotti.

Questo documento fornisce un'analisi dettagliata dell'entità `Product` e di tutte le entità ad essa collegate.

---

## Entità Product

### Definizione
**Namespace**: `EventForge.Server.Data.Entities.Products`  
**File**: `Product.cs`  
**Base Class**: `AuditableEntity`

### Scopo
L'entità `Product` rappresenta un prodotto nell'inventario del sistema. Può essere un prodotto semplice o un bundle (composto da altri prodotti).

### Proprietà Principali

#### Informazioni di Base
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `Name` | `string` | Sì | Nome del prodotto (max 100 caratteri) |
| `ShortDescription` | `string` | No | Descrizione breve (max 50 caratteri) |
| `Description` | `string` | No | Descrizione dettagliata (max 500 caratteri) |
| `Code` | `string` | No | Codice prodotto (SKU o simile) |
| `Status` | `ProductStatus` | Sì | Stato del prodotto (Active, Suspended, OutOfStock, Deleted) |

#### Informazioni Visive
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `ImageUrl` | `string` | No | URL dell'immagine (DEPRECATED, max 500 caratteri) |
| `ImageDocumentId` | `Guid?` | No | Riferimento al documento immagine |
| `ImageDocument` | `DocumentReference?` | No | Navigation property per il documento immagine |

#### Informazioni Prezzo e IVA
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `DefaultPrice` | `decimal?` | No | Prezzo predefinito del prodotto |
| `IsVatIncluded` | `bool` | No | Indica se il prezzo include l'IVA (default: false) |
| `VatRateId` | `Guid?` | No | Riferimento all'aliquota IVA |
| `VatRate` | `VatRate?` | No | Navigation property per l'aliquota IVA |

#### Unità di Misura
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `UnitOfMeasureId` | `Guid?` | No | Riferimento all'unità di misura predefinita |
| `UnitOfMeasure` | `UM?` | No | Navigation property per l'unità di misura |
| `Units` | `ICollection<ProductUnit>` | No | Collezione di unità di misura associate |

#### Classificazione Gerarchica
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `CategoryNodeId` | `Guid?` | No | Riferimento al nodo categoria principale |
| `CategoryNode` | `ClassificationNode?` | No | Navigation property per la categoria |
| `FamilyNodeId` | `Guid?` | No | Riferimento al nodo famiglia |
| `FamilyNode` | `ClassificationNode?` | No | Navigation property per la famiglia |
| `GroupNodeId` | `Guid?` | No | Riferimento al nodo gruppo statistico |
| `GroupNode` | `ClassificationNode?` | No | Navigation property per il gruppo |

#### Codici Alternativi
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `Codes` | `ICollection<ProductCode>` | No | Collezione di codici alias (SKU, EAN, UPC, ecc.) |

#### Stazione e Posizione
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `StationId` | `Guid?` | No | Riferimento alla stazione |
| `Station` | `Station?` | No | Navigation property per la stazione |

#### Bundle/Composizione
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `IsBundle` | `bool` | No | Indica se il prodotto è un bundle (default: false) |
| `BundleItems` | `ICollection<ProductBundleItem>` | No | Componenti del bundle (se il prodotto è un bundle) |
| `IncludedInBundles` | `ICollection<ProductBundleItem>` | No | Bundle in cui questo prodotto è incluso come componente |

#### Brand e Modello
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `BrandId` | `Guid?` | No | Riferimento al brand |
| `Brand` | `Brand?` | No | Navigation property per il brand |
| `ModelId` | `Guid?` | No | Riferimento al modello |
| `Model` | `Model?` | No | Navigation property per il modello |

#### Gestione Fornitori
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `Suppliers` | `ICollection<ProductSupplier>` | No | Collezione dei fornitori del prodotto |
| `PreferredSupplierId` | `Guid?` | No | Riferimento al fornitore preferito |

#### Gestione Inventario
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `ReorderPoint` | `decimal?` | No | Livello di riordino |
| `SafetyStock` | `decimal?` | No | Stock di sicurezza minimo |
| `TargetStockLevel` | `decimal?` | No | Livello di stock desiderato |
| `AverageDailyDemand` | `decimal?` | No | Domanda media giornaliera |

### Enumerazioni

#### ProductStatus
```csharp
public enum ProductStatus
{
    Active,      // Prodotto attivo e disponibile
    Suspended,   // Prodotto temporaneamente sospeso
    OutOfStock,  // Prodotto esaurito
    Deleted      // Prodotto cancellato/disabilitato
}
```

---

## Entità Collegate Direttamente

### 1. ProductCode

#### Definizione
**Namespace**: `EventForge.Server.Data.Entities.Products`  
**File**: `ProductCode.cs`  
**Base Class**: `AuditableEntity`

#### Scopo
Rappresenta un codice alias (SKU, barcode, EAN, UPC, ecc.) per un prodotto. Permette di associare multipli codici identificativi a un singolo prodotto.

#### Proprietà
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `ProductId` | `Guid` | Sì | Foreign key al prodotto |
| `Product` | `Product?` | No | Navigation property per il prodotto |
| `CodeType` | `string` | Sì | Tipo di codice (SKU, EAN, UPC, ecc.) - max 30 caratteri |
| `Code` | `string` | Sì | Valore del codice - max 100 caratteri |
| `AlternativeDescription` | `string?` | No | Descrizione alternativa - max 200 caratteri |
| `Status` | `ProductCodeStatus` | Sì | Stato del codice |

#### Enumerazioni
```csharp
public enum ProductCodeStatus
{
    Active,     // Codice attivo e utilizzabile
    Suspended,  // Codice temporaneamente sospeso
    Expired,    // Codice scaduto/non più valido
    Deleted     // Codice cancellato/disabilitato
}
```

#### Relazioni
- **Product**: Relazione 1:N (un prodotto può avere molti codici)

---

### 2. ProductUnit

#### Definizione
**Namespace**: `EventForge.Server.Data.Entities.Products`  
**File**: `ProductUnit.cs`  
**Base Class**: `AuditableEntity`

#### Scopo
Rappresenta un'unità di misura per un prodotto, con fattore di conversione e tipo. Permette di gestire prodotti con diverse unità di misura (es. pezzo, confezione, pallet).

#### Proprietà
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `ProductId` | `Guid` | Sì | Foreign key al prodotto |
| `Product` | `Product?` | No | Navigation property per il prodotto |
| `UnitOfMeasureId` | `Guid` | Sì | Foreign key all'unità di misura |
| `UnitOfMeasure` | `UM?` | No | Navigation property per l'unità di misura |
| `ConversionFactor` | `decimal` | Sì | Fattore di conversione all'unità base (es. 6.5 per una confezione di 6.5 pezzi) |
| `UnitType` | `string` | Sì | Tipo di unità (Base, Pack, Pallet, ecc.) - max 20 caratteri |
| `Description` | `string?` | No | Descrizione aggiuntiva - max 100 caratteri |
| `Status` | `ProductUnitStatus` | Sì | Stato dell'unità |

#### Vincoli
- `ConversionFactor` deve essere maggiore di 0.001

#### Enumerazioni
```csharp
public enum ProductUnitStatus
{
    Active,     // Unità attiva e utilizzabile
    Suspended,  // Unità temporaneamente sospesa
    Deleted     // Unità cancellata/disabilitata
}
```

#### Relazioni
- **Product**: Relazione 1:N (un prodotto può avere molte unità di misura)
- **UM**: Relazione N:1 (molte ProductUnit possono riferirsi alla stessa UM)

---

### 3. ProductSupplier

#### Definizione
**Namespace**: `EventForge.Server.Data.Entities.Products`  
**File**: `ProductSupplier.cs`  
**Base Class**: `AuditableEntity`

#### Scopo
Rappresenta la relazione tra un prodotto e un fornitore. Gestisce informazioni specifiche del fornitore come codice prodotto, prezzo, tempi di consegna, ecc.

#### Proprietà
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `ProductId` | `Guid` | Sì | Foreign key al prodotto |
| `Product` | `Product?` | No | Navigation property per il prodotto |
| `SupplierId` | `Guid` | Sì | Foreign key al fornitore (BusinessParty) |
| `Supplier` | `BusinessParty?` | No | Navigation property per il fornitore |
| `SupplierProductCode` | `string?` | No | Codice prodotto del fornitore - max 100 caratteri |
| `PurchaseDescription` | `string?` | No | Descrizione d'acquisto specifica - max 500 caratteri |
| `UnitCost` | `decimal?` | No | Costo unitario dal fornitore |
| `Currency` | `string?` | No | Valuta del costo - max 10 caratteri |
| `MinOrderQty` | `int?` | No | Quantità minima d'ordine |
| `IncrementQty` | `int?` | No | Incremento quantità ordine |
| `LeadTimeDays` | `int?` | No | Tempo di consegna in giorni |
| `LastPurchasePrice` | `decimal?` | No | Ultimo prezzo d'acquisto |
| `LastPurchaseDate` | `DateTime?` | No | Data ultimo acquisto |
| `Preferred` | `bool` | No | Indica se è il fornitore preferito (default: false) |
| `Notes` | `string?` | No | Note aggiuntive - max 1000 caratteri |

#### Regole di Business
1. Solo un fornitore per prodotto può essere impostato come `Preferred`
2. Il fornitore deve essere di tipo `Fornitore` o `ClienteFornitore`
3. Non è permessa la duplicazione della relazione prodotto-fornitore

#### Relazioni
- **Product**: Relazione 1:N (un prodotto può avere molti fornitori)
- **BusinessParty**: Relazione N:1 (molti ProductSupplier possono riferirsi allo stesso fornitore)

---

### 4. ProductBundleItem

#### Definizione
**Namespace**: `EventForge.Server.Data.Entities.Products`  
**File**: `ProductBundleItem.cs`  
**Base Class**: `AuditableEntity`

#### Scopo
Rappresenta un componente di un bundle di prodotti. Permette di creare prodotti composti da altri prodotti con quantità specifiche.

#### Proprietà
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `BundleProductId` | `Guid` | Sì | Foreign key al prodotto bundle (padre) |
| `BundleProduct` | `Product?` | No | Navigation property per il prodotto bundle |
| `ComponentProductId` | `Guid` | Sì | Foreign key al prodotto componente (figlio) |
| `ComponentProduct` | `Product?` | No | Navigation property per il prodotto componente |
| `Quantity` | `int` | Sì | Quantità del componente nel bundle (1-10,000) |

#### Vincoli
- `Quantity` deve essere compreso tra 1 e 10,000

#### Relazioni
- **Product** (BundleProduct): Relazione 1:N (un bundle può avere molti componenti)
- **Product** (ComponentProduct): Relazione 1:N (un prodotto può essere componente di molti bundle)

---

### 5. Brand

#### Definizione
**Namespace**: `EventForge.Server.Data.Entities.Products`  
**File**: `Brand.cs`  
**Base Class**: `AuditableEntity`

#### Scopo
Rappresenta un brand o produttore di prodotti.

#### Proprietà
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `Name` | `string` | Sì | Nome del brand - max 200 caratteri |
| `Description` | `string?` | No | Descrizione del brand - max 1000 caratteri |
| `Website` | `string?` | No | URL del sito web - max 500 caratteri |
| `Country` | `string?` | No | Paese di origine - max 100 caratteri |
| `Models` | `ICollection<Model>` | No | Collezione di modelli associati |
| `Products` | `ICollection<Product>` | No | Collezione di prodotti associati |

#### Relazioni
- **Model**: Relazione 1:N (un brand può avere molti modelli)
- **Product**: Relazione 1:N (un brand può avere molti prodotti)

---

### 6. Model

#### Definizione
**Namespace**: `EventForge.Server.Data.Entities.Products`  
**File**: `Model.cs`  
**Base Class**: `AuditableEntity`

#### Scopo
Rappresenta un modello di prodotto all'interno di un brand.

#### Proprietà
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `BrandId` | `Guid` | Sì | Foreign key al brand |
| `Brand` | `Brand?` | No | Navigation property per il brand |
| `Name` | `string` | Sì | Nome del modello - max 200 caratteri |
| `Description` | `string?` | No | Descrizione del modello - max 1000 caratteri |
| `ManufacturerPartNumber` | `string?` | No | Numero di parte del produttore (MPN) - max 100 caratteri |
| `Products` | `ICollection<Product>` | No | Collezione di prodotti associati |

#### Relazioni
- **Brand**: Relazione N:1 (molti modelli appartengono a un brand)
- **Product**: Relazione 1:N (un modello può avere molti prodotti)

---

## Entità di Supporto

### 1. ClassificationNode

#### Definizione
**Namespace**: `EventForge.Server.Data.Entities.Common`  
**File**: `ClassificationNode.cs`  
**Base Class**: `AuditableEntity`

#### Scopo
Rappresenta un nodo nella gerarchia di classificazione dei prodotti (categoria, famiglia, gruppo). Implementa una struttura ad albero per organizzare i prodotti gerarchicamente.

#### Proprietà
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `Code` | `string?` | No | Codice del nodo (es. CAT01, FAM02) - max 30 caratteri |
| `Name` | `string` | Sì | Nome del nodo - max 100 caratteri |
| `Description` | `string?` | No | Descrizione del nodo - max 200 caratteri |
| `Type` | `ProductClassificationType` | No | Tipo di classificazione (Category, Family, Group) |
| `Status` | `ProductClassificationNodeStatus` | Sì | Stato del nodo |
| `Level` | `int` | No | Livello gerarchico (root = 0, range 0-10) |
| `Order` | `int` | No | Ordine per ordinare i nodi allo stesso livello (range 0-1000) |
| `ParentId` | `Guid?` | No | Foreign key al nodo padre (null se root) |
| `Parent` | `ClassificationNode?` | No | Navigation property per il nodo padre |
| `Children` | `ICollection<ClassificationNode>` | No | Collezione di nodi figli |

#### Vincoli
- `Level` deve essere compreso tra 0 e 10
- `Order` deve essere compreso tra 0 e 1000

#### Relazioni
- **Parent**: Relazione auto-referenziale (un nodo può avere un padre)
- **Children**: Relazione auto-referenziale (un nodo può avere molti figli)
- **Product**: Relazione 1:N tramite CategoryNodeId, FamilyNodeId, GroupNodeId

---

### 2. VatRate

#### Definizione
**Namespace**: `EventForge.Server.Data.Entities.Common`  
**File**: `VatRate.cs`  
**Base Class**: `AuditableEntity`

#### Scopo
Rappresenta un'aliquota IVA applicabile ai prodotti.

#### Proprietà
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `Name` | `string` | Sì | Nome dell'aliquota IVA (es. "IVA 22%") - max 50 caratteri |
| `Percentage` | `decimal` | Sì | Percentuale dell'aliquota (0-100) |
| `Status` | `ProductVatRateStatus` | Sì | Stato dell'aliquota |
| `ValidFrom` | `DateTime?` | No | Data inizio validità |
| `ValidTo` | `DateTime?` | No | Data fine validità |
| `Notes` | `string?` | No | Note aggiuntive - max 200 caratteri |
| `Products` | `ICollection<Product>` | No | Collezione di prodotti associati |

#### Vincoli
- `Percentage` deve essere compreso tra 0 e 100

#### Enumerazioni
```csharp
public enum ProductVatRateStatus
{
    Active,     // Aliquota attiva e utilizzabile
    Suspended,  // Aliquota temporaneamente sospesa
    Expired,    // Aliquota scaduta (non più valida)
    Deleted     // Aliquota cancellata/disabilitata
}
```

#### Relazioni
- **Product**: Relazione 1:N (un'aliquota IVA può essere usata da molti prodotti)

---

### 3. UM (Unit of Measure)

#### Definizione
**Namespace**: `EventForge.Server.Data.Entities.Common`  
**File**: `UM.cs`  
**Base Class**: `AuditableEntity`

#### Scopo
Rappresenta un'unità di misura (es. "Kg", "L", "Pezzi").

#### Proprietà
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `Name` | `string` | Sì | Nome dell'unità di misura (es. "Chilogrammo") - max 50 caratteri |
| `Symbol` | `string` | Sì | Simbolo dell'unità (es. "kg") - max 10 caratteri |
| `Description` | `string?` | No | Descrizione dell'unità - max 200 caratteri |
| `IsDefault` | `bool` | No | Indica se è l'unità predefinita (default: false) |
| `Products` | `ICollection<Product>` | No | Collezione di prodotti associati |

#### Relazioni
- **Product**: Relazione 1:N (un'unità di misura può essere usata da molti prodotti)
- **ProductUnit**: Relazione 1:N (un'unità di misura può essere usata in molte ProductUnit)

---

### 4. Station

#### Definizione
**Namespace**: `EventForge.Server.Data.Entities.StationMonitor`  
**File**: `Station.cs`  
**Base Class**: `AuditableEntity`

#### Scopo
Rappresenta una stazione (es. bar, cucina, cassa, ecc.) associabile ai prodotti.

#### Proprietà
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `Name` | `string` | Sì | Nome della stazione - max 100 caratteri |
| `Description` | `string?` | No | Descrizione della stazione - max 200 caratteri |
| `Status` | `StationStatus` | Sì | Stato della stazione |
| `Location` | `string?` | No | Posizione fisica o logica - max 50 caratteri |
| `SortOrder` | `int` | No | Ordine di visualizzazione (default: 0) |
| `Notes` | `string?` | No | Note aggiuntive - max 200 caratteri |
| `Printers` | `ICollection<Printer>` | No | Collezione di stampanti assegnate |

#### Enumerazioni
```csharp
public enum StationStatus
{
    Active,      // Stazione attiva e operativa
    Suspended,   // Temporaneamente sospesa
    Maintenance, // In manutenzione
    Disabled     // Disabilitata/non utilizzabile
}
```

#### Relazioni
- **Product**: Relazione 1:N (una stazione può essere associata a molti prodotti)
- **Printer**: Relazione 1:N (una stazione può avere molte stampanti)

---

### 5. BusinessParty

#### Definizione
**Namespace**: `EventForge.Server.Data.Entities.Business`  
**File**: `BusinessParty.cs`  
**Base Class**: `AuditableEntity`

#### Scopo
Rappresenta un'entità commerciale (cliente, fornitore, o entrambi). Viene utilizzata per associare fornitori ai prodotti.

#### Proprietà (Principali)
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `PartyType` | `BusinessPartyType` | Sì | Tipo di entità (Cliente, Fornitore, ClienteFornitore) |
| `Name` | `string` | Sì | Nome azienda o nome completo - max 200 caratteri |
| `TaxCode` | `string?` | No | Codice fiscale - max 20 caratteri |
| `VatNumber` | `string?` | No | Partita IVA - max 20 caratteri |
| `SdiCode` | `string?` | No | Codice SDI per fatturazione elettronica - max 10 caratteri |
| `Pec` | `string?` | No | Email certificata (PEC) - max 100 caratteri |
| `Notes` | `string?` | No | Note aggiuntive - max 500 caratteri |
| `Addresses` | `ICollection<Address>` | No | Collezione di indirizzi |
| `Contacts` | `ICollection<Contact>` | No | Collezione di contatti generali |
| `References` | `ICollection<Reference>` | No | Collezione di persone di riferimento |

#### Enumerazioni
```csharp
public enum BusinessPartyType
{
    Cliente,           // Solo cliente
    Fornitore,         // Solo fornitore
    ClienteFornitore   // Sia cliente che fornitore
}
```

#### Relazioni
- **ProductSupplier**: Relazione 1:N (un BusinessParty può fornire molti prodotti)

---

### 6. DocumentReference

#### Definizione
**Namespace**: `EventForge.Server.Data.Entities.Teams`  
**File**: `DocumentReference.cs`  
**Base Class**: `AuditableEntity`

#### Scopo
Rappresenta un riferimento a un documento (immagini, certificati, foto, ecc.). Utilizzato per associare documenti immagine ai prodotti.

#### Proprietà (Principali)
| Proprietà | Tipo | Obbligatorio | Descrizione |
|-----------|------|--------------|-------------|
| `OwnerId` | `Guid?` | No | ID dell'entità proprietaria |
| `OwnerType` | `string?` | No | Tipo dell'entità proprietaria - max 50 caratteri |
| `FileName` | `string` | Sì | Nome file originale - max 255 caratteri |
| `Type` | `DocumentReferenceType` | Sì | Tipo di documento |
| `SubType` | `DocumentReferenceSubType` | No | Sotto-tipo per categorizzazione più specifica |
| `MimeType` | `string` | Sì | Tipo MIME del file - max 100 caratteri |
| `StorageKey` | `string` | Sì | Chiave/percorso nel sistema di storage - max 500 caratteri |
| `Url` | `string?` | No | URL pubblico per accedere al documento - max 1000 caratteri |
| `ThumbnailStorageKey` | `string?` | No | Chiave storage per thumbnail - max 500 caratteri |

#### Relazioni
- **Product**: Relazione 1:N tramite ImageDocumentId (un documento può essere usato da molti prodotti)

---

## Relazioni tra Entità

### Diagramma Testuale delle Relazioni

```
Product (Entità Centrale)
│
├─ One-to-Many: ProductCode (1 Product → N ProductCodes)
│  └─ Codici alias per identificare il prodotto (SKU, EAN, UPC, ecc.)
│
├─ One-to-Many: ProductUnit (1 Product → N ProductUnits)
│  ├─ Many-to-One: UM (N ProductUnits → 1 UM)
│  └─ Unità di misura alternative per il prodotto
│
├─ Many-to-One: UM (N Products → 1 UM)
│  └─ Unità di misura predefinita del prodotto
│
├─ One-to-Many: ProductSupplier (1 Product → N ProductSuppliers)
│  └─ Many-to-One: BusinessParty (N ProductSuppliers → 1 BusinessParty)
│     └─ Fornitori del prodotto
│
├─ One-to-Many: ProductBundleItem (1 Product [Bundle] → N ProductBundleItems)
│  └─ Many-to-One: Product (N ProductBundleItems → 1 Product [Component])
│     └─ Componenti del bundle (auto-referenziale)
│
├─ Many-to-One: Brand (N Products → 1 Brand)
│  └─ Brand/produttore del prodotto
│
├─ Many-to-One: Model (N Products → 1 Model)
│  └─ Many-to-One: Brand (N Models → 1 Brand)
│     └─ Modello specifico del brand
│
├─ Many-to-One: ClassificationNode (CategoryNode)
│  └─ Categoria principale del prodotto
│
├─ Many-to-One: ClassificationNode (FamilyNode)
│  └─ Famiglia del prodotto
│
├─ Many-to-One: ClassificationNode (GroupNode)
│  └─ Gruppo statistico del prodotto
│
├─ Many-to-One: VatRate (N Products → 1 VatRate)
│  └─ Aliquota IVA applicata al prodotto
│
├─ Many-to-One: Station (N Products → 1 Station)
│  └─ Stazione associata al prodotto
│
└─ Many-to-One: DocumentReference (N Products → 1 DocumentReference)
   └─ Documento immagine del prodotto
```

### Gerarchia di Dipendenze

#### Livello 1: Entità di Supporto Base
Queste entità non dipendono da altre entità del dominio prodotti:
- `UM` (Unit of Measure)
- `VatRate`
- `Station`
- `DocumentReference`
- `Brand`

#### Livello 2: Entità di Classificazione
Queste entità hanno dipendenze limitate:
- `ClassificationNode` (auto-referenziale per la gerarchia)
- `Model` (dipende da Brand)
- `BusinessParty`

#### Livello 3: Entità Centrale
- `Product` (dipende da tutte le entità dei livelli 1 e 2)

#### Livello 4: Entità Dipendenti da Product
Queste entità esistono solo in relazione a Product:
- `ProductCode`
- `ProductUnit` (dipende anche da UM)
- `ProductSupplier` (dipende anche da BusinessParty)
- `ProductBundleItem` (auto-referenziale tramite Product)

---

## Regole di Business

### Product

1. **Validazione Nome**: Il nome del prodotto è obbligatorio e non può superare 100 caratteri
2. **Stato Prodotto**: Deve essere sempre specificato uno stato valido
3. **Bundle**:
   - Se `IsBundle = true`, il prodotto dovrebbe avere elementi in `BundleItems`
   - Un prodotto bundle non dovrebbe essere componente di se stesso (prevenzione loop)
4. **Immagine**: Preferire `ImageDocumentId` rispetto a `ImageUrl` (deprecato)
5. **Prezzo e IVA**:
   - Se `IsVatIncluded = false`, il prezzo è considerato netto
   - Se `IsVatIncluded = true`, il prezzo è considerato lordo (include IVA)
6. **Inventario**:
   - `ReorderPoint` indica quando riordinare il prodotto
   - `SafetyStock` rappresenta il livello minimo di sicurezza
   - `TargetStockLevel` è il livello ottimale desiderato

### ProductCode

1. **Unicità**: La combinazione (ProductId, CodeType, Code) dovrebbe essere unica
2. **Tipo Codice**: Deve essere specificato e non può superare 30 caratteri
3. **Valore Codice**: Deve essere specificato e non può superare 100 caratteri
4. **Stato**: Solo i codici con stato `Active` dovrebbero essere utilizzati attivamente

### ProductUnit

1. **Fattore di Conversione**: Deve essere sempre maggiore di 0.001
2. **Tipo Unità**: Deve essere specificato (Base, Pack, Pallet, ecc.)
3. **Relazione con UM**: Deve riferirsi a una unità di misura esistente e valida
4. **Unità Base**: Almeno un ProductUnit dovrebbe avere `UnitType = "Base"` con `ConversionFactor = 1`

### ProductSupplier

1. **Fornitore Preferito**: Solo un fornitore per prodotto può avere `Preferred = true`
2. **Tipo Fornitore**: Il `BusinessParty` riferito deve essere di tipo `Fornitore` o `ClienteFornitore`
3. **Unicità Relazione**: Non è permessa la duplicazione della relazione prodotto-fornitore
4. **Prezzo e Valuta**: Se specificato `UnitCost`, dovrebbe essere specificata anche `Currency`
5. **Quantità Minima**: Se specificato `MinOrderQty`, dovrebbe essere positivo
6. **Lead Time**: Se specificato `LeadTimeDays`, dovrebbe essere positivo

### ProductBundleItem

1. **Quantità**: Deve essere compresa tra 1 e 10,000
2. **Prodotti Distinti**: `BundleProductId` e `ComponentProductId` devono essere diversi
3. **Cicli**: Prevenire cicli nelle dipendenze dei bundle (A contiene B, B contiene A)
4. **Prodotto Bundle**: Il prodotto con `BundleProductId` dovrebbe avere `IsBundle = true`

### Brand e Model

1. **Nome Brand**: Obbligatorio, max 200 caratteri
2. **Modello e Brand**: Un Model deve sempre riferirsi a un Brand valido
3. **Nome Modello**: Obbligatorio, max 200 caratteri

### ClassificationNode

1. **Gerarchia**: Deve essere mantenuta una struttura ad albero coerente
2. **Livello**: Deve corrispondere alla profondità nell'albero (root = 0)
3. **Parent**: Se `ParentId` è null, il nodo è root (Level = 0)
4. **Tipo**: Il tipo di nodo (Category, Family, Group) dovrebbe essere coerente con il livello gerarchico
5. **Ordine**: Utilizzato per ordinare i nodi allo stesso livello

### VatRate

1. **Percentuale**: Deve essere compresa tra 0 e 100
2. **Validità**: Se specificato `ValidFrom` e `ValidTo`, `ValidFrom` deve essere precedente a `ValidTo`
3. **Stato**: Solo aliquote con stato `Active` e nel periodo di validità dovrebbero essere utilizzate

### UM

1. **Nome e Simbolo**: Entrambi sono obbligatori
2. **Simbolo Unico**: Il simbolo dovrebbe essere unico nel sistema
3. **Unità Predefinita**: Solo una UM dovrebbe avere `IsDefault = true`

---

## Diagramma delle Relazioni

### Diagramma ER (Entity-Relationship) in Formato Testuale

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           PRODUCT (Entità Centrale)                          │
│─────────────────────────────────────────────────────────────────────────────│
│ • Id (PK)                      • CategoryNodeId (FK)                        │
│ • Name *                       • FamilyNodeId (FK)                          │
│ • Code                         • GroupNodeId (FK)                           │
│ • Description                  • StationId (FK)                             │
│ • DefaultPrice                 • BrandId (FK)                               │
│ • Status *                     • ModelId (FK)                               │
│ • IsVatIncluded                • VatRateId (FK)                             │
│ • UnitOfMeasureId (FK)         • ImageDocumentId (FK)                       │
│ • IsBundle                     • PreferredSupplierId                        │
│ • ReorderPoint                 • AuditableEntity fields                     │
│ • SafetyStock                                                               │
│ • TargetStockLevel                                                          │
└─────────────────────────────────────────────────────────────────────────────┘
          │                │              │              │            │
          │                │              │              │            │
          ▼                ▼              ▼              ▼            ▼
   ┌────────────┐   ┌───────────┐  ┌──────────┐  ┌──────────┐  ┌─────────┐
   │ProductCode │   │ProductUnit│  │ Product  │  │ Product  │  │ Product │
   │            │   │           │  │ Supplier │  │  Bundle  │  │   (N)   │
   │• ProductId │   │• ProductId│  │          │  │   Item   │  │         │
   │• CodeType  │   │• UMId     │  │• Product │  │          │  │Relations│
   │• Code      │   │• Conversion│  │  Id     │  │• Bundle  │  │   to    │
   │• Status    │   │  Factor   │  │• Supplier│  │  Product │  │ Support │
   └────────────┘   │• UnitType │  │  Id     │  │  Id      │  │Entities │
                    │• Status   │  │• Preferred│  │• Comp.   │  └─────────┘
                    └───────────┘  │• UnitCost│  │  Product │
                          │        └──────────┘  │  Id      │
                          │              │       │• Quantity│
                          ▼              ▼       └──────────┘
                    ┌──────────┐  ┌─────────────┐
                    │    UM    │  │ Business    │
                    │          │  │   Party     │
                    │• Name    │  │             │
                    │• Symbol  │  │• PartyType  │
                    │• IsDefault│  │• Name       │
                    └──────────┘  │• TaxCode    │
                                  │• VatNumber  │
                                  └─────────────┘

┌──────────────────────────────────────────────────────────────────────────┐
│                         ENTITÀ DI SUPPORTO                                │
└──────────────────────────────────────────────────────────────────────────┘

┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│ Classification   │  │     VatRate      │  │    Station       │
│      Node        │  │                  │  │                  │
│──────────────────│  │──────────────────│  │──────────────────│
│• Id (PK)         │  │• Id (PK)         │  │• Id (PK)         │
│• Code            │  │• Name *          │  │• Name *          │
│• Name *          │  │• Percentage *    │  │• Description     │
│• Type            │  │• Status *        │  │• Status *        │
│• Status *        │  │• ValidFrom       │  │• Location        │
│• Level           │  │• ValidTo         │  │• SortOrder       │
│• Order           │  └──────────────────┘  └──────────────────┘
│• ParentId (FK)   │
│• Children (1:N)  │
└──────────────────┘

┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│      Brand       │  │      Model       │  │  Document        │
│                  │  │                  │  │  Reference       │
│──────────────────│  │──────────────────│  │──────────────────│
│• Id (PK)         │  │• Id (PK)         │  │• Id (PK)         │
│• Name *          │  │• BrandId (FK) *  │  │• FileName *      │
│• Description     │  │• Name *          │  │• Type *          │
│• Website         │  │• Description     │  │• MimeType *      │
│• Country         │  │• MPN             │  │• StorageKey *    │
│• Models (1:N)    │  └──────────────────┘  │• Url             │
│• Products (1:N)  │          │             │• OwnerId         │
└──────────────────┘          │             │• OwnerType       │
         │                    │             └──────────────────┘
         │                    │
         └────────────────────┘
```

### Cardinalità delle Relazioni

| Entità 1 | Relazione | Entità 2 | Tipo | Note |
|----------|-----------|----------|------|------|
| Product | has many | ProductCode | 1:N | Un prodotto può avere molti codici alternativi |
| Product | has many | ProductUnit | 1:N | Un prodotto può avere molte unità di misura |
| Product | belongs to | UM | N:1 | Unità di misura predefinita |
| ProductUnit | belongs to | UM | N:1 | Riferimento all'unità di misura specifica |
| Product | has many | ProductSupplier | 1:N | Un prodotto può avere molti fornitori |
| ProductSupplier | belongs to | BusinessParty | N:1 | Riferimento al fornitore |
| Product | has many | ProductBundleItem (bundle) | 1:N | Componenti del bundle |
| Product | has many | ProductBundleItem (component) | 1:N | Bundle in cui è incluso come componente |
| Product | belongs to | Brand | N:1 | Brand del prodotto |
| Product | belongs to | Model | N:1 | Modello del prodotto |
| Model | belongs to | Brand | N:1 | Brand del modello |
| Product | belongs to | ClassificationNode (Category) | N:1 | Categoria del prodotto |
| Product | belongs to | ClassificationNode (Family) | N:1 | Famiglia del prodotto |
| Product | belongs to | ClassificationNode (Group) | N:1 | Gruppo del prodotto |
| ClassificationNode | belongs to | ClassificationNode (Parent) | N:1 | Nodo padre (auto-referenziale) |
| ClassificationNode | has many | ClassificationNode (Children) | 1:N | Nodi figli (auto-referenziale) |
| Product | belongs to | VatRate | N:1 | Aliquota IVA del prodotto |
| Product | belongs to | Station | N:1 | Stazione associata |
| Product | belongs to | DocumentReference | N:1 | Documento immagine |

---

## Considerazioni Architetturali

### 1. Ereditarietà da AuditableEntity

Tutte le entità ereditano da `AuditableEntity`, che fornisce:
- `Id` (Guid): Identificatore univoco
- `CreatedAt` (DateTime): Data creazione
- `CreatedBy` (string): Utente che ha creato l'entità
- `ModifiedAt` (DateTime?): Data ultima modifica
- `ModifiedBy` (string?): Utente che ha modificato l'entità
- `DeletedAt` (DateTime?): Data cancellazione logica
- `DeletedBy` (string?): Utente che ha cancellato l'entità
- `IsDeleted` (bool): Flag cancellazione logica
- `IsActive` (bool): Flag stato attivo
- `RowVersion` (byte[]): Versione per concorrenza ottimistica

### 2. Soft Delete

Il sistema implementa la cancellazione logica (soft delete) attraverso:
- `IsDeleted`: Flag che indica se l'entità è cancellata
- `DeletedAt`: Timestamp della cancellazione
- `DeletedBy`: Utente che ha eseguito la cancellazione

Questo permette di mantenere lo storico e recuperare dati se necessario.

### 3. Concorrenza Ottimistica

Il campo `RowVersion` in `AuditableEntity` viene utilizzato per implementare la concorrenza ottimistica, prevenendo conflitti di aggiornamento simultanei.

### 4. Validazione

La validazione è implementata a più livelli:
- **Entità**: Attributi DataAnnotations (`Required`, `MaxLength`, `Range`, ecc.)
- **DTO**: Validazione input con FluentValidation o DataAnnotations
- **Servizi**: Regole di business complesse

### 5. Pattern Repository e Service Layer

Il sistema utilizza:
- **Repository Pattern**: Per l'accesso ai dati tramite DbContext
- **Service Layer**: Per implementare la logica di business
- **DTO Pattern**: Per il trasferimento dati tra layer

---

## Casi d'Uso Comuni

### 1. Creazione Prodotto Semplice

```
1. Creare o selezionare Brand (opzionale)
2. Creare o selezionare Model (opzionale)
3. Selezionare VatRate
4. Selezionare UM (unità di misura predefinita)
5. Selezionare ClassificationNode (categoria, famiglia, gruppo)
6. Creare Product con tutti i riferimenti
7. Aggiungere ProductCode (se necessari codici alternativi)
8. Aggiungere ProductUnit (se necessarie unità alternative)
```

### 2. Creazione Prodotto Bundle

```
1. Creare i prodotti componenti (se non esistono)
2. Creare il prodotto bundle con IsBundle = true
3. Creare ProductBundleItem per ogni componente con quantità
4. Verificare che non ci siano cicli nelle dipendenze
```

### 3. Associazione Fornitori a Prodotto

```
1. Verificare che BusinessParty esista con tipo Fornitore o ClienteFornitore
2. Creare ProductSupplier con dati specifici del fornitore
3. Se è il fornitore preferito, impostare Preferred = true
   (resettando eventuali altri fornitori preferiti per lo stesso prodotto)
```

### 4. Gestione Classificazione Gerarchica

```
1. Creare nodi root (Level = 0, ParentId = null)
2. Creare nodi figli collegandoli ai rispettivi parent
3. Assegnare prodotti ai nodi appropriati tramite CategoryNodeId, FamilyNodeId, GroupNodeId
```

### 5. Gestione Inventario

```
1. Impostare ReorderPoint (punto di riordino)
2. Impostare SafetyStock (scorta di sicurezza)
3. Impostare TargetStockLevel (livello target)
4. Impostare AverageDailyDemand (per calcoli automatici)
5. Il sistema può calcolare quando riordinare in base ai livelli
```

---

## Riepilogo

Il modello dati di EventForge per la gestione dei prodotti è:

### ✅ Punti di Forza

1. **Flessibilità**: Supporta diverse tipologie di prodotti (semplici, bundle)
2. **Classificazione Multipla**: Sistema gerarchico a tre livelli (categoria, famiglia, gruppo)
3. **Multi-codice**: Supporto per codici alternativi (SKU, EAN, UPC, ecc.)
4. **Multi-unità**: Gestione di unità di misura multiple con fattori di conversione
5. **Multi-fornitore**: Supporto per più fornitori per prodotto con fornitore preferito
6. **Audit Completo**: Tracking completo di creazione, modifica e cancellazione
7. **Soft Delete**: Cancellazione logica per mantenere lo storico
8. **Concorrenza**: Gestione della concorrenza ottimistica

### 📋 Entità Principali

- **Product**: 242 righe di codice - Entità centrale completa
- **ProductCode**: 63 righe - Gestione codici alternativi
- **ProductUnit**: 73 righe - Gestione unità di misura multiple
- **ProductSupplier**: 104 righe - Relazione con fornitori
- **ProductBundleItem**: 41 righe - Composizione bundle
- **Brand e Model**: Gestione marchi e modelli
- **ClassificationNode**: 76 righe - Classificazione gerarchica

### 🔗 Complessità delle Relazioni

Il sistema gestisce **15+ relazioni dirette** dalla tabella Product verso altre entità, creando un grafo complesso ma ben strutturato che supporta:
- Gestione completa del catalogo prodotti
- Pianificazione inventario
- Gestione fornitori
- Classificazione e ricerca
- Bundle e kit

---

## Riferimenti

- **Codice sorgente**: `/EventForge.Server/Data/Entities/Products/`
- **Entità comuni**: `/EventForge.Server/Data/Entities/Common/`
- **DTOs**: `/EventForge.DTOs/Products/`
- **Servizi**: `/EventForge.Server/Services/Products/`

---

*Documento generato in data: Gennaio 2025*  
*Versione: 1.0*  
*Repository: ivanopaulon/EventForge*
