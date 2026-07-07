# Audit Palette Colori POS2026 (Prym) — Fase 1

**Data audit:** 2026-07-07
**Branch:** `copilot/prym-audit-remediation-color-palette`
**Scope:** `Prym.Web/Pages/Sales/POS2026.*`, `Prym.Web/Shared/Components/Sales/**`, `Prym.Web/Shared/Components/Dialogs/Sales/**`, `Prym.Web/Shared/Components/Dialogs/DailyClosureDialog.razor`, `Prym.Web/wwwroot/css/pos26-payment.css`
**Metodo:** analisi statica (grep esaustivo + lettura file), nessuna modifica al codice. Nessun ambiente browser disponibile per screenshot: la verifica dark/light è fatta staticamente seguendo la catena `ThemeService → MudThemeProvider → var(--mud-palette-*)` vs. il `:root` statico di `pos26-payment.css`.

---

## 1. Sintesi dei findings preliminari — verificati

| # | Finding preliminare | Esito verifica |
|---|---|---|
| 1 | `pos26-payment.css` definisce un secondo set di token indipendenti su `:root` | **Confermato**. 17 custom property su `:root` (righe 17-43), tutte hex letterali, nessuna dipendenza da `var(--mud-palette-*)` o da media query/attributo di tema. |
| 2 | File caricato globalmente, non scoped | **Confermato**. `Prym.Web/wwwroot/index.html:59` → `<link rel="stylesheet" href="css/pos26-payment.css" />`, nessun attributo `scoped`/`razor-scope`. |
| 3 | `.pos26-dialog-dark` usata in 11 componenti | **Parzialmente confermato, con precisazione**. La classe `.pos26-dialog-dark` letterale è applicata solo in **8 dialog**: `POSTouchLineEditDialog`, `MergeSessionsDialog`, `FiscalDrawerTransactionDialog`, `SplitPaymentDialog`, `Pos26CustomerChangeDialog`, `Pos26DrawerClosureDialog`, `ItemNotesDialog`, `DailyClosureDialog`. Gli altri 3 componenti citati (`Pos26PaymentDialog`, `Pos26PaymentRow`, `Pos26PaymentMethodCard`) **non** portano la classe wrapper `.pos26-dialog-dark`, ma usano direttamente classi definite nello stesso file (`pos26-payment-dialog-wrap`, `pos26-payment-row`, `pos26-pm-card`, ecc.) che referenziano gli stessi token `--pos-*`. L'insieme complessivo di 11 componenti "nel perimetro `--pos-*`" è quindi confermato, ma il meccanismo di applicazione non è omogeneo (classe wrapper vs. classi dedicate). |
| 4 | Elementi `--pos-*` restano fissi indipendentemente dal tema | **Confermato con un'eccezione documentata**. Vedi §4. |
| 5 | `MUDBLAZOR_GUIDELINES.md` §3 cita `#1F2F46` come esempio di anti-pattern, identico a `--pos-brand-navy` | **Confermato**. `Prym.Web/MUDBLAZOR_GUIDELINES.md:276` → `.my-element { background-color: #1F2F46; ...}` come esempio ❌ WRONG; `pos26-payment.css:18` → `--pos-brand-navy: #1F2F46;` — valore letterale identico. Nota: `--pos-brand-navy` risulta definita ma **non risulta usata** in nessuna regola del file (grep su `var(--pos-brand-navy)` = 0 occorrenze) — token morto/riservato, non solo anti-pattern in produzione ma anche dead code. |
| 6 | Colori hex isolati fuori dal sistema `--pos-*` | **Confermato ed esteso** — vedi §3, incluso un valore aggiuntivo non elencato nei findings preliminari (`POSTouchLineEditDialog.razor:17`, `rgba(255,255,255,0.06)`). |

---

## 2. Inventario completo

### 2.1 Token `--pos-*` definiti in `pos26-payment.css` (righe 17-43)

