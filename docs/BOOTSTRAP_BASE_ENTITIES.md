# Bootstrap Base Entities - Implementation Guide

## Overview

This document describes the implementation of automatic seeding of base entities during tenant bootstrap in EventForge. The system now automatically creates essential entities when a new tenant is initialized, providing a complete foundation for warehouse and product management.

## Features

### 1. VAT Nature Codes (Natura IVA)

The system now includes comprehensive support for Italian VAT nature codes required for tax compliance. A new `VatNature` entity has been added to manage these codes.

#### Entity: VatNature

**Location**: `EventForge.Server/Data/Entities/Common/VatNature.cs`

**Properties**:
- `Code`: VAT nature code (e.g., "N1", "N2.1", "N3", etc.)
- `Name`: Display name of the VAT nature
- `Description`: Detailed description of when to use this nature code
- `IsActive`: Inherited from AuditableEntity
- `VatRates`: Collection of VAT rates associated with this nature

#### Seeded VAT Nature Codes

The system seeds 24 Italian VAT nature codes covering all scenarios:

| Code | Name | Description |
|------|------|-------------|
| N1 | Escluse ex art. 15 | Operazioni escluse dal campo di applicazione dell'IVA |
| N2 | Non soggette | Operazioni non soggette ad IVA |
| N2.1 | Non soggette - Carenza territoriale | Cessioni di beni e prestazioni di servizi non soggette |
| N2.2 | Non soggette - Altre operazioni | Altre operazioni non soggette |
| N3 | Non imponibili | Esportazioni, cessioni intracomunitarie |
| N3.1-N3.6 | Non imponibili - Varie | Subcodes for different types of non-taxable operations |
| N4 | Esenti | Operazioni esenti da IVA |
| N5 | Regime del margine | Regime del margine / IVA non esposta |
| N6 | Inversione contabile | Reverse charge operations |
| N6.1-N6.9 | Inversione contabile - Varie | Subcodes for different reverse charge scenarios |
| N7 | IVA assolta in altro stato UE | VAT paid in another EU state |

### 2. VAT Rates (Aliquote IVA)

The `VatRate` entity has been enhanced with a foreign key to `VatNature`.

#### Updated Entity: VatRate

**New Properties**:
- `VatNatureId`: Optional foreign key to VatNature
- `VatNature`: Navigation property to VatNature entity

#### Seeded VAT Rates

The system seeds 5 Italian VAT rates as per current legislation (2024-2025):

| Rate | Percentage | Usage |
|------|-----------|-------|
| IVA 22% | 22% | Standard VAT rate (aliquota ordinaria) |
| IVA 10% | 10% | Reduced VAT rate - Food, beverages, tourism services |
| IVA 5% | 5% | Reduced VAT rate - Essential goods |
| IVA 4% | 4% | Minimum VAT rate - Basic necessities (bread, milk, etc.) |
| IVA 0% | 0% | Non-taxable, exempt, or out-of-scope operations |

### 3. Units of Measure (Unità di Misura)

The system seeds 20 commonly used units of measure for warehouse management.

#### Seeded Units of Measure

**Count/Piece Units**:
- Pezzo (pz) - Single pieces
- Confezione (conf) - Packages
- Scatola (scat) - Boxes
- Cartone (cart) - Cartons
- Pallet (pallet) - Pallets
- Bancale (banc) - Skids
- Collo (collo) - Parcels

**Weight Units**:
- Kilogrammo (kg) - Kilograms
- Grammo (g) - Grams
- Tonnellata (t) - Tons
- Quintale (q) - Quintals

**Volume Units**:
- Litro (l) - Liters
- Millilitro (ml) - Milliliters
- Metro cubo (m³) - Cubic meters

**Length Units**:
- Metro (m) - Meters
- Centimetro (cm) - Centimeters
- Metro quadrato (m²) - Square meters

**Other Units**:
- Paio (paio) - Pairs
- Set (set) - Sets
- Kit (kit) - Kits

### 4. Default Warehouse and Storage Location

The system automatically creates a default warehouse and storage location for new tenants.

#### Default Warehouse

**Properties**:
- **Name**: Magazzino Principale
- **Code**: MAG-01
- **IsFiscal**: true
- **Notes**: Magazzino principale creato durante l'inizializzazione del sistema

#### Default Storage Location

**Properties**:
- **Code**: UB-DEF
- **Description**: Ubicazione predefinita
- **WarehouseId**: Links to the default warehouse

## Implementation Details

### Bootstrap Service

**Location**: `EventForge.Server/Services/Auth/BootstrapService.cs`

The bootstrap service has been enhanced with the following new methods:

