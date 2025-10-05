# ProductCode-ProductUnit Relationship: Visual Guide

## Entity Relationship Diagram

```
┌─────────────────────┐
│      Product        │
│─────────────────────│
│ Id (PK)            │
│ Name               │
│ Description        │
│ UnitOfMeasureId    │────┐
│ ...                │    │
└─────────────────────┘    │
         │                 │
         │ 1               │
         │                 │
         │ N               │
         ├─────────────────┼────────────────────────┐
         │                 │                        │
         │                 │                        │
┌────────▼─────────┐ ┌─────▼────────────┐  ┌───────▼──────────┐
│  ProductCode     │ │  ProductUnit     │  │        UM        │
│──────────────────│ │──────────────────│  │──────────────────│
│ Id (PK)         │ │ Id (PK)         │  │ Id (PK)         │
│ ProductId (FK)  │ │ ProductId (FK)  │  │ Name            │
│ ProductUnitId ◄─┼─┤ UnitOfMeasureId─┼──┤ Symbol          │
│   (FK, nullable)│ │ ConversionFactor│  │ ...             │
│ CodeType        │ │ UnitType        │  └──────────────────┘
│ Code            │ │ Description     │
│ Status          │ │ Status          │
│ ...             │ │ ...             │
└──────────────────┘ └──────────────────┘
```

## Key Relationships

1. **Product → ProductCode** (1:N)
   - Un prodotto può avere molti codici (SKU, EAN-13, UPC, etc.)
   
2. **Product → ProductUnit** (1:N)
   - Un prodotto può avere molte unità di misura (Base, Pack, Pallet, etc.)
   
3. **ProductCode → ProductUnit** (N:1, optional)
   - **NEW RELATIONSHIP**: Un codice può essere associato a un'unità specifica
   - Nullable: un codice può non avere un'unità specifica
   - Delete Cascade: SetNull (se l'unità viene eliminata, il codice rimane ma ProductUnitId diventa NULL)

4. **ProductUnit → UM** (N:1)
   - Ogni ProductUnit riferisce un'unità di misura (UM) del catalogo

## Data Flow Examples

### Example 1: Creating a Product with Multiple Barcodes

```
Product: "Acqua Minerale 500ml"
├─ Base Unit: 1 PZ (pezzo)
│
├─ ProductCode #1
│  ├─ Code: "8001234567890" (EAN-13)
│  ├─ Type: "EAN"
│  └─ ProductUnitId: → ProductUnit "Base" (1 PZ)
│
├─ ProductCode #2
│  ├─ Code: "8001234567999" (EAN-13)
│  ├─ Type: "EAN"
│  └─ ProductUnitId: → ProductUnit "Pack" (6 PZ)
│
└─ ProductCode #3
   ├─ Code: "18001234567897" (EAN-14)
   ├─ Type: "EAN"
   └─ ProductUnitId: → ProductUnit "Pallet" (144 PZ)
```

### Example 2: Barcode Scanning Flow

```
┌──────────────────┐
│ Scan Barcode     │
│ "8001234567999"  │
└────────┬─────────┘
         │
         ▼
┌────────────────────┐
│ Lookup ProductCode │
│ Find Product       │
└────────┬───────────┘
         │
         ▼
┌────────────────────────────┐
│ Check ProductUnitId        │
│ - If set: Use that unit    │
│ - If null: Use base unit   │
└────────┬───────────────────┘
         │
         ▼
┌────────────────────────────┐
│ Get ProductUnit details:   │
│ - UnitType: "Pack"        │
│ - ConversionFactor: 6     │
│ - UnitOfMeasure: PZ       │
└────────┬───────────────────┘
         │
         ▼
┌────────────────────────────┐
│ Apply to transaction:      │
│ Quantity: 1 pack = 6 PZ   │
└────────────────────────────┘
```

## API Endpoints

### ProductCode (existing, enhanced)
```
GET    /api/v1/product-management/products/{productId}/codes
POST   /api/v1/product-management/products/{productId}/codes
       Body: { productId, productUnitId?, codeType, code, ... }
```

### ProductUnit (new endpoints)
```
GET    /api/v1/product-management/products/{productId}/units
POST   /api/v1/product-management/products/{productId}/units
       Body: { productId, unitOfMeasureId, conversionFactor, unitType, ... }
       
PUT    /api/v1/product-management/products/units/{id}
       Body: { unitOfMeasureId, conversionFactor, unitType, status, ... }
       
DELETE /api/v1/product-management/products/units/{id}
```

## UI Changes

### ProductDrawer - ProductCode Table

**Before:**
```
┌──────────────┬─────────┬────────────┬────────┐
│ Tipo Codice  │ Codice  │ Descrizione│ Stato  │
├──────────────┼─────────┼────────────┼────────┤
│ EAN          │ 800123..│ Pezzo      │ Attivo │
│ EAN          │ 800199..│ Confezione │ Attivo │
└──────────────┴─────────┴────────────┴────────┘
```

**After:**
```
┌──────────────┬─────────┬──────────────────────┬────────────┬────────┐
│ Tipo Codice  │ Codice  │ Unità di Misura     │ Descrizione│ Stato  │
├──────────────┼─────────┼──────────────────────┼────────────┼────────┤
│ EAN          │ 800123..│ Base (PZ (pz) x 1.00)│ Pezzo      │ Attivo │
│ EAN          │ 800199..│ Pack (CF (cf) x 6.00)│ Confezione │ Attivo │
└──────────────┴─────────┴──────────────────────┴────────────┴────────┘
```

## Use Cases by Industry

### Retail Store
- **Single Item Sale**: Scan single-unit barcode → 1 piece
- **Multi-pack Sale**: Scan pack barcode → 6 pieces automatically counted

### Warehouse
- **Receiving**: Scan pallet barcode → 144 pieces added to inventory
- **Picking**: Scan individual barcode → 1 piece reduced from inventory

### E-commerce
- **Product Variants**: Different SKUs for same product in different quantities
- **Automatic Pricing**: Each unit type can have different pricing in PriceList

## Migration Path

For existing installations:
1. Database migration adds nullable `ProductUnitId` column
2. Existing ProductCodes continue to work (ProductUnitId = NULL)
3. Gradually associate codes with units as needed
4. No breaking changes - backward compatible

## Benefits Summary

✅ **Accuracy**: Automatic unit detection from barcode  
✅ **Efficiency**: Faster checkout and inventory processes  
✅ **Flexibility**: Optional feature, use as needed  
✅ **Scalability**: Supports complex inventory scenarios  
✅ **Data Integrity**: Foreign key constraints ensure data consistency  
✅ **User Experience**: Clear visual indication in UI of unit-code associations
