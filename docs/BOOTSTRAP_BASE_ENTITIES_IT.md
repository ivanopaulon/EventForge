# Entità Base Bootstrap - Implementazione Completata

## Panoramica

Il sistema EventForge è stato aggiornato per creare automaticamente le entità base necessarie durante l'inizializzazione di un nuovo tenant. Questo fornisce una base completa per la gestione del magazzino e dei prodotti sin dall'inizio.

## Cosa è Stato Implementato

### 1. Natura IVA (VatNature)

È stata aggiunta una nuova entità `VatNature` per gestire le nature IVA previste dalla normativa fiscale italiana.

**Codici Natura IVA Precaricati**: 24 codici completi

- **N1**: Escluse ex art. 15
- **N2**: Non soggette (con sottocasi N2.1, N2.2)
- **N3**: Non imponibili (con sottocasi N3.1 - N3.6)
  - N3.1: Esportazioni
  - N3.2: Cessioni intracomunitarie
  - N3.3: Cessioni verso San Marino
  - N3.4: Operazioni assimilate
  - N3.5: Altre operazioni non imponibili
  - N3.6: Altre operazioni non imponibili che non concorrono al plafond
- **N4**: Esenti
- **N5**: Regime del margine
- **N6**: Inversione contabile (con sottocasi N6.1 - N6.9)
  - N6.1: Cessioni di rottami
  - N6.2: Cessioni di oro e argento
  - N6.3: Subappalto nel settore edile
  - N6.4: Cessioni di fabbricati
  - N6.5: Cessioni di telefoni cellulari
  - N6.6: Cessioni di prodotti elettronici
  - N6.7: Prestazioni settore edile
  - N6.8: Operazioni settore energetico
  - N6.9: Altri casi
- **N7**: IVA assolta in altro stato UE

### 2. Aliquote IVA Aggiornate

L'entità `VatRate` è stata aggiornata per includere il collegamento alla Natura IVA.

**Aliquote IVA Precaricate**: 5 aliquote secondo la normativa vigente (2024-2025)

- **22%**: Aliquota IVA ordinaria
- **10%**: Aliquota ridotta (generi alimentari, bevande, servizi turistici)
- **5%**: Aliquota ridotta (generi di prima necessità)
- **4%**: Aliquota minima (beni di primissima necessità: pane, latte, ecc.)
- **0%**: Operazioni non imponibili, esenti o fuori campo IVA

### 3. Unità di Misura

Il sistema precarica 19 unità di misura comunemente utilizzate nella gestione del magazzino:

**Unità per Conteggio**:
- Pezzo (pz) - *impostata come predefinita*
- Confezione (conf)
- Scatola (scat)
- Cartone (cart)
- Pallet (pallet)
- Bancale (banc)
- Collo (collo)

**Unità di Peso**:
- Kilogrammo (kg)
- Grammo (g)
- Tonnellata (t)
- Quintale (q)

**Unità di Volume**:
- Litro (l)
- Millilitro (ml)
- Metro cubo (m³)

**Unità di Lunghezza**:
- Metro (m)
- Centimetro (cm)
- Metro quadrato (m²)

**Altre Unità**:
- Paio (paio)
- Set (set)
- Kit (kit)

### 4. Magazzino e Ubicazione Predefiniti

Ogni nuovo tenant riceve automaticamente:

**Magazzino Principale** (Codice: MAG-01)
- Nome: Magazzino Principale
- Marcato come fiscale (IsFiscal = true)
- Note: Magazzino principale creato durante l'inizializzazione del sistema

**Ubicazione Predefinita** (Codice: UB-DEF)
- Descrizione: Ubicazione predefinita
- Collegata al magazzino principale

## Dettagli Tecnici

### Modifiche al Database

**Nuova Tabella**: `VatNatures`
- Contiene tutti i codici natura IVA italiani
- Supporta relazione con VatRate

**Tabella Modificata**: `VatRates`
- Aggiunto campo `VatNatureId` (nullable)
- Aggiunta relazione con VatNature

**Migration**: `AddVatNatureAndBootstrapEnhancements`
- Data: 05/01/2025
- Crea tabella VatNatures
- Aggiunge foreign key a VatRates

### Flusso di Bootstrap

Durante l'inizializzazione del sistema, se non esistono tenant:

1. Creazione tenant predefinito
2. Creazione utente SuperAdmin
3. Assegnazione licenza
4. **Seeding entità base** (NUOVO):
   - Nature IVA (24 codici)
   - Aliquote IVA (5 aliquote)
   - Unità di misura (19 unità)
   - Magazzino e ubicazione predefiniti

### Caratteristiche Importanti

**Idempotenza**: Tutti i metodi di seeding controllano l'esistenza dei dati prima dell'inserimento
- Nessuna duplicazione in caso di riavvio
- Sicuro per esecuzioni multiple
- I tenant esistenti non sono influenzati

**Tenant-Scoped**: Tutti i dati sono associati al tenant specifico
- Isolamento completo tra tenant
- Ogni tenant ha il proprio set di dati base

