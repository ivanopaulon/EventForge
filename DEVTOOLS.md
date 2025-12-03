# EventForge - DevTools: Strumenti di Sviluppo

## Panoramica

EventForge include una suite di strumenti di sviluppo (DevTools) progettati per facilitare il testing e lo sviluppo dell'applicazione. Questi strumenti sono disponibili **solo in ambiente di sviluppo** o quando esplicitamente abilitati tramite variabili d'ambiente.

## ⚠️ Avvertenze Importanti

- **NON utilizzare questi strumenti in ambiente di produzione** a meno che non si sappia esattamente cosa si sta facendo
- Gli strumenti possono creare grandi quantità di dati nel database
- Si consiglia di utilizzare un database di test o di avere un backup recente prima di utilizzare questi strumenti
- L'accesso è limitato agli utenti con ruolo **Admin** o **SuperAdmin**

## Abilitazione DevTools

I DevTools sono abilitati automaticamente quando:

1. **L'ambiente è Development**: `ASPNETCORE_ENVIRONMENT=Development`
2. **OPPURE** quando la variabile d'ambiente `DEVTOOLS_ENABLED` è impostata a `true`

### Configurazione per ambiente Development

Nel file `appsettings.Development.json` non è necessaria alcuna configurazione aggiuntiva. I DevTools sono abilitati automaticamente.

### Configurazione per altri ambienti

Per abilitare i DevTools in un ambiente diverso da Development (es. Staging), aggiungere la seguente variabile d'ambiente:

```bash
# Windows
set DEVTOOLS_ENABLED=true

# Linux/Mac
export DEVTOOLS_ENABLED=true

# Docker
docker run -e DEVTOOLS_ENABLED=true ...

# appsettings.json (NON RACCOMANDATO per produzione)
{
  "DEVTOOLS_ENABLED": "true"
}
```

### Configurazione per Produzione

**NON abilitare mai i DevTools in produzione** a meno che non ci sia una ragione specifica e temporanea per farlo. Se necessario:

1. Assicurarsi che solo gli amministratori di sistema abbiano accesso
2. Impostare `DEVTOOLS_ENABLED=true` solo temporaneamente
3. Monitorare attentamente l'utilizzo delle risorse
4. Rimuovere la variabile d'ambiente al termine

## Funzionalità Disponibili

### 1. Generazione Prodotti di Test

Crea automaticamente un numero configurabile di prodotti randomizzati nel database per testare le funzionalità di gestione prodotti e le prestazioni della tabella EFTable.

#### Come Usare

1. **Accedere all'applicazione** come utente con ruolo Admin o SuperAdmin
2. **Navigare alla pagina** "Gestione Prodotti" (`/product-management/products`)
3. **Cercare il pulsante** "Genera Prodotti Test" (icona provetta/science) nella toolbar
   - Il pulsante è visibile solo se:
     - L'utente ha ruolo Admin o SuperAdmin
     - I DevTools sono abilitati (environment Development o DEVTOOLS_ENABLED=true)
4. **Cliccare sul pulsante** per aprire la finestra di configurazione
5. **Configurare i parametri**:
   - **Numero di prodotti**: da 1 a 20.000 (default: 5.000)
   - **Dimensione batch**: da 10 a 1.000 (default: 100)
6. **Confermare** cliccando su "Conferma e Genera"
7. **Monitorare il progresso**:
   - La barra di progresso mostra lo stato di avanzamento
   - È possibile chiudere la finestra; il job continuerà in background
   - È possibile cancellare il job in corso tramite il pulsante "Annulla Job"
8. **Al completamento**:
   - Viene mostrato un riepilogo con:
     - Numero di prodotti creati
     - Numero di errori (se presenti)
     - Durata totale dell'operazione
   - È possibile ricaricare la pagina per vedere i nuovi prodotti

#### Cosa Genera

Per ogni prodotto, il sistema genera automaticamente:

- **Informazioni di base**:
  - Nome (es. "Tastiera Wireless", "Mouse Ergonomico")
  - Codice/SKU univoco (formato: TEST-XXXXXXXX)
  - Descrizione breve e dettagliata
