# Guida al Test Manuale - Auto-Focus Inventario

## Obiettivo
Verificare che l'auto-focus sui campi barcode e quantità funzioni correttamente nella procedura di inventario rapido.

## Prerequisiti
1. EventForge server avviato e funzionante
2. Utente autenticato con ruolo Operator, Manager, Admin o SuperAdmin
3. Almeno un magazzino configurato nel sistema
4. Almeno un prodotto attivo con barcode/codice configurato
5. Browser moderno (Chrome, Edge, Firefox)

## Setup Ambiente di Test

### 1. Avviare il Server
```bash
cd EventForge.Server
dotnet run
```

### 2. Avviare il Client (se separato)
```bash
cd EventForge.Client
npm install
dotnet run
```

### 3. Accedere all'Applicazione
- URL: `http://localhost:5000` (o porta configurata)
- Login con credenziali valide
- Navigare a: **Warehouse → Inventory Procedure** (`/warehouse/inventory-procedure`)

## Test Cases

### Test 1: Auto-Focus su Apertura Pagina (Sessione Esistente)

**Precondizioni:**
- Esiste una sessione di inventario aperta nel localStorage

**Procedura:**
1. Aprire la pagina `/warehouse/inventory-procedure`
2. Attendere il caricamento completo

**Risultato Atteso:**
- ✅ Il campo "Codice a Barre" deve avere il focus automaticamente
- ✅ Il cursore deve lampeggiare nel campo barcode
- ✅ È possibile digitare immediatamente senza cliccare

**Criteri di Successo:**
- Il focus è visibile (bordo evidenziato)
- Digitando caratteri appaiono nel campo barcode
- Non è necessario cliccare sul campo

---

### Test 2: Auto-Focus Dopo Avvio Nuova Sessione

**Precondizioni:**
- Nessuna sessione attiva
- Almeno un magazzino configurato

**Procedura:**
1. Aprire la pagina `/warehouse/inventory-procedure`
2. Selezionare un magazzino dal dropdown
3. Cliccare "Avvia Sessione"
4. Attendere la conferma (snackbar verde)

**Risultato Atteso:**
- ✅ Dopo l'avvio, il campo barcode deve avere il focus automaticamente
- ✅ Il banner "Sessione di Inventario Attiva" appare in alto
- ✅ È possibile digitare immediatamente nel campo barcode

**Criteri di Successo:**
- Focus visibile sul campo barcode
- Banner sessione attiva visibile
- Digitazione immediata possibile

---

### Test 3: Auto-Focus su Quantità (Prodotto Trovato)

**Precondizioni:**
- Sessione inventario attiva
- Prodotto con barcode "TEST123" esistente nel sistema

**Procedura:**
1. Assicurarsi che il campo barcode abbia il focus
2. Digitare "TEST123" (o scansionare con lettore barcode)
3. Premere **Enter**
4. Attendere che il dialog si apra

**Risultato Atteso:**
- ✅ Dialog "Inserimento Inventario" si apre
- ✅ Informazioni prodotto visibili in alto (ProductQuickInfo)
- ✅ Se una sola ubicazione: già selezionata automaticamente
- ✅ Campo "Quantità" (o unità alternativa) ha il focus automaticamente
- ✅ È possibile digitare immediatamente la quantità

**Criteri di Successo:**
- Dialog aperto correttamente
- Focus sul campo quantità (bordo evidenziato)
- Digitazione numero funziona immediatamente
- Se campo aveva valore: testo selezionato (in modalità edit)

---

### Test 4: Auto-Focus su Quantità (Modifica Riga)

**Precondizioni:**
- Sessione inventario attiva
- Almeno una riga già inserita nel documento

**Procedura:**
1. Scorrere alla tabella "Articoli nel Documento di Inventario"
2. Cliccare icona "Modifica" (matita) su una riga
3. Attendere apertura dialog