**Logging Completo**: Ogni operazione di seeding è tracciata nei log
- Facile debug
- Monitoraggio del processo di bootstrap
- Verifica della corretta inizializzazione

## Verifica del Funzionamento

### Query di Verifica

Dopo il bootstrap, eseguire queste query per verificare:

```sql
-- Verifica Nature IVA
SELECT COUNT(*) as TotaleNature FROM VatNatures WHERE TenantId = @tenantId;
-- Risultato atteso: 24

-- Verifica Aliquote IVA
SELECT Name, Percentage, Status FROM VatRates WHERE TenantId = @tenantId;
-- Risultato atteso: 5 aliquote (22%, 10%, 5%, 4%, 0%)

-- Verifica Unità di Misura
SELECT COUNT(*) as TotaleUM FROM UMs WHERE TenantId = @tenantId;
-- Risultato atteso: 19

-- Verifica Magazzino
SELECT Name, Code, IsFiscal FROM StorageFacilities WHERE TenantId = @tenantId;
-- Risultato atteso: 1 magazzino (MAG-01)

-- Verifica Ubicazioni
SELECT Code, Description FROM StorageLocations WHERE TenantId = @tenantId;
-- Risultato atteso: 1 ubicazione (UB-DEF)
```

### Log Attesi

Durante il bootstrap, nei log dovrebbero apparire questi messaggi:

```
[INFO] Starting bootstrap process...
[INFO] Seeding base entities for tenant {TenantId}...
[INFO] Seeding VAT natures for tenant {TenantId}...
[INFO] Seeded 24 VAT natures for tenant {TenantId}
[INFO] Seeding VAT rates for tenant {TenantId}...
[INFO] Seeded 5 VAT rates for tenant {TenantId}
[INFO] Seeding units of measure for tenant {TenantId}...
[INFO] Seeded 19 units of measure for tenant {TenantId}
[INFO] Seeding default warehouse for tenant {TenantId}...
[INFO] Created default warehouse 'Magazzino Principale' with default location 'UB-DEF'
[INFO] Base entities seeded successfully for tenant {TenantId}
[INFO] === BOOTSTRAP COMPLETED SUCCESSFULLY ===
```

## File Modificati/Creati

### Nuovi File
1. `EventForge.Server/Data/Entities/Common/VatNature.cs` - Nuova entità per le nature IVA
2. `EventForge.Server/Migrations/20251005223454_AddVatNatureAndBootstrapEnhancements.cs` - Migration EF Core
3. `docs/BOOTSTRAP_BASE_ENTITIES.md` - Documentazione completa in inglese
4. `docs/BOOTSTRAP_BASE_ENTITIES_IT.md` - Questa documentazione in italiano

### File Modificati
1. `EventForge.Server/Data/Entities/Common/VatRate.cs` - Aggiunto VatNatureId
2. `EventForge.Server/Data/EventForgeDbContext.cs` - Aggiunto DbSet<VatNature>
3. `EventForge.Server/Services/Auth/BootstrapService.cs` - Aggiunti metodi di seeding

## Benefici dell'Implementazione

### Per l'Utente
- **Pronto all'uso**: Il sistema è immediatamente operativo con dati base configurati
- **Conformità fiscale**: Nature IVA complete secondo normativa italiana
- **Gestione semplificata**: Magazzino e ubicazione predefiniti già creati
- **Unità standard**: Set completo di unità di misura per iniziare subito

### Per lo Sviluppo
- **Manutenibilità**: Codice ben strutturato e documentato
- **Estensibilità**: Facile aggiungere nuove entità base
- **Testabilità**: Metodi indipendenti facilmente testabili
- **Sicurezza**: Idempotenza garantita per evitare duplicazioni

## Prossimi Passi Suggeriti

1. **Test End-to-End**: Testare il bootstrap completo su database vuoto
2. **UI per Gestione**: Creare interfacce per gestire nature IVA e aliquote
3. **Configurazione Avanzata**: Permettere personalizzazione delle entità base
4. **Multi-Paese**: Estendere il supporto ad altri paesi europei
5. **Import/Export**: Funzionalità per importare/esportare configurazioni base

## Riferimenti

- Agenzia delle Entrate: Nature IVA e codici fiscali
- Normativa IVA italiana vigente (2024-2025)
- Documentazione EF Core Migrations
- Bootstrap System Guide: `docs/frontend/BOOTSTRAP_SYSTEM_GUIDE.md`

## Note Finali

Questa implementazione fornisce una base solida per la gestione fiscale e del magazzino in EventForge. Tutti i dati sono tenant-scoped e rispettano le best practice di auditing e soft delete ereditate da `AuditableEntity`.

Il sistema è stato progettato per essere:
- **Robusto**: Gestione errori completa
- **Sicuro**: Nessuna duplicazione di dati
- **Tracciabile**: Logging completo di tutte le operazioni
- **Manutenibile**: Codice pulito e ben documentato
