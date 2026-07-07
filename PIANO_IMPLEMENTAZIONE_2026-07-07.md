# PIANO_IMPLEMENTAZIONE_2026-07-07 — EventForge

**Basato su:** `docs/STATO_CODEBASE_2026-07-07.md`  
**Data:** 2026-07-07  
**Nota:** il **punto 5** (P05 — BuildServiceProvider() in Program.cs) è escluso da questo piano per decisione esplicita. Gli altri 17 problemi sono pianificati integralmente.  
**Metodologia:** ogni fase è autonomamente eseguibile e verificabile. Non procedere alla fase successiva senza verifica.

---

## Executive Summary

| Problemi identificati | 18 |
|---|---|
| Pianificati in questo documento | 17 (P01–P04, P06–P18; P05 escluso) |
| Milestone | 5 |
| Sforzo totale stimato | M–L (la maggior parte S o XS) |
| Errori di build attuali | 0 |
| Test attuali | 1419 passed |

---

## FASE A — Sicurezza critica (P01, P02)
**Obiettivo:** eliminare le vulnerabilità di sicurezza ad alta priorità prima di qualsiasi altro intervento.

---

### Task A1 — Rimuovere credenziali dev da appsettings.json [P01]

> **⚠️ Checkpoint umano obbligatorio:** questo task richiede che ogni sviluppatore crei `appsettings.Development.json` locale prima di avviare il progetto.

**Problema:** La sezione principale di `EventForge.Server/appsettings.json` (righe 22-71) contiene password e chiavi in chiaro tracciate in git. Il PIANO_CORREZIONE.md dichiarava questo task "completato" in M1-A2 ma la verifica mostra che **non è mai stato eseguito sulla sezione Development**.

**File da modificare:**
- `EventForge.Server/appsettings.json`
- `Prym.Agent/appsettings.json`
- `.gitignore`

**Azioni specifiche:**

1. In `EventForge.Server/appsettings.json`, sostituire:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost\\SQLEXPRESS;...",   // con "Server=REPLACE_SQL_HOST;Database=EventData;User Id=REPLACE_SQL_USER;******;TrustServerCertificate=True;"
     "SqlServer": "...",                                       // stessa sostituzione
     "LogDb": "..."                                            // stessa sostituzione
   },
   "Encryption": {
     "Key": "DevelopmentEncryptionKey-ChangeInProduction!!"   // → "REPLACE_WITH_STRONG_ENCRYPTION_KEY_32CHARS_MINIMUM"
   },
   "Authentication": {
     "Jwt": {
       "SecretKey": "DevelopmentSecretKeyThatIsAtLeast32CharsLong!!"  // → "REPLACE_WITH_STRONG_JWT_SECRET_MIN_32_CHARS"
     }
   },
   "Bootstrap": {
     "SuperAdminPassword": "EventForge@2024!",    // → "REPLACE_WITH_SUPERADMIN_PASSWORD"
     "StoreOperatorPassword": "Operator@2025!"   // → "REPLACE_WITH_OPERATOR_PASSWORD"
   }
   ```

2. Nella sezione `Environments.Development`:
   ```json
   "UpdateHub": {
     "AdminApiKey": "dev-admin-key",                          // → "REPLACE_WITH_UPDATEHUB_ADMIN_KEY"
     "MaintenanceSecret": "dev-maintenance-secret-2024"      // → "REPLACE_WITH_MAINTENANCE_SECRET"
   },
   "Agent": {
     "Password": "Admin#123!"                                 // → "REPLACE_WITH_AGENT_PASSWORD"
   }
   ```

3. In `Prym.Agent/appsettings.json`: verificare e sostituire tutte le password in chiaro con `REPLACE_*` placeholder.

4. Creare `EventForge.Server/appsettings.Development.json.example` come template con i valori dev sicuri per onboarding team:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=EventData;User Id=vsapp;******;TrustServerCertificate=True;"
     }
   }
   ```

5. Verificare che `.gitignore` (root) contenga:
   ```
   appsettings.Development.json
   appsettings.Production.json
   appsettings.*.json
   !appsettings.json
   !appsettings.Development.json.example
   ```

**Verifica:**
```bash
grep -rn "pass123!\|EventForge@2024!\|Admin#123!\|dev-admin-key\|DevelopmentEncryption\|DevelopmentSecretKey\|Operator@2025!\|dev-maintenance" EventForge.Server/appsettings.json Prym.Agent/appsettings.json
# → 0 risultati
```

**Effetti collaterali:** ogni sviluppatore deve configurare `appsettings.Development.json` prima del primo avvio.  
**Rollback:** `git checkout -- EventForge.Server/appsettings.json Prym.Agent/appsettings.json`

