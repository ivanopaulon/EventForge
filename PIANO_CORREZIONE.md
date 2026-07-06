# PIANO_CORREZIONE.md — EventForge Workspace

**Generato da:** Audit completo del 2026-07-06  
**Stato:** Piano operativo — da eseguire fase per fase con verifica a ogni chiusura  
**Nota:** questo documento NON deve essere eseguito tutto in una volta. Ogni fase va applicata, verificata e approvata prima di procedere alla successiva.

---

## 1. Executive Summary

L'audit del 2026-07-06 ha identificato **18 problemi** distribuiti su 4 progetti principali (EventForge.Server, Prym.Web, Prym.Agent, root/docs).

| Severità | Count | Descrizione |
|---|---|---|
| 🔴 Bloccante | 1 | Migrazione EPPlus→ClosedXML incompleta (licenza commerciale a rischio) |
| 🟠 Alta | 4 | Credenziali dev in git, link rotti in docs, file orfano, doc planning stale |
| 🟡 Media | 5 | Commenti obsoleti, metodi deprecated, version mismatch package, pragma warning soppresso |
| 🟢 Bassa | 8 | TODO cosmetic, commenti fuorvianti, doc di audit superata, version header doc |

**Sforzo complessivo stimato:** S–M (la maggior parte sono interventi chirurgici su file singoli; nessun refactoring architetturale maggiore)

**Nessun bug funzionale bloccante** è stato identificato: tutte le feature dichiarate come implementate sono confermate nel codice.

---

## 2. Registro Problemi Deduplicato

| ID | Problema | Categoria | Severità | Sforzo | Rischio regressione | Progetto/Modulo |
|---|---|---|---|---|---|---|
| P01 | `ExportService.cs` usa ancora EPPlus (`OfficeOpenXml`) invece di ClosedXML, nonostante ADR di migrazione | Debito tecnico / Licenza | 🔴 Bloccante | M | Medio | EventForge.Server / Services/Export |
| P02 | `appsettings.json` contiene password dev in chiaro tracciate in git (`pass123!`, `EventForge@2024!`, `dev-admin-key`, ecc.) | Sicurezza | 🟠 Alta | S | Basso | EventForge.Server / Prym.Agent |
| P03 | `docs/features/README.md` punta a 11 file inesistenti (link rotti) | Documentazione | 🟠 Alta | XS | Basso | docs/features |
| P04 | `SHIFTS_PLAN.md` e `SHIFTS_AUDIT.md` in root descrivono CashierShift come "non ancora implementato" — stale | Documentazione | 🟠 Alta | XS | Basso | Root |
| P05 | `Server/Data/EntityConfigurations/TransferOrderConfiguration.cs` è un file orfano tracciato in git, fuori da qualsiasi `.csproj` | Incoerenza architetturale | 🟠 Alta | XS | Basso | Root (Server/) |
| P06 | `audit/AUDIT_REPORT.md` generato il 2025-09-05, obsoleto rispetto allo stato attuale (Sprint 4-5 non inclusi) | Documentazione | 🟠 Alta | XS | Basso | audit/ |
| P07 | `FidelityCardViewModel.cs:26` e `FidelityPointsTransactionViewModel.cs:15` hanno commento XML `(client-side mock)` — il backend è reale da Sprint 4 | Documentazione / Commento | 🟡 Media | XS | Basso | Prym.Web / Models/Fidelity |
| P08 | `MUDBLAZOR_AUDIT.md:3` dichiara `MudBlazor: 9.2.0`, versione reale è `9.5.0` | Documentazione | 🟡 Media | XS | Basso | Prym.Web |
| P09 | `FluentValidation.AspNetCore` v11.3.1 e `FluentValidation` v12.1.1 — versione mismatch; l'integration package AspNetCore non è allineato al core | Debito tecnico | 🟡 Media | S | Basso | EventForge.Server |
| P10 | ~~`ISaleSessionService.cs:174,183` — due metodi marcati `[Obsolete]` senza roadmap di rimozione o deprecation notice strutturata~~ **✅ RISOLTO — 2026-07-06 (Milestone 4)**: metodi rimossi da `ISaleSessionService`, `SaleSessionService`, `SalesController`; client `SalesService.cs` migrato a `pos-sessions/open` e `pos-sessions/operator/{id}`. | Debito tecnico | 🟡 Media | S | Basso | EventForge.Server / Services/Sales |
| P11 | ~~`ProductService.cs` — 7 blocchi `#pragma warning disable CS0618` per `Product.ImageUrl` obsoleto; necessita roadmap rimozione post-migrazione ImageDocumentId~~ **✅ RISOLTO PARZIALMENTE — 2026-07-06 (Milestone 4)**: `ImageUrl` rimosso da tutti i DTO, validator, UI e `ProductService`; 0 pragma CS0618 residui nei file server. Colonna DB ancora presente (checkpoint umano): eseguire `20260706_RemoveImageUrlFromProducts.sql` quando approvato. | Debito tecnico | 🟡 Media | S | Medio | EventForge.Server / Services/Products |
| P12 | `IExportService.cs:9` — docstring dice "using EPPlus" (obsoleta dopo P01) | Documentazione | 🟢 Bassa | XS | Basso | EventForge.Server / Services/Export |
| P13 | `Directory.Packages.props` — `PackageVersion Include="EPPlus"` diventerà inutilizzato dopo P01 | Debito tecnico | 🟢 Bassa | XS | Basso | Directory.Packages.props |
| P14 | `NotificationCenter.razor:259` — commento `<!-- Pagination placeholder -->` fuorviante; la paginazione è implementata | Documentazione / Commento | 🟢 Bassa | XS | Basso | Prym.Web / Pages/Notifications |
| P15 | `FiscalPrintersDashboard.razor:8` — TODO EFTable per real-time SignalR (dipende da feature EFTable non pianificata) | TODO documentato | 🟢 Bassa | — | Basso | Prym.Web / Pages/Admin |
| P16 | `InventoryProcedure.razor:7` — TODO ManagementPageHeader per uniformare header | TODO cosmetico | 🟢 Bassa | XS | Basso | Prym.Web / Pages/Warehouse |
| P17 | `InventoryMerge.razor:7` — TODO EFTable embedded (dipende da archetype WizardPage futuro) | TODO cosmetico | 🟢 Bassa | — | Basso | Prym.Web / Pages/Warehouse |
| P18 | `docs/decision-log/ADR-CLOSEDXML-MIGRATION.md` dichiara la migrazione "Implemented" — parzialmente falso (ExportService.cs ancora su EPPlus) | Documentazione | 🟡 Media | XS | Basso | docs/decision-log |

