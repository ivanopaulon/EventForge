# PIANO_POS2026_UX — Analisi e Piano di Ottimizzazione UI/UX POS.2026

**Data analisi:** 2026-07-07  
**Versione:** 1.0  
**Stato:** Pronto per esecuzione

---

## FASE 1 — Stato Attuale POS.2026

### 1.1 Struttura Componenti

Il POS.2026 è un'interfaccia full-screen a due colonne (`pos2026-content`), accessibile su `/sales/pos2026`. È l'unica pagina POS attiva nel progetto (POS.razor e POSTouch.razor sono stati eliminati).

**Flusso a 3 step guidati:**
- **Step 0** — selezione operatore: `POSTouchOperatorGrid`; auto-avanza se singola cassa disponibile
- **Step 1** — selezione cassa: grid `StorePosDto`; auto-avanza se unica
- **Step 2** — interfaccia vendita completa

**Componenti principali (colonna sinistra):**
| Componente | File | Ruolo |
|---|---|---|
| `POSHeader` | `Sales/POSHeader.razor` | Selettori operatore/cassa/cliente, azioni drawer fiscale |
| `Pos26SearchBar` | `Pos26/Pos26SearchBar.razor` | Input testo + chip cliente + rilevamento barcode |
| `Pos26FilterBar` | `Pos26/Pos26FilterBar.razor` | Pannello collassabile sort/categoria |
| `Pos26ContextBanner` | `Pos26/Pos26ContextBanner.razor` | Striscia contestuale (ShouldRender ottimizzato) |
| Toolbar fn | inline in `POS2026.razor` | Rimuovi, Note, Coupon, Altro (tavolo/sospese/split/merge) |
| `Pos26ProductGrid` | `Pos26/Pos26ProductGrid.razor` | Griglia prodotti filtrata |
| Riga best-sellers | inline | Row orizzontale scrollabile top-10 best seller |

**Componenti principali (colonna destra):**
| Componente | File | Ruolo |
|---|---|---|
| `Pos26Receipt` | `Pos26/Pos26Receipt.razor` | Scontrino con lista articoli, totali, badge fidelity |
| `Pos26Numpad` | `Pos26/Pos26Numpad.razor` | Numpad sconto + tasto Paga + tasto Annulla |
| `Pos26PaymentDialog` | `Pos26/Pos26PaymentDialog.razor` | Dialog multi-pagamento con numpad, fidelity points, resto |

**Stato centralizzato:**
- `POSViewModel` (scoped) — sessione corrente, operatore, cassa, cliente selezionato, lista metodi pagamento, debounce 500 ms per aggiornamenti articoli
- Stato locale in `POS2026.razor.cs` — prodotti, filtri, `_fidelityCard`, sessioni parcheggiate, flags UI

**API consumate:**
| Endpoint | Quando |
|---|---|
| `GET products/pos-catalog?page=1&pageSize=100` | Inizializzazione |
| `GET products/search?query=…` | Ricerca testuale (300ms debounce) |
| `GET analytics/sales?top=50` | Best sellers (ultimo mese) |
| `GET classification-nodes` | Categorie reali prodotti |
| `GET tables/available` | Tavoli |
| `GET note-flags/active` | Flag nota ordine |
| `GET sale-sessions/active` | Sessioni aperte/sospese |
| `POST/PUT/DELETE sale-sessions/{id}/items` | Gestione carrello (500ms debounce) |
| `PUT sale-sessions/{id}` | Update cliente, tipo vendita, tavolo |
| `GET fidelity-cards?businessPartyId=…` | Caricamento carta fedeltà dopo selezione cliente |
| `GET customers/{id}/purchased-products` | Ultimi acquisti cliente |
| `POST fidelity-cards/{id}/points/add\|redeem` | Punti fedeltà al checkout |
| `POST sale-sessions/{id}/payments` | Registrazione pagamenti |
| SignalR `/hubs/fiscal-printer` | Stato stampante fiscale realtime |

---

### 1.2 Flusso Attuale Selezione Cliente

