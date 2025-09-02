# License Management System - EventForge

Questo documento descrive il sistema di gestione delle licenze implementato per EventForge.

## Panoramica

Il sistema di licensing permette di:
- Definire diverse tipologie di licenze con limitazioni specifiche
- Assegnare licenze ai tenant
- Controllare l'accesso alle funzionalità API basato sulla licenza
- Monitorare l'utilizzo delle API e i limiti degli utenti

## Componenti Implementati

### 1. Entità del Database

#### `License`
- Definisce i tipi di licenza disponibili
- Proprietà: Nome, Descrizione, Max Utenti, Max API Calls, Tier Level

#### `LicenseFeature`
- Funzionalità disponibili in una licenza
- Collegatae alle permission necessarie

#### `LicenseFeaturePermission`
- Mapping tra features e permissions richieste

#### `TenantLicense`
- Assegnazione di una licenza a un tenant
- Include date di validità e contatori API

### 2. DTOs (Data Transfer Objects)

- `LicenseDto`: Informazioni complete della licenza
- `LicenseFeatureDto`: Dettagli delle funzionalità
- `TenantLicenseDto`: Informazioni licenza assegnata a tenant
- `CreateLicenseDto`: Creazione nuova licenza
- `AssignLicenseDto`: Assegnazione licenza a tenant
- `ApiUsageDto`: Statistiche utilizzo API

### 3. Controller API

#### `LicenseController`
Endpoints disponibili:
- `GET /api/license` - Lista tutte le licenze
- `GET /api/license/{id}` - Dettagli licenza specifica
- `POST /api/license` - Crea nuova licenza (SuperAdmin only)
- `PUT /api/license/{id}` - Aggiorna licenza (SuperAdmin only)
- `DELETE /api/license/{id}` - Elimina licenza (SuperAdmin only)
- `GET /api/license/tenant-licenses` - Lista licenze assegnate ai tenant
- `POST /api/license/assign` - Assegna licenza a tenant (SuperAdmin only)
- `GET /api/license/tenant/{tenantId}` - Licenza di un tenant specifico

### 4. Servizi

#### `ILicenseService` / `LicenseService`
Servizi per la gestione delle licenze:
- `HasFeatureAccessAsync()` - Verifica accesso a una funzionalità
- `GetTenantLicenseAsync()` - Ottiene licenza di un tenant
- `IsWithinApiLimitsAsync()` - Verifica limiti API
- `IncrementApiCallAsync()` - Incrementa contatore API
- `GetApiUsageAsync()` - Statistiche utilizzo API
- `CanAddUserAsync()` - Verifica se è possibile aggiungere utenti
- `GetAvailablePermissionsAsync()` - Permissions disponibili per tenant

### 5. Filtri di Autorizzazione

#### `RequireLicenseFeatureAttribute`
Filtro che può essere applicato ai controller per verificare:
- Licenza valida del tenant
- Accesso alla funzionalità richiesta
- Limiti API non superati
- Permissions utente necessarie

## Utilizzo

### Esempio 1: Proteggere un Controller con License Feature

```csharp
[RequireLicenseFeature("EventManagement")]
public class EventsController : BaseApiController
{
    // Questo controller richiede la feature "EventManagement"
}
```

### Esempio 2: Proteggere un Endpoint Specifico

```csharp
[HttpPost]
[RequireLicenseFeature("PremiumFeatures", checkApiLimits: true)]
public async Task<ActionResult> CreateAdvancedEvent(CreateEventDto dto)
{
    // Questo endpoint richiede "PremiumFeatures" e verifica i limiti API
}
```

### Esempio 3: Verificare Programmaticamente l'Accesso

```csharp
public class SomeService
{
    private readonly ILicenseService _licenseService;

    public async Task<bool> CanPerformAction(Guid tenantId)
    {
        return await _licenseService.HasFeatureAccessAsync(tenantId, "AdvancedReporting");
    }
}
```

## Configurazione delle Licenze

### Creazione di una Licenza Base

```json
{
  "name": "basic",
  "displayName": "Basic License",
  "description": "Licenza base con funzionalità essenziali",
  "maxUsers": 10,
  "maxApiCallsPerMonth": 1000,
  "tierLevel": 1,
  "isActive": true
}
```

### Creazione di una Licenza Premium

```json
{
  "name": "premium",
  "displayName": "Premium License", 
  "description": "Licenza premium con tutte le funzionalità",
  "maxUsers": 100,
  "maxApiCallsPerMonth": 50000,
  "tierLevel": 3,
  "isActive": true
}
```

### Assegnazione Licenza a Tenant

```json
{
  "tenantId": "12345678-1234-1234-1234-123456789012",
  "licenseId": "87654321-4321-4321-4321-210987654321",
  "startsAt": "2024-01-01T00:00:00Z",
  "expiresAt": "2024-12-31T23:59:59Z",
  "isActive": true
}
```

## Migrazione Database

Il sistema include una migrazione EF Core chiamata `AddLicensingSystem` che crea tutte le tabelle necessarie:
- `Licenses`
- `LicenseFeatures` 
- `LicenseFeaturePermissions`
- `TenantLicenses`

## Funzionalità Avanzate

### Reset Automatico API Calls
Il sistema resetta automaticamente il contatore delle chiamate API ogni mese.

### Licenze con Scadenza
Le licenze possono avere date di scadenza che vengono verificate automaticamente.

### Controllo Utenti Massimi
Il sistema verifica che il numero di utenti non superi il limite della licenza.

### Audit Trail
Tutte le operazioni sulle licenze sono tracciate attraverso il sistema di audit esistente.

## Sicurezza

- Solo i SuperAdmin possono creare, modificare ed eliminare licenze
- Le verifiche delle licenze sono integrate nel sistema di autorizzazione
- I limiti API sono applicati in tempo reale
- Tutte le operazioni sono loggate per audit

## Performance

- Le query sono ottimizzate con Include appropriati
- I contatori API sono aggiornati in modo efficiente
- Le verifiche delle licenze utilizzano caching quando possibile
- Il reset mensile delle API è automatico e performante