# STATO CODEBASE EventForge — Audit del 2026-07-07

**Generato da:** Audit automatico post-PR #1362 (Swagger fix)  
**Data:** 2026-07-07  
**Stato build:** ✅ 0 errori, 8 warning (2 NU1903, 6 MSB3026 transienti)  
**Stato test:** ✅ 1419 passed, 4 skipped, 0 failed  
**Branch:** master (HEAD `e5ad710`)

---

## 1. Contesto

Questo documento descrive lo stato **attuale** del codebase dopo la serie di milestone eseguite il 2026-07-06 e il fix Swagger di PR #1362. Tutti i problemi elencati di seguito sono **problemi reali e aperti** non presenti in PIANO_CORREZIONE.md o non ancora risolti nonostante dichiarati completati.

### Milestone già completate (da NON re-implementare)
| Milestone | Stato |
|-----------|-------|
| M1-A1: EPPlus → ClosedXML | ✅ Completato |
| M1-A2: Credenziali appsettings (**parziale**) | ⚠️ INCOMPLETO — vedi P02 |
| M2: File orfani, SHIFTS archiviati, FluentValidation | ✅ Completato |
| M3: Commenti mock, MudBlazor versione, link docs, ADR | ✅ Completato |
| M4: ISaleSessionService obsolete, Product.ImageUrl DTO/UI | ✅ Completato (DB pending) |
| M5: ManagementPageHeader, GenericDocumentProcedure eliminato | ✅ Completato |
| PR #1362: Swagger 4 root causes | ✅ Completato |

---

## 2. Registro Problemi — Audit 2026-07-07

### 🔴 ALTA SEVERITÀ

---

#### P01 — Credenziali dev ancora presenti in appsettings.json
> **⚠️ DISCREPANZA**: PIANO_CORREZIONE.md dichiara M1-A2 "COMPLETATA" ma le credenziali sono ancora presenti nel file principale.

| Campo | Valore |
|-------|--------|
| **File** | `EventForge.Server/appsettings.json` (sezione Development, righe 22-71) |
| **Categoria** | 🔴 Sicurezza |
| **Severità** | Alta |
| **Sforzo** | S |
| **Rischio regressione** | Basso |

**Credenziali ancora in chiaro** nel file tracciato in git:
```
ConnectionStrings.DefaultConnection → ******
ConnectionStrings.SqlServer        → ******
ConnectionStrings.LogDb            → ******
Encryption.Key                     → "DevelopmentEncryptionKey-ChangeInProduction!!"
Authentication.Jwt.SecretKey       → "DevelopmentSecretKeyThatIsAtLeast32CharsLong!!"
Bootstrap.SuperAdminPassword       → "EventForge@2024!"
Bootstrap.StoreOperatorPassword    → "Operator@2025!"
Environments.Development.Agent.Password → "Admin#123!"
Environments.Development.UpdateHub.AdminApiKey → "dev-admin-key"
Environments.Development.UpdateHub.MaintenanceSecret → "dev-maintenance-secret-2024"
```

**Cosa è stato fatto correttamente:** la sezione `Environments.Production` usa correttamente `REPLACE_*` placeholder.  
**Cosa manca:** la sezione `Development` e le righe root (22-71) non sono state sostituite.

**Mitigazione attuale:** le chiavi `SyncfusionLicenseKey` e `BoldReportsLicenseKey` sono chiavi di sviluppo/community non di produzione.

---

#### P02 — SQLitePCLRaw 2.1.11 — vulnerabilità HIGH (GHSA-2m69-gcr7-jv3q)

| Campo | Valore |
|-------|--------|
| **File** | `EventForge.Tests/EventForge.Tests.csproj`, `Prym.ManagementHub/Prym.ManagementHub.csproj` |
| **Warning build** | `NU1903: Package 'SQLitePCLRaw.lib.e_sqlite3' 2.1.11 has a known high severity vulnerability` |
| **Advisory** | https://github.com/advisories/GHSA-2m69-gcr7-jv3q |
| **Categoria** | 🔴 Sicurezza / Dipendenze |
| **Severità** | Alta |
| **Sforzo** | S |
| **Rischio regressione** | Basso |

**Origine:** `SQLitePCLRaw` è una dipendenza transitiva di `Microsoft.EntityFrameworkCore.Sqlite` (usata nei due progetti indicati).  
**Soluzione:** pinnare `SQLitePCLRaw.lib.e_sqlite3` a una versione ≥ 2.1.12 in `Directory.Packages.props` con `CentralPackageTransitivePinningEnabled=true` già attivo.

