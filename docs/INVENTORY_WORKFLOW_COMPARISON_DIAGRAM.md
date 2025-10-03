# Diagramma Comparativo: Procedura Inventario

## Workflow Prima dell'Ottimizzazione

```
┌─────────────────────────────────────────────────────────────────┐
│                    VECCHIO WORKFLOW                             │
│                   (Single-Entry Approach)                       │
└─────────────────────────────────────────────────────────────────┘

┌─────────────┐
│  Operatore  │
└──────┬──────┘
       │
       ▼
┌─────────────────────────────────────┐
│  1. Scansiona Codice a Barre        │
│     (Scanner/Digitazione manuale)   │
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│  2. Click "Cerca"                   │   ❌ Click mouse richiesto
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│  3. Attendi Risposta Server         │
│     → Prodotto Trovato              │
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│  4. Click Dropdown Ubicazione       │   ❌ Click mouse richiesto
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│  5. Click Opzione Ubicazione        │   ❌ Click mouse richiesto
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│  6. Click Campo Quantità            │   ❌ Click mouse richiesto
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│  7. Digita Quantità                 │
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│  8. Click "Salva"                   │   ❌ Click mouse richiesto
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│  ⚠️  STOCK MODIFICATO IMMEDIATAMENTE│   ⚠️ Nessuna revisione
│      Movimento Creato                │   ⚠️ Non reversibile
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│  Ripeti per Articolo Successivo     │
└─────────────────────────────────────┘

Totale per 1 articolo:
├─ 5 click mouse
├─ 2 digitazioni (barcode + quantity)
├─ ~30 secondi
└─ ⚠️ Nessuna possibilità di revisione o annullamento
```

---

## Workflow Dopo l'Ottimizzazione

```
┌─────────────────────────────────────────────────────────────────┐
│                    NUOVO WORKFLOW                               │
│                  (Document-Based Approach)                      │
└─────────────────────────────────────────────────────────────────┘

┌─────────────┐
│  Operatore  │
└──────┬──────┘
       │
       ▼
┌═════════════════════════════════════┐
║  FASE 1: AVVIO SESSIONE (1 volta)  ║
└═════════════════════════════════════┘
       │
       ▼
┌─────────────────────────────────────┐
│  Seleziona Magazzino                │   ✅ Auto-selezionato se uno solo
│  (auto-selezionato se possibile)    │
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│  Click "Avvia Sessione"             │   ✅ Solo una volta
└──────┬──────────────────────────────┘
       │
       ▼
┌═════════════════════════════════════════════════════════════════┐
║                  DOCUMENTO INVENTARIO CREATO                    ║
║  ┌─────────────────────────────────────────────────────┐       ║
║  │ 📄 Sessione Attiva: INV-20250115-100000             │       ║
║  │ 📊 Articoli contati: 0                               │       ║
║  │ [✅ Finalizza] [❌ Annulla]                          │       ║
║  └─────────────────────────────────────────────────────┘       ║
└═════════════════════════════════════════════════════════════════┘
       │
       ▼
┌═════════════════════════════════════┐
║  FASE 2: SCANSIONE ARTICOLI         ║
║  (Ripeti per ogni articolo)         ║
└═════════════════════════════════════┘
       │
       ▼
┌─────────────────────────────────────┐
│  1. Scansiona/Digita Barcode        │
│     (auto-focus attivo)              │
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│  2. Premi ENTER                     │   ✅ Tastiera (no mouse!)
│     → Cerca automaticamente          │
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│  3. Prodotto Trovato ✅             │   ✅ Snackbar verde
│     → Dettagli visualizzati          │   ✅ Info complete
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│  4. Seleziona Ubicazione            │   ✅ Può essere predefinita
│     (dropdown o predefinita)         │
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│  5. Digita Quantità                 │
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│  6. Premi ENTER                     │   ✅ Tastiera (no mouse!)
│     → Aggiunge al documento          │
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│  ✅ Articolo aggiunto al documento  │   ✅ Nessuna modifica stock
│     → Auto-focus su barcode          │   ✅ Solo aggiunto a lista
│     → Campo pulito, pronto           │   ✅ Cursore pronto
└──────┬──────────────────────────────┘
       │
       ▼
┌═════════════════════════════════════════════════════════════════┐
║              TABELLA ARTICOLI AGGIORNATA                        ║
║  ┌─────────────────────────────────────────────────────┐       ║
║  │ Prodotto  │ Ubicazione │ Qtà │ Agg.   │ Ora      │       ║
║  ├───────────┼────────────┼─────┼────────┼──────────┤       ║
║  │ Prod A    │ A-01-01    │ 95  │ 🟢 +5  │ 10:30:15 │       ║
║  │ Prod B    │ A-01-02    │ 47  │ 🟡 -3  │ 10:31:22 │       ║
║  └─────────────────────────────────────────────────────┘       ║
║  📊 Totale: 2 articoli                                         ║
└═════════════════════════════════════════════════════════════════┘
       │
       │  (Ripeti per tutti gli articoli)
       │
       ▼
┌═════════════════════════════════════┐
║  FASE 3: REVISIONE                  ║
└═════════════════════════════════════┘
       │
       ▼
┌─────────────────────────────────────┐
│  Operatore Rivede Tabella           │   ✅ Tutti gli articoli visibili
│  → Verifica aggiustamenti            │   ✅ Colori per discrepanze
│  → Controlla quantità                │   ✅ Timestamp disponibili
└──────┬──────────────────────────────┘
       │
       ▼
      / \
     /   \
    /     \
   / OK?   \
  /_________\
   │       │
   │ NO    │ SÌ
   │       │
   ▼       ▼
   │    ┌─────────────────────────────────────┐
   │    │  Click "Finalizza"                   │
   │    └──────┬──────────────────────────────┘
   │           │
   │           ▼
   │    ┌─────────────────────────────────────┐
   │    │  Dialog Conferma:                    │
   │    │  "Applicare aggiustamenti per        │
   │    │   50 articoli?"                      │
   │    │  [Sì] [No]                           │
   │    └──────┬──────────────────────────────┘
   │           │
   │           ▼
   │    ┌═════════════════════════════════════════════════════════┐
   │    ║  ✅ TUTTI GLI AGGIUSTAMENTI APPLICATI IN BATCH         ║
   │    ║  ✅ Documento Chiuso                                    ║
   │    ║  ✅ Movimenti Stock Creati                             ║
   │    ║  ✅ Sessione Completata                                ║
   │    └═════════════════════════════════════════════════════════┘
   │
   ▼
┌─────────────────────────────────────┐
│  Click "Annulla"                    │
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│  Dialog Conferma:                   │
│  "Perdere tutti i dati (50 art.)?"  │
│  [Sì] [No]                          │
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────┐
│  ❌ Sessione Annullata              │   ✅ Nessuna modifica stock
│     → Nessun movimento creato        │   ✅ Sicuro
│     → Documento scartato             │
└─────────────────────────────────────┘

Totale per 50 articoli:
├─ 1 click avvio sessione
├─ 100 tasti Enter (2 per articolo)
├─ 1 click finalizza
├─ 1 click conferma
├─ ~12.5 minuti scansione + 5 min revisione
└─ ✅ Revisione completa + possibilità annullamento
```

