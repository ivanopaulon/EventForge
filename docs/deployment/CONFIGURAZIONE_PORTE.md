# Guida alla Configurazione delle Porte - Prym

## Panoramica

Prym utilizza un sistema di configurazione flessibile per le porte di comunicazione tra server e client.

**Mappa porte (autoritative):**

| Progetto | Ambiente | HTTP | HTTPS |
|---|---|---|---|
| Server | Dev (Kestrel) | 5240 | 7241 |
| Server | Prod (IIS) | — | 7242 |
| Client | Dev (Kestrel) | 5048 | 7009 |
| Client | Prod (IIS) | — | 5240 |
| UpdateHub | Dev (Kestrel) | 59407 | 59406 |
| UpdateHub | Prod IIS / Standalone | 7243 | 7244 |
| UpdateAgent | Prod (Windows Service, localhost) | 5780 | — |

## Configurazione Server (Prym.Server)

### Sviluppo Locale

#### Metodo 1: launchSettings.json (Consigliato per sviluppo)

Edita il file `Prym.Server/Properties/launchSettings.json`:

```json
{
  "profiles": {
    "https": {
      "commandName": "Project",
      "applicationUrl": "https://localhost:7241;http://localhost:5240",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

#### Metodo 2: Parametro da riga di comando

```bash
dotnet run --project Prym.Server --urls "https://localhost:7241;http://localhost:5240"
```

#### Metodo 3: Variabile d'ambiente

**Linux/macOS:**
```bash
export ASPNETCORE_URLS="https://localhost:7241;http://localhost:5240"
dotnet run --project Prym.Server
```

**Windows PowerShell:**
```powershell
$env:ASPNETCORE_URLS="https://localhost:7241;http://localhost:5240"
dotnet run --project Prym.Server
```

**Windows CMD:**
```cmd
set ASPNETCORE_URLS=https://localhost:7241;http://localhost:5240
dotnet run --project Prym.Server
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
  - ASPNETCORE_URLS=https://+:7241;http://+:5240
ports:
  - "7241:7241"
  - "5240:5240"
```

#### Servizio systemd (Linux)

Crea un file di override per il servizio:

```ini
[Service]
Environment="ASPNETCORE_URLS=https://+:7241;http://+:5240"
```

## Configurazione UpdateHub (Prym.UpdateHub)

L'UpdateHub espone la propria UI e l'API SignalR su due porte indipendenti configurabili in `appsettings.json`.

### Porte predefinite

| Protocollo | Porta | Note |
|---|---|---|
| HTTPS | 7244 | Produzione IIS; Kestrel standalone |
| HTTP | 7243 | Kestrel standalone; disabilitabile con `0` |

### Configurazione `appsettings.json`

```json
"UpdateHub": {
  "UI": {
    "HttpsPort": 7244,
    "HttpPort":  7243
  }
}
```

Imposta una porta a `0` per disabilitarla (es. HTTP-only o HTTPS-only).

### Produzione su IIS

Il setup script (`Setup-Prym-UpdateHub.ps1`) legge automaticamente `UI.HttpsPort` da `appsettings.json` per configurare il binding IIS. Non è necessario modificare lo script.

### Standalone (senza IIS)

Kestrel rispetta i valori `HttpsPort` / `HttpPort` e si lega direttamente alle porte configurate. Per HTTPS standalone è necessario un certificato configurato nella sezione `Kestrel:Endpoints` di `appsettings.json`.

### Sviluppo Locale

In sviluppo, `launchSettings.json` definisce porte separate per evitare conflitti con le istanze di produzione:

```
https://localhost:59406 (dev HTTPS)
http://localhost:59407  (dev HTTP)
```

---

## Configurazione UpdateAgent (Prym.UpdateAgent)

L'Agent espone una UI locale (`localhost`-only) su una singola porta HTTP (non HTTPS — il traffico è sempre intra-macchina).

```json
"UpdateAgent": {
  "UI": {
    "Port": 5780
  }
}
```

---



### File di Configurazione

Il client utilizza file `appsettings.json` per configurare l'URL del server API.

#### Sviluppo Locale

Edita `Prym.Client/wwwroot/appsettings.Development.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7241/"
  }
}
```

#### Produzione

Edita `Prym.Client/wwwroot/appsettings.Production.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://tuoserver.com:7241/"
  }
}
```

#### File Base

Il file `Prym.Client/wwwroot/appsettings.json` fornisce la configurazione predefinita:

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

Quando modifichi le porte del server, **devi aggiornare** anche la configurazione CORS in `Prym.Server/appsettings.json` (oppure nel file `appsettings.{Ambiente}.json`):

```json
"Cors": {
  "AllowedOrigins": [
    "https://localhost:7009",  // Client dev HTTPS
    "http://localhost:5048"    // Client dev HTTP
  ]
}
```

In produzione IIS il setup script aggiorna automaticamente `AllowedOrigins` con l'origine del sito Client (es. `https://localhost:5240`).

## Profili Disponibili

### Profilo "https" (Predefinito)
Porte: 7241 (HTTPS) + 5240 (HTTP)
- Configurazione raccomandata per nuovi sviluppi

### Profilo "http"
Porta: 5240 (solo HTTP)
- Utile per test senza certificati SSL

### Profilo "Legacy"
Porte: 7001 (HTTPS) + 5000 (HTTP)
- Mantiene compatibilità con configurazioni precedenti

## Testing della Configurazione

### Verifica Porte Server

```bash
# Avvia il server
cd Prym.Server
dotnet run

# In un altro terminale, verifica le porte in ascolto
# Linux/macOS:
netstat -an | grep -E "7241|5240"

# Windows:
netstat -an | findstr "7241 5240"
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
curl http://localhost:5240/health
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
- Server: 7241 (HTTPS), 5240 (HTTP)
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
         - ASPNETCORE_URLS=https://+:7241;http://+:5240
       ports:
         - "7241:7241"
         - "5240:5240"
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
**Autore:** Prym Team