- **Prezzi e dati finanziari**:
  - Prezzo di default (tra €1 e €1.000)
  - Flag IVA inclusa (randomizzato)
- **Inventario**:
  - Livello di riordino (reorder point)
  - Stock di sicurezza (safety stock)
  - Livello di stock target
  - Domanda media giornaliera
- **Stato e metadata**:
  - Stato prodotto (Attivo, Sospeso, Fuori Stock, Eliminato)
  - Flag bundle (10% sono bundle)
  - Data di creazione (negli ultimi 2 anni)
  - URL immagine (placeholder)
  - Tenant ID e User ID appropriati

#### Parametri di Performance

- **Dimensione Batch**: controlla quanti prodotti vengono inseriti alla volta
  - Batch più piccoli: più lenti ma meno impatto sul database
  - Batch più grandi: più veloci ma maggiore utilizzo di memoria
  - Consigliato: 100 per un buon equilibrio

- **Numero di Prodotti**: 
  - Fino a 1.000: esecuzione veloce (pochi secondi)
  - 1.000 - 5.000: esecuzione moderata (circa 30-60 secondi)
  - 5.000 - 20.000: esecuzione più lunga (2-5 minuti)

#### Note Tecniche

- I prodotti sono generati usando la libreria **Bogus** per dati realistici
- Ogni prodotto ha un codice SKU univoco con prefisso "TEST-" per facilitare l'identificazione
- I job sono eseguiti in modo asincrono sul server
- Il polling dello stato avviene ogni 2 secondi
- I dati vengono inseriti in transazioni per garantire coerenza
- Gli errori per singoli prodotti sono registrati ma non fermano l'intera operazione

## API Endpoints

Gli endpoint DevTools sono disponibili sotto il prefisso `/api/v1/devtools/`.

### POST /api/v1/devtools/generate-products

Avvia un job di generazione prodotti.

**Autenticazione**: Richiesta (Bearer Token)  
**Ruoli Richiesti**: Admin, SuperAdmin

**Request Body**:
```json
{
  "count": 5000,
  "batchSize": 100
}
```

**Response**: 200 OK
```json
{
  "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "message": "Job di generazione di 5000 prodotti avviato con successo.",
  "startedAt": "2025-12-03T20:00:00Z"
}
```

### GET /api/v1/devtools/generate-products/status/{jobId}

Ottiene lo stato di un job di generazione.

**Autenticazione**: Richiesta (Bearer Token)  
**Ruoli Richiesti**: Admin, SuperAdmin

**Response**: 200 OK
```json
{
  "jobId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Running",
  "processed": 2500,
  "total": 5000,
  "created": 2480,
  "errors": 20,
  "errorMessage": null,
  "startedAt": "2025-12-03T20:00:00Z",
  "completedAt": null,
  "durationSeconds": null
}
```

**Stati possibili**:
- `Pending`: Job in attesa di avvio
- `Running`: Job in esecuzione
- `Done`: Job completato con successo
- `Failed`: Job fallito
- `Cancelled`: Job cancellato dall'utente

### POST /api/v1/devtools/generate-products/cancel/{jobId}

Cancella un job in esecuzione.

**Autenticazione**: Richiesta (Bearer Token)  
**Ruoli Richiesti**: Admin, SuperAdmin

**Response**: 200 OK
```json
{
  "message": "Job cancellato con successo."
}
```

## Sicurezza

### Protezioni Implementate

1. **Autenticazione**: Tutti gli endpoint richiedono un token JWT valido
2. **Autorizzazione**: Solo utenti con ruolo Admin o SuperAdmin possono accedere
3. **Environment Check**: I DevTools sono disabilitati di default in produzione
4. **Validation**: Tutti gli input sono validati (es. max 20.000 prodotti)
5. **Rate Limiting**: Protezione contro abuso tramite rate limiter globale
6. **Audit Logging**: Tutte le operazioni sono registrate nei log

### Codice di Verifica