> **P15 e P17**: non pianificabili autonomamente — dipendono da feature EFTable/WizardPage non ancora in roadmap. Segnalati come aperti senza data.

---

## 3. Analisi Dipendenze

### Dipendenze dirette

```
P01 (EPPlus migration) ──────────► P12 (aggiorna docstring IExportService)
                       ──────────► P13 (rimuovi EPPlus da Directory.Packages.props)
                       ──────────► P18 (aggiorna ADR-CLOSEDXML-MIGRATION.md)

P09 (FluentValidation mismatch) → nessuna dipendenza esterna; può procedere standalone

P11 (ImageUrl obsolete pragma) → nessun blocco, ma richiede conferma che nessun consumer
                                  esterno usi Product.ImageUrl prima della rimozione
```

### Problemi "leva" (risolvono o sboccano altri)

1. **P01** — risolvere P01 sblocca P12, P13, P18 (tre fix automaticamente conseguenti)
2. **P09** — non sblocca altri ma elimina un potenziale runtime issue silenzioso con FluentValidation

### Problemi indipendenti (eseguibili in qualsiasi ordine)

P02, P03, P04, P05, P06, P07, P08, P10, P14, P16

### Nessun ciclo o conflitto reciproco rilevato.

---

## 4. Piano a Fasi

---

### FASE A — Sicurezza e compliance licenze
**Obiettivo:** eliminare la vulnerabilità credenziali git e risolvere il problema di licenza EPPlus prima di qualsiasi altro intervento.

#### Task A1 — Migrazione ExportService.cs da EPPlus a ClosedXML [P01]

**Cosa fare:**
1. In `EventForge.Server/Services/Export/ExportService.cs`:
   - Rimuovere `using OfficeOpenXml;` e `using System.Drawing;`
   - Rimuovere il metodo statico `InitStatic()` e la call a `ExcelPackage.License.SetNonCommercialPersonal`
   - Riscrivere `ExportToExcelAsync<T>` usando `ClosedXML.Excel` (pattern `new XLWorkbook()`, `workbook.Worksheets.Add(...)`, `worksheet.Cell(row, col).Value = ...`), allineato al pattern già usato in `ExcelExportService.cs` e `DocumentExportService.cs`
   - La firma del metodo pubblico (`Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, string sheetName, CancellationToken)`) rimane invariata — nessun breaking change verso i consumer