---

#### P03 — DbContext.SaveChanges() — blocking call (potenziale deadlock)

| Campo | Valore |
|-------|--------|
| **File** | `EventForge.Server/Data/EventForgeDbContext.cs:367` |
| **Codice** | `return SaveChangesAsync().ConfigureAwait(false).GetAwaiter().GetResult();` |
| **Categoria** | 🟠 Debito tecnico / Async |
| **Severità** | Alta |
| **Sforzo** | M |
| **Rischio regressione** | Medio |

**Problema:** il metodo sincrono `SaveChanges()` blocca il thread ASP.NET Core attendendo un task asincrono. In ambienti con `SynchronizationContext` attivo (Blazor Server, test host), questo pattern causa **deadlock**.  
**Contesto:** il metodo è richiesto dall'override di `DbContext.SaveChanges()` di Entity Framework Core. Il team lo usa per propagare la audit trail sincrona. Tutti i consumer del server usano `SaveChangesAsync()`.  
**Soluzione:** implementare la audit trail sincrona senza chiamare `SaveChangesAsync()` (duplicando la logica `GetCurrentUser()` e applicando le modifiche ai ChangeTracker entries prima del salvataggio sincrono EF nativo `base.SaveChanges()`).

---

### 🟠 MEDIA SEVERITÀ

---

#### P04 — CORS: AllowAnyHeader + AllowAnyMethod (security posture)

| Campo | Valore |
|-------|--------|
| **File** | `EventForge.Server/Program.cs:367-368` |
| **Codice** | `.AllowAnyHeader().AllowAnyMethod()` |
| **Categoria** | 🟠 Sicurezza |
| **Severità** | Media |
| **Sforzo** | S |
| **Rischio regressione** | Basso |

**Problema:** `AllowAnyHeader()` + `AllowAnyMethod()` con `AllowCredentials()` è una configurazione CORS estremamente permissiva. Qualsiasi header custom e qualsiasi verbo HTTP (incluso TRACE, CONNECT) sono consentiti da origini autorizzate.  
**Contesto:** l'origine è già limitata a `WithOrigins(allowedOrigins)`, il che mitiga parzialmente il rischio.  
**Soluzione:** restringere a `WithHeaders("Authorization", "Content-Type", "X-*")` e `WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")`.

---

#### P05 — BuildServiceProvider() in Program.cs (ASP0001 warning soppresso)

| Campo | Valore |
|-------|--------|
| **File** | `EventForge.Server/Program.cs:357` e riga `613-615` |
| **Warning** | ASP0001 (soppresso con `#pragma warning disable ASP0001`) |
| **Categoria** | 🟡 Code quality / Performance |
| **Severità** | Media |
| **Sforzo** | S |
| **Rischio regressione** | Basso |

**Problema:** `builder.Services.BuildServiceProvider()` durante la fase di registrazione dei servizi crea una **seconda copia** di tutti i singleton services. Questo causa memory waste e potenziali inconsistenze di stato tra i due service provider instances.  
**Contesto:** usato per ottenere `ILogger<Program>` per loggare un warning sulla configurazione CORS. Il `#pragma warning disable ASP0001` sopprime il warning del compilatore.  
**Soluzione:** usare `LoggerFactory.Create(...)` per logging temporaneo durante startup, oppure loggare via `Console.WriteLine` o posticipare il check CORS alla fase `app.Use*`.

---

#### P06 — AllowAnonymous su AgentLogsController (autenticazione header non verificata in tutti i path)

| Campo | Valore |
|-------|--------|
| **File** | `EventForge.Server/Controllers/AgentLogsController.cs:21` |
| **Categoria** | 🟠 Sicurezza / Design |
| **Severità** | Media |
| **Sforzo** | XS |
| **Rischio regressione** | Basso |

**Problema:** `AgentLogsController` è `[AllowAnonymous]` e protetto da un header `X-Maintenance-Secret`. L'header viene verificato internamente dal servizio, ma se la verifica viene rimossa/modificata per errore, l'endpoint è esposto senza JWT. È preferibile usare un middleware dedicato o una policy personalizzata `[Authorize(Policy = "MaintenanceSecret")]`.  
**Contesto:** stessa logica applicabile a `ClientLogsController` (rate limited ma AllowAnonymous).  
**Nota:** il commento nel controller spiega il motivo (Agent non ha JWT). È una scelta progettuale, non necessariamente un errore, ma merita hardening.