Il controller verifica sempre:
```csharp
// Verifica ambiente
if (!IsDevToolsEnabled())
{
    return Forbid();
}

// Verifica ruolo
if (!User.IsInRole("Admin") && !User.IsInRole("SuperAdmin"))
{
    return Forbid();
}
```

## Testing

### Test Unitari

I DevTools includono test unitari per validare la funzionalità:

```bash
# Eseguire i test DevTools
cd EventForge.Tests
dotnet test --filter "Category=Unit&FullyQualifiedName~DevTools"
```

### Test di Integrazione

Per testare localmente:

1. Assicurarsi di utilizzare un database di test
2. Avviare l'applicazione in modalità Development
3. Accedere come admin
4. Navigare alla pagina Gestione Prodotti
5. Utilizzare il pulsante "Genera Prodotti Test" con un numero basso (es. 10-100 prodotti)
6. Verificare che i prodotti siano stati creati correttamente
7. Verificare le prestazioni della tabella EFTable con i nuovi dati

### Pulizia dei Dati di Test

I prodotti generati hanno tutti un codice che inizia con "TEST-". Per rimuoverli:

```sql
-- SQL Server
DELETE FROM Products WHERE Code LIKE 'TEST-%' AND TenantId = '<your-tenant-id>';

-- Oppure utilizzare Soft Delete
UPDATE Products 
SET IsDeleted = 1, DeletedAt = GETUTCDATE(), DeletedBy = 'cleanup'
WHERE Code LIKE 'TEST-%' AND TenantId = '<your-tenant-id>';
```

## Troubleshooting

### Il pulsante non è visibile

**Possibili cause**:
1. L'utente non ha ruolo Admin o SuperAdmin
   - **Soluzione**: Verificare i ruoli dell'utente nel database
2. I DevTools non sono abilitati
   - **Soluzione**: Verificare `ASPNETCORE_ENVIRONMENT` o `DEVTOOLS_ENABLED`
3. L'ambiente è Production e DEVTOOLS_ENABLED non è impostato
   - **Soluzione**: Abilitare esplicitamente tramite variabile d'ambiente

### Errore 403 Forbidden

**Causa**: L'utente non ha i permessi necessari o i DevTools sono disabilitati

**Soluzione**:
1. Verificare che l'utente sia loggato
2. Verificare il ruolo dell'utente
3. Verificare le variabili d'ambiente
4. Controllare i log del server per dettagli

### Il job è troppo lento

**Possibili soluzioni**:
1. Ridurre il numero di prodotti
2. Aumentare la dimensione del batch (max 1000)
3. Verificare le prestazioni del database
4. Verificare che non ci siano altri job in esecuzione

### Errori durante la generazione

**Cosa controllare**:
1. Log del server per dettagli specifici
2. Connessione al database
3. Spazio disponibile nel database
4. Vincoli di integrità referenziale (es. TenantId valido)

## Limiti e Restrizioni

- **Numero massimo di prodotti**: 20.000 per job
- **Dimensione batch massima**: 1.000 prodotti
- **Dimensione batch minima**: 10 prodotti
- **Job concorrenti**: Non limitato (ma sconsigliato)
- **Timeout**: Nessun timeout esplicito (il job continua fino al completamento o cancellazione)

## Estensioni Future

Possibili miglioramenti:
- Generazione di altri tipi di entità (clienti, fornitori, ordini, ecc.)
- Generazione di dati con relazioni complesse (prodotti con varianti, bundle, ecc.)
- Export dei dati generati per riutilizzo
- Template personalizzabili per la generazione
- Dashboard di monitoring per i job in esecuzione
- Generazione incrementale con seed per riproducibilità

## Supporto

Per problemi o domande relative ai DevTools:

1. Controllare i log del server: `Logs/log-YYYY-MM-DD.txt`
2. Verificare la documentazione: questo file
3. Contattare il team di sviluppo

## Changelog

### Versione 1.0.0 (2025-12-03)
- ✅ Implementazione iniziale
- ✅ Generazione prodotti con Bogus
- ✅ UI con modal interattiva e progress bar
- ✅ API REST completa
- ✅ Test unitari
- ✅ Protezioni di sicurezza
- ✅ Documentazione completa
