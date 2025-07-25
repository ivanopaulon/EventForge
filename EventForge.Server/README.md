# EventForge

Sistema completo di gestione eventi con funzionalità avanzate di business management, team, audit e monitoraggio.

## 🚀 Caratteristiche Principali

EventForge è una piattaforma completa per la gestione di eventi costruita con tecnologie moderne .NET 8, che offre:

### 🎯 Gestione Eventi e Team
- **Gestione Eventi**: Sistema completo per la creazione, modifica e monitoraggio di eventi
- **Team Management**: Organizzazione di team con membri, ruoli e permessi
- **Collaborazione**: Strumenti per la collaborazione tra team e gestione delle responsabilità

### 🔐 Autenticazione e Autorizzazione
- **Sistema Utenti**: Gestione completa di utenti con gruppi e privilegi
- **Controllo Accessi**: Autorizzazione granulare basata su ruoli e permessi
- **Store Users**: Sistema di utenti dedicato per operazioni di store

### 📋 Audit e Tracciamento
- **Audit Log Completo**: Tracciamento di tutte le modifiche alle entità
- **Correlation ID**: Tracciamento delle richieste per debugging e monitoraggio
- **Change Tracking**: Sistema avanzato di tracking delle modifiche con valori precedenti/nuovi
- **Query Avanzate**: Filtri per tipo di entità, utente, range di date

### 🏢 Gestione Business
- **Gestione Prodotti**: Catalogo prodotti con classificazioni e unità di misura
- **Listini Prezzi**: Sistema completo di gestione prezzi e listini
- **Promozioni**: Regole promozionali e gestione sconti
- **Contatti**: Gestione completa di contatti business
- **Termini di Pagamento**: Configurazione metodi e termini di pagamento

### 🏪 Warehouse Management
- **Strutture di Stoccaggio**: Gestione facility e ubicazioni
- **Inventario**: Tracciamento prodotti e movimenti
- **Documenti**: Sistema documentale per header, righe e collegamenti

### 📊 Monitoraggio e Performance
- **Station Monitor**: Monitoraggio in tempo reale delle stazioni
- **Health Checks**: Controlli di stato dell'applicazione e dipendenze
- **Logging Strutturato**: Logging avanzato con Serilog
- **Performance Tracking**: Monitoraggio performance e identificazione colli di bottiglia

### 🔧 Gestione Errori Avanzata
- **ProblemDetails**: Gestione errori standardizzata secondo RFC 7807
- **Middleware Personalizzato**: Gestione centralizzata di eccezioni e correlazione
- **Validation**: Validazione completa con messaggi di errore chiari
- **Environment-aware**: Dettagli errori configurabili per ambiente

## 🛠️ Tecnologie e Architettura

### Stack Tecnologico
- **.NET 8.0**: Framework principale con ASP.NET Core
- **Entity Framework Core**: ORM con supporto SQLite e SQL Server
- **AutoMapper**: Mapping automatico tra entità e DTO
- **Swagger/OpenAPI**: Documentazione API completa
- **Serilog**: Logging strutturato e avanzato
- **Dapper**: Query ad alte performance quando necessario

### Pattern Architetturali
- **Service Layer Pattern**: Separazione logica con interfacce ben definite
- **DTO Pattern**: Separazione completa tra entità di dominio e trasferimento dati
- **Dependency Injection**: Configurazione DI completa e testabile
- **Repository Pattern**: Astrazione accesso dati (dove applicabile)
- **Middleware Pipeline**: Pipeline personalizzata per cross-cutting concerns

### Principi di Design
- **Domain-Driven Design**: Architettura orientata al dominio
- **RESTful API**: API REST standard con best practices
- **Clean Architecture**: Separazione responsabilità e dipendenze
- **SOLID Principles**: Codice mantenibile e estensibile

## 📦 Installazione

### Prerequisiti
- .NET 8.0 SDK
- SQL Server (opzionale, SQLite incluso)
- Git

### Setup Locale

1. **Clone del repository**
```bash
git clone https://github.com/ivanopaulon/EventForge.git
cd EventForge/EventForge.Server
```

2. **Restore delle dipendenze**
```bash
dotnet restore
```

3. **Configurazione database**
```bash
# Applicazione migrazioni (opzionale, viene fatto automaticamente)
dotnet ef database update
```

4. **Avvio dell'applicazione**
```bash
dotnet run
```

5. **Accesso alla documentazione API**
   - Swagger UI: `https://localhost:5001` (porta predefinita)
   - API Base URL: `https://localhost:5001/api/v1`

### Configurazione

Modifica `appsettings.json` per configurare:
- **Connection String**: Database SQLite o SQL Server
- **Logging**: Livelli e destinazioni log
- **CORS**: Configurazione domini consentiti
- **Authentication**: Provider e configurazioni auth