---

## Confronto Visivo: Metriche

```
┌─────────────────────────────────────────────────────────────────┐
│                    CONFRONTO METRICHE                           │
└─────────────────────────────────────────────────────────────────┘

╔═══════════════════╦══════════════╦══════════════╦═══════════════╗
║   METRICA         ║    PRIMA     ║     DOPO     ║  MIGLIORAMENTO║
╠═══════════════════╬══════════════╬══════════════╬═══════════════╣
║ Click Mouse       ║  5/articolo  ║  2/articolo  ║    -60%       ║
║                   ║  (250 tot)   ║  (100 tot)   ║               ║
╠═══════════════════╬══════════════╬══════════════╬═══════════════╣
║ Uso Tastiera      ║  Limitato    ║  Completo    ║   +100%       ║
╠═══════════════════╬══════════════╬══════════════╬═══════════════╣
║ Tempo (50 art)    ║  25 minuti   ║  17.5 min    ║   -30%        ║
╠═══════════════════╬══════════════╬══════════════╬═══════════════╣
║ Revisione         ║     ❌       ║     ✅       ║   +100%       ║
╠═══════════════════╬══════════════╬══════════════╬═══════════════╣
║ Annullamento      ║     ❌       ║     ✅       ║   +100%       ║
╠═══════════════════╬══════════════╬══════════════╬═══════════════╣
║ Tracciabilità     ║  Parziale    ║  Completa    ║   +100%       ║
║                   ║  (50 mov.)   ║  (1 doc.)    ║               ║
╠═══════════════════╬══════════════╬══════════════╬═══════════════╣
║ Conferme          ║     ❌       ║     ✅       ║   +100%       ║
╠═══════════════════╬══════════════╬══════════════╬═══════════════╣
║ Feedback Visivo   ║  Minimo      ║  Completo    ║   +100%       ║
╚═══════════════════╩══════════════╩══════════════╩═══════════════╝
```

---

## Legenda Simboli

```
✅ = Funzionalità presente/ottimizzata
❌ = Funzionalità assente/problematica
⚠️  = Attenzione/Warning
🟢 = Aggiustamento positivo (stock aumentato)
🟡 = Aggiustamento negativo (stock diminuito)
⚪ = Nessun aggiustamento
📄 = Documento
📊 = Statistiche/Metriche
📍 = Ubicazione
🔢 = Quantità
🕐 = Timestamp
```

---

**Conclusione:** Il nuovo workflow è significativamente più efficiente, sicuro e user-friendly!