2. In `EventForge.Server/Services/Export/IExportService.cs`:
   - Aggiornare il commento XML a "Exports data to Excel format using ClosedXML"

3. In `EventForge.Server/EventForge.Server.csproj`:
   - Rimuovere `<PackageReference Include="EPPlus" />`

4. In `Directory.Packages.props`:
   - Rimuovere (o commentare) `<PackageVersion Include="EPPlus" Version="8.6.1" />`

5. In `docs/decision-log/ADR-CLOSEDXML-MIGRATION.md`:
   - Aggiornare la sezione "Status" da "Implemented" a "Implemented (2026-07-xx: ExportService.cs migrated)"

**Dove:** `EventForge.Server/Services/Export/ExportService.cs`, `IExportService.cs`, `EventForge.Server.csproj`, `Directory.Packages.props`, `docs/decision-log/ADR-CLOSEDXML-MIGRATION.md`

**Consumer da verificare in parallelo:**
- `BusinessPartiesController.cs` — chiama `ExportToExcelAsync(data, "Business Parties", ct)` → firma invariata, nessun cambiamento
- `DocumentHeadersController.cs` — chiama `ExportToExcelAsync(data, "Documents", ct)` → idem
- `ProductManagementController.cs` — chiama `ExportToExcelAsync(data, "Products", ct)` → idem
- `WarehouseFacade.cs` — chiama `ExportToExcelAsync(data, sheetName, ct)` → idem

**Come verificare:**
1. Build `dotnet build EventForge.Server/EventForge.Server.csproj` — deve passare senza warning NU
2. Nessun `using OfficeOpenXml` residuo: `grep -rn "OfficeOpenXml\|ExcelPackage" EventForge.Server/ --include="*.cs"` → 0 risultati
3. Endpoint `/api/v1/export` (ExportController) e endpoint export su BusinessParties, Products, Documents → test manuale con download Excel e verifica apertura file
4. Run test suite `EventForge.Tests`: tutti i test preesistenti passano

**Effetti collaterali:** nessuno; ClosedXML è già presente e in uso in altri tre servizi del progetto.

**Rollback:** ripristinare i file originali da git (`git checkout -- <file>`); EPPlus era funzionante.

---

#### Task A2 — Gestione credenziali dev in appsettings.json [P02]

> **⚠️ Checkpoint umano obbligatorio** prima di procedere: questo task modifica la configurazione del progetto e può impattare gli ambienti di sviluppo locali del team.

**Cosa fare:**
1. In `EventForge.Server/appsettings.json`: sostituire i valori sensibili con placeholder descrittivi nelle sezioni:
   - `ConnectionStrings.DefaultConnection` → `"Server=REPLACE_SQL_HOST;Database=EventData;User Id=REPLACE_SQL_USER;******;TrustServerCertificate=True;"`
   - `ConnectionStrings.SqlServer` → idem
   - `ConnectionStrings.LogDb` → idem
   - `Encryption.Key` → `"REPLACE_WITH_STRONG_ENCRYPTION_KEY"`
   - `Authentication.Jwt.SecretKey` → `"REPLACE_WITH_STRONG_JWT_SECRET_AT_LEAST_32_CHARS"`
   - `Bootstrap.SuperAdminPassword` → `"REPLACE_WITH_SUPERADMIN_PASSWORD"`
   - `Bootstrap.StoreOperatorPassword` → `"REPLACE_WITH_OPERATOR_PASSWORD"`
   - Lasciare `SyncfusionLicenseKey` e `BoldReportsLicenseKey` in quanto sono chiavi di dev già pubbliche nei file originali (valutare se spostare a secrets separatamente)

2. Creare (se non esiste) `EventForge.Server/appsettings.Development.json` con i valori dev effettivi (questo file va aggiunto a `.gitignore`).

3. Verificare che `EventForge.Server/.gitignore` includa `appsettings.Development.json` e `appsettings.Production.json`.

4. Aggiornare il README di deploy (`EventForge.Server/README_DEPLOY_CLIENT.md`) con istruzioni per la configurazione locale.

5. Stesso processo per `Prym.Agent/appsettings.json`: le sezioni `Password: "Admin#123!"` e `ConnectionString: "...pass123!..."` vanno sostituite con placeholder.

