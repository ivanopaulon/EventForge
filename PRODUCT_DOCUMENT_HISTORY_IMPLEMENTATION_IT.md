# Implementazione Storico Documenti Prodotto

## 📋 Sommario

Questa implementazione aggiunge la funzionalità di visualizzare tutti i documenti in cui un prodotto è presente, direttamente dalla scheda "Magazzino e Inventario" nella pagina dettaglio prodotto.

## 🎯 Obiettivo

Permettere agli utenti di visualizzare lo storico completo dei documenti (ordini, fatture, DDT, ecc.) che contengono un prodotto specifico, con la possibilità di filtrare per:
- Cliente/Fornitore
- Intervallo di date
- Tipo di documento (futuro)

## 🔧 Modifiche Implementate

### Backend

#### 1. DocumentHeaderQueryParameters.cs
**File**: `EventForge.DTOs/Documents/DocumentHeaderQueryParameters.cs`

Aggiunto nuovo parametro di filtro:
```csharp
/// <summary>
/// Filter by product ID (documents containing this product).
/// </summary>
public Guid? ProductId { get; set; }
```

#### 2. DocumentHeaderService.cs
**File**: `EventForge.Server/Services/Documents/DocumentHeaderService.cs`

Aggiunto filtro nella query per ProductId:
```csharp
if (parameters.ProductId.HasValue)
    query = query.Where(dh => dh.Rows.Any(r => !r.IsDeleted && r.ProductId == parameters.ProductId.Value));
```

Questo permette di recuperare tutti i documenti che hanno almeno una riga con il prodotto specificato.

### Frontend

#### 1. Servizi Client

**File**: `EventForge.Client/Services/IDocumentHeaderService.cs`
- Interfaccia del servizio per operazioni sui document headers

**File**: `EventForge.Client/Services/DocumentHeaderService.cs`
- Implementazione del servizio che comunica con l'API backend
- Costruisce query string con tutti i parametri di filtro
- Gestisce la chiamata HTTP GET all'endpoint `/api/v1/DocumentHeaders`

**File**: `EventForge.Client/Program.cs`
- Registrazione del servizio `IDocumentHeaderService`

#### 2. Componente UI Migliorato

**File**: `EventForge.Client/Pages/Management/ProductDetailTabs/StockInventoryTab.razor`

Modifiche principali:
- Mantenuti i campi originali per la gestione inventario (ReorderPoint, SafetyStock, ecc.)
- Aggiunta nuova sezione "Storico Documenti" con:
  - **Filtri**:
    - Data inizio (Da Data)
    - Data fine (A Data)
    - Nome cliente/fornitore (ricerca testuale)
  - **Tabella documenti** con colonne:
    - Numero documento
    - Data
    - Tipo documento
    - Cliente/Fornitore
    - Stato (con chip colorato)
    - Totale (importo)
  - **Paginazione**: 10 documenti per pagina

## 📊 Caratteristiche UI

### Filtri
- **Layout responsive**: 3 colonne su desktop, stack su mobile
- **Date picker** con formato italiano (dd/MM/yyyy)
- **Pulsante Filtra** per applicare i filtri

### Tabella Documenti
- **Design pulito**: Tabella striped e hover
- **Status chips**: Colori diversi per ogni stato
  - Bozza → Grigio
  - Approvato → Verde
  - Rifiutato → Rosso
  - Annullato → Rosso
- **Formato valute**: Totale in formato currency (€)
- **Date formattate**: dd/MM/yyyy

### Stati di Caricamento
- **Spinner** durante il caricamento
- **Messaggio informativo** quando non ci sono documenti
- **Gestione errori** con logging

## 🔄 Flusso Operativo

1. L'utente apre la pagina dettaglio di un prodotto
2. Naviga alla tab "Magazzino e Inventario"
3. La sezione "Storico Documenti" carica automaticamente i primi 10 documenti
4. L'utente può:
   - Filtrare per intervallo di date
   - Cercare per nome cliente/fornitore
   - Navigare tra le pagine dei risultati
5. I documenti vengono visualizzati con tutte le informazioni rilevanti

## ✅ Test e Validazione

### Build
- ✅ Backend compila senza errori
- ✅ Frontend compila senza errori
- ✅ Soluzione completa compila correttamente

### Test Automatici
- ✅ **214 test superati** su 214
- ✅ Nessun test fallito
- ✅ Nessuna regressione introdotta

### Compatibilità
- ✅ Endpoint API esistenti non modificati
- ✅ Backward compatible
- ✅ Nessuna migrazione database richiesta

## 🚀 Utilizzo

### Per Utente Finale

1. Aprire la pagina di dettaglio di un prodotto esistente
2. Cliccare sulla tab "Magazzino e Inventario"
3. Scorrere fino alla sezione "Storico Documenti"
4. Utilizzare i filtri se necessario
5. Esplorare i documenti che contengono il prodotto

### Esempio di Query API

Endpoint: `GET /api/v1/DocumentHeaders`

Query Parameters:
```
page=1
pageSize=10
productId=550e8400-e29b-41d4-a716-446655440000
fromDate=2024-01-01
toDate=2024-12-31
customerName=Rossi
```

## 📝 Considerazioni Future

### Possibili Miglioramenti

1. **Filtro per tipo documento**: Aggiungere dropdown per filtrare per tipo (fattura, DDT, ordine, ecc.)
2. **Click su riga**: Navigare al dettaglio del documento
3. **Export**: Esportare l'elenco in Excel/CSV
4. **Statistiche**: Mostrare statistiche aggregate (totale vendite, quantità totale, ecc.)
5. **Ordinamento**: Permettere ordinamento per colonna
6. **Visualizzazione quantità**: Mostrare la quantità del prodotto in ogni documento

### Estensibilità

Il design è stato creato per essere facilmente estendibile:
- Nuovi filtri possono essere aggiunti facilmente
- Il servizio `DocumentHeaderService` può essere riutilizzato in altre pagine
- La query backend supporta già molti altri filtri disponibili

## 🔍 Dettagli Tecnici

### Performance
- **Paginazione server-side**: Solo 10 record caricati per volta
- **Query ottimizzata**: Utilizza indici esistenti su ProductId nelle DocumentRows
- **Lazy loading**: I documenti vengono caricati solo quando si apre il tab

### Sicurezza
- ✅ Autenticazione richiesta per l'endpoint
- ✅ Filtro automatico per tenant corrente
- ✅ Validazione parametri query
- ✅ Protezione contro SQL injection (EF Core)

## 📚 File Modificati/Creati

### Nuovi File
1. `EventForge.Client/Services/IDocumentHeaderService.cs`
2. `EventForge.Client/Services/DocumentHeaderService.cs`

### File Modificati
1. `EventForge.DTOs/Documents/DocumentHeaderQueryParameters.cs`
2. `EventForge.Server/Services/Documents/DocumentHeaderService.cs`
3. `EventForge.Client/Program.cs`
4. `EventForge.Client/Pages/Management/ProductDetailTabs/StockInventoryTab.razor`

## ✨ Conclusione

L'implementazione fornisce una soluzione completa e user-friendly per visualizzare lo storico dei documenti di un prodotto, mantenendo la compatibilità con il sistema esistente e seguendo i pattern architetturali del progetto.

La funzionalità è pronta per essere testata manualmente e rilasciata in produzione.
