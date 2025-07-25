# Guida ai Contributi - EventForge

Benvenuto nel progetto EventForge! Siamo entusiasti del tuo interesse nel contribuire al nostro sistema di gestione eventi. Questa guida ti aiuterà a iniziare e a contribuire efficacemente al progetto.

## 📋 Indice

- [Come Iniziare](#come-iniziare)
- [Tipi di Contributi](#tipi-di-contributi)
- [Processo di Contribuzione](#processo-di-contribuzione)
- [Standard di Codifica](#standard-di-codifica)
- [Testing](#testing)
- [Documentazione](#documentazione)
- [Sicurezza](#sicurezza)
- [Community Guidelines](#community-guidelines)
- [Contatti](#contatti)

## 🚀 Come Iniziare

### Prerequisiti

Prima di contribuire, assicurati di avere installato:

- **.NET 8.0 SDK** o superiore
- **Git** per version control
- **Visual Studio 2022** o **VS Code** con estensioni C#
- **SQL Server** (opzionale, SQLite incluso per sviluppo)

### Setup Ambiente di Sviluppo

1. **Fork del Repository**
   ```bash
   # Clona il tuo fork
   git clone https://github.com/TUO-USERNAME/EventForge.git
   cd EventForge
   
   # Aggiungi upstream remote
   git remote add upstream https://github.com/ivanopaulon/EventForge.git
   ```

2. **Configurazione Locale**
   ```bash
   # Restore dipendenze
   dotnet restore
   
   # Build del progetto
   dotnet build
   
   # Run dei test (quando disponibili)
   dotnet test
   
   # Avvio applicazione
   dotnet run
   ```

3. **Verifica Setup**
   - Apri browser a `https://localhost:5001`
   - Verifica che Swagger UI sia accessibile
   - Testa alcune API di base

## 🎯 Tipi di Contributi

Accogliamo diversi tipi di contributi:

### 🐛 Bug Reports
- Usa i template GitHub Issues
- Includi passi per riprodurre il bug
- Fornisci informazioni ambiente (OS, .NET version, browser)
- Allega screenshot o log quando possibile

### ✨ Feature Requests
- Descrivi il problema che la feature risolverebbe
- Proponi una soluzione dettagliata
- Considera impatti su performance e sicurezza
- Verifica che non sia già in roadmap

### 🔧 Code Contributions
- Bug fixes
- Nuove funzionalità
- Miglioramenti performance
- Refactoring e ottimizzazioni

### 📚 Documentazione
- Miglioramenti README
- Guide utente
- Documentazione API
- Esempi di codice

### 🧪 Testing
- Unit tests
- Integration tests
- End-to-end tests
- Performance tests

## 🔄 Processo di Contribuzione

### 1. Pianificazione

#### Prima di Iniziare
- **Verifica Issues Esistenti**: Controlla se il problema è già stato segnalato
- **Discuti Feature Grandi**: Apri una discussion per feature significative
- **Assegnazione Issues**: Commenta l'issue per richiedere assegnazione

#### Branch Strategy
```bash
# Crea branch dal main aggiornato
git checkout main
git pull upstream main
git checkout -b tipo/descrizione-breve

# Esempi di nomi branch
git checkout -b feature/team-member-roles
git checkout -b bugfix/audit-log-pagination
git checkout -b docs/api-examples
git checkout -b refactor/service-layer-optimization
```

### 2. Sviluppo

#### Processo di Codifica
1. **Segui Standard di Codifica** (vedi sezione dedicata)
2. **Scrivi Test** per nuove funzionalità
3. **Aggiorna Documentazione** se necessario
4. **Testa Localmente** prima di push

#### Commit Guidelines
```bash
# Formato commit message
tipo(scope): descrizione breve

# Esempi
feat(teams): aggiungi supporto ruoli personalizzati
fix(audit): correggi paginazione log entità
docs(api): aggiorna esempi Swagger
refactor(services): ottimizza dependency injection
test(integration): aggiungi test controller teams
```

**Tipi di commit:**
- `feat`: Nuova funzionalità
- `fix`: Bug fix
- `docs`: Solo documentazione
- `style`: Formatting, mancano semicolon, etc
- `refactor`: Refactoring codice
- `test`: Aggiunta test
- `chore`: Maintenance tasks

### 3. Pull Request

#### Prima di Aprire PR
```bash
# Sincronizza con upstream
git fetch upstream
git rebase upstream/main

# Verifica build e test
dotnet build
dotnet test

# Push del branch
git push origin nome-branch
```

#### Template Pull Request
```markdown
## Descrizione
Breve descrizione delle modifiche

## Tipo di Modifica
- [ ] Bug fix
- [ ] Nuova funzionalità
- [ ] Breaking change
- [ ] Documentazione

## Testing
- [ ] Test esistenti passano
- [ ] Nuovi test aggiunti
- [ ] Test manuali eseguiti

## Checklist
- [ ] Codice segue style guidelines
- [ ] Self-review eseguita
- [ ] Documentazione aggiornata
- [ ] CHANGELOG.md aggiornato (se necessario)
```

## 📝 Standard di Codifica

### C# Coding Standards

#### Naming Conventions
```csharp
// Classi, Interfacce, Metodi: PascalCase
public class TeamService : ITeamService
public async Task<TeamDto> GetTeamAsync(Guid id)

// Variabili locali, parametri: camelCase
var teamMember = await _repository.GetByIdAsync(memberId);
public async Task UpdateAsync(Guid id, UpdateTeamDto updateDto)

// Costanti: PascalCase
public const int MaxTeamMembers = 50;

// Campi privati: _camelCase
private readonly ITeamRepository _teamRepository;
```

#### File Organization
```csharp
// 1. Using statements (gruppi separati da riga vuota)
using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using EventForge.DTOs.Teams;
using EventForge.Services.Teams;

// 2. Namespace
namespace EventForge.Controllers;

// 3. Class documentation
/// <summary>
/// Controller per gestione team e membri.
/// </summary>
public class TeamsController : BaseApiController
{
    // 4. Fields
    private readonly ITeamService _teamService;
    
    // 5. Constructor
    public TeamsController(ITeamService teamService)
    {
        _teamService = teamService ?? throw new ArgumentNullException(nameof(teamService));
    }
    
    // 6. Public methods
    // 7. Private methods
}
```

#### Error Handling
```csharp
// Usa ProblemDetails per error responses
[HttpPost]
public async Task<ActionResult<TeamDto>> CreateTeam(
    [FromBody] CreateTeamDto createDto,
    CancellationToken cancellationToken = default)
{
    if (!ModelState.IsValid)
    {
        return CreateValidationProblemDetails();
    }

    try
    {
        var team = await _teamService.CreateAsync(createDto, cancellationToken);
        return CreatedAtAction(nameof(GetTeam), new { id = team.Id }, team);
    }
    catch (ValidationException ex)
    {
        return CreateValidationProblem(ex.Message);
    }
    catch (Exception ex)
    {
        return CreateInternalServerErrorProblem("Errore durante creazione team", ex);
    }
}
```

#### Async/Await Best Practices
```csharp
// ✅ Corretto
public async Task<TeamDto> GetTeamAsync(Guid id, CancellationToken cancellationToken = default)
{
    var team = await _repository.GetByIdAsync(id, cancellationToken);
    return _mapper.Map<TeamDto>(team);
}

// ❌ Evitare async void (eccetto event handlers)
public async void ProcessTeam() { } // NO

// ✅ ConfigureAwait in libraries
var result = await _httpClient.GetAsync(url).ConfigureAwait(false);
```

### Database e Entity Framework

#### Entity Configuration
```csharp
// Usa Fluent API per configurazioni complesse
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Team>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        
        entity.HasMany(e => e.Members)
              .WithOne(e => e.Team)
              .HasForeignKey(e => e.TeamId)
              .OnDelete(DeleteBehavior.Cascade);
    });
}
```

#### Service Layer Patterns
```csharp
public class TeamService : ITeamService
{
    private readonly EventForgeDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<TeamService> _logger;

    // Usa AsNoTracking per read-only queries
    public async Task<IEnumerable<TeamDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var teams = await _context.Teams
            .AsNoTracking()
            .Include(t => t.Members)
            .ToListAsync(cancellationToken);
            
        return _mapper.Map<IEnumerable<TeamDto>>(teams);
    }
    
    // Usa tracking per modifiche
    public async Task<TeamDto> UpdateAsync(Guid id, UpdateTeamDto updateDto, CancellationToken cancellationToken = default)
    {
        var team = await _context.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
            
        if (team == null)
            throw new NotFoundException($"Team {id} non trovato");
            
        _mapper.Map(updateDto, team);
        await _context.SaveChangesAsync(cancellationToken);
        
        return _mapper.Map<TeamDto>(team);
    }
}
```

### API Design Guidelines

#### Controller Best Practices
```csharp
[Route("api/v1/[controller]")]
[ApiController]
public class TeamsController : BaseApiController
{
    /// <summary>
    /// Ottiene team con paginazione.
    /// </summary>
    /// <param name="page">Numero pagina (base 1)</param>
    /// <param name="pageSize">Elementi per pagina (max 100)</param>
    /// <param name="cancellationToken">Token cancellazione</param>
    /// <returns>Lista paginata di team</returns>
    /// <response code="200">Restituisce la lista paginata</response>
    /// <response code="400">Parametri query non validi</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TeamDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<TeamDto>>> GetTeams(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }
}
```

#### DTO Design
```csharp
/// <summary>
/// DTO per creazione team.
/// </summary>
public class CreateTeamDto
{
    /// <summary>
    /// Nome del team.
    /// </summary>
    [Required(ErrorMessage = "Il nome è obbligatorio")]
    [StringLength(200, ErrorMessage = "Il nome non può superare 200 caratteri")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descrizione opzionale del team.
    /// </summary>
    [StringLength(1000, ErrorMessage = "La descrizione non può superare 1000 caratteri")]
    public string? Description { get; set; }

    /// <summary>
    /// ID dell'evento associato.
    /// </summary>
    [Required(ErrorMessage = "L'ID evento è obbligatorio")]
    public Guid EventId { get; set; }
}
```

## 🧪 Testing

### Test Strategy

#### Unit Tests
```csharp
[Fact]
public async Task GetTeamAsync_WithValidId_ReturnsTeamDto()
{
    // Arrange
    var teamId = Guid.NewGuid();
    var expectedTeam = new Team { Id = teamId, Name = "Test Team" };
    _mockRepository.Setup(r => r.GetByIdAsync(teamId, default))
               .ReturnsAsync(expectedTeam);

    // Act
    var result = await _teamService.GetTeamAsync(teamId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(teamId, result.Id);
    Assert.Equal("Test Team", result.Name);
}
```

#### Integration Tests
```csharp
public class TeamsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetTeams_ReturnsSuccessWithTeams()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/teams");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("teams", content);
    }
}
```

#### Test Naming Convention
```csharp
// Pattern: MethodName_Scenario_ExpectedResult
public async Task CreateTeam_WithValidData_ReturnsCreatedTeam()
public async Task CreateTeam_WithInvalidData_ReturnsBadRequest()
public async Task GetTeam_WithNonExistentId_ReturnsNotFound()
```

### Test Requirements

#### Per Nuove Funzionalità
- **Unit tests** per service layer
- **Integration tests** per controller endpoints
- **Test coverage** > 80% per nuovi componenti
- **Edge cases** e error scenarios

#### Test Data
```csharp
// Usa builder pattern per test data
public class TeamTestDataBuilder
{
    private Team _team = new Team();
    
    public TeamTestDataBuilder WithName(string name)
    {
        _team.Name = name;
        return this;
    }
    
    public TeamTestDataBuilder WithMembers(int count)
    {
        _team.Members = Enumerable.Range(1, count)
            .Select(i => new TeamMember { Name = $"Member {i}" })
            .ToList();
        return this;
    }
    
    public Team Build() => _team;
}

// Utilizzo
var team = new TeamTestDataBuilder()
    .WithName("Development Team")
    .WithMembers(5)
    .Build();
```

## 📚 Documentazione

### API Documentation

#### XML Comments
```csharp
/// <summary>
/// Servizio per gestione team e membri del team.
/// Fornisce operazioni CRUD complete con supporto audit.
/// </summary>
public interface ITeamService
{
    /// <summary>
    /// Ottiene un team per ID con i suoi membri.
    /// </summary>
    /// <param name="id">ID univoco del team</param>
    /// <param name="cancellationToken">Token per cancellazione operazione</param>
    /// <returns>
    /// Il team corrispondente all'ID fornito, o null se non trovato.
    /// Include automaticamente la lista dei membri del team.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Lanciata quando l'ID fornito non è un GUID valido
    /// </exception>
    Task<TeamDto?> GetTeamAsync(Guid id, CancellationToken cancellationToken = default);
}
```

#### README Updates
Quando aggiungi nuove funzionalità, aggiorna:
- Sezione caratteristiche principali
- Esempi di utilizzo API
- Configurazione se necessaria
- Troubleshooting per problemi comuni

#### Code Examples
```csharp
// Sempre includi esempi completi e funzionanti
var client = new HttpClient();
var response = await client.PostAsJsonAsync("/api/v1/teams", new CreateTeamDto
{
    Name = "Marketing Team",
    Description = "Team per campagne marketing",
    EventId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000")
});

if (response.IsSuccessStatusCode)
{
    var team = await response.Content.ReadFromJsonAsync<TeamDto>();
    Console.WriteLine($"Team creato: {team.Name}");
}
```

## 🔒 Sicurezza

### Security Best Practices

#### Input Validation
```csharp
// Valida sempre input utente
[HttpPost]
public async Task<ActionResult<TeamDto>> CreateTeam([FromBody] CreateTeamDto dto)
{
    if (!ModelState.IsValid)
        return CreateValidationProblemDetails();
    
    // Validazione business logic
    if (await _teamService.TeamNameExistsAsync(dto.Name))
        return CreateValidationProblem("Nome team già esistente");
    
    // Sanitizza input se necessario
    dto.Name = dto.Name.Trim();
    
    // Processa richiesta
}
```

#### SQL Injection Prevention
```csharp
// ✅ Usa sempre parametri EF Core
var teams = await _context.Teams
    .Where(t => t.Name.Contains(searchTerm))
    .ToListAsync();

// ❌ MAI concatenare stringhe SQL
// var sql = $"SELECT * FROM Teams WHERE Name LIKE '%{searchTerm}%'"; // VULNERABILE
```

#### Error Information Disclosure
```csharp
// ✅ Non esporre dettagli interni in produzione
catch (Exception ex)
{
    _logger.LogError(ex, "Errore durante creazione team");
    
    if (_environment.IsDevelopment())
    {
        return CreateInternalServerErrorProblem("Errore interno", ex);
    }
    else
    {
        return CreateInternalServerErrorProblem("Si è verificato un errore interno");
    }
}
```

### Segnalazione Vulnerabilità

#### Processo Security Issues
1. **NON aprire issue pubblici** per vulnerabilità
2. **Invia email** a: `security@eventforge.com`
3. **Includi dettagli**:
   - Descrizione vulnerabilità
   - Passi per riprodurre
   - Impatto potenziale
   - Versioni affette

#### Response Time
- **Acknowledgment**: 24 ore
- **Initial assessment**: 72 ore
- **Status updates**: Settimanali
- **Resolution target**: 30 giorni per vulnerabilità critiche

## 👥 Community Guidelines

### Code of Conduct

#### Comportamenti Incoraggiati
- ✅ Linguaggio rispettoso e inclusivo
- ✅ Feedback costruttivo e specifico
- ✅ Aiutare altri contributori
- ✅ Essere aperti a critiche e suggerimenti
- ✅ Focalizzarsi sul beneficio del progetto

#### Comportamenti Non Accettabili
- ❌ Linguaggio offensivo o discriminatorio
- ❌ Attacchi personali o trolling
- ❌ Spam o self-promotion inappropriata
- ❌ Pubblicazione informazioni private senza consenso
- ❌ Comportamenti non professionali

### Communication Guidelines

#### GitHub Issues
- **Usa template** forniti per bug reports e feature requests
- **Cerca duplicati** prima di aprire nuovo issue
- **Fornisci contesto** sufficiente per comprendere il problema
- **Mantieni focus** su un problema per issue

#### Pull Request Reviews
- **Review costruttive** con suggerimenti specifici
- **Approva rapidamente** PR pronte
- **Richiedi modifiche** con spiegazioni chiare
- **Ringrazia** i contributori per il loro lavoro

#### Discussions
- **Usa Discussions** per domande generali e brainstorming
- **Cerca thread esistenti** prima di crearne di nuovi
- **Partecipa attivamente** alle discussioni della community
- **Condividi conoscenza** ed esperienze

## 📞 Contatti

### Team di Sviluppo

#### Maintainers
- **Lead Developer**: [@ivanopaulon](https://github.com/ivanopaulon)
  - Architettura generale e direzione progetto
  - Review PR complesse e breaking changes

#### Core Contributors
- Consulta la [lista contributors](https://github.com/ivanopaulon/EventForge/graphs/contributors)
- Partecipa alle [discussions](https://github.com/ivanopaulon/EventForge/discussions)

### Canali di Supporto

#### Per Sviluppatori
- **Issues**: Problemi tecnici e bug reports
- **Discussions**: Domande generali e aiuto sviluppo
- **Email**: tech@eventforge.com

#### Per Contributors
- **Mentorship**: Per nuovi contributors, richiedici supporto nelle issues
- **Architecture discussions**: Per modifiche significative all'architettura
- **Code reviews**: Feedback costruttivi su tutte le PR

### Community

#### Eventi e Meetups
- **Virtual meetups**: Trimestrali per discussioni roadmap
- **Code reviews sessions**: Sessioni collaborative di review
- **Learning sessions**: Workshop su nuove tecnologie implementate

#### Recognition
- **Contributor of the month**: Riconoscimento contributori attivi
- **Achievement badges**: Per milestone importanti
- **Credits**: Tutti i contributori sono elencati nel README

---

## 🎉 Conclusione

Grazie per il tuo interesse nel contribuire a EventForge! La tua partecipazione aiuta a migliorare un sistema utilizzato da sviluppatori e organizzatori di eventi.

### Primi Passi Raccomandati
1. **Leggi** questa guida completamente
2. **Configura** ambiente di sviluppo locale
3. **Esplora** il codice e la documentazione API
4. **Inizia** con una issue etichettata "good first issue"
5. **Chiedi** se hai dubbi o domande

### Ricorda
- **Qualità** prima della quantità
- **Comunica** spesso e chiaramente
- **Testa** accuratamente le tue modifiche
- **Divertiti** e impara nel processo!

Benvenuto nella community EventForge! 🚀
=======
# Contribuire a EventForge

Grazie per il tuo interesse nel contribuire a EventForge! Questo documento fornisce le linee guida per contribuire efficacemente al progetto.

## Come Contribuire

### 1. Fork e Setup

```bash
# Fork del repository su GitHub, poi clona il tuo fork
git clone https://github.com/TUO_USERNAME/EventForge.git
cd EventForge

# Aggiungi il repository originale come remote
git remote add upstream https://github.com/ivanopaulon/EventForge.git
```

### 2. Preparazione dell'Ambiente

```bash
# Verifica che .NET 8 sia installato
dotnet --version

# Ripristina le dipendenze
dotnet restore

# Compila il progetto
dotnet build

# Esegui i test (se presenti)
dotnet test
```

### 3. Workflow di Sviluppo

#### Creazione Branch
```bash
# Sincronizza con il repository upstream
git fetch upstream
git checkout main
git merge upstream/main

# Crea un branch per la tua feature/fix
git checkout -b feature/nome-della-feature
# oppure
git checkout -b fix/nome-del-bug
```

#### Sviluppo
- Fai commit piccoli e frequenti con messaggi chiari
- Segui le convenzioni di codifica del progetto
- Scrivi test per le nuove funzionalità
- Aggiorna la documentazione se necessario

#### Pull Request
1. Push del tuo branch: `git push origin nome-del-branch`
2. Apri una Pull Request su GitHub
3. Compila il template della PR con tutte le informazioni richieste
4. Attendi la revisione e applica i feedback ricevuti

## Standard di Codifica

### Convenzioni C#
- Utilizza le convenzioni di denominazione .NET standard
- Segui i principi SOLID e DDD (Domain-Driven Design)
- Implementa pattern async/await per operazioni I/O
- Usa dependency injection per la gestione delle dipendenze

### Architettura
- **Controller**: Solo logica di controllo e validazione input
- **Service**: Logica business e orchestrazione
- **Repository**: Accesso ai dati (se implementato)
- **DTO**: Oggetti per trasferimento dati, mai esporre entità direttamente

### Esempio di Servizio
```csharp
public interface IExampleService
{
    Task<IEnumerable<ExampleDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ExampleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ExampleDto> CreateAsync(CreateExampleDto createDto, CancellationToken cancellationToken = default);
    Task<ExampleDto?> UpdateAsync(Guid id, UpdateExampleDto updateDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
```

### Gestione Errori
- Usa `ProblemDetails` per tutte le risposte di errore
- Implementa validation con attributi sui DTO
- Gestisci le eccezioni nei controller usando il `BaseApiController`

## Messaggi di Commit

Usa il formato Conventional Commits:

```
tipo(scope): descrizione breve

Descrizione più dettagliata se necessaria.

Fixes #123
```

### Tipi di Commit
- `feat`: Nuova funzionalità
- `fix`: Correzione bug
- `docs`: Solo modifiche alla documentazione
- `style`: Modifiche che non influenzano il significato del codice
- `refactor`: Refactoring del codice
- `test`: Aggiunta o modifica test
- `chore`: Modifiche agli strumenti di build, dipendenze, ecc.

### Esempi
```
feat(api): aggiungi endpoint per gestione eventi
fix(auth): correggi validazione token JWT
docs(readme): aggiorna istruzioni di installazione
refactor(services): migra a pattern async/await
```

## Testing

### Test Unitari
- Scrivi test per tutti i servizi e controller
- Usa xUnit come framework di test
- Moq per il mocking delle dipendenze
- FluentAssertions per assertion più leggibili

### Test di Integrazione
- Testa gli endpoint API usando `TestServer`
- Usa database in-memory o test database
- Verifica la validazione dei DTO

### Struttura Test
```
Tests/
├── Unit/
│   ├── Services/
│   ├── Controllers/
│   └── Common/
├── Integration/
│   ├── Controllers/
│   └── Common/
└── TestUtilities/
```

## Sicurezza

### Linee Guida di Sicurezza
- **Mai committare credenziali o chiavi API**
- Usa `dotnet user-secrets` per sviluppo locale
- Valida sempre input utente nei DTO
- Usa Entity Framework per prevenire SQL injection
- Implementa logging appropriato senza esporre dati sensibili

### Segnalazione Vulnerabilità
Per segnalare vulnerabilità di sicurezza:
1. **NON** aprire issue pubbliche
2. Invia email a: [inserire email sicurezza]
3. Include dettagli sulla vulnerabilità
4. Attendi conferma prima di divulgare pubblicamente

### Pratiche di Sicurezza nel Codice
```csharp
// ✅ GIUSTO: Validazione input
[Required]
[StringLength(100, MinimumLength = 1)]
public string Name { get; set; } = string.Empty;

// ✅ GIUSTO: Gestione password
services.AddAuthentication()
    .AddJwtBearer(options => { /* configurazione sicura */ });

// ❌ SBAGLIATO: Logging di dati sensibili
_logger.LogInformation("User password: {password}", password);
```

## Documentazione

### Documentazione Codice
- Usa commenti XML per API pubbliche
- Documenta parametri e valori di ritorno
- Include esempi per API complesse

### Swagger/OpenAPI
- Aggiungi attributi `ProducesResponseType`
- Documenta tutti i possibili codici di risposta
- Include esempi nelle response

## Revisione del Codice

### Checklist per PR
- [ ] Codice compila senza warning
- [ ] Test passano tutti
- [ ] Codice segue le convenzioni del progetto
- [ ] Documentazione aggiornata
- [ ] Changelog aggiornato per modifiche significative
- [ ] Nessuna informazione sensibile nel codice

### Processo di Revisione
1. Automated checks devono passare
2. Almeno un maintainer approva la PR
3. Tutti i commenti di revisione sono risolti
4. Branch è aggiornato con main

## Supporto

### Dove Chiedere Aiuto
- **Issues**: Per bug report e feature request
- **Discussions**: Per domande generali e discussioni
- **Wiki**: Per documentazione estesa

### Template Issue
Usa i template forniti per:
- Bug Report
- Feature Request
- Domande

## Riconoscimenti

Tutti i contributor saranno riconosciuti nel file CONTRIBUTORS.md.

## Licenza

Contribuendo a EventForge, accetti che i tuoi contributi saranno licenziati sotto la stessa licenza del progetto.
