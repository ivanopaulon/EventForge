# Getting Started with Prym

Benvenuto in Prym! Questa guida ti aiuterà a configurare rapidamente l'ambiente di sviluppo e a comprendere la struttura del progetto.

## 📋 Prerequisiti

### Software Richiesto
- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server** (LocalDB per sviluppo) - [Download](https://www.microsoft.com/sql-server/sql-server-downloads)
- **Visual Studio 2022** o **VS Code** - [VS Download](https://visualstudio.microsoft.com/)
- **Git** - [Download](https://git-scm.com/)

### Verifiche Prerequisiti
```bash
# Verifica .NET 8
dotnet --version

# Verifica Git
git --version

# Verifica SQL Server LocalDB
sqllocaldb info
```

## 🚀 Setup Iniziale

### 1. Clone Repository
```bash
git clone https://github.com/ivanopaulon/Prym.git
cd Prym
```

### 2. Configurazione Database
```bash
# Configura connection string (opzionale per LocalDB)
# Modifica appsettings.Development.json se necessario

# Esegui migrazioni database
dotnet ef database update --project Prym.Server
```

### 3. Build Progetto
```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Verifica build success
echo "Build Status: $?"
```

### 4. Avvio Applicazione
```bash
# Avvia server
dotnet run --project Prym.Server

# L'applicazione sarà disponibile su:
# https://localhost:7241 (HTTPS - porta predefinita)
# http://localhost:5240 (HTTP - porta predefinita)
```

### 5. Configurazione Porte

#### Porte Server (Prym.Server)

**Porte predefinite:**
- HTTPS: `7241`
- HTTP: `5240`

**Modifica porte in sviluppo:**
Edita `Prym.Server/Properties/launchSettings.json`:
```json
{
  "profiles": {
    "https": {
      "applicationUrl": "https://localhost:7241;http://localhost:5240"
    }
  }
}
```

**Modifica porte in produzione/IIS:**
```bash
# Variabile d'ambiente
export ASPNETCORE_URLS="https://localhost:7241;http://localhost:5240"

# Parametro da riga di comando
dotnet run --project Prym.Server --urls "https://localhost:7241;http://localhost:5240"
```

#### Porte Client (Prym.Client)

**Porta predefinita server API:** `https://localhost:7241/`

**Modifica porta API del client:**
Edita `Prym.Client/wwwroot/appsettings.json`:
```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7241/"
  }
}
```

Per ambienti diversi, crea file specifici:
- `appsettings.Development.json` - per sviluppo locale
- `appsettings.Production.json` - per produzione

**Importante:** Se modifichi la porta del server, aggiorna anche:
1. Il file `appsettings.json` del client con la nuova porta
2. La configurazione CORS in `Prym.Server/Program.cs`

## 🎯 Prima Configurazione

### Default Admin User
Al primo avvio, Prym crea automaticamente un utente amministratore:
- **Email**: admin@eventforge.local
- **Password**: Admin123!
- **Ruolo**: SuperAdmin

⚠️ **Importante**: Cambia la password predefinita al primo login!

### Configurazione Base
1. **Login**: Usa le credenziali admin predefinite
2. **Profilo**: Aggiorna il profilo amministratore
3. **Tenant**: Configura il tenant predefinito
4. **Utenti**: Crea utenti aggiuntivi se necessario

## 🏗️ Struttura Progetto

### Organizzazione Solution
```
Prym/
├── Prym.Server/          # Backend Blazor Server
│   ├── Controllers/            # API Controllers
│   ├── Data/                   # Entity Framework setup
│   │   ├── Entities/          # Database entities
│   │   └── Contexts/          # DbContext classes
│   ├── Services/              # Business logic services
│   └── Components/            # Blazor components
├── Prym.Client/         # Frontend (se applicabile)
├── Prym.DTOs/           # Data Transfer Objects
├── Prym.Tests/          # Test project
└── docs/                      # Documentazione
    ├── core/                  # Documentazione core
    ├── backend/               # Documentazione backend
    ├── frontend/              # Documentazione frontend
    ├── testing/               # Documentazione testing
    ├── deployment/            # Documentazione deployment
    ├── features/              # Guide funzionalità
    └── migration/             # Report migrazioni
```

### Architettura Livelli
1. **Presentation Layer**: Blazor Server components
2. **API Layer**: REST API controllers
3. **Business Layer**: Services e business logic
4. **Data Layer**: Entity Framework e database

## 📚 Prossimi Passi

### Per Sviluppatori Backend
1. **Architettura**: Leggi [Backend Documentation](../backend/)
2. **API Development**: Consulta [API Development Guide](../backend/api-development.md)
3. **Database**: Studia [Data Models](../backend/data-models.md)
4. **Testing**: Implementa [Testing Guidelines](../testing/)

### Per Sviluppatori Frontend
1. **UI Guidelines**: Leggi [Frontend Documentation](../frontend/)
2. **Components**: Studia [MudBlazor Components](../frontend/mudblazor.md)
3. **Theming**: Implementa [Theming System](../frontend/theming.md)
4. **Translations**: Configura [Translation System](../frontend/translation.md)

### Per DevOps
1. **Deployment**: Configura [Deployment Guide](../deployment/)
2. **Environment**: Setup [Environment Configuration](../deployment/environment.md)
3. **Monitoring**: Implementa [Monitoring Setup](../deployment/monitoring.md)

## 🔧 Development Workflow

### Workflow Giornaliero
1. **Pull**: `git pull origin main`
2. **Build**: `dotnet build`
3. **Test**: `dotnet test`
4. **Develop**: Implementa features
5. **Test**: Testa modifiche localmente
6. **Commit**: `git commit -m "descrizione"`
7. **Push**: `git push origin feature-branch`

### Best Practices
- **Commits**: Messaggi descrittivi e atomici
- **Testing**: Test prima di commit
- **Code Review**: Review prima di merge
- **Documentation**: Aggiorna documentazione

### Comandi Utili
```bash
# Build e test completo
dotnet build && dotnet test

# Avvio con hot reload
dotnet watch run --project Prym.Server

# Analisi route conflicts
./analyze-routes.sh

# Esecuzione audit
cd audit && dotnet run
```

## 🧪 Testing Locale

### Test Categories
```bash
# Test unitari
dotnet test --filter Category=Unit

# Test integrazione
dotnet test --filter Category=Integration

# Analisi route
dotnet test --filter Category=RouteAnalysis
```

### Debugging
- **Breakpoints**: Usa debugger VS/VS Code
- **Logging**: Controlla output console
- **Database**: Usa SQL Server Object Explorer
- **Network**: Monitor network tab browser

## 🔍 Risoluzione Problemi Comuni

### Build Errors
```bash
# Pulisci e rebuilda
dotnet clean
dotnet restore
dotnet build
```

### Database Issues
```bash
# Reset database
dotnet ef database drop --project Prym.Server
dotnet ef database update --project Prym.Server
```

### Port Conflicts
Se le porte predefinite (7241/5240) sono già in uso:

**Opzione 1 - Modifica launchSettings.json:**
```bash
# Edita Prym.Server/Properties/launchSettings.json
# Cambia applicationUrl con le tue porte
```

**Opzione 2 - Usa parametro da riga di comando:**
```bash
dotnet run --project Prym.Server --urls "https://localhost:TUA_PORTA"
```

**Importante:** Ricorda di aggiornare anche il file `Prym.Client/wwwroot/appsettings.json` con la nuova porta del server!

### Permission Issues
- Verifica ruoli utente nel database
- Controlla configurazione JWT
- Valida tenant assignment

### MONO Hash Errors in Console
Se vedi messaggi `[MONO] /__w/1/s/src/mono/mono/metadata/mono-hash.c` nella console del browser:
- Questi messaggi sono stati **risolti** con il sistema di filtro console
- Vedi documentazione completa: `docs/frontend/MONO_HASH_ERROR_FIX.md`
- Se i messaggi persistono, verifica che `console-filter.js` sia caricato
- Esegui rebuild pulito: `dotnet clean && dotnet build`

## 📖 Risorse Aggiuntive

### Documentazione Tecnica
- [Architecture Overview](../backend/architecture-overview.md)
- [UI/UX Guidelines](../frontend/ui-guidelines.md)
- [Testing Strategy](../testing/testing-guide.md)

### Community & Support
- **Repository**: [GitHub Prym](https://github.com/ivanopaulon/Prym)
- **Issues**: [GitHub Issues](https://github.com/ivanopaulon/Prym/issues)
- **Discussions**: [GitHub Discussions](https://github.com/ivanopaulon/Prym/discussions)

### Learning Resources
- **Blazor**: [Microsoft Blazor Docs](https://docs.microsoft.com/aspnet/core/blazor/)
- **MudBlazor**: [MudBlazor Documentation](https://mudblazor.com/)
- **Entity Framework**: [EF Core Docs](https://docs.microsoft.com/ef/core/)

---

🎉 **Congratulazioni!** Hai completato il setup di Prym. Ora sei pronto per iniziare lo sviluppo!

Per domande o problemi, consulta la [documentazione completa](../README.md) o apri una issue nel repository.