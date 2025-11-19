# Risoluzione Errori Pagina Gestione Aliquote IVA

## Problema Rilevato
Quando si tentava di accedere alla pagina di gestione delle aliquote IVA, apparivano questi errori nel browser:

1. **LoadingDialog**: `Object of type 'EventForge.Client.Shared.Components.Dialogs.LoadingDialog' does not have a property matching the name 'Visible'`
2. **PageLoadingOverlay**: `Object of type 'EventForge.Client.Shared.Components.PageLoadingOverlay' does not have a property matching the name 'Visible'`
3. **EFTable**: `Object of type 'EventForge.Client.Shared.Components.EFTable`1[...]' does not have a property matching the name 'T'`

## Causa
I componenti erano stati refactorizzati per utilizzare il parametro `IsVisible` invece di `Visible`, ma molte pagine nel codebase utilizzavano ancora il vecchio nome del parametro `Visible`. Inoltre, il componente `EFTable` nella pagina VatRateManagement utilizzava il parametro errato `T` invece di `TItem`.

## Soluzione Implementata

### 1. PageLoadingOverlay.razor
Aggiunto un parametro alias `Visible` che mappa a `IsVisible` per mantenere la retrocompatibilità:

```csharp
[Parameter] 
public bool Visible 
{ 
    get => IsVisible; 
    set => IsVisible = value; 
}
```

### 2. LoadingDialog.razor
Aggiunto lo stesso parametro alias `Visible` per la retrocompatibilità:

```csharp
[Parameter] 
public bool Visible 
{ 
    get => IsVisible; 
    set => IsVisible = value; 
}
```

### 3. VatRateManagement.razor
Corretto l'utilizzo del componente EFTable da `T="VatRateDto"` a `TItem="VatRateDto"`:

```razor
<EFTable @ref="_efTable"
         TItem="VatRateDto"
         Items="_filteredVatRates"
         ...
```

## Risultati
- ✅ La pagina di gestione aliquote IVA ora si carica correttamente
- ✅ Nessun errore in console
- ✅ Tutte le altre pagine che utilizzavano `Visible` continuano a funzionare
- ✅ Build del progetto completato senza errori
- ✅ Retrocompatibilità mantenuta

## File Modificati
1. `EventForge.Client/Shared/Components/PageLoadingOverlay.razor`
2. `EventForge.Client/Shared/Components/Dialogs/LoadingDialog.razor`
3. `EventForge.Client/Pages/Management/Financial/VatRateManagement.razor`

## Note per il Futuro
- Per nuovi sviluppi, utilizzare preferibilmente `IsVisible` invece di `Visible`
- I parametri `Visible` sono mantenuti per retrocompatibilità ma sono considerati legacy
- Il tipo generico corretto per `EFTable` è `TItem`, non `T`
