# Analisi approfondita — Prym.Agent

> **Versione analisi:** 2  
> **Data:** 2026-04-13  
> **Stato del codice:** post Sprint 1–4 (commit `7452d9a`)  
> **Autore:** Copilot SWE Agent

---

## 1. Panoramica architetturale

Prym.Agent è un servizio Windows (ASP.NET Core hosted service) che opera in tre modalità:

| Modalità | Descrizione |
|---|---|
| **Full** | Gestisce aggiornamenti Server + Client via SignalR Hub |
| **Client-only / Server-only** | Gestisce un solo componente |
| **Standalone (printer-proxy-only)** | Nessuna connessione Hub; espone solo il proxy stampanti |

### 1.1 Componenti principali

```
Program.cs
├── Background workers
│   ├── AgentWorker          – connessione SignalR Hub, heartbeat, download comandi
│   └── ScheduledInstallWorker – loop di installazione schedulata
├── Services
│   ├── UpdateExecutorService  – download → verifica → backup → migrazione → deploy
│   ├── PendingInstallService  – coda FIFO con persistenza su disco
│   ├── BackupService          – backup/ripristino directory di deploy
│   ├── MigrationRunnerService – esecuzione script SQL
│   ├── IisManagerService      – stop/start sito IIS via appcmd.exe
│   ├── VersionDetectorService – rilevamento versioni installate
│   ├── AgentServerSink        – Serilog sink per forward log a EventForge.Server
│   ├── CommandTrackingService – tracciatura comandi Hub
│   ├── DownloadProgressService– stato download in real-time per la UI
│   ├── AgentStatusService     – stato connessione Hub per la UI
│   ├── InstallationCodeGenerator – generazione codice univoco prima startup
│   └── AgentPrinterService    – proxy USB / TCP / HTTP per stampanti fiscali
├── Controllers
│   └── PrinterProxyController – endpoint REST per il proxy stampanti
├── Middleware
│   └── BasicAuthMiddleware    – autenticazione HTTP Basic per la UI locale
└── Pages (Razor)
    └── Index, Logs, Packages, Schedule, Settings
```

---

## 2. Stato post Sprint 1–4 (modifiche già apportate)

Le seguenti criticità sono state risolte nel commit `2c02dc4` + `7452d9a`:

| ID | File | Fix |
|---|---|---|
| S1.1 | `UpdateExecutorService.cs` | `ValidateZipPathTraversal()` prima di `ZipFile.ExtractToDirectory` |
| S1.2 | `AgentWorker.cs` | Disk-first in `TryEnrollAsync`: `PersistEnrollmentAsync` → poi update in-memory |
| S1.3 | `AgentServerSink.cs` | `_queue.Count` catturato dentro il `lock` |
| S1.4 | `AgentWorker.cs` | `MapComponents()` `(false,false)` → `0` + `LogWarning` |
| S2.1 | `IisManagerService.cs` | Timeout 30 s + `process.Kill(entireProcessTree)` + `TimeoutException` |
| S2.2 | `VersionDetectorService.cs` | `LogWarning` se `version.txt` esiste ma è vuoto |
| S2.3 | `InstallationCodeGenerator.cs` | Scrittura atomica temp+rename in `PersistCode` |
| S2.4 | `PrinterProxyController.cs` | `ListSystemPrintersAsync` con `ReadToEndAsync`+`WaitForExitAsync` |
| S3.1 | `BackupService.cs` | `Parallel.ForEachAsync` capped a `Math.Min(ProcessorCount, 8)` |
| S3.2 | `MigrationRunnerService.cs` | Unica `SqlConnection` riutilizzata per tutti gli script |
| S3.3 | `PendingInstallService.cs` | `static readonly JsonSerializerOptions` per serialize/deserialize |
| S3.4 | `AgentPrinterService.cs` | `Parallel.For` + `ConcurrentBag` per scansione USB001-USB009 |
| S3.5 | `AgentOptions.cs` + `UpdateExecutorService.cs` | `DownloadBufferSizeKb` configurabile (default 80, clamp 16-4096) |
| S3.6 | `AgentWorker.cs` | Flag `_firstConnection` volatile: `RegisterInstallation` solo alla prima connessione |
| S4.1 | `UpdateExecutorService.cs` | Rimosso `GetNextWindowStart` duplicato; usa `pendingInstallService.GetNextWindowStart()` |
| S4.2 | `UpdateExecutorService.cs` | `DeployBinariesAsync` → metodo d'istanza (rimosso `static` e param `ILogger`) |
| S4.3 | `Prym.DTOs/Agent/HubMessages.cs` | DTOs Hub spostati in `Prym.DTOs.Agent`; eliminato `Models/HubMessages.cs` |
| S4.4 | `PrinterProxyController.cs` + `AgentOptions.cs` | SSRF allowlist `AllowedHostPatterns` con wildcard boundary-check |