---

#### P07 — Product.ImageUrl — entità C# ancora con [Obsolete], colonna DB non rimossa

| Campo | Valore |
|-------|--------|
| **File** | `EventForge.Server/Data/Entities/Products/Product.cs:45` |
| **File SQL** | `Migrations/20260706_RemoveImageUrlFromProducts.sql` (pronto, non eseguito) |
| **Categoria** | 🟡 Debito tecnico |
| **Severità** | Media |
| **Sforzo** | XS (post-migration SQL) |
| **Rischio regressione** | Medio |

**Stato attuale:** il campo `Product.ImageUrl` è marcato `[Obsolete]`, la migration SQL è pronta in `Migrations/20260706_RemoveImageUrlFromProducts.sql`. Mancano due step finali:
1. Eseguire la migration SQL (richiede checkpoint umano per produzione)
2. Rimuovere il campo `[Obsolete] ImageUrl` dall'entità C#

**Consumer correnti:**
- `ProductManagementController.cs:3765` — `ImageUploadResultDto.ImageUrl` (locale, non `Product.ImageUrl`)
- `EventForge.Server/Services/Store/StoreUserService.cs:987,1021,1046,1831` — `ImageUrl` su entità diverse (StoreUser, non Product)

Il campo `Product.ImageUrl` in `Product.cs:45` non viene più scritto da nessun servizio (confermato da audit M4/M5).

---

#### P08 — PromotionRule.CustomerGroupIds — campo [Obsolete] con pragma warning e uso in PromotionService

| Campo | Valore |
|-------|--------|
| **File** | `EventForge.Server/Data/Entities/Promotions/PromotionRule.cs:78`, `EventForge.Server/Services/Promotions/PromotionService.cs:645-647` |
| **Categoria** | 🟡 Debito tecnico |
| **Severità** | Media |
| **Sforzo** | M |
| **Rischio regressione** | Basso |

**Problema:** `PromotionRule.CustomerGroupIds` è marcato `[Obsolete("Use BusinessPartyGroupIds instead")]`. Viene ancora usato in `PromotionService.cs:646` con `#pragma warning disable CS0618` come fallback.  
**Soluzione:** completare la migrazione dei dati da `CustomerGroupIds` a `BusinessPartyGroupIds` e rimuovere il campo deprecato (richiede migration SQL).

---

#### P09 — TableManagementController — endpoint [Obsolete] non deprecato formalmente via API versioning

| Campo | Valore |
|-------|--------|
| **File** | `EventForge.Server/Controllers/TableManagementController.cs:25-37` |
| **Categoria** | 🟡 Debito tecnico |
| **Severità** | Media |
| **Sforzo** | S |
| **Rischio regressione** | Basso |

**Problema:** il metodo `GetAllTables()` è decorato con `[Obsolete("Use GetTables with pagination instead")]` e internamente usa `GetAllTablesAsync()` via `#pragma warning disable CS0618`. Non viene verificato se consumer di Prym.Web usano ancora questo endpoint non paginato.  
**Verifica:** `grep -rn "GetAllTables\|tables\b" Prym.Web/Services/` conferma che `ITableManagementService.GetAllTablesAsync()` esiste in Prym.Web, ma non se è chiamato da qualche pagina.  
**Soluzione:** identificare i consumer, migrarli all'endpoint paginato `/tables` con `?pageSize=`, quindi rimuovere sia il metodo controller che il metodo di servizio.

---

### 🟢 BASSA SEVERITÀ

---

#### P10 — FiscalPrintersDashboard.razor — TODO EFTable real-time (sospeso)

| Campo | Valore |
|-------|--------|
| **File** | `Prym.Web/Pages/Admin/FiscalPrintersDashboard.razor:8` |
| **Categoria** | 🟢 TODO cosmetico (sospeso) |
| **Severità** | Bassa |
| **Sforzo** | — (dipendenza esterna) |

**Dipendenza:** richiede supporto SignalR real-time in EFTable. Non pianificabile autonomamente.

---

#### P11 — InventoryMerge.razor — TODO EFTable wizard (sospeso)

| Campo | Valore |
|-------|--------|
| **File** | `Prym.Web/Pages/Management/Warehouse/InventoryMerge.razor:7` |
| **Categoria** | 🟢 TODO cosmetico (sospeso) |
| **Severità** | Bassa |
| **Sforzo** | — (dipendenza esterna) |

**Dipendenza:** richiede archetype WizardPage. Non pianificabile autonomamente.

---

