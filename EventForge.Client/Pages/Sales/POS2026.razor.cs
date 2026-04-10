using EventForge.Client.Components.Pos26;
using EventForge.Client.Models.Sales;
using EventForge.Client.Services.Sales;
using EventForge.Client.Shared.Components.Dialogs;
using EventForge.Client.Shared.Components.Dialogs.Sales;
using EventForge.Client.Shared.Components.FiscalPrinting;
using EventForge.Client.ViewModels;
using EventForge.DTOs.Business;
using EventForge.DTOs.FiscalPrinting;
using EventForge.DTOs.Products;
using EventForge.DTOs.Sales;
using EventForge.DTOs.Store;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;

namespace EventForge.Client.Pages.Sales;

/// <summary>
/// POS 2026 — versione evolutiva del Punto Vendita.
/// Replica tutte le funzionalità di POS.razor e POSTouch.razor con nuovo layout a due colonne
/// e griglia prodotti interattiva.
/// </summary>
public partial class POS2026 : IAsyncDisposable
{
    // --- Componente reference per focus ---
    private Pos26SearchBar? _searchBar;

    // --- Stato prodotti ---
    private List<ProductDto> _allProducts = new();
    private List<string> _categories = new();
    private bool _isLoadingProducts = false;

    // --- Filtri e ordinamento ---
    private string _searchTerm = string.Empty;
    private Pos26SortMode _sortMode = Pos26SortMode.BestSeller;
    private string? _selectedCategory;

    // --- Best seller / ultimi acquisti (TODO: implementare API dedicate se disponibili) ---
    private HashSet<Guid> _bestSellerIds = new();
    private HashSet<Guid> _lastPurchaseIds = new();

    // --- Carrello (derivato dalla sessione) ---
    private Dictionary<Guid, int> _cartQuantities = new();

    // --- Prodotti filtrati (derivati) ---
    private IEnumerable<ProductDto> _filteredProducts => ApplyFilters();

    // --- Fiscal printing (stesso pattern di POS.razor) ---
    private Guid? _fiscalPrinterId;
    private FiscalPrinterStatus? _fiscalPrinterStatus;
    private FiscalDrawerSummaryDto? _fiscalDrawerSummary;
    private HubConnection? _fiscalHubConnection;

    // --- Keyboard shortcuts JS (stesso pattern di POS.razor) ---
    private IJSObjectReference? _shortcutsModule;
    private DotNetObjectReference<POS2026>? _dotNetRef;

    // =========================================================================
    //  Inizializzazione
    // =========================================================================

    protected override async Task OnInitializedAsync()
    {
        try
        {
            ViewModel.StateChanged += OnViewModelStateChanged;
            ViewModel.OnNotification += HandleNotification;

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var username = authState.User.Identity?.Name;
            await ViewModel.InitializeAsync(username);

            await LoadProductsAsync();
            await LoadCategoriesFromProductsAsync();

            // Se c'è già un cliente, usa modalità "Ultimi acquisti"
            if (ViewModel.SelectedCustomer != null)
                _sortMode = Pos26SortMode.UltimiAcquisti;

            RebuildCartQuantities();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore durante l'inizializzazione di POS2026.");
            AppNotification.ShowWarning("Si è verificato un errore durante l'avvio.");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (firstRender)
            {
                _shortcutsModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                    "import", "./js/pos-shortcuts.js");
                _dotNetRef = DotNetObjectReference.Create(this);
                await _shortcutsModule.InvokeVoidAsync("setupPOSKeyboardShortcuts", _dotNetRef);

                await ConnectFiscalHubAsync();
                await LoadFiscalDrawerAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore in OnAfterRenderAsync di POS2026.");
        }
    }

    // =========================================================================
    //  Caricamento prodotti e categorie
    // =========================================================================

    private async Task LoadProductsAsync()
    {
        _isLoadingProducts = true;
        StateHasChanged();
        try
        {
            // TODO: se disponibile un metodo GetBestSeller() usarlo per popolare _bestSellerIds
            // TODO: se disponibile GetUltimiAcquisti(clienteId) usarlo per popolare _lastPurchaseIds
            var result = await ProductService.GetProductsAsync(page: 1, pageSize: 500);
            _allProducts = result?.Items?.ToList() ?? new();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore caricamento prodotti in POS2026.");
        }
        finally
        {
            _isLoadingProducts = false;
        }
    }