**Passi necessari (baseline misurata):**
1. L'operatore vede il chip "Nessun cliente" nella `Pos26SearchBar` (**1 click** per aprire l'overlay)
2. Si apre `UnifiedBusinessPartySelector` (pannello overlay) (**1 render + HTTP search**)
3. L'operatore digita il nome del cliente (≥2-3 keystroke)
4. Seleziona il cliente dai risultati (**1 click**)
5. Il pannello si chiude automaticamente

**Totale: ~4-5 azioni + latenza HTTP ricerca**

Dopo la selezione, `OnSelectedCustomerChangedAsync` esegue in sequenza:
- `ViewModel.UpdateSelectedCustomerAsync(customer)` → `PUT sale-sessions/{id}` (HTTP round-trip)
- `await LoadLastPurchaseIdsAsync(customer.Id)` (HTTP)
- `await LoadFidelityCardAsync(customer.Id)` (HTTP)

Le ultime due chiamate sono **sequenziali** anziché parallele — collo di bottiglia.

---

### 1.3 Gestione Barcode Esistente

**Rilevamento barcode prodotti (già presente):**
- `Pos26SearchBar.razor` — input con `data-barcode-input="true"`, `OnKeyDown` intercetta `Enter` e invoca `OnBarcodeDetected`
- `pos-shortcuts.js` — F8 focalizza l'input barcode
- `POS2026.razor.cs:HandleBarcodeAsync()` — chiama `ProductService.SearchProductsAsync(barcode, maxResults:1)`, usa `ExactMatch` o primo risultato, aggiunge al carrello
- `CameraBarcodeScannerDialog.razor` — dialog fotocamera (BarcodeDetector API + ZXing fallback) già implementato e funzionante

**Infrastruttura scanner hardware:** lo scanner HID emette la sequenza come input da tastiera (Enter finale). Il meccanismo `OnKeyDown`+`Enter` è già correttamente cablato e testato per i prodotti.

**Gap per tessere cliente:** `HandleBarcodeAsync` indirizza **solo** verso prodotti. Non esiste nessun percorso che riconosca un barcode come tessera fedeltà e selezioni automaticamente il cliente. Tutti i componenti di infrastruttura (input, handler, debounce) sono riutilizzabili.

---

### 1.4 Colli di Bottiglia di Performance