| Token | Valore | Categoria | Scopo visivo |
|---|---|---|---|
| `--pos-brand-navy` | `#1F2F46` | hex-literal | Definito, **mai referenziato** (dead token) |
| `--pos-brand-blue` | `#247BFF` | hex-literal | Accent/brand primario (icone, bordi selezione, badge, CTA) |
| `--pos-brand-blue-dim` | `#1B5FC4` | hex-literal | Definito, **mai referenziato** nel perimetro auditato |
| `--pos-brand-blue-bg` | `#1A2C4A` | hex-literal | Sfondo stato "selezionato" (`.pos26-pm-card--selected`) |
| `--pos-surface-0` | `#17171A` | hex-literal | Sfondo colonna numpad (`.pos26-pay-right`) |
| `--pos-surface-1` | `#212226` | hex-literal | Sfondo dialog/card primario |
| `--pos-surface-2` | `#2B2C31` | hex-literal | Sfondo secondario (poco usato) |
| `--pos-surface-3` | `#37383E` | hex-literal | Hover tasti numpad |
| `--pos-border` | `#3C3D44` | hex-literal | Bordi, sfondo tasti numpad |
| `--pos-text-1` | `#EDEDEF` | hex-literal | Testo primario su sfondo scuro |
| `--pos-text-2` | `#A6A7AE` | hex-literal | Testo secondario |
| `--pos-text-3` | `#6E6F78` | hex-literal | Testo terziario/disabled |
| `--pos-success` | `#2FAE7C` | hex-literal | Stato semantico positivo (resto, stock ok) |
| `--pos-success-bg` | `#16332A` | hex-literal | Sfondo badge/banner successo |
| `--pos-warning` | `#D69A3C` | hex-literal | Stato semantico attenzione |
| `--pos-warning-bg` | `#362A15` | hex-literal | Sfondo badge/banner warning |
| `--pos-danger` | `#E15B5B` | hex-literal | Stato semantico errore |
| `--pos-danger-bg` | `#3A1E1E` | hex-literal | Sfondo badge/banner errore |
| `--pos-radius-sm/md/lg` | `8/12/16px` | non-color | Fuori scope (non è un colore) |

### 2.2 Occorrenze di `var(--pos-*)` — conteggio per file

| File | Occorrenze `var(--pos-*)` | Occorrenze `var(--mud-palette-*)` |
|---|---|---|
| `Prym.Web/Pages/Sales/POS2026.razor.css` | 142 | 46 |
| `Prym.Web/wwwroot/css/pos26-payment.css` | 29 | 11 |

Il mix nello stesso file (`POS2026.razor.css`) è confermato: entrambe le famiglie di variabili sono usate per la stessa categoria di proprietà (`background`, `color`, `border`) su elementi diversi della stessa pagina — non esiste un criterio dichiarato che distingua quando usare l'una o l'altra, a eccezione del pattern documentato in §2.3.

### 2.3 Pattern di override locale già presente (non documentato nei findings preliminari)

`POS2026.razor.css` contiene **due sottoalberi che già ridefiniscono localmente** i token `--pos-surface-*`/`--pos-text-*`/`--pos-border` come alias di `var(--mud-palette-*)`, con un commento esplicativo dello sviluppatore originale:

```css
/* riga 440-452 */
::deep .pos26-product-card {
    --pos-surface-1: var(--mud-palette-surface);
    --pos-surface-2: var(--mud-palette-background-grey);
    --pos-text-1: var(--mud-palette-text-primary);
    --pos-text-2: var(--mud-palette-text-secondary);
    --pos-text-3: var(--mud-palette-text-secondary);
    --pos-border: var(--mud-palette-lines-default);
}
```
```css
/* riga 582-589, stesso pattern su ::deep .pos26-receipt */
```

**Implicazione importante per la Fase 2**: questo pattern dimostra che (a) lo sviluppo è già consapevole del problema, (b) esiste una soluzione locale già collaudata (variable override per sottoalbero), (c) **ma è incompleta**: `--pos-brand-blue`, `--pos-brand-blue-bg`, `--pos-success(-bg)`, `--pos-warning(-bg)`, `--pos-danger(-bg)` **non vengono ridefiniti** nemmeno in questi due sottoalberi "chiari" — quindi anche `Pos26ProductCard` e `Pos26Receipt`, pur avendo sfondo/testo reattivi al tema, mantengono badge/stock-chip/accent color fissi sulla palette scura in ogni tema. Questo è un bug di reattività theme parziale, non totale.