#### P12 — Commento XML malformato in IProductService.cs

| Campo | Valore |
|-------|--------|
| **File** | `EventForge.Server/Services/Products/IProductService.cs:257` |
| **Categoria** | 🟢 Documentazione |
| **Severità** | Bassa |
| **Sforzo** | XS |

Tag `<summary>` non chiuso — CS1570 warning. Impatta IntelliSense e Swagger.

---

#### P13 — Missing XML `<param>` tags in ProductManagementController.GetProducts()

| Campo | Valore |
|-------|--------|
| **File** | `EventForge.Server/Controllers/ProductManagementController.cs:70-71` |
| **Categoria** | 🟢 Documentazione |
| **Severità** | Bassa |
| **Sforzo** | XS |

Parametri `includeInactive` e `quickFilter` non documentati nei commenti XML.

---

#### P14 — Cref ambiguo in StockReconciliationService.cs

| Campo | Valore |
|-------|--------|
| **File** | `EventForge.Server/Services/Warehouse/StockReconciliationService.cs:1660` |
| **Categoria** | 🟢 Documentazione |
| **Severità** | Bassa |
| **Sforzo** | XS |

`<see cref="Math.Abs"/>` ambiguo → dovrebbe essere `<see cref="Math.Abs(decimal)"/>`.

---

#### P15 — Microsoft.OpenApi versione outdated (warning nella catena Swashbuckle)

| Campo | Valore |
|-------|--------|
| **File** | `Directory.Packages.props` |
| **Versione attuale** | `Microsoft.OpenApi 2.7.5` |
| **Versione disponibile** | `3.x.x` (major — breaking changes) |
| **Categoria** | 🟢 Dipendenze |
| **Severità** | Bassa |
| **Sforzo** | XS–S |