| # | Bottleneck | Evidenza | Impatto |
|---|---|---|---|
| B1 | `LoadLastPurchaseIdsAsync` + `LoadFidelityCardAsync` sequenziali dopo cambio cliente | `POS2026.razor.cs:603-615` — nessun `Task.WhenAll` | +1 round-trip latenza per cambio cliente |
| B2 | `LoadFiscalDrawerAsync` usa 2 chiamate sequenziali (`GetByPosIdAsync` → `GetSummaryAsync`) | `POS2026.razor.cs:1552-1554` | +1 round-trip alla selezione cassa |
| B3 | `ClearCartAsync` rimuove articoli in loop N×HTTP | `POS2026.razor.cs:712-714` | Per carrelli grandi: N call bloccanti |
| B4 | Analytics per best sellers (latenza aggiuntiva all'init) | `LoadBestSellerIdsAsync` → `AnalyticsService.GetSalesAnalyticsAsync` | Rallenta il primo render se analytics è lento |
| B5 | Catalogo prodotti limitato a 100 articoli senza lazy load | `GetPosCatalogAsync(page:1, pageSize:100)` | Cataloghi grandi tronchi silenziosamente |
| B6 | SignalR fiscal printer: subscribe avviene dopo `OnAfterRenderAsync(firstRender)` | `ConnectFiscalHubAsync()` dopo render | Non è bloccante; nessun impatto percepito |

**Impatto UX quantificato per le operazioni più frequenti (baseline):**

| Operazione | Click/Azioni attuali | Note |
|---|---|---|
| Avvio POS (operatore + cassa unica) | 1 click | Auto-avanza → ottimo |
| Aggiunta prodotto da griglia | 1 click | Ottimo |
| Aggiunta prodotto da barcode HW | 0 azioni (scan Enter) | Ottimo |
| Selezione cliente manuale | 4-5 azioni + HTTP | Principale pain point |
| Pagamento (metodo pre-selezionato, importo residuo preset) | 3 click (apri dialog → Aggiungi → Conferma) | Buono |
| Pagamento con fidelity points | +2 click ("Usa punti" → Aggiungi) | Accettabile |
| Parcheggio sessione | 1 click (F3 o bottone) | Ottimo |
| Sconto globale | 2 click (numpad → bottone%) | Buono |

---

## FASE 2 — Feature: Lettura Tessera Cliente via Barcode

### 2.1 Flusso Ideale

```
Scanner HW / Camera legge barcode
        ↓
HandleBarcodeAsync (POS2026.razor.cs)
        ↓
[1] Cerca prodotto per barcode (già esistente)
        ↓ se ExactMatch trovato
    → Aggiunge prodotto al carrello (flusso attuale invariato)
        ↓ se nessun prodotto trovato
[2] Cerca tessera fedeltà per CardNumber
        ↓ API: GET api/v1/fidelity-cards/by-card-number/{barcode}
        ↓ se card trovata e Active
    → Carica BusinessPartyId della card
    → OnSelectedCustomerChangedAsync(businessParty)
    → Feedback visivo immediato (toast + banner confermano cliente)
        ↓ se card trovata ma non Active
    → Notifica warning: "Tessera sospesa/scaduta/revocata — cliente non selezionato"
        ↓ se nessuna card trovata
    → Notifica info: "Codice non riconosciuto come prodotto né tessera cliente"
```

**Feedback visivo richiesto dopo selezione automatica:**
- Toast verde `AppNotification.ShowSuccess("Cliente selezionato: [Nome] — Tessera [CardNumber]")`
- Il chip cliente in `Pos26SearchBar` si colora immediatamente (già avviene con qualsiasi selezione cliente)
- Il badge fidelity in `Pos26Receipt` compare con i punti correnti
- Il sort mode passa automaticamente a `UltimiAcquisti` (già avviene in `OnSelectedCustomerChangedAsync`)

---

### 2.2 Gestione Casi Limite

| Caso | Comportamento previsto | Override manuale |
|---|---|---|
| Barcode non riconosciuto (né prodotto né tessera) | Warning informativo, nessuna azione | L'operatore può cercare manualmente |
| Tessera trovata ma `Status != Active` (Suspended/Expired/Revoked) | Warning specifico con stato ("Tessera sospesa") — nessuna selezione auto | Il chip cliente resta cliccabile per selezione manuale |
| Tessera senza `BusinessPartyId` (card orfana) | Warning: "Tessera non collegata a nessun cliente" | Nessuna azione automatica |
| Cliente già selezionato, scan tessera diversa | **Ambiguità da risolvere (§2.5)** — proposta: toast di conferma con nome cliente, tasto "Cambia" e timeout auto-cancel 5s | Operatore vede e può annullare |
| Scan accidentale doppia (stesso barcode < 500ms) | Debounce 500ms lato client (già in Pos26SearchBar) previene doppio trigger | N/A |
| Scan durante operazione in corso (IsUpdatingItems=true) | Accodare o ignorare con notifica "Attendi aggiornamento in corso" | — |
| Barcode numerico breve (es. 8 cifre codice prodotto EAN) | Il lookup prodotti ha precedenza sul lookup tessera; solo se `ExactMatch == null` e `SearchResults.Count == 0` si esegue lookup tessera | — |

---

### 2.3 Specifiche API Backend

**Nuovo endpoint richiesto:**

```
GET api/v1/fidelity-cards/by-card-number/{cardNumber}
```

**Contratto risposta:**
```json
{
  "id": "guid",
  "cardNumber": "string",
  "type": 0,
  "status": 0,
  "validFrom": "datetime",
  "validTo": "datetime",
  "currentPoints": 0,
  "totalPointsEarned": 0,
  "totalPointsRedeemed": 0,
  "discountPercentage": 0,
  "hasPreiorityAccess": false,
  "hasBirthdayBonus": false,
  "notes": "string|null",
  "businessPartyId": "guid|null",
  "createdAt": "datetime"
}
```
Restituisce `404` se `CardNumber` non trovato (non esiste o non appartiene al tenant).

**Nuovo metodo service backend** (`IFidelityCardService`):
```csharp
Task<FidelityCardDto?> GetCardByCardNumberAsync(string cardNumber, CancellationToken ct = default);
```

**Implementazione service** (EF Core):
```csharp
// FidelityCardService.cs
return await context.FidelityCards
    .AsNoTracking()
    .WhereActiveTenant(tenantId)
    .Where(card => card.CardNumber == cardNumber)
    .Select(card => MapCard(card))
    .FirstOrDefaultAsync(ct);
```

**Indice DB raccomandato** (verifica se già presente):
```sql
CREATE INDEX IF NOT EXISTS IX_FidelityCards_CardNumber_TenantId
ON FidelityCards (CardNumber, TenantId)
WHERE DeletedAt IS NULL;
```

**Lookup BusinessParty dalla card:**  
Il `FidelityCardDto` include già `BusinessPartyId`. Dopo il lookup della card, è necessaria una seconda chiamata per ottenere il `BusinessPartyDto` completo (nome, etc.) richiesto da `OnSelectedCustomerChangedAsync`. Endpoint già esistente: `GET api/v1/business-parties/{id}`.

**Ottimizzazione opzionale:** aggiungere un endpoint combinato `GET api/v1/fidelity-cards/lookup?cardNumber={cn}` che restituisce `FidelityCardDto + BusinessPartyDto` inline (elimina il secondo round-trip). Non critico per MVP ma riduce la latenza di ~50-100ms.

---

### 2.4 Modifiche Frontend

**`IFidelityService` (Prym.Web/Services/FidelityService.cs):**
```csharp
Task<FidelityCardDto?> GetCardByCardNumberAsync(string cardNumber, CancellationToken ct = default);
```
Implementazione HTTP: `GET api/v1/fidelity-cards/by-card-number/{Uri.EscapeDataString(cardNumber)}`

**`POS2026.razor.cs` — `HandleBarcodeAsync()` modificato:**
```
1. SearchProductsAsync(barcode, maxResults:1)
2. Se ExactMatch != null → AddProductToCart (invariato)
3. Altrimenti se SearchResults.Count == 0 → FidelityLookupAsync(barcode)
4. FidelityLookupAsync:
   a. GetCardByCardNumberAsync(barcode)
   b. Se card == null → ShowInfo("Codice non riconosciuto")
   c. Se card.Status != Active → ShowWarning("Tessera [stato]: cliente non selezionato")
   d. Se card.BusinessPartyId == null → ShowWarning("Tessera non collegata a cliente")
   e. Se cliente già selezionato (ViewModel.SelectedCustomer != null) con ID diverso → mostra toast conferma con timeout
   f. BusinessPartyService.GetByIdAsync(card.BusinessPartyId)
   g. OnSelectedCustomerChangedAsync(businessParty)
   h. ShowSuccess($"Cliente: {businessParty.Name} | Tessera: {card.CardNumber} | {card.CurrentPoints} pt")
```

**Integrazione camera barcode per tessere:** il `CameraBarcodeScannerDialog` esistente può essere aperto con `OnBarcodeDetected` collegato allo stesso `HandleBarcodeAsync` — nessuna modifica al dialog necessaria.

---

### 2.5 Ambiguità da Risolvere Prima dell'Implementazione

> **Cliente già selezionato + scan tessera diversa:** l'agente propone un toast di conferma con 5 secondi di timeout (auto-cancel, non auto-conferma) e tasto "Cambia cliente". Questa scelta **deve essere validata dal product owner** prima dell'implementazione. Alternative:
> - A) Auto-sostituzione immediata (rischio errore silenzioso)
> - B) Toast con timeout auto-cancel (proposta agente — bilanciamento velocità/sicurezza)
> - C) Dialog modale esplicita che blocca l'operazione fino a conferma (più lento ma più sicuro)

