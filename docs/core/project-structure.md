# Prym Project Structure

Guida completa alla struttura del progetto Prym e organizzazione del codice.

## 📁 Struttura Directory Principale

```
Prym/
├── 📁 Prym.Server/           # Backend Blazor Server application
├── 📁 Prym.Client/           # Frontend client (se applicabile)
├── 📁 Prym.DTOs/             # Data Transfer Objects condivisi
├── 📁 Prym.Tests/            # Test project unificato
├── 📁 docs/                        # Documentazione organizzata
├── 📁 audit/                       # Sistema audit automatizzato
├── 📄 Prym.sln               # Solution file
├── 📄 Directory.Packages.props     # Gestione pacchetti centralizzata
├── 📄 README.md                    # Documentazione principale
└── 🔧 analyze-routes.sh            # Script analisi route
```

## 🏗️ Prym.Server - Backend Structure

### Directory Principali
```
Prym.Server/
├── 📁 Controllers/                 # API Controllers REST
│   ├── AuthController.cs
│   ├── ProductsController.cs
│   ├── DocumentsController.cs
│   └── ...
├── 📁 Data/                        # Entity Framework setup
│   ├── 📁 Entities/               # Database entities
│   │   ├── 📁 Core/               # Entità core (User, Tenant, etc.)
│   │   ├── 📁 Products/           # Entità prodotti
│   │   ├── 📁 Documents/          # Entità documenti
│   │   ├── 📁 Notifications/      # Entità notifiche
│   │   └── 📁 Audit/              # Entità audit
│   ├── 📁 Contexts/               # DbContext classes
│   │   ├── EventDataContext.cs
│   │   └── EventLoggerContext.cs
│   └── 📁 Migrations/             # Entity Framework migrations
├── 📁 Services/                    # Business logic services
│   ├── 📁 Interfaces/             # Service interfaces
│   ├── 📁 Implementations/        # Service implementations
│   └── 📁 Extensions/             # Service extensions
├── 📁 Components/                  # Blazor components
│   ├── 📁 Layout/                 # Layout components
│   ├── 📁 Pages/                  # Page components
│   ├── 📁 Shared/                 # Shared components
│   └── 📁 Dialogs/                # Modal dialogs
├── 📁 wwwroot/                     # Static files
│   ├── 📁 css/                    # Stylesheets
│   ├── 📁 js/                     # JavaScript files
│   └── 📁 images/                 # Images e assets
├── 📁 Resources/                   # Localization resources
│   ├── SharedResource.en.json
│   ├── SharedResource.it.json
│   └── ...
└── 📄 Program.cs                   # Application entry point
```

### Architettura Livelli
1. **Presentation Layer**
   - Blazor Server components
   - API Controllers
   - Static resources

2. **Business Layer**
   - Services implementations
   - Business logic
   - Validation rules

3. **Data Access Layer**
   - Entity Framework contexts
   - Database entities
   - Repository pattern

4. **Cross-Cutting Concerns**
   - Authentication & Authorization
   - Logging & Audit
   - Localization
   - Exception handling

## 📦 Prym.DTOs - Data Transfer Objects

### Organizzazione per Feature
```
Prym.DTOs/
├── 📁 Core/                        # DTOs core
│   ├── UserDto.cs
│   ├── TenantDto.cs
│   └── AuthDto.cs
├── 📁 Products/                    # DTOs prodotti
│   ├── ProductDto.cs
│   ├── CategoryDto.cs
│   └── ProductManagementDTOs.cs
├── 📁 Documents/                   # DTOs documenti
│   ├── DocumentHeaderDto.cs
│   ├── DocumentRowDto.cs
│   └── DocumentManagementDTOs.cs
├── 📁 Notifications/               # DTOs notifiche
│   ├── NotificationDto.cs
│   ├── ChatMessageDto.cs
│   └── NotificationManagementDTOs.cs
└── 📁 Common/                      # DTOs comuni
    ├── BaseDto.cs
    ├── PagedResultDto.cs
    └── ValidationDto.cs
```

### Convenzioni DTO
- **Suffisso**: Tutti i DTO terminano con `Dto`
- **Grouping**: DTOs correlati raggruppati per file funzionale
- **Naming**: `{Feature}ManagementDTOs.cs` per gruppi
- **Validation**: Data annotations per validazione

## 🧪 Prym.Tests - Test Structure

### Organizzazione Test
```
Prym.Tests/
├── 📁 Unit/                        # Test unitari
│   ├── 📁 Services/               # Test servizi
│   ├── 📁 Controllers/            # Test controller
│   └── 📁 Entities/               # Test entità
├── 📁 Integration/                 # Test integrazione
│   ├── 📁 API/                    # Test API endpoints
│   ├── 📁 Database/               # Test database
│   └── 📁 Authentication/         # Test autenticazione
├── 📁 RouteAnalysis/              # Test analisi route
│   └── RouteConflictAnalyzer.cs
├── 📁 Fixtures/                    # Test fixtures comuni
│   ├── DatabaseFixture.cs
│   └── AuthenticationFixture.cs
└── 📁 Helpers/                     # Helper per test
    ├── TestHelper.cs
    └── MockHelper.cs
```

