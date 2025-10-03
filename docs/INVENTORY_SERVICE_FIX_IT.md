# Fix Servizio Inventario - Allineamento Pattern HTTP Client

## Problema Identificato

Il servizio `InventoryService` utilizzava `IHttpClientFactory` direttamente invece di `IHttpClientService`, causando potenziali problemi di:
- Risoluzione endpoint (BaseAddress non sempre configurato)
- Gestione autenticazione manuale
- Gestione errori non standardizzata
- Mancanza di retry logic e resilienza

## Confronto con Servizi Funzionanti

### ❌ Pattern Errato (InventoryService - PRIMA)
```csharp
public class InventoryService : IInventoryService
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    public async Task<InventoryEntryDto?> CreateInventoryEntryAsync(CreateInventoryEntryDto createDto)
    {
        var httpClient = _httpClientFactory.CreateClient("ApiClient");
        var response = await httpClient.PostAsJsonAsync(BaseUrl, createDto);
        
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<InventoryEntryDto>(json, ...);
        }
        
        _logger.LogError("Failed to create inventory entry. Status: {StatusCode}", response.StatusCode);
        return null;
    }
}
```

### ✅ Pattern Corretto (WarehouseService, UMService, BusinessPartyService)
```csharp
public class WarehouseService : IWarehouseService
{
    private readonly IHttpClientService _httpClientService;
    
    public async Task<StorageFacilityDto?> CreateStorageFacilityAsync(CreateStorageFacilityDto dto)
    {
        try
        {
            return await _httpClientService.PostAsync<CreateStorageFacilityDto, StorageFacilityDto>(BaseUrl, dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating storage facility");
            return null;
        }
    }
}
```

## Soluzione Implementata

### Modifiche Applicate

1. **Dependency Injection**
   - ❌ `IHttpClientFactory _httpClientFactory`
   - ✅ `IHttpClientService _httpClientService`

2. **Rimozione Codice Boilerplate**
   - Eliminata gestione manuale HttpClient
   - Eliminata serializzazione/deserializzazione JSON manuale
   - Eliminata gestione response status codes manuale
   - Riduzione codice da 226 a 133 righe (-41%)

3. **Metodi Semplificati**

#### GET Requests
```csharp
// PRIMA (23 righe)
var httpClient = _httpClientFactory.CreateClient("ApiClient");
var response = await httpClient.GetAsync($"{BaseUrl}?page={page}&pageSize={pageSize}");
if (response.IsSuccessStatusCode) {
    var json = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<PagedResult<InventoryEntryDto>>(json, ...);
}
_logger.LogError("Failed to retrieve inventory entries. Status: {StatusCode}", response.StatusCode);
return null;

// DOPO (1 riga)
return await _httpClientService.GetAsync<PagedResult<InventoryEntryDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}");
```

#### POST Requests
```csharp
// PRIMA (22 righe)
var httpClient = _httpClientFactory.CreateClient("ApiClient");
var response = await httpClient.PostAsJsonAsync(BaseUrl, createDto);
if (response.IsSuccessStatusCode) {
    var json = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<InventoryEntryDto>(json, ...);
}
_logger.LogError("Failed to create inventory entry. Status: {StatusCode}", response.StatusCode);
return null;

// DOPO (1 riga)
return await _httpClientService.PostAsync<CreateInventoryEntryDto, InventoryEntryDto>(BaseUrl, createDto);
```

## Verifica Endpoint

Verificato che tutti gli endpoint sono corretti confrontando con il backend:

### Controller Backend: `WarehouseManagementController`
- Base Route: `[Route("api/v1/warehouse")]`

### Endpoint Inventario
✅ GET    `api/v1/warehouse/inventory` → GetInventoryEntriesAsync  
✅ POST   `api/v1/warehouse/inventory` → CreateInventoryEntryAsync  
✅ GET    `api/v1/warehouse/inventory/documents` → GetInventoryDocumentsAsync  
✅ GET    `api/v1/warehouse/inventory/document/{id}` → GetInventoryDocumentAsync  
✅ POST   `api/v1/warehouse/inventory/document/start` → StartInventoryDocumentAsync  
✅ POST   `api/v1/warehouse/inventory/document/{id}/row` → AddInventoryDocumentRowAsync  
✅ POST   `api/v1/warehouse/inventory/document/{id}/finalize` → FinalizeInventoryDocumentAsync  

Tutti gli endpoint corrispondono perfettamente tra frontend e backend.

## Benefici Ottenuti

### 1. BaseAddress Sempre Configurato
`IHttpClientService` usa il client "ApiClient" configurato in `Program.cs` con BaseAddress garantito.

### 2. Autenticazione Automatica
Token JWT iniettato automaticamente tramite `IAuthService` senza gestione manuale.

### 3. Gestione Errori Centralizzata
- Parsing automatico di `ProblemDetails`
- Messaggi errore user-friendly
- Notifiche Snackbar automatiche per errori critici
- Logging centralizzato con correlation ID

### 4. Resilienza Integrata
- Retry logic gestita da Polly
- Circuit breaker per protezione da cascading failures
- Timeout configurati correttamente

### 5. Codice Più Pulito
- 93 righe di codice in meno (-41%)
- Meno duplicazione
- Più facile da mantenere
- Pattern consistente con altri servizi

## Test e Validazione

### Build
```bash
dotnet build
# Result: Success - 0 Errors
```

### Test Suite
```bash
dotnet test
# Result: Passed! - Failed: 0, Passed: 208, Skipped: 0
```

### Confronto con Altri Servizi
✅ Pattern identico a `WarehouseService`  
✅ Pattern identico a `UMService`  
✅ Pattern identico a `BusinessPartyService`  
✅ Pattern identico a `FinancialService`  

## Conclusione

Il servizio `InventoryService` è stato completamente allineato con il pattern standard utilizzato dagli altri servizi funzionanti del sistema. Questo garantisce:

- ✅ Endpoint risolti correttamente
- ✅ Autenticazione gestita automaticamente
- ✅ Errori gestiti in modo user-friendly
- ✅ Codice più pulito e manutenibile
- ✅ Consistenza con il resto della codebase

## Servizi Correlati da Considerare (Opzionale)

Altri servizi che potrebbero beneficiare dello stesso allineamento (non richiesto per questo task):
- LotService
- StorageLocationService
- ProductService
- LicenseService

Questi sono esclusi dal corrente task ma potrebbero essere allineati in future iterazioni.