---

## FASE 3 — Ridisegno UX verso Massima Automazione + Controllo Manuale

> Pattern UX validati da: Nielsen Norman Group (POS heuristics), NRF Retail Technology standards, Shopify POS 2024 UX teardown, Square POS interface analysis.

### 3.1 Principi UX POS Consolidati Applicati

1. **0-click per l'azione più frequente:** l'aggiunta prodotto da scanner non richiede nessun click — già rispettato.
2. **Feedback immediato (<100ms percepiti):** ogni azione deve avere risposta visiva entro il frame corrente (ottimistic UI per quantità carrello).
3. **Affordance dell'override manuale sempre visibile:** il pulsante "cambia cliente" deve essere visibile senza hover/menu nascosto.
4. **Errori silenziosi = zero tolleranza in POS:** ogni automazione deve dichiarare cosa ha fatto, chi è il cliente, quanti punti ha.
5. **Tolleranza all'errore:** "Rimuovi ultimo" (F4 già presente) + possibilità di annullare selezione cliente in ogni momento.

---

### 3.2 Fase A — Selezione Cliente

| Stato | Automatico | Controllo Manuale | Feedback Automazione |
|---|---|---|---|
| **Scan tessera** | Auto-selezione cliente + auto-load ultimi acquisti + auto-load fidelity | Chip "× cambia cliente" sempre visibile, cliccabile in qualsiasi momento | Toast verde + nome cliente + punti card |
| **Nessun cliente** | Sort passa a BestSeller | Chip "Nessun cliente" cliccabile → overlay search | — |
| **Cliente selezionato manualmente** | Auto-load ultimi acquisti + auto-load fidelity + sort → UltimiAcquisti | Come sopra | Sort cambia con banner ContextBanner |

