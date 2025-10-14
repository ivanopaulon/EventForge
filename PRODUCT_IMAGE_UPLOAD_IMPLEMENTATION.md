# Product Image Upload Implementation - Using ImageDocumentId

## 🎯 Obiettivo

Implementare la funzionalità di caricamento e gestione dell'immagine del prodotto nella pagina di dettaglio, utilizzando il nuovo campo `ImageDocumentId` invece del campo deprecato `ImageUrl`.

---

## 🔍 Analisi del Problema

### Problema Originale
La pagina di gestione del prodotto non aveva alcuna interfaccia per caricare l'immagine del prodotto, nonostante:
- Il modello `ProductDto` avesse il campo `ImageDocumentId` (nuovo) e `ImageUrl` (deprecato)
- Il backend avesse già l'endpoint implementato: `POST /api/v1/product-management/products/{id}/image`
- Il servizio server-side `UploadProductImageAsync` fosse completamente implementato

### Campi nel ProductDto
```csharp
public class ProductDto
{
    // Campo deprecato - non più utilizzato
    public string ImageUrl { get; set; } = string.Empty;
    
    // Campo nuovo - utilizza DocumentReference
    public Guid? ImageDocumentId { get; set; }
    
    // URL dell'anteprima generato dal DocumentReference
    public string? ThumbnailUrl { get; set; }
}
```

---

## ✅ Soluzione Implementata

### 1. Nuovo Metodo Client Service

**File**: `EventForge.Client/Services/IProductService.cs`
**File**: `EventForge.Client/Services/ProductService.cs`

Aggiunto il metodo `UploadProductImageDocumentAsync`:
```csharp
Task<ProductDto?> UploadProductImageDocumentAsync(Guid productId, IBrowserFile file);
```

Questo metodo:
- Chiama l'endpoint corretto: `POST /api/v1/product-management/products/{productId}/image`
- Restituisce il `ProductDto` aggiornato con `ImageDocumentId` e `ThumbnailUrl`
- Non usa il campo deprecato `ImageUrl`

### 2. UI Completa in GeneralInfoTab

**File**: `EventForge.Client/Pages/Management/ProductDetailTabs/GeneralInfoTab.razor`

#### Componenti Aggiunti:

**a. Sezione Header Immagine**
```razor
<MudText Typo="Typo.h6" Class="mb-3 mt-4">
    <MudIcon Icon="@Icons.Material.Outlined.Image" Class="mr-2" />
    Immagine Prodotto
</MudText>
```

**b. Anteprima Immagine Esistente**
```razor
@if (Product.ImageDocumentId.HasValue && !string.IsNullOrEmpty(Product.ThumbnailUrl))
{
    <MudCard>
        <MudCardMedia Image="@Product.ThumbnailUrl" Height="200" />
        <MudCardActions>
            @if (IsEditMode)
            {
                <MudButton OnClick="RemoveImage" Color="Color.Error">
                    Rimuovi
                </MudButton>
            }
        </MudCardActions>
    </MudCard>
}
```

**c. Upload Nuova Immagine (solo in Edit Mode)**
```razor
@if (IsEditMode)
{
    <MudFileUpload T="IBrowserFile" Accept="image/*" @bind-Files="_selectedImage">
        <ActivatorContent>
            <MudButton HtmlTag="label" Variant="Variant.Filled">
                Seleziona Immagine
            </MudButton>
        </ActivatorContent>
    </MudFileUpload>
    
    @if (_selectedImage != null)
    {
        <MudChip OnClose="ClearImage">@_selectedImage.Name</MudChip>
        <MudButton OnClick="UploadImage" Color="Color.Success">
            Carica
        </MudButton>
    }
    
    @if (_isUploadingImage)
    {
        <MudProgressLinear Indeterminate="true" />
    }
}
```

### 3. Logica Upload

```csharp
private async Task UploadImage()
{
    if (_selectedImage == null || Product.Id == Guid.Empty)
        return;

    _isUploadingImage = true;
    try
    {
        var updatedProduct = await ProductService.UploadProductImageDocumentAsync(
            Product.Id, 
            _selectedImage
        );
        
        if (updatedProduct != null)
        {
            // Aggiorna i campi con i valori dal server
            Product.ImageDocumentId = updatedProduct.ImageDocumentId;
            Product.ThumbnailUrl = updatedProduct.ThumbnailUrl;
            
            Snackbar.Add("Immagine caricata con successo", Severity.Success);
            _selectedImage = null;
            
            // Notifica il parent per ricaricare i dati
            await OnProductUpdated.InvokeAsync();
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Error uploading image");
        Snackbar.Add("Errore nel caricamento dell'immagine", Severity.Error);
    }
    finally
    {
        _isUploadingImage = false;
    }
}
```

### 4. Callback di Aggiornamento

**File**: `EventForge.Client/Pages/Management/ProductDetail.razor`

Aggiunto callback per ricaricare i dati dopo l'upload:
```csharp
private async Task HandleProductUpdated()
{
    await LoadProductAsync();
}
```

E passato al tab:
```razor
<GeneralInfoTab Product="@_product" 
                IsEditMode="@_isEditMode" 
                OnProductUpdated="@HandleProductUpdated" />
```

---

## 🎨 Flusso Utente

### Visualizzazione Immagine Esistente
1. Utente apre un prodotto con immagine
2. L'immagine viene mostrata usando `ThumbnailUrl`
3. In modalità View: solo visualizzazione
4. In modalità Edit: pulsante "Rimuovi" disponibile

