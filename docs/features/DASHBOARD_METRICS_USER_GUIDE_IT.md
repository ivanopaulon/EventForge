# Guida Utente - Creazione Metriche Dashboard

## Introduzione

Questa guida spiega come utilizzare la nuova funzionalità di creazione e modifica delle metriche per la configurazione della dashboard.

## Accesso alla Configurazione

1. Apri una pagina con dashboard (es. Gestione Aliquote IVA)
2. Cerca il widget "Dashboard" con le metriche
3. Clicca sull'icona ingranaggio (⚙️) nell'angolo in alto a destra del widget

## Prima Configurazione

Se non hai ancora configurazioni, vedrai:
- Alert informativo: "Nessuna configurazione trovata per questa dashboard"
- Form per creare la prima configurazione con:
  - Nome configurazione (obbligatorio)
  - Descrizione (opzionale)
  - Checkbox "Imposta come predefinita"
  - Lista metriche (inizialmente vuota)

## Creare una Nuova Metrica

### Passo 1: Aprire l'Editor

Clicca sul pulsante **"Aggiungi Metrica"**

Si aprirà il dialog "Crea Nuova Metrica" con i seguenti campi:

### Passo 2: Configurazione Base

#### **Titolo Metrica** (obbligatorio)
- Nome descrittivo da visualizzare
- Esempio: "Totale Aliquote IVA", "Aliquote Attive", "Media Percentuale"

#### **Descrizione** (opzionale)
- Testo tooltip che appare al passaggio del mouse
- Spiega cosa rappresenta la metrica
- Esempio: "Numero totale di aliquote IVA nel sistema"

#### **Tipo di Metrica** (obbligatorio)
Scegli il tipo di calcolo:

| Tipo | Quando Usarlo | Esempio |
|------|---------------|---------|
| **Conteggio** | Per contare elementi | "Numero totale prodotti" |
| **Somma** | Per sommare valori numerici | "Totale vendite" |
| **Media** | Per calcolare la media | "Prezzo medio prodotti" |
| **Minimo** | Per trovare il valore più basso | "Prezzo minimo" |
| **Massimo** | Per trovare il valore più alto | "Prezzo massimo" |

### Passo 3: Configurazione Avanzata

#### **Nome Campo** (obbligatorio per Sum, Average, Min, Max)
- Nome del campo/proprietà da valutare
- **Non richiesto** per tipo "Conteggio"
- Esempi comuni:
  - `Percentage` - per aliquote IVA
  - `Amount` - per importi
  - `Quantity` - per quantità
  - `Price` - per prezzi
  - `Total` - per totali

**💡 Suggerimento**: Il campo deve corrispondere a una proprietà dell'entità che stai monitorando.

#### **Condizione di Filtro** (opzionale)
- Filtra gli elementi prima del calcolo
- Sintassi: `NomeCampo operatore valore`
- Esempi utili:
  ```
  Status == 'Active'           // Solo elementi attivi
  Amount > 0                   // Solo importi positivi
  Percentage >= 20             // Percentuale minima 20
  IsEnabled == true            // Solo elementi abilitati
  Category == 'Electronics'    // Solo categoria specifica
  ```

### Passo 4: Personalizzazione Visuale

#### **Formato Visualizzazione**
Controlla come viene mostrato il numero:

| Formato | Descrizione | Esempio Output |
|---------|-------------|----------------|
| `N0` | Numero intero | 42 |
| `N2` | Due decimali | 42.50 |
| `C0` | Valuta senza decimali | €42 |
| `C2` | Valuta con decimali | €42.50 |
| `P0` | Percentuale intera | 75% |
| `P2` | Percentuale con decimali | 75.50% |

#### **Icona**
Scegli tra 20+ icone predefinite:
- 📊 Analytics - per metriche generali
- 📈 Trending Up - per crescite
- 📉 Trending Down - per diminuzioni
- 🧮 Calculate - per calcoli
- % Percent - per percentuali
- 💰 Money - per valori monetari
- € Euro - per valute europee
- ✓ Check Circle - per successi
- ⚠ Warning - per avvisi
- E molte altre...

#### **Colore**
Scegli il colore semantico:
- 🔵 **Primary** (Blu) - per metriche generali
- ⚪ **Secondary** (Grigio) - per info secondarie
- 🟢 **Success** (Verde) - per risultati positivi
- 🔵 **Info** (Celeste) - per informazioni
- 🟠 **Warning** (Arancione) - per attenzione
- 🔴 **Error** (Rosso) - per problemi
- ⚫ **Dark** (Nero) - per contrasto

### Passo 5: Anteprima

Nella parte inferiore del dialog vedrai un'anteprima live di come apparirà la metrica:
- Icona colorata
- Titolo della metrica
- Valore di esempio formattato

Questo ti aiuta a vedere il risultato prima di salvare.

### Passo 6: Salvataggio

1. Verifica che tutti i campi obbligatori siano compilati
2. Il pulsante "Crea Metrica" si abiliterà quando la validazione è OK
3. Clicca "Crea Metrica" per salvare
4. Vedrai un messaggio di successo
5. La metrica apparirà nella lista

## Modificare una Metrica Esistente

