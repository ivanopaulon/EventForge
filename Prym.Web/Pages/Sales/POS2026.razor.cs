using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using Prym.DTOs.Analytics;
using Prym.DTOs.Business;
using Prym.DTOs.Business.Fidelity;
using Prym.DTOs.Common;
using Prym.DTOs.Constants;
using Prym.DTOs.FiscalPrinting;
using Prym.DTOs.Products;
using Prym.DTOs.Sales;
using Prym.DTOs.Store;
using Prym.DTOs.Warehouse;
using Prym.Web.Models.Sales;
using Prym.Web.Services;
using Prym.Web.Shared.Components.Dialogs;
using Prym.Web.Shared.Components.Dialogs.Sales;
using Prym.Web.Shared.Components.Sales.Pos26;

namespace Prym.Web.Pages.Sales;

/// <summary>
/// POS 2026 — versione evolutiva del Punto Vendita.
/// Replica tutte le funzionalità di POS.razor e POSTouch.razor con nuovo layout a due colonne
/// e griglia prodotti interattiva.
/// </summary>
public partial class POS2026 : IAsyncDisposable
{
    [Inject] IFidelityService FidelityService { get; set; } = default!;
    [Inject] IBusinessPartyService BusinessPartyService { get; set; } = default!;

    // --- Componente reference per focus ---
    private Pos26SearchBar? _searchBar;

    // --- Stato prodotti ---
    private List<ProductDto> _allProducts = new();
    private List<ProductDto> _fullCatalogProducts = new();              // catalogo completo — mai modificato dalla ricerca
    private List<ClassificationNodeDto> _classificationNodes = new();   // nodi categoria reali
    private List<string> _categories = new();
    private bool _isLoadingProducts = false;

    // --- Disponibilità stock per prodotto (somma AvailableQuantity su tutte le location) ---
    private Dictionary<Guid, decimal> _stockByProductId = new();

    // --- Filtri e ordinamento ---
    private string _searchTerm = string.Empty;
    private Pos26SortMode _sortMode = Pos26SortMode.BestSeller;
    private string? _selectedCategory;
    private CancellationTokenSource? _searchDebounce;                   // debounce ricerca per evitare HTTP ad ogni tasto

    // --- Best seller / ultimi acquisti ---
    private HashSet<Guid> _bestSellerIds = new();
    private List<Guid> _bestSellerRankedIds = new(); // ordine conservato dall'API (rank crescente)
    private HashSet<Guid> _lastPurchaseIds = new();

    // --- Top best sellers per la riga rapida sopra la griglia (max 10 articoli) ---
    private List<ProductDto> _topBestSellers = new();

    // --- Carrello (derivato dalla sessione) ---
    private Dictionary<Guid, int> _cartQuantities = new();

    // --- Prodotti filtrati (materializzati — aggiornati solo quando cambiano i parametri rilevanti) ---
    private IReadOnlyList<ProductDto> _filteredProducts = [];

    // --- Dizionario categoria O(1) per lookup nella sort ---
    private Dictionary<Guid, string> _categoryNodeDict = new();

    // --- Dizionario inverso O(1): nome → ID, per filtraggio categoria ---
    private Dictionary<string, Guid> _categoryNameToIdDict = new();

    // --- Fidelity card ---
    private FidelityCardDto? _fidelityCard;
    private bool _isFidelityLookupInProgress = false;  // guard: prevenzione race condition su scan rapidi

    // --- Sessioni parcheggiate ---
    private List<SaleSessionDto> _parkedSessions = new();
    private bool _showParkedSessions = false;

    // --- Mobile tab (0 = Prodotti, 1 = Scontrino+Paga) ---
    private int _mobileTab = 0;

    // --- Nota ordine ---
    private bool _showNoteInput = false;
    private string _orderNoteText = string.Empty;

    // --- Coupon ---
    private bool _showCouponInput = false;
    private string _couponInput = string.Empty;

    // --- Tavolo / selezione tipo vendita ---
    private List<TableSessionDto> _availableTables = new();
    private bool _showTablePicker = false;

    // --- Menu azioni secondarie (Tavolo, Sospese, Dividi, Unisci) ---
    private bool _showMoreMenu = false;

    // --- Note flags (richiesti da AddSessionNoteDto.NoteFlagId) ---
    private List<NoteFlagDto> _noteFlags = new();
    private Guid? _selectedNoteFlagId;

    // --- Fiscal printing (stesso pattern di POS.razor) ---
    private Guid? _fiscalPrinterId;
    private FiscalPrinterStatus? _fiscalPrinterStatus;
    private FiscalDrawerSummaryDto? _fiscalDrawerSummary;
    private HubConnection? _fiscalHubConnection;

    // --- Morning closure check ---
    private bool _previousDayClosureMissing = false;
    private bool _noPrinterConfigured = false;
    private DateTime? _lastClosureDate = null;

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

            // Tutti i loader non dipendono dalla sessione: vengono avviati in parallelo con
            // ViewModel.InitializeAsync per ridurre il tempo totale di avvio della pagina.
            await Task.WhenAll(
                ViewModel.InitializeAsync(username),
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
                await LoadFidelityCardAsync(ViewModel.SelectedCustomer.Id);
            }
            else
            {
                _fidelityCard = null;
            }

            RebuildCartQuantities();