**Risultato Atteso:**
- ✅ Dialog "Modifica Riga Inventario" si apre
- ✅ Campo quantità ha il focus automaticamente
- ✅ Il valore esistente è **selezionato** (evidenziato)
- ✅ Digitando un numero, sovrascrive il valore esistente

**Criteri di Successo:**
- Dialog aperto in modalità edit
- Focus sul campo quantità
- Testo completamente selezionato (evidenziato blu)
- Digitare sovrascrive il valore

---

### Test 5: Ritorno Focus a Barcode (Dopo Inserimento)

**Precondizioni:**
- Sessione inventario attiva
- Dialog inserimento aperto con quantità inserita

**Procedura:**
1. Compilare il form del dialog:
   - Ubicazione: selezionata (o auto-selezionata)
   - Quantità: inserire valore (es. 10)
   - Note: (opzionale)
2. Cliccare "Avanti"
3. Nella schermata conferma, cliccare "Conferma"
4. Attendere snackbar conferma

**Risultato Atteso:**
- ✅ Dialog si chiude
- ✅ Nuova riga appare nella tabella
- ✅ Campo barcode è vuoto
- ✅ Campo barcode ha il focus automaticamente
- ✅ È possibile scansionare/digitare immediatamente il prossimo barcode

**Criteri di Successo:**
- Dialog chiuso
- Riga visibile nella tabella
- Campo barcode vuoto e con focus
- Snackbar verde "Articolo aggiunto al documento"

---

### Test 6: Focus su Barcode (Prodotto Non Trovato - Skip)

**Precondizioni:**
- Sessione inventario attiva

**Procedura:**
1. Nel campo barcode digitare "BARCODE_INESISTENTE"
2. Premere **Enter**
3. Dialog "Prodotto non trovato" appare
4. Cliccare "Salta"

**Risultato Atteso:**
- ✅ Dialog si chiude
- ✅ Campo barcode è vuoto
- ✅ Campo barcode ha il focus automaticamente
- ✅ Snackbar info "Prodotto saltato"

**Criteri di Successo:**
- Dialog chiuso
- Campo barcode vuoto e con focus
- Snackbar informativo mostrato

---

### Test 7: Focus su Barcode (Annulla Dialog)

**Precondizioni:**
- Sessione inventario attiva
- Dialog inserimento aperto

**Procedura:**
1. Aprire dialog inserimento per un prodotto valido
2. Cliccare "Chiudi" o premere **Esc**

**Risultato Atteso:**
- ✅ Dialog si chiude
- ✅ Campo barcode è vuoto
- ✅ Campo barcode ha il focus automaticamente

**Criteri di Successo:**
- Dialog chiuso senza salvare
- Campo barcode vuoto e con focus

---

### Test 8: Auto-Focus con Ubicazione Singola

**Precondizioni:**
- Sistema configurato con **una sola** ubicazione
- Sessione inventario attiva

**Procedura:**
1. Scansionare un barcode valido
2. Attendere apertura dialog

**Risultato Atteso:**
- ✅ Dialog si apre
- ✅ Ubicazione già selezionata automaticamente (non dropdown visibile)
- ✅ Campo quantità ha il focus immediatamente
- ✅ È possibile digitare subito la quantità

**Criteri di Successo:**
- Ubicazione auto-selezionata
- Focus su quantità senza passaggi intermedi
- Workflow ancora più veloce

---

### Test 9: Auto-Focus con Multiple Ubicazioni

**Precondizioni:**
- Sistema configurato con **multiple** ubicazioni
- Sessione inventario attiva

**Procedura:**
1. Scansionare un barcode valido
2. Attendere apertura dialog

**Risultato Atteso:**
- ✅ Dialog si apre
- ✅ Focus sul dropdown "Ubicazione"
- ✅ Dopo selezione ubicazione e Tab, focus va su quantità

**Criteri di Successo:**
- Focus iniziale su ubicazione (logico)
- Tab funziona per navigare tra campi
- Workflow fluido

