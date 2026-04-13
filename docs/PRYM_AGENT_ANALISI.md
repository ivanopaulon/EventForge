# Prym.Agent — Analisi Approfondita del Progetto

> **Data analisi:** Aprile 2026  
> **Progetto:** `Prym.Agent` (Windows Service — .NET 10, ASP.NET Core, SignalR)  
> **Scopo:** Orchestratore aggiornamenti software remoto + proxy stampanti fiscali locali  
> **Nota:** Il progetto `EventForge.UpdateAgent` (predecessore con namespace vecchio) è stato rimosso. `Prym.Agent` è l'unica implementazione attiva.

---

## Indice

1. [Panoramica Architetturale](#1-panoramica-architetturale)
2. [Inventario File e Responsabilità](#2-inventario-file-e-responsabilità)
3. [Criticità di Codice](#3-criticità-di-codice)
4. [Criticità di Performance](#4-criticità-di-performance)
5. [Criticità di Sicurezza](#5-criticità-di-sicurezza)
6. [Punti di Forza (da preservare)](#6-punti-di-forza-da-preservare)
7. [Implementazioni Proposte](#7-implementazioni-proposte)
8. [Roadmap Prioritizzata](#8-roadmap-prioritizzata)

---

## 1. Panoramica Architetturale

`Prym.Agent` è un Windows Service ASP.NET Core che svolge due macro-funzioni:

```
┌──────────────────────────────────────────────────────────────────┐
│  FUNZIONE A: Update Orchestration                                │
│  ┌─────────────────┐      ┌─────────────────────────────────┐   │
│  │  AgentWorker    │◄────►│  EventForge.Server (SignalR Hub) │   │
│  │  (SignalR conn) │      └─────────────────────────────────┘   │
│  └────────┬────────┘                                            │
│           │                                                      │
│  ┌────────▼────────────────────────────────────────────────┐    │
│  │  UpdateExecutorService (12 fasi)                         │    │
│  │  Download → Verify → Backup → PreMigrate →              │    │
│  │  StopIIS → Deploy → WriteVersion → StartIIS →           │    │
│  │  PostMigrate → HealthCheck → VerifyDeploy → Complete    │    │
│  └──────────────────────────────────────────────────────────┘   │
│           │                                                      │
│  ┌────────▼────────┐    ┌──────────────────────────────────┐    │
│  │PendingInstallSvc│    │  ScheduledInstallWorker           │    │
│  │(FIFO + blocco)  │◄──►│  (polling ogni 60s, finestra mnt)│    │
│  └─────────────────┘    └──────────────────────────────────┘    │
│                                                                  │
│  FUNZIONE B: Printer Proxy                                       │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  PrinterProxyController + AgentPrinterService            │   │
│  │  USB (FileStream) │ TCP (TcpClient) │ HTTP (HttpClient)  │   │
│  └──────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────┘
```

**Binding:** `http://localhost:5780` (solo loopback — non esposto all'esterno)  
**Autenticazione UI:** HTTP Basic Auth via `BasicAuthMiddleware` (PBKDF2-SHA256)  
**Autenticazione Hub:** `X-Api-Key` header nella connessione SignalR

---

## 2. Inventario File e Responsabilità

| File | Righe | Responsabilità |
|------|------:|----------------|
| `Program.cs` | 272 | Bootstrap, DI, Serilog, endpoint Minimal API |
| `Configuration/AgentOptions.cs` | 285 | Schema config (~60 proprietà), completamente documentato |
| `Controllers/PrinterProxyController.cs` | 367 | Endpoint proxy USB/TCP/HTTP per stampanti locali |
| `Services/UpdateExecutorService.cs` | 832 | **Cuore**: orchestrazione 12 fasi, download resiliente, deploy, rollback |
| `Services/PendingInstallService.cs` | 324 | Coda FIFO persistente, finestre di manutenzione, stato di blocco |
| `Workers/AgentWorker.cs` | 608 | Connessione SignalR persistente, heartbeat, enrollment, gestione comandi Hub |
| `Workers/ScheduledInstallWorker.cs` | 159 | Scheduler: installa la testa della coda nella finestra di manutenzione |
| `Services/BackupService.cs` | 96 | Backup pre-deploy, pruning automatico, restore |
| `Services/MigrationRunnerService.cs` | 69 | Esecuzione script SQL con supporto `GO` separator |
| `Services/IisManagerService.cs` | 67 | Stop/start IIS via `appcmd.exe` |
| `Services/AgentServerSink.cs` | 185 | Serilog sink: invia log in batch al Server (best-effort) |
| `Services/AgentPrinterService.cs` | 336 | I/O USB/TCP/HTTP per stampanti fiscali |
| `Services/VersionDetectorService.cs` | 87 | Lettura versioni da `version.txt` o assembly |
| `Services/CommandTrackingService.cs` | 126 | Tracking in-memory stati comandi (max 50 entries) |
| `Services/DownloadProgressService.cs` | 96 | Snapshot progresso download real-time |
| `Services/InstallationCodeGenerator.cs` | 93 | Genera codice univoco identità (128 bit entropia) |
| `Services/StartupValidator.cs` | 141 | Validazione config all'avvio, crea directory mancanti |
| `Services/AgentStatusService.cs` | 32 | Stato Hub connection + ultimo heartbeat (singleton) |
| `Services/SystemInfoService.cs` | 39 | Raccoglie info statiche sistema (MachineName, OS, .NET) |
| `Middleware/BasicAuthMiddleware.cs` | 82 | HTTP Basic Auth per UI locale |
| `Security/PasswordHasher.cs` | 64 | PBKDF2-SHA256 (100k iter), timing-safe, migrazione da plaintext |
| `Models/HubMessages.cs` | 58 | DTOs SignalR locali (mirror di Prym.DTOs) |
| `Models/UpdateManifest.cs` | 65 | Schema `manifest.json` nei pacchetti ZIP |
| `Extensions/StringExtensions.cs` | 15 | `TruncateForDisplay()` helper |
| `Workers/*.cs` + `Pages/*.cs` | vari | UI Razor Pages locale |

---

## 3. Criticità di Codice

### 🔴 C1 — `DeployBinariesAsync`: firma con parametro `ILogger` non coerente

**File:** `Services/UpdateExecutorService.cs` riga ~623  
**Problema:** Il metodo statico `DeployBinariesAsync` riceve `ILogger<UpdateExecutorService>` come ultimo parametro perché è `static` e non può accedere a `this`. È un anti-pattern che crea dipendenza implicita sul tipo generico e rende il metodo difficile da testare in isolamento.

```csharp
// ATTUALE — anti-pattern
private static async Task DeployBinariesAsync(
    string extractedPath, string deployPath,
    UpdateManifest manifest, CancellationToken ct,
    ILogger<UpdateExecutorService> log)  // ← logger passato come param solo perché è static
```

**Soluzione:** Rendere il metodo di istanza (non c'è ragione reale per essere `static`), oppure accettare `ILogger` (non generico) tramite l'interfaccia base.

---

### 🔴 C2 — `MigrationRunnerService`: nuova connessione SQL per ogni script

**File:** `Services/MigrationRunnerService.cs` righe 47-65  
**Problema:** Per ogni script SQL viene aperta e chiusa una connessione separata. Se ci sono 10 script di migrazione, vengono aperte 10 connessioni distinte.

```csharp
foreach (var relativePath in scriptRelativePaths)
{
    // ...
    await using var conn = new SqlConnection(connectionString); // ← nuova conn ogni script
    await conn.OpenAsync(ct);
    await using var cmd = conn.CreateCommand();
    foreach (var batch in sql.Split(...))
    {
        cmd.CommandText = trimmed;
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
```

**Soluzione:** Aprire una singola connessione prima del loop e riutilizzarla per tutti gli script.

---

### 🔴 C3 — `AgentWorker.PersistEnrollmentAsync`: race condition alla scrittura

**File:** `Workers/AgentWorker.cs` righe 174-224  
**Problema:** Le credenziali enrollment vengono aggiornate in-memory **prima** di essere scritte su disco. Se il processo termina tra le due operazioni, al prossimo avvio `ApiKey` è vuoto e viene ri-tentato l'enrollment (che potrebbe fallire o creare un'installazione duplicata).

```csharp
options.ApiKey = result.ApiKey;            // 1. aggiornato in-memory
options.InstallationId = result.InstallationId.ToString();
await PersistEnrollmentAsync(...);         // 2. scritto su disco — se crash qui, ApiKey perso
```

**Soluzione:** Scrivere su disco prima, poi aggiornare in-memory. Usare un file temporaneo + rename atomico (come già fa `PendingInstallService`).

---

### 🔴 C4 — `AgentServerSink.Emit`: check dimensione coda non thread-safe

**File:** `Services/AgentServerSink.cs` righe 86-87  
**Problema:** Il controllo `if (_queue.Count >= BatchSize)` viene fatto **fuori** dal lock, dopo che l'evento è già stato aggiunto dentro il lock. In un ambiente altamente concorrente il valore di `_queue.Count` letto fuori lock potrebbe essere stantio, provocando mancati flush o flush multipli ridondanti.

```csharp
lock (_queue)
{
    if (_queue.Count >= MaxQueueDepth) _queue.Dequeue();
    _queue.Enqueue(entry);
}
// ← lettura Count FUORI lock
if (_queue.Count >= BatchSize)
    _ = FlushAsync(CancellationToken.None);
```

**Soluzione:** Catturare il count dentro il lock e usarlo fuori.

```csharp
int countAfterEnqueue;
lock (_queue)
{
    if (_queue.Count >= MaxQueueDepth) _queue.Dequeue();
    _queue.Enqueue(entry);
    countAfterEnqueue = _queue.Count;
}
if (countAfterEnqueue >= BatchSize)
    _ = FlushAsync(CancellationToken.None);
```

---

### 🟡 C5 — `IisManagerService.RunAppCmdAsync`: nessun timeout sul processo

**File:** `Services/IisManagerService.cs` righe 54-57  
**Problema:** Il processo `appcmd.exe` non ha un timeout esplicito. Se IIS si blocca e `appcmd stop site` non risponde, l'intera procedura di update rimane appesa indefinitamente.

```csharp
process.Start();
var output = await process.StandardOutput.ReadToEndAsync(ct);
await process.WaitForExitAsync(ct); // ← attende senza limite di tempo
```

**Soluzione:** Aggiungere un timeout di 30 secondi con `CancellationTokenSource.CreateLinkedTokenSource` e terminare il processo se scade.

---

### 🟡 C6 — `VersionDetectorService`: nessun log se `version.txt` esiste ma è vuoto

**File:** `Services/VersionDetectorService.cs` righe 19-31  
**Problema:** Se `version.txt` esiste ma è vuoto (es. scrittura parziale interrotta), il metodo cade silenziosamente sul fallback senza loggare alcun warning. Un deploy anomalo risulterebbe invisibile nei log.

```csharp
if (File.Exists(versionFile))
{
    var v = File.ReadAllText(versionFile).Trim();
    if (!string.IsNullOrEmpty(v)) return v;
    // ← nessun warning se v è vuoto!
}
```

**Soluzione:** Aggiungere `logger.LogWarning(...)` se il file esiste ma è vuoto.

---

### 🟡 C7 — `PrinterProxyController.GetWindowsPrinters`: processo sincrono in endpoint async

**File:** `Controllers/PrinterProxyController.cs` righe 303-334  
**Problema:** `proc.StandardOutput.ReadToEnd()` e `proc.WaitForExit(5000)` sono chiamate **sincrone** dentro `ListSystemPrinters()`, che è chiamata da un endpoint HTTP. Questo blocca un thread del thread pool per fino a 5 secondi.

```csharp
var output = proc.StandardOutput.ReadToEnd();  // ← sincrona, blocca thread
proc.WaitForExit(5000);                         // ← sincrona, blocca thread
```

**Soluzione:** Convertire a `ReadToEndAsync()` e `WaitForExitAsync()`. Rendere `ListSystemPrinters()` e `GetWindowsPrinters()` async.

---

### 🟡 C8 — `AgentWorker.MapComponents`: fallback silenzioso "both"

**File:** `Workers/AgentWorker.cs` righe 226-237  
**Problema:** Se `Server.Enabled = false` e `Client.Enabled = false`, il codice ritorna `3` (entrambi) — comportamento contro-intuitivo e non documentato.

```csharp
return (server, client) switch
{
    (true, true) => 3,
    (true, false) => 1,
    (false, true) => 2,
    _ => 3   // ← entrambi disabilitati = "entrambi" ?? 
};
```

**Soluzione:** Il caso `(false, false)` dovrebbe ritornare `0` (nessuno) e loggare un warning.

---

### 🟡 C9 — `PendingInstallService.GetNext`: assunzione "Server prima di Client" non versionata

**File:** `Services/PendingInstallService.cs` righe 133-147  
**Problema:** Il codice installa il Server prima del Client **solo se hanno la stessa versione esatta** (`p.Command.Version == head.Command.Version`). Se il Server è alla v2.0 e il Client è alla v1.9 (scenario possibile in caso di rilasci separati), la priorità non viene applicata.

```csharp
var serverFirst = _queue.FirstOrDefault(p =>
    p.Command.Component.Equals("Server", ...) &&
    p.Command.Version == head.Command.Version && // ← confronto stringa esatta
    File.Exists(p.LocalZipPath));
```

**Soluzione:** La priorità Server>Client dovrebbe essere sempre applicata indipendentemente dalla versione, con un commento esplicativo sul perché.

---

### 🟡 C10 — `UpdateExecutorService`: `GetNextWindowStart` duplicato

**File:** `Services/UpdateExecutorService.cs` righe 164-188  
**Problema:** Il metodo `GetNextWindowStart()` è implementato sia in `UpdateExecutorService` (privato) sia in `PendingInstallService` (pubblico), con la stessa logica. Duplicazione che può portare a divergenze.

**Soluzione:** Rimuovere la copia privata in `UpdateExecutorService` e usare quella di `PendingInstallService` (già iniettata).

---

### 🟡 C11 — `ZipFile.ExtractToDirectory`: vulnerabile a path traversal

**File:** `Services/UpdateExecutorService.cs` riga 250  
**Problema:** `ZipFile.ExtractToDirectory()` può estrarre file in percorsi arbitrari se il pacchetto ZIP contiene entry con path come `../../Windows/System32/...`. Sebbene l'integrità del pacchetto sia verificata via SHA-256, la sola checksum non elimina il rischio se la chiave di firma del server venisse compromessa.

```csharp
ZipFile.ExtractToDirectory(zipPath, tempDir, overwriteFiles: true);
```

**Soluzione:** Prima dell'estrazione, iterare le entry dello ZIP e verificare che nessuna entry abbia un `FullName` che esce da `tempDir`:

```csharp
using var zip = ZipFile.OpenRead(zipPath);
foreach (var entry in zip.Entries)
{
    var destPath = Path.GetFullPath(Path.Combine(tempDir, entry.FullName));
    if (!destPath.StartsWith(tempDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        throw new SecurityException($"ZIP path traversal rilevato: {entry.FullName}");
}
zip.ExtractToDirectory(tempDir, overwriteFiles: true);
```

---

### 🟡 C12 — `HubMessages.cs`: DTOs duplicati localmente

**File:** `Models/HubMessages.cs`  
**Problema:** I DTOs SignalR (`RegisterInstallationMessage`, `HeartbeatMessage`, ecc.) sono ridefiniti localmente nel progetto per evitare la dipendenza circolare. Se i DTOs su `Prym.DTOs` cambiano, quelli locali devono essere aggiornati manualmente — rischio di disallineamento silenzioso.

**Soluzione:** Spostare i DTOs Hub in `Prym.DTOs` in un namespace dedicato (es. `Prym.DTOs.Agent`) senza dipendenze da EF/ASP.NET, così entrambi i progetti possono referenziarli.

---

### 🟡 C13 — `InstallationCodeGenerator`: scrittura su `appsettings.json` non atomica

**File:** `Services/InstallationCodeGenerator.cs`  
**Problema:** La scrittura del codice di installazione su `appsettings.json` avviene tramite `File.WriteAllText` senza usare il pattern temp-file + rename atomico (usato correttamente in `PendingInstallService`). Un crash durante la scrittura corrompe `appsettings.json`.

**Soluzione:** Usare il pattern `WriteAllText(tmpPath) + File.Move(tmpPath, target, overwrite: true)`.

---

### 🟢 C14 — `ListDevices`: scansione hardcoded USB001-USB009

**File:** `Services/AgentPrinterService.cs` righe 73-98  
**Nota:** La scansione è limitata a `USB001`-`USB009`. Stampanti sulle porte USB010+ non vengono rilevate (limite noto di Windows per le porte di stampa virtuali, ma vale la pena documentarlo).

---

## 4. Criticità di Performance

### 🔴 P1 — `BackupService.CopyDirectoryAsync`: copia sequenziale file per file

**File:** `Services/BackupService.cs` righe 82-94  
**Problema:** I file vengono copiati uno alla volta con `foreach`. Per deployment di grandi dimensioni (es. 500MB con migliaia di file), il backup può richiedere minuti interi bloccando l'avanzamento dell'update.

```csharp
foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
{
    // copia sincrona one-by-one
    await src.CopyToAsync(dst, ct);
}
```

**Impatto stimato:** Su un deployment da 1.000 file piccoli: ~10-30 secondi vs ~2-5 secondi con parallelismo.

**Soluzione:** Parallelizzare la copia con `Parallel.ForEachAsync` (introdotto in .NET 6) con un grado di parallelismo configurabile (default 4-8):

```csharp
var files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
await Parallel.ForEachAsync(files, new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = ct },
    async (file, token) =>
    {
        var relative = Path.GetRelativePath(source, file);
        var destFile = Path.Combine(destination, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
        await using var src = File.OpenRead(file);
        await using var dst = File.Create(destFile);
        await src.CopyToAsync(dst, token);
    });
```

---

### 🔴 P2 — `MigrationRunnerService`: singola connessione per batch vs connessione per script

*Vedi anche C2.* Oltre alla logica duplicata, aprire una connessione per ogni script è lento perché ogni `SqlConnection.OpenAsync()` paga il costo del connection pool lookup + handshake. Con 5-10 script la differenza è ~100-500ms.

---

### 🟡 P3 — `UpdateExecutorService._http`: `HttpClient` non condiviso con `AgentServerSink`

**Problema:** `UpdateExecutorService` crea il proprio `HttpClient` con `SocketsHttpHandler` (corretto), ma `AgentServerSink` crea un `HttpClient` separato senza factory. In totale ci sono almeno 3 istanze di `HttpClient` nel processo (UpdateExecutor, AgentServerSink, TryEnrollAsync in AgentWorker). Ogni istanza mantiene il proprio pool di connessioni TCP, aumentando il numero di socket aperti.

**Soluzione:** Registrare un `HttpClient` named (o typed) via `IHttpClientFactory` per `AgentServerSink` e `UpdateExecutorService`. `AgentPrinterService` usa già correttamente `IHttpClientFactory`.

---

### 🟡 P4 — `PendingInstallService.SaveToDisk`: serializzazione JSON a ogni modifica

**File:** `Services/PendingInstallService.cs` righe 301-316  
**Problema:** `SaveToDisk()` viene chiamata da ogni operazione che modifica la coda (`Enqueue`, `Remove`, `Block`, `Unblock`). Se in un breve intervallo arrivano più comandi, vengono eseguite più serializzazioni + scritture su disco consecutive, ognuna con `JsonSerializer.Serialize(new JsonSerializerOptions { WriteIndented = true })` che alloca un nuovo oggetto opzioni ad ogni chiamata.

**Soluzione (parziale):** Cache delle `JsonSerializerOptions` come campo statico readonly:

```csharp
private static readonly JsonSerializerOptions _jsonOptions = 
    new() { WriteIndented = true };
```

**Soluzione (completa):** Introdurre un debounce/dirty-flag: marcare lo stato come "dirty" e salvare su disco al massimo ogni 500ms via un timer, non ad ogni singola operazione.

---

### 🟡 P5 — `DownloadProgressService`: allocazione `DownloadProgressSnapshot` ogni 300ms

**File:** `Services/DownloadProgressService.cs`  
**Problema:** `Update()` crea un nuovo record `DownloadProgressSnapshot` (immutabile) ogni volta che viene chiamato — potenzialmente 3-4 volte al secondo per tutta la durata del download. Con file grandi (1GB+) questo può generare centinaia di allocazioni.

**Soluzione:** Usare un `struct` mutabile con lock per aggiornamento in-place, oppure accettare il costo (allocation pressure bassa per record piccoli).

---

### 🟡 P6 — `WriteResponseToFileAsync`: buffer fisso 80KB, nessun progress sul flush

**File:** `Services/UpdateExecutorService.cs` righe 535-600  
**Problema:** Il buffer di download è fisso a 81.920 byte (80KB). Per connessioni veloci (LAN gigabit) un buffer più grande (256KB-1MB) ridurrebbe il numero di syscall e migliorerebbe il throughput. Per connessioni lente (ADSL) la dimensione attuale va bene.

**Soluzione:** Rendere configurabile la dimensione del buffer in `AgentOptions.DownloadBufferSizeKb` (default 80, massimo 1024).

---

### 🟡 P7 — `AgentPrinterService.ListDevices`: scansione sincrona bloccante nel costruttore

**File:** `Services/AgentPrinterService.cs` righe 69-99  
**Problema:** `ListDevices()` apre `FileStream` su ogni porta USB001-USB009 in modo sequenziale. Sebbene sia registrato come endpoint HTTP (non chiamato all'avvio), ogni chiamata a `GET /api/printer-proxy/devices` impegna il thread per la durata di tutti i tentativi di apertura (9 tentativi, potenzialmente con IO timeout).

**Soluzione:** Parallelizzare con `Parallel.ForEach` o `Task.WhenAll`.

---

### 🟡 P8 — `AgentWorker.ConnectAndRunAsync`: re-registrazione ad ogni riconnessione

**File:** `Workers/AgentWorker.cs` righe 510-526  
**Problema:** Ad ogni riconnessione al Hub (anche se la disconnessione è durata pochi secondi), viene inviato un `RegisterInstallation` completo con tutti i metadati del sistema. Se la connessione è instabile, questo genera traffico ridondante.

**Soluzione:** Inviare `RegisterInstallation` completo solo alla prima connessione; per le riconnessioni successive inviare solo l'heartbeat.

---

## 5. Criticità di Sicurezza

### 🔴 S1 — Password UI di default `Admin#123!` in `appsettings.json`

**File:** `appsettings.json` riga 46  
**Problema:** La password di default è in chiaro nel file di configurazione, che è nel repository (con il `Connection String` di sviluppo). Anche se `StartupValidator` logga un warning, non blocca l'avvio.

**Raccomandazioni:**
1. Non committare mai `appsettings.json` con segreti reali (aggiungere a `.gitignore` o usare `appsettings.Production.json` ignorato)
2. All'avvio in produzione, se la password è ancora quella di default, **bloccare** il servizio (non solo loggare un warning)
3. Supportare la lettura della password da variabili d'ambiente (`PRYM_AGENT__UI__PASSWORD`)

---

### 🔴 S2 — Credenziali SQL in chiaro nel file di configurazione

**File:** `appsettings.json` sezione `Components.Server.ConnectionString`  
**Problema:** La connection string SQL con username e password è in chiaro.

**Raccomandazioni:**
1. Usare Windows Authentication (`Integrated Security=True`) dove possibile
2. Supportare `PRYM_AGENT__COMPONENTS__SERVER__CONNECTIONSTRING` come variabile d'ambiente
3. In ambienti enterprise, integrare con Azure Key Vault o Windows DPAPI

---

### 🔴 S3 — `X-Maintenance-Secret` inviato in chiaro su HTTP

**File:** `Services/UpdateExecutorService.cs` riga 114, `Services/AgentServerSink.cs` riga 145  
**Problema:** Il secret di manutenzione viene inviato come header HTTP. Nelle configurazioni di default (`http://localhost:7242`) la comunicazione è in chiaro su HTTP, rendendo il secret intercettabile in ambienti multi-tenant o con proxy locali.

**Raccomandazione:** Forzare HTTPS per tutte le comunicazioni verso il Server in produzione. Aggiungere validazione in `StartupValidator` che verifichi che i `NotificationBaseUrl` usino `https://` in ambiente non-Development.

---

### 🟡 S4 — `PersistEnrollmentAsync`: `ApiKey` scritto in chiaro in `appsettings.json`

**File:** `Workers/AgentWorker.cs` righe 196-203  
**Problema:** L'`ApiKey` ricevuto dall'enrollment viene salvato in chiaro in `appsettings.json` su disco. Se il file è accessibile ad altri processi o utenti sulla macchina, la chiave può essere esfiltrata.

**Raccomandazione:** Cifrare l'`ApiKey` a riposo usando Windows DPAPI (`ProtectedData.Protect`). Il valore cifrato (base64) viene scritto nel file e decifrato in memoria all'avvio solo dall'account del servizio.

---

### 🟡 S5 — `MapComponents()`: fallback a `3` (Both) su configurazione ambigua

*Vedi C8.* Se entrambi i componenti sono disabilitati, l'enrollment dichiara di gestire entrambi — il Server potrebbe quindi inviare aggiornamenti per componenti che l'agent non sa gestire, con risultati imprevedibili.

---

### 🟡 S6 — `PrinterProxyController.HttpForwardAsync`: nessuna whitelist URL

**File:** `Controllers/PrinterProxyController.cs` righe 274-279  
**Problema:** La validazione del `targetUrl` verifica solo che sia un URL HTTP/HTTPS assoluto, ma non restringe il dominio/IP. Un attaccante che ha accesso al Server (o alla connessione SignalR) potrebbe usare l'agent come proxy per raggiungere host interni non altrimenti accessibili (SSRF — Server-Side Request Forgery).

```csharp
if (!Uri.TryCreate(request.TargetUrl, UriKind.Absolute, out var uri) ||
    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
    return BadRequest("...");
// ← nessuna restrizione su host/IP
```

**Soluzione:** Aggiungere in `AgentOptions` una lista `PrinterProxy.AllowedHosts` (es. `["192.168.1.*", "10.0.0.*"]`) e validare il `targetUrl` contro di essa.

---

## 6. Punti di Forza (da preservare)

Questi meccanismi sono ben progettati e non richiedono modifiche sostanziali:

| Meccanismo | Perché è buono |
|------------|----------------|
| **Coda FIFO con blocco** | Garantisce ordine migrazioni DB; blocco automatico su fallimento |
| **Separazione Download / Install** | Download immediato, install nella finestra di manutenzione |
| **Backup pre-deploy + rollback** | Restore completo in caso di fallimento; pruning automatico |
| **Download resiliente** | 5 retry con backoff esponenziale (5/15/30/60/120s), resume con Range header |
| **SHA-256 checksum** | Verifica integrità pacchetto prima dell'estrazione |
| **JSON deep-merge config** | Nuove chiavi di configurazione aggiunte automaticamente, valori esistenti preservati |
| **Enrollment automatico** | Zero-config per nuove installazioni con `EnrollmentToken` |
| **PBKDF2-SHA256 UI password** | 100k iterazioni, FixedTimeEquals, migrazione trasparente da plaintext |
| **Finestre overnight** | Gestione corretta finestre `23:00 → 01:00` |
| **Persistenza atomica** | `pending.json` scritto con temp-file + rename |
| **Standalone mode** | Proxy-only senza Hub, utile per POS dedicati |
| **Downgrade a manual** | Dopo N fallimenti, richiede approvazione operatore |

---

## 7. Implementazioni Proposte

### 7.1 Fix Critici (entro il prossimo sprint)

#### FIX-1: Validazione ZIP path traversal

```csharp
// In UpdateExecutorService.InstallFromZipAsync, PRIMA di ExtractToDirectory
private static void ValidateZipPathTraversal(string zipPath, string targetDir)
{
    using var zip = ZipFile.OpenRead(zipPath);
    var normalizedTarget = Path.GetFullPath(targetDir) + Path.DirectorySeparatorChar;
    foreach (var entry in zip.Entries)
    {
        if (string.IsNullOrEmpty(entry.Name)) continue; // directory entry
        var dest = Path.GetFullPath(Path.Combine(targetDir, entry.FullName));
        if (!dest.StartsWith(normalizedTarget, StringComparison.OrdinalIgnoreCase))
            throw new SecurityException($"ZIP path traversal rilevato: '{entry.FullName}'");
    }
}
```

#### FIX-2: Risoluzione race condition enrollment

```csharp
// In AgentWorker.TryEnrollAsync
// PRIMA: aggiorna in-memory, poi scrivi su disco
// DOPO: scrivi su disco, poi aggiorna in-memory
await PersistEnrollmentAsync(result.ApiKey, result.InstallationId); // prima
options.ApiKey = result.ApiKey;                                      // poi
options.InstallationId = result.InstallationId.ToString();
```

#### FIX-3: Count thread-safe in AgentServerSink

```csharp
int countAfterEnqueue;
lock (_queue)
{
    if (_queue.Count >= MaxQueueDepth) _queue.Dequeue();
    _queue.Enqueue(entry);
    countAfterEnqueue = _queue.Count; // cattura dentro il lock
}
if (countAfterEnqueue >= BatchSize)
    _ = FlushAsync(CancellationToken.None);
```

#### FIX-4: Timeout esplicito su appcmd.exe

```csharp
private async Task RunAppCmdAsync(string arguments, CancellationToken ct)
{
    // ...
    using var processCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    processCts.CancelAfter(TimeSpan.FromSeconds(30)); // timeout esplicito
    
    process.Start();
    var outputTask = process.StandardOutput.ReadToEndAsync(processCts.Token);
    var errorTask  = process.StandardError.ReadToEndAsync(processCts.Token);
    
    try
    {
        await process.WaitForExitAsync(processCts.Token);
    }
    catch (OperationCanceledException) when (!ct.IsCancellationRequested)
    {
        process.Kill(entireProcessTree: true);
        throw new TimeoutException($"appcmd.exe '{arguments}' non ha risposto entro 30s.");
    }
    // ...
}
```

#### FIX-5: Log warning version.txt vuoto

```csharp
if (File.Exists(versionFile))
{
    var v = File.ReadAllText(versionFile).Trim();
    if (!string.IsNullOrEmpty(v)) return v;
    logger.LogWarning("version.txt esiste in {Path} ma è vuoto — fallback su FileVersion.", deployPath);
}
```

#### FIX-6: MapComponents fix caso (false, false)

```csharp
return (server, client) switch
{
    (true, true)   => 3,
    (true, false)  => 1,
    (false, true)  => 2,
    (false, false) => 0   // nessun componente gestito
};
// + warning in StartupValidator se Components == 0 e StandaloneMode == false
```

---

### 7.2 Ottimizzazioni Performance

#### OPT-1: Backup parallelo

```csharp
// BackupService.CopyDirectoryAsync — versione ottimizzata
private static async Task CopyDirectoryAsync(string source, string destination, CancellationToken ct)
{
    var files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
    await Parallel.ForEachAsync(files, 
        new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = ct },
        async (file, token) =>
        {
            var relative = Path.GetRelativePath(source, file);
            var destFile = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            await using var src = File.OpenRead(file);
            await using var dst = File.Create(destFile);
            await src.CopyToAsync(dst, token);
        });
}
```

#### OPT-2: Connessione SQL unica per tutte le migrazioni

```csharp
// MigrationRunnerService.RunScriptsAsync — connessione condivisa
await using var conn = new SqlConnection(connectionString);
await conn.OpenAsync(ct); // aperta UNA volta

foreach (var relativePath in scriptRelativePaths)
{
    // ... ogni script riusa la stessa connessione
    await using var cmd = conn.CreateCommand();
    foreach (var batch in sql.Split(...))
    {
        cmd.CommandText = trimmed;
        cmd.CommandTimeout = options.Install.SqlCommandTimeoutSeconds;
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
// conn chiusa dal using alla fine
```

#### OPT-3: Cache JsonSerializerOptions in PendingInstallService

```csharp
private static readonly JsonSerializerOptions _jsonOpts = new() { WriteIndented = true };
private static readonly JsonSerializerOptions _jsonReadOpts = new() { PropertyNameCaseInsensitive = true };

// In SaveToDisk:
var json = JsonSerializer.Serialize(state, _jsonOpts);

// In LoadFromDisk:
var state = JsonSerializer.Deserialize<PersistentState>(json, _jsonReadOpts);
```

#### OPT-4: ListDevices asincrono parallelo

```csharp
public IReadOnlyList<string> ListDevices()
{
    var found = new System.Collections.Concurrent.ConcurrentBag<string>();
    Parallel.For(1, 10, i =>
    {
        var suffix = $"USB00{i}";
        var path   = BuildDevicePath(suffix);
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Write,
                FileShare.ReadWrite, bufferSize: 1, FileOptions.None);
            found.Add(suffix);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    });
    return found.OrderBy(x => x).ToList().AsReadOnly();
}
```

#### OPT-5: Buffer download configurabile

```csharp
// In AgentOptions
/// <summary>Dimensione buffer download in KB (default 80, max 4096).</summary>
public int DownloadBufferSizeKb { get; set; } = 80;

// In UpdateExecutorService.WriteResponseToFileAsync
var bufferSize = Math.Clamp(options.DownloadBufferSizeKb, 16, 4096) * 1024;
var buffer = new byte[bufferSize];
await using var fileStream = new FileStream(tmpPath, fileMode, FileAccess.Write, 
    FileShare.None, bufferSize, useAsync: true);
```

---

### 7.3 Refactoring Architetturale

#### REFACT-1: Rimozione `GetNextWindowStart` duplicato

Eliminare `GetNextWindowStart()` privato da `UpdateExecutorService` e usare `pendingInstallService.GetNextWindowStart()` iniettato tramite DI.

```csharp
// UpdateExecutorService — costruttore aggiornato
public class UpdateExecutorService(
    AgentOptions options,
    BackupService backupService,
    IisManagerService iisManagerService,
    MigrationRunnerService migrationRunner,
    DownloadProgressService downloadProgress,
    PendingInstallService pendingInstallService, // ← aggiunto
    ILogger<UpdateExecutorService> logger)
```

#### REFACT-2: `DeployBinariesAsync` come metodo di istanza

Rimuovere il parametro `ILogger` dalla firma e usare `logger` del campo di istanza.

#### REFACT-3: DTOs Hub in `Prym.DTOs`

Spostare i record di `HubMessages.cs` in `Prym.DTOs/Agent/` con namespace `Prym.DTOs.Agent`, eliminando la ridefinizione locale e il rischio di disallineamento.

#### REFACT-4: Allowlist SSRF per printer proxy

```csharp
// In AgentOptions
public class PrinterProxyOptions
{
    /// <summary>
    /// Pattern IP/hostname consentiti per http-forward (es. "192.168.1.*", "10.0.*.*").
    /// Vuoto = nessuna restrizione (compatibilità backward).
    /// </summary>
    public List<string> AllowedHostPatterns { get; set; } = [];
}

// In PrinterProxyController.HttpForwardAsync
if (options.PrinterProxy.AllowedHostPatterns.Count > 0)
{
    var host = uri.Host;
    if (!options.PrinterProxy.AllowedHostPatterns.Any(p => MatchesPattern(host, p)))
        return StatusCode(403, $"Host '{host}' non è nella lista consentita.");
}
```

---

### 7.4 Sicurezza

#### SEC-1: Blocco avvio se password default in produzione

```csharp
// In StartupValidator.Run
if (options.UI.Password == "Admin#123!")
{
    if (IsProduction())
    {
        logger.LogCritical("[StartupValidator] PASSWORD DEFAULT RILEVATA IN PRODUZIONE. Avvio bloccato.");
        return false; // blocca l'avvio
    }
    else
    {
        logger.LogWarning("[StartupValidator] Password UI ancora quella di default. Cambiarla prima del deploy in produzione.");
    }
}
```

#### SEC-2: Supporto variabili d'ambiente per segreti

ASP.NET Core supporta nativamente le variabili d'ambiente come `PRYMagent__UI__PASSWORD` per sovrascrivere qualsiasi sezione di configurazione. Aggiungere documentazione esplicita nel README e in `appsettings.json`.

#### SEC-3: Enforcing HTTPS in produzione

```csharp
// In StartupValidator.Run — solo in produzione
if (options.Components.Server.Enabled &&
    !string.IsNullOrWhiteSpace(options.Components.Server.NotificationBaseUrl) &&
    options.Components.Server.NotificationBaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
{
    logger.LogWarning("[StartupValidator] NotificationBaseUrl usa HTTP (non HTTPS) — le comunicazioni col Server non sono cifrate.");
}
```

---

### 7.5 Testing

Il progetto attualmente **non ha test automatizzati**. Le aree prioritarie per la copertura:

| Area | Tipo | Priorità |
|------|------|----------|
| `PendingInstallService` (enqueue, dequeue, blocco, finestre) | Unit | 🔴 Alta |
| `PasswordHasher` (hash, verify, plaintext legacy) | Unit | 🔴 Alta |
| `UpdateExecutorService.MergeJsonElements` | Unit | 🔴 Alta |
| `AgentWorker.IsNewerVersion` | Unit | 🟡 Media |
| `VersionDetectorService` (fallback chains) | Unit | 🟡 Media |
| Flusso completo update (mock HttpClient + mock IIS) | Integration | 🟡 Media |
| Scenario rollback su health check failure | Integration | 🟡 Media |
| ZIP path traversal prevention | Security | 🔴 Alta |
| Basic Auth bypass | Security | 🟡 Media |

---

## 8. Roadmap Prioritizzata

### Sprint 1 — Sicurezza e Correctness (obbligatorio)

| ID | Azione | File | Impatto |
|----|--------|------|---------|
| FIX-1 | Validazione ZIP path traversal | `UpdateExecutorService` | 🔴 Sicurezza |
| FIX-2 | Race condition enrollment (disk-first) | `AgentWorker` | 🔴 Correctness |
| FIX-3 | Thread-safe count in AgentServerSink | `AgentServerSink` | 🔴 Correctness |
| FIX-6 | MapComponents fix (false,false)→0 | `AgentWorker` | 🟡 Correctness |
| SEC-1 | Blocco avvio se password default in prod | `StartupValidator` | 🔴 Sicurezza |

### Sprint 2 — Performance e Robustezza

| ID | Azione | File | Guadagno |
|----|--------|------|---------|
| OPT-1 | Backup parallelo | `BackupService` | ⚡ 3-10x più veloce |
| OPT-2 | Connessione SQL unica per migrazioni | `MigrationRunnerService` | ⚡ ~100-500ms risparmio |
| OPT-3 | Cache JsonSerializerOptions | `PendingInstallService` | ⚡ Minor GC pressure |
| FIX-4 | Timeout su appcmd.exe | `IisManagerService` | 🛡️ No hang indefinito |
| FIX-5 | Log warning version.txt vuoto | `VersionDetectorService` | 🔍 Osservabilità |

### Sprint 3 — Refactoring e Qualità

| ID | Azione | File | Beneficio |
|----|--------|------|---------|
| REFACT-1 | Rimozione `GetNextWindowStart` duplicato | `UpdateExecutorService` | DRY |
| REFACT-2 | `DeployBinariesAsync` → istanza | `UpdateExecutorService` | Pulizia |
| REFACT-3 | DTOs Hub in `Prym.DTOs` | `Models/HubMessages.cs` | Single source of truth |
| OPT-4 | `ListDevices` parallelo | `AgentPrinterService` | ⚡ Riduzione latenza |
| OPT-5 | Buffer download configurabile | `UpdateExecutorService` | ⚡ Throughput LAN |
| SEC-2 | Documentazione variabili d'ambiente | `appsettings.json` + README | 🔐 Best practice |
| SEC-3 | Warning HTTPS in produzione | `StartupValidator` | 🔐 Osservabilità |

### Sprint 4 — Testing e SSRF Prevention

| ID | Azione | Beneficio |
|----|--------|---------|
| TEST-1 | Unit test `PendingInstallService` | Regressioni FIFO/blocco |
| TEST-2 | Unit test `PasswordHasher` | Sicurezza credential |
| TEST-3 | Unit test `MergeJsonElements` | Config upgrade corretto |
| TEST-4 | Security test ZIP traversal | Verifica FIX-1 |
| REFACT-4 | Allowlist SSRF printer proxy | `PrinterProxyController` / `AgentOptions` |
| SEC-4 | DPAPI per ApiKey a riposo | `AgentWorker` |

---

*Documento generato ad aprile 2026. Riesaminare dopo ogni refactoring significativo.*
