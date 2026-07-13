# Migrations — convenzione del team

> **Nota (2026-07-13) — REVISIONE decisione P05** (`AUDIT_SERVER_ARCHITETTURA_2026-07-13.md`, §4bis):
> tutti gli script `.sql` (inclusi i vari `ROLLBACK_*.sql`) presenti in questa cartella sono
> **storici**: rappresentano lo schema applicato manualmente fino alla migrazione
> `InitialBaseline` (in `EventForge.Server/Migrations/`, gestita da EF Core Migrations) e
> **non vanno più toccati né estesi**.
>
> **Da questo momento in avanti, ogni modifica allo schema del database passa da
> `dotnet ef migrations add`**, non più da script `.sql` scritti a mano.

## Comandi standard

Da eseguire dalla root del repository (o con `--project`/`--startup-project` come sotto), dopo
`dotnet tool restore` (il tool `dotnet-ef` è dichiarato in `EventForge.Server/.config/dotnet-tools.json`):

```bash
# Aggiungere una nuova migrazione dopo aver modificato le entità / EventForgeDbContext
dotnet ef migrations add <NomeMigrazione> --project EventForge.Server --startup-project EventForge.Server

# Rimuovere l'ultima migrazione non ancora applicata (es. per correggerla)
dotnet ef migrations remove --project EventForge.Server --startup-project EventForge.Server

# Elencare le migrazioni e il loro stato (applicata / pendente)
dotnet ef migrations list --project EventForge.Server --startup-project EventForge.Server

# Generare lo script idempotente da revisionare e applicare manualmente in produzione
dotnet ef migrations script --idempotent --project EventForge.Server --startup-project EventForge.Server -o release_<data>.sql
```

## ⚠️ Non usare mai `dotnet ef database update` direttamente contro la produzione

`dotnet ef database update` applica immediatamente le migrazioni pendenti alla connessione
configurata. Contro un ambiente locale/di sviluppo va bene; **contro staging o produzione NON
va mai eseguito direttamente**. La procedura corretta (già raccomandata da Microsoft e già
citata nell'audit originale, §4) è:

1. Generare lo script idempotente: `dotnet ef migrations script --idempotent -o release_<data>.sql`.
2. Revisionare lo script riga per riga (in particolare `ALTER`/`DROP` su tabelle esistenti).
3. Applicarlo manualmente/con approvazione (es. tramite pipeline di deploy controllata o DBA),
   separatamente per ciascun ambiente, esattamente come già avveniva per gli script storici in
   questa cartella.

## Baseline (`InitialBaseline`)

Il database aveva già lo schema completo applicato tramite i 126 script storici quando è stata
introdotta la prima migrazione EF Core. Per evitare che EF Core tentasse di ricreare tabelle già
esistenti, la migrazione `InitialBaseline` è stata generata e poi marcata come "già applicata" in
ciascun ambiente eseguendo manualmente `Migrations/POST_BASELINE_MarkAsApplied.sql` (che inserisce
solo la riga corrispondente in `__EFMigrationsHistory`, senza eseguire la creazione delle tabelle).
Questo script **non va rieseguito** una volta applicato con successo su un ambiente, ed è
mantenuto qui solo come riferimento storico/di riproducibilità.

Nessuna migrazione automatica viene eseguita all'avvio dell'applicazione (`Program.cs` non chiama
`Database.Migrate()`/`EnsureCreated()` in modo incondizionato); l'unico servizio che chiama
`Database.MigrateAsync()` è `BootstrapHostedService`, che lo fa solo se `GetPendingMigrations()`
non è vuoto — condizione che, dopo la marcatura della baseline, non si verifica finché non viene
aggiunta una nuova migrazione reale.
