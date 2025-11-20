# Guida Utente - Nuove Funzionalità Inventario

## Issue #614 - Ottimizzazione Procedura Inventario

**Versione:** 1.0  
**Data:** Novembre 2025  
**Funzionalità:** Merge automatico righe duplicate + Audit barcode

---

## 📋 Panoramica

Questa guida descrive le nuove funzionalità aggiunte alla procedura di inventario per rendere il lavoro più veloce, preciso e tracciabile.

### Cosa è cambiato? ✨

1. **✅ Merge Automatico Righe Duplicate**
   - Non serve più gestire manualmente righe duplicate
   - Le quantità vengono sommate automaticamente

2. **✅ Pannello Audit Codici Assegnati**
   - Visualizza tutti i barcode/codici assegnati durante la sessione
   - Tracciabilità completa delle operazioni

---

## 🎯 Funzionalità 1: Merge Automatico Righe

### Come Funziona

**Prima (comportamento vecchio):**
```
1. Scansioni prodotto "Penne Bic" in ubicazione A1 → Qty: 5
2. Scansioni di nuovo "Penne Bic" in ubicazione A1 → Qty: 3
3. Scansioni ancora "Penne Bic" in ubicazione A1 → Qty: 2

Risultato: 3 righe separate da gestire ❌
┌────────────────────────────────────────────┐
│ Penne Bic - Ubicazione A1 - Quantità: 5   │
│ Penne Bic - Ubicazione A1 - Quantità: 3   │
│ Penne Bic - Ubicazione A1 - Quantità: 2   │
└────────────────────────────────────────────┘
```

**Adesso (comportamento nuovo):**
```
1. Scansioni prodotto "Penne Bic" in ubicazione A1 → Qty: 5
2. Scansioni di nuovo "Penne Bic" in ubicazione A1 → Qty: 3
   → Quantità aggiornata automaticamente a 8 ✅
3. Scansioni ancora "Penne Bic" in ubicazione A1 → Qty: 2
   → Quantità aggiornata automaticamente a 10 ✅

Risultato: 1 sola riga già consolidata ✅
┌────────────────────────────────────────────┐
│ Penne Bic - Ubicazione A1 - Quantità: 10  │
└────────────────────────────────────────────┘
```

### Vantaggi

✅ **Più Veloce**
- Nessuna gestione manuale di righe duplicate
- Meno click per completare l'inventario

✅ **Più Preciso**
- Somma automatica riduce errori di calcolo
- Quantità sempre aggiornata in tempo reale

✅ **Più Pulito**
- Documento finale più leggibile
- Una riga per prodotto/ubicazione

### Quando Avviene il Merge?

Il merge automatico avviene quando:
- ✅ **Stesso prodotto** (anche con codice barcode diverso)
- ✅ **Stessa ubicazione** (es. A1-01-01)
- ✅ **Stesso documento** di inventario

Il merge NON avviene quando:
- ❌ Prodotto diverso
- ❌ Ubicazione diversa
- ❌ Unità di misura diversa (es. pezzi vs scatole)

### Esempio con Unità di Misura Alternative

```
Scenario: Penne vendute singolarmente (PZ) o a scatole (CF)

1. Scansioni barcode singola penna → 10 PZ
2. Scansioni barcode scatola (1 CF = 12 PZ) → 2 CF

Risultato: 2 righe separate (UoM diverse)
┌────────────────────────────────────────────┐
│ Penne - A1 - 10 PZ (pezzi)                 │
│ Penne - A1 - 2 CF (scatole = 24 pezzi)     │
└────────────────────────────────────────────┘

NOTA: Le quantità sono corrette perché il sistema
      rispetta il fattore di conversione!
```

---

## 🔍 Funzionalità 2: Pannello Audit Codici

### Dove Si Trova

Nella pagina **Procedura Inventario**, dopo la tabella delle righe del documento, troverai un nuovo pannello:

```
┌─────────────────────────────────────────────────────┐
│ 🔍 Codici Assegnati                      [Badge: 3] │
│                                                [▼]   │
├─────────────────────────────────────────────────────┤
│ Revisione mapping barcode/prodotto creati in       │
│ questa sessione                                      │
└─────────────────────────────────────────────────────┘
```

