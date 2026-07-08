# Audit POS2026 — Griglia Prodotti, Best Seller, Toolbar Azioni

**Data:** 2026-07-08
**Scope:** `Prym.Web/Pages/Sales/POS2026.razor(.cs/.css)`, `Pos26ProductGrid.razor`, `Pos26ProductCard.razor`, `Pos26SearchBar.razor`, `Prym.Web/ViewModels/POSViewModel.cs`
**Stato:** SOLO LETTURA — nessun file modificato. In attesa di approvazione piani (Fase 2).

---

## A. Aggiunta al carrello di prodotti senza disponibilità

**Confermato.** Nessun controllo di stock lungo l'intero percorso di aggiunta.

| File | Riga | Evidenza |
|---|---|---|
| `Pos26ProductCard.razor` | 84-89 | `HandleTap()` invoca `OnAddToCart.InvokeAsync(Product)` incondizionatamente; `StockAvailable` non è letto. |
| `Pos26ProductCard.razor` | 166-177 | `GetStockColorClass()`/`FormatStock()` sono usati **solo** per il rendering del chip (riga 51-54), mai per bloccare/segnalare l'azione. |
| `POS2026.razor.cs` | 790-811 | `OnAddProductToCartAsync` chiama `ViewModel.AddProductAsync(product)` senza mai leggere `_stockByProductId` (dichiarato riga 44, popolato riga 312, mai riletto altrove). |
| `POSViewModel.cs` | 405-472 | `AddProductAsync` non contiene alcuna verifica su stock/disponibilità: aggiunge sempre se `CurrentSession != null`. |
| `POSViewModel.cs` | 421, 458 | `NotifySuccess($"Quantity increased: {product.Name}")` / `NotifySuccess($"Product added: {product.Name}")` — messaggi **in inglese**, identici per prodotto disponibile o esaurito. |

**Espansione:** altri messaggi in inglese nello stesso file, non citati nel prompt ma coerenti con lo stesso difetto di i18n: `"Session parked"` (349), `"Session cancelled"` (387), `"Item removed"` (642), `"Sale completed successfully!"` (735), `"Session created successfully"` (1030), oltre a vari messaggi di errore (`"No active session or invalid product"`, `"Timeout acquiring lock"`, `"Error adding product"`, ecc.). Il prompt richiede la traduzione solo dei due messaggi di `AddProductAsync`; segnalo gli altri come **fuori scope** ma nota per eventuale follow-up futuro (non toccare senza richiesta esplicita, per rispettare il vincolo "nessuna modifica oltre il previsto").

**Severità:** Alta — impatto diretto su gestione di cassa/magazzino (vendita di articoli esauriti senza alcun avviso).

---

## B. Flicker durante scroll veloce nella griglia prodotti

**Confermato.**

| File | Riga | Evidenza |
|---|---|---|
| `POS2026.razor.css` | 460 | `.pos26-product-card { min-height: 90px; }` — minimo, non altezza fissa. |
| `POS2026.razor.css` | 545-551 | `.pos26-product-name { font-size: 11px; line-height: 1.3; word-break: break-word; }` — nessun `-webkit-line-clamp`, nessuna altezza/numero di righe fissato. |
| `POS2026.razor.css` | 430-438 | Skeleton: `.pos26-skeleton-line--name` è una singola riga fissa a 10px di altezza — non rispecchia la variabilità reale del nome prodotto (contraddice il commento riga 407 "dimensioni identiche alle card reali"). |
| `Pos26ProductGrid.razor` | 32 | `<Virtualize Items="..." OverscanCount="10">` — nessun `ItemSize` fisso passato; il componente stima l'altezza media dai render precedenti. |
| `Pos26ProductCard.razor` | 160-161 | `TruncateName`: tronca a 26 caratteri + `…` solo se `Name.Length > 28` — quindi nomi fino a 28 caratteri passano interi e possono andare su più righe. |

### Verifica empirica (stima calcolata, nessun browser disponibile nel sandbox per il rendering reale)

Parametri noti dal CSS: card content width ≈ 128px (minmax minimo) − padding orizzontale (4px×2) ≈ **120px**; font-size 11px, line-height 1.3 (≈14.3px/riga); font di sistema MudBlazor (Roboto-like), larghezza media carattere latino minuscolo/maiuscolo misto ≈ 5.5–6px a 11px.

Capacità stimata per riga: 120px / 5.75px ≈ **~20-21 caratteri/riga** (variabile per punteggiatura/spazi/maiuscole).

| # | Nome (lunghezza) | Righe stimate | Note |
|---|---|---|---|
| 1 | "Pane" (4) | 1 | corto |
| 2 | "Latte 1L" (8) | 1 | corto |
| 3 | "Detersivo piatti" (17) | 1 | al limite riga singola |
| 4 | "Detersivo per piatti" (21) | 2 | supera ~20 char, va a capo su spazio |
| 5 | "Shampoo capelli grassi" (23) | 2 | 2 parole lunghe dopo wrap |
| 6 | "Confezione biscotti frollini" (28, al limite `TruncateName`) | 2-3 | dipende da punti di rottura parola; possibile 3ª riga corta |
| 7 | "Detergente multiuso agrumi" (26) | 2 | |
| 8 | "Vino rosso" (10) | 1 | |
| 9 | "Formaggio stagionato 24 mesi" (28, troncato a 26+…) | 2-3 | |
| 10 | "Uovo" (4) | 1 | |

