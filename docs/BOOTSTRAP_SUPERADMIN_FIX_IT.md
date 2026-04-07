# EventForge - Correzione Bootstrap SuperAdmin

## Problema Identificato

Durante la verifica del processo di bootstrap, è stato identificato un problema critico:

**❌ Il ruolo SuperAdmin non aveva permessi assegnati durante il bootstrap**

### Analisi del Problema

Il codice originale assegnava tutti i permessi solo al ruolo "Admin", ma non al ruolo "SuperAdmin":

```csharp
// Codice originale - PROBLEMA
var adminRole = await _dbContext.Roles
    .Include(r => r.RolePermissions)
    .FirstOrDefaultAsync(r => r.Name == "Admin", cancellationToken);

if (adminRole != null && !adminRole.RolePermissions.Any())
{
    var allPermissions = await _dbContext.Permissions.ToListAsync(cancellationToken);
    
    foreach (var permission in allPermissions)
    {
        // Assegnava i permessi SOLO al ruolo Admin
        var rolePermission = new RolePermission { ... };
        _dbContext.RolePermissions.Add(rolePermission);
    }
}
// ❌ Nessuna assegnazione per il ruolo SuperAdmin!
```

### Impatto

- L'utente SuperAdmin aveva il ruolo SuperAdmin assegnato
- Ma il ruolo SuperAdmin era vuoto (senza permessi)
- Risultato: **L'utente SuperAdmin non aveva accesso a nulla nel sistema**

## Soluzione Implementata

### 1. Assegnazione Permessi a SuperAdmin Role

Ora il bootstrap assegna tutti i permessi sia al ruolo Admin che al ruolo SuperAdmin:

```csharp
// Get all permissions once for assigning to roles
var allPermissions = await _dbContext.Permissions.ToListAsync(cancellationToken);

// Assign permissions to Admin role
var adminRole = await _dbContext.Roles
    .Include(r => r.RolePermissions)
    .FirstOrDefaultAsync(r => r.Name == "Admin", cancellationToken);

if (adminRole != null && !adminRole.RolePermissions.Any())
{
    foreach (var permission in allPermissions)
    {
        var rolePermission = new RolePermission
        {
            RoleId = adminRole.Id,
            PermissionId = permission.Id,
            GrantedBy = "system",
            GrantedAt = DateTime.UtcNow,
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow,
            TenantId = Guid.Empty
        };
        _dbContext.RolePermissions.Add(rolePermission);
    }
    _logger.LogInformation("Assigned {Count} permissions to Admin role", allPermissions.Count);
}

// ✅ NUOVO: Assign permissions to SuperAdmin role
var superAdminRole = await _dbContext.Roles
    .Include(r => r.RolePermissions)
    .FirstOrDefaultAsync(r => r.Name == "SuperAdmin", cancellationToken);

if (superAdminRole != null && !superAdminRole.RolePermissions.Any())
{
    foreach (var permission in allPermissions)
    {
        var rolePermission = new RolePermission
        {
            RoleId = superAdminRole.Id,
            PermissionId = permission.Id,
            GrantedBy = "system",
            GrantedAt = DateTime.UtcNow,
            CreatedBy = "system",
            CreatedAt = DateTime.UtcNow,
            TenantId = Guid.Empty
        };
        _dbContext.RolePermissions.Add(rolePermission);
    }
    _logger.LogInformation("Assigned {Count} permissions to SuperAdmin role", allPermissions.Count);
}

await _dbContext.SaveChangesAsync(cancellationToken);
```

### 2. Logging Migliorato

Aggiunto logging specifico per tracciare l'assegnazione dei permessi:

- `"Assigned {Count} permissions to Admin role"` - conferma permessi Admin
- `"Assigned {Count} permissions to SuperAdmin role"` - conferma permessi SuperAdmin

## Processo di Bootstrap Completo

Il processo di bootstrap ora funziona correttamente nel seguente ordine:

### 1. Seed Ruoli e Permessi (sempre)
```
✅ Crea/aggiorna 70 permessi (TenantId = Guid.Empty)
✅ Crea/aggiorna 5 ruoli (TenantId = Guid.Empty)
   - SuperAdmin, Admin, Manager, User, Viewer
✅ Assegna TUTTI i permessi al ruolo Admin
✅ Assegna TUTTI i permessi al ruolo SuperAdmin ← NUOVO
```

### 2. Crea/Aggiorna Licenza SuperAdmin (sempre)
```
✅ Nome: "superadmin"
✅ DisplayName: "SuperAdmin License"
✅ MaxUsers: int.MaxValue (illimitati)
✅ MaxApiCallsPerMonth: int.MaxValue (illimitate)
✅ TierLevel: 5 (massimo)
✅ 16 feature abilitate
✅ TenantId = Guid.Empty
```

### 3. Se Nessun Tenant Esiste

#### 3.1 Crea Tenant di Default
```
✅ Nome: "DefaultTenant"
✅ Code: "default"
✅ DisplayName: "Default Tenant"
✅ ContactEmail: "superadmin@localhost"
✅ Domain: "localhost"
✅ MaxUsers: 10
✅ TenantId = Guid.Empty (sistema)
```

#### 3.2 Assegna Licenza al Tenant
```
✅ Crea TenantLicense
✅ Collega SuperAdmin license al default tenant
✅ TenantId = Guid.Empty (sistema)
```

#### 3.3 Crea Utente SuperAdmin
```
✅ Username: "superadmin"
✅ Email: "superadmin@localhost"
✅ Password: da configurazione/environment
✅ TenantId = [ID del tenant di default]
✅ Assegna ruolo SuperAdmin all'utente
```