    private Task LoadCategoriesFromProductsAsync()
    {
        // Estrae le categorie dai prodotti caricati (usando il nome del nodo classificazione)
        // TODO: caricare da IEntityManagementService.GetClassificationNodesAsync() per lista completa
        _categories = _allProducts
            .Where(p => !string.IsNullOrEmpty(p.VatRateName))
            .Select(p => p.VatRateName!)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
        return Task.CompletedTask;
    }

    // =========================================================================
    //  Filtri e ordinamento
    // =========================================================================

    private IEnumerable<ProductDto> ApplyFilters()
    {
        var source = _allProducts.AsEnumerable();

        // Filtro testuale
        if (!string.IsNullOrWhiteSpace(_searchTerm))
        {
            var term = _searchTerm.Trim().ToLowerInvariant();
            source = source.Where(p =>
                p.Name.ToLowerInvariant().Contains(term) ||
                (p.Code?.ToLowerInvariant().Contains(term) == true));
        }

        // Filtro categoria
        if (!string.IsNullOrEmpty(_selectedCategory))
            source = source.Where(p => p.VatRateName == _selectedCategory);

        // Ordinamento
        source = _sortMode switch
        {
            Pos26SortMode.Alfabetico     => source.OrderBy(p => p.Name),
            Pos26SortMode.Prezzo         => source.OrderBy(p => p.DefaultPrice ?? 0),
            Pos26SortMode.BestSeller     => source.OrderByDescending(p => _bestSellerIds.Contains(p.Id)).ThenBy(p => p.Name),
            Pos26SortMode.UltimiAcquisti when ViewModel.SelectedCustomer != null
                                         => source.OrderByDescending(p => _lastPurchaseIds.Contains(p.Id)).ThenBy(p => p.Name),
            Pos26SortMode.UltimiAcquisti => source.OrderByDescending(p => _bestSellerIds.Contains(p.Id)).ThenBy(p => p.Name),
            Pos26SortMode.Categoria      => source.OrderBy(p => p.VatRateName).ThenBy(p => p.Name),
            _                            => source.OrderBy(p => p.Name)
        };

        return source;
    }

    // =========================================================================
    //  Gestione eventi UI
    // =========================================================================

    private async Task OnSearchTermChanged(string term)
    {
        _searchTerm = term;

        if (!string.IsNullOrEmpty(term))
        {
            // Cerca anche lato server per risultati più completi
            try
            {
                var result = await ProductService.SearchProductsAsync(term, maxResults: 100);
                if (result?.SearchResults != null)
                    _allProducts = result.SearchResults.ToList();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Errore nella ricerca prodotti POS2026.");
            }
        }
        else
        {
            await LoadProductsAsync();
        }

        await LoadCategoriesFromProductsAsync();
        StateHasChanged();
    }

    private async Task HandleBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return;
        try
        {
            var result = await ProductService.SearchProductsAsync(barcode, maxResults: 1);
            var product = result?.ExactMatch?.Product ?? result?.SearchResults?.FirstOrDefault();
            if (product != null)
            {
                await OnAddProductToCartAsync(product);
                _searchTerm = string.Empty;
                StateHasChanged();
            }
            else
            {
                AppNotification.ShowWarning($"Prodotto non trovato per il codice: {barcode}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore nella ricerca barcode POS2026: {Barcode}", barcode);
        }
    }

    private async Task OnSortModeChangedAsync(Pos26SortMode mode)
    {
        _sortMode = mode;

        // Se si seleziona "Ultimi acquisti" ma non c'è cliente, fallback a BestSeller
        if (mode == Pos26SortMode.UltimiAcquisti && ViewModel.SelectedCustomer == null)
            AppNotification.ShowInfo("Seleziona un cliente per vedere gli ultimi acquisti.");

        // TODO: caricare _lastPurchaseIds da API se disponibile
        await Task.CompletedTask;
        StateHasChanged();
    }

    private async Task OnCategorySelectedAsync(string? category)
    {
        _selectedCategory = category;
        await Task.CompletedTask;
        StateHasChanged();
    }

