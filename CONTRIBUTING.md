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