---

## 3. Criticità residue — analisi fresca (post Sprint 1–4)

### 3.1 🔴 Sicurezza

#### R1 — `PersistEnrollmentAsync`: scrittura non atomica
**File:** `AgentWorker.cs` › `PersistEnrollmentAsync`  
**Problema:** La scrittura delle credenziali di enrollment usa `File.WriteAllBytesAsync` diretto, senza il pattern temp+rename. Un crash durante la scrittura corrompe `appsettings.json`, rendendo il servizio non avviabile.  
**Impatto:** Critico — perdita dell'intera configurazione dell'agente.  
**Fix:** Adottare il medesimo pattern temp+rename già usato in `PendingInstallService.SaveToDisk`, `InstallationCodeGenerator.PersistCode` e `AgentWorker.PersistEnrollmentAsync` (parzialmente, il temp file già esiste ma non viene rinominato atomicamente).

```csharp
// Attuale (non atomico)
await File.WriteAllBytesAsync(appSettingsPath, stream.ToArray());

// Fix
var tmpPath = appSettingsPath + ".tmp";
await File.WriteAllBytesAsync(tmpPath, stream.ToArray());
File.Move(tmpPath, appSettingsPath, overwrite: true);
```

---

#### R2 — Username comparison non constant-time in `BasicAuthMiddleware`
**File:** `BasicAuthMiddleware.cs` › `TryAuthenticate`  
**Problema:** La comparazione `credentials[0] == expectedUser` è non constant-time (early-exit su primo carattere differente). Il timing side-channel è sfruttabile per enumerare username.  
**Impatto:** Basso su localhost-only; potenzialmente più rilevante se l'Agent fosse esposto su una rete.  
**Fix:**

```csharp
// Attuale
return credentials[0] == expectedUser && PasswordHasher.Verify(credentials[1], expectedPass);

// Fix
return CryptographicOperations.FixedTimeEquals(
           Encoding.UTF8.GetBytes(credentials[0]),
           Encoding.UTF8.GetBytes(expectedUser ?? string.Empty))
       && PasswordHasher.Verify(credentials[1], expectedPass);
```

---

#### R3 — Endpoint di gestione coda non autenticati
**File:** `Program.cs`  
**Problema:** Gli endpoint `/api/agent/pending-installs`, `/api/agent/install-now`, `/api/agent/unblock-queue` sono esclusi da `BasicAuthMiddleware` per il trust model localhost-only. Questo è un rischio concreto in ambienti multi-tenant: qualsiasi processo sul server (compresi processi compromessi) può avviare, sbloccare o skippare installazioni senza credenziali.  
**Impatto:** Medio — accesso privilegiato senza autenticazione da localhost.  
**Fix:** Aggiungere un header segreto condiviso (`X-Agent-Internal-Token`) inviato da EventForge.Server, validato in `BasicAuthMiddleware` per questi endpoint invece di bypassarli completamente.

---

#### R4 — `MergeJsonFilesAsync`: scrittura config non atomica
**File:** `UpdateExecutorService.cs` › `MergeJsonFilesAsync`  
**Problema:** `File.WriteAllTextAsync(targetPath, mergedJson, ct)` scrive direttamente sul file di configurazione di destinazione. Un crash durante il merge lascia un config file parzialmente scritto.  
**Impatto:** Medio — potenziale corruzione di `appsettings.json` del Server/Client dopo un aggiornamento.  
**Fix:**

```csharp
var tmpPath = targetPath + ".tmp";
await File.WriteAllTextAsync(tmpPath, mergedJson, ct);
File.Move(tmpPath, targetPath, overwrite: true);
```

---

### 3.2 🟠 Correctness