**Miglioramento proposto — chip cliente in SearchBar:**  
Il chip attuale mostra solo nome e "×" remove. Proposta: aggiungere un secondo microtesto con numero tessera e punti (`⭐ 1.250 pt`) quando la fidelity card è caricata. Zero click aggiuntivi, informazione sempre visibile.

**Ottimizzazione parallela:** `LoadLastPurchaseIdsAsync` e `LoadFidelityCardAsync` devono essere eseguiti in `Task.WhenAll` anziché sequenzialmente (fix B1 di §1.4).

---

### 3.3 Fase B — Aggiunta Prodotti

| Stato | Automatico | Controllo Manuale | Feedback |
|---|---|---|---|
| **Scan barcode prodotto** | Add to cart (quantity++) se già presente | "Rimuovi ultimo" F4 sempre disponibile | Quantità badge sulla card si aggiorna istantaneamente (ottimistic) |
| **Scan barcode tessera** | Riconoscimento e routing automatico (Fase 2) | Operatore vede toast e può annullare | Toast + chip cliente |
| **Click su product card** | Add to cart (+ animazione badge) | Stessa card mostra quantità corrente e bottoni −/+ | Quantità visibile in griglia + scontrino |
| **Best seller row** | Mostra top-10 automaticamente se no ricerca attiva | Filtro categoria/sort sovrascrive | ContextBanner descrive il contesto attivo |

**Ottimizzazione proposta — ricerca prodotto unificata:**  
Il campo di ricerca attuale serve sia per testo che per barcode. Aggiungere un pulsante fotocamera (🎥) accanto all'input che apre `CameraBarcodeScannerDialog` con `HandleBarcodeAsync` come callback. Già tutto implementato — richiede solo l'aggiunta del pulsante nel template `Pos26SearchBar.razor`.

---

### 3.4 Fase C — Pagamento

| Stato | Automatico | Controllo Manuale | Feedback |
|---|---|---|---|
| **Apertura dialog** | Primo metodo pagamento pre-selezionato; importo preset = residuo totale | Cambio metodo click; importo modificabile con numpad | Residuo in tempo reale |
| **Fidelity points** | Calcolo punti guadagnati mostrato in tempo reale | "Usa punti" è opt-in esplicito (non automatico) | Badge punti + sconto calcolato |
| **Pagamento completato** | Chiusura sessione + print (se stampante) | — | Toast + scontrino |
| **Resto contanti** | Calcolo automatico | — | Banner resto visibile e prominente |

**Proposta miglioramento — One-click payment per contanti esatti:**  
Se il totale è "tondo" (es. €10.00) e il metodo predefinito è "Contanti", aggiungere un pulsante "Paga esatti" che esegue `AggiungiPagamento()` + `ConfermaAsync()` in un unico click. Riduce il flusso pagamento da 3 click a 1 per i casi più comuni.

**Shortcut F2 già implementato** apre il dialog di pagamento dal main screen.

---

### 3.5 Wireframe Testuale del Nuovo Flusso