#### 3.4 Crea Record AdminTenant
```
✅ Collega utente SuperAdmin al tenant di default
✅ AccessLevel: FullAccess
✅ TenantId = Guid.Empty (sistema)
```

## Relazioni TenantId Corrette

Tutte le entità hanno il `TenantId` corretto:

### Entità Sistema (TenantId = Guid.Empty)
- ✅ **Permissions** - disponibili per tutti i tenant
- ✅ **Roles** - disponibili per tutti i tenant
- ✅ **RolePermissions** - associazioni a livello sistema
- ✅ **License (SuperAdmin)** - licenza sistema
- ✅ **LicenseFeatures** - feature della licenza sistema
- ✅ **Tenant (default)** - tenant di sistema
- ✅ **TenantLicense** - assegnazione a livello sistema
- ✅ **AdminTenant** - gestione a livello sistema

### Entità Tenant (TenantId = [Tenant ID])
- ✅ **User (SuperAdmin)** - appartiene al tenant di default
- ✅ **UserRole** - assegnazione ruolo nell'ambito del tenant

## Flusso di Autorizzazione

Quando l'utente SuperAdmin effettua il login:

1. **Caricamento Dati**
```csharp
var user = await _dbContext.Users
    .Include(u => u.UserRoles)
        .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
    .FirstOrDefaultAsync(...);
```

2. **Estrazione Ruoli**
```csharp
var roles = user.UserRoles
    .Where(ur => ur.IsCurrentlyValid)
    .Select(ur => ur.Role.Name)
    .ToList();
// Risultato: ["SuperAdmin"]
```

3. **Estrazione Permessi**
```csharp
var permissions = user.UserRoles
    .Where(ur => ur.IsCurrentlyValid)
    .SelectMany(ur => ur.Role.RolePermissions)
    .Select(rp => $"{rp.Permission.Category}.{rp.Permission.Resource}.{rp.Permission.Action}")
    .Distinct()
    .ToList();
// Risultato: TUTTI i 70 permessi del sistema
```

4. **Generazione Token JWT**
```csharp
var token = _jwtTokenService.GenerateToken(user, user.Tenant, roles, permissions);
// Token contiene: ruoli + tutti i permessi
```

5. **Accesso alle Risorse**
```csharp
[Authorize(Policy = "RequireAdmin")]  // Controlla ruolo
[Authorize(Policy = "CanManageUsers")]  // Controlla permesso specifico
```

Le policy di autorizzazione verificano:
- **RequireAdmin**: `RequireRole("Admin", "SuperAdmin")` ✅
- **CanManageUsers**: `RequireClaim("permission", "Users.Users.Create", ...)` ✅

## Test Implementati

### Test Nuovo: Verifica Assegnazione Permessi SuperAdmin

```csharp
[Fact]
public async Task EnsureAdminBootstrappedAsync_ShouldAssignAllPermissionsToSuperAdminRole()
{
    // Verifica che:
    // 1. Il ruolo SuperAdmin esista
    // 2. Il ruolo SuperAdmin abbia TUTTI i permessi
    // 3. Il ruolo Admin abbia TUTTI i permessi
    // 4. L'utente SuperAdmin abbia il ruolo SuperAdmin
    // 5. Tutti i RolePermissions abbiano TenantId = Guid.Empty
}
```

### Risultati Test
```
✅ EnsureAdminBootstrappedAsync_WithEmptyDatabase_ShouldCreateInitialData
✅ EnsureAdminBootstrappedAsync_WithExistingTenants_ShouldSkipBootstrap
✅ EnsureAdminBootstrappedAsync_WithEnvironmentPassword_ShouldUseEnvironmentValue
✅ EnsureAdminBootstrappedAsync_RunningTwice_ShouldUpdateLicenseConfiguration
✅ EnsureAdminBootstrappedAsync_WithExistingData_ShouldUpdateLicenseOnlyWithoutRecreatingTenant
✅ EnsureAdminBootstrappedAsync_ShouldAssignAllPermissionsToSuperAdminRole ← NUOVO

6/6 test passati
```

## Benefici della Soluzione

### 1. Accesso Completo SuperAdmin
✅ L'utente SuperAdmin ora ha accesso illimitato a tutte le funzionalità
✅ Nessuna limitazione sui permessi o sulle risorse
✅ Può gestire completamente il sistema dal primo avvio

### 2. Conformità al Modello
✅ I ruoli Admin e SuperAdmin hanno entrambi tutti i permessi
✅ Coerenza nella struttura dei permessi
✅ Supporto per policy di autorizzazione basate su ruoli e permessi

### 3. Manutenibilità
✅ Il codice è chiaro e ben documentato
✅ I log tracciano l'assegnazione dei permessi
✅ I test verificano il comportamento corretto

### 4. Sicurezza
✅ I permessi sono esplicitamente assegnati
✅ Nessun accesso implicito non documentato
✅ Audit trail completo dell'assegnazione permessi

## Conclusione

La correzione implementata risolve il problema critico del bootstrap:

✅ **Il ruolo SuperAdmin ora riceve tutti i permessi durante il bootstrap**  
✅ **L'utente SuperAdmin ha accesso completo al sistema senza limitazioni**  
✅ **Tutti i permessi, ruoli e associazioni sono correttamente configurati**  
✅ **Il processo di bootstrap è completamente testato e verificato**

Il sistema ora funziona correttamente con:
- ✅ Tenant di default creato
- ✅ Ruoli e permessi configurati
- ✅ Utente SuperAdmin con accesso completo
- ✅ Licenza SuperAdmin con risorse illimitate
- ✅ Tutte le associazioni corrette (tenant, ruoli, permessi, licenze)

---

**Data:** 2025-01-09  
**Issue:** Bootstrap SuperAdmin Permissions Fix  
**Stato:** ✅ RISOLTO E TESTATO