---

### Test 10: Workflow Completo End-to-End

**Procedura:**
1. Aprire pagina inventario (verificare focus su barcode)
2. Scansionare "PROD001" + Enter
3. Nel dialog: digitare "50" + Tab + "Test note" + Avanti
4. Confermare
5. Verificare focus su barcode
6. Scansionare "PROD002" + Enter
7. Digitare "100" + confermare
8. Verificare focus su barcode
9. Ripetere per 5-10 prodotti

**Risultato Atteso:**
- ✅ Workflow fluido senza interruzioni
- ✅ Nessun clic manuale necessario per il focus
- ✅ Scansione continua possibile
- ✅ Tempi di inserimento ridotti

**Criteri di Successo:**
- Zero clic aggiuntivi per focus
- Inserimento prodotti veloce e continuo
- Nessun errore o comportamento inatteso

---

## Metriche di Performance

### Tempo per Inserimento Articolo (Prima)
1. Scansiona barcode
2. Premi Enter
3. **Clicca su campo quantità** ⏱️ +1-2 secondi
4. Digita quantità
5. Clicca Avanti
6. Clicca Conferma
7. **Clicca su campo barcode** ⏱️ +1-2 secondi

**Tempo totale:** ~15-18 secondi/articolo

### Tempo per Inserimento Articolo (Dopo)
1. Scansiona barcode
2. Premi Enter
3. Digita quantità (focus automatico)
4. Clicca Avanti
5. Clicca Conferma
6. Scansiona prossimo (focus automatico)

**Tempo totale:** ~10-12 secondi/articolo

**Risparmio:** ~30-40% di tempo per articolo

---

## Browser Testing

Testare su tutti i browser principali:

- ✅ **Chrome/Edge** (Chromium)
- ✅ **Firefox**
- ✅ **Safari** (se disponibile)

### Note Browser-Specific
- I delays (50ms/100ms) dovrebbero funzionare su tutti i browser
- `FocusAsync()` e `SelectAsync()` sono supportati da MudBlazor su tutti i browser

---

## Troubleshooting

### Focus non funziona
1. Verificare che il campo sia visibile (no `display: none`)
2. Controllare console browser per errori JavaScript
3. Verificare che il componente sia completamente renderizzato
4. Aumentare i delays se necessario (da 100ms a 200ms)

### Focus intermittente
- Possibile problema di timing del DOM
- Verificare che `StateHasChanged()` sia chiamato prima del focus
- Controllare che non ci siano altri componenti che "rubano" il focus

### Testo non selezionato in Edit
- Verificare che `SelectAsync()` sia supportato dal componente
- In caso contrario, potrebbe essere necessario JS Interop

---

## Conclusioni Test

Dopo aver completato tutti i test, compilare questa checklist:

- [ ] Test 1: Auto-focus apertura pagina - ✅ Passato / ❌ Fallito
- [ ] Test 2: Auto-focus nuova sessione - ✅ Passato / ❌ Fallito
- [ ] Test 3: Auto-focus quantità - ✅ Passato / ❌ Fallito
- [ ] Test 4: Auto-focus modifica riga - ✅ Passato / ❌ Fallito
- [ ] Test 5: Ritorno focus barcode - ✅ Passato / ❌ Fallito
- [ ] Test 6: Focus skip prodotto - ✅ Passato / ❌ Fallito
- [ ] Test 7: Focus annulla dialog - ✅ Passato / ❌ Fallito
- [ ] Test 8: Focus ubicazione singola - ✅ Passato / ❌ Fallito
- [ ] Test 9: Focus ubicazioni multiple - ✅ Passato / ❌ Fallito
- [ ] Test 10: Workflow completo E2E - ✅ Passato / ❌ Fallito

**Note aggiuntive:**
[Inserire qui eventuali osservazioni, bug trovati, o suggerimenti]