**Dove:** `EventForge.Server/appsettings.json`, `Prym.Agent/appsettings.json`, `.gitignore`, `EventForge.Server/README_DEPLOY_CLIENT.md`

**Come verificare:**
- `git diff HEAD appsettings.json` — nessuna password in chiaro nel diff
- `grep -rn "pass123!\|EventForge@2024!\|dev-admin-key\|dev-maintenance-secret" . --include="*.json"` → 0 risultati nei file tracciati
- L'applicazione si avvia correttamente con le variabili d'ambiente o `appsettings.Development.json` locale

**Effetti collaterali:** gli sviluppatori che clonano il repo dovranno configurare `appsettings.Development.json` prima di avviare il progetto. Documentare nel README.

**Rollback:** ripristino git; comunicare al team prima del merge.

---

### FASE B — Pulizia strutturale e file orfani
**Obiettivo:** eliminare file orfani e planning doc stale che creano confusione sullo stato del codice.

#### Task B1 — Eliminare file orfano Server/ [P05]

**Cosa fare:**
- Eliminare l'intera directory `Server/` dalla root del repository:
  ```
  git rm -r Server/
  ```

**Dove:** `Server/Data/EntityConfigurations/TransferOrderConfiguration.cs`

**Come verificare:**
- `ls Server/` → "No such file or directory"
- Build completa passa senza errori
- `git ls-files Server/` → nessun output

**Effetti collaterali:** nessuno; il file non è referenziato da nessun `.csproj`.

---

#### Task B2 — Archiviare SHIFTS_PLAN.md e SHIFTS_AUDIT.md [P04]

**Cosa fare:**
- Spostare entrambi i file da root a `docs/archive/`:
  ```
  git mv SHIFTS_PLAN.md docs/archive/SHIFTS_PLAN_archived.md
  git mv SHIFTS_AUDIT.md docs/archive/SHIFTS_AUDIT_archived.md
  ```
- Aggiungere in cima a ciascun file un banner:
  ```markdown
  > **ARCHIVIATO** — CashierShift è completamente implementato (vedi `EventForge.Server/Data/Entities/Store/CashierShift.cs`). Questo documento è conservato come riferimento storico della fase di pianificazione.
  ```

**Dove:** `SHIFTS_PLAN.md`, `SHIFTS_AUDIT.md` → `docs/archive/`

**Come verificare:**
- `ls SHIFTS_PLAN.md SHIFTS_AUDIT.md` → "No such file or directory"
- `ls docs/archive/SHIFTS_*` → due file presenti

---

#### Task B3 — Archiviare audit/AUDIT_REPORT.md [P06]

**Cosa fare:**
- In `audit/AUDIT_REPORT.md`, aggiungere banner in cima:
  ```markdown
  > **OBSOLETO** — Generato il 2025-09-05. Non riflette Sprint 4-5 (StationMonitor, FidelityCard, AI Order, RebuildMovements, ecc.). Conservato come riferimento storico. Per lo stato aggiornato vedere `PIANO_CORREZIONE.md`.
  ```
- Non eliminare il file: contiene lo storico delle 175 osservazioni dell'audit precedente.

**Dove:** `audit/AUDIT_REPORT.md`

---

### FASE C — Aggiornamento packagee dipendenze
**Obiettivo:** allineare le versioni di package con potenziale impatto runtime.

#### Task C1 — Aggiornare FluentValidation.AspNetCore [P09]

**Contesto:** `FluentValidation` v12.1.1 + `FluentValidation.AspNetCore` v11.3.1 è un mismatch. FluentValidation.AspNetCore 11.x è stato progettato per FluentValidation v11. Con v12 l'integrazione DI usa direttamente `AddValidatorsFromAssembly` (già in uso in `Program.cs` a riga 113), rendendo `FluentValidation.AspNetCore` potenzialmente non necessario.

**Cosa fare:**
1. Verificare se `FluentValidation.AspNetCore` è usato direttamente in qualche file:
   ```
   grep -rn "using FluentValidation.AspNetCore" EventForge.Server/ --include="*.cs"
   ```
2. **Se non usato direttamente**: rimuovere `<PackageReference Include="FluentValidation.AspNetCore" />` da `EventForge.Server.csproj` e la relativa `<PackageVersion>` da `Directory.Packages.props`. Il setup DI funziona già via `AddValidatorsFromAssemblyContaining<Program>()`.
3. **Se usato**: aggiornare `FluentValidation.AspNetCore` a una versione compatibile con v12 (al momento: FluentValidation.AspNetCore 12.x, se disponibile, o verificare il changelog ufficiale).