            // Carica sessioni aperte e controlla possibilità di split (non dipende dalla sessione corrente)
            await Task.WhenAll(LoadOpenSessionsAsync(), CheckCanSplitAsync());
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

                // Controllo mattutino: verifica che la chiusura del giorno precedente sia stata eseguita.
                // DB-only, non richiede la stampante online.
                await CheckPreviousDayClosureAsync();
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
    /// Entrambi vengono attesi prima del primo render per evitare layout shift (flickering della griglia).
    /// </summary>
    private async Task LoadProductsAndBestSellersAsync()
    {
        _isLoadingProducts = true;
        // Non chiamare StateHasChanged() qui: durante OnInitializedAsync, il PageLoadingOverlay
        // è già visibile tramite ViewModel.IsLoading. Il render finale avverrà al termine di OnInitializedAsync,
        // dopo che sia i prodotti sia i best sellers sono stati caricati in parallelo.
        try
        {
            // Carica prodotti, best sellers e disponibilità stock in parallelo — unico render al completamento.
            var productsTask = ProductService.GetPosCatalogAsync(page: 1, pageSize: 100);
            var bestSellersTask = LoadBestSellerIdsAsync();
            var stockTask = LoadStockAvailabilityAsync();
            await Task.WhenAll(productsTask, bestSellersTask, stockTask);

            var result = productsTask.Result;
            _allProducts = result?.Items?.ToList() ?? new();
            _fullCatalogProducts = new List<ProductDto>(_allProducts); // cache per ripristino dopo ricerca
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore caricamento prodotti in POS2026.");
        }
        finally
        {
            _isLoadingProducts = false;
            // StateHasChanged() non è necessario qui: OnInitializedAsync chiama RebuildFilteredProducts
            // e poi Blazor effettua il render al termine di OnInitializedAsync. Se chiamato
            // come standalone (es. dopo una ricerca), il chiamante è responsabile del render.
        }
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
                DateFrom = DateTime.UtcNow.AddMonths(-1).Date, // finestra di 1 mese per prestazioni query
                DateTo = DateTime.UtcNow.Date,
                Top = 50
            };
            var analytics = await AnalyticsService.GetSalesAnalyticsAsync(filter);
            if (analytics?.TopProducts is { Count: > 0 } topProducts)
            {
                var ranked = topProducts
                    .Where(p => p.ProductId.HasValue)
                    .Select(p => p.ProductId!.Value)
                    .ToList();
                _bestSellerRankedIds = ranked;
                _bestSellerIds = ranked.ToHashSet();
                // RebuildFilteredProducts viene invocata al ritorno in LoadProductsAndBestSellersAsync, dopo Task.WhenAll.
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "POS2026: impossibile caricare best seller da analytics. Fallback a ordine alfabetico.");
        }
    }

    /// <summary>
    /// Carica la disponibilità stock per tutti i prodotti, gestendo la paginazione.
    /// Aggrega AvailableQuantity (Quantity − Reserved) per prodotto su tutte le location/lotti.
    /// Viene eseguita in parallelo con il caricamento prodotti all'avvio.
    /// </summary>
    private async Task LoadStockAvailabilityAsync()
    {
        const int stockPageSize = 500;
        try
        {
            var allItems = new List<StockDto>();
            var firstPage = await StockService.GetStockAsync(page: 1, pageSize: stockPageSize);
            if (firstPage?.Items != null)
            {
                allItems.AddRange(firstPage.Items);
                // Recupera le pagine rimanenti se il catalogo supera stockPageSize record
                for (int p = 2; p <= (firstPage.TotalPages); p++)
                {
                    var page = await StockService.GetStockAsync(page: p, pageSize: stockPageSize);
                    if (page?.Items != null)
                        allItems.AddRange(page.Items);
                }
            }

            _stockByProductId = allItems
                .GroupBy(s => s.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.AvailableQuantity));
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "POS2026: impossibile caricare disponibilità stock.");
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
            _categoryNameToIdDict = _classificationNodes.ToDictionary(n => n.Name, n => n.Id, StringComparer.Ordinal);
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
    /// Carica la carta fedeltà attiva con il saldo punti più alto per il cliente selezionato.
    /// </summary>
    private async Task LoadFidelityCardAsync(Guid businessPartyId)
    {
        try
        {
            var cards = await FidelityService.GetCardsByBusinessPartyAsync(businessPartyId);
            _fidelityCard = cards
                .Where(c => c.Status == FidelityCardStatus.Active)
                .OrderByDescending(c => c.CurrentPoints)
                .FirstOrDefault();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "POS2026: impossibile caricare carta fedeltà per cliente {Id}.", businessPartyId);
            _fidelityCard = null;
        }
    }

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
            _fullCatalogProducts = new List<ProductDto>(_allProducts); // aggiorna cache catalogo completo
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

        // Filtro categoria — usa _categoryNameToIdDict (O(1)) se disponibili i nodi reali, altrimenti VatRateName
        if (!string.IsNullOrEmpty(_selectedCategory))
        {
            if (_categoryNameToIdDict.TryGetValue(_selectedCategory, out var nodeId))
                source = source.Where(p => p.CategoryNodeId == nodeId);
            else if (_categoryNodeDict.Count == 0)
                source = source.Where(p => p.VatRateName == _selectedCategory);
        }

        // Ordinamento — usa _categoryNodeDict per lookup O(1) nella sort per categoria
        source = _sortMode switch
        {
            Pos26SortMode.Alfabetico => source.OrderBy(p => p.Name),
            Pos26SortMode.Prezzo => source.OrderBy(p => p.DefaultPrice ?? 0),
            Pos26SortMode.BestSeller => source.OrderByDescending(p => _bestSellerIds.Contains(p.Id)).ThenBy(p => p.Name),
            Pos26SortMode.UltimiAcquisti when ViewModel.SelectedCustomer != null
                                         => source.OrderByDescending(p => _lastPurchaseIds.Contains(p.Id)).ThenBy(p => p.Name),
            Pos26SortMode.UltimiAcquisti => source.OrderByDescending(p => _bestSellerIds.Contains(p.Id)).ThenBy(p => p.Name),
            Pos26SortMode.Categoria =>
                _categoryNodeDict.Count > 0
                    ? source.OrderBy(p => p.CategoryNodeId.HasValue && _categoryNodeDict.TryGetValue(p.CategoryNodeId.Value, out var cn) ? cn : string.Empty).ThenBy(p => p.Name)
                    : source.OrderBy(p => p.VatRateName).ThenBy(p => p.Name),
            _ => source.OrderBy(p => p.Name)
        };

        _filteredProducts = source.ToList();

        // Aggiorna la riga rapida dei best sellers (max 10 articoli del catalogo completo ordinati per rank).
        // La riga è visibile solo quando non c'è ricerca attiva e ci sono best sellers disponibili.
        if (_bestSellerRankedIds.Count > 0 && string.IsNullOrWhiteSpace(_searchTerm))
        {
            // Crea un dizionario rank → posizione per ordinare i prodotti nell'ordine del rank analytics
            var rankMap = new Dictionary<Guid, int>(_bestSellerRankedIds.Count);
            for (int i = 0; i < _bestSellerRankedIds.Count; i++)
                rankMap[_bestSellerRankedIds[i]] = i;

            _topBestSellers = _fullCatalogProducts
                .Where(p => _bestSellerIds.Contains(p.Id))
                .OrderBy(p => rankMap.GetValueOrDefault(p.Id, int.MaxValue))
                .Take(10)
                .ToList();
        }
        else
        {
            _topBestSellers = new List<ProductDto>();
        }
    }

    // =========================================================================
    //  Gestione eventi UI
    // =========================================================================

    private async Task OnSearchTermChanged(string term)
    {
        _searchTerm = term;

        // Cancellation guard: discards stale results if the user types faster than the HTTP response.
        _searchDebounce?.Cancel();
        _searchDebounce?.Dispose();
        _searchDebounce = new CancellationTokenSource();
        var cts = _searchDebounce;

        if (string.IsNullOrWhiteSpace(term))
        {
            // Ricerca azzerata: ripristina il catalogo completo dalla cache locale (zero HTTP call)
            if (_fullCatalogProducts.Count > 0)
            {
                _allProducts = new List<ProductDto>(_fullCatalogProducts);
                BuildCategoryList();
                // Se la categoria selezionata non esiste più nella lista ricostruita, resettala
                if (_selectedCategory != null && !_categories.Contains(_selectedCategory))
                    _selectedCategory = null;
            }
            else
            {
                // Catalogo mai caricato (es. primo avvio fallito): ricarica dal server
                await LoadProductsAsync();
            }
            RebuildFilteredProducts();
            StateHasChanged();
            return;
        }

        // Reset della categoria selezionata durante la ricerca testuale: i pill di categoria
        // vengono ricostruiti sui risultati di ricerca, quindi la categoria potrebbe scomparire
        // rendendo il filtro invisibile ma ancora attivo.
        if (_selectedCategory != null)
            _selectedCategory = null;

        try
        {
            var result = await ProductService.SearchProductsAsync(term, maxResults: 100);

            // Unica guard dopo la chiamata async: se nel frattempo l'utente ha digitato altro,
            // scartiamo il risultato senza aggiornare lo stato del componente.
            if (cts.IsCancellationRequested) return;

            if (result?.SearchResults != null)
            {
                _allProducts = result.SearchResults.ToList();
                BuildCategoryList();
            }

            RebuildFilteredProducts();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Errore nella ricerca prodotti POS2026.");
        }
    }

    private async Task HandleBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return;
        if (_isFidelityLookupInProgress) return;
        _isFidelityLookupInProgress = true;  // impostato prima di qualsiasi await per bloccare re-entry

        try
        {
            // 1. Lookup prodotto — ha sempre la precedenza
            var result = await ProductService.SearchProductsAsync(barcode, maxResults: 1);
            var product = result?.ExactMatch?.Product ?? result?.SearchResults?.FirstOrDefault();
            if (product != null)
            {
                await OnAddProductToCartAsync(product);
                _searchTerm = string.Empty;
                StateHasChanged();
                return;
            }

            // 2. Nessun prodotto trovato → tenta lookup tessera fedeltà
            var card = await FidelityService.GetCardByCardNumberAsync(barcode);

            if (card is null)
            {
                AppNotification.ShowWarning($"Codice non riconosciuto come prodotto né tessera cliente: {barcode}");
                return;
            }

            if (card.Status != FidelityCardStatus.Active)
            {
                var statusLabel = card.Status switch
                {
                    FidelityCardStatus.Suspended => "sospesa",
                    FidelityCardStatus.Expired   => "scaduta",
                    FidelityCardStatus.Revoked   => "revocata",
                    _ => card.Status.ToString()
                };
                if (card.Status is not (FidelityCardStatus.Suspended or FidelityCardStatus.Expired or FidelityCardStatus.Revoked))
                    Logger.LogWarning("Stato tessera sconosciuto: {Status}", card.Status);
                AppNotification.ShowWarning($"Tessera {statusLabel} — cliente non selezionato automaticamente.");
                return;
            }

            if (card.BusinessPartyId is null)
            {
                AppNotification.ShowWarning("Tessera non collegata a nessun cliente.");
                return;
            }

            // 3. Tessera valida — carica il BusinessParty
            var newCustomer = await BusinessPartyService.GetBusinessPartyAsync(card.BusinessPartyId.Value);
            if (newCustomer is null)
            {
                AppNotification.ShowWarning("Cliente della tessera non trovato.");
                return;
            }

            // 4. Se c'è già un cliente diverso selezionato → dialog esplicito
            if (ViewModel.SelectedCustomer != null && ViewModel.SelectedCustomer.Id != newCustomer.Id)
            {
                var parameters = new DialogParameters<Pos26CustomerChangeDialog>
                {
                    { d => d.CurrentCustomer, ViewModel.SelectedCustomer },
                    { d => d.NewCustomer, newCustomer },
                    { d => d.NewFidelityCard, card }
                };
                var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small };
                var dialog = await DialogService.ShowAsync<Pos26CustomerChangeDialog>(
                    "Cambio Cliente", parameters, options);
                var dialogResult = await dialog.Result;
                if (dialogResult is null || dialogResult.Canceled) return;
            }

            // 5. Selezione cliente (percorso nominale o dopo conferma dialog)
            await OnSelectedCustomerChangedAsync(newCustomer);
            AppNotification.ShowSuccess(
                $"Cliente: {newCustomer.Name} | Tessera: {card.CardNumber} | ⭐ {card.CurrentPoints} pt");
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore nella ricerca barcode POS2026: {Barcode}", barcode);
            AppNotification.ShowWarning("Errore nella lettura del codice.");
        }
        finally
        {
            _isFidelityLookupInProgress = false;
        }
    }

    private async Task OpenCameraBarcodeScannerAsync()
    {
        var parameters = new DialogParameters<CameraBarcodeScannerDialog>
        {
            { d => d.OnBarcodeDetected, EventCallback.Factory.Create<string>(this, HandleBarcodeAsync) }
        };
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small };
        await DialogService.ShowAsync<CameraBarcodeScannerDialog>("Scansiona con fotocamera", parameters, options);
    }

    private void OnSortModeChangedAsync(Pos26SortMode mode)
    {
        _sortMode = mode;

        if (mode == Pos26SortMode.UltimiAcquisti && ViewModel.SelectedCustomer == null)
            AppNotification.ShowInfo("Seleziona un cliente per vedere gli ultimi acquisti.");

        RebuildFilteredProducts();
        StateHasChanged();
    }

    private void OnCategorySelectedAsync(string? category)
    {
        _selectedCategory = category;
        RebuildFilteredProducts();
        StateHasChanged();
    }

    private async Task OnSelectedCustomerChangedAsync(BusinessPartyDto? customer)
    {
        try
        {
            await ViewModel.UpdateSelectedCustomerAsync(customer);

            if (customer != null)
            {
                // Carica ultimi acquisti e fidelity card in parallelo
                await Task.WhenAll(
                    LoadLastPurchaseIdsAsync(customer.Id),
                    LoadFidelityCardAsync(customer.Id)
                );
                if (_sortMode == Pos26SortMode.BestSeller)
                    _sortMode = Pos26SortMode.UltimiAcquisti;
            }
            else
            {
                _lastPurchaseIds.Clear();
                if (_sortMode == Pos26SortMode.UltimiAcquisti)
                    _sortMode = Pos26SortMode.BestSeller;
            }

            if (customer == null)
                _fidelityCard = null;

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
                // Notification is already emitted by POSViewModel via OnNotification → HandleNotification.
                // Do NOT call AppNotification here to avoid showing two toasts at once.
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
                ["Item"] = item,
                ["InitialNotes"] = item.Notes ?? string.Empty
            };
            var dialog = await DialogService.ShowAsync<ItemNotesDialog>(
                "Note articolo", parameters,
                new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true });
            var result = await dialog.Result;
            if (result?.Canceled == false)
            {
                var notes = result.Data as string ?? string.Empty;
                var updateDto = new UpdateSaleItemDto
                {
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountPercent = item.DiscountPercent,
                    Notes = notes
                };
                await SalesService.UpdateItemAsync(ViewModel.CurrentSession!.Id, item.Id, updateDto);
                await ViewModel.ReloadSessionAsync();
                RebuildCartQuantities();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore modifica note articolo POS2026.");
            AppNotification.ShowWarning("Si è verificato un errore.");
        }
    }

    /// <summary>
    /// Apre il dialog di modifica riga (quantità, prezzo, sconto) e salva le modifiche tramite UpdateItemAsync.
    /// Invocato dal bottone di modifica in <see cref="Pos26Receipt"/>.
    /// </summary>
    private async Task EditLineItemAsync(SaleItemDto item)
    {
        if (!ViewModel.HasActiveSession) return;
        try
        {
            var parameters = new DialogParameters { ["Item"] = item };
            var dialog = await DialogService.ShowAsync<POSTouchLineEditDialog>(
                string.Empty, parameters,
                new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = false });
            var result = await dialog.Result;
            if (result?.Canceled == false && result.Data is POSTouchLineEditDialog.EditResult edit)
            {
                var updateDto = new UpdateSaleItemDto
                {
                    Quantity = edit.Quantity,
                    UnitPrice = edit.UnitPrice,
                    DiscountPercent = edit.DiscountPercent,
                    Notes = item.Notes
                };
                await SalesService.UpdateItemAsync(ViewModel.CurrentSession!.Id, item.Id, updateDto);
                await ViewModel.ReloadSessionAsync();
                RebuildCartQuantities();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore modifica riga articolo POS2026.");
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

    /// <summary>Stampa la ricevuta tramite il dialog di stampa del browser (per POS senza stampante fiscale).</summary>
    private async Task PrintBrowserReceiptAsync()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("window.print");
        }
        catch (JSDisconnectedException)
        {
            // Connessione WebAssembly persa — ignorare silenziosamente
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore nell'avvio della stampa browser.");
            AppNotification.ShowWarning("Impossibile avviare la stampa.");
        }
    }

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
                [nameof(Pos26PaymentDialog.TotaleOrdine)] = ViewModel.GrandTotal,
                [nameof(Pos26PaymentDialog.ScontoApplicato)] = ViewModel.CurrentSession?.DiscountAmount ?? 0m,
                [nameof(Pos26PaymentDialog.Ordine)] = ViewModel.CurrentSession,
                [nameof(Pos26PaymentDialog.Cliente)] = ViewModel.SelectedCustomer,
                [nameof(Pos26PaymentDialog.PaymentMethods)] = paymentMethods,
                [nameof(Pos26PaymentDialog.FidelityCard)] = (object?)_fidelityCard,
                [nameof(Pos26PaymentDialog.FiscalDrawerId)] = _fiscalDrawerSummary?.Id
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

            if (risultato.FidelityCardId.HasValue)
            {
                var shouldRefreshFidelityCard = false;

                if (risultato.PointsRedeemed > 0)
                {
                    try
                    {
                        await FidelityService.RedeemPointsAsync(
                            risultato.FidelityCardId.Value,
                            new ModifyFidelityPointsDto
                            {
                                Points = risultato.PointsRedeemed,
                                Description = "Riscatto punti POS"
                            });
                        shouldRefreshFidelityCard = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "POS2026: errore riscatto punti fedeltà.");
                    }
                }

                if (risultato.PointsEarned > 0)
                {
                    try
                    {
                        await FidelityService.AddPointsAsync(
                            risultato.FidelityCardId.Value,
                            new ModifyFidelityPointsDto
                            {
                                Points = risultato.PointsEarned,
                                Description = "Acquisto POS"
                            });
                        shouldRefreshFidelityCard = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "POS2026: errore accumulo punti fedeltà.");
                    }
                }

                if (shouldRefreshFidelityCard)
                {
                    try
                    {
                        _fidelityCard = await FidelityService.GetCardByIdAsync(risultato.FidelityCardId.Value);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "POS2026: errore refresh carta fedeltà.");
                    }
                }
            }

            // Chiudi la sessione se completamente pagata
            if (ViewModel.CurrentSession?.IsFullyPaid == true)
                await CloseSaleAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore durante la registrazione del pagamento POS2026.");
            // Ricarica la sessione per allineare la UI allo stato reale del server
            // (possibile pagamento parziale già registrato)
            try { await ViewModel.ReloadSessionAsync(); } catch { /* best effort */ }
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
            // Notification ("Sale completed successfully!") already emitted by POSViewModel.
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
            // Notification ("Session parked") already emitted by POSViewModel.
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
            // Notification ("Session cancelled") already emitted by POSViewModel.
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
                Text = _orderNoteText.Trim()
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
            var updateDto = new UpdateSaleSessionDto
            {
                SaleType = SaleTypes.Retail,
                TableId = table.Id
            };
            await SalesService.UpdateSessionAsync(ViewModel.CurrentSession!.Id, updateDto);
            await ViewModel.ReloadSessionAsync();
            AppNotification.ShowSuccess($"Tavolo {table.TableNumber} assegnato.");
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
                var label = saleType == SaleTypes.Retail ? "Banco" : "Asporto";
                AppNotification.ShowSuccess($"Tipo vendita: {label}.");
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

    // =========================================================================
    //  Menu azioni secondarie ("Altro") — Azione 2
    // =========================================================================

    private void ToggleMoreMenu() => _showMoreMenu = !_showMoreMenu;

    private async Task ToggleTablePickerFromMenu()
    {
        _showMoreMenu = false;
        _showTablePicker = !_showTablePicker;
        if (_showTablePicker)
            await LoadAvailableTablesAsync();
    }

    private async Task ToggleParkedFromMenu()
    {
        _showMoreMenu = false;
        _showParkedSessions = !_showParkedSessions;
        if (_showParkedSessions)
            await LoadParkedSessionsAsync();
    }

    private async Task OpenSplitFromMenu()
    {
        _showMoreMenu = false;
        await OpenSplitDialogAsync();
    }

    private async Task OpenMergeFromMenu()
    {
        _showMoreMenu = false;
        await OpenMergeDialogAsync();
    }

    private async Task ResumeParkedSessionAsync(SaleSessionDto session)
    {
        try
        {
            // Aggiorna lo stato della sessione sospesa a Open sul server
            var updateDto = new UpdateSaleSessionDto { Status = SaleSessionStatusDto.Open };
            var resumed = await SalesService.UpdateSessionAsync(session.Id, updateDto);
            if (resumed != null)
            {
                // ResumeSessionAsync imposta CurrentSession = resumed e aggiorna OperatorId/PosId
                await ViewModel.ResumeSessionAsync(resumed);
                RebuildCartQuantities();
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

    // =========================================================================
    //  Rimuovi ultimo articolo
    // =========================================================================

    private async Task RemoveLastItemAsync()
    {
        if (ViewModel.CurrentSession?.Items.Any() != true) return;
        try
        {
            var last = ViewModel.CurrentSession.Items.Last();
            var result = await ViewModel.RemoveItemAsync(last);
            if (!result.Success)
                AppNotification.ShowWarning(result.Error ?? "Errore durante la rimozione dell'articolo.");
            RebuildCartQuantities();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Errore rimozione ultimo articolo POS2026.");
            AppNotification.ShowWarning("Si è verificato un errore.");
        }
    }

    // =========================================================================
    //  Split / Merge sessioni
    // =========================================================================

    private bool _canSplit = false;
    private List<SaleSessionDto> _openSessions = new();

    private async Task LoadOpenSessionsAsync()
    {
        try
        {
            var all = await SalesService.GetActiveSessionsAsync();
            _openSessions = (all ?? Enumerable.Empty<SaleSessionDto>())
                .Where(s => s.Status == SaleSessionStatusDto.Open && s.ParentSessionId == null)
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "POS2026: errore caricamento sessioni aperte.");
        }
    }

    private async Task CheckCanSplitAsync()
    {
        _canSplit = ViewModel.CurrentSession != null &&
                    await SalesService.CanSplitSessionAsync(ViewModel.CurrentSession.Id);
    }

    private async Task OpenSplitDialogAsync()
    {
        if (ViewModel.CurrentSession == null) return;
        try
        {
            var parameters = new DialogParameters
            {
                { "Session", ViewModel.CurrentSession }
            };
            var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true };
            var dialog = await DialogService.ShowAsync<SplitPaymentDialog>("Dividi Conto", parameters, options);
            var result = await dialog.Result;
            if (result is { Canceled: false })
            {
                await ViewModel.ReloadSessionAsync();
                await LoadOpenSessionsAsync();
                await CheckCanSplitAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "POS2026: errore apertura dialog divisione conto.");
            AppNotification.ShowWarning("Si è verificato un errore.");
        }
    }

    private async Task OpenMergeDialogAsync()
    {
        try
        {
            var parameters = new DialogParameters
            {
                { "AvailableSessions", _openSessions }
            };
            var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true };
            var dialog = await DialogService.ShowAsync<MergeSessionsDialog>("Unisci Sessioni", parameters, options);
            var result = await dialog.Result;
            if (result is { Canceled: false })
            {
                await ViewModel.ReloadSessionAsync();
                await LoadOpenSessionsAsync();
                await CheckCanSplitAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "POS2026: errore apertura dialog unione sessioni.");
            AppNotification.ShowWarning("Si è verificato un errore.");
        }
    }

    // =========================================================================
    //  Helper UI
    // =========================================================================

    private string GetLoadingTooltipText() =>
        ViewModel.IsUpdatingItems ? "Salvataggio in corso..." : "Aggiornamento completato";

    private Color GetLoadingSpinnerColor() =>
        ViewModel.IsUpdatingItems ? Color.Warning : Color.Primary;

    /// <summary>Nome dell'operatore selezionato, risolto da <see cref="POSViewModel.AvailableOperators"/>
    /// (la stessa risoluzione che avveniva dentro il vecchio POSHeader, ora sostituito da Pos26StatusBar).</summary>
    private string? SelectedOperatorName =>
        ViewModel.AvailableOperators?.FirstOrDefault(o => o.Id == ViewModel.SelectedOperatorId)?.Name;

    /// <summary>Nome della cassa selezionata, risolto da <see cref="POSViewModel.AvailablePos"/>.</summary>
    private string? SelectedPosName =>
        ViewModel.AvailablePos?.FirstOrDefault(p => p.Id == ViewModel.SelectedPosId)?.Name;

    /// <summary>Percentuale di sconto globale corrente, calcolata da DiscountAmount/OriginalTotal
    /// (il DTO sessione non espone una percentuale diretta — vedi ApplyDiscountAsync).</summary>
    private decimal CurrentDiscountPercent
    {
        get
        {
            var subtotal = ViewModel.CurrentSession?.OriginalTotal ?? 0m;
            var discount = ViewModel.CurrentSession?.DiscountAmount ?? 0m;
            return subtotal > 0 ? Math.Round(discount / subtotal * 100, 2) : 0m;
        }
    }

    /// <summary>Rimuove lo sconto globale corrente riapplicando uno sconto dello 0% (Task 6 — Pos26DiscountZone).</summary>
    private Task RemoveDiscountAsync() => ApplyDiscountAsync(0m);

    private void OnViewModelStateChanged()
    {
        RebuildCartQuantities();
        InvokeAsync(StateHasChanged);
    }

    // =========================================================================
    //  Step-guided flow (Azione 1) — selezione operatore e cassa a schermo intero
    // =========================================================================

    /// <summary>
    /// Seleziona l'operatore dal pannello step 0.
    /// Se c'è una sola cassa disponibile, la seleziona automaticamente e avanza allo step 2.
    /// </summary>
    private async Task OnOperatorCardSelectedAsync(StoreUserDto op)
    {
        try
        {
            ViewModel.SelectedOperatorId = op.Id;
            // Auto-select cassa se unica disponibile (stesso comportamento di POSTouch)
            if (ViewModel.AvailablePos.Count == 1)
            {
                ViewModel.SelectedPosId = ViewModel.AvailablePos[0].Id;
                LoadFiscalPrinterIdFromPos(ViewModel.SelectedPosId);
                await Task.WhenAll(LoadFiscalDrawerAsync(), ConnectFiscalHubAsync());
            }
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "POS2026: errore selezione operatore nel flusso guidato.");
            AppNotification.ShowWarning("Si è verificato un errore imprevisto.");
        }
    }

    /// <summary>
    /// Seleziona la cassa dal pannello step 1 e avanza allo step 2 (interfaccia vendita).
    /// </summary>
    private async Task OnPosCardSelectedAsync(StorePosDto pos)
    {
        try
        {
            ViewModel.SelectedPosId = pos.Id;
            LoadFiscalPrinterIdFromPos(pos.Id);
            await Task.WhenAll(LoadFiscalDrawerAsync(), ConnectFiscalHubAsync());
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "POS2026: errore selezione cassa nel flusso guidato.");
            AppNotification.ShowWarning("Si è verificato un errore imprevisto.");
        }
    }

    /// <summary>Torna al passo 0 (selezione operatore) dal passo 1 (selezione cassa).</summary>
    private void DeselectOperator()
    {
        ViewModel.SelectedOperatorId = null;
        ViewModel.SelectedPosId = null;
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
                    await RemoveLastItemAsync();
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
            // Dispose any existing WebSocket connection to prevent leaks when the POS changes.
            if (_fiscalHubConnection != null)
            {
                try { await _fiscalHubConnection.DisposeAsync(); }
                catch (Exception ex) { Logger.LogWarning(ex, "POS2026: errore durante il dispose della vecchia connessione hub."); }
                _fiscalHubConnection = null;
            }

            _fiscalHubConnection = new HubConnectionBuilder()
                .WithUrl(new Uri(HttpClientFactory.CreateClient("ApiClient").BaseAddress!, "/hubs/fiscal-printer").ToString(), options =>
                {
                    options.AccessTokenProvider = () => AuthService.GetAccessTokenAsync();
                })
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

    /// <summary>
    /// Controllo mattutino: verifica via DB (nessuna comunicazione hardware) se la chiusura
    /// del giorno precedente è stata eseguita. Se mancante imposta il flag <see cref="_previousDayClosureMissing"/>
    /// per mostrare il banner di avviso nell'UI.
    /// </summary>
    private async Task CheckPreviousDayClosureAsync()
    {
        if (!_fiscalPrinterId.HasValue)
        {
            // No fiscal printer configured for this POS terminal.
            // Set the flag so the UI can surface an informational notice.
            _noPrinterConfigured = true;
            Logger.LogDebug("POS2026: nessuna stampante fiscale configurata per questo punto cassa – verifica chiusura saltata.");
            await InvokeAsync(StateHasChanged);
            return;
        }

        _noPrinterConfigured = false;
        try
        {
            var status = await FiscalPrintingService.GetPreviousDayClosureStatusAsync(_fiscalPrinterId.Value);
            if (status != null)
            {
                _previousDayClosureMissing = status.IsPreviousDayClosureMissing;
                _lastClosureDate = status.LastClosureDate;

                if (_previousDayClosureMissing)
                {
                    Logger.LogWarning(
                        "POS2026: chiusura giornaliera mancante per il {PreviousDay} | Ultima chiusura: {LastClosure} | PrinterId={PrinterId}",
                        status.PreviousBusinessDay.ToString("dd/MM/yyyy"),
                        status.LastClosureDate?.ToString("dd/MM/yyyy HH:mm") ?? "mai",
                        _fiscalPrinterId.Value);
                }
            }
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "POS2026: impossibile verificare la chiusura del giorno precedente.");
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
                ["DrawerId"] = _fiscalDrawerSummary.Id,
                ["TransactionType"] = type,
                ["CurrencyCode"] = _fiscalDrawerSummary.CurrencyCode
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

    /// <summary>
    /// Punto d'ingresso unico per la chiusura giornata (P1.2): concatena, in sequenza logica,
    /// la riconciliazione del cassetto (se una sessione è aperta) e la chiusura fiscale esistente
    /// (<see cref="DailyClosureDialog"/>, invariata). Se l'operatore annulla lo step cassetto,
    /// l'intero wizard si interrompe e la chiusura fiscale non viene eseguita.
    /// </summary>
    private async Task OpenEndOfDayWizardAsync()
    {
        try
        {
            decimal? drawerClosingBalance = null;
            decimal? drawerDifference = null;

            // Step 1 — Riconciliazione cassetto, solo se una sessione è effettivamente aperta.
            if (_fiscalDrawerSummary?.HasOpenSession == true)
            {
                var expectedBalanceBeforeClosure = _fiscalDrawerSummary.CurrentBalance;
                var drawerParameters = new DialogParameters
                {
                    [nameof(Pos26DrawerClosureDialog.FiscalDrawerId)] = _fiscalDrawerSummary.Id,
                    [nameof(Pos26DrawerClosureDialog.FiscalDrawerSummary)] = _fiscalDrawerSummary,
                    [nameof(Pos26DrawerClosureDialog.OperatorId)] = ViewModel.SelectedOperatorId
                };
                var drawerDialog = await DialogService.ShowAsync<Pos26DrawerClosureDialog>(
                    string.Empty, drawerParameters, EFDialogDefaults.Options);
                var drawerResult = await drawerDialog.Result;

                if (drawerResult == null || drawerResult.Canceled)
                {
                    // Annullato dall'utente — interrompe l'intero wizard, la chiusura fiscale non viene eseguita.
                    return;
                }

                if (drawerResult.Data is FiscalDrawerSessionDto drawerSession)
                {
                    drawerClosingBalance = drawerSession.ClosingBalance;
                    drawerDifference = drawerSession.ClosingBalance - expectedBalanceBeforeClosure;
                }

                await LoadFiscalDrawerAsync();
            }

            // Step 2 — Chiusura fiscale (Z-report o "Non Fiscale"), componente esistente invariato.
            var closureParameters = new DialogParameters
            {
                [nameof(DailyClosureDialog.PrinterId)] = _fiscalPrinterId,
                [nameof(DailyClosureDialog.PosId)] = ViewModel.SelectedPosId,
                [nameof(DailyClosureDialog.PrinterName)] = _fiscalPrinterId.HasValue ? "Stampante POS" : null
            };
            var closureDialog = await DialogService.ShowAsync<DailyClosureDialog>(
                string.Empty, closureParameters, EFDialogDefaults.Options);
            var closureResult = await closureDialog.Result;

            if (closureResult?.Canceled == false)
            {
                if (_fiscalPrinterId.HasValue)
                    _fiscalPrinterStatus = await FiscalPrintingService.GetStatusAsync(_fiscalPrinterId.Value);

                var fiscalSuccess = closureResult.Data is DailyClosureResultDto fiscalClosure && fiscalClosure.Success;
                if (fiscalSuccess)
                    _previousDayClosureMissing = false;

                // Notifica di riepilogo finale.
                if (drawerClosingBalance.HasValue)
                {
                    var diffText = drawerDifference.HasValue
                        ? $" (differenza €{drawerDifference.Value:F2})"
                        : string.Empty;
                    AppNotification.ShowSuccess(
                        $"Giornata chiusa — Cassetto: €{drawerClosingBalance.Value:F2} contati{diffText} · " +
                        (fiscalSuccess ? "Chiusura fiscale completata." : "Chiusura fiscale non completata."));
                }
                else if (fiscalSuccess)
                {
                    AppNotification.ShowSuccess("Giornata chiusa — Chiusura fiscale completata.");
                }

                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "POS2026: errore durante il wizard di chiusura giornata.");
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
        var items = session.Items.Select(item =>
        {
            var vatCode = MapVatCodeFromTaxRate(item.TaxRate);
            return new FiscalReceiptItem
            {
                Description = item.ProductName ?? item.ProductCode ?? "Articolo",
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                VatCode = vatCode,
                Department = vatCode   // departments are configured to match the VAT code in standard setups
            };
        }).ToList();

        var payments = session.Payments.Select(p => new FiscalPayment
        {
            Amount = p.Amount,
            MethodCode = 1,
            Description = p.PaymentMethodName ?? "Pagamento"
        }).ToList();

        return new FiscalReceiptData { Items = items, Payments = payments };
    }

    /// <summary>
    /// Maps an item's tax rate to the standard Italian fiscal printer VAT code (1–4).
    /// Standard assignment for Italian fiscal printers (Custom, Epson):
    ///   Code 1 → 22%  (standard rate)
    ///   Code 2 → 10%  (reduced rate — food, hospitality, etc.)
    ///   Code 3 → 5%   (intermediate rate)
    ///   Code 4 → 4%   (super-reduced — essential goods)
    ///   Code 5 → 0%   (exempt / no VAT)
    /// Merchants who have a non-standard printer configuration must update this mapping
    /// or introduce a configurable VAT-code table in the printer settings.
    /// </summary>
    private static int MapVatCodeFromTaxRate(decimal taxRate) => taxRate switch
    {
        22m => 1,
        10m => 2,
        5m => 3,
        4m => 4,
        0m => 5,
        _ => 1    // fallback: unknown rate defaults to code 1 (22%)
    };

    // =========================================================================
    //  Dispose
    // =========================================================================

    public async ValueTask DisposeAsync()
    {
        ViewModel.StateChanged -= OnViewModelStateChanged;
        ViewModel.OnNotification -= HandleNotification;
        // NON chiamare ViewModel.Dispose(): in Blazor WASM i servizi Scoped condividono
        // il lifetime dell'app. Distruggere il SemaphoreSlim causerebbe ObjectDisposedException
        // alla prossima navigazione sulla stessa pagina. Il DI container smaltirà il ViewModel
        // al termine della sessione applicativa.

        // Annulla eventuale ricerca pendente
        _searchDebounce?.Cancel();
        _searchDebounce?.Dispose();
        _searchDebounce = null;

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
