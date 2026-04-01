# Guida alla Configurazione delle Porte - EventForge

## Panoramica

EventForge utilizza un sistema di configurazione flessibile per le porte di comunicazione tra server e client.

**Porte predefinite:**
- **HTTPS**: 7241
- **HTTP**: 7240

## Configurazione Server (EventForge.Server)

### Sviluppo Locale

#### Metodo 1: launchSettings.json (Consigliato per sviluppo)

Edita il file `EventForge.Server/Properties/launchSettings.json`:

```json
{
  "profiles": {
    "https": {
      "commandName": "Project",
      "applicationUrl": "https://localhost:7241;http://localhost:7240",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

#### Metodo 2: Parametro da riga di comando

```bash
dotnet run --project EventForge.Server --urls "https://localhost:7241;http://localhost:7240"
```

#### Metodo 3: Variabile d'ambiente

**Linux/macOS:**
```bash
export ASPNETCORE_URLS="https://localhost:7241;http://localhost:7240"
dotnet run --project EventForge.Server
```

**Windows PowerShell:**
```powershell
$env:ASPNETCORE_URLS="https://localhost:7241;http://localhost:7240"
dotnet run --project EventForge.Server
```

**Windows CMD:**
```cmd
set ASPNETCORE_URLS=https://localhost:7241;http://localhost:7240
dotnet run --project EventForge.Server
```

### Produzione e IIS

#### IIS
IIS gestisce automaticamente le porte tramite i binding del sito web. Non è necessaria alcuna configurazione aggiuntiva nel codice.

1. Apri **IIS Manager**
2. Seleziona il tuo sito web
3. Clicca su **Bindings** nel pannello di destra
4. Aggiungi/modifica i binding per le porte desiderate
5. IIS passerà automaticamente le informazioni sulla porta ad ASP.NET Core

#### Docker

Nel `Dockerfile` o `docker-compose.yml`:

```yaml
environment:
  - ASPNETCORE_URLS=https://+:7241;http://+:7240
ports:
  - "7241:7241"
  - "7240:7240"
```

#### Servizio systemd (Linux)

Crea un file di override per il servizio:

```ini
[Service]
Environment="ASPNETCORE_URLS=https://+:7241;http://+:7240"
```

## Configurazione Client (EventForge.Client)

### File di Configurazione

Il client utilizza file `appsettings.json` per configurare l'URL del server API.

#### Sviluppo Locale

Edita `EventForge.Client/wwwroot/appsettings.Development.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7241/"
  }
}
```

#### Produzione

Edita `EventForge.Client/wwwroot/appsettings.Production.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://tuoserver.com:7241/"
  }
}
```

#### File Base

Il file `EventForge.Client/wwwroot/appsettings.json` fornisce la configurazione predefinita:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7241/"
  }
}
```

### Priorità dei File di Configurazione

Blazor WebAssembly carica i file di configurazione in questo ordine:
1. `appsettings.json` (base)
2. `appsettings.{Environment}.json` (sovrascrive la base)

L'ambiente è determinato dalla proprietà `HostEnvironment.Environment` durante l'esecuzione.

## Configurazione CORS

Quando modifichi le porte del server, **devi aggiornare** anche la configurazione CORS in `EventForge.Server/Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        _ = policy
            .WithOrigins(
                "https://localhost:7241",  // Porta HTTPS
                "http://localhost:7240",    // Porta HTTP
                "https://localhost:5000",   // Porta legacy
                "https://localhost:7009"    // Altre porte se necessarie
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
```

## Profili Disponibili

### Profilo "https" (Predefinito)
Porte: 7241 (HTTPS) + 7240 (HTTP)
- Configurazione raccomandata per nuovi sviluppi

### Profilo "http"
Porta: 7240 (solo HTTP)
- Utile per test senza certificati SSL

### Profilo "Legacy"
Porte: 7001 (HTTPS) + 5000 (HTTP)
- Mantiene compatibilità con configurazioni precedenti

## Testing della Configurazione

### Verifica Porte Server

```bash
# Avvia il server
cd EventForge.Server
dotnet run

# In un altro terminale, verifica le porte in ascolto
# Linux/macOS:
netstat -an | grep -E "7241|7240"

# Windows:
netstat -an | findstr "7241 7240"
```