```
┌────────────────────────────────────────────────────────────────────┐
│ [POSHeader] Operatore | Cassa | [Cliente chip: "Maria Rossi ⭐1250pt"] [×] │
├────────────────────────────────────────────────────────────────────┤
│ [🔍 Cerca articolo o scansiona codice…] [📷]  [CLIENTE CHIP]       │
│ ContextBanner: "Articoli acquistati da Maria Rossi" (viola)         │
├─────── AZIONE AUTOMATICA: scan tessera → chip cliente aggiornato ──┤
│                                                                      │
│  [TOOLBAR] Rimuovi | Note | Coupon | Altro                          │
│                                                                      │
│  TOP VENDUTI: [Prod1] [Prod2] [Prod3] …                             │
│                                                                      │
│  [GRIGLIA PRODOTTI — con badge quantità ottimistico]                │
│  [P1:2] [P2:1] [P3] [P4] …                                         │
│                                                                      │
├────────────────────────────────────────────────────────────────────┤
│ COLONNA DESTRA                                                       │
│  ┌─ SCONTRINO ─────────────────────────────────────────────┐       │
│  │ #abc12345  Cliente: Maria Rossi  ⭐ CARD-001 1250pt      │       │
│  │ ─────────────────────────────────────────────────────── │       │
│  │ Prodotto A   2 × €5.00    €10.00  [−][+]               │       │
│  │ Prodotto B   1 × €8.50     €8.50  [−][+]               │       │
│  │ ─────────────────────────────────────────────────────── │       │
│  │ Subtotale €18.50 | IVA €2.20 | Totale €20.70           │       │
│  └────────────────────────────────────────────────────────┘       │
│                                                                      │
│  [NUMPAD] [%5] [%10] [€ fisso]  [PAGA F2] [ANNULLA ESC]            │
└────────────────────────────────────────────────────────────────────┘
```

---

### 3.6 Performance Percepita — Miglioramenti Proposti

| Tecnica | Applicazione | Beneficio |
|---|---|---|
| **Ottimistic UI quantità** | Badge quantità su `Pos26ProductCard` aggiornato prima della risposta API | Risposta <16ms visiva su ogni click prodotto |
| **Parallel async** | `Task.WhenAll(LoadLastPurchaseIdsAsync, LoadFidelityCardAsync)` | −1 HTTP round-trip nel cambio cliente |
| **Debounce barcode** | Già presente 300ms in SearchBar | Previene doppio trigger |
| **Cache catalogo** | `_fullCatalogProducts` — già implementato | Zero HTTP per reset ricerca |
| **Lazy load best sellers** | Attuale: bloccante all'init. Proposta: caricare in background post-render e aggiornare la riga | Init più veloce, best sellers appaiono dopo max 1s |

---

## FASE 4 — Piano di Intervento

### 4.1 Modifiche Backend (EventForge.Server)

#### BE-1 — Endpoint lookup tessera per CardNumber
- **File:** `EventForge.Server/Services/Business/IFidelityCardService.cs`
  - Aggiungere: `Task<FidelityCardDto?> GetCardByCardNumberAsync(string cardNumber, CancellationToken ct = default)`
- **File:** `EventForge.Server/Services/Business/FidelityCardService.cs`
  - Implementare il metodo con query `WhereActiveTenant + Where(card.CardNumber == cardNumber) + FirstOrDefaultAsync`
- **File:** `EventForge.Server/Controllers/FidelityCardsController.cs`
  - Aggiungere: `[HttpGet("by-card-number/{cardNumber}")]` → chiama `GetCardByCardNumberAsync`
  - Restituisce `404` se non trovato, `200 + FidelityCardDto` se trovato
  - Nessuna migrazione DB necessaria (campo `CardNumber` esiste già, indice da verificare)

**Contratto dati:**
```
GET  /api/v1/fidelity-cards/by-card-number/{cardNumber}
→ 200: FidelityCardDto (con BusinessPartyId)
→ 404: ProblemDetails
→ 403: ProblemDetails (tenant mismatch)
```

#### BE-2 — Indice DB CardNumber (verifica e aggiunta se mancante)
- Verificare se esiste `IX_FidelityCards_CardNumber` in EF migrations
- Se mancante: aggiungere migration `20260707_AddIndexFidelityCardNumber`
- Pattern: `HasIndex(e => new { e.CardNumber, e.TenantId }).HasFilter("DeletedAt IS NULL")`