#### R5 — `_firstConnection` non copre le riconnessioni del loop esterno
**File:** `AgentWorker.cs`  
**Problema:** Il flag `_firstConnection` è `false` dopo la prima connessione. Se la connessione Hub si interrompe completamente e il ciclo esterno in `ExecuteAsync` richiama `ConnectAndRunAsync`, il nuovo `HubConnection` non esegue `RegisterInstallation` — invia solo l'heartbeat. Il Hub riceve un heartbeat senza aver mai ricevuto la registrazione per questa connessione, e potrebbe non avere lo stato aggiornato (versioni, metadati).  
**Impatto:** Medio — il Hub non aggiornerà i metadati dell'installazione (versioni, nome, OS) dopo una reconnessione esterna.  
**Fix:** Distinguere tra riconnessioni SignalR interne (handler `Reconnected`) e riconnessioni dell'intero ciclo esterno. Resettare `_firstConnection = true` all'inizio di ogni chiamata `ConnectAndRunAsync`, e usare il flag per la prima connessione del ciclo attuale.

```csharp
private async Task ConnectAndRunAsync(CancellationToken ct)
{
    var isFirst = true; // locale al ciclo, non il campo volatile
    // ... handler Reconnected usa isFirst = false dopo la prima registrazione
```

---

#### R6 — `PendingInstallService.LoadFromDisk`: cleanup missing entries non persistito
**File:** `PendingInstallService.cs` › `LoadFromDisk`  
**Problema:** Quando al caricamento vengono rimosse le entry con zip mancante, `SaveToDisk()` non viene chiamato. La coda ripulita non viene scritta su disco: al prossimo riavvio le entry fantasma ricompariranno (e verranno ripulite di nuovo, generando warning inutili).  
**Impatto:** Basso ma fastidioso — warning ripetuti a ogni riavvio se i file zip vengono eliminati manualmente.  
**Fix:** Chiamare `SaveToDisk()` dentro il blocco `lock` se `missing.Count > 0`.

---

#### R7 — `DeployBinariesAsync`: copia file sequenziale
**File:** `UpdateExecutorService.cs` › `DeployBinariesAsync`  
**Problema:** La copia dei binari nel deploy path avviene con un `foreach` sequenziale (non asincrono, non parallelo) nonostante `BackupService.CopyDirectoryAsync` sia già stato aggiornato a `Parallel.ForEachAsync`. Deploy di package grandi (50+ file) risulta più lento del necessario.  
**Impatto:** Basso — performance durante l'installazione.  
**Fix:** Parallelizzare la copia con `Parallel.ForEachAsync` capped a `Math.Min(Environment.ProcessorCount, 8)`, analogamente a `BackupService`.

---

#### R8 — `AgentPrinterService.ListDevices`: scansione limitata a USB001-USB009
**File:** `AgentPrinterService.cs`  
**Problema:** Il `Parallel.For(1, 10, ...)` genera solo `USB001`–`USB009`. La regex `DeviceIdPattern` accetta fino a 3 cifre (`USB0*[1-9][0-9]{0,2}`), quindi USB010–USB099 sono validi ma non vengono mai scansionati.  
**Impatto:** Basso — sistemi con più di 9 porte USB virtuali non vedranno le stampanti oltre USB009.  
**Fix:** Estendere a `Parallel.For(1, 100, ...)` (o rendere il range configurabile in `AgentOptions`).

---

### 3.3 🟡 Robustezza & Osservabilità

#### R9 — `BackupService.PruneOldBackupsAsync`: I/O sincrono in metodo asincrono
**File:** `BackupService.cs` › `PruneOldBackupsAsync`  
**Problema:** Il metodo dichiara `Task` come tipo di ritorno ma esegue `Directory.Delete` sincrono e restituisce `Task.CompletedTask`. `Directory.Delete(recursive: true)` su directory grandi blocca il thread.  
**Impatto:** Basso — blocca brevemente il thread pool durante il pruning.  
**Fix:** Rendere il metodo `async Task` e wrappare il loop in `await Task.Run(...)`, oppure usare `Directory.Delete` sincrono ma segnalarlo con `// intentionally synchronous`.

---

#### R10 — `VersionDetectorService`: I/O sincrono bloccante sull'heartbeat
**File:** `VersionDetectorService.cs`, usato in `AgentWorker`  
**Problema:** `GetServerVersion()` e `GetClientVersion()` chiamano `File.ReadAllText` sincrono. Vengono invocati a ogni heartbeat (default: ogni 60 s). Su file system lenti o rete (UNC path) possono bloccare il thread.  
**Impatto:** Basso — heartbeat ritardato in ambienti con storage lento.  
**Fix:** Aggiungere versioni asincrone `GetServerVersionAsync` / `GetClientVersionAsync` che usino `File.ReadAllTextAsync`.

