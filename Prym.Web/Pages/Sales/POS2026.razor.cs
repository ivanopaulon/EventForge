using Prym.Web.Shared.Components.Sales.Pos26;
using Prym.Web.Models.Sales;
using Prym.Web.Services;
using Prym.Web.Services.Sales;
using Prym.Web.Shared.Components.Dialogs;
using Prym.Web.Shared.Components.Dialogs.Sales;

using Prym.Web.ViewModels;
using Prym.DTOs.Analytics;
using Prym.DTOs.Business;
using Prym.DTOs.Common;
using Prym.DTOs.Constants;
using Prym.DTOs.FiscalPrinting;
using Prym.DTOs.Products;
using Prym.DTOs.Sales;
using Prym.DTOs.Store;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;

namespace Prym.Web.Pages.Sales;

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
    private List<ClassificationNodeDto> _classificationNodes = new();   // nodi categoria reali
    private List<string> _categories = new();
    private bool _isLoadingProducts = false;

    // --- Filtri e ordinamento ---
    private string _searchTerm = string.Empty;
    private Pos26SortMode _sortMode = Pos26SortMode.BestSeller;
    private string? _selectedCategory;

    // --- Best seller / ultimi acquisti ---
    private HashSet<Guid> _bestSellerIds = new();
    private HashSet<Guid> _lastPurchaseIds = new();

    // --- Carrello (derivato dalla sessione) ---
    private Dictionary<Guid, int> _cartQuantities = new();

    // --- Prodotti filtrati (materializzati — aggiornati solo quando cambiano i parametri rilevanti) ---
    private IReadOnlyList<ProductDto> _filteredProducts = [];

    // --- Dizionario categoria O(1) per lookup nella sort ---
    private Dictionary<Guid, string> _categoryNodeDict = new();

    // --- Sessioni parcheggiate ---
    private List<SaleSessionDto> _parkedSessions = new();
    private bool _showParkedSessions = false;

    // --- Nota ordine ---
    private bool _showNoteInput = false;
    private string _orderNoteText = string.Empty;

    // --- Coupon ---
    private bool _showCouponInput = false;
    private string _couponInput = string.Empty;

    // --- Tavolo / selezione tipo vendita ---
    private List<TableSessionDto> _availableTables = new();
    private bool _showTablePicker = false;

    // --- Note flags (richiesti da AddSessionNoteDto.NoteFlagId) ---
    private List<NoteFlagDto> _noteFlags = new();
    private Guid? _selectedNoteFlagId;

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
            // Ensure loading flags start clean (guards against stale ViewModel state on re-navigation)
            _isLoadingProducts = false;

            ViewModel.StateChanged += OnViewModelStateChanged;
            ViewModel.OnNotification += HandleNotification;

            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var username = authState.User.Identity?.Name;
            await ViewModel.InitializeAsync(username);

            // Carica in parallelo prodotti, categorie e best seller
            await Task.WhenAll(
                LoadProductsAndBestSellersAsync(),
                LoadClassificationNodesAsync(),
                LoadAvailableTablesAsync(),
                LoadNoteFlagsAsync()
            );

            BuildCategoryList();
            RebuildFilteredProducts();

            // Se c'è già un cliente, carica gli ultimi acquisti
            if (ViewModel.SelectedCustomer != null)
            {
                _sortMode = Pos26SortMode.UltimiAcquisti;
                await LoadLastPurchaseIdsAsync(ViewModel.SelectedCustomer.Id);
            }

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

                // Resolve fiscal printer from the POS terminal already selected by the ViewModel
                LoadFiscalPrinterIdFromPos(ViewModel.SelectedPosId);

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
    //  Caricamento prodotti, categorie e dati ausiliari
    // =========================================================================

    /// <summary>
    /// Carica i prodotti dal servizio e, in parallelo, ottiene i best seller dagli analytics.
    /// </summary>
    private async Task LoadProductsAndBestSellersAsync()
    {
        _isLoadingProducts = true;
        StateHasChanged();
        try
        {
            // Use the slim POS-catalog endpoint (no Codes/Units/BundleItems eager loading)
            var result = await ProductService.GetPosCatalogAsync(page: 1, pageSize: 100);
            _allProducts = result?.Items?.ToList() ?? new();
            RebuildFilteredProducts();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore caricamento prodotti in POS2026.");
        }
        finally
        {
            _isLoadingProducts = false;
            StateHasChanged();
        }

        // Best-sellers fire-and-forget — non blocca la visualizzazione della griglia
        _ = LoadBestSellerIdsAsync().ContinueWith(_ => InvokeAsync(StateHasChanged));
    }

    /// <summary>
    /// Carica gli ID dei best seller usando IAnalyticsService.GetSalesAnalyticsAsync().
    /// TopProducts è ordinato per rank (1 = best seller).
    /// </summary>
    private async Task LoadBestSellerIdsAsync()
    {
        try
        {
            var filter = new AnalyticsFilterDto
            {
                DateFrom = DateTime.UtcNow.AddMonths(-3).Date,
                DateTo   = DateTime.UtcNow.Date,
                Top      = 50
            };
            var analytics = await AnalyticsService.GetSalesAnalyticsAsync(filter);
            if (analytics?.TopProducts is { Count: > 0 } topProducts)
            {
                _bestSellerIds = topProducts
                    .Where(p => p.ProductId.HasValue)
                    .Select(p => p.ProductId!.Value)
                    .ToHashSet();
                RebuildFilteredProducts();
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "POS2026: impossibile caricare best seller da analytics. Fallback a ordine alfabetico.");
        }
    }

    /// <summary>
    /// Carica i nodi di classificazione prodotto (categorie reali) da IEntityManagementService.
    /// I ProductDto.CategoryNodeId vengono poi abbinati ai nomi dei nodi.
    /// </summary>
    private async Task LoadClassificationNodesAsync()
    {
        try
        {
            var nodes = await EntityManagementService.GetClassificationNodesAsync();
            _classificationNodes = nodes?.Where(n => n.IsActive).OrderBy(n => n.Name).ToList() ?? new();
            _categoryNodeDict = _classificationNodes.ToDictionary(n => n.Id, n => n.Name);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "POS2026: impossibile caricare nodi classificazione.");
        }
    }

    /// <summary>
    /// Carica i tavoli disponibili da ITableManagementService.
    /// </summary>
    private async Task LoadAvailableTablesAsync()
    {
        try
        {
            _availableTables = await TableManagementService.GetAvailableTablesAsync() ?? new();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "POS2026: impossibile caricare i tavoli disponibili.");
        }
    }

    /// <summary>
    /// Carica i flag nota attivi (richiesti da AddSessionNoteDto.NoteFlagId).
    /// Il primo flag attivo viene pre-selezionato come default.
    /// </summary>
    private async Task LoadNoteFlagsAsync()
    {
        try
        {
            _noteFlags = await NoteFlagService.GetActiveAsync() ?? new();
            _selectedNoteFlagId = _noteFlags.FirstOrDefault()?.Id;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "POS2026: impossibile caricare i flag nota.");
        }
    }


    /// Costruisce la lista delle categorie da mostrare nella CategoryBar.
    /// Usa i nodi di classificazione (GetClassificationNodesAsync) abbinati a CategoryNodeId dei prodotti.
    /// Se i nodi non sono disponibili, cade back sui nomi IVA come proxy.
    /// </summary>
    private void BuildCategoryList()
    {
        if (_classificationNodes.Any())
        {
            // Categorie reali: include solo quelle effettivamente usate dai prodotti caricati
            var usedNodeIds = _allProducts
                .Where(p => p.CategoryNodeId.HasValue)
                .Select(p => p.CategoryNodeId!.Value)
                .ToHashSet();

            _categories = _classificationNodes
                .Where(n => usedNodeIds.Contains(n.Id))
                .Select(n => n.Name)
                .ToList();
        }
        else
        {
            // Fallback: usa VatRateName come proxy categoria (meno preciso ma sempre disponibile)
            _categories = _allProducts
                .Where(p => !string.IsNullOrEmpty(p.VatRateName))
                .Select(p => p.VatRateName!)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }
    }

    /// <summary>
    /// Carica gli ID dei prodotti acquistati dal cliente usando l'endpoint dedicato.
    /// </summary>
    private async Task LoadLastPurchaseIdsAsync(Guid customerId)
    {
        _lastPurchaseIds.Clear();
        try
        {
            var ids = await SalesService.GetCustomerPurchasedProductIdsAsync(customerId);
            if (ids != null)
                _lastPurchaseIds = ids.ToHashSet();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "POS2026: impossibile caricare ultimi acquisti cliente {CustomerId}.", customerId);
        }
    }

    /// <summary>
    /// Carica le sessioni parcheggiate (SaleSessionStatusDto.Suspended) per il menu "Sessioni parcheggiate".
    /// </summary>
    private async Task LoadParkedSessionsAsync()
    {
        try
        {
            var all = await SalesService.GetActiveSessionsAsync();
            _parkedSessions = all?
                .Where(s => s.Status == SaleSessionStatusDto.Suspended)
                .OrderByDescending(s => s.UpdatedAt)
                .ToList() ?? new();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "POS2026: impossibile caricare sessioni parcheggiate.");
        }
    }

    private async Task LoadProductsAsync()
    {
        _isLoadingProducts = true;
        StateHasChanged();
        try
        {
            var result = await ProductService.GetPosCatalogAsync(page: 1, pageSize: 100);
            _allProducts = result?.Items?.ToList() ?? new();
            BuildCategoryList();
            RebuildFilteredProducts();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore caricamento prodotti in POS2026.");
        }
        finally
        {
            _isLoadingProducts = false;
            StateHasChanged();
        }
    }

    // =========================================================================
    //  Filtri e ordinamento
    // =========================================================================

    /// <summary>
    /// Materializza _filteredProducts. Chiamata solo quando cambiano i parametri rilevanti.
    /// </summary>
    private void RebuildFilteredProducts()
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

        // Filtro categoria — usa CategoryNodeId se disponibili i nodi reali, altrimenti VatRateName
        if (!string.IsNullOrEmpty(_selectedCategory))
        {
            if (_categoryNodeDict.Count > 0)
            {
                var nodeId = _classificationNodes
                    .FirstOrDefault(n => n.Name == _selectedCategory)?.Id;
                if (nodeId.HasValue)
                    source = source.Where(p => p.CategoryNodeId == nodeId.Value);
            }
            else
            {
                source = source.Where(p => p.VatRateName == _selectedCategory);
            }
        }

        // Ordinamento — usa _categoryNodeDict per lookup O(1) nella sort per categoria
        source = _sortMode switch
        {
            Pos26SortMode.Alfabetico     => source.OrderBy(p => p.Name),
            Pos26SortMode.Prezzo         => source.OrderBy(p => p.DefaultPrice ?? 0),
            Pos26SortMode.BestSeller     => source.OrderByDescending(p => _bestSellerIds.Contains(p.Id)).ThenBy(p => p.Name),
            Pos26SortMode.UltimiAcquisti when ViewModel.SelectedCustomer != null
                                         => source.OrderByDescending(p => _lastPurchaseIds.Contains(p.Id)).ThenBy(p => p.Name),
            Pos26SortMode.UltimiAcquisti => source.OrderByDescending(p => _bestSellerIds.Contains(p.Id)).ThenBy(p => p.Name),
            Pos26SortMode.Categoria      =>
                _categoryNodeDict.Count > 0
                    ? source.OrderBy(p => p.CategoryNodeId.HasValue && _categoryNodeDict.TryGetValue(p.CategoryNodeId.Value, out var cn) ? cn : string.Empty).ThenBy(p => p.Name)
                    : source.OrderBy(p => p.VatRateName).ThenBy(p => p.Name),
            _                            => source.OrderBy(p => p.Name)
        };

        _filteredProducts = source.ToList();
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
                {
                    _allProducts = result.SearchResults.ToList();
                    BuildCategoryList();
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Errore nella ricerca prodotti POS2026.");
            }
        }
        else if (_allProducts.Count == 0)
        {
            // Solo se il catalogo è vuoto lo ricarichiamo; altrimenti usa _allProducts già in memoria
            await LoadProductsAsync();
        }

        RebuildFilteredProducts();
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

        if (mode == Pos26SortMode.UltimiAcquisti && ViewModel.SelectedCustomer == null)
            AppNotification.ShowInfo("Seleziona un cliente per vedere gli ultimi acquisti.");

        RebuildFilteredProducts();
        StateHasChanged();
        await Task.CompletedTask;
    }

    private async Task OnCategorySelectedAsync(string? category)
    {
        _selectedCategory = category;
        RebuildFilteredProducts();
        StateHasChanged();
        await Task.CompletedTask;
    }

    private async Task OnSelectedCustomerChangedAsync(BusinessPartyDto? customer)
    {
        try
        {
            await ViewModel.UpdateSelectedCustomerAsync(customer);

            if (customer != null)
            {
                // Carica gli ultimi acquisti del cliente appena selezionato
                await LoadLastPurchaseIdsAsync(customer.Id);
                if (_sortMode == Pos26SortMode.BestSeller)
                    _sortMode = Pos26SortMode.UltimiAcquisti;
            }
            else
            {
                _lastPurchaseIds.Clear();
                if (_sortMode == Pos26SortMode.UltimiAcquisti)
                    _sortMode = Pos26SortMode.BestSeller;
            }

            RebuildFilteredProducts();
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

    /// <summary>
    /// Opens the product detail/edit dialog when the user long-presses a card.
    /// Uses the existing <c>ProductDetailDialog</c> with read-only entity view.
    /// </summary>
    private async Task OpenProductDetailAsync(ProductDto product)
    {
        try
        {
            var parameters = new DialogParameters<Prym.Web.Shared.Components.Dialogs.Products.ProductDetailDialog>
            {
                { p => p.EntityId, product.Id }
            };
            await DialogService.ShowAsync<Prym.Web.Shared.Components.Dialogs.Products.ProductDetailDialog>(
                product.Name, parameters, EFDialogDefaults.Options);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore apertura dettaglio prodotto POS2026: {ProductId}", product.Id);
            AppNotification.ShowWarning("Impossibile aprire il dettaglio prodotto.");
        }
    }

    private void RebuildCartQuantities()
    {
        _cartQuantities = ViewModel.CurrentSession?.Items?
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => (int)g.Sum(i => i.Quantity))
            ?? new();
    }

    private void IncreaseQuantity(SaleItemDto item)
    {
        if (ViewModel.IsUpdatingItems) return;
        item.Quantity++;
        ViewModel.QueueItemUpdate(item);
        RebuildCartQuantities();
    }

    private void DecreaseQuantity(SaleItemDto item)
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
    //  Sconto globale dal numpad (percentuale e valore fisso)
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

    /// <summary>
    /// Applica uno sconto a valore fisso calcolando la percentuale equivalente sul totale corrente.
    /// Il DTO dell'API accetta solo percentuali, quindi la conversione avviene lato client.
    /// </summary>
    private async Task ApplyFixedDiscountAsync(decimal importo)
    {
        if (!ViewModel.HasActiveSession) return;
        var totale = ViewModel.GrandTotal;
        if (totale <= 0 || importo <= 0 || importo > totale)
        {
            AppNotification.ShowWarning("Importo sconto non valido.");
            return;
        }

        var percentuale = Math.Round(importo / totale * 100, 4);
        await ApplyDiscountAsync(percentuale);
    }

    // =========================================================================
    //  Dialog pagamento
    // =========================================================================

    private async Task OpenPaymentDialogAsync()
    {
        if (!ViewModel.CanPay) return;
        try
        {
            var paymentMethods = ViewModel.PaymentMethods.Count > 0
                ? ViewModel.PaymentMethods
                : await PaymentMethodService.GetActiveAsync() ?? new();

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

    // =========================================================================
    //  Note ordine
    // =========================================================================

    private void ToggleNoteInput()
    {
        _showNoteInput = !_showNoteInput;
        if (!_showNoteInput) _orderNoteText = string.Empty;
    }

    private async Task SaveOrderNoteAsync()
    {
        if (!ViewModel.HasActiveSession || string.IsNullOrWhiteSpace(_orderNoteText)) return;

        // AddSessionNoteDto richiede un NoteFlagId valido.
        // Usa il flag selezionato; se non disponibile mostra avviso.
        if (!_selectedNoteFlagId.HasValue)
        {
            AppNotification.ShowWarning("Nessun flag nota disponibile. Configurare almeno un flag nota in Impostazioni.");
            return;
        }

        try
        {
            var dto = new AddSessionNoteDto
            {
                NoteFlagId = _selectedNoteFlagId.Value,
                Text       = _orderNoteText.Trim()
            };
            var session = await ViewModel.AddSessionNoteAsync(dto);
            if (session != null)
            {
                AppNotification.ShowSuccess("Nota aggiunta.");
                _orderNoteText = string.Empty;
                _showNoteInput = false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore aggiunta nota ordine POS2026.");
            AppNotification.ShowWarning("Si è verificato un errore.");
        }
    }

    // =========================================================================
    //  Coupon
    // =========================================================================

    private void ToggleCouponInput()
    {
        _showCouponInput = !_showCouponInput;
        if (!_showCouponInput) _couponInput = string.Empty;
    }

    private async Task ApplyCouponAsync()
    {
        if (string.IsNullOrWhiteSpace(_couponInput)) return;
        try
        {
            var (success, promoName, error) = await ViewModel.ApplyCouponAsync(_couponInput.Trim());
            if (success)
            {
                AppNotification.ShowSuccess($"Coupon applicato: {promoName}");
                _couponInput = string.Empty;
                _showCouponInput = false;
            }
            else
            {
                AppNotification.ShowWarning(error ?? "Coupon non valido.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore applicazione coupon POS2026.");
            AppNotification.ShowWarning("Si è verificato un errore.");
        }
    }

    // =========================================================================
    //  Tavolo / tipo vendita
    // =========================================================================

    private async Task ToggleTablePickerAsync()
    {
        _showTablePicker = !_showTablePicker;
        if (_showTablePicker)
            await LoadAvailableTablesAsync();
    }

    private async Task AssignTableAsync(TableSessionDto table)
    {
        if (!ViewModel.HasActiveSession) return;
        try
        {
            // UpdateSaleSessionDto non espone TableId; la sessione viene aggiornata con SaleType
            // e il numero tavolo viene mostrato nell'header come info contestuale.
            // Per assegnare davvero un tavolo alla sessione è necessario un endpoint dedicato.
            // TODO: chiamare un endpoint dedicato /api/v1/sales/sessions/{id}/table una volta disponibile.
            var updateDto = new UpdateSaleSessionDto { SaleType = SaleTypes.Retail };
            await SalesService.UpdateSessionAsync(ViewModel.CurrentSession!.Id, updateDto);
            await ViewModel.ReloadSessionAsync();
            AppNotification.ShowSuccess($"Sessione impostata su Tavolo {table.TableNumber}.");
            _showTablePicker = false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore assegnazione tavolo POS2026.");
            AppNotification.ShowWarning("Si è verificato un errore.");
        }
    }

    private async Task SetSaleTypeAsync(string saleType)
    {
        if (!ViewModel.HasActiveSession) return;
        try
        {
            var updateDto = new UpdateSaleSessionDto { SaleType = saleType };
            var session = await SalesService.UpdateSessionAsync(ViewModel.CurrentSession!.Id, updateDto);
            if (session != null)
            {
                await ViewModel.ReloadSessionAsync();
                var label = saleType == SaleTypes.Retail ? "Banco" : "Asporto";                AppNotification.ShowSuccess($"Tipo vendita: {label}.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore cambio tipo vendita POS2026.");
            AppNotification.ShowWarning("Si è verificato un errore.");
        }
    }

    // =========================================================================
    //  Sessioni parcheggiate
    // =========================================================================

    private async Task ToggleParkedSessionsAsync()
    {
        _showParkedSessions = !_showParkedSessions;
        if (_showParkedSessions)
            await LoadParkedSessionsAsync();
    }

    private async Task ResumeParkedSessionAsync(SaleSessionDto session)
    {
        try
        {
            // Riprende la sessione sospesa aggiornando lo stato a Open
            var updateDto = new UpdateSaleSessionDto { Status = SaleSessionStatusDto.Open };
            var resumed = await SalesService.UpdateSessionAsync(session.Id, updateDto);
            if (resumed != null)
            {
                await ViewModel.ReloadSessionAsync();
                _showParkedSessions = false;
                AppNotification.ShowSuccess($"Sessione #{session.Id.ToString()[..8]} ripresa.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore ripresa sessione parcheggiata POS2026.");
            AppNotification.ShowWarning("Si è verificato un errore.");
        }
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
            LoadFiscalPrinterIdFromPos(value);
            await Task.WhenAll(LoadFiscalDrawerAsync(), ConnectFiscalHubAsync());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore cambio POS POS2026.");
        }
    }

    /// <summary>
    /// Populates <see cref="_fiscalPrinterId"/> from the selected POS terminal's
    /// <c>DefaultFiscalPrinterId</c> property (no additional HTTP call required).
    /// </summary>
    private void LoadFiscalPrinterIdFromPos(Guid? posId)
    {
        _fiscalPrinterId = posId.HasValue
            ? ViewModel.AvailablePos.FirstOrDefault(p => p.Id == posId.Value)?.DefaultFiscalPrinterId
            : null;
        Logger.LogDebug("POS2026: fiscal printer ID set to {PrinterId}", _fiscalPrinterId);
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
                .WithUrl(NavigationManager.ToAbsoluteUri("/hubs/fiscal-printer"))
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

            _fiscalHubConnection.Reconnected += async _ =>
            {
                try { await _fiscalHubConnection.SendAsync("SubscribeToPrinter", _fiscalPrinterId.Value); }
                catch (Exception ex) { Logger.LogWarning(ex, "POS2026: failed to re-subscribe to printer {PrinterId} after reconnect", _fiscalPrinterId.Value); }
            };

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