    private async Task OnSelectedCustomerChangedAsync(BusinessPartyDto? customer)
    {
        try
        {
            await ViewModel.UpdateSelectedCustomerAsync(customer);

            // Se c'è un cliente, suggerisci modalità "Ultimi acquisti"
            if (customer != null && _sortMode == Pos26SortMode.BestSeller)
            {
                _sortMode = Pos26SortMode.UltimiAcquisti;
                // TODO: caricare _lastPurchaseIds da GetUltimiAcquisti(customer.Id) se disponibile
            }
            else if (customer == null && _sortMode == Pos26SortMode.UltimiAcquisti)
            {
                _sortMode = Pos26SortMode.BestSeller;
                _lastPurchaseIds.Clear();
            }

            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore nel cambio cliente POS2026.");
            AppNotification.ShowWarning("Si è verificato un errore.");
        }
    }

    // =========================================================================
    //  Gestione carrello
    // =========================================================================

    private async Task OnAddProductToCartAsync(ProductDto product)
    {
        try
        {
            var result = await ViewModel.AddProductAsync(product);
            if (result.Success)
            {
                RebuildCartQuantities();
                AppNotification.ShowSuccess($"✅ {product.Name} aggiunto");
            }
            else
            {
                AppNotification.ShowError(result.Error ?? "Errore durante l'aggiunta.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore durante l'aggiunta al carrello POS2026.");
            AppNotification.ShowWarning("Si è verificato un errore.");
        }
    }

    private void RebuildCartQuantities()
    {
        _cartQuantities = ViewModel.CurrentSession?.Items?
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => (int)g.Sum(i => i.Quantity))
            ?? new();
    }

    private void IncreaseQuantityAsync(SaleItemDto item)
    {
        if (ViewModel.IsUpdatingItems) return;
        item.Quantity++;
        ViewModel.QueueItemUpdate(item);
        RebuildCartQuantities();
    }

    private void DecreaseQuantityAsync(SaleItemDto item)
    {
        if (ViewModel.IsUpdatingItems) return;
        if (item.Quantity > 1)
        {
            item.Quantity--;
            ViewModel.QueueItemUpdate(item);
            RebuildCartQuantities();
        }
    }

    private async Task ClearCartAsync()
    {
        if (ViewModel.CurrentSession?.Items?.Any() != true) return;
        try
        {
            var confirmed = await ShowConfirmAsync("Svuota carrello", "Rimuovere tutti gli articoli dal carrello?");
            if (!confirmed) return;

            foreach (var item in ViewModel.CurrentSession!.Items.ToList())
                await SalesService.RemoveItemAsync(ViewModel.CurrentSession.Id, item.Id);

            await ViewModel.ReloadSessionAsync();
            RebuildCartQuantities();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore svuotamento carrello POS2026.");
            AppNotification.ShowWarning("Si è verificato un errore.");
        }
    }