## 🏗️ Struttura del Progetto

```
EventForge.Server/
├── Controllers/           # Controller API REST
├── Services/             # Service layer con business logic
│   ├── Audit/           # Servizi audit e tracking
│   ├── Teams/           # Gestione team e membri
│   ├── Store/           # Gestione store users
│   ├── Products/        # Gestione prodotti
│   └── ...
├── DTOs/                # Data Transfer Objects
├── Data/                # Entity Framework e entità
│   ├── Entities/        # Entità di dominio
│   └── EventForgeDbContext.cs
├── Middleware/          # Middleware personalizzati
├── Extensions/          # Extension methods e configurazioni
├── Filters/             # Action filters
├── Mappings/            # AutoMapper profiles
└── Migrations/          # Migrazioni database
```

## 📚 Documentazione API

### Swagger/OpenAPI
La documentazione completa delle API è disponibile tramite Swagger UI all'indirizzo principale dell'applicazione.

### Esempi di Utilizzo

#### Gestione Team
```http
# Ottenere tutti i team
GET /api/v1/teams?page=1&pageSize=20

# Creare un nuovo team
POST /api/v1/teams
Content-Type: application/json

{
  "name": "Team Sviluppo",
  "description": "Team di sviluppo principale",
  "eventId": "guid-evento"
}
```

#### Audit Logs
```http
# Ottenere log di audit per entità
GET /api/v1/audit/entity/{entityId}

# Ricerca log con filtri
GET /api/v1/audit/search?entityType=Team&fromDate=2024-01-01
```

### Schema di Response
Tutte le API utilizzano il formato ProblemDetails per gli errori:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Errore di validazione",
  "status": 400,
  "detail": "I dati forniti non sono validi",
  "instance": "/api/v1/teams",
  "correlationId": "12345678-1234-1234-1234-123456789012",
  "timestamp": "2024-01-01T12:00:00Z",
  "errors": {
    "Name": ["Il campo Nome è obbligatorio"]
  }
}
```

## 🤝 Contributi

Consulta [CONTRIBUTING.md](CONTRIBUTING.md) per le linee guida complete su come contribuire al progetto.

### Quick Start per Contribuitori
1. Fork del repository
2. Crea un branch feature: `git checkout -b feature/nome-funzionalita`
3. Commit delle modifiche: `git commit -am 'Aggiunta nuova funzionalità'`
4. Push del branch: `git push origin feature/nome-funzionalita`
5. Apri una Pull Request

## 🔒 Sicurezza

### Best Practices Implementate
- **Input Validation**: Validazione completa su tutti i DTO
- **SQL Injection Prevention**: Utilizzo esclusivo di Entity Framework
- **Error Information Disclosure**: Dettagli errori limitati in produzione
- **Correlation Tracking**: Tracciamento richieste per audit security

### Segnalazione Vulnerabilità
Per segnalare vulnerabilità di sicurezza, invia una email a: `security@eventforge.com`

## 📈 Performance e Scalabilità

### Ottimizzazioni Implementate
- **Async/Await**: Tutti i servizi utilizzano pattern asincroni
- **Pagination**: Impaginazione su tutte le liste
- **Query Optimization**: Query EF ottimizzate con `AsNoTracking()` quando appropriato
- **Caching Ready**: Struttura preparata per implementazione cache

### Monitoring
- **Health Checks**: Endpoint per verifica stato applicazione
- **Structured Logging**: Log strutturati per analisi performance
- **Correlation IDs**: Tracciamento completo delle richieste

## 📄 Changelog

Consulta [CHANGELOG.md](CHANGELOG.md) per la cronologia completa delle versioni e delle modifiche.

## 🙏 Credits

### Team di Sviluppo
- **Lead Developer**: ivanopaulon
- **Architecture**: Basata su .NET best practices e Domain-Driven Design

### Tecnologie Open Source
- ASP.NET Core Team per il framework eccellente
- Entity Framework Team per l'ORM robusto
- AutoMapper contributors per il mapping sempificato
- Serilog team per il logging avanzato

## 📞 Supporto

### Canali di Supporto
- **Issues**: [GitHub Issues](https://github.com/ivanopaulon/EventForge/issues)
- **Discussions**: [GitHub Discussions](https://github.com/ivanopaulon/EventForge/discussions)
- **Email**: support@eventforge.com

### Documentazione Aggiuntiva
- **Wiki**: Documentazione tecnica dettagliata
- **API Reference**: Documentazione Swagger integrata
- **Architecture Docs**: Documenti di architettura e best practices

---

## 📋 License

Questo progetto è rilasciato sotto licenza MIT. Consulta il file `LICENSE` per i dettagli completi.

---

**EventForge** - Sistema completo di gestione eventi con architettura enterprise-ready 🚀