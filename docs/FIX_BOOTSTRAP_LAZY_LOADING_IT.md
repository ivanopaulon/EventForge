# Fix Bootstrap - Caricamento Lazy dei Valori di Default

## Problema Riportato

Dopo aver ricreato il database, i valori di default (aliquote IVA, unità di misura, magazzino, ecc.) non vengono popolati. Era stato pensato precedentemente ad un caricamento lazy ma qualcosa non funzionava.

## Analisi del Problema

Il problema era causato da due ottimizzazioni nel processo di bootstrap che impedivano il caricamento dei dati di base in determinati scenari:

### 1. BootstrapHostedService - Fast-Path Check Troppo Aggressivo

Il servizio `BootstrapHostedService` aveva un controllo "fast-path" che verificava solo se l'utente superadmin esisteva, e se sì, saltava completamente il processo di bootstrap:

```csharp
// PRIMA (PROBLEMA)
var adminExists = await dbContext.Users.AnyAsync(u => u.Username == "superadmin", cancellationToken);
if (adminExists)
{
    _logger.LogInformation("Bootstrap already complete. Skipping bootstrap process for faster startup.");
    return; // ❌ Salta tutto il bootstrap anche se mancano i dati base
}
```

**Problema**: Se il database veniva ricreato ma l'utente superadmin esisteva (o veniva creato manualmente), il bootstrap veniva completamente saltato, lasciando i tenant senza i dati di base.

### 2. BootstrapService - Mancata Verifica dei Dati Base per Tenant Esistenti

Il servizio `BootstrapService` verificava solo se esistevano tenant, e se sì, assumeva che i dati base fossero già popolati:

```csharp
// PRIMA (PROBLEMA)
var existingTenants = await _dbContext.Tenants.AnyAsync(cancellationToken);
if (existingTenants)
{
    _logger.LogInformation("Tenants already exist. Bootstrap data update completed.");
    return true; // ❌ Non verifica se i dati base esistono effettivamente
}
```

**Problema**: Se il database veniva ricreato con tenant ma senza dati base (VAT rates, unità di misura, magazzino), il bootstrap non li creava.

## Soluzione Implementata

### 1. Miglioramento Fast-Path Check in BootstrapHostedService

Il controllo fast-path ora verifica **sia** l'esistenza del superadmin **che** la presenza dei dati base:

```csharp
// DOPO (CORRETTO)
var adminExists = await dbContext.Users.AnyAsync(u => u.Username == "superadmin", cancellationToken);
if (adminExists)
{
    // Verifica che esista almeno un tenant con dati base
    var tenantWithBaseEntities = await dbContext.Tenants
        .Where(t => t.Id != Guid.Empty)
        .AnyAsync(cancellationToken);

    if (tenantWithBaseEntities)
    {
        // Verifica che i dati base esistano effettivamente
        var hasVatRates = await dbContext.VatRates.AnyAsync(cancellationToken);
        var hasUnitsMeasure = await dbContext.UMs.AnyAsync(cancellationToken);
        var hasWarehouses = await dbContext.StorageFacilities.AnyAsync(cancellationToken);

        if (hasVatRates && hasUnitsMeasure && hasWarehouses)
        {
            // ✅ Tutti i dati base esistono, può saltare il bootstrap
            _logger.LogInformation("Bootstrap already complete. Skipping bootstrap process for faster startup.");
            return;
        }
        else
        {
            // ⚠️ Mancano alcuni dati base, esegue il bootstrap
            _logger.LogWarning("Superadmin exists but base entities are missing. Running bootstrap to seed base entities...");
        }
    }
}
```

### 2. Verifica e Seeding Automatico per Tenant Esistenti

Il `BootstrapService` ora verifica ogni tenant e popola i dati base mancanti:

```csharp
// DOPO (CORRETTO)
var existingTenants = await _dbContext.Tenants.ToListAsync(cancellationToken);
if (existingTenants.Any())
{
    _logger.LogInformation("Tenants already exist. Checking if base entities need to be seeded...");
    
    // Verifica ogni tenant
    foreach (var tenant in existingTenants)
    {
        // Salta il tenant di sistema
        if (tenant.Id == Guid.Empty)
            continue;

        // Verifica se questo tenant ha i dati base
        var hasVatNatures = await _dbContext.VatNatures.AnyAsync(v => v.TenantId == tenant.Id, cancellationToken);
        var hasVatRates = await _dbContext.VatRates.AnyAsync(v => v.TenantId == tenant.Id, cancellationToken);
        var hasUnitsMeasure = await _dbContext.UMs.AnyAsync(u => u.TenantId == tenant.Id, cancellationToken);
        var hasWarehouses = await _dbContext.StorageFacilities.AnyAsync(w => w.TenantId == tenant.Id, cancellationToken);

        // Se mancano dati base, li popola
        if (!hasVatNatures || !hasVatRates || !hasUnitsMeasure || !hasWarehouses)
        {
            _logger.LogWarning("Tenant {TenantId} ({TenantName}) is missing base entities. Seeding now...", 
                tenant.Id, tenant.Name);
            
            if (!await SeedTenantBaseEntitiesAsync(tenant.Id, cancellationToken))
            {
                _logger.LogError("Failed to seed base entities for tenant {TenantId}", tenant.Id);
                // Continua con gli altri tenant invece di fallire completamente
            }
            else
            {
                _logger.LogInformation("Successfully seeded base entities for tenant {TenantId} ({TenantName})", 
                    tenant.Id, tenant.Name);
            }
        }
    }
}
```

## Dati Base Popolati Automaticamente

Quando il bootstrap rileva che mancano i dati base per un tenant, popola automaticamente:

1. **Nature IVA**: 24 codici natura IVA italiani (N1, N2, N2.1, N3, N3.1-N3.6, N4, N5, N6, N6.1-N6.9, N7)
2. **Aliquote IVA**: 5 aliquote IVA standard (22%, 10%, 5%, 4%, 0%)
3. **Unità di Misura**: 20 unità di misura comuni (pz, kg, l, m, ecc.)
4. **Magazzino Predefinito**: Magazzino Principale (MAG-01)
5. **Ubicazione Predefinita**: UB-DEF
6. **Tipi Documento**: 12 tipi documento standard (DDT, Fatture, Ordini, ecc.)

## Test Implementati

È stato aggiunto un nuovo test specifico per verificare il fix:

```csharp
[Fact]
public async Task EnsureAdminBootstrappedAsync_WithTenantButMissingBaseEntities_ShouldSeedBaseEntities()
{
    // Simula un database ricreato con tenant ma senza dati base
    // 1. Crea tenant e dati base
    // 2. Rimuove tutti i dati base
    // 3. Esegue nuovamente il bootstrap
    // 4. Verifica che i dati base siano stati ricreati
}
```

### Risultati Test

Tutti i 9 test di bootstrap passano con successo:

- ✅ `EnsureAdminBootstrappedAsync_WithEmptyDatabase_ShouldCreateInitialData`
- ✅ `EnsureAdminBootstrappedAsync_WithExistingTenants_ShouldSkipBootstrap`
- ✅ `EnsureAdminBootstrappedAsync_WithEnvironmentPassword_ShouldUseEnvironmentValue`
- ✅ `EnsureAdminBootstrappedAsync_RunningTwice_ShouldUpdateLicenseConfiguration`
- ✅ `EnsureAdminBootstrappedAsync_WithExistingData_ShouldUpdateLicenseOnlyWithoutRecreatingTenant`
- ✅ `EnsureAdminBootstrappedAsync_ShouldAssignAllPermissionsToSuperAdminRole`
- ✅ `EnsureAdminBootstrappedAsync_WithNewTenant_ShouldSeedBaseEntities`
- ✅ `EnsureAdminBootstrappedAsync_RunTwice_ShouldNotDuplicateBaseEntities`
- ✅ `EnsureAdminBootstrappedAsync_WithTenantButMissingBaseEntities_ShouldSeedBaseEntities` ← **NUOVO**

## Log Migliorati

Il sistema ora fornisce log più dettagliati per aiutare nella diagnosi:

```
[INFO] Tenants already exist. Checking if base entities need to be seeded...
[WARN] Tenant {TenantId} ({TenantName}) is missing base entities. Seeding now...
[INFO] Seeding base entities for tenant {TenantId}...
[INFO] Seeding VAT natures for tenant {TenantId}...
[INFO] Seeded 24 VAT natures for tenant {TenantId}
[INFO] Seeding VAT rates for tenant {TenantId}...
[INFO] Seeded 5 VAT rates for tenant {TenantId}
[INFO] Seeding units of measure for tenant {TenantId}...
[INFO] Seeded 20 units of measure for tenant {TenantId}
[INFO] Seeding default warehouse for tenant {TenantId}...
[INFO] Created default warehouse 'Magazzino Principale' with default location 'UB-DEF' for tenant {TenantId}
[INFO] Seeding document types for tenant {TenantId}...
[INFO] Seeded 12 document types for tenant {TenantId}
[INFO] Successfully seeded base entities for tenant {TenantId} ({TenantName})
[INFO] Bootstrap data update completed.
```

## Idempotenza

Tutte le funzioni di seeding sono idempotenti:
- Verificano sempre se i dati esistono già prima di inserirli
- Possono essere eseguite più volte senza creare duplicati
- Il sistema è sicuro da riavviare durante il processo di bootstrap

## Come Testare il Fix

### Scenario 1: Database Nuovo
1. Elimina il database esistente
2. Avvia l'applicazione
3. Verifica nei log che vengano popolati tutti i dati base

### Scenario 2: Database Ricreato con Tenant
1. Crea manualmente un tenant nel database
2. Avvia l'applicazione
3. Verifica nei log che il bootstrap rilevi i dati mancanti e li popoli

### Scenario 3: Verifica Dati nel Database
```sql
SELECT COUNT(*) FROM VatNatures;        -- Dovrebbe restituire 24
SELECT COUNT(*) FROM VatRates;          -- Dovrebbe restituire 5
SELECT COUNT(*) FROM UMs;               -- Dovrebbe restituire 20
SELECT COUNT(*) FROM StorageFacilities; -- Dovrebbe restituire 1
SELECT COUNT(*) FROM StorageLocations;  -- Dovrebbe restituire 1
SELECT COUNT(*) FROM DocumentTypes;     -- Dovrebbe restituire 12
```

## File Modificati

1. **EventForge.Server/Services/Configuration/BootstrapHostedService.cs**
   - Migliorato il fast-path check per verificare anche i dati base

2. **EventForge.Server/Services/Auth/BootstrapService.cs**
   - Aggiunta verifica e seeding per tenant esistenti senza dati base

3. **EventForge.Tests/Services/Auth/BootstrapServiceTests.cs**
   - Aggiunto test per verificare il fix con database ricreato

## Conclusione

Il problema del caricamento lazy è stato risolto implementando:
1. Controlli più robusti per verificare la completezza dei dati base
2. Seeding automatico per tenant esistenti che mancano di dati base
3. Logging dettagliato per facilitare la diagnosi
4. Test specifici per garantire che il problema non si ripresenti

Ora, quando il database viene ricreato o un tenant esiste senza dati base, il sistema automaticamente rileva la situazione e popola tutti i valori di default necessari.