---

### Task A2 — Pinnare SQLitePCLRaw.lib.e_sqlite3 a versione patched [P02]

**Problema:** `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 ha una vulnerabilità HIGH (GHSA-2m69-gcr7-jv3q). Non è pinnato in `Directory.Packages.props`.

**File da modificare:** `Directory.Packages.props`

**Azione:**
1. Identificare la versione patched:
   ```bash
   # Consultare https://github.com/advisories/GHSA-2m69-gcr7-jv3q
   # La versione patched è SQLitePCLRaw.lib.e_sqlite3 >= 2.1.12 (verificare)
   ```

2. Aggiungere in `Directory.Packages.props` nella sezione comment `<!-- Security: pin transitive packages to patched versions -->`:
   ```xml
   <!-- GHSA-2m69-gcr7-jv3q: DoS in SQLitePCLRaw.lib.e_sqlite3 <= 2.1.11; fixed in 2.1.12+ -->
   <PackageVersion Include="SQLitePCLRaw.lib.e_sqlite3" Version="2.1.12" />
   ```

3. Eseguire build e verificare che il warning NU1903 scompaia.

**Verifica:**
```bash
dotnet build EventForge.sln 2>&1 | grep "GHSA-2m69"
# → 0 risultati
dotnet test EventForge.Tests/ --no-build 2>&1 | grep -E "Failed|Passed"
# → 0 Failed
```

**Effetti collaterali:** minimi; il pinning di package transitivi è già abilitato con `CentralPackageTransitivePinningEnabled=true`.

---

## FASE B — Debito tecnico async critico (P03)
**Obiettivo:** eliminare il blocking call in DbContext che può causare deadlock in produzione.

---

### Task B1 — DbContext.SaveChanges() — implementazione sincrona senza deadlock [P03]

**Problema:** `EventForgeDbContext.SaveChanges()` (riga 367) chiama `SaveChangesAsync().GetAwaiter().GetResult()`, bloccando il thread. Il metodo è necessario perché le audit trail sono implementate in `SaveChangesAsync()`.

**File da modificare:** `EventForge.Server/Data/EventForgeDbContext.cs`

**Strategia:** estrarre la logica audit (applicazione delle entità auditabili) in un metodo privato sincrono `ApplyAuditTrail()` chiamabile sia da `SaveChanges()` che da `SaveChangesAsync()`.

**Azioni specifiche:**

1. Analizzare `SaveChangesAsync()` per identificare la logica di audit trail (probabilmente in `OnBeforeSaveChanges()` o simile).

2. Estrarre la parte sincrona dell'audit (modifica degli `EntityEntry` — che è sincrona) in un metodo `private void ApplyAuditEntries()`.

3. Riscrivere `SaveChanges()` come:
   ```csharp
   public override int SaveChanges()
   {
       ApplyAuditEntries();        // sincrono, nessun await
       return base.SaveChanges();  // chiamata EF Core nativa sincrona
   }
   
   public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
   {
       ApplyAuditEntries();
       return await base.SaveChangesAsync(cancellationToken);
   }
   ```

4. `GetCurrentUser()` è già sincrono (legge da `IHttpContextAccessor`) — nessuna modifica richiesta.

**Verifica:**
```bash
grep -n "GetAwaiter().GetResult()" EventForge.Server/Data/EventForgeDbContext.cs
# → 0 risultati
dotnet build EventForge.Server/ 2>&1 | grep -E "error|Error"
# → 0 errori
dotnet test EventForge.Tests/ --no-build
# → 0 Failed
```

**Rischio regressione:** MEDIO — testare con test suite completa e verifica manuale del salvataggio di entità auditabili.  
**Rollback:** `git checkout -- EventForge.Server/Data/EventForgeDbContext.cs`

---

## FASE C — Sicurezza e hardening (P04, P06)
**Obiettivo:** ridurre la superficie di attacco dei layer CORS e autenticazione.

---

### Task C1 — Restringere CORS policy [P04]

**File da modificare:** `EventForge.Server/Program.cs`

**Azione:** sostituire `AllowAnyHeader()` + `AllowAnyMethod()` con headers e metodi espliciti:

```csharp
// PRIMA
_ = policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials();

// DOPO
_ = policy
    .WithOrigins(allowedOrigins)
    .WithHeaders(
        "Authorization", "Content-Type", "Accept",
        "X-Requested-With", "X-SignalR-*",
        "X-Maintenance-Secret", "X-Agent-Internal-Token",
        "Baggage", "traceparent", "tracestate")  // OpenTelemetry headers
    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
    .AllowCredentials(); // Required for SignalR WebSocket connections