    private async Task EditItemNotesAsync(SaleItemDto item)
    {
        try
        {
            var parameters = new DialogParameters
            {
                ["Item"]         = item,
                ["InitialNotes"] = item.Notes ?? string.Empty
            };
            var dialog = await DialogService.ShowAsync<ItemNotesDialog>(
                "Note articolo", parameters,
                new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true });
            var result = await dialog.Result;
            if (result?.Canceled == false)
                await ViewModel.ReloadSessionAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore modifica note articolo POS2026.");
            AppNotification.ShowWarning("Si è verificato un errore.");
        }
    }

    // =========================================================================
    //  Sconto globale dal numpad
    // =========================================================================

    private async Task ApplyDiscountAsync(decimal percentuale)
    {
        if (!ViewModel.HasActiveSession) return;
        try
        {
            var dto = new ApplyGlobalDiscountDto { DiscountPercent = percentuale };
            var session = await SalesService.ApplyGlobalDiscountAsync(ViewModel.CurrentSession!.Id, dto);
            if (session != null)
            {
                await ViewModel.ReloadSessionAsync();
                AppNotification.ShowSuccess($"Sconto {percentuale:F1}% applicato.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore applicazione sconto POS2026.");
            AppNotification.ShowWarning("Si è verificato un errore.");
        }
    }

    // =========================================================================
    //  Dialog pagamento
    // =========================================================================

    private async Task OpenPaymentDialogAsync()
    {
        if (!ViewModel.CanPay) return;
        try
        {
            var paymentMethods = await PaymentMethodService.GetActiveAsync() ?? new();

            var parameters = new DialogParameters
            {
                [nameof(Pos26PaymentDialog.TotaleOrdine)]    = ViewModel.GrandTotal,
                [nameof(Pos26PaymentDialog.ScontoApplicato)] = ViewModel.CurrentSession?.DiscountAmount ?? 0m,
                [nameof(Pos26PaymentDialog.Ordine)]          = ViewModel.CurrentSession,
                [nameof(Pos26PaymentDialog.Cliente)]         = ViewModel.SelectedCustomer,
                [nameof(Pos26PaymentDialog.PaymentMethods)]  = paymentMethods
            };

            var dialog = await DialogService.ShowAsync<Pos26PaymentDialog>(
                string.Empty, parameters, EFDialogDefaults.Options);

            var result = await dialog.Result;
            if (result is { Canceled: false } && result.Data is RisultatoPagamento risultato)
                await ProcessPaymentResultAsync(risultato);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore apertura dialog pagamento POS2026.");
            AppNotification.ShowWarning("Si è verificato un errore.");
        }
    }

    private async Task ProcessPaymentResultAsync(RisultatoPagamento risultato)
    {
        try
        {
            // Aggiunge ogni riga di pagamento alla sessione
            foreach (var riga in risultato.Righe)
            {
                var paymentDto = new AddSalePaymentDto
                {
                    PaymentMethodId = riga.Metodo.Id,
                    Amount = riga.Importo
                };
                await SalesService.AddPaymentAsync(ViewModel.CurrentSession!.Id, paymentDto);
            }

            await ViewModel.ReloadSessionAsync();

            // Chiudi la sessione se completamente pagata
            if (ViewModel.CurrentSession?.IsFullyPaid == true)
                await CloseSaleAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore durante la registrazione del pagamento POS2026.");
            AppNotification.ShowError("Errore durante il pagamento.");
        }
    }

    // =========================================================================
    //  Azioni sessione
    // =========================================================================

    private async Task CloseSaleAsync()
    {
        try
        {
            await ViewModel.CloseSaleAsync();
            if (_fiscalPrinterId.HasValue)
                await TriggerFiscalPrintAsync(ViewModel.CurrentSession!);
            AppNotification.ShowSuccess("Vendita completata.");
            RebuildCartQuantities();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore chiusura vendita POS2026.");
            AppNotification.ShowError("Errore durante la chiusura della vendita.");
        }
    }

    private async Task ParkSessionAsync()
    {
        try
        {
            await ViewModel.ParkSessionAsync();
            AppNotification.ShowInfo("Sessione parcheggiata.");
            RebuildCartQuantities();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore parcheggio sessione POS2026.");
            AppNotification.ShowWarning("Si è verificato un errore.");
        }
    }

    private async Task CancelSessionAsync()
    {
        if (!ViewModel.HasActiveSession) return;
        try
        {
            var confirmed = await ShowConfirmAsync("Annulla vendita", "Annullare la vendita corrente?");
            if (!confirmed) return;
            await ViewModel.CancelSessionAsync();
            _cartQuantities.Clear();
            AppNotification.ShowInfo("Vendita annullata.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore annullamento sessione POS2026.");
            AppNotification.ShowWarning("Si è verificato un errore.");
        }
    }

    // =========================================================================
    //  Helper UI
    // =========================================================================

    private async Task<bool> ShowConfirmAsync(string title, string message)
    {
        var result = await DialogService.ShowMessageBoxAsync(title, message,
            yesText: "Conferma", cancelText: "Annulla");
        return result == true;
    }

    private void HandleNotification(string message, bool isSuccess)
    {
        if (isSuccess) AppNotification.ShowSuccess(message);
        else AppNotification.ShowError(message);
    }

    private void OnViewModelStateChanged()
    {
        RebuildCartQuantities();
        InvokeAsync(StateHasChanged);
    }

    private async Task OnOperatorIdChangedAsync(Guid? value)
    {
        try
        {
            ViewModel.SelectedOperatorId = value;
            await LoadFiscalDrawerAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore cambio operatore POS2026.");
        }
    }

    private async Task OnPosIdChangedAsync(Guid? value)
    {
        try
        {
            ViewModel.SelectedPosId = value;
            await LoadFiscalDrawerAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore cambio POS POS2026.");
        }
    }

    // =========================================================================
    //  Keyboard shortcuts (stesso pattern POS.razor)
    // =========================================================================

    [JSInvokable]
    public async Task HandleKeyboardShortcut(string key)
    {
        try
        {
            if (!ViewModel.HasActiveSession && key != "Escape") return;

            switch (key)
            {
                case "F2":
                    if (ViewModel.CanPay) await OpenPaymentDialogAsync();
                    break;
                case "F3":
                    await ParkSessionAsync();
                    break;
                case "F4":
                    if (ViewModel.CurrentSession?.Items.LastOrDefault() is { } last)
                        await SalesService.RemoveItemAsync(ViewModel.CurrentSession.Id, last.Id);
                    await ViewModel.ReloadSessionAsync();
                    RebuildCartQuantities();
                    break;
                case "F8":
                    _searchBar?.FocusInput();
                    break;
                case "F12":
                    if (ViewModel.CanCloseSale) await CloseSaleAsync();
                    break;
                case "Escape":
                    await CancelSessionAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore shortcut tastiera POS2026: {Key}", key);
        }
    }

    // =========================================================================
    //  Fiscal printing (stesso pattern POS.razor)
    // =========================================================================

    private async Task ConnectFiscalHubAsync()
    {
        if (!_fiscalPrinterId.HasValue) return;
        try
        {
            _fiscalHubConnection = new HubConnectionBuilder()
                .WithUrl("/hubs/fiscal-printer")
                .WithAutomaticReconnect()
                .Build();

            _fiscalHubConnection.On<Guid, FiscalPrinterStatus>("PrinterStatusUpdated", (id, status) =>
            {
                if (id != _fiscalPrinterId.Value) return;
                _fiscalPrinterStatus = status;
                InvokeAsync(StateHasChanged);
            });

            _fiscalHubConnection.On<Guid, string>("ClosureRequired", (id, _) =>
            {
                if (id != _fiscalPrinterId.Value) return;
                if (_fiscalPrinterStatus != null) _fiscalPrinterStatus.IsDailyClosureRequired = true;
                InvokeAsync(StateHasChanged);
            });

            await _fiscalHubConnection.StartAsync();
            await _fiscalHubConnection.SendAsync("SubscribeToPrinter", _fiscalPrinterId.Value);
            _fiscalPrinterStatus = await FiscalPrintingService.GetStatusAsync(_fiscalPrinterId.Value);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "POS2026: impossibile connettersi all'hub stampante fiscale.");
        }
    }

    private async Task LoadFiscalDrawerAsync()
    {
        _fiscalDrawerSummary = null;
        try
        {
            if (ViewModel.SelectedPosId.HasValue)
            {
                var drawer = await FiscalDrawerService.GetByPosIdAsync(ViewModel.SelectedPosId.Value);
                if (drawer != null)
                    _fiscalDrawerSummary = await FiscalDrawerService.GetSummaryAsync(drawer.Id);
            }
            else if (ViewModel.SelectedOperatorId.HasValue)
            {
                var drawer = await FiscalDrawerService.GetByOperatorIdAsync(ViewModel.SelectedOperatorId.Value);
                if (drawer != null)
                    _fiscalDrawerSummary = await FiscalDrawerService.GetSummaryAsync(drawer.Id);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "POS2026: impossibile caricare il cassetto fiscale.");
        }
        StateHasChanged();
    }

    private async Task OpenDrawerSessionAsync()
    {
        try
        {
            if (_fiscalDrawerSummary == null) return;
            var dto = new OpenFiscalDrawerSessionDto { OpeningBalance = 0 };
            var result = await FiscalDrawerService.OpenSessionAsync(_fiscalDrawerSummary.Id, dto);
            if (result != null)
            {
                AppNotification.ShowSuccess("Sessione cassetto aperta.");
                await LoadFiscalDrawerAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "POS2026: errore apertura sessione cassetto.");
            AppNotification.ShowWarning("Si è verificato un errore.");
        }
    }

    private async Task OpenDrawerTransactionDialogAsync(FiscalDrawerTransactionType type)
    {
        try
        {
            if (_fiscalDrawerSummary == null) return;
            var parameters = new DialogParameters
            {
                ["DrawerId"]        = _fiscalDrawerSummary.Id,
                ["TransactionType"] = type,
                ["CurrencyCode"]    = _fiscalDrawerSummary.CurrencyCode
            };
            var dialog = await DialogService.ShowAsync<FiscalDrawerTransactionDialog>(
                type == FiscalDrawerTransactionType.Deposit ? "Versamento" : "Prelievo",
                parameters,
                new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true });
            var result = await dialog.Result;
            if (result?.Canceled == false)
                await LoadFiscalDrawerAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "POS2026: errore dialog transazione cassetto.");
            AppNotification.ShowWarning("Si è verificato un errore.");
        }
    }

    private async Task OpenPhysicalDrawerAsync()
    {
        if (!_fiscalPrinterId.HasValue) return;
        try
        {
            var result = await FiscalPrintingService.OpenDrawerAsync(_fiscalPrinterId.Value);
            if (result?.Success == true)
                AppNotification.ShowSuccess("Cassetto aperto.");
            else
                AppNotification.ShowWarning($"Impossibile aprire il cassetto: {result?.ErrorMessage ?? "stampante non disponibile."}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "POS2026: errore apertura cassetto fisico.");
            AppNotification.ShowError("Errore durante l'apertura del cassetto.");
        }
    }

    private async Task OpenDailyClosureDialogAsync()
    {
        try
        {
            if (!_fiscalPrinterId.HasValue) return;
            var parameters = new DialogParameters
            {
                [nameof(DailyClosureDialog.PrinterId)]   = _fiscalPrinterId.Value,
                [nameof(DailyClosureDialog.PrinterName)] = "Stampante POS"
            };
            var dialog = await DialogService.ShowAsync<DailyClosureDialog>(
                string.Empty, parameters, EFDialogDefaults.Options);
            var result = await dialog.Result;
            if (result?.Canceled == false)
            {
                _fiscalPrinterStatus = await FiscalPrintingService.GetStatusAsync(_fiscalPrinterId.Value);
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "POS2026: errore dialog chiusura giornaliera.");
            AppNotification.ShowWarning("Si è verificato un errore.");
        }
    }

    private async Task TriggerFiscalPrintAsync(SaleSessionDto session)
    {
        try
        {
            var receipt = BuildFiscalReceiptData(session);
            var printResult = await FiscalPrintingService.PrintReceiptAsync(_fiscalPrinterId!.Value, receipt);

            if (printResult?.Success == true)
                AppNotification.ShowSuccess($"Scontrino fiscale {printResult.ReceiptNumber} emesso.");
            else
                AppNotification.ShowWarning($"Errore stampa fiscale: {printResult?.ErrorMessage ?? "stampante non disponibile."}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "POS2026: errore stampa fiscale.");
            AppNotification.ShowError("Errore imprevisto durante la stampa fiscale.");
        }
    }

    private static FiscalReceiptData BuildFiscalReceiptData(SaleSessionDto session)
    {
        var items = session.Items.Select(item => new FiscalReceiptItem
        {
            Description = item.ProductName ?? item.ProductCode ?? "Articolo",
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            VatCode = 1,
            Department = 1
        }).ToList();

        var payments = session.Payments.Select(p => new FiscalPayment
        {
            Amount = p.Amount,
            MethodCode = 1,
            Description = p.PaymentMethodName ?? "Pagamento"
        }).ToList();

        return new FiscalReceiptData { Items = items, Payments = payments };
    }

    // =========================================================================
    //  Dispose
    // =========================================================================

    public async ValueTask DisposeAsync()
    {
        ViewModel.StateChanged -= OnViewModelStateChanged;
        ViewModel.OnNotification -= HandleNotification;
        ViewModel.Dispose();

        if (_shortcutsModule != null)
        {
            try
            {
                await _shortcutsModule.InvokeVoidAsync("cleanup");
                await _shortcutsModule.DisposeAsync();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "POS2026: errore pulizia shortcuts JS durante Dispose.");
            }
        }

        _dotNetRef?.Dispose();

        if (_fiscalHubConnection != null)
        {
            try
            {
                if (_fiscalPrinterId.HasValue)
                    await _fiscalHubConnection.SendAsync("UnsubscribeFromPrinter", _fiscalPrinterId.Value);
                await _fiscalHubConnection.DisposeAsync();
            }
            catch { /* swallowed during disposal */ }
        }
    }
}