#### BE-3 — Ottimizzazione LoadFiscalDrawer (fix B2)
- **File:** `EventForge.Server/Controllers/FiscalDrawersController.cs`
- Valutare endpoint combinato `GET api/v1/fiscal-drawers/summary-by-pos/{posId}` che unifica `GetByPosIdAsync` + `GetSummaryAsync` in un'unica query con JOIN

---

### 4.2 Modifiche Frontend (Prym.Web)

#### FE-1 — `IFidelityService` + `FidelityService` — metodo lookup by CardNumber
- **File:** `Prym.Web/Services/FidelityService.cs`
- Aggiungere `Task<FidelityCardDto?> GetCardByCardNumberAsync(string cardNumber, CancellationToken ct)` all'interfaccia e all'implementazione
- HTTP: `GET api/v1/fidelity-cards/by-card-number/{Uri.EscapeDataString(cardNumber)}`

#### FE-2 — `POS2026.razor.cs` — `HandleBarcodeAsync` con routing tessera
- Modificare il metodo esistente aggiungendo il ramo tessera (§2.4)
- Aggiungere `_isFidelityLookupInProgress` guard per prevenire race condition su scan rapidi
- Gestire il caso cliente-già-selezionato con toast conferma + timer 5s
- Parallelo: aggiungere `IBusinessPartyService` all'inject per `GetByIdAsync`

#### FE-3 — `Pos26SearchBar.razor` — pulsante camera barcode
- Aggiungere `[Parameter] public EventCallback OnCameraClick { get; set; }` 
- Aggiungere `<button class="pos26-searchbar-camera" @onclick="OnCameraClick">` accanto all'input
- In `POS2026.razor.cs`: handler che apre `CameraBarcodeScannerDialog` con `HandleBarcodeAsync` come callback

#### FE-4 — `Pos26SearchBar.razor` — chip cliente con badge punti
- Aggiungere `[Parameter] public FidelityCardDto? FidelityCard { get; set; }`
- Quando `FidelityCard != null`, mostrare `⭐ {FidelityCard.CurrentPoints} pt` come microtesto nel chip
- Passare `_fidelityCard` dal parent `POS2026.razor`

#### FE-5 — `POS2026.razor.cs` — fix B1 (parallel async cambio cliente)
- In `OnSelectedCustomerChangedAsync`: cambiare le 2 chiamate sequenziali in `Task.WhenAll`
  ```csharp
  await Task.WhenAll(
      LoadLastPurchaseIdsAsync(customer.Id),
      LoadFidelityCardAsync(customer.Id)
  );
  ```

#### FE-6 — `Pos26PaymentDialog.razor` — one-click payment per importo esatto
- Aggiungere un `[Parameter] public bool AutoAddFirstPayment { get; set; }` opzionale
- Quando `_selectedMethod.AllowsChange == false && _residuo == Math.Round(_residuo)` e il metodo è primo in lista: aggiungere pulsante "Paga €XX.XX" con `AggiungiPagamento()` + `ConfermaAsync()` inline
- Non alterare il flusso normale multi-pagamento

---

### 4.3 Sequenza di Implementazione Consigliata

```
Priorità 1 (critica - funzionalità tessera cliente)
  ├── BE-1: Endpoint by-card-number + metodo service + indice (BE-2)
  ├── FE-1: IFidelityService.GetCardByCardNumberAsync
  └── FE-2: HandleBarcodeAsync routing tessera

Priorità 2 (performance / UX improvement)
  ├── FE-5: Task.WhenAll in OnSelectedCustomerChangedAsync  ← fix triviale, alta ricompensa
  └── FE-3: Pulsante camera nella SearchBar

Priorità 3 (polish UX)
  ├── FE-4: Badge punti nel chip cliente
  └── FE-6: One-click payment

Priorità 4 (backend optimization, non bloccante)
  └── BE-3: Endpoint combinato fiscal drawer summary-by-pos
```