---

#### R11 — `AgentServerSink.SendBatchAsync`: `JsonSerializerOptions` allocato per ogni batch
**File:** `AgentServerSink.cs` › `SendBatchAsync`  
**Problema:** `new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }` viene allocato ad ogni chiamata `SendBatchAsync`. Con batch frequenti ciò produce pressione sul GC.  
**Impatto:** Basso — visibile solo con logging ad alta frequenza.  
**Fix:** Campo `private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };`.

---

#### R12 — `UpdateExecutorService.LoadManifestAsync`: `JsonSerializerOptions` non cached
**File:** `UpdateExecutorService.cs` › `LoadManifestAsync`  
**Problema:** `new JsonSerializerOptions { PropertyNameCaseInsensitive = true }` allocato a ogni chiamata.  
**Fix:** Campo `private static readonly JsonSerializerOptions _manifestOpts = new() { PropertyNameCaseInsensitive = true };`.

---

#### R13 — `NotifyPhaseBackground`: eccezioni silenziosamente inghiottite
**File:** `UpdateExecutorService.cs` › `NotifyPhaseBackground`  
**Problema:** Il metodo è `private void` e usa `_ = Task.Run(...)`. Qualsiasi eccezione non catchata nella task viene silenziosamente ignorata (finisce su `TaskScheduler.UnobservedTaskException`).  
**Impatto:** Difficoltà nel diagnosticare mancate notifiche ai client.  
**Fix:** Aggiungere un `ContinueWith` per loggare le eccezioni:

```csharp
_ = Task.Run(...).ContinueWith(t =>
    logger.LogWarning(t.Exception, "NotifyPhaseBackground faulted"),
    TaskContinuationOptions.OnlyOnFaulted);
```

---

#### R14 — `WriteResponseToFileAsync`: notifiche progress fire-and-forget non tracciate
**File:** `UpdateExecutorService.cs` › `WriteResponseToFileAsync`  
**Problema:** `_ = NotifyPhaseAsync(...)` durante il download (ogni 500 ms) è fire-and-forget. Se il Server è irraggiungibile, si accumulano N task concorrenti non osservate.  
**Impatto:** Basso — nessuna perdita di dati, ma rumore nel garbage collector.  
**Fix:** Wrappare in `try/catch` oppure usare `Task.Run(() => NotifyPhaseAsync(...)).ContinueWith(...)`.

---

### 3.4 🔵 Performance

#### R15 — `AgentStatusService`: proprietà pubbliche senza sincronizzazione
**File:** `AgentStatusService.cs`  
**Problema:** `HubConnectionState`, `LastHeartbeatAt`, `LastHeartbeatError`, `EnrollmentStatus` sono proprietà pubbliche con getter/setter auto-generati. Vengono scritte da `AgentWorker` (thread background) e lette da Razor Pages (request thread). Nessuna sincronizzazione → torn read potenziale su `DateTime?`.  
**Impatto:** Molto basso in pratica (JIT tende ad atomizzare le write a 64-bit su x64), ma tecnicamente non sicuro.  
**Fix:** Usare `volatile` per i campi o `Interlocked` / `lock` minimo per `LastHeartbeatAt`.

---

#### R16 — `CommandTrackingService.TrimOldest`: ricerca LINQ a ogni insert
**File:** `CommandTrackingService.cs`  
**Problema:** `TrimOldest()` esegue `_commands.Where(...).OrderBy(...).Take(...)` a ogni `Track()`. Con `MaxEntries=50` è trascurabile, ma è O(n log n) inutile.  
**Impatto:** Trascurabile con il limite attuale.  
**Fix (opzionale):** Usare `Queue<T>` invece di `List<T>` per mantenere l'ordine FIFO senza sort.

---

#### R17 — `UpdateExecutorService._http`: nessun `IDisposable`
**File:** `UpdateExecutorService.cs`  
**Problema:** Il campo `private readonly HttpClient _http` viene creato nel costruttore ma la classe non implementa `IDisposable`. Come singleton ASP.NET Core il lifetime coincide con quello del processo (OK per la pratica), ma un dispose esplicito sarebbe più corretto.  
**Impatto:** Nessuno in produzione (singleton non viene mai disposto prima della chiusura del processo).  
**Fix:** Implementare `IDisposable` + `Dispose()` che chiama `_http.Dispose()`, oppure usare `IHttpClientFactory` (ma richiede refactoring del timeout per-request).