1. **SeedTenantBaseEntitiesAsync**: Main coordinator method that calls all seeding methods
2. **SeedVatNaturesAsync**: Seeds Italian VAT nature codes
3. **SeedVatRatesAsync**: Seeds Italian VAT rates
4. **SeedUnitsOfMeasureAsync**: Seeds common units of measure
5. **SeedDefaultWarehouseAsync**: Creates default warehouse and storage location

### Execution Flow

```
EnsureAdminBootstrappedAsync
  ├── SeedDefaultRolesAndPermissionsAsync (always runs)
  ├── EnsureSuperAdminLicenseAsync (always runs)
  └── If no tenants exist:
      ├── CreateDefaultTenantAsync
      ├── AssignLicenseToTenantAsync
      ├── CreateSuperAdminUserAsync
      ├── CreateAdminTenantRecordAsync
      └── SeedTenantBaseEntitiesAsync (NEW)
          ├── SeedVatNaturesAsync
          ├── SeedVatRatesAsync
          ├── SeedUnitsOfMeasureAsync
          └── SeedDefaultWarehouseAsync
```

### Idempotency

All seeding methods check for existing data before inserting to ensure idempotency:

```csharp
var existingRates = await _dbContext.VatRates
    .AnyAsync(v => v.TenantId == tenantId, cancellationToken);

if (existingRates)
{
    _logger.LogInformation("VAT rates already exist for tenant {TenantId}", tenantId);
    return true;
}
```

This ensures that:
- Running bootstrap multiple times doesn't duplicate data
- The system is safe to restart during bootstrap
- Existing tenants are not affected by bootstrap updates

## Database Changes

### Migration: AddVatNatureAndBootstrapEnhancements

**Created**: 2025-01-05

**Changes**:
1. Created `VatNatures` table with all AuditableEntity columns
2. Added `VatNatureId` column to `VatRates` table (nullable)
3. Created foreign key relationship between `VatRates` and `VatNatures`
4. Created index on `VatRates.VatNatureId`

## Testing

### Manual Testing

1. **First Run (New System)**:
   ```bash
   # Delete existing database
   # Start application
   # Check logs for bootstrap messages
   ```

2. **Verify Seeded Data**:
   ```sql
   SELECT COUNT(*) FROM VatNatures; -- Should return 24
   SELECT COUNT(*) FROM VatRates; -- Should return 5
   SELECT COUNT(*) FROM UMs; -- Should return 19
   SELECT COUNT(*) FROM StorageFacilities; -- Should return 1
   SELECT COUNT(*) FROM StorageLocations; -- Should return 1
   ```

3. **Second Run (Existing System)**:
   ```bash
   # Restart application
   # Check logs to verify no duplication
   ```

### Expected Log Output

```
[INFO] Starting bootstrap process...
[INFO] Seeding base entities for tenant {TenantId}...
[INFO] Seeding VAT natures for tenant {TenantId}...
[INFO] Seeded 24 VAT natures for tenant {TenantId}
[INFO] Seeding VAT rates for tenant {TenantId}...
[INFO] Seeded 5 VAT rates for tenant {TenantId}
[INFO] Seeding units of measure for tenant {TenantId}...
[INFO] Seeded 20 units of measure for tenant {TenantId}
[INFO] Seeding default warehouse for tenant {TenantId}...
[INFO] Created default warehouse 'Magazzino Principale' with default location 'UB-DEF' for tenant {TenantId}
[INFO] Base entities seeded successfully for tenant {TenantId}
```

## Future Enhancements

Potential improvements for future versions:

1. **Customizable VAT Rates**: Allow configuration of VAT rates per country/region
2. **Additional Warehouse Templates**: Provide templates for different warehouse types
3. **Bulk Import**: Support importing custom units of measure and warehouse layouts
4. **VAT Nature Validation**: Add validation rules for VAT nature usage based on invoice type
5. **Multi-Country Support**: Extend VAT nature codes for other European countries
6. **UI Management**: Create UI components for managing VAT natures and rates

## References

- [Italian VAT Nature Codes - Agenzia delle Entrate](https://www.agenziaentrate.gov.it/)
- [Italian VAT Rates - Current Legislation](https://www.agenziaentrate.gov.it/)
- EF Core Migrations: `EventForge.Server/Migrations/20251005223454_AddVatNatureAndBootstrapEnhancements.cs`

## Notes

- All seeded data is tenant-scoped (includes TenantId)
- All seeded data includes audit fields (CreatedBy: "system", CreatedAt)
- The default unit of measure is "Pezzo" (IsDefault = true)
- The default warehouse is marked as fiscal (IsFiscal = true)
- All VAT rates are initially set to Active status
- All entities support soft delete through AuditableEntity inheritance