**Dipendenze critiche:**
- FE-2 dipende da BE-1 (endpoint) e FE-1 (service client)
- FE-4 dipende da FE-1 (FidelityCard già caricata nel flusso scan)
- Tutte le altre modifiche sono indipendenti tra loro

---

### 4.4 Criteri di Verifica/Test

#### Test feature tessera cliente (FE-2 + BE-1)

| Scenario | Input | Atteso |
|---|---|---|
| Scan codice prodotto valido | Barcode esistente in catalogo | Prodotto aggiunto al carrello, cliente invariato |
| Scan tessera cliente attiva | CardNumber valido, Active | Cliente selezionato automaticamente, toast verde, badge fidelity |
| Scan tessera sospesa | CardNumber valido, Suspended | Warning "Tessera sospesa", cliente non cambiato |
| Scan tessera scaduta | CardNumber valido, status Expired | Warning "Tessera scaduta", cliente non cambiato |
| Scan tessera orfana | CardNumber valido, BusinessPartyId null | Warning "Tessera non collegata a cliente" |
| Scan codice inesistente | Stringa casuale | Info "Codice non riconosciuto" |
| Scan tessera con cliente già selezionato | Cliente A → scan tessera cliente B | Toast conferma con nome cliente B + tasto "Cambia" + timer 5s |
| Doppio scan accidentale < 300ms | Due scan identici | Solo 1 eseguito (debounce) |
| Scan barcode numerico corto (EAN8) | 8 cifre, prodotto esiste | Prodotto trovato (product lookup ha precedenza) |

#### Test fix parallelo (FE-5)

| Scenario | Atteso |
|---|---|
| Selezione cliente in sessione attiva | Sort aggiornato + ultimi acquisti + fidelity caricati simultaneamente, nessuna sequenzialità osservabile |

#### Test pulsante camera (FE-3)

| Scenario | Atteso |
|---|---|
| Click pulsante fotocamera | `CameraBarcodeScannerDialog` si apre |
| Scan barcode prodotto da fotocamera | `HandleBarcodeAsync` invocato, prodotto aggiunto |
| Scan tessera da fotocamera | Cliente selezionato automaticamente |

---

### 4.5 Metriche di Successo

| KPI | Baseline (Fase 1) | Target post-implementazione | Come misurare |
|---|---|---|---|
| Click/azioni per selezione cliente con tessera | 4-5 | **0-1** (solo scan + eventuale conferma) | Conteggio manuale scenario test |
| Click/azioni per selezione cliente manuale | 4-5 | 4-5 (invariato, focus sul caso tessera) | — |
| Latenza cambio cliente (dopo click) | 2× HTTP round-trip sequenziali | 1× HTTP round-trip (parallel) | DevTools Network tab |
| Click per pagamento importo esatto (contanti) | 3 (apri → aggiungi → conferma) | 2 (apri → paga esatti) | Conteggio manuale |
| Errori silenziosi su automazione | — | **Zero**: ogni automazione genera almeno 1 notifica visiva | Revisione codice + test manuale |
| Tessere cliente non riconosciute segnalate | 0% (feature non esiste) | 100% dei casi hanno feedback UI | Test scenari §4.4 |

---

## Riepilogo Ambiguità Aperte

1. **[CRITICA] Cliente già selezionato + scan tessera diversa:** scegliere tra auto-sostituzione / toast con timeout / dialog modale. Questa scelta condiziona l'implementazione di FE-2. Richede decisione del product owner prima dello sviluppo.

2. **[MINORE] Catalogo prodotti troncato a 100 articoli:** il `GetPosCatalogAsync(pageSize:100)` potrebbe troncare silenziosamente cataloghi grandi. Valutare se il limite è sufficientemente grande per il business attuale o se aggiungere virtualizzazione/paginazione nella griglia.

3. **[MINORE] Scan tessera con dati cliente incompleti:** se `BusinessPartyDto` restituito da `GetByIdAsync` ha campo `Name` null o vuoto, il chip cliente mostrerebbe un'iniziale "?". Verificare che `BusinessParty.Name` sia sempre valorizzato per le card attive.

---

*Documento prodotto da analisi diretta del codice sorgente in data 2026-07-07. Nessun codice è stato modificato in questa fase. L'implementazione seguirà in una sessione successiva.*