1. Nella lista delle metriche, trova quella da modificare
2. Clicca sull'icona **matita** (✏️) accanto alla metrica
3. Si aprirà lo stesso editor con i valori pre-popolati
4. Modifica i campi desiderati
5. Clicca "Salva Modifiche"
6. La metrica verrà aggiornata

## Gestire le Metriche

### Riordinare Metriche

Usa le frecce per cambiare l'ordine:
- **↑** Freccia su - sposta la metrica in alto
- **↓** Freccia giù - sposta la metrica in basso

L'ordine determina come appaiono nella dashboard.

### Eliminare Metriche

Clicca sull'icona **cestino** (🗑️) per rimuovere una metrica dalla configurazione.

## Esempi Pratici

### Esempio 1: Conteggio Aliquote IVA Attive
```
Titolo: Aliquote Attive
Descrizione: Numero di aliquote IVA attualmente attive
Tipo: Conteggio
Nome Campo: (non richiesto)
Filtro: Status == 'Active'
Formato: N0
Icona: ✓ Check Circle
Colore: Success (Verde)
```

### Esempio 2: Media Percentuale IVA
```
Titolo: Media Percentuale
Descrizione: Percentuale media delle aliquote IVA attive
Tipo: Media
Nome Campo: Percentage
Filtro: Status == 'Active'
Formato: N2
Icona: % Percent
Colore: Info (Celeste)
```

### Esempio 3: Totale Vendite
```
Titolo: Totale Vendite Mensili
Descrizione: Somma di tutte le vendite del mese corrente
Tipo: Somma
Nome Campo: Amount
Filtro: Date >= '2025-01-01' && Date <= '2025-01-31'
Formato: C2
Icona: 💰 Money
Colore: Success (Verde)
```

### Esempio 4: Prezzo Massimo Prodotto
```
Titolo: Prezzo Più Alto
Descrizione: Prodotto con il prezzo di vendita più elevato
Tipo: Massimo
Nome Campo: Price
Filtro: IsActive == true
Formato: C2
Icona: 📈 Trending Up
Colore: Warning (Arancione)
```

## Salvare la Configurazione

Dopo aver aggiunto tutte le metriche desiderate:

1. Assicurati di aver aggiunto almeno una metrica
2. Compila il nome della configurazione (se prima configurazione)
3. Clicca "Salva Configurazione"
4. La configurazione verrà salvata nel database
5. La dashboard si aggiornerà automaticamente con le nuove metriche

## Gestire Configurazioni Multiple

Se hai già configurazioni salvate:
- Vedrai una lista di tutte le configurazioni esistenti
- Ogni configurazione mostra:
  - Nome
  - Descrizione (se presente)
  - Badge "Predefinita" se è la configurazione di default
- Puoi:
  - ✏️ Modificare configurazioni esistenti
  - 🗑️ Eliminare configurazioni
  - ⭐ Impostare come predefinita
  - ➕ Creare nuove configurazioni

## Consigli e Best Practices

### 🎯 Mantieni Metriche Focalizzate
- Ogni metrica dovrebbe rispondere a una specifica domanda
- Non creare troppe metriche (4-8 è un buon numero)
- Prioritizza le metriche più importanti in alto

### 📊 Usa Colori Semantici
- Verde per risultati positivi (vendite, attivi, successi)
- Rosso per problemi (errori, mancanze)
- Arancione per avvisi (soglie raggiunte)
- Blu per informazioni generali

### 🏷️ Nomi Chiari e Descrittivi
- Usa titoli brevi ma chiari
- Aggiungi sempre una descrizione utile
- Pensa a chi leggerà la dashboard

### 🔍 Testa i Filtri
- Verifica che i filtri producano i risultati attesi
- Usa filtri semplici quando possibile
- Documenta filtri complessi nelle descrizioni

### 📐 Formati Appropriati
- Usa N0 per conteggi (mai decimali)
- Usa C2 per valori monetari (sempre 2 decimali)
- Usa P0 o P2 per percentuali
- Mantieni consistenza tra metriche simili

## Risoluzione Problemi

### Pulsante "Salva" Disabilitato
**Causa**: Validazione non superata
**Soluzione**: Verifica che:
- Titolo sia compilato
- Nome Campo sia compilato (per Sum, Average, Min, Max)
- Almeno una metrica sia stata aggiunta

### Metrica Mostra Valore Zero
**Causa**: Nessun dato corrisponde ai filtri
**Soluzione**: 
- Verifica la condizione di filtro
- Controlla che i dati esistano
- Prova senza filtro per vedere tutti i dati

### Campo Non Trovato
**Causa**: Nome campo errato
**Soluzione**:
- Verifica il nome esatto del campo nell'entità
- I nomi campo sono case-sensitive
- Consulta la documentazione dell'entità

## Supporto

Per ulteriore assistenza o segnalazione bug:
- Consulta la documentazione tecnica: `IMPLEMENTAZIONE_METRICHE_DASHBOARD.md`
- Verifica i log dell'applicazione per errori
- Contatta il team di supporto con screenshot del problema

---

**Versione**: 1.0  
**Data**: Novembre 2025  
**Autore**: Prym Development Team