---

### 3.5 🔵 Refactoring & Design

#### R18 — `AgentWorker.PersistEnrollmentAsync`: logica JSON ridondante
**File:** `AgentWorker.cs`  
**Problema:** `PersistEnrollmentAsync` ri-implementa la serializzazione JSON manuale (iterazione su `JsonDocument`) per aggiornare solo due campi. La stessa logica di "aggiorna un campo in appsettings.json" esiste in `InstallationCodeGenerator.PersistCode` (usa `JsonNode`) e in `SettingsModel.OnPostSave` (usa `JsonNode`). Tre implementazioni diverse per lo stesso problema.  
**Fix:** Estrarre un helper statico `AppSettingsUpdater.SetValues(Dictionary<string,string> keyValues)` condiviso da tutti e tre i siti.

---

#### R19 — `SettingsModel.OnPostSave`: nessun hash automatico della nuova password
**File:** `Pages/Settings.cshtml.cs`  
**Problema:** Quando il Settings form salva una nuova password, `OnPostSave` controlla se `PasswordHasher.IsHashed(newPassword)` e in caso contrario la hasha. La logica è corretta, ma l'intent non è documentato inline e potrebbe essere rimosso per errore in un refactoring futuro.  
**Fix:** Aggiungere un commento XML summary che spieghi la migrazione trasparente.

---

#### R20 — `PrinterProxyController.HttpForwardAsync`: URL target non validata oltre il hostname
**File:** `PrinterProxyController.cs`  
**Problema:** `IsHostAllowed()` valida il hostname ma non la porta. Potrebbe essere possibile aggirare una regola `192.168.1.*` usando una porta non standard per raggiungere un host consentito ma con un servizio diverso dalla stampante.  
**Impatto:** Molto basso — la validazione del host è comunque significativa.  
**Fix (opzionale):** Estendere l'allowlist a `host:port` pair oppure aggiungere `AllowedPorts: List<int>` in `PrinterProxyAgentOptions`.

---

## 4. Tabella priorità — implementazioni raccomandate

| # | ID | Priorità | File | Sforzo | Descrizione breve |
|---|---|---|---|---|---|
| 1 | R1 | 🔴 Alta | `AgentWorker.cs` | Minimo | `PersistEnrollmentAsync` → temp+rename atomico |
| 2 | R4 | 🔴 Alta | `UpdateExecutorService.cs` | Minimo | `MergeJsonFilesAsync` → temp+rename atomico |
| 3 | R5 | 🟠 Media | `AgentWorker.cs` | Basso | `_firstConnection` reset per reconnessione ciclo esterno |
| 4 | R6 | 🟠 Media | `PendingInstallService.cs` | Minimo | `SaveToDisk()` dopo cleanup in `LoadFromDisk` |
| 5 | R2 | 🟠 Media | `BasicAuthMiddleware.cs` | Minimo | Comparazione username constant-time |
| 6 | R3 | 🟠 Media | `Program.cs` | Basso | Token segreto per endpoint interni di gestione coda |
| 7 | R7 | 🟡 Bassa | `UpdateExecutorService.cs` | Basso | `DeployBinariesAsync` → `Parallel.ForEachAsync` |
| 8 | R11 | 🟡 Bassa | `AgentServerSink.cs` | Minimo | `JsonSerializerOptions` statico per batch |
| 9 | R12 | 🟡 Bassa | `UpdateExecutorService.cs` | Minimo | `JsonSerializerOptions` statico per manifest |
| 10 | R13 | 🟡 Bassa | `UpdateExecutorService.cs` | Minimo | Log eccezioni in `NotifyPhaseBackground` |
| 11 | R10 | 🟡 Bassa | `VersionDetectorService.cs` | Basso | Versioni async `GetServerVersionAsync` |
| 12 | R9 | 🟡 Bassa | `BackupService.cs` | Minimo | `PruneOldBackupsAsync` → I/O asincrono |
| 13 | R18 | 🔵 Molto bassa | `AgentWorker.cs` | Medio | Estrarre `AppSettingsUpdater` condiviso |
| 14 | R8 | 🔵 Molto bassa | `AgentPrinterService.cs` | Minimo | Scansione USB001-USB099 (o configurabile) |
| 15 | R15 | 🔵 Molto bassa | `AgentStatusService.cs` | Minimo | `volatile` su campi di stato |
| 16 | R17 | 🔵 Molto bassa | `UpdateExecutorService.cs` | Minimo | `IDisposable` su `HttpClient` campo |