**Nota:** la versione 2.7.5 è quella richiesta da Swashbuckle 10.2.3 (fissata in PR #1362). Un upgrade a Microsoft.OpenApi 3.x richiederebbe anche Swashbuckle 11+. Non urgente.

---

#### P16 — NotSupportedException nei FiscalPrinter service closures

| Campo | Valore |
|-------|--------|
| **File** | `EventForge.Server/Services/FiscalPrinting/EpsonFiscalPrinterService.Closure.cs:17,385`, `CustomFiscalPrinterService.Closure.cs:13,376` |
| **Categoria** | 🟢 Code quality / Design |
| **Severità** | Bassa |
| **Sforzo** | M |

Metodi interfaccia implementati solo per lanciare `NotSupportedException`. Pattern corretto per segregazione interfaccia: valutare `ISupportsClosure` interfaccia separata.

---

#### P17 — Pragma warning CS0618 non commentati

| Campo | Valore |
|-------|--------|
| **File** | `EventForge.Server/Services/Promotions/PromotionService.cs:645-647`, `EventForge.Server/Controllers/TableManagementController.cs:34-36` |
| **Categoria** | 🟢 Code quality |
| **Severità** | Bassa |
| **Sforzo** | XS |

I pragma CS0618 esistenti sono giustificati ma mancano di commento `// Reason: ...` che spieghi il perché.

---

#### P18 — Warning MSB3026 transienti durante parallel build

| Campo | Valore |
|-------|--------|
| **File** | `EventForge.Tests/EventForge.Tests.csproj` (parallel build race condition) |
| **Categoria** | 🟢 Build warning (transiente) |
| **Severità** | Bassa |
| **Sforzo** | XS (non richiedono azione) |

Race condition su file DLL durante build parallele. Non impatta il risultato finale. Ripetuto build risolve.

---

## 3. Matrice di Priorità

| ID | Problema | Categoria | Severità | Sforzo | Dipendenze |
|----|----------|-----------|----------|--------|------------|
| P01 | Credenziali dev in appsettings.json | Sicurezza | 🔴 Alta | S | nessuna |
| P02 | SQLitePCLRaw vulnerability HIGH | Sicurezza | 🔴 Alta | S | nessuna |
| P03 | DbContext.SaveChanges() blocking | Async/Debito | 🔴 Alta | M | nessuna |
| P04 | CORS AllowAnyHeader/AllowAnyMethod | Sicurezza | 🟠 Media | S | nessuna |
| P05 | BuildServiceProvider() in Program.cs | Code quality | 🟠 Media | S | nessuna |
| P06 | AgentLogsController AllowAnonymous hardening | Sicurezza | 🟠 Media | XS | nessuna |
| P07 | Product.ImageUrl [Obsolete] — rimozione finale | Debito tecnico | 🟠 Media | XS+SQL | checkpoint umano |
| P08 | PromotionRule.CustomerGroupIds [Obsolete] | Debito tecnico | 🟠 Media | M | migration SQL |
| P09 | TableManagementController GetAllTables [Obsolete] | Debito tecnico | 🟠 Media | S | verifica consumer |
| P10 | FiscalPrintersDashboard TODO EFTable | TODO sospeso | 🟢 Bassa | — | feature EFTable |
| P11 | InventoryMerge TODO EFTable/Wizard | TODO sospeso | 🟢 Bassa | — | feature WizardPage |
| P12 | IProductService.cs XML malformato | Documentazione | 🟢 Bassa | XS | nessuna |
| P13 | ProductManagementController param missing | Documentazione | 🟢 Bassa | XS | nessuna |
| P14 | StockReconciliationService cref ambiguo | Documentazione | 🟢 Bassa | XS | nessuna |
| P15 | Microsoft.OpenApi versione | Dipendenze | 🟢 Bassa | XS | Swashbuckle 11+ |
| P16 | FiscalPrinter NotSupportedException | Design | 🟢 Bassa | M | nessuna |
| P17 | Pragma CS0618 senza commento reason | Code quality | 🟢 Bassa | XS | nessuna |
| P18 | MSB3026 build transient warning | Build | 🟢 Bassa | XS | nessuna |

---

## 4. Analisi Dipendenze

```
P01 (credenziali) ─── indipendente
P02 (SQLitePCLRaw) ── indipendente
P03 (DbContext blocking) ── indipendente (alto rischio regressione)
P04 (CORS) ─────────── indipendente
P05 (BuildServiceProvider) ── indipendente
P06 (AgentLogs anonymous) ── indipendente
P07 (ImageUrl rimozione) ── dipende da: checkpoint umano DB migration
P08 (CustomerGroupIds) ─── dipende da: migration SQL separata
P09 (GetAllTables obsolete) ─ dipende da: verifica consumer Prym.Web
P10, P11 ─────────────────── sospesi (feature esterne non pianificate)
P12–P14 ───────────────────── doc fix, tutti indipendenti e triviali
P15 ───────────────────────── dipende da upgrade Swashbuckle 11+
P16 ───────────────────────── architetturale, no breaking change
P17, P18 ─────────────────── triviali, indipendenti
```

---

## 5. Stato Build e Test

```
Build:  0 errori | 8 warning (2 NU1903 SQLitePCLRaw, 6 MSB3026 transient)
Tests:  1419 passed | 4 skipped | 0 failed
```

### Test skipped (noti, non regressioni)
I 4 test skippati appartengono a `StockServiceValidationTests` e risultano skippati anche sul master precedente. Non sono regressioni introdotte.

### Warning NU1903 (actionable)
```
Package 'SQLitePCLRaw.lib.e_sqlite3' 2.1.11 has a known high severity vulnerability
  → EventForge.Tests/EventForge.Tests.csproj
  → Prym.ManagementHub/Prym.ManagementHub.csproj
Advisory: https://github.com/advisories/GHSA-2m69-gcr7-jv3q
```

---

## 6. Problemi NON trovati (conferma stato attuale)

- ❌ EPPlus / OfficeOpenXml: zero occorrenze — migrazione completata ✅
- ❌ Product.ImageUrl in DTO/Validators/Services/UI: zero occorrenze — M4/M5 completati ✅
- ❌ ISaleSessionService GetActiveSessionsAsync/GetOperatorSessionsAsync: rimossi ✅
- ❌ FluentValidation.AspNetCore: rimosso ✅
- ❌ GenericDocumentProcedure.razor: eliminato ✅
- ❌ FidelityCard "mock" comments: rimossi ✅
- ❌ Link rotti docs/features/README.md: tutti i link verificati OK ✅
- ❌ Server/ orfano: eliminato ✅
- ❌ Swagger generation: 4 root causes risolti in PR #1362 ✅

---

## 7. Glossario Severità

| Simbolo | Significato |
|---------|-------------|
| 🔴 Alta | Rischio sicurezza confermato o debito bloccante in produzione |
| 🟠 Media | Impatto funzionale o di sicurezza reale, non bloccante |
| 🟡 Media-bassa | Debito tecnico senza impatto immediato |
| 🟢 Bassa | Cosmetic, documentazione, stilistica |

---

*Fine documento — generato il 2026-07-07 da audit automatico post-PR #1362*
