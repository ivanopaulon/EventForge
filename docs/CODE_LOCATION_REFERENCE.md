# ProductNotFoundDialog - Guida Rapida Posizioni nel Codice

## 🎯 Riferimento Rapido per Verificare le Modifiche

Questa guida mostra esattamente dove trovare ogni modifica nel codice sorgente.

---

## 📁 File 1: ProductNotFoundDialog.razor

**Path:** `Prym.Client/Shared/Components/ProductNotFoundDialog.razor`

### Modifica 1: Parametro IsInventoryContext
```
📍 LINEE 72-73

[Parameter]
public bool IsInventoryContext { get; set; } = false;
```

### Modifica 2: Testo Prompt Condizionale
```
📍 LINEE 10-19

<MudText Typo="Typo.body1" Class="mb-4">
    @if (IsInventoryContext)
    {
        @TranslationService.GetTranslation("warehouse.inventoryProductNotFoundPrompt", 
            "Il prodotto non esiste. Salta questo codice o assegnalo a un prodotto esistente?")
    }
    else
    {
        @TranslationService.GetTranslation("warehouse.createOrAssignPrompt", "Cosa vuoi fare?")
    }
</MudText>
```

### Modifica 3: Pulsanti Condizionali
```
📍 LINEE 21-56

<MudStack Spacing="3">
    @if (IsInventoryContext)
    {
        <!-- NUOVO: Pulsante "Salta e Continua" -->
        <MudButton StartIcon="@Icons.Material.Outlined.SkipNext"
                  Color="Color.Info"
                  Variant="Variant.Filled"
                  FullWidth="true"
                  OnClick="@(() => SelectAction("skip"))">
            @TranslationService.GetTranslation("warehouse.skipProduct", "Salta e Continua")
        </MudButton>
        
        <!-- Pulsante "Assegna a Prodotto Esistente" -->
        <MudButton StartIcon="@Icons.Material.Outlined.Link"
                  Color="Color.Primary"
                  Variant="Variant.Filled"
                  FullWidth="true"
                  OnClick="@(() => SelectAction("assign"))">
            @TranslationService.GetTranslation("warehouse.assignToExisting", "Assegna a Prodotto Esistente")
        </MudButton>
    }
    else
    {
        <!-- Comportamento originale mantenuto -->
        <MudButton ... OnClick="@(() => SelectAction("create"))">...</MudButton>
        <MudButton ... OnClick="@(() => SelectAction("assign"))">...</MudButton>
    }
</MudStack>
```

---

## 📁 File 2: InventoryProcedure.razor

**Path:** `Prym.Client/Pages/Management/InventoryProcedure.razor`

### Modifica 1: Passaggio Parametro al Dialog
```
📍 LINEE 967-987

private async Task ShowProductNotFoundDialog()
{
    var parameters = new DialogParameters
    {
        { "Barcode", _scannedBarcode },
        { "IsInventoryContext", true }  // ✅ QUESTA È LA MODIFICA CHIAVE
    };

    var options = new DialogOptions
    {
        CloseOnEscapeKey = true,
        MaxWidth = MaxWidth.Medium,
        FullWidth = true
    };

    var dialog = await DialogService.ShowAsync<ProductNotFoundDialog>(
        TranslationService.GetTranslation("warehouse.productNotFound", "Prodotto non trovato"),
        parameters,
        options
    );
    // ...
}
```

### Modifica 2: Gestione Azione "skip"
```
📍 LINEE 991-1018

var result = await dialog.Result;

if (!result.Canceled && result.Data is string action)
{
    if (action == "create")
    {
        CreateNewProduct();
    }
    else if (action == "assign")
    {
        await AssignToExistingProduct();
    }
    else if (action == "skip")  // ✅ NUOVO HANDLER
    {
        // Skip this product and continue with inventory
        Snackbar.Add(
            TranslationService.GetTranslation("warehouse.productSkipped", "Prodotto saltato: {0}", _scannedBarcode), 
            Severity.Info
        );
        AddOperationLog(
            TranslationService.GetTranslation("warehouse.productSkipped", "Prodotto saltato"),
            $"Codice: {_scannedBarcode}",
            "Info"
        );
        
        // Clear the form and refocus on barcode input
        ClearProductForm();
    }
}
```

---

## 📁 File 3: it.json (Traduzioni Italiane)

**Path:** `Prym.Client/wwwroot/i18n/it.json`

### Chiavi Aggiunte nella Sezione "warehouse"
```
📍 CERCARE NELLA SEZIONE "warehouse"

{
  "warehouse": {
    ...
    "inventoryProductNotFoundPrompt": "Il prodotto non esiste. Salta questo codice o assegnalo a un prodotto esistente?",
    ...
    "productSkipped": "Prodotto saltato",
    ...
    "skipProduct": "Salta e Continua",
    ...
  }
}
```