### Come Funziona

**Quando assegni un barcode a un prodotto** o **crei un nuovo prodotto con barcode**, il sistema traccia l'operazione nel pannello audit.

#### Esempio 1: Assegnazione Barcode a Prodotto Esistente

```
1. Scansioni barcode: 8001234567890
2. Sistema non trova il prodotto
3. Cerchi "Penne Bic" e assegni il barcode
4. ✅ Operazione registrata nel pannello audit

Pannello mostra:
┌────────────────────────────────────────────────────────┐
│ Barcode        │ Tipo │ Prodotto      │ UoM  │ Data    │
├────────────────────────────────────────────────────────┤
│ 8001234567890  │ EAN  │ Penne Bic     │ PZ   │ 10:30   │
│                │      │ Cod: PEN-001  │ x1   │ [👁️]   │
└────────────────────────────────────────────────────────┘
```

#### Esempio 2: Creazione Nuovo Prodotto con Barcode

```
1. Scansioni barcode: 8009876543210
2. Sistema non trova il prodotto
3. Clicchi "Crea Nuovo Prodotto"
4. Compili form e salvi
5. ✅ Operazione registrata nel pannello audit

Pannello mostra:
┌────────────────────────────────────────────────────────┐
│ Barcode        │ Tipo │ Prodotto      │ UoM  │ Data    │
├────────────────────────────────────────────────────────┤
│ 8009876543210  │ EAN  │ Matita HB     │ PZ   │ 10:35   │
│                │      │ Cod: MAT-002  │ x1   │ [👁️]   │
│ 8001234567890  │ EAN  │ Penne Bic     │ PZ   │ 10:30   │
│                │      │ Cod: PEN-001  │ x1   │ [👁️]   │
└────────────────────────────────────────────────────────┘
```

### Informazioni Visualizzate

Il pannello mostra per ogni codice assegnato:

| Colonna | Descrizione | Esempio |
|---------|-------------|---------|
| **Barcode/Codice** | Il codice scansionato o inserito | `8001234567890` |
| **Tipo** | Tipo di codice (EAN, UPC, SKU, etc.) | `EAN` |
| **Prodotto** | Nome e codice prodotto associato | `Penne Bic` <br> `Cod: PEN-001` |
| **Unità** | Unità di misura (se alternativa) | `PZ` (base) <br> `CF` (alternativa) |
| **Fattore** | Fattore di conversione (se != 1) | `x1` (base) <br> `x12` (1 CF = 12 PZ) |
| **Assegnato il** | Data e ora dell'assegnazione | `20/11/2025 10:30:45` |
| **Azioni** | Link rapido al dettaglio prodotto | 👁️ (icona visualizza) |

### Badge Contatore

Il pannello ha un **badge verde** che mostra quanti codici hai assegnato:

```
🔍 Codici Assegnati                      [Badge: 5]
```

Questo ti permette di vedere a colpo d'occhio quante operazioni di assegnazione hai fatto durante la sessione.

### Ordinamento

I codici sono mostrati in **ordine cronologico inverso** (più recenti prima), così vedi subito le ultime operazioni.

---

## 💡 Casi d'Uso Comuni

### Caso 1: Inventario Normale (Nessun Nuovo Barcode)

**Scenario:** Inventario di routine, tutti i prodotti già censiti.

**Flusso:**
1. Scansioni barcode prodotto → Sistema trova prodotto
2. Inserisci quantità → Riga aggiunta al documento
3. Ripeti per tutti i prodotti
4. Finalizzi inventario

**Pannello Audit:** Rimane vuoto (nessuna assegnazione) ✅

---

### Caso 2: Prodotto Nuovo Durante Inventario

**Scenario:** Trovi un prodotto non censito nel sistema.

**Flusso:**
1. Scansioni barcode → Sistema NON trova prodotto
2. Dialog "Prodotto non trovato" appare
3. Clicchi "Crea Nuovo Prodotto"
4. Compili form:
   - Nome prodotto
   - Codice interno
   - Barcode (già precompilato)
   - Prezzo, categorie, etc.