**Dove:** `Directory.Packages.props`, `EventForge.Server/EventForge.Server.csproj`

**Come verificare:**
- Build completa senza warning NU
- Run test suite: tutti i test di validazione passano
- Endpoint con payload invalido restituisce correttamente 400 con dettagli FluentValidation

**Effetti collaterali:** basso; il pattern DI esistente non usa `AddFluentValidation()` legacy.

---

### FASE D — Allineamento documentazione tecnica
**Obiettivo:** aggiornare tutti i commenti e documenti che descrivono erroneamente lo stato del codice.

#### Task D1 — Aggiornare commenti "mock" FidelityCard [P07]

**Cosa fare:**
- In `Prym.Web/Models/Fidelity/FidelityCardViewModel.cs:26`:
  Cambiare `/// View Model per la carta fedeltà (client-side mock)` → `/// View model client-side per FidelityCard. Il backend è implementato (FidelityCardsController, FidelityCardService).`
- In `Prym.Web/Models/Fidelity/FidelityPointsTransactionViewModel.cs:15`:
  Cambiare `/// View Model per transazione punti fedeltà (client-side mock)` → `/// View model client-side per FidelityPointsTransaction. Il backend è implementato.`

**Come verificare:** build Prym.Web passa; commento aggiornato visibile in IDE.

---

#### Task D2 — Aggiornare versione MudBlazor in MUDBLAZOR_AUDIT.md [P08]

**Cosa fare:**
- In `Prym.Web/MUDBLAZOR_AUDIT.md:3`: cambiare `MudBlazor:** 9.2.0` → `MudBlazor:** 9.5.0`
- Aggiornare la data audit da `2025 — updated 2026-04-12` a `2025 — updated 2026-07-06`

---

#### Task D3 — Rimuovere link rotti in docs/features/README.md [P03]

**Cosa fare:**
- In `docs/features/README.md`, rimuovere le righe che puntano a file inesistenti:
  - `[Notifications & Chat System](./notifications-chat.md)`
  - `[Real-time Messaging](./NOTIFICATIONS_CHAT_IMPLEMENTATION.md)` ← verificare esistenza (esiste come `NOTIFICATIONS_CHAT_IMPLEMENTATION.md` nella stessa dir)
  - `[Chat UI Components](./NOTIFICATIONS_CHAT_UI_IMPLEMENTATION.md)` ← idem
  - `[SignalR Integration](./signalr-integration.md)` ← non esiste
  - `[Retail Cart Session](./retail-cart.md)` ← non esiste
  - `[Document Management](./document-management.md)` ← non esiste
  - `[Workflow System](./workflow-system.md)` ← non esiste
  - `[Barcode Integration](./barcode-integration.md)` ← non esiste (esiste `BARCODE_INTEGRATION_GUIDE.md`)
  - `[Cross-Platform Barcode](./BARCODE_CROSS_PLATFORM_GUIDE.md)` ← esiste, mantenere
  - `[Printing System](./printing-system.md)` ← non esiste
  - `[Custom Theming](./theming-advanced.md)` ← non esiste
  - `[Responsive Components](./responsive-components.md)` ← non esiste
  - `[Accessibility Features](./accessibility-features.md)` ← non esiste
  - `[Performance Optimization](./performance-features.md)` ← non esiste

**Azione:** per i file effettivamente esistenti (es. `NOTIFICATIONS_CHAT_IMPLEMENTATION.md`, `BARCODE_CROSS_PLATFORM_GUIDE.md`) correggere il link al nome reale del file. Per quelli inesistenti, rimuovere la riga o sostituire con `[Nome Feature] — documentazione da creare`.

**Come verificare:** `find docs/features/ -name "*.md" | sort` — ogni link nel README corrisponde a un file esistente.

---

#### Task D4 — Rimuovere commento pagination placeholder in NotificationCenter.razor [P14]

**Cosa fare:**
- In `Prym.Web/Pages/Notifications/NotificationCenter.razor:259`:
  Cambiare `<!-- Pagination placeholder -->` → `<!-- Paginazione -->`

---

#### Task D5 — Aggiornare ADR-CLOSEDXML-MIGRATION.md [P18]
(Task eseguito automaticamente come parte di Task A1 — vedi sopra)