### Verifica Connessione Client-Server

1. Avvia il server
2. Apri il browser e vai su `https://localhost:7241`
3. Verifica nella console del browser che non ci siano errori CORS
4. Controlla la tab **Network** per vedere le chiamate API al server

### Test con cURL

```bash
# Test endpoint HTTPS
curl -k https://localhost:7241/health

# Test endpoint HTTP
curl http://localhost:7240/health
```

## Risoluzione Problemi Comuni

### Porta già in uso

**Errore:** `Failed to bind to address https://127.0.0.1:7241: address already in use`

**Soluzione:**
1. Trova il processo che usa la porta:
   ```bash
   # Linux/macOS
   lsof -i :7241
   
   # Windows
   netstat -ano | findstr :7241
   ```

2. Termina il processo o usa una porta diversa

### Errori CORS

**Errore:** `Access to fetch at 'https://localhost:7241/...' has been blocked by CORS policy`

**Soluzione:**
1. Verifica che la porta client sia inclusa in CORS (Program.cs del server)
2. Riavvia il server dopo modifiche CORS
3. Pulisci la cache del browser

### Certificato SSL non valido in sviluppo

**Soluzione:**
```bash
# Installa certificato di sviluppo
dotnet dev-certs https --trust
```

### Client non si connette al server

**Checklist:**
1. ✓ Server in esecuzione sulla porta corretta
2. ✓ File appsettings.json del client con BaseUrl corretta
3. ✓ Porta del client inclusa in CORS del server
4. ✓ Firewall non blocca la porta
5. ✓ Certificato SSL valido (o trusted per sviluppo)

## Esempi di Configurazione

### Scenario 1: Sviluppo Locale Standard
- Server: 7241 (HTTPS), 7240 (HTTP)
- Client: legge da appsettings.Development.json → "https://localhost:7241/"
- Nessuna modifica necessaria

### Scenario 2: Porta Personalizzata (es. 8443)
1. Server launchSettings.json:
   ```json
   "applicationUrl": "https://localhost:8443"
   ```
2. Client appsettings.json:
   ```json
   "BaseUrl": "https://localhost:8443/"
   ```
3. Server Program.cs CORS:
   ```csharp
   .WithOrigins("https://localhost:8443", ...)
   ```

### Scenario 3: Deploy su IIS (porta 80/443)
1. Configura binding IIS per porta 80/443
2. Client appsettings.Production.json:
   ```json
   "BaseUrl": "https://tuodominio.com/"
   ```
3. CORS del server include il dominio di produzione

### Scenario 4: Deploy su Docker
1. docker-compose.yml:
   ```yaml
   services:
     eventforge-server:
       environment:
         - ASPNETCORE_URLS=https://+:7241;http://+:7240
       ports:
         - "7241:7241"
         - "7240:7240"
   ```
2. Client si connette all'IP/dominio del container

## Best Practice

1. ✅ **Non hardcodare mai le porte nel codice** - usa sempre file di configurazione
2. ✅ **Usa HTTPS in produzione** - mai HTTP per dati sensibili
3. ✅ **Mantieni CORS aggiornato** - includi tutte le origini client legittime
4. ✅ **Usa profili diversi per ambienti diversi** - Development, Staging, Production
5. ✅ **Documenta le porte personalizzate** - nel README o wiki del progetto
6. ✅ **Testa dopo ogni modifica** - verifica connessione client-server
7. ✅ **Usa variabili d'ambiente per secrets** - mai committare credenziali o chiavi

## Riferimenti

- [ASP.NET Core Configuration](https://docs.microsoft.com/aspnet/core/fundamentals/configuration/)
- [Kestrel Web Server Configuration](https://docs.microsoft.com/aspnet/core/fundamentals/servers/kestrel)
- [Blazor WebAssembly Configuration](https://docs.microsoft.com/aspnet/core/blazor/fundamentals/configuration)
- [CORS in ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/cors)

---

**Ultima modifica:** 2025-01-12  
**Versione:** 1.0  
**Autore:** EventForge Team
