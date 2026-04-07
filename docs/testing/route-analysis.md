# Checklist per l'Analisi e Risoluzione Conflitti Route HTTP - Swagger/OpenAPI

## Panoramica
Questa checklist fornisce una procedura dettagliata per identificare e risolvere i conflitti di route HTTP che possono impedire la corretta generazione del file Swagger/OpenAPI nel progetto EventForge.

## 📋 Checklist Analisi Manuale

### Fase 1: Preparazione Ambiente
- [ ] **Verifica Build**: Assicurarsi che il progetto compili correttamente con `dotnet build`
- [ ] **Controllo Swagger**: Verificare che Swagger sia configurato in `Program.cs`
- [ ] **Backup**: Creare backup dei controller prima delle modifiche

### Fase 2: Identificazione Conflitti
- [ ] **Esecuzione Script**: Utilizzare lo script `RouteConflictAnalyzer` per generare il report
- [ ] **Revisione Report**: Analizzare il file di output per identificare:
  - Route duplicate (stesso verbo HTTP + stesso percorso)
  - Pattern di route ambigui
  - Parametri di route conflittuali
- [ ] **Documentazione Conflitti**: Elencare tutti i conflitti trovati con dettagli

### Fase 3: Analisi Dettagliata
Per ogni conflitto identificato:

#### 3.1 Analisi Route Attributes
- [ ] **Verifica `[Route]`**: Controllare l'attributo `[Route("api/v1/[controller]")]` su ogni controller
- [ ] **Verifica HTTP Verbs**: Controllare `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`, `[HttpPatch]`
- [ ] **Parametri Route**: Verificare parametri nelle route (es. `{id:guid}`, `{id:int}`)

#### 3.2 Identificazione Pattern Problematici
- [ ] **Route Identiche**: Due endpoint con stesso verbo HTTP e stesso percorso
- [ ] **Route Ambigue**: Pattern che possono matchare multiple route
- [ ] **Parametri Opzionali**: Controllo dei parametri opzionali che causano ambiguità

### Fase 4: Risoluzione Conflitti

#### 4.1 Strategie di Risoluzione
- [ ] **Rinominare Endpoints**: Modificare i nomi delle action per renderle uniche
- [ ] **Aggiungere Prefissi**: Utilizzare prefissi specifici nelle route (es. `/search`, `/export`)
- [ ] **Parametri Specifici**: Rendere più specifici i vincoli dei parametri di route
- [ ] **Versioning**: Implementare versioning delle API se necessario

#### 4.2 Implementazione Correzioni
- [ ] **Modifica Controller**: Applicare le correzioni identificate
- [ ] **Test Locali**: Verificare che le modifiche non rompano funzionalità esistenti
- [ ] **Verifica Swagger**: Controllare che Swagger generi la documentazione correttamente

### Fase 5: Validazione
- [ ] **Build Success**: Verificare che il progetto compili dopo le modifiche
- [ ] **Test Swagger UI**: Accedere a `/swagger` e verificare che tutti gli endpoint siano visibili
- [ ] **Re-run Script**: Eseguire nuovamente lo script per confermare l'assenza di conflitti
- [ ] **Test Funzionalità**: Testare gli endpoint modificati

## 🔍 Pattern Comuni di Conflitti

### Conflitto Tipo 1: Route Identiche
```csharp
// PROBLEMA: Due endpoint con stesso percorso
[HttpGet("{id}")]
public async Task<ActionResult<UserDto>> GetUser(Guid id) { }

[HttpGet("{id}")]
public async Task<ActionResult<UserDto>> GetUserDetails(Guid id) { }

// SOLUZIONE: Differenziare i percorsi
[HttpGet("{id}")]
public async Task<ActionResult<UserDto>> GetUser(Guid id) { }

[HttpGet("{id}/details")]
public async Task<ActionResult<UserDto>> GetUserDetails(Guid id) { }
```

### Conflitto Tipo 2: Parametri Ambigui
```csharp
// PROBLEMA: Parametri di tipo diverso ma stesso percorso
[HttpGet("{id:guid}")]
public async Task<ActionResult<UserDto>> GetUser(Guid id) { }

[HttpGet("{username}")]
public async Task<ActionResult<UserDto>> GetUserByUsername(string username) { }

// SOLUZIONE: Percorsi più specifici
[HttpGet("{id:guid}")]
public async Task<ActionResult<UserDto>> GetUser(Guid id) { }

[HttpGet("by-username/{username}")]
public async Task<ActionResult<UserDto>> GetUserByUsername(string username) { }
```

### Conflitto Tipo 3: Route Opzionali
```csharp
// PROBLEMA: Parametri opzionali che creano ambiguità
[HttpGet]
public async Task<ActionResult<PagedResult<UserDto>>> GetUsers() { }

[HttpGet("{filter?}")]
public async Task<ActionResult<PagedResult<UserDto>>> GetUsersWithFilter(string filter) { }

// SOLUZIONE: Route esplicite
[HttpGet]
public async Task<ActionResult<PagedResult<UserDto>>> GetUsers() { }

[HttpGet("filter/{filter}")]
public async Task<ActionResult<PagedResult<UserDto>>> GetUsersWithFilter(string filter) { }
```

## 📝 Template Report Manuale

```
REPORT CONFLITTI ROUTE - EventForge
Data: [DATA]
Analizzato da: [NOME]

CONFLITTI IDENTIFICATI:
1. Controller: [NomeController]
   - Endpoint 1: [HTTP_VERB] [ROUTE_PATTERN]
   - Endpoint 2: [HTTP_VERB] [ROUTE_PATTERN]
   - Tipo Conflitto: [Route Identiche/Parametri Ambigui/Altri]
   - Priorità: [Alta/Media/Bassa]

SOLUZIONI PROPOSTE:
1. [Descrizione soluzione per conflitto 1]

IMPLEMENTAZIONE:
- [ ] Modifiche applicate
- [ ] Test completati
- [ ] Swagger verificato
```

## 🚨 Segnali di Allarme

### Durante lo Sviluppo
- ⚠️ **Swagger UI** non mostra tutti gli endpoint
- ⚠️ **Errori di compilazione** relativi a route ambigue
- ⚠️ **404 Not Found** su endpoint che dovrebbero esistere
- ⚠️ **Endpoint non raggiungibili** tramite API calls

### Durante il Build
- ⚠️ **Warning del compilatore** su route duplicate
- ⚠️ **Errori di routing** nel log dell'applicazione
- ⚠️ **OpenAPI document generation failed**

## 📚 Risorse Aggiuntive

### Documentazione Microsoft
- [ASP.NET Core Routing](https://docs.microsoft.com/aspnet/core/fundamentals/routing)
- [API Controller Routing](https://docs.microsoft.com/aspnet/core/web-api/controller-action)
- [Swagger/OpenAPI Configuration](https://docs.microsoft.com/aspnet/core/tutorials/web-api-help-pages-using-swagger)

### Best Practices
- Utilizzare sempre vincoli di tipo sui parametri route (`{id:guid}`, `{id:int}`)
- Preferire route esplicite a parametri opzionali
- Mantenere convenzioni di naming consistenti
- Documentare sempre gli endpoint con XML comments