```

**Attenzione:** verificare che SignalR, BoldReports e altri componenti non usino header o metodi non inclusi nella whitelist.

**Verifica:**
```bash
dotnet build EventForge.Server/ 2>&1 | grep -E "error|Error"
# Test manuale SignalR: aprire app e verificare che le notifiche real-time funzionino
# Test manuale BoldReports: aprire un report e verificarlo
```

---

### Task C2 — Hardening AgentLogsController AllowAnonymous [P06]

**File da modificare:** `EventForge.Server/Controllers/AgentLogsController.cs`, `EventForge.Server/Program.cs` (o equivalente middleware)

**Azione:** aggiungere una policy personalizzata che verifica `X-Maintenance-Secret` a livello di policy di autorizzazione, non di logica applicativa:

```csharp
// In Program.cs — AddAuthorization:
services.AddAuthorization(options =>
{
    options.AddPolicy("MaintenanceSecret", policy =>
        policy.RequireAssertion(ctx =>
        {
            var req = ctx.Resource as HttpContext ?? 
                      ctx.User.Claims.First(); // fallback
            // Estrai header dal HttpContext
            ...
        }));
});

// In AgentLogsController — sostituire [AllowAnonymous] con:
[Authorize(Policy = "MaintenanceSecret")]
```

**Alternativa più semplice:** mantenere `[AllowAnonymous]` ma aggiungere un requirement esplicito nel middleware o un middleware dedicato `UseMaintenanceSecretAuth()`. Documentare la scelta nel commento del controller.

**Verifica:** test manuale invio log dall'Agent sia con secret corretto che senza.

---

## FASE D — Debito tecnico strategico (P07, P08, P09)
**Obiettivo:** completare le migrazioni di campi obsoleti avviate nelle milestone precedenti.

---

### Task D1 — Rimozione finale Product.ImageUrl [P07]

> **⚠️ Checkpoint umano obbligatorio:** la rimozione della colonna DB richiede approvazione. Eseguire step 1 prima di step 2.

**Step 1 — Eseguire migration SQL (checkpoint umano):**
```sql
-- File pronto: Migrations/20260706_RemoveImageUrlFromProducts.sql
-- Prerequisito: confermare con SELECT COUNT(*) FROM Products WHERE ImageUrl != '' AND ImageUrl IS NOT NULL;
-- Se count > 0: decidere se perdere i dati o migrarli a DocumentReference prima
BEGIN TRANSACTION;
ALTER TABLE Products DROP COLUMN ImageUrl;
COMMIT;
```

**Step 2 — Rimuovere campo dall'entità C# (dopo migration SQL approvata):**

File: `EventForge.Server/Data/Entities/Products/Product.cs`
- Rimuovere le righe 43-46 (`[Obsolete]`, `[MaxLength]`, `[Display]`, `public string ImageUrl`)

**Verifica:**
```bash
grep -rn "Product\.ImageUrl\|ImageUrl" EventForge.Server/ --include="*.cs" | grep -v "StoreUser\|StorePos\|ImageUploadResultDto\|ImageUrl ="
# → 0 risultati
dotnet build EventForge.Server/ 2>&1 | grep "error"
# → 0 errori
```

---

### Task D2 — Completare migrazione PromotionRule.CustomerGroupIds → BusinessPartyGroupIds [P08]

**File da modificare:**
- `EventForge.Server/Data/Entities/Promotions/PromotionRule.cs`
- `EventForge.Server/Services/Promotions/PromotionService.cs`
- `Migrations/` — nuova migration SQL per rimuovere colonna `CustomerGroupIds`

**Azioni:**

1. **Verificare se CustomerGroupIds ha dati in produzione:**
   ```sql
   SELECT COUNT(*) FROM PromotionRules WHERE CustomerGroupIds IS NOT NULL AND CustomerGroupIds != '[]';
   ```

2. **Se ci sono dati:** creare una migration di data migration che copia `CustomerGroupIds` → `BusinessPartyGroupIds` dove `BusinessPartyGroupIds` è null.

3. **In PromotionService.cs (riga 645-647):** rimuovere il fallback:
   ```csharp
   // PRIMA
   #pragma warning disable CS0618
   var groupIdsToCheck = rule.BusinessPartyGroupIds ?? rule.CustomerGroupIds;
   #pragma warning restore CS0618
   
   // DOPO
   var groupIdsToCheck = rule.BusinessPartyGroupIds;
   ```

4. **In PromotionRule.cs:** rimuovere il campo `[Obsolete] CustomerGroupIds`.

5. **Creare migration SQL:** `Migrations/20260707_RemoveCustomerGroupIdsFromPromotionRules.sql`

**Verifica:**
```bash
grep -rn "CustomerGroupIds" EventForge.Server/ --include="*.cs"
# → 0 risultati
dotnet test EventForge.Tests/ --no-build
# → 0 Failed
```

---

### Task D3 — Deprecazione formale GetAllTables endpoint non paginato [P09]

**File da modificare:**
- `EventForge.Server/Controllers/TableManagementController.cs`
- `EventForge.Server/Services/Sales/ITableManagementService.cs`
- `EventForge.Server/Services/Sales/TableManagementService.cs`
- `Prym.Web/Services/Sales/ITableManagementService.cs`
- `Prym.Web/Services/Sales/TableManagementService.cs`

**Azioni:**

1. **Verificare consumer Prym.Web:** cercate `GetAllTablesAsync` in tutte le pagine Razor e code-behind:
   ```bash
   grep -rn "GetAllTablesAsync\|GetAllTables" Prym.Web/ --include="*.cs" --include="*.razor"
   ```

2. **Se non usato da nessuna pagina:** rimuovere:
   - Endpoint `GetAllTables()` da `TableManagementController.cs`
   - Metodo `GetAllTablesAsync()` da `ITableManagementService.cs` e `TableManagementService.cs`
   - Metodo dal client Prym.Web

3. **Se ancora usato:** migrare il consumer all'endpoint paginato `/api/v1/tables?pageSize=9999`, poi rimuovere.

**Verifica:**
```bash
grep -rn "GetAllTables" . --include="*.cs" --include="*.razor"
# → 0 risultati
dotnet build EventForge.sln 2>&1 | grep error
```

---

## FASE E — Documentazione e code quality (P12, P13, P14, P16, P17)
**Obiettivo:** correggere tutti i warning di documentazione e i pragma senza commento.

Tutte le azioni in questa fase sono XS e possono essere eseguite in un unico commit.

---

### Task E1 — Fix XML malformato IProductService [P12]

**File:** `EventForge.Server/Services/Products/IProductService.cs:257`

Trovare il tag `<summary>` non chiuso e aggiungere `</summary>`.

---

### Task E2 — Aggiungere param tags mancanti ProductManagementController [P13]

**File:** `EventForge.Server/Controllers/ProductManagementController.cs` (metodo `GetProducts`, righe 70-71)

Aggiungere:
```xml
/// <param name="includeInactive">Se true, include anche i prodotti inattivi nel risultato.</param>
/// <param name="quickFilter">Filtro rapido per nome/codice prodotto.</param>
```

---

### Task E3 — Fix cref ambiguo StockReconciliationService [P14]

**File:** `EventForge.Server/Services/Warehouse/StockReconciliationService.cs:1660`

Cambiare `<see cref="Math.Abs"/>` → `<see cref="Math.Abs(decimal)"/>`.

---

### Task E4 — Aggiungere commenti reason ai pragma CS0618 [P17]

**File 1:** `EventForge.Server/Services/Promotions/PromotionService.cs:645`:
```csharp
// Reason: CustomerGroupIds is deprecated but may still exist in legacy rules (fallback until data migration D2 is complete)
#pragma warning disable CS0618
var groupIdsToCheck = rule.BusinessPartyGroupIds ?? rule.CustomerGroupIds;
#pragma warning restore CS0618
```

**File 2:** `EventForge.Server/Controllers/TableManagementController.cs:34`:
```csharp
// Reason: This endpoint is itself deprecated; GetAllTablesAsync is used only here (remove in Task D3)
#pragma warning disable CS0618
var tables = await tableService.GetAllTablesAsync(cancellationToken);
#pragma warning restore CS0618
```

---

### Task E5 — Documentare NotSupportedException nei FiscalPrinter closures [P16]

**File:** `EventForge.Server/Services/FiscalPrinting/EpsonFiscalPrinterService.Closure.cs:17`, `CustomFiscalPrinterService.Closure.cs:13`

Aggiungere commento XML che spiega perché il metodo non è supportato da questo tipo di stampante. Esempio:
```csharp
/// <summary>
/// Not supported by this printer model. Use a printer that implements full closure support.
/// </summary>
/// <exception cref="NotSupportedException">Always thrown — this operation is not available for this printer type.</exception>
```

---

## FASE F — Dipendenze (P15, P18)
**Obiettivo:** segnalare lo stato delle dipendenze outdated e dei warning build.

---

### Task F1 — Valutare upgrade Microsoft.OpenApi [P15]

**Situazione:** `Microsoft.OpenApi 2.7.5` è la versione richiesta da `Swashbuckle.AspNetCore 10.2.3` (fissata in PR #1362). Un upgrade a OpenApi 3.x richiederebbe Swashbuckle 11+.

**Azione:** monitorare il changelog di Swashbuckle per il rilascio di una versione 11.x stabile compatibile con .NET 10. Al momento non è richiesta alcuna azione immediata.

**Stato:** 🔵 Monitorare — nessuna azione immediata.

---

### Task F2 — MSB3026 transient warnings [P18]

**Situazione:** i warning MSB3026 sono race condition durante parallel build e non impattano il risultato.

**Azione:** aggiungere nel file `.gitignore` o in `.editorconfig` una soppressione del warning per CI:
```xml
<!-- In EventForge.Tests.csproj, se necessario: -->
<NoWarn>$(NoWarn);MSB3026</NoWarn>
```

**Stato:** 🔵 Opzionale — solo se il CI fallisce per questi warning.

---

## 5. Timeline e Milestone

### Milestone A — Sicurezza critica
**Contenuto:** A1 (credenziali), A2 (SQLitePCLRaw)  
**Pre-condizioni:** nessuna  
**Checkpoint umano:** richiesto per A1 (coordinamento team)  
**Verifica:** `grep -rn "pass123!\|EventForge@2024!" appsettings.json` → 0; build warning NU1903 assente

---

### Milestone B — Async debito critico
**Contenuto:** B1 (DbContext SaveChanges)  
**Pre-condizioni:** M-A completata  
**Rischio regressione:** MEDIO — eseguire test suite completa dopo  
**Verifica:** `grep -n "GetAwaiter().GetResult()" EventForgeDbContext.cs` → 0; 1419+ test passed

---

### Milestone C — Hardening sicurezza
**Contenuto:** C1 (CORS), C2 (AgentLogs)  
**Pre-condizioni:** nessuna (parallela a B se necessario)  
**Verifica:** test manuale SignalR + BoldReports post-deploy

---

### Milestone D — Debito tecnico strategico
**Contenuto:** D1 (ImageUrl DB+entity), D2 (CustomerGroupIds), D3 (GetAllTables)  
**Pre-condizioni:** D1 richiede checkpoint umano DB  
**Ordine consigliato:** D3 → D2 → D1 (dal più semplice al più rischioso)  
**Verifica:** 0 occorrenze dei simboli rimossi; test suite completa

---

### Milestone E — Documentazione e code quality
**Contenuto:** E1–E5 (tutti XS)  
**Pre-condizioni:** nessuna (eseguibile in qualsiasi ordine)  
**Verifica:** `dotnet build 2>&1 | grep CS1570\|CS1573\|CS0419` → 0

---

## 6. Problemi Esclusi

### P05 — BuildServiceProvider() in Program.cs (ESCLUSO per decisione esplicita)

> **Escluso dall'utente**: `lascia perdere il punto 5 per ora`

**File:** `EventForge.Server/Program.cs:357` e riga `613-615`  
**Problema:** `builder.Services.BuildServiceProvider()` durante startup crea copia aggiuntiva dei singleton.  
**Impatto:** memory waste, nessun impatto funzionale confermato.  
**Quando riprendere:** in una sessione dedicata, se il team decide di eliminare tutti i `#pragma warning disable ASP0001`.

---

### P10, P11 — FiscalPrintersDashboard e InventoryMerge TODO (SOSPESI — dipendenze esterne)

Richiedono feature EFTable SignalR real-time e WizardPage archetype non in roadmap corrente.

---

## 7. Comandi di Verifica Finale

Eseguire dopo ogni milestone:

```bash
# Build completo
dotnet build EventForge.sln 2>&1 | grep -E "^.*error|NU1903" | grep -v MSB3026

# Test suite
dotnet test EventForge.Tests/ --no-build 2>&1 | tail -5

# Sicurezza credenziali
grep -rn "pass123!\|EventForge@2024!\|Admin#123!\|dev-admin-key\|DevelopmentEncryption" \
  EventForge.Server/appsettings.json Prym.Agent/appsettings.json

# Simboli obsoleti
grep -rn "GetAwaiter().GetResult()" EventForge.Server/Data/ --include="*.cs"
grep -rn "CustomerGroupIds\b" EventForge.Server/ --include="*.cs"
grep -rn "Product\.ImageUrl\b" EventForge.Server/ --include="*.cs"
```

---

*Fine documento — generato il 2026-07-07*