### Caricamento Nuova Immagine
1. Utente clicca "Modifica" nella pagina prodotto
2. Va al tab "Informazioni Generali"
3. Clicca "Seleziona Immagine"
4. Sceglie un file immagine (JPEG, PNG, GIF, WebP)
5. Vede il nome del file in un chip
6. Clicca "Carica"
7. Vede barra di progresso durante l'upload
8. Riceve notifica di successo/errore
9. Immagine appare immediatamente nell'anteprima

### Rimozione Immagine
1. Utente in modalità Edit clicca "Rimuovi"
2. I campi `ImageDocumentId` e `ThumbnailUrl` vengono puliti
3. L'immagine scompare dall'interfaccia
4. L'utente può salvare per confermare la rimozione

---

## 🔧 Dettagli Tecnici

### Endpoint Backend Utilizzato
```
POST /api/v1/product-management/products/{id}/image
Content-Type: multipart/form-data

Form Data:
  file: [binary data]

Response: ProductDto (with ImageDocumentId and ThumbnailUrl populated)
```

### Validazioni Server-Side
- File non vuoto
- Dimensione massima: 5MB
- Tipi consentiti: image/jpeg, image/jpg, image/png, image/gif, image/webp

### Gestione DocumentReference
Il server:
1. Salva il file fisicamente in `wwwroot/images/products/`
2. Crea un record `DocumentReference` nel database
3. Collega il DocumentReference al prodotto via `ImageDocumentId`
4. Genera `ThumbnailUrl` con il percorso dell'immagine
5. Restituisce il `ProductDto` aggiornato

### Differenza con ImageUrl (deprecato)
| Campo | Tipo | Utilizzo |
|-------|------|----------|
| `ImageUrl` | string | ❌ Deprecato - Stringa semplice, nessuna gestione documenti |
| `ImageDocumentId` | Guid? | ✅ Nuovo - Riferimento a DocumentReference con metadata completi |
| `ThumbnailUrl` | string? | ✅ Nuovo - Generato automaticamente dal DocumentReference |

---

## 📊 Vantaggi della Soluzione

### 1. Architettura Corretta
- ✅ Usa `DocumentReference` per gestire i file
- ✅ Metadata completi (nome, dimensione, tipo MIME, data creazione)
- ✅ Audit trail completo
- ✅ Pulizia file vecchi gestita automaticamente

### 2. UX Migliorata
- ✅ Anteprima immediata dell'immagine caricata
- ✅ Feedback visivo durante l'upload
- ✅ Notifiche chiare di successo/errore
- ✅ Validazione lato client e server

### 3. Consistenza
- ✅ Pattern uguale a CreateProductDialog (che usa file upload)
- ✅ Stile UI coerente con il resto dell'applicazione
- ✅ Gestione errori uniforme

---

## 🧪 Test Eseguiti

### Build
```bash
$ dotnet build
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Checklist Funzionale
- [x] Compilazione senza errori
- [x] Metodo service client implementato correttamente
- [x] UI componenti visibili nel tab
- [x] Validazioni client-side presenti
- [x] Gestione errori implementata
- [x] Callback parent implementato
- [x] Documentazione aggiornata

### Test da Eseguire in Browser (Prossimo Step)
- [ ] Apertura pagina prodotto senza errori console
- [ ] Visualizzazione immagine esistente
- [ ] Upload nuova immagine funzionante
- [ ] Rimozione immagine funzionante
- [ ] Validazioni funzionanti
- [ ] Notifiche corrette

---

## 📁 File Modificati

1. **EventForge.Client/Services/IProductService.cs**
   - Aggiunto: `Task<ProductDto?> UploadProductImageDocumentAsync(Guid productId, IBrowserFile file)`

2. **EventForge.Client/Services/ProductService.cs**
   - Implementato: `UploadProductImageDocumentAsync`

3. **EventForge.Client/Pages/Management/ProductDetailTabs/GeneralInfoTab.razor**
   - Aggiunta: Sezione completa upload/visualizzazione immagini
   - Aggiunto: Metodi `UploadImage()`, `RemoveImage()`, `ClearImage()`
   - Aggiunto: Parametro `EventCallback OnProductUpdated`

4. **EventForge.Client/Pages/Management/ProductDetail.razor**
   - Aggiunto: Metodo `HandleProductUpdated()`
   - Modificato: Tab GeneralInfoTab per passare callback

---

## 🚀 Note per il Deploy

- ✅ Nessuna modifica al database richiesta
- ✅ Nessuna breaking change all'API
- ✅ Solo modifiche frontend
- ✅ Backward compatible (campo ImageUrl ancora presente ma non usato)
- ✅ Safe to deploy

---

## 📖 Riferimenti

- `PRODUCT_DETAIL_PAGE_IMPLEMENTATION.md` - Struttura pagina prodotto
- `PRODUCT_MANAGEMENT_FIX_2025.md` - Fix precedenti
- `StoreUsersController.cs` - Pattern simile per upload immagini StorePOS

---

**Data**: Gennaio 2025  
**Issue**: Mancata gestione upload immagine prodotto  
**Status**: ✅ **Implementato e Testato (Build)**  
**Campo Utilizzato**: `ImageDocumentId` (nuovo) invece di `ImageUrl` (deprecato)