### Categorizzazione Test
```csharp
// Test unitari
[Trait("Category", "Unit")]
public class ProductServiceTests { ... }

// Test integrazione
[Trait("Category", "Integration")]
public class ProductsControllerTests { ... }

// Test analisi route
[Trait("Category", "RouteAnalysis")]
public class RouteAnalysisTests { ... }
```

## 📚 docs/ - Documentation Structure

### Organizzazione Documentazione
```
docs/
├── 📄 README.md                    # Indice documentazione
├── 📁 core/                        # Documentazione core
│   ├── README.md                  # Project overview
│   ├── getting-started.md
│   └── project-structure.md
├── 📁 backend/                     # Documentazione backend
│   ├── README.md
│   ├── refactoring-guide.md
│   └── api-development.md
├── 📁 frontend/                    # Documentazione frontend
│   ├── README.md
│   ├── ui-guidelines.md
│   └── theming.md
├── 📁 testing/                     # Documentazione testing
│   ├── README.md
│   ├── route-analysis.md
│   └── 📁 audit/
├── 📁 deployment/                  # Documentazione deployment
│   ├── README.md
│   ├── deployment-guide.md
│   └── licensing.md
├── 📁 features/                    # Guide funzionalità
│   ├── README.md
│   └── notifications-chat.md
└── 📁 migration/                   # Report migrazioni
    ├── README.md
    └── refactoring-summaries.md
```

## 🔧 Tools & Scripts

### Script Disponibili
```
Prym/
├── 🔧 analyze-routes.sh            # Analisi conflitti route (Unix)
├── 🔧 analyze-routes.bat           # Analisi conflitti route (Windows)
└── 📁 audit/                       # Sistema audit
    ├── 🔧 run-audit.sh            # Script audit (Unix)
    ├── 🔧 Prym-Audit.ps1    # Script audit (Windows)
    └── 📄 Program.cs              # Audit tool
```

### File di Configurazione
```
Prym/
├── 📄 Prym.sln               # Solution configuration
├── 📄 Directory.Packages.props     # NuGet package management
├── 📄 .gitignore                   # Git ignore rules
├── 📄 .gitattributes              # Git attributes
└── Prym.Server/
    ├── 📄 appsettings.json        # Application settings
    ├── 📄 appsettings.Development.json
    └── 📄 launchSettings.json     # Launch profiles
```

## 🏛️ Architectural Patterns

### Clean Architecture
Prym segue i principi di Clean Architecture:

1. **Entities** (`Prym.Server/Data/Entities/`)
   - Business entities con regole business core
   - Indipendenti da framework esterni

2. **Use Cases** (`Prym.Server/Services/`)
   - Application business rules
   - Orchestrazione entità

3. **Interface Adapters** (`Prym.Server/Controllers/`)
   - Conversione dati per use cases
   - Presentazione e controllo

4. **Frameworks & Drivers** (External dependencies)
   - Entity Framework (Database)
   - Blazor Server (UI)
   - External APIs

### Dependency Flow
```
Controllers → Services → Entities
     ↓           ↓         ↓
   DTOs     Interfaces  Database
```

### Patterns Utilizzati
- **Repository Pattern**: Per accesso dati
- **Service Layer Pattern**: Per business logic
- **DTO Pattern**: Per trasferimento dati
- **CQRS Pattern**: Per operazioni complesse
- **Unit of Work**: Per transazioni

## 📊 Naming Conventions

### Backend Components
- **Entities**: `Product`, `DocumentHeader`, `User`
- **Services**: `IProductService`, `ProductService`
- **Controllers**: `ProductsController` (plurale)
- **DTOs**: `ProductDto`, `CreateProductDto`

### Frontend Components
- **Pages**: `Products.razor`, `DocumentManagement.razor`
- **Components**: `ProductCard.razor`, `DocumentList.razor`
- **Dialogs**: `CreateProductDialog.razor`

### Database Objects
- **Tables**: `Products`, `DocumentHeaders` (plurale)
- **Columns**: `PascalCase` (`ProductName`, `CreatedAt`)
- **Foreign Keys**: `{Entity}Id` (`ProductId`, `UserId`)

## 🔗 Dependencies & Relationships

### Project Dependencies
```
Prym.Server
├── → Prym.DTOs
└── → External packages (MudBlazor, EF Core, etc.)

Prym.Tests
├── → Prym.Server
├── → Prym.DTOs
└── → Test packages (xUnit, Moq, etc.)
```

### Key External Dependencies
- **MudBlazor**: UI component library
- **Entity Framework Core**: ORM
- **SignalR**: Real-time communication
- **AutoMapper**: Object mapping
- **Serilog**: Logging
- **xUnit**: Testing framework

## 📝 Best Practices

### File Organization
- **Single Responsibility**: Un file per concetto
- **Logical Grouping**: Raggruppa file correlati
- **Consistent Naming**: Convenzioni di naming uniformi
- **Clear Hierarchy**: Struttura directory logica

### Code Organization
- **Separation of Concerns**: Responsabilità separate
- **Dependency Injection**: Dipendenze iniettate
- **Interface Segregation**: Interfacce specifiche
- **Open/Closed Principle**: Estensibile, non modificabile

---

Questa struttura fornisce una base solida per lo sviluppo scalabile e la manutenzione del progetto Prym.