---

## 5. Sprint proposto — Sprint 5

### Sprint 5A — Sicurezza & Correctness (priorità assoluta)

**S5A.1 — `PersistEnrollmentAsync`: scrittura atomica** (`AgentWorker.cs`)  
Aggiungere `var tmpPath = appSettingsPath + ".tmp";` e usare `File.Move(tmp, target, overwrite: true)` come negli altri persistence point. Sforzo: 3 righe.

**S5A.2 — `MergeJsonFilesAsync`: scrittura atomica** (`UpdateExecutorService.cs`)  
Stessa cosa: temp file + `File.Move`. Sforzo: 3 righe.

**S5A.3 — Username constant-time comparison** (`BasicAuthMiddleware.cs`)  
Sostituire `credentials[0] == expectedUser` con `CryptographicOperations.FixedTimeEquals(...)`. Sforzo: 1 riga.

**S5A.4 — `_firstConnection` reset per reconnessione ciclo esterno** (`AgentWorker.cs`)  
Usare una variabile locale `bool isFirstForThisConnection = true` dentro `ConnectAndRunAsync`, anziché un campo volatile condiviso tra riconnessioni. Il campo `_firstConnection` può essere eliminato. Sforzo: basso.

**S5A.5 — `LoadFromDisk`: persistenza dopo cleanup** (`PendingInstallService.cs`)  
Chiamare `SaveToDisk()` dopo la rimozione delle entry con zip mancante. Sforzo: 1 riga.

---

### Sprint 5B — Performance & Robustezza

**S5B.1 — `JsonSerializerOptions` statici** (`AgentServerSink.cs`, `UpdateExecutorService.cs`)  
Due campi `static readonly`: uno per batch log (CamelCase), uno per manifest (CaseInsensitive). Sforzo: 4 righe totali.

**S5B.2 — `NotifyPhaseBackground`: log su eccezione** (`UpdateExecutorService.cs`)  
Aggiungere `.ContinueWith(t => logger.LogWarning(...), TaskContinuationOptions.OnlyOnFaulted)`. Sforzo: 2 righe.

**S5B.3 — `DeployBinariesAsync` parallelo** (`UpdateExecutorService.cs`)  
Sostituire il `foreach` sequenziale con `Parallel.ForEachAsync` capped a `Math.Min(ProcessorCount, 8)`. Sforzo: 5-8 righe.

**S5B.4 — `VersionDetectorService` async** (`VersionDetectorService.cs`)  
Aggiungere `GetServerVersionAsync` / `GetClientVersionAsync` con `File.ReadAllTextAsync`. Aggiornare `SendHeartbeatAsync` in `AgentWorker` per usarle. Sforzo: basso.

---

## 6. Checklist finale qualità

| Area | Status |
|---|---|
| ZIP path traversal (zip-slip) | ✅ Risolto (S1.1) |
| Race condition enrollment | ✅ Risolto (S1.2) |
| Scrittura atomica configurazioni (pending.json, installationCode) | ✅ Risolto (S2.3, S3.3) |
| Thread-safety AgentServerSink | ✅ Risolto (S1.3) |
| Timeout appcmd.exe | ✅ Risolto (S2.1) |
| SSRF printer proxy | ✅ Risolto (S4.4) |
| Hub DTOs in Prym.DTOs | ✅ Risolto (S4.3) |
| Scrittura atomica PersistEnrollmentAsync | ❌ **Aperta (R1)** |
| Scrittura atomica MergeJsonFilesAsync | ❌ **Aperta (R4)** |
| _firstConnection semantics su reconnect esterno | ❌ **Aperta (R5)** |
| Cleanup LoadFromDisk persistito | ❌ **Aperta (R6)** |
| Username constant-time | ❌ **Aperta (R2)** |
| Endpoint interni autenticati | ❌ **Aperta (R3)** |
| JsonSerializerOptions cached (AgentServerSink, LoadManifest) | ❌ **Aperta (R11, R12)** |
| NotifyPhaseBackground exception logging | ❌ **Aperta (R13)** |
| DeployBinariesAsync parallelo | ❌ **Aperta (R7)** |
| VersionDetectorService async | ❌ **Aperta (R10)** |