### 2.4 Colori hex/rgba isolati fuori dal sistema `--pos-*` (punto 6 preliminare, verificato ed esteso)

| File | Riga | Valore | Categoria | Componente/elemento | Scopo |
|---|---|---|---|---|---|
| `Prym.Web/Shared/Components/Sales/Pos26/Pos26PaymentRow.razor` | 21 | `#FAEEDA` / `#E5EFFF` | hex-literal (C#, non CSS) | `_color` — pallino di stato riga pagamento | Distingue contanti (ambra chiaro) da altri metodi (blu chiaro); commento in codice dichiara "allineato a `--pos-brand-blue-bg`" ma il valore non è derivato dal token, è un hex indipendente |
| `Prym.Web/Shared/Components/Sales/Pos26/Pos26PaymentMethodCard.razor` | 9 | `#1D9E75` | inline-style (`Style=` su `MudIcon`) | Icona `CheckCircle` per metodo "già aggiunto" | Verde di conferma — non usa `Color=` del componente MudBlazor né `var(--pos-success)` |
| `Prym.Web/Shared/Components/Dialogs/Sales/SessionNoteDialog.razor` | 30 | `#666` (fallback) | inline-style | Colore icona/flag nota | Fallback quando `flag.Color` (da backend) è null |
| `Prym.Web/Shared/Components/Dialogs/Sales/POSTouchLineEditDialog.razor` | 17 | `rgba(255,255,255,0.06)` | inline-style | Sfondo pannello riepilogo riga | **Non presente nei findings preliminari** — overlay bianco trasparente, coerente solo perché il contenitore è sempre scuro (`.pos26-dialog-dark`); se in futuro il dialog diventasse chiaro, l'overlay sparirebbe/invertirebbe contrasto |
| `Prym.Web/Pages/Sales/POS2026.razor.css` | 214 | `#1a1305` | hex-literal | `.sb-btn--warning` testo su sfondo `--pos-warning` | Testo scuro leggibile su ambra — valore isolato, non derivato da token |
| `Prym.Web/Pages/Sales/POS2026.razor.css` | 315, 379, 491, 776, 943, 1002, 1133, 1180 (+ pos26-payment.css:223, 411) | `#fff` | hex-literal | Testo su sfondo `--pos-brand-blue` / `--pos-success` / `--pos-danger` | Ripetuto 10 volte; potrebbe essere token semantico unico (`--pos-on-accent` o simile) invece di letterale ripetuto |
| `Prym.Web/Pages/Sales/POS2026.razor.css` | 858 | `#268f66` | hex-literal | `:hover` di `.pos26-pay-cta` (stato hover di bottone success) | Variante scura di `--pos-success` non tokenizzata |
| `Prym.Web/Pages/Sales/POS2026.razor.css` | 1144-1145 | `#f9f9f9`, `#e0e0e0` | hex-literal (fallback di `var(..., fallback)`) | `.pos26-fn-panel` | Fallback statico di `var(--mud-palette-background-grey, #f9f9f9)` — pattern accettabile ma il fallback esiste solo qui, incoerenza con il resto del file che non usa fallback |

### 2.5 Verifica esistenza `pos2026-layout-proposta.html`

- **Non presente nel repository** (nessun match per nome file, in nessun percorso).
- **Non presente in cronologia git accessibile**: `git log --all --diff-filter=A --name-only` non mostra alcuna aggiunta di file con questo nome.
- Il repository clonato è uno **shallow clone con un solo branch locale** (`copilot/prym-audit-remediation-color-palette`); non è possibile escludere che il file sia esistito in un branch di lavoro locale mai pushato o in un tool esterno (Figma/mockup HTML condiviso fuori da git). Nel repository attuale resta **solo un riferimento testuale nel commento** di `pos26-payment.css:12`, senza il file sorgente. Non essendo necessario recuperare storia/branch aggiuntivi per un'analisi read-only completa (il file semplicemente non esiste qui), non è stato eseguito un unshallow — se Ivano conferma che il mockup è irrintracciabile anche altrove, il riferimento va comunque rimosso in Fase 3 (già previsto dal piano).

---

## 3. Confronto quantitativo: token POS vs. token tema più vicino

| Token `--pos-*` | Valore | Token tema più vicino (funzione) | Valore tema | ΔH (hue) | ΔS | ΔL (lightness) | Classificazione |
|---|---|---|---|---|---|---|---|
| `--pos-brand-blue` | `#247BFF` | `Primary` light (`#0099CC`) | `#0099CC` | 21.2° | 0 | 17.1 | **(b) scopo nuovo ma ingiustificato** — stesso ruolo (brand/accent primario), hue percettibilmente diverso (blu più "elettrico" vs. ciano teal del tema) |
| `--pos-brand-blue` | `#247BFF` | `Primary` dark (`#00F5FF`) | `#00F5FF` | 33.8° | 0 | 7.1 | **(a→c) duplicato mancato**: dovrebbe essere lo stesso concetto (colore primario brand) ma diverge molto di più in dark che in light — sintomo che i due mondi non sono mai stati confrontati insieme |
| `--pos-brand-navy` | `#1F2F46` | — (nessuna proprietà `Mud*` corrispondente diretta; più vicino concettualmente ad `AppbarBackground` scuro) | n/d | — | — | — | **(c) valore isolato/dimenticato** — non referenziato in nessuna regola attiva, coincide col caso denunciato in `MUDBLAZOR_GUIDELINES.md` §3 |
| `--pos-surface-1` | `#212226` | `Surface` dark (`#262626`) | `#262626` | 132° (hue instabile, colore quasi acromatico) | 7.0 | 1.0 | **(a) duplicato sinonimo** — luminosità quasi identica (ΔL=1.0), differenza percettiva trascurabile; nessuna ragione per due valori distinti |
| `--pos-surface-2` | `#2B2C31` | `Background` dark (`#121212`) o superficie "grey" (non esplicitamente esposta come singolo hex in `EventForgeTheme.cs`, MudBlazor la deriva) | ~`#1E1E1E` (stima) | 130° | 6.5 | 6.2 | **(a) duplicato sinonimo probabile**, ma servirebbe il valore effettivo calcolato da MudBlazor per `background-grey` in dark per confermare con precisione (non hardcoded esplicitamente in `EventForgeTheme.cs`) |
| `--pos-text-1` | `#EDEDEF` | `TextPrimary` dark (`#FFFFFF`) | `#FFFFFF` | 120° (acromatico) | 5.9 | 6.7 | **(a) duplicato sinonimo** — differenza minima, quasi bianco vs bianco leggermente attenuato |
| `--pos-text-2` | `#A6A7AE` | `TextSecondary` dark (`#B3B3B3`) | `#B3B3B3` | 127.5° (acromatico) | 4.7 | 3.5 | **(a) duplicato sinonimo** — praticamente identico |
| `--pos-success` | `#2FAE7C` | `Success` light (`#00C853`) | `#00C853` | 11.5° | 42.5 | 4.1 | **(b) variante desaturata dello stesso concetto** — hue vicino, saturazione molto più bassa (verde "tenue" vs. verde acceso Material) |
| `--pos-success` | `#2FAE7C` | `Success` dark (`#10B981`) | `#10B981` | 3.7° | 26.6 | 3.9 | **(a/b borderline)** — hue quasi identico, saturazione ancora piuttosto diversa: stesso concetto semantico, tono più tenue nel design POS (verosimilmente intenzionale per leggibilità in ambiente luminoso) |
| `--pos-warning` | `#D69A3C` | `Warning` light/dark (`#FFB300`/`#F59E0B`) | v. tabella | 1.1°-5.5° | 27-35 | 3.5-3.7 | **(b) stesso hue, meno saturo** — pattern coerente col caso success: la palette POS sembra sistematicamente più desaturata di ~30 punti percentuali sui semantici, non casuale |
| `--pos-danger` | `#E15B5B` | `Error` dark (`#EF4444`) | `#EF4444` | 0.0° | 15.1 | 1.8 | **(a) duplicato quasi esatto** in dark (hue identico!) — conferma che la palette POS è stata "derivata" dal dark theme in origine e poi non riallineata quando i valori del tema sono cambiati, oppure che i valori dark del tema sono stati la fonte originale del design POS |
| `--pos-danger` | `#E15B5B` | `Error` light (`#FF3D00`) | `#FF3D00` | 14.4° | 30.9 | 12.0 | **(b)** — più distante dal light theme |

**Osservazione trasversale**: i token semantici (`success`/`warning`/`danger`) mostrano una desaturazione sistematica di circa 25-40 punti rispetto a *entrambe* le palette del tema, mentre restano quasi identici in hue al tema **dark**. Questo supporto quantitativo rafforza l'ipotesi B del piano (dark POS "kiosk" intenzionale, con toni più tenui per uso prolungato) più che un semplice errore di copia — ma non spiega perché lo stesso design non sia mai stato replicato come vera opzione "light POS".

---

## 4. Verifica empirica comportamento dark/light (analisi statica)

Meccanismo di cambio tema: `ThemeService.ToggleThemeAsync` cambia `_currentTheme` tra `carbon-neon-light`/`carbon-neon-dark`, che pilota `IsDarkMode` → `MudThemeProvider` → seleziona `PaletteLight`/`PaletteDark` da `EventForgeTheme.cs`, che si riflette nelle CSS custom properties `--mud-palette-*` iniettate da MudBlazor sull'elemento root. Non esiste alcun meccanismo (classe su `<html>`/`<body>`, media query, o binding Blazor) che alteri i valori di `:root { --pos-*: ... }` definiti in `pos26-payment.css`: sono letterali statici, quindi **strutturalmente indipendenti da `ThemeService`**.

Verifica sui 3 componenti richiesti:

1. **`Pos26PaymentDialog`** (via `.pos26-payment-dialog-wrap`/`.pos26-pm-card`/`.pos26-pay-*`): usa `var(--pos-brand-blue)`, `var(--pos-surface-0/1)`, `var(--pos-success/warning/danger(-bg))` in modo esclusivo per gli elementi di brand/stato/numpad — **nessuna di queste regole cambia** al variare di `IsDarkMode`. Le uniche regole che *cambiano* correttamente sono quelle su `var(--mud-palette-background-grey)`, `var(--mud-palette-text-secondary)`, `var(--mud-palette-lines-default)` (es. `.pos26-pay-summary-card`, `.pos26-pay-card-label`, `.pos26-pm-card`) — **confermato comportamento misto all'interno dello stesso dialog**.
2. **`MergeSessionsDialog`** (via `.pos26-dialog-dark`): l'intero sfondo/testo del dialog è impostato da `.pos26-dialog-dark` con `var(--pos-surface-1)`/`var(--pos-text-1)` — **fisso, non cambia mai** tra i due temi. Il dialog rimane visivamente identico (scuro) sia in `carbon-neon-light` sia in `carbon-neon-dark`.
3. **`Pos26ProductCard`** (via override locale §2.3): sfondo/testo/bordo **cambiano correttamente** col tema (alias a `var(--mud-palette-*)`), ma badge/stock-chip che usano `--pos-success`/`--pos-warning`/`--pos-danger` (non ridefiniti nel subtree) **restano fissi** sui toni desaturati della palette POS in entrambi i temi — comportamento parzialmente reattivo, non totale come sembrerebbe dal solo commento in testa alla regola.

**Conclusione verifica**: il finding preliminare #4 è confermato nella sostanza (il dark POS "vince" sempre sui token che non sono stati derivati da `--mud-palette-*`), con la precisazione che non è un fenomeno binario per-componente ma **per singola proprietà CSS**: alcuni componenti (product card, receipt) sono già parzialmente corretti su sfondo/testo ma non su accent/stato; altri (i dialog con `.pos26-dialog-dark`) sono completamente statici.

---

## 5. Elenco definitivo file/righe da toccare in Fase 3 (in base all'opzione che verrà scelta)

| File | Righe interessate | Tipo di intervento previsto |
|---|---|---|
| `Prym.Web/wwwroot/css/pos26-payment.css` | 1-43 (header + `:root`), 113-123, 159-176, 220-224, 280-421 | Ridefinizione/eliminazione token `--pos-*`, aggiornamento commento intestazione (rimozione riferimento a `pos2026-layout-proposta.html`) |
| `Prym.Web/Pages/Sales/POS2026.razor.css` | 142 occorrenze `var(--pos-*)`, righe 214, 315, 379, 491, 776, 850, 858, 943, 1002, 1133, 1144-1145, 1180 (hex isolati), 440-452 e 582-589 (subtree override da estendere o rimuovere) | Rimappatura/rimozione hex isolati, eventuale estensione o rimozione del pattern di override locale |
| `Prym.Web/Shared/Components/Sales/Pos26/Pos26PaymentRow.razor` | 21 | Sostituzione `#FAEEDA`/`#E5EFFF` con token semantico |
| `Prym.Web/Shared/Components/Sales/Pos26/Pos26PaymentMethodCard.razor` | 9 | Sostituzione `Style="color:#1D9E75"` con `Color=` su `MudIcon` o token `--pos-success`/`--mud-palette-success` |
| `Prym.Web/Shared/Components/Sales/Pos26/Pos26PaymentDialog.razor` | 290 | Rimappatura `background:var(--pos-danger);color:#fff;` |
| `Prym.Web/Shared/Components/Dialogs/Sales/SessionNoteDialog.razor` | 30 | Sostituzione fallback `#666` con token (es. `var(--mud-palette-text-disabled)`) |
| `Prym.Web/Shared/Components/Dialogs/Sales/POSTouchLineEditDialog.razor` | 17 | Sostituzione `rgba(255,255,255,0.06)` con token semantico coerente con l'opzione scelta |
| `Prym.Web/Shared/Components/Dialogs/Sales/MergeSessionsDialog.razor` | 12 | Dipende da opzione (A: rimuove `.pos26-dialog-dark`; B: la rende un vero tema; C: nessuna modifica al markup) |
| `Prym.Web/Shared/Components/Dialogs/Sales/FiscalDrawerTransactionDialog.razor` | 15 | Come sopra |
| `Prym.Web/Shared/Components/Dialogs/Sales/SplitPaymentDialog.razor` | 13 | Come sopra |
| `Prym.Web/Shared/Components/Dialogs/Sales/Pos26CustomerChangeDialog.razor` | 13 | Come sopra |
| `Prym.Web/Shared/Components/Dialogs/Sales/Pos26DrawerClosureDialog.razor` | 17, 22 | Come sopra |
| `Prym.Web/Shared/Components/Dialogs/Sales/ItemNotesDialog.razor` | 8 | Come sopra |
| `Prym.Web/Shared/Components/Dialogs/DailyClosureDialog.razor` | 14 | Come sopra |
| `Prym.Web/MUDBLAZOR_GUIDELINES.md` | §3 (Anti-pattern 5), §4 (Dark Mode Checklist) | Solo se Opzione B: documentare eccezione/nuovo tema POS |
| `docs/frontend/ui-guidelines.md` | (da verificare in Fase 3 se contiene riferimenti equivalenti) | Solo se Opzione B |

**Totale hex/rgba letterali da rimuovere**: 17 (token `:root`) + 8 isolate in `POS2026.razor.css`/`pos26-payment.css` + 3 isolate in componenti Razor = **28 valori**.
**Totale righe con `var(--pos-*)` da valutare/rimappare**: 171 (142 + 29).

---

## 6. Nota metodologica

Questo audit non ha modificato alcun file di codice. È stato creato solo questo file `AUDIT_POS2026_PALETTE.md`, come esplicitamente richiesto come output della Fase 1. La Fase 2 (piano di remediation con opzioni A/B/C) è stata presentata separatamente in risposta; **Ivano ha approvato l'Opzione A — Unificazione totale su MudBlazor**. La Fase 3 è stata implementata di conseguenza (vedi §7).

---

## 7. Fase 3 — Changelog implementazione (Opzione A)

Rimappatura completa di tutti i token `--pos-*` colore su `var(--mud-palette-*)`. I soli token non-colore (`--pos-radius-sm/md/lg`) sono stati mantenuti invariati (fuori scope, non sono colori). Rimozione della classe wrapper `.pos26-dialog-dark` da tutti gli 8 dialog che la usavano, poiché `EFDialog` fornisce già uno sfondo/testo coerente col tema (`var(--mud-palette-surface)`/`var(--mud-palette-text-primary)`), rendendo il wrapper scuro statico ridondante e in conflitto col tema.

### Mappatura token → variabile MudBlazor

| Token rimosso | Nuovo riferimento | Motivazione |
|---|---|---|
| `--pos-brand-navy` | `var(--mud-palette-dark)` (testo: `var(--mud-palette-dark-text)`) | Accento sempre scuro ma theme-aware, stesso pattern già usato in `TableFloorPlan.razor.css`/`entity-drawer.css` |
| `--pos-brand-blue` | `var(--mud-palette-primary)` (testo su sfondo: `var(--mud-palette-primary-text)`) | Colore brand/accent primario del tema |
| `--pos-brand-blue-dim` | `var(--mud-palette-primary-darken)` | Variante scura per stato hover, pattern già usato in `MainLayout.razor.css` |
| `--pos-brand-blue-bg` | `var(--mud-palette-primary-hover)` | Tinta chiara per stato selezionato/attivo, stesso pattern di `EFDialog.razor` (tab attiva) |
| `--pos-surface-0` | `var(--mud-palette-background)` | Sfondo pannello "incassato" (numpad), più scuro della superficie dialog |
| `--pos-surface-1` | `var(--mud-palette-surface)` | Superficie dialog/card standard |
| `--pos-surface-2` | `var(--mud-palette-background-grey)` | Superficie secondaria |
| `--pos-surface-3` | `var(--mud-palette-action-default-hover)` | Stato hover |
| `--pos-border` | `var(--mud-palette-lines-default)` | Bordi standard |
| `--pos-text-1` | `var(--mud-palette-text-primary)` | Testo primario |
| `--pos-text-2` | `var(--mud-palette-text-secondary)` | Testo secondario |
| `--pos-text-3` | `var(--mud-palette-text-disabled)` | Testo terziario/disabled |
| `--pos-success` / `--pos-success-bg` | `var(--mud-palette-success)` / `var(--mud-palette-success-lighten)` | Stato semantico positivo, pattern già in `app.css` (`.bg-success-light`) |
| `--pos-warning` / `--pos-warning-bg` | `var(--mud-palette-warning)` / `var(--mud-palette-warning-lighten)` | Stato semantico attenzione |
| `--pos-danger` / `--pos-danger-bg` | `var(--mud-palette-error)` / `var(--mud-palette-error-lighten)` | Stato semantico errore |

### File modificati

| File | Prima | Dopo | Motivazione |
|---|---|---|---|
| `Prym.Web/wwwroot/css/pos26-payment.css` | `:root` con 17 hex letterali + regole `.pos26-dialog-dark`; commento riferiva `pos2026-layout-proposta.html` | `:root` con soli 3 token radius (non-colore); `.pos26-dialog-dark` rimossa; tutte le regole colore rimappate su `var(--mud-palette-*)`; commento aggiornato a riferire `EventForgeTheme.cs` | Elimina il secondo sistema di colori statico; rimuove riferimento a mockup non versionato |
| `Prym.Web/Pages/Sales/POS2026.razor.css` | 142 `var(--pos-*)` colore + 13 hex letterali isolati (`#fff`, `#1a1305`, `#268f66`, fallback `#f9f9f9`/`#e0e0e0`) + 2 subtree di override locale ridondanti | Tutti i riferimenti colore rimappati su `var(--mud-palette-*)`; hex isolati sostituiti con token semantici corretti (incl. varianti `-text` per contrasto su sfondi accent); subtree di override rimossi (non più necessari, i token base sono già theme-aware) | Uniforma l'intero file su un'unica fonte di colore |
| `Prym.Web/Pages/Sales/POS2026.razor` | 1 `Style="color:var(--pos-success);"` su `MudIcon` (icona coupon) | `Color="Color.Success"` | Usa il parametro `Color=` del componente MudBlazor invece di CSS inline |
| `Prym.Web/Shared/Components/Sales/Pos26/Pos26ProductCard.razor` | `Style="color:var(--pos-text-3);"` su `MudIcon` (placeholder immagine) | `Style="color:var(--mud-palette-text-disabled);"` | Nessun parametro `Color=` MudBlazor equivalente a "disabled text"; mantenuto come var CSS theme-aware |
| `Prym.Web/Shared/Components/Sales/Pos26/Pos26PaymentDialog.razor` | `"background:var(--pos-danger);color:#fff;"` (stringa C# per tasto backspace numpad) | `"background:var(--mud-palette-error);color:var(--mud-palette-error-text);"` | Rimappatura token + contrasto testo corretto |
| `Prym.Web/Shared/Components/Sales/Pos26/Pos26PaymentMethodCard.razor` | `Style="color:#1D9E75;"` su `MudIcon` (spunta "già aggiunto") | `Color="Color.Success"` | Hex isolato sostituito con parametro `Color=` del componente |
| `Prym.Web/Shared/Components/Sales/Pos26/Pos26PaymentRow.razor` | `_color` C# restituiva `"#FAEEDA"`/`"#E5EFFF"` (hex isolati) | Restituisce `"var(--mud-palette-warning-lighten)"`/`"var(--mud-palette-primary-hover)"` | Elimina hex isolati indipendenti dal tema |
| `Prym.Web/Shared/Components/Dialogs/Sales/SessionNoteDialog.razor` | Fallback `flag.Color ?? "#666"` | Fallback `flag.Color ?? "var(--mud-palette-text-disabled)"` | Il colore custom per-flag resta legittimo (dato di dominio), solo il fallback statico è stato sostituito |
| `Prym.Web/Shared/Components/Dialogs/Sales/POSTouchLineEditDialog.razor` | Wrapper `<div class="pos26-dialog-dark">`; `Style="background: rgba(255,255,255,0.06);..."` | Wrapper rimosso; `Style="background: var(--mud-palette-background-grey);..."` | Elimina doppio sfondo statico e overlay bianco fisso |
| `Prym.Web/Shared/Components/Dialogs/Sales/MergeSessionsDialog.razor` | Wrapper `<div class="pos26-dialog-dark">` | Wrapper rimosso | `EFDialog` fornisce già sfondo/testo theme-aware |
| `Prym.Web/Shared/Components/Dialogs/Sales/FiscalDrawerTransactionDialog.razor` | `<div class="pa-4 pos26-dialog-dark">` | `<div class="pa-4">` | Come sopra, mantenuto solo il padding utility |
| `Prym.Web/Shared/Components/Dialogs/Sales/SplitPaymentDialog.razor` | Wrapper `<div class="pos26-dialog-dark">` | Wrapper rimosso | Come sopra |
| `Prym.Web/Shared/Components/Dialogs/Sales/Pos26CustomerChangeDialog.razor` | Wrapper `<div class="pos26-dialog-dark">` | Wrapper rimosso | Come sopra |
| `Prym.Web/Shared/Components/Dialogs/Sales/Pos26DrawerClosureDialog.razor` | `<div class="pa-4 pos26-dialog-dark">` | `<div class="pa-4">` | Come sopra |
| `Prym.Web/Shared/Components/Dialogs/Sales/ItemNotesDialog.razor` | Wrapper `<div class="pos26-dialog-dark">` | Wrapper rimosso | Come sopra |
| `Prym.Web/Shared/Components/Dialogs/DailyClosureDialog.razor` | Wrapper `<div class="pos26-dialog-dark">` | Wrapper rimosso | Come sopra |

### Verifica

- `dotnet build Prym.Web/Prym.Web.csproj -c Debug` → **Build succeeded, 0 Error(s)**, nessun nuovo warning nei file toccati.
- Scansione repo-wide finale: **0** occorrenze residue di `var(--pos-*)` colore, **0** hex/rgba letterali isolati nello scope auditato (radius `--pos-radius-*` mantenuti, non sono colori).
- Nessuna modifica alla logica funzionale: solo classi CSS, attributi `Style=`/`Color=` e wrapper `<div>` di puro styling sono stati toccati.