5. Salvi → Prodotto creato
6. Inserisci quantità → Riga aggiunta
7. ✅ Assegnazione tracciata in pannello audit

**Pannello Audit:** Mostra il nuovo codice assegnato ✅

---

### Caso 3: Barcode Alternativo per Prodotto Esistente

**Scenario:** Stesso prodotto con packaging/formato diverso (es. singolo vs multiplo).

**Flusso:**
1. Scansioni barcode scatola → Sistema NON trova codice
2. Dialog "Prodotto non trovato" appare
3. Cerchi prodotto esistente (es. "Penne Bic")
4. Selezioni prodotto dalla ricerca
5. Scegli tipo codice (es. "EAN")
6. (Opzionale) Inserisci descrizione alternativa (es. "Scatola da 12")
7. Confermi assegnazione
8. Sistema assegna barcode al prodotto
9. ✅ Assegnazione tracciata in pannello audit

**Pannello Audit:** Mostra il barcode alternativo assegnato ✅

---

### Caso 4: Prodotto con Unità di Misura Alternative

**Scenario:** Prodotto vendibile in diverse UoM (es. pezzi e scatole).

**Flusso:**
1. Scansioni barcode scatola → Sistema NON trova codice
2. Dialog "Prodotto non trovato" appare
3. Selezioni "Crea con UoM Alternative"
4. Compili form:
   - Nome prodotto
   - Codice barcode singolo (PZ)
   - Codice barcode scatola (CF)
   - Fattore conversione: 1 CF = 12 PZ
5. Salvi → Prodotto creato con 2 barcode
6. ✅ Entrambe le assegnazioni tracciate in pannello audit

**Pannello Audit:** Mostra entrambi i codici con fattore conversione ✅

```
┌────────────────────────────────────────────────────────┐
│ Barcode        │ Tipo │ Prodotto      │ UoM  │ Data    │
├────────────────────────────────────────────────────────┤
│ 8009876543222  │ EAN  │ Penne Bic     │ CF   │ 11:05   │
│                │      │ Cod: PEN-001  │ x12  │ [👁️]   │
│ 8009876543210  │ EAN  │ Penne Bic     │ PZ   │ 11:05   │
│                │      │ Cod: PEN-001  │ x1   │ [👁️]   │
└────────────────────────────────────────────────────────┘
```

---

## 🎨 Interfaccia Utente

### Pannello Collassabile

Il pannello audit è **collassabile** per non ingombrare:

**Chiuso (default):**
```
┌─────────────────────────────────────────────────────┐
│ 🔍 Codici Assegnati                      [Badge: 3] │
│                                                [▼]   │
├─────────────────────────────────────────────────────┤
│ Revisione mapping barcode/prodotto...        [click]│
└─────────────────────────────────────────────────────┘
```

**Aperto:**
```
┌─────────────────────────────────────────────────────┐
│ 🔍 Codici Assegnati                      [Badge: 3] │
│                                                [▲]   │
├─────────────────────────────────────────────────────┤
│ Barcode        │ Tipo │ Prodotto      │ UoM  │ Data │
├────────────────────────────────────────────────────┤
│ 8009876543210  │ EAN  │ Matita HB     │ PZ   │ ...  │
│ 8001234567890  │ EAN  │ Penne Bic     │ PZ   │ ...  │
│ 8001234567123  │ UPC  │ Gomma         │ PZ   │ ...  │
└─────────────────────────────────────────────────────┘
```

### Link Rapido Prodotto

Cliccando sull'icona 👁️ nella colonna Azioni, vieni portato alla **pagina dettaglio del prodotto** dove puoi:
- Vedere tutte le informazioni
- Modificare dati
- Vedere tutti i codici associati
- Vedere lo storico

---

## ⚙️ Dettagli Tecnici

### Quando Viene Tracciato un Codice?

Il sistema traccia un codice nel pannello audit quando:

1. **Assegni barcode a prodotto esistente**
   - Via dialog "Prodotto non trovato"
   - Ricerca + selezione prodotto
   - Conferma assegnazione

