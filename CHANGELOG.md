# Changelog

Tutte le modifiche importanti a questo progetto saranno documentate in questo file.

Il formato è basato su [Keep a Changelog](https://keepachangelog.com/it/1.0.0/),
e questo progetto aderisce al [Versionamento Semantico](https://semver.org/spec/v2.0.0.html).

## [Non rilasciato]

### Aggiunte
- Architettura a livelli di servizio completa con interfacce e dependency injection
- Integrazione AutoMapper per mapping automatico degli oggetti
- Gestione errori migliorata con ProblemDetails secondo RFC 7231
- Pattern DTO completo per separazione tra entità e API
- Documentazione XML completa e integrazione Swagger migliorata
- Ottimizzazione del database context con configurazione delle relazioni
- Servizio DocumentType con operazioni CRUD complete
- Servizio Reference con gestione completa delle entità di riferimento
- Controller base `BaseApiController` per gestione errori consistente
- Attributi di validazione completi sui DTO Create/Update
- Tracking delle richieste con Correlation ID per debugging
- Configurazione precisa per dati finanziari nel database

### Modificate
- Migrazione di tutti i servizi per utilizzare pattern async/await
- Aggiornamento dei controller per utilizzo consistente di ProblemDetails
- Configurazione dependency injection migliorata in `ServiceCollectionExtensions.cs`
- Mapping delle relazioni tra entità nel `EventForgeDbContext`

### Corrette
- Risolti tutti i warning della documentazione XML
- Eliminazione dell'esposizione diretta delle entità nelle API
- Migliorata la gestione delle eccezioni con dettagli solo in development

## [1.0.0] - Data da definire

### Aggiunte
- Implementazione iniziale dell'API EventForge
- Sistema di gestione eventi con team e audit log
- Funzionalità complete di gestione business
- Database SQLite integrato
- Logging strutturato con Serilog
- Supporto per SQL Server
- Integrazione Dapper per query avanzate

---

## Linee guida per le versioni

- **MAJOR**: Modifiche incompatibili con le API esistenti
- **MINOR**: Funzionalità aggiunte in modo retrocompatibile
- **PATCH**: Correzioni di bug retrocompatibili

## Categorie delle modifiche

- **Aggiunte**: per nuove funzionalità
- **Modificate**: per modifiche a funzionalità esistenti
- **Deprecate**: per funzionalità che saranno rimosse nelle prossime versioni
- **Rimosse**: per funzionalità rimosse in questa versione
- **Corrette**: per correzioni di bug
- **Sicurezza**: in caso di vulnerabilità