---

### FASE E — Debito tecnico residuo (bassa urgenza)
**Obiettivo:** segnalare e documentare i debiti tecnici che richiedono decisione architetturale prima dell'esecuzione.

#### Task E1 — Roadmap rimozione Product.ImageUrl obsoleto [P11]

**Contesto:** `Product.ImageUrl` è marcato `[Obsolete]` ma ancora usato in 7 punti di `ProductService.cs` con `#pragma warning disable CS0618`. La migrazione verso `ImageDocumentId` (Issue #315) è completa per le entità Store, ma `Product.ImageUrl` è ancora il campo attivo per i prodotti.

**Cosa fare (piano, non esecuzione immediata):**
1. Verificare se tutti i consumer di `Product.ImageUrl` sul client (`Prym.Web`) sono stati migrati a `ImageDocumentId` o se leggono ancora `ImageUrl`
2. Se ci sono consumer client ancora su `ImageUrl`: completare la migrazione UI prima di rimuovere il campo server
3. Definire una migration SQL per rimuovere la colonna `ImageUrl` dalla tabella `Products`
4. Rimuovere le 7 occorrenze `#pragma warning disable CS0618` e il campo stesso

> **⚠️ Checkpoint umano:** la rimozione di `ImageUrl` da `Products` è un breaking change sul modello dati. Richiede migration SQL, aggiornamento di tutti i DTO, e test di regressione completo.

---

#### Task E2 — Pulire metodi Obsolete in ISaleSessionService [P10]

**Cosa fare:**
- Verificare se `GetActiveSessionsAsync` e `GetSessionsByOperatorAsync` sono ancora chiamati da qualche controller/service/client
- Se non usati: rimuoverli dall'interfaccia e dall'implementazione
- Se ancora usati: pianificare la deprecazione formale con data target

**Dove:** `EventForge.Server/Services/Sales/ISaleSessionService.cs`, implementazione corrispondente, consumer in Prym.Web

---

### FASE F — TODO cosmetic (backlog)
I seguenti TODO sono documentati e non urgenti. Vanno tenuti nel backlog fino a quando le feature dipendenti saranno disponibili:

- **P15** (`FiscalPrintersDashboard.razor`) — richiede supporto SignalR in EFTable; non pianificabile ora
- **P16** (`InventoryProcedure.razor`) — allineamento header con ManagementPageHeader; XS effort, schedulabile in qualsiasi sprint
- **P17** (`InventoryMerge.razor`) — richiede archetype WizardPage; non pianificabile ora

---

## 5. Timeline e Milestone

### Milestone 1 — Sicurezza e compliance licenze
**Contenuto:** Task A1 (EPPlus → ClosedXML), Task A2 (credenziali git)  
**Priorità:** eseguire entro il prossimo sprint  
**Criterio di uscita:**
- Build passa senza dipendenza EPPlus
- `grep -rn "pass123!\|OfficeOpenXml\|ExcelPackage" .` → 0 risultati nei file tracciati
- Export Excel funzionante su almeno 3 endpoint (BusinessParties, Products, DocumentHeaders)

**Punto di verifica:** dopo Milestone 1, eseguire `dotnet build` + `dotnet test` full suite e confermare 0 regressioni.

> **⚠️ Checkpoint umano obbligatorio prima di Task A2:** concordare con il team il processo di gestione delle credenziali dev (user secrets, variabili d'ambiente, o altro) per non interrompere il flusso di sviluppo locale.

---

### Milestone 2 — Pulizia strutturale
**Contenuto:** Task B1 (file orfano), Task B2 (archive SHIFTS docs), Task B3 (banner AUDIT_REPORT), Task C1 (FluentValidation)  
**Criterio di uscita:**
- `ls Server/` → not found
- `ls SHIFTS_*.md` → not found
- FluentValidation.AspNetCore rimosso o allineato a v12
- Build e test passano

**Punto di verifica:** review manuale del diff git — verificare che nessun file funzionale sia stato rimosso per errore.

---

### Milestone 3 — Allineamento documentazione
**Contenuto:** Task D1–D5  
**Criterio di uscita:**
- Nessun link rotto in `docs/features/README.md`
- Commenti "mock" rimossi
- Versione MudBlazor corretta nei doc
- Commento pagination aggiornato

**Punto di verifica:** review umana rapida dei file modificati.

---

### Milestone 4 — Debito tecnico strategico ✅ COMPLETATA 2026-07-06
**Contenuto:** Task E1 (ImageUrl roadmap), Task E2 (Obsolete methods)  
**Stato:** IMPLEMENTATA — vedi PR Milestone 4.

**Completato:**
- P10 ✅: `GetActiveSessionsAsync` e `GetOperatorSessionsAsync` rimossi da `ISaleSessionService`, `SaleSessionService`, `SalesController`. Client `SalesService.cs` migrato agli endpoint paginati `pos-sessions/open` e `pos-sessions/operator/{id}`.
- P11 ✅ (parziale): `ImageUrl` rimosso da tutti i DTO (`ProductDto`, `CreateProductDto`, `UpdateProductDto`, `ProductDetailDto`), validator, `ProductService.cs`, UI Blazor (`ProductPreviewCard.razor`, `ProductQuickInfo.razor`, `DocumentRowDialog.razor.cs`, `QuickCreateProductDialog.razor`). Zero pragma CS0618 residui nei file server. `UpdateProductImageAsync` (dead code) rimosso.

**Pendente (checkpoint umano):**
- Eseguire `Migrations/20260706_RemoveImageUrlFromProducts.sql` per rimuovere la colonna DB `Products.ImageUrl`. Prerequisiti: verificare che nessun client esterno usi il campo; eseguire la query di validazione nel file SQL.
- Dopo la migration SQL approvata: rimuovere `[Obsolete]` e il campo `Product.ImageUrl` dall'entità `EventForge.Server/Data/Entities/Products/Product.cs`.

> **⚠️ Checkpoint umano per la rimozione DB:** la rimozione di `Products.ImageUrl` è un breaking change sul DB. Deve essere approvata da chi gestisce le installazioni in produzione. La migration SQL è pronta in `Migrations/20260706_RemoveImageUrlFromProducts.sql`.

---

### Milestone 5 — Backlog cosmetic (schedulare in sprint futuri)
**Contenuto:** Task F (P15, P16, P17)  
**Nota:** P16 (InventoryProcedure header) può essere eseguito in qualsiasi sprint senza dipendenze. P15 e P17 richiedono feature EFTable/WizardPage da pianificare.

---

## 6. Appendice: Rischi Aperti e Assunzioni

### Rischi aperti

| Rischio | Probabilità | Impatto | Mitigazione |
|---|---|---|---|
| ExportService.cs migrazione rompe output Excel in produzione | Bassa | Alto | Test manuale del file Excel generato prima del deploy |
| Rimozione credenziali dev rompe CI/CD pipeline se usa appsettings.json direttamente | Media | Alto | Verificare i workflow CI prima di Task A2; aggiungere secrets a GitHub Actions se necessario |
| FluentValidation.AspNetCore rimozione disabilita silenziosamente alcuni validatori | Bassa | Alto | Run test suite completo dopo C1; test manuale endpoint con payload invalidi |
| `Product.ImageUrl` ancora in uso in client non audita | Media | Medio | Audit grep su Prym.Web prima di E1 |

### Assunzioni

1. Il deployment di produzione NON usa `appsettings.json` così com'è nel repo; usa variabili d'ambiente o file di configurazione separati non tracciati. **Se questa assunzione è falsa, Task A2 ha impatto immediato su produzione.**
2. `Server/Data/EntityConfigurations/TransferOrderConfiguration.cs` è effettivamente un file orfano e non è incluso in nessun progetto tramite wildcard glob nel `.csproj`. **Verificare prima di B1 con `dotnet build` in modalità verbose.**
3. Le chiavi `SyncfusionLicenseKey` e `BoldReportsLicenseKey` in `appsettings.json` sono chiavi di sviluppo/community e non licenze di produzione. Se sono licenze di produzione, vanno trattate come segreti e rimosse immediatamente dal file tracciato in git.
4. `FluentValidation.AspNetCore` non è direttamente importato in nessun file `.cs` con `using` esplicito — da verificare prima di C1.

### Problemi non pianificabili (richiedono decisione esterna)

- **P15** e **P17**: dipendono da feature EFTable (SignalR real-time) e WizardPage archetype non ancora in roadmap. Nessuna data proposta.
- **E1 completo** (rimozione `Product.ImageUrl`): richiede decisione sul ciclo di vita del campo, migration SQL approvata, e coordinamento con eventuali installazioni in produzione che usano il campo legacy.