**Per trovare velocemente:**
```bash
grep -n "inventoryProductNotFoundPrompt" Prym.Client/wwwroot/i18n/it.json
grep -n "productSkipped" Prym.Client/wwwroot/i18n/it.json
grep -n "skipProduct" Prym.Client/wwwroot/i18n/it.json
```

---

## 📁 File 4: en.json (Traduzioni Inglesi)

**Path:** `Prym.Client/wwwroot/i18n/en.json`

### Chiavi Aggiunte nella Sezione "warehouse"
```
📍 CERCARE NELLA SEZIONE "warehouse"

{
  "warehouse": {
    ...
    "inventoryProductNotFoundPrompt": "The product does not exist. Skip this code or assign it to an existing product?",
    ...
    "productSkipped": "Product skipped",
    ...
    "skipProduct": "Skip and Continue",
    ...
  }
}
```

**Per trovare velocemente:**
```bash
grep -n "inventoryProductNotFoundPrompt" Prym.Client/wwwroot/i18n/en.json
grep -n "productSkipped" Prym.Client/wwwroot/i18n/en.json
grep -n "skipProduct" Prym.Client/wwwroot/i18n/en.json
```

---

## 🔍 Comandi di Verifica Rapida

### Verificare ProductNotFoundDialog.razor
```bash
# Verificare presenza parametro IsInventoryContext
grep -n "IsInventoryContext" Prym.Client/Shared/Components/ProductNotFoundDialog.razor

# Output atteso:
# 11:        @if (IsInventoryContext)
# 22:        @if (IsInventoryContext)
# 73:    public bool IsInventoryContext { get; set; } = false;
```

### Verificare InventoryProcedure.razor
```bash
# Verificare passaggio del parametro
grep -A2 "IsInventoryContext" Prym.Client/Pages/Management/InventoryProcedure.razor

# Output atteso:
# 972:            { "IsInventoryContext", true }

# Verificare handler skip
grep -A5 "action == \"skip\"" Prym.Client/Pages/Management/InventoryProcedure.razor
```

### Verificare Traduzioni
```bash
# Italiano
grep "skipProduct\|productSkipped\|inventoryProductNotFoundPrompt" Prym.Client/wwwroot/i18n/it.json

# Inglese
grep "skipProduct\|productSkipped\|inventoryProductNotFoundPrompt" Prym.Client/wwwroot/i18n/en.json
```

---

## 📊 Riepilogo Modifiche per File

| File | Linee Modificate | Tipo Modifica |
|------|------------------|---------------|
| **ProductNotFoundDialog.razor** | 10-19, 21-56, 72-73 | Rendering condizionale + parametro |
| **InventoryProcedure.razor** | 967-1018 | Passaggio parametro + handler skip |
| **it.json** | Sezione warehouse | +3 chiavi traduzione |
| **en.json** | Sezione warehouse | +3 chiavi traduzione |

---

## ✅ Checklist di Verifica

Usa questa checklist per verificare manualmente ogni modifica:

- [ ] **ProductNotFoundDialog.razor** - Parametro `IsInventoryContext` presente (linea 73)
- [ ] **ProductNotFoundDialog.razor** - Prompt condizionale presente (linee 10-19)
- [ ] **ProductNotFoundDialog.razor** - Pulsante "Salta e Continua" presente (linee 24-30)
- [ ] **ProductNotFoundDialog.razor** - Rendering condizionale MudStack (linee 21-56)
- [ ] **InventoryProcedure.razor** - `IsInventoryContext = true` passato al dialog (linea 972)
- [ ] **InventoryProcedure.razor** - Handler `action == "skip"` presente (linee 1001-1016)
- [ ] **it.json** - Chiave `inventoryProductNotFoundPrompt` presente
- [ ] **it.json** - Chiave `productSkipped` presente
- [ ] **it.json** - Chiave `skipProduct` presente
- [ ] **en.json** - Chiave `inventoryProductNotFoundPrompt` presente
- [ ] **en.json** - Chiave `productSkipped` presente
- [ ] **en.json** - Chiave `skipProduct` presente

---

## 🎯 Conclusione

**TUTTE LE MODIFICHE SONO PRESENTI E VERIFICABILI NEL CODICE.**

Se segui questa guida, potrai trovare esattamente ogni singola modifica documentata nei file di riepilogo.

Le modifiche sono state implementate nella **PR #429**, non nella PR #418.

---

**Creato:** 3 Ottobre 2025  
**Per:** Verifica PR #418  
**Stato:** ✅ Tutte le modifiche presenti