2. **Crei nuovo prodotto con barcode**
   - Via dialog "Creazione Rapida"
   - Via dialog "Creazione Avanzata con UoM"
   - Form compilato e salvato

### Durata Tracking

Il tracking è **per sessione**:
- ✅ Dura finché rimani nella pagina
- ✅ Persiste se cambi ubicazione
- ✅ Persiste se aggiungi righe
- ❌ Si resetta se chiudi/riapri la pagina
- ❌ Si resetta se finalizzi l'inventario
- ❌ Si resetta se annulli la sessione

**Perché?** Il pannello è pensato per **audit durante la sessione corrente**, non per storico permanente.

### Limite Tracking

Il sistema traccia fino a **500 assegnazioni** per sessione:
- In caso di overflow, rimuove le più vecchie (FIFO)
- 500 è un limite molto generoso (raramente raggiunto)
- Evita problemi di memoria in sessioni lunghissime

### Rispetto Fattore Conversione

Quando scansioni un prodotto con UoM alternativa:

**Esempio:**
```
Prodotto: Acqua Minerale
- Barcode bottiglia (500ml): 8001234567890 → UoM: PZ (pezzi)
- Barcode cartone (6x500ml): 8001234567123 → UoM: CF (cartoni)
- Fattore: 1 CF = 6 PZ

Scansioni:
1. Bottiglia x5 → 5 PZ
2. Cartone x2 → 2 CF = 12 PZ (calcolato automaticamente)

Risultato: 2 righe separate
- Riga 1: 5 PZ
- Riga 2: 2 CF (= 12 PZ)

Totale reale: 17 pezzi (5 + 12)
```

Il sistema **mantiene separate** le righe con UoM diverse per tracciabilità, ma calcola correttamente le quantità base.

---

## ❓ FAQ (Domande Frequenti)

### Q1: Se scansiono stesso prodotto 10 volte, avrò 10 righe?

**R:** No! Avrai **1 sola riga** con quantità = somma di tutte le scansioni. È il merge automatico! ✅

---

### Q2: Posso disabilitare il merge automatico?

**R:** No, il merge è sempre attivo. È progettato per semplificare il lavoro, non per complicarlo. Se hai bisogno di righe separate, usa ubicazioni diverse.

---

### Q3: Il merge funziona anche con UoM diverse?

**R:** No, il merge avviene solo per **stesso prodotto + stessa ubicazione + stessa UoM**. Se scansioni sia pezzi che scatole, avrai 2 righe separate (corretto!).

---

### Q4: Posso esportare il pannello audit in Excel?

**R:** Non ancora. Il pannello è pensato per audit "live" durante la sessione. Per audit permanente, usa i log di sistema o il report inventario finale.

---

### Q5: I codici nel pannello audit vengono salvati nel database?

**R:** No, il tracking è **solo in memoria** durante la sessione. I codici assegnati vengono comunque salvati nel database (tabella ProductCodes), ma il pannello audit è solo un comodo riepilogo temporaneo.

---

### Q6: Cosa succede se refresh la pagina?

**R:** Il pannello audit si **resetta** (è in memoria). Ma non preoccuparti:
- Le righe del documento rimangono
- I codici assegnati rimangono nel database
- Puoi continuare l'inventario normalmente

---

### Q7: Posso vedere chi ha assegnato un codice?

**R:** Nel pannello audit c'è la colonna "Assegnato da", ma attualmente mostra "Current User" (tu). In futuro potrebbe mostrare il nome utente reale.

---

### Q8: Il merge funziona se scansiono da terminali diversi?

**R:** Sì! Il merge avviene **lato server**, quindi funziona anche se usi più dispositivi contemporaneamente (utile per team grandi).

---

## 📞 Supporto

Se hai problemi o domande:
1. Consulta questa guida
2. Verifica i log di sistema (pulsante in alto a destra)
3. Contatta il supporto tecnico

---

## 🔄 Changelog

### Versione 1.0 (Novembre 2025)
- ✅ Implementato merge automatico righe duplicate
- ✅ Implementato pannello audit codici assegnati
- ✅ Supporto conversion factor per UoM alternative
- ✅ Test completi e documentazione

---

**Buon inventario!** 📦✨