**Conclusione:** con nomi reali di lunghezza 4–28 caratteri (limite di `TruncateName`) si osservano **almeno 3 varianti di altezza reale** del blocco nome (1, 2, 3 righe), pari a una differenza di ≈14-15px/riga extra rispetto al caso a 1 riga. Sommata alla struttura fissa della card (immagine 40px + prezzo 12px + eventuale chip stock 9px + gap 4px×3 + padding 12px ≈ 80-85px), la card reale oscilla grossomodo tra **90px** (1 riga, coincide col `min-height` dichiarato) e **~120px** (3 righe) — variazione del **~30%**, sufficiente a produrre stime errate di `Virtualize` (che assume un'altezza media costante) e quindi il "salto"/flicker visibile durante scroll rapido, specialmente quando molte card a 1 riga sono seguite da card a 2-3 righe fuori dal viewport corrente.

**Severità:** Media-Alta — impatto UX percepibile su terminale POS usato ad alta frequenza.

---

## C. Densità/dimensionamento card e volume prodotti

**Confermato.**

| File | Riga | Evidenza |
|---|---|---|
| `POS2026.razor.css` | 389-393 | `.pos26-product-grid { grid-template-columns: repeat(auto-fill, minmax(128px, 1fr)); gap: 8px; }` |
| `POS2026.razor.css` | 1436-1439 | `.pos26-bestseller-row ::deep .pos26-product-card { flex: 0 0 90px; min-width: 90px; }` — **stesso componente** `Pos26ProductCard`, unica differenza è la larghezza (90px invece di ≥128px), nessuna variante di font/line-clamp. Ogni problema di B si propaga identico qui, aggravato dalla larghezza ridotta (meno caratteri/riga → più righe a parità di nome). |
| `POS2026.razor` | blocco `pos26-bestseller-row` (righe ~555-570 area) | Conferma riuso diretto del componente senza parametri di variante. |

**Severità:** Media — non è un bug isolato, è un effetto leva del punto B/F.

---

## D. Toolbar azioni primarie

**Confermato.**

| File | Riga | Evidenza |
|---|---|---|
| `POS2026.razor.css` | 1045-1052 | `.pos26-fn-toolbar { display: flex; gap: 6px; flex-wrap: wrap; padding: 6px 8px; }` |
| `POS2026.razor.css` | 1054-1067 | `.pos26-fn-btn { padding: 4px 8px; font-size: 11px; border: 1px solid transparent; ... }` — bottoni HTML puri, non MudButton/MudChip. |
| `POS2026.razor` | 272-333 | 6 controlli: Rimuovi (274-280), Parcheggia (284-290), Note (293-303), Coupon (306-316), Flusso Giornaliero (318-324), Altro (328-333) — tutti dentro `.pos2026-left` (colonna `1fr` di `.pos2026-content`). |

### Calcolo larghezza minima necessaria (stima)

Per bottone: icona `MudIcon Size="Size.Small"` (~20px) + gap 4px + testo (≈5.5px/carattere a 11px) + padding orizzontale 16px (8px×2) + bordo 2px (1px×2).

| Bottone | Testo (car.) | Larghezza testo | Larghezza bottone stimata |
|---|---|---|---|
| Rimuovi | 7 | ≈38px | ≈80px |
| Parcheggia | 10 | ≈55px | ≈97px |
| Note | 4 | ≈22px | ≈64px |
| Coupon | 6 | ≈33px | ≈75px |
| Flusso Giornaliero | 18 (incl. spazio) | ≈99px | ≈141px |
| Altro | 5 | ≈27px | ≈69px |

Somma bottoni ≈ **526px** + 5 gap×6px (30px) + padding toolbar 16px ≈ **~572px minimi** per restare su una riga.

Confronto con `.pos2026-left` (colonna sinistra, `grid-template-columns: 1fr 360px` più `padding: 8px` → contenuto = larghezza colonna − 16px):

- **Viewport 1280px** → colonna destra 360px → sinistra ≈ 920px − 16px padding ≈ **904px disponibili**: la toolbar (~572px) **entra comodamente su una riga**.
- **Breakpoint 900px** (`grid-template-columns: 1fr 320px`) → sinistra ≈ 900 − 320 = 580px − 16px padding ≈ **564px disponibili**: **inferiore** al minimo stimato (~572px) → **wrap quasi certo** già a 900px, prima che il breakpoint successivo intervenga.
- **Breakpoint 600px** (colonna singola, `.pos2026-left` occupa l'intera larghezza) → ≈600 − 16px padding ≈ **584px disponibili** (ma qui interviene anche il layout a tab mobile) — ancora vicino/sotto soglia, wrap probabile a seconda del rendering reale dei font.

**Conclusione:** la toolbar va effettivamente a capo (produce una seconda riga) già **a partire da ~900-1000px di larghezza reale della colonna sinistra**, ben prima che l'utente percepisca il breakpoint 600px come "mobile". Il problema è quindi presente anche su viewport desktop stretti/finestre non massimizzate, non solo su tablet.

**Severità:** Media — impatto su usabilità/coerenza visiva, non blocca funzionalità (i controlli restano cliccabili, solo su 2 righe).

---

## E. Focus e cattura barcode

**Confermato ed esteso.**

| File | Riga | Evidenza |
|---|---|---|
| `Pos26SearchBar.razor` | 235 | `public void FocusInput() => _ = _inputRef.FocusAsync();` |
| `POS2026.razor.cs` | 1849-1867 | `HandleKeyboardShortcut(string key)`, `case "F8": _searchBar?.FocusInput();` — **unico** punto di invocazione di `FocusInput()` in tutto il progetto (verificato con ricerca globale: nessun'altra occorrenza). |
| `POS2026.razor.cs` | 790-811 | `OnAddProductToCartAsync` (percorso tap-su-card) non richiama mai `FocusInput()` dopo l'aggiunta. |
| `POS2026.razor.cs` | 632+ | `HandleBarcodeAsync` non richiama `FocusInput()` al termine dell'elaborazione. |
| `Pos26SearchBar.razor` | — | Cattura basata unicamente su `@onkeydown` sull'`<input data-barcode-input="true">`: nessun listener `document`/`window`-level via JS interop. |

**Punto 4 — altri spostamenti di focus programmatici individuati** (potenziali "ladri" di focus, oltre F8):
- Apertura dialog via `DialogService.ShowAsync` (es. `OpenProductDetailAsync` riga 817-833, `Pos26CustomerChangeDialog`, `ProductDetailDialog`, dialog di pagamento) — MudBlazor sposta il focus sul dialog per accessibilità.
- Click su bottoni toolbar (`ToggleNoteInput`, `ToggleCouponInput`, `ToggleDailyFlowAsync`, `ToggleMoreMenu`) — ogni `<button>` riceve il focus al click per comportamento nativo HTML.
- Apertura pannello "Altro" (`_showMoreMenu`) e relativi sotto-pannelli (`_showTablePicker`, `_showParkedSessions`).
- Input di testo secondari quando aperti: `_orderNoteText` (textarea note), campo inserimento coupon — questi *devono* poter mantenere il focus mentre l'utente digita (rilevante per Piano E3).
- Tap su una card prodotto stessa (`Pos26ProductCard`, `@onclick`) — il `<div>` non è nativamente focusabile ma il click generico su altri elementi della pagina può comunque spostare il focus attivo del browser via via che l'utente interagisce.

**Severità:** Alta — impatta il flusso operativo primario (scan veloce da lettore hardware), causando perdita silenziosa di input su qualunque interazione touch/mouse intermedia.

---

## F. Leggibilità generale della pagina

**Confermato.**

- Rilevazione: 49/55 dichiarazioni `font-size` in px in `POS2026.razor.css` sono ≤13px (verificato via grep, elenco righe: 95, 144, 195, 245, 305, 327, 360, 374, 404, 487, 510, 546, 554, 561, 598, 604, 613, 627(print), 669, 689, 697, 761, 767, 789, 848, 860, 883, 910(14, escluso), 956, 984, 1002, 1063, 1116, 1134, 1150, 1165, 1182, 1198, 1220, 1244, 1284, 1290, 1295, 1301, 1307, 1312, 1380, 1393, 1416, 1468 — molte a 9-11px su nomi prodotto, chip stock, badge, toolbar, contatori).
- Target touch ridotti confermati: `.pos26-fn-btn` padding `4px 8px` (riga 1058), `.pos26-qty-badge` 18×18px (righe 485-486).

**Severità:** Media — impatto trasversale su accessibilità/usabilità, specialmente su terminali touch senza puntatore preciso.

---

## Elenco definitivo file da toccare in Fase 3 (in base ai piani che verranno approvati)

| File | Piani coinvolti |
|---|---|
| `Prym.Web/ViewModels/POSViewModel.cs` | A |
| `Prym.Web/Shared/Components/Sales/Pos26/Pos26ProductCard.razor` | A, B, C, F |
| `Prym.Web/Shared/Components/Sales/Pos26/Pos26ProductGrid.razor` | B |
| `Prym.Web/Pages/Sales/POS2026.razor.css` | B, C, D, F |
| `Prym.Web/Pages/Sales/POS2026.razor` | D, E (eventuali chiamate FocusInput aggiuntive) |
| `Prym.Web/Pages/Sales/POS2026.razor.cs` | A (routing esito), E |
| `Prym.Web/Shared/Components/Sales/Pos26/Pos26SearchBar.razor` | E (solo se E2/E3 approvato: JS interop) |

Nessun file modificato in questa fase.
