# Getting Started with EventForge

Benvenuto in EventForge! Questa guida ti aiuterà a configurare rapidamente l'ambiente di sviluppo e a comprendere la struttura del progetto.

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
git clone https://github.com/ivanopaulon/EventForge.git
cd EventForge
```

### 2. Configurazione Database
```bash
# Configura connection string (opzionale per LocalDB)
# Modifica appsettings.Development.json se necessario

# Esegui migrazioni database
dotnet ef database update --project EventForge.Server
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
dotnet run --project EventForge.Server

# L'applicazione sarà disponibile su:
# https://localhost:7001 (HTTPS)
# http://localhost:5000 (HTTP)
```

## 🎯 Prima Configurazione

### Default Admin User
Al primo avvio, EventForge crea automaticamente un utente amministratore:
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
EventForge/
├── EventForge.Server/          # Backend Blazor Server
│   ├── Controllers/            # API Controllers
│   ├── Data/                   # Entity Framework setup
│   │   ├── Entities/          # Database entities
│   │   └── Contexts/          # DbContext classes
│   ├── Services/              # Business logic services
│   └── Components/            # Blazor components
├── EventForge.Client/         # Frontend (se applicabile)
├── EventForge.DTOs/           # Data Transfer Objects
├── EventForge.Tests/          # Test project
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
dotnet watch run --project EventForge.Server

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
dotnet ef database drop --project EventForge.Server
dotnet ef database update --project EventForge.Server
```

### Port Conflicts
```bash
# Cambia porte in launchSettings.json
# O usa porte diverse:
dotnet run --project EventForge.Server --urls "https://localhost:7002"
```

### Permission Issues
- Verifica ruoli utente nel database
- Controlla configurazione JWT
- Valida tenant assignment

## 📖 Risorse Aggiuntive

### Documentazione Tecnica
- [Architecture Overview](../backend/architecture-overview.md)
- [UI/UX Guidelines](../frontend/ui-guidelines.md)
- [Testing Strategy](../testing/testing-guide.md)

### Community & Support
- **Repository**: [GitHub EventForge](https://github.com/ivanopaulon/EventForge)
- **Issues**: [GitHub Issues](https://github.com/ivanopaulon/EventForge/issues)
- **Discussions**: [GitHub Discussions](https://github.com/ivanopaulon/EventForge/discussions)

### Learning Resources
- **Blazor**: [Microsoft Blazor Docs](https://docs.microsoft.com/aspnet/core/blazor/)
- **MudBlazor**: [MudBlazor Documentation](https://mudblazor.com/)
- **Entity Framework**: [EF Core Docs](https://docs.microsoft.com/ef/core/)

---

🎉 **Congratulazioni!** Hai completato il setup di EventForge. Ora sei pronto per iniziare lo sviluppo!

Per domande o problemi, consulta la [documentazione completa](../README.md) o apri una issue nel repository.