# Guida Utente: Riconciliazione Giacenze

## Cos'è la Riconciliazione Giacenze?

La funzionalità di **Riconciliazione Giacenze** permette di verificare e correggere le discrepanze tra:
- Le giacenze registrate nel database (Stock.Quantity)
- Le giacenze calcolate dai movimenti documentati (documenti, inventari, movimenti manuali)

## Quando usarla?

Utilizza la riconciliazione giacenze quando:
- ✅ Sospetti che le giacenze non siano corrette
- ✅ Dopo l'importazione di dati da sistemi esterni
- ✅ Dopo operazioni di inventario fisico
- ✅ Come verifica periodica della qualità dei dati
- ✅ Prima di operazioni critiche (chiusura contabile, bilancio)

## Come funziona?

### 1. Accesso alla Funzione

Naviga nel menu:
```
Magazzino → Giacenze → Riconciliazione Giacenze
```

### 2. Impostazione Filtri

**Periodo:**
- `Da Data` - `A Data`: limita l'analisi a un periodo specifico

**Filtri:**
- `Magazzino`: analizza solo un magazzino specifico
- `Ubicazione`: analizza solo un'ubicazione specifica
- `Prodotto`: analizza solo un prodotto specifico

**Opzioni:**
- ☑️ `Includi documenti di carico/scarico`: considera i movimenti da DDT, fatture, ordini
- ☑️ `Includi inventari fisici`: considera gli inventari eseguiti (sostituiscono la giacenza)
- ☑️ `Solo prodotti con discrepanze`: mostra solo i prodotti con differenze

### 3. Calcolo Preview

1. Clicca su **🔍 Calcola Preview**
2. Il sistema analizza i movimenti e calcola le giacenze teoriche
3. Viene mostrata una tabella con:
   - **Giacenza Attuale**: quantità registrata nel database
   - **Giacenza Ricalcolata**: quantità calcolata dai movimenti
   - **Differenza**: discrepanza trovata
   - **Gravità**: livello di discrepanza (Corretto, Minore, Maggiore, Mancante)

### 4. Interpretazione Risultati

**Badge Gravità:**
- ✅ **Corretto** (verde): nessuna differenza
- ⚠️ **Discrepanza Minore** (giallo): differenza < 10%
- ❌ **Discrepanza Maggiore** (rosso): differenza > 10%
- 🔴 **Mancante** (viola): giacenza attuale 0 ma dovrebbe esserci

**Espansione Righe:**
- Clicca su una riga per vedere i movimenti fonte
- Formato: `📄 DDT-001 (15/01/2026): +10 (Carico)`
- Gli inventari che sostituiscono la giacenza sono evidenziati

### 5. Selezione Elementi

- Seleziona le giacenze da correggere tramite checkbox
- Usa `Seleziona tutto` / `Deseleziona tutto` per velocità
- **Consiglio**: applica solo le correzioni con discrepanze maggiori inizialmente

### 6. Applicazione Correzioni

1. Seleziona gli elementi da correggere
2. Clicca su **💾 Applica Ricalcolo**
3. **IMPORTANTE**: Compare un dialogo di conferma con:
   - Numero di giacenze da aggiornare
   - Dettagli delle operazioni che verranno eseguite
   - Avviso che l'operazione **NON può essere annullata**
4. Inserisci il **Motivo** (obbligatorio)
5. Conferma l'applicazione

**Cosa succede:**
- ✅ Aggiorna `Stock.Quantity` con il valore ricalcolato
- ✅ Crea movimenti di aggiustamento per tracciabilità
- ✅ Registra l'operazione nell'audit log
- ✅ Mostra riepilogo delle modifiche applicate

### 7. Esportazione Report

Clicca su **📥 Esporta Report** per scaricare un file Excel con:
- Summary: riepilogo statistiche
- Details: dettaglio di tutte le giacenze analizzate
- Movements: movimenti fonte per ogni prodotto

## Best Practices

### ✅ Fare

1. **Backup prima di applicare**: esegui sempre un backup prima di correzioni massive
2. **Testa su piccolo campione**: applica prima su pochi prodotti per verificare
3. **Documenta il motivo**: inserisci sempre un motivo chiaro e dettagliato
4. **Verifica post-applicazione**: ricalcola dopo l'applicazione per verificare
5. **Esporta report**: conserva il report Excel per audit e documentazione

### ❌ Evitare

1. **Non applicare senza analisi**: non applicare correzioni senza aver verificato i movimenti
2. **Non ignorare discrepanze maggiori**: investigare sempre le differenze >10%
3. **Non applicare su tutto**: seleziona solo le correzioni necessarie
4. **Non applicare durante operazioni**: evita durante chiusure contabili o inventari in corso

## Risoluzione Problemi

### Problema: Discrepanze persistenti dopo applicazione

**Cause possibili:**
- Nuovi movimenti creati dopo il calcolo
- Documenti non considerati nei filtri
- Bug nel calcolo

**Soluzione:**
1. Ricalcola senza filtri di data
2. Verifica che tutti i documenti abbiano ProductId e LocationId
3. Controlla i movimenti manuali

### Problema: Differenze inspiegabili

**Verifica:**
1. Espandi la riga e analizza i movimenti fonte
2. Controlla se ci sono inventari che sostituiscono la giacenza
3. Verifica documenti con tipo di movimento errato (IsStockIncrease)

## FAQ

**Q: Posso annullare una riconciliazione applicata?**
A: No, l'operazione è irreversibile. Puoi però applicare una nuova riconciliazione inversa.

**Q: I movimenti di aggiustamento influenzano il calcolo?**
A: Sì, sono movimenti manuali e vengono considerati nei calcoli successivi.

**Q: Cosa succede se applico più volte?**
A: Ogni applicazione crea nuovi movimenti di aggiustamento. Meglio evitare applicazioni multiple ravvicinate.

**Q: Come verifico che la riconciliazione sia corretta?**
A: Ricalcola subito dopo l'applicazione: tutte le giacenze dovrebbero risultare "Corrette".

**Q: Posso riconciliare durante un inventario in corso?**
A: Sconsigliato. Completa prima l'inventario e poi esegui la riconciliazione.

## Contatti

Per supporto o segnalazione problemi:
- Documentazione tecnica: `STOCK_RECONCILIATION_TECHNICAL.md`
- Repository: https://github.com/ivanopaulon/EventForge
