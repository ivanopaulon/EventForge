# Guida Utente - Nuove Funzionalità Procedura Inventario

## 🎉 Benvenuto alla Procedura di Inventario Potenziata!

Questa guida ti aiuterà a utilizzare al meglio le nuove funzionalità aggiunte alla procedura di inventario di EventForge.

---

## 📊 Pannello Statistiche in Tempo Reale

Durante una sessione di inventario, ora puoi vedere in tempo reale:

### 1. Totale Articoli (Blu)
- Mostra quanti prodotti hai scansionato finora
- Si aggiorna automaticamente ogni volta che aggiungi un articolo
- Utile per tenere traccia dei progressi

### 2. Eccedenze (Verde)
- Conta i prodotti trovati in quantità maggiore rispetto allo stock registrato
- Numero **positivo** = hai trovato più merce di quella che il sistema si aspettava
- Esempio: Sistema dice 10 pezzi, tu ne conti 12 → Eccedenza di +2

### 3. Mancanze (Giallo)
- Conta i prodotti trovati in quantità minore rispetto allo stock registrato
- Numero **negativo** = hai trovato meno merce di quella registrata
- Esempio: Sistema dice 10 pezzi, tu ne conti 8 → Mancanza di -2

### 4. Durata Sessione (Azzurro)
- Timer che mostra quanto tempo è trascorso dall'inizio dell'inventario
- Formato: MM:SS (minuti:secondi)
- Utile per pianificare le prossime sessioni

**💡 Suggerimento**: Tieni d'occhio le statistiche per identificare rapidamente se ci sono molte discrepanze che richiedono attenzione!

---

## 📝 Registro Operazioni

### Cos'è?
Una timeline che registra **tutto** quello che fai durante l'inventario. Ogni azione viene tracciata con data, ora e dettagli.

### Come Leggerla
La timeline mostra le ultime 20 operazioni, dalla più recente alla più vecchia:

#### 🔵 Informazioni (Blu)
Operazioni normali come ricerca prodotti o avvio export

**Esempio:**
```
Ricerca prodotto
Codice: 8001234567890
15/01/2025 14:35:22
```

#### ✅ Successo (Verde)
Operazioni completate con successo

**Esempio:**
```
Articolo aggiunto
Penne BIC Blu - Ubicazione: A-01-05 - Quantità: 50
15/01/2025 14:35:45
```

#### ⚠️ Attenzione (Giallo)
Situazioni che richiedono attenzione

**Esempio:**
```
Prodotto non trovato
Codice: 8001234567999
15/01/2025 14:36:12
```

#### ❌ Errore (Rosso)
Problemi tecnici o errori

**Esempio:**
```
Errore nella ricerca
Network timeout
15/01/2025 14:37:00
```

### Perché è Utile?
- ✅ **Tracciabilità**: Puoi vedere esattamente cosa hai fatto e quando
- ✅ **Audit**: In caso di domande, hai una prova documentale
- ✅ **Risoluzione problemi**: Se qualcosa va storto, puoi capire cosa è successo
- ✅ **Apprendimento**: Rivedi il processo per migliorare la prossima volta

---

## 💾 Esportazione Documento

### Come Esportare
1. Durante una sessione di inventario attiva
2. Clicca sul pulsante **"Esporta"** (icona download) nella barra in alto
3. Il file CSV viene scaricato automaticamente nel tuo browser

### Cosa Contiene il File
Il file Excel/CSV include tutte le informazioni del documento:

| Colonna | Descrizione | Esempio |
|---------|-------------|---------|
| Codice Prodotto | Codice a barre del prodotto | PROD001 |
| Nome Prodotto | Nome completo | Penne BIC Blu |
| Ubicazione | Dove si trova | A-01-05 |
| Quantità Contata | Quello che hai trovato | 50 |
| Aggiustamento | Differenza con sistema | +5 |
| Note | Tue annotazioni | Trovate in magazzino B |
| Data/Ora | Quando hai scansionato | 15/01/2025 14:35 |

### Quando Esportare
- **Prima di finalizzare**: Per rivedere tutto con calma su Excel
- **Per backup**: Conservare una copia dei dati
- **Per reporting**: Condividere con responsabili o ufficio amministrativo
- **Per analisi**: Elaborare i dati con strumenti esterni

### Nome del File
I file vengono salvati con questo formato:
```
Inventario_INV-001_20250115_143500.csv
```
Dove:
- `INV-001` = Numero documento
- `20250115` = Data (2025/01/15)
- `143500` = Ora (14:35:00)

**💡 Suggerimento**: Esporta sempre il documento prima di finalizzare, così hai una copia di sicurezza!

---

## 🔍 Filtro "Solo Differenze"

### Come Funziona
Nella tabella degli articoli, c'è un interruttore **"Solo Differenze"** che puoi attivare:

- **OFF** (predefinito): Mostra tutti gli articoli scansionati
- **ON**: Mostra solo gli articoli con differenze (+ o -)

### Quando Usarlo
- ✅ Quando hai scansionato molti articoli e vuoi vedere solo i problemi
- ✅ Per ricontrollare velocemente le discrepanze prima di finalizzare
- ✅ Per identificare quali prodotti richiedono verifica
- ✅ Per concentrarti sugli articoli critici

### Esempio Pratico
Hai scansionato 100 articoli:
- 85 hanno quantità corretta (0 differenza)
- 10 hanno eccedenze (+)
- 5 hanno mancanze (-)

Attivando il filtro, vedrai solo quei 15 articoli con differenze, risparmiando tempo!

---

## 🎯 Icone e Colori Migliorati

### Nella Tabella Articoli

#### Aggiustamenti
Ora ogni aggiustamento ha un'icona che lo rende immediatamente riconoscibile:

- **📈 Freccia Su (Verde)**: Eccedenza - Hai trovato più merce
- **📉 Freccia Giù (Giallo)**: Mancanza - Hai trovato meno merce
- **➖ Linea (Grigio)**: Nessuna differenza - Quantità corretta

#### Note
- **💬 Icona Commento (Blu)**: Indica che ci sono note
- **Passa il mouse** sull'icona per leggere le note senza aprire nulla

---

## 💡 Tooltip Esplicativi

### Cosa Sono
Piccole etichette che appaiono quando passi il mouse sui pulsanti, spiegando cosa fanno.

### Dove Trovarli
- **Pulsante Esporta**: "Esporta documento in Excel"
- **Pulsante Finalizza**: "Applica tutti gli aggiustamenti e chiudi la sessione"
- **Pulsante Annulla**: "Annulla sessione senza salvare"
- **Filtro Differenze**: "Mostra solo articoli con differenze"

**💡 Suggerimento**: Se non sei sicuro di cosa fa un pulsante, passa il mouse sopra per vedere la spiegazione!

---

## 📋 Flusso di Lavoro Consigliato

### 1. Preparazione
```
✓ Verifica di avere scanner funzionante
✓ Stampa lista ubicazioni se necessario
✓ Prepara area di lavoro
```

### 2. Avvio Sessione
```
✓ Seleziona magazzino
✓ Clicca "Avvia Sessione"
✓ Verifica che il banner blu appaia in alto
✓ Controlla che le statistiche siano a 0
```

### 3. Scansione Prodotti
```
✓ Scansiona codice a barre (o digita manualmente)
✓ Premi ENTER per cercare
✓ Verifica nome prodotto
✓ Seleziona ubicazione
✓ Inserisci quantità contata
✓ Aggiungi note se necessario (es. "Prodotto danneggiato")
✓ Premi ENTER o clicca "Aggiungi al Documento"
✓ Controlla che appaia nella tabella sotto
✓ Guarda le statistiche aggiornarsi
```

### 4. Monitoraggio
```
✓ Controlla periodicamente le statistiche
✓ Se vedi molte mancanze/eccedenze, ricontrolla
✓ Usa il registro operazioni per verificare cosa hai fatto
✓ Attiva "Solo Differenze" per vedere i problemi
```

### 5. Revisione
```
✓ Quando hai finito, scorri tutta la tabella
✓ Attiva filtro "Solo Differenze"
✓ Ricontrolla articoli con grosse discrepanze
✓ Verifica note inserite
✓ Esporta documento per backup
```

### 6. Finalizzazione
```
✓ Clicca "Finalizza"
✓ Leggi il messaggio di conferma (quanti articoli)
✓ Conferma solo se sei sicuro
✓ Attendi completamento
✓ Verifica messaggio di successo
```

---

## ⚠️ Domande Frequenti

### D: Cosa succede se chiudo il browser durante l'inventario?
**R**: La sessione potrebbe andare persa. È meglio esportare periodicamente per sicurezza. In futuro verrà aggiunto il salvataggio automatico.

### D: Posso modificare un articolo già aggiunto?
**R**: Attualmente no, ma puoi annullare la sessione e rifarla, oppure aggiungere una nuova riga correttiva.

### D: Il registro operazioni viene salvato?
**R**: Al momento viene salvato nei log del sistema lato server. In futuro potrebbe essere esportabile insieme al documento.

### D: Posso fare più sessioni contemporaneamente?
**R**: No, puoi avere solo una sessione attiva per volta per magazzino.

### D: Cosa succede se dimentico di finalizzare?
**R**: La sessione rimane in stato "bozza" e puoi riprenderla in seguito (funzionalità in sviluppo).

### D: L'export include anche il registro operazioni?
**R**: Attualmente no, solo i dati degli articoli. In futuro verrà aggiunta questa opzione.

### D: Posso stampare il documento?
**R**: Sì, dopo aver esportato il CSV puoi aprirlo in Excel e stamparlo da lì.

---

## 🎓 Consigli Pro

### Per Velocizzare il Lavoro
1. **Usa ENTER** invece del mouse per navigare
2. **Prepara codici ubicazione** in anticipo
3. **Fai pause regolari** ogni 50-100 articoli
4. **Esporta periodicamente** per backup

### Per Ridurre Errori
1. **Ricontrolla sempre** le discrepanze grandi
2. **Usa le note** per segnalare situazioni particolari
3. **Controlla le statistiche** prima di finalizzare
4. **Fai l'inventario in due** se possibile (uno conta, uno inserisce)

### Per Documentazione Migliore
1. **Aggiungi note dettagliate** sui problemi trovati
2. **Esporta sempre** prima di finalizzare
3. **Conserva i file CSV** per riferimento futuro
4. **Confronta con inventari precedenti** per trend

---

## 📞 Supporto

Se hai bisogno di aiuto:
- 📧 Email: support@eventforge.com
- 📱 Telefono: [Numero supporto]
- 💬 Chat: [Link chat supporto]
- 📖 Wiki: [Link documentazione completa]

---

## ✅ Checklist Prima di Iniziare

Stampa e usa questa checklist:

```
□ Scanner funzionante e carico
□ Connessione internet stabile
□ Lista ubicazioni disponibile
□ Area di lavoro organizzata
□ Tempo sufficiente (stima 2-3h per 500 articoli)
□ Backup dispositivo disponibile in caso di problemi
□ Responsabile informato dell'inventario in corso
```

---

**Buon Inventario! 🎯**

Per domande o suggerimenti su questa guida:
- Contatta il team IT
- Apri una segnalazione su GitHub
- Chiedi al tuo responsabile di magazzino

---

**Versione Guida:** 2.0  
**Data:** Gennaio 2025  
**Aggiornamenti:** Visita la documentazione online per la versione più recente
