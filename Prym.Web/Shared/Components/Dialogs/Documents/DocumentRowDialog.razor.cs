using Prym.Web.Models.Documents;
using Prym.Web.Services;
using Prym.Web.Services.Common;
using Prym.Web.Services.Documents;
using Prym.DTOs.Common;
using Prym.DTOs.Documents;
using Prym.DTOs.Products;
using Prym.DTOs.UnitOfMeasures;
using Prym.DTOs.VatRates;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using static Prym.Web.Shared.Components.Dialogs.Documents.DocumentRowDialogConstants;

namespace Prym.Web.Shared.Components.Dialogs.Documents;

/// <summary>
/// Code-behind per DocumentRowDialog - Gestisce inserimento/modifica righe documento
/// </summary>
public partial class DocumentRowDialog : IAsyncDisposable
{
    #region Injected Dependencies

    [Inject] private IDocumentHeaderService DocumentHeaderService { get; set; } = null!;
    [Inject] private IProductService ProductService { get; set; } = null!;
    [Inject] private IFinancialService FinancialService { get; set; } = null!;
    [Inject] private IDocumentRowCalculationService CalculationService { get; set; } = null!;
    [Inject] private IDocumentDialogCacheService CacheService { get; set; } = null!;
    [Inject] private IPriceResolutionService PriceResolutionService { get; set; } = null!;
    [Inject] private IAppNotificationService AppNotification { get; set; } = null!;
    [Inject] private ITranslationService TranslationService { get; set; } = null!;
    [Inject] private ILogger<DocumentRowDialog> Logger { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private IDocumentRowValidator Validator { get; set; } = null!;
    [Inject] private IPromotionClientService PromotionClientService { get; set; } = null!;

    #endregion

    #region Parameters

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public Guid DocumentHeaderId { get; set; }
    [Parameter] public Guid? RowId { get; set; }

    /// <summary>
    /// Initial dialog mode applied when opening in add mode.
    /// Defaults to <see cref="DialogMode.Standard"/> so the operator sees the full form;
    /// pass <see cref="DialogMode.ContinuousScan"/> explicitly when opening via the "Scansiona" toolbar action.
    /// Ignored in edit mode (always starts in Standard).
    /// </summary>
    [Parameter] public DialogMode InitialMode { get; set; } = DialogMode.Standard;

    #endregion

    #region Component References

    private UnifiedProductSelector? _productScannerRef;
    private UnifiedProductSelector? _continuousScanRef;

    #endregion

    #region State Variables

    private DocumentRowDialogState _state = new();

    private bool _isEditMode => RowId.HasValue;
    private CreateDocumentRowDto _model => _state.Model;

    // Backward compatibility properties for .razor file
    private DialogMode _dialogMode => _state.Mode;
    private int _continuousScanCount => _state.ContinuousScan.ScanCount;
    private bool _isLoadingData => _state.Processing.IsLoadingData;
    private List<string> _validationErrors => _state.Validation.Errors;
    private bool _isProcessingContinuousScan => _state.ContinuousScan.IsProcessing;
    private bool _isProcessing => _state.Processing.IsSaving;
    private Guid? _selectedVatRateId => _state.SelectedVatRateId;
    private List<VatRateDto> _allVatRates => _state.Cache.AllVatRates;

    /// <summary>
    /// Simple variable for product autocomplete binding.
    /// ✅ PATTERN: Same as GenericDocumentProcedure BusinessParty autocomplete (line 687).
    /// ✅ CRITICAL: Simple variable, NOT a property with getter/setter.
    /// This allows Blazor's @bind-Value to work correctly without interference.
    /// </summary>
    private ProductDto? _selectedProduct = null;
    private List<ContinuousScanEntry> _recentContinuousScans => _state.ContinuousScan.RecentScans;
    private Guid? _selectedUnitOfMeasureId => _state.SelectedUnitOfMeasureId;
    private List<ProductUnitDto> _availableUnits => _state.Cache.AvailableUnits;
    private List<UMDto> _allUnitsOfMeasure => _state.Cache.AllUnitsOfMeasure;
    private List<RecentProductTransactionDto> _recentTransactions => _state.Cache.RecentTransactions;
    private bool _loadingTransactions => _state.Processing.IsLoadingTransactions;

    // Cached calculation result to avoid redundant calculations
    private Prym.Web.Models.Documents.DocumentRowCalculationResult? _cachedCalculationResult = null;
    private string _cachedCalculationKey = string.Empty;

    // Price list metadata - PriceResolutionService integration
    private string? _appliedPriceListName;
    private string? _appliedPromotionsSummary;

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// Initializes the dialog component by loading required data in parallel
    /// </summary>
    /// <remarks>
    /// Loads document header, units of measure, VAT rates, and panel states.
    /// In edit mode, also loads the existing row data.
    /// Performance: ~200ms average (3x faster than sequential loading)
    /// </remarks>
    protected override async Task OnInitializedAsync()
    {
        _state.Processing.IsLoadingData = true;
        StateHasChanged();

        try
        {
            // Load data in parallel for faster initialization
            await Task.WhenAll(
                LoadDocumentHeaderAsync(),
                LoadUnitsOfMeasureAsync(),
                LoadVatRatesAsync(),
                PreloadPriceListsCacheAsync() // ✅ NUOVO: Preload cache listini
            );

            // Add edit mode task if applicable
            if (_isEditMode && RowId.HasValue)
            {
                await LoadRowForEdit(RowId.Value);
            }

            // Apply initial dialog mode. In edit mode always stay in Standard;
            // in add mode use the caller-supplied InitialMode (default: Standard).
            if (!_isEditMode)
            {
                SetDialogMode(InitialMode);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing DocumentRowDialog.");
            AppNotification.ShowError(TranslationService.GetTranslation("documents.errorInitializing", "Errore durante l'inizializzazione del dialogo."));
        }
        finally
        {
            _state.Processing.IsLoadingData = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Updates the model when parameters change
    /// </summary>
    /// <remarks>
    /// Align with Business Party autocomplete: do not react to product changes here.
    /// Selection changes are handled by OnProductSelected and programmatic flows (barcode, dialogs).
    /// </remarks>
    protected override void OnParametersSet()
    {
        // CRITICAL FIX: Only update if DocumentHeaderId actually changed
        // This prevents unnecessary rerenders that reset MudAutocomplete while user is typing
        if (_state.Model.DocumentHeaderId != DocumentHeaderId)
        {
            Logger.LogDebug("DocumentHeaderId changed from {Old} to {New}",
                _state.Model.DocumentHeaderId, DocumentHeaderId);
            _state.Model.DocumentHeaderId = DocumentHeaderId;
        }
        // DO NOT touch _selectedProduct or other state during OnParametersSet
    }

    /// <summary>
    /// Called AFTER _selectedProduct is bound by Blazor.
    /// Pattern: Same as GenericDocumentProcedure but with product-specific field population.
    /// CRITICAL: This runs AFTER the binding is complete, so it doesn't interfere with typing.
    /// </summary>
    private async Task OnProductSelectedAsync(ProductDto? product)
    {
        Logger.LogDebug("OnProductSelectedAsync called. Product: {ProductId} - {ProductName}",
            product?.Id, product?.Name ?? "NULL");

        try
        {
            // Update local variable to match the parameter
            _selectedProduct = product;

            if (_selectedProduct != null)
            {
                // Sync to state for other components that need it
                _state.SelectedProduct = _selectedProduct;
                _state.PreviousSelectedProduct = _selectedProduct;

                Logger.LogInformation("Populating fields for product {ProductId}", _selectedProduct.Id);

                // Populate all related fields from the selected product
                await PopulateFromProductAsync(_selectedProduct);

                // ✅ Force UI update after population
                await InvokeAsync(StateHasChanged);

                Logger.LogInformation("Product fields populated. UnitOfMeasureId: {UnitId}, VatRateId: {VatId}",
                    _state.SelectedUnitOfMeasureId, _state.SelectedVatRateId);
            }
            else
            {
                Logger.LogInformation("Product cleared");

                // Product was cleared
                _state.SelectedProduct = null;
                _state.PreviousSelectedProduct = null;
                ClearProductFields();

                await InvokeAsync(StateHasChanged);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in OnProductSelectedAsync for product {ProductId}.", product?.Id);
            AppNotification.ShowError(TranslationService.GetTranslation("documents.errorSelectingProduct", "Errore durante la selezione del prodotto."));
        }
    }

    /// <summary>
    /// Sets focus on the barcode field after first render in create mode
    /// </summary>
    /// <remarks>
    /// Only executes on first render and when not in edit mode to improve UX.
    /// Also registers keyboard shortcuts on first render.
    /// </remarks>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            try
            {
                // In ContinuousScan mode, ensure the scanner is focused.
                // Standard mode: UnifiedProductSelector handles its own autofocus via AutoFocus="!_isEditMode".
                if (!_isEditMode && _dialogMode == DialogMode.ContinuousScan && _continuousScanRef != null)
                {
                    await Task.Delay(100);
                    await _continuousScanRef.FocusAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during first render initialization in DocumentRowDialog.");
            }
        }
    }

    #endregion

    #region Error Handling Utility

    /// <summary>
    /// Executes an async operation with standardized error handling
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operation">The async operation to execute</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <param name="successMessageKey">Translation key for success message (optional)</param>
    /// <param name="showErrorToUser">Whether to show error to user via Snackbar</param>
    /// <returns>Result of the operation or default(T) on error</returns>
    private async Task<T?> ExecuteWithErrorHandlingAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        string? successMessageKey = null,
        bool showErrorToUser = true)
    {
        try
        {
            var result = await operation();

            if (successMessageKey != null)
            {
                AppNotification.ShowSuccess(TranslationService.GetTranslation(successMessageKey, "Operazione completata"));
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "HTTP error during {Operation}", operationName);

            if (showErrorToUser)
            {
                AppNotification.ShowError(TranslationService.GetTranslation(
                        "error.networkError",
                        "Errore di connessione. Verifica la connessione di rete."));
            }
        }
        catch (TaskCanceledException ex)
        {
            Logger.LogWarning(ex, "Operation {Operation} was cancelled", operationName);

            if (showErrorToUser)
            {
                AppNotification.ShowWarning(TranslationService.GetTranslation(
                        "error.operationCancelled",
                        "Operazione annullata"));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during {Operation}", operationName);

            if (showErrorToUser)
            {
                AppNotification.ShowError(TranslationService.GetTranslation(
                        $"error.{operationName}",
                        $"Errore durante {operationName}"));
            }
        }

        return default;
    }

    #endregion

    #region Data Loading Methods

    /// <summary>
    /// Loads the document header information
    /// </summary>
    /// <remarks>
    /// Loads without rows to improve performance. Row data is loaded separately if needed.
    /// </remarks>
    private async Task LoadDocumentHeaderAsync()
    {
        _state.DocumentHeader = await ExecuteWithErrorHandlingAsync(
            () => DocumentHeaderService.GetDocumentHeaderByIdAsync(DocumentHeaderId, includeRows: false),
            operationName: "loadDocumentHeader",
            showErrorToUser: true);
    }

    /// <summary>
    /// Loads all available units of measure from cache
    /// </summary>
    /// <remarks>
    /// Uses cache service instead of direct API call for improved performance.
    /// </remarks>
    private async Task LoadUnitsOfMeasureAsync()
    {
        _state.Cache.AllUnitsOfMeasure = await ExecuteWithErrorHandlingAsync(
            () => CacheService.GetUnitsOfMeasureAsync(),
            operationName: "loadUnitsOfMeasure",
            showErrorToUser: true) ?? new List<UMDto>();
    }

    /// <summary>
    /// Loads all active VAT rates from cache
    /// </summary>
    /// <remarks>
    /// Uses cache service instead of direct API call for improved performance.
    /// </remarks>
    private async Task LoadVatRatesAsync()
    {
        _state.Cache.AllVatRates = await ExecuteWithErrorHandlingAsync(
            () => CacheService.GetVatRatesAsync(),
            operationName: "loadVatRates",
            showErrorToUser: true) ?? new List<VatRateDto>();
    }

    /// <summary>
    /// Precarica i listini nel cache in background per performance.
    /// Carica solo i listini rilevanti per il tipo di documento (vendita o acquisto).
    /// </summary>
    private async Task PreloadPriceListsCacheAsync()
    {
        try
        {
            // Use the server-side boolean flag instead of fragile name-based text matching.
            // Same logic used in CalculateProductPriceAsync.
            var isStockIncrease = _state.DocumentHeader?.IsDocumentTypeStockIncrease ?? false;

            if (isStockIncrease)
            {
                await CacheService.GetActivePurchasePriceListsAsync();
                Logger.LogDebug("Price lists cache preloaded: Input (purchase) direction");
            }
            else
            {
                await CacheService.GetActiveSalesPriceListsAsync();
                Logger.LogDebug("Price lists cache preloaded: Output (sales) direction");
            }
        }
        catch (Exception ex)
        {
            // Non bloccare l'init del dialog se preload fallisce
            Logger.LogWarning(ex, "Failed to preload price lists cache (non-blocking)");
        }
    }

    /// <summary>
    /// Loads recent product transactions for pricing suggestions
    /// </summary>
    /// <param name="productId">The product ID to load transactions for</param>
    /// <remarks>
    /// Determines transaction type based on document type keywords.
    /// Loads top 3 transactions with optional party filtering.
    /// </remarks>
    private async Task LoadRecentTransactions(Guid productId)
    {
        if (_state.DocumentHeader == null)
        {
            return;
        }

        _state.Processing.IsLoadingTransactions = true;
        _state.Cache.RecentTransactions.Clear();

        try
        {
            string transactionType = DetermineTransactionType();
            Guid? partyId = GetBusinessPartyId();

            var transactions = await ProductService.GetRecentProductTransactionsAsync(
                productId,
                transactionType,
                partyId,
                top: 3
            );

            if (transactions != null)
            {
                _state.Cache.RecentTransactions = transactions.ToList();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading recent transactions for product {ProductId}", productId);
        }
        finally
        {
            _state.Processing.IsLoadingTransactions = false;
        }
    }

    private string DetermineTransactionType()
    {
        string transactionType = "purchase";

        if (!string.IsNullOrEmpty(_state.DocumentHeader!.DocumentTypeName))
        {
            var lowerName = _state.DocumentHeader.DocumentTypeName.ToLower();

            if (DocumentTypeKeywords.Sale.Any(k => lowerName.Contains(k)))
            {
                transactionType = "sale";
            }
            else if (DocumentTypeKeywords.Purchase.Any(k => lowerName.Contains(k)))
            {
                transactionType = "purchase";
            }
        }

        return transactionType;
    }

    private Guid? GetBusinessPartyId()
    {
        return _state.DocumentHeader!.BusinessPartyId != Guid.Empty
            ? _state.DocumentHeader.BusinessPartyId
            : null;
    }

    /// <summary>
    /// Handles recent price application with visual feedback.
    /// </summary>
    private async Task HandleRecentPriceAppliedWithFeedback(decimal price)
    {
        try
        {
            _model.UnitPrice = price;
            InvalidateCalculationCache();
            AppNotification.ShowSuccess(TranslationService.GetTranslation("documents.priceApplied", "Prezzo applicato"));
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error applying recent price {Price} in DocumentRowDialog.", price);
            AppNotification.ShowError(TranslationService.GetTranslation("documents.errorApplyingPrice", "Errore durante l'applicazione del prezzo."));
        }
    }

    /// <summary>
    /// Loads an existing document row for editing
    /// </summary>
    private async Task LoadRowForEdit(Guid rowId)
    {
        try
        {
            var document = await DocumentHeaderService.GetDocumentHeaderByIdAsync(
                DocumentHeaderId, includeRows: true);
            var row = document?.Rows?.FirstOrDefault(r => r.Id == rowId);

            if (row != null)
            {
                await PopulateModelFromRow(row);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading row {RowId} for edit", rowId);
            AppNotification.ShowError(TranslationService.GetTranslation("documents.errorLoadingRow",
                    "Errore nel caricamento della riga"));
        }
    }

    /// <summary>
    /// Populates the form model from an existing document row
    /// </summary>
    /// <param name="row">The document row to populate from</param>
    /// <remarks>
    /// Loads all row data including product information, units, and pricing.
    /// </remarks>
    private async Task PopulateModelFromRow(DocumentRowDto row)
    {
        _state.Model.ProductCode = row.ProductCode;
        _state.Model.Description = row.Description;
        _state.Model.Quantity = row.Quantity;
        _state.Model.UnitPrice = row.UnitPrice;
        _state.Model.UnitOfMeasure = row.UnitOfMeasure;
        _state.Model.UnitOfMeasureId = row.UnitOfMeasureId;
        _state.Model.Notes = row.Notes;
        _state.Model.VatRate = row.VatRate;
        _state.Model.VatDescription = row.VatDescription;
        _state.SelectedUnitOfMeasureId = row.UnitOfMeasureId;

        if (_state.Model.VatRate > 0 || !string.IsNullOrEmpty(_state.Model.VatDescription))
        {
            var vatRate = _state.Cache.AllVatRates.FirstOrDefault(v =>
                v.Percentage == _state.Model.VatRate &&
                (string.IsNullOrEmpty(_state.Model.VatDescription) || v.Name == _state.Model.VatDescription));
            if (vatRate != null)
            {
                _state.SelectedVatRateId = vatRate.Id;
            }
        }

        if (row.ProductId.HasValue)
        {
            var product = await ProductService.GetProductByIdAsync(row.ProductId.Value);
            if (product != null)
            {
                _state.SelectedProduct = product;
                var units = await ProductService.GetProductUnitsAsync(product.Id);
                _state.Cache.AvailableUnits = units?.ToList() ?? new List<ProductUnitDto>();
            }
        }

        StateHasChanged();
    }

    #endregion

    #region Barcode Handling

    /// <summary>
    /// Handles keyboard shortcuts for the dialog (Ctrl+E to edit selected product).
    /// </summary>
    private async Task OnDialogKeyDown(KeyboardEventArgs e)
    {
        try
        {
            if (e.CtrlKey && e.Key == "e")
            {
                if (_state.SelectedProduct != null && _productScannerRef != null)
                    await _productScannerRef.TriggerEditAsync();
                else if (_state.SelectedProduct == null)
                    AppNotification.ShowWarning(TranslationService.GetTranslation("products.noProductSelected", "Nessun prodotto selezionato"));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling keyboard shortcut '{Key}' in DocumentRowDialog.", e.Key);
        }
    }

    /// <summary>
    /// Cerca un prodotto tramite barcode
    /// </summary>
    private async Task SearchByBarcode(string barcode)
    {
        if (_state.Processing.IsProcessingBarcode) return;

        try
        {
            _state.Processing.IsProcessingBarcode = true;
            _state.Barcode.ScannedBarcode = barcode;

            var productWithCode = await ProductService.GetProductWithCodeByCodeAsync(barcode);
            if (productWithCode?.Product != null)
            {
                _state.Barcode.ProductUnitId = productWithCode.Code?.ProductUnitId;

                // Set product and populate fields
                await SelectProductAndPopulateAsync(productWithCode.Product);

                _state.Barcode.Input = string.Empty;
                _state.Barcode.ScannedBarcode = string.Empty;

                AppNotification.ShowSuccess(TranslationService.GetTranslation("warehouse.productFound", "Prodotto trovato"));
            }
            else
            {
                await ShowProductNotFoundDialog(barcode);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error searching product by barcode {Barcode}", barcode);
            AppNotification.ShowError($"Errore: {ex.Message}");
        }
        finally
        {
            _state.Processing.IsProcessingBarcode = false;
        }
    }

    /// <summary>
    /// Mostra il dialog per prodotto non trovato
    /// </summary>
    private async Task ShowProductNotFoundDialog(string code)
    {
        var parameters = new DialogParameters
        {
            { nameof(ProductNotFoundDialog.Barcode), code },
            { nameof(ProductNotFoundDialog.IsInventoryContext), false }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseOnEscapeKey = false
        };

        var dialog = await DialogService.ShowAsync<ProductNotFoundDialog>(
            TranslationService.GetTranslation("warehouse.productNotFound", "Prodotto non trovato"),
            parameters,
            options);

        var result = await dialog.Result;

        if (result is { Canceled: false } && result.Data != null)
        {
            await HandleProductNotFoundResult(result.Data);
        }
        else
        {
            _state.Barcode.Input = string.Empty;
            _state.Barcode.ScannedBarcode = string.Empty;
        }
    }

    /// <summary>
    /// Gestisce il risultato del dialog prodotto non trovato
    /// </summary>
    private async Task HandleProductNotFoundResult(object data)
    {
        try
        {
            if (data is ProductDto createdProduct)
            {
                await SelectProductAndPopulateAsync(createdProduct);
                AppNotification.ShowSuccess(TranslationService.GetTranslation("warehouse.productCreatedAndSelected",
                        "Prodotto creato e selezionato"));
            }
            else if (data is ProductNotFoundDialog.AssignResult assignResult && assignResult.Product != null)
            {
                await SelectProductAndPopulateAsync(assignResult.Product);
                AppNotification.ShowSuccess(TranslationService.GetTranslation("warehouse.codeAssignedAndProductSelected",
                        "Codice assegnato e prodotto selezionato"));
            }
        }
        finally
        {
            _state.Barcode.Input = string.Empty;
            _state.Barcode.ScannedBarcode = string.Empty;
        }
    }

    #endregion

    #region Product Selection & Search

    /// <summary>
    /// Populates form fields from selected product data
    /// </summary>
    /// <param name="product">The product to populate from</param>
    /// <remarks>
    /// ✅ PATTERN: Simplified version based on problem statement requirements.
    /// CRITICAL CHANGES:
    /// <list type="bullet">
    /// <item>Does NOT modify _selectedProduct (that's handled by Blazor binding)</item>
    /// <item>Calls StateHasChanged() ONLY at the end</item>
    /// <item>No recursive calls or complex async patterns</item>
    /// </list>
    /// <para>Key operations:</para>
    /// <list type="number">
    /// <item>Populates base product fields (ID, code, description)</item>
    /// <item>Handles price calculation with VAT</item>
    /// <item>Loads and configures product units</item>
    /// <item>Invalidates cached calculations</item>
    /// <item>Loads recent transaction history</item>
    /// <item>Auto-focuses quantity field for quick data entry</item>
    /// </list>
    /// </remarks>
    private async Task PopulateFromProductAsync(ProductDto product)
    {
        try
        {
            Logger.LogInformation("Populating fields from product: {ProductId} - {ProductName}",
                product.Id, product.Name);

            // 1. Populate base fields
            _state.Model.ProductId = product.Id;
            _state.Model.ProductCode = product.Code;
            _state.Model.Description = product.Name;

            // 2. Populate price and VAT using PriceResolutionService
            var (productPrice, vatRate) = await CalculateProductPriceAsync(product);

            // 3. Load product units
            var units = await ProductService.GetProductUnitsAsync(product.Id);
            _state.Cache.AvailableUnits = units?.ToList() ?? new List<ProductUnitDto>();

            if (_state.Cache.AvailableUnits.Any())
            {
                var defaultUnit = _state.Cache.AvailableUnits.FirstOrDefault(u => u.UnitType == "Base")
                               ?? _state.Cache.AvailableUnits.FirstOrDefault();

                if (defaultUnit != null)
                {
                    _state.SelectedUnitOfMeasureId = defaultUnit.UnitOfMeasureId;
                    _state.Model.UnitOfMeasureId = defaultUnit.UnitOfMeasureId;
                    UpdateModelUnitOfMeasure(_state.SelectedUnitOfMeasureId);
                }
            }
            else if (product.UnitOfMeasureId.HasValue)
            {
                _state.SelectedUnitOfMeasureId = product.UnitOfMeasureId;
                _state.Model.UnitOfMeasureId = product.UnitOfMeasureId;

                var um = _state.Cache.AllUnitsOfMeasure.FirstOrDefault(u => u.Id == product.UnitOfMeasureId.Value);
                if (um != null)
                {
                    _state.Model.UnitOfMeasure = um.Symbol;
                }
            }

            // 4. Set final price
            _state.Model.UnitPrice = productPrice;

            // 5. Invalidate cached calculation
            _cachedCalculationResult = null;
            _cachedCalculationKey = string.Empty;

            // 6. Load recent transactions
            await LoadRecentTransactions(product.Id);

            // 7. Force UI update

            // ✅ Force UI update
            await InvokeAsync(StateHasChanged);

            Logger.LogInformation("Product fields populated successfully. UnitOfMeasureId: {UnitId}, Price: {Price}, VatRate: {VatRate}%",
                _state.Model.UnitOfMeasureId, _state.Model.UnitPrice, _state.Model.VatRate);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error populating from product {ProductId}", product.Id);
            AppNotification.ShowError(TranslationService.GetTranslation("error.loadProductData",
                    "Errore caricamento dati prodotto"));

            // ✅ Ensure UI update even on error
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task<(decimal price, decimal vatRate)> CalculateProductPriceAsync(ProductDto product)
    {
        decimal vatRate = 0m;

        try
        {
            // ── 1. Respect PriceApplicationModeOverride from the document header ──────
            var priceMode = _state.DocumentHeader?.PriceApplicationModeOverride;

            if (priceMode == PriceApplicationMode.Manual)
            {
                // Manual mode: use product DefaultPrice without calling the price resolution service
                Logger.LogDebug("PriceApplicationMode=Manual: skipping price resolution for product {ProductId}", product.Id);

                var (manualPrice, manualVat) = ResolveVatForProduct(product, product.DefaultPrice ?? 0m);
                _state.Model.IsPriceManual = true;
                _state.Model.AppliedPriceListId = null;
                _state.Model.OriginalPriceFromPriceList = null;
                return (manualPrice, manualVat);
            }

            // ── 2. Determine direction via IsDocumentTypeStockIncrease ────────────────
            // Uses the server-side boolean flag instead of fragile name-based text matching
            var direction = (_state.DocumentHeader?.IsDocumentTypeStockIncrease ?? false)
                ? PriceListDirection.Input
                : PriceListDirection.Output;

            Logger.LogDebug(
                "Price direction: {Direction} (IsStockIncrease={IsStockIncrease}, DocumentType='{TypeName}')",
                direction,
                _state.DocumentHeader?.IsDocumentTypeStockIncrease,
                _state.DocumentHeader?.DocumentTypeName);

            // ── 3. Determine forced price list ────────────────────────────────────────
            Guid? forcedPriceListId = null;
            if (priceMode == PriceApplicationMode.ForcedPriceList || priceMode == PriceApplicationMode.HybridForcedWithOverrides)
            {
                forcedPriceListId = _state.DocumentHeader?.PriceListId;
                Logger.LogDebug("PriceApplicationMode={Mode}: forcing price list {PriceListId}", priceMode, forcedPriceListId);
            }

            // ── 4. Call PriceResolutionService ────────────────────────────────────────
            var priceResult = await PriceResolutionService.ResolvePriceAsync(
                productId: product.Id,
                documentHeaderId: DocumentHeaderId,
                businessPartyId: _state.DocumentHeader?.BusinessPartyId,
                forcedPriceListId: forcedPriceListId,
                direction: direction,
                quantity: _model.Quantity
            );

            // ── 5. Populate row metadata ──────────────────────────────────────────────
            _state.Model.AppliedPriceListId = priceResult.AppliedPriceListId;
            _state.Model.OriginalPriceFromPriceList = priceResult.OriginalPrice;
            _state.Model.IsPriceManual = false;

            if (priceResult.IsPriceFromList)
            {
                _appliedPriceListName = priceResult.PriceListName;
                Logger.LogInformation(
                    "Price resolved from price list '{PriceListName}' (ID={PriceListId}), Price={Price}, Source={Source}",
                    priceResult.PriceListName, priceResult.AppliedPriceListId, priceResult.Price, priceResult.Source);
            }
            else
            {
                _appliedPriceListName = null;
                Logger.LogInformation("Price resolved from default: {Price}, Source={Source}", priceResult.Price, priceResult.Source);
            }

            decimal productPrice = priceResult.Price;

            // ── 6. Resolve VAT rate ───────────────────────────────────────────────────
            (productPrice, vatRate) = ResolveVatForProduct(product, productPrice);

            // ── 7. Apply active promotions ────────────────────────────────────────────
            productPrice = await ApplyPromotionsToRowAsync(product, productPrice, vatRate);

            return (productPrice, vatRate);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calculating price for product {ProductId}, falling back to DefaultPrice", product.Id);

            // Fallback: use product default price
            decimal productPrice = product.DefaultPrice ?? 0m;
            (productPrice, vatRate) = ResolveVatForProduct(product, productPrice);
            return (productPrice, vatRate);
        }
    }

    /// <summary>
    /// Resolves the VAT rate for the product and strips VAT from the price if the product price is VAT-inclusive.
    /// </summary>
    private (decimal price, decimal vatRate) ResolveVatForProduct(ProductDto product, decimal price)
    {
        decimal vatRate = 0m;

        if (product.VatRateId.HasValue)
        {
            _state.SelectedVatRateId = product.VatRateId;
            var vatRateDto = _state.Cache.AllVatRates.FirstOrDefault(v => v.Id == product.VatRateId.Value);
            if (vatRateDto != null)
            {
                vatRate = vatRateDto.Percentage;
                _state.Model.VatRate = vatRate;
                _state.Model.VatDescription = vatRateDto.Name;
            }
        }

        if (product.IsVatIncluded && vatRate > 0)
            price = price / (1m + vatRate / 100m);

        return (price, vatRate);
    }

    /// <summary>
    /// Applies active promotion rules to the current document row.
    /// Updates <see cref="DocumentRowCalculationModels.DocumentRowState.Model"/>.AppliedPromotionsJSON
    /// and returns the net unit price after the promotional discount.
    /// On any failure (e.g. service unreachable) the original price is returned without interrupting the flow.
    /// </summary>
    private async Task<decimal> ApplyPromotionsToRowAsync(ProductDto product, decimal netPrice, decimal vatRate)
    {
        try
        {
            // Build the payload with a single item representing the current row
            var applyDto = new Prym.DTOs.Promotions.ApplyPromotionRulesDto
            {
                CartItems = new List<Prym.DTOs.Promotions.CartItemDto>
                {
                    new()
                    {
                        ProductId = product.Id,
                        ProductCode = product.Code,
                        ProductName = product.Name ?? string.Empty,
                        UnitPrice = netPrice,
                        Quantity = (int)Math.Ceiling(_model.Quantity),
                        CategoryIds = product.CategoryNodeId.HasValue
                            ? new List<Guid> { product.CategoryNodeId.Value }
                            : null,
                        ExistingLineDiscount = _state.Model.LineDiscount
                    }
                },
                CustomerId = _state.DocumentHeader?.BusinessPartyId,
                OrderDateTime = DateTime.UtcNow,
                Currency = "EUR"
            };

            var result = await PromotionClientService.ApplyPromotionsAsync(applyDto);

            if (result?.Success == true && result.CartItems.Count > 0)
            {
                var itemResult = result.CartItems[0];

                if (itemResult.AppliedPromotions.Count > 0)
                {
                    // Serialize applied promotions as JSON for persistence
                    _state.Model.AppliedPromotionsJSON = System.Text.Json.JsonSerializer.Serialize(
                        itemResult.AppliedPromotions.Select(ap => new
                        {
                            ap.PromotionId,
                            ap.PromotionName,
                            ap.RuleType,
                            ap.DiscountAmount,
                            ap.DiscountPercentage,
                            ap.Description
                        }));

                    // Apply the promotion discount only when no manual discount is already set
                    if (itemResult.EffectiveDiscountPercentage > 0 && _state.Model.LineDiscount == 0m)
                    {
                        _state.Model.LineDiscount = itemResult.EffectiveDiscountPercentage;
                        _state.Model.DiscountType = Prym.DTOs.Common.DiscountType.Percentage;
                    }

                    _appliedPromotionsSummary = string.Join(", ", itemResult.AppliedPromotions.Select(ap => ap.PromotionName));

                    AppNotification.ShowSuccess(
                        $"🏷️ {TranslationService.GetTranslation("documents.promotionsApplied", "Promozioni applicate")}: {_appliedPromotionsSummary}");

                    Logger.LogInformation(
                        "Promotions applied to product {ProductId}: [{Promotions}], EffectiveDiscount={Discount}%",
                        product.Id, _appliedPromotionsSummary, itemResult.EffectiveDiscountPercentage);

                    // Return the final unit price (with promotion discount already factored into FinalLineTotal)
                    return itemResult.OriginalLineTotal > 0
                        ? itemResult.FinalLineTotal / (decimal)Math.Ceiling(_model.Quantity)
                        : netPrice;
                }
                else
                {
                    _state.Model.AppliedPromotionsJSON = null;
                    _appliedPromotionsSummary = null;
                }
            }

            return netPrice;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not apply promotions for product {ProductId}; proceeding without promotion discount", product.Id);
            _state.Model.AppliedPromotionsJSON = null;
            _appliedPromotionsSummary = null;
            return netPrice;
        }
    }


    private void InvalidateCalculationCache()
    {
        _cachedCalculationResult = null;
        _cachedCalculationKey = string.Empty;
    }

    /// <summary>
    /// Clears all product-dependent fields
    /// </summary>
    private void ClearProductFields()
    {
        _state.Model.ProductId = null;
        _state.Model.ProductCode = string.Empty;
        _state.Model.Description = string.Empty;
        _state.Model.UnitPrice = 0m;
        _state.SelectedUnitOfMeasureId = null;
        _state.Model.UnitOfMeasureId = null;
        _state.Model.UnitOfMeasure = string.Empty;
        _state.Cache.AvailableUnits.Clear();
        _state.Cache.RecentTransactions.Clear();
    }

    /// <summary>
    /// Helper method to set a product and populate all related fields.
    /// Used for programmatic selections (barcode, dialogs, edit mode).
    /// </summary>
    private async Task SelectProductAndPopulateAsync(ProductDto product)
    {
        _selectedProduct = product;
        _state.SelectedProduct = product;
        _state.PreviousSelectedProduct = product;
        await PopulateFromProductAsync(product);
    }

    #endregion

    #region Unit & VAT Selection

    /// <summary>
    /// Gestisce il cambio dell'unità di misura
    /// </summary>
    private void OnUnitOfMeasureChanged(Guid? unitOfMeasureId)
    {
        _state.SelectedUnitOfMeasureId = unitOfMeasureId;
        UpdateModelUnitOfMeasure(unitOfMeasureId);
    }

    /// <summary>
    /// Gestisce il cambio dell'aliquota IVA
    /// </summary>
    private void OnVatRateChanged(Guid? vatRateId)
    {
        _state.SelectedVatRateId = vatRateId;
        if (vatRateId.HasValue)
        {
            var vatRate = _state.Cache.AllVatRates.FirstOrDefault(v => v.Id == vatRateId.Value);
            if (vatRate != null)
            {
                _state.Model.VatRate = vatRate.Percentage;
                _state.Model.VatDescription = vatRate.Name;
            }
        }
        else
        {
            _state.Model.VatRate = 0;
            _state.Model.VatDescription = null;
        }

        // Invalidate cached calculation result
        InvalidateCalculationCache();

        StateHasChanged();
    }

    /// <summary>
    /// Aggiorna l'unità di misura nel model
    /// </summary>
    private void UpdateModelUnitOfMeasure(Guid? unitOfMeasureId)
    {
        if (unitOfMeasureId.HasValue)
        {
            var selectedUom = _state.Cache.AllUnitsOfMeasure.FirstOrDefault(u => u.Id == unitOfMeasureId);
            if (selectedUom != null)
            {
                _state.Model.UnitOfMeasure = selectedUom.Symbol;
                _state.Model.UnitOfMeasureId = selectedUom.Id;
            }
        }
        else
        {
            _state.Model.UnitOfMeasure = null;
            _state.Model.UnitOfMeasureId = null;
        }
    }

    #endregion

    #region Calculation Methods

    /// <summary>
    /// Generates a unique key for caching based on current values.
    /// ✅ OPTIMIZATION: Key-based caching automatically detects value changes
    /// without needing explicit invalidation handlers for each field.
    /// </summary>
    private string GetCalculationCacheKey()
    {
        return $"{_state.Model.Quantity}|{_state.Model.UnitPrice}|{_state.Model.VatRate}|{_state.Model.LineDiscount}|{_state.Model.LineDiscountValue}|{_state.Model.DiscountType}";
    }

    /// <summary>
    /// Gets calculation results from centralized service with caching.
    /// ✅ OPTIMIZATION: Caches calculation results to avoid redundant calculations
    /// during UI rendering. Cache is automatically invalidated when any input value changes.
    /// </summary>
    private Prym.Web.Models.Documents.DocumentRowCalculationResult GetCalculationResult()
    {
        var currentKey = GetCalculationCacheKey();

        // Use cached result if key matches (check string equality first as it's cheaper than null check in common case)
        if (_cachedCalculationKey == currentKey && _cachedCalculationResult != null)
        {
            return _cachedCalculationResult;
        }

        // Calculate and cache the result
        var input = new Prym.Web.Models.Documents.DocumentRowCalculationInput
        {
            Quantity = _state.Model.Quantity,
            UnitPrice = _state.Model.UnitPrice,
            VatRate = _state.Model.VatRate,
            DiscountPercentage = _state.Model.LineDiscount,
            DiscountValue = _state.Model.LineDiscountValue,
            DiscountType = _state.Model.DiscountType
        };

        _cachedCalculationResult = CalculationService.CalculateRowTotals(input);
        _cachedCalculationKey = currentKey;

        return _cachedCalculationResult;
    }

    private bool IsProductVatIncluded => _state.SelectedProduct?.IsVatIncluded ?? false;

    private decimal GetSubtotal() => GetCalculationResult().NetAmount;
    private decimal GetVatAmount() => GetCalculationResult().VatAmount;
    private decimal GetLineTotal() => GetCalculationResult().TotalAmount;
    private decimal GetTotalDiscount() => GetCalculationResult().DiscountAmount;

    #endregion

    #region Discount Mutual Exclusion Logic

    /// <summary>
    /// Gestisce il cambio del valore dello sconto percentuale.
    /// Azzera lo sconto in importo quando viene valorizzato lo sconto percentuale.
    /// </summary>
    private void OnDiscountPercentChanged(decimal value)
    {
        _model.LineDiscount = value;

        if (value > 0)
        {
            _model.LineDiscountValue = 0m;
            _model.DiscountType = DiscountType.Percentage;
        }
        else
        {
            _model.DiscountType = DiscountType.Percentage;
        }

        StateHasChanged();
    }

    /// <summary>
    /// Gestisce il cambio del valore dello sconto in importo.
    /// Azzera lo sconto percentuale quando viene valorizzato lo sconto in importo.
    /// </summary>
    private void OnDiscountAmountChanged(decimal value)
    {
        _model.LineDiscountValue = value;

        if (value > 0)
        {
            _model.LineDiscount = 0m;
            _model.DiscountType = DiscountType.Value;
        }
        else
        {
            _model.DiscountType = DiscountType.Percentage;
        }

        StateHasChanged();
    }

    /// <summary>
    /// Valida che gli sconti siano mutualmente esclusivi
    /// </summary>
    private bool ValidateDiscounts()
    {
        // Non possono essere valorizzati entrambi
        if (_model.LineDiscount > 0 && _model.LineDiscountValue > 0)
        {
            _state.Validation.Errors.Add(
                TranslationService.GetTranslation(
                    "documents.exclusiveDiscountsError",
                    "Non è possibile applicare contemporaneamente sconto percentuale e in importo"
                )
            );
            return false;
        }

        // Lo sconto in importo non può superare il subtotale
        if (_model.LineDiscountValue > 0)
        {
            decimal subtotal = _model.Quantity * _model.UnitPrice;
            if (_model.LineDiscountValue > subtotal)
            {
                _state.Validation.Errors.Add(
                    TranslationService.GetTranslation(
                        "documents.discountExceedsTotal",
                        "Lo sconto in importo non può superare il totale della riga"
                    )
                );
                return false;
            }
        }

        return true;
    }

    #endregion

    #region Save & Validation

    /// <summary>
    /// Valida il form
    /// </summary>
    private bool IsValid()
    {
        _state.Validation.Errors.Clear();

        var basicValidation = !string.IsNullOrWhiteSpace(_state.Model.Description) && _state.Model.Quantity > 0;
        var discountsValidation = ValidateDiscounts();

        return basicValidation && discountsValidation;
    }

    /// <summary>
    /// Salva la riga e continua
    /// </summary>
    private async Task SaveAndContinue()
    {
        if (_state.Processing.IsSaving)
            return;

        _state.Processing.IsSaving = true;
        try
        {
            // Validate using validator service
            var validationResult = Validator.Validate(_state.Model);
            if (!validationResult.IsValid)
            {
                _state.Validation.Errors.Clear();
                _state.Validation.Errors.AddRange(validationResult.GetErrorMessages(TranslationService));
                AppNotification.ShowError(TranslationService.GetTranslation("validation.fixErrors", "Correggi gli errori prima di salvare"));
                return;
            }

            if (_state.SelectedUnitOfMeasureId.HasValue && _state.Model.UnitOfMeasureId != _state.SelectedUnitOfMeasureId)
            {
                UpdateModelUnitOfMeasure(_state.SelectedUnitOfMeasureId);
            }

            if (_isEditMode && RowId.HasValue)
            {
                await UpdateExistingRow();
            }
            else
            {
                await CreateNewRow();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving document row");
            var errorKey = _isEditMode ? "documents.rowUpdatedError" : "documents.rowAddedError";
            var errorMessage = _isEditMode
                ? "Errore durante l'aggiornamento della riga"
                : "Errore durante l'aggiunta della riga";
            AppNotification.ShowError(TranslationService.GetTranslation(errorKey, errorMessage));
        }
        finally
        {
            _state.Processing.IsSaving = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Aggiorna una riga esistente
    /// </summary>
    private async Task UpdateExistingRow()
    {
        var updateDto = new UpdateDocumentRowDto
        {
            ProductCode = _state.Model.ProductCode,
            Description = _state.Model.Description,
            UnitOfMeasure = _state.Model.UnitOfMeasure,
            UnitOfMeasureId = _state.Model.UnitOfMeasureId,
            UnitPrice = _state.Model.UnitPrice,
            Quantity = _state.Model.Quantity,
            Notes = _state.Model.Notes,
            RowType = _state.Model.RowType,
            LineDiscount = _state.Model.LineDiscount,
            LineDiscountValue = _state.Model.LineDiscountValue,
            DiscountType = _state.Model.DiscountType,
            VatRate = _state.Model.VatRate,
            VatDescription = _state.Model.VatDescription,
            IsGift = _state.Model.IsGift,
            IsManual = _state.Model.IsManual,
            SourceWarehouseId = _state.Model.SourceWarehouseId,
            DestinationWarehouseId = _state.Model.DestinationWarehouseId,
            SortOrder = _state.Model.SortOrder,
            StationId = _state.Model.StationId,
            ParentRowId = _state.Model.ParentRowId
        };

        // Validate before submitting
        var validationResult = Validator.Validate(updateDto);

        if (!validationResult.IsValid)
        {
            _state.Validation.Errors = validationResult.GetErrorMessages(TranslationService);
            StateHasChanged();
            return;
        }

        var result = await ExecuteWithErrorHandlingAsync(
            () => DocumentHeaderService.UpdateDocumentRowAsync(RowId!.Value, updateDto),
            operationName: "updateDocumentRow",
            successMessageKey: "documents.rowUpdatedSuccess");

        if (result != null)
        {
            MudDialog.Close(DialogResult.Ok(result));
        }
    }

    /// <summary>
    /// Crea una nuova riga
    /// </summary>
    private async Task CreateNewRow()
    {
        // Validate before submitting
        var validationResult = Validator.Validate(_state.Model);

        if (!validationResult.IsValid)
        {
            _state.Validation.Errors = validationResult.GetErrorMessages(TranslationService);
            StateHasChanged();
            return;
        }

        Logger.LogInformation(
            "Adding document row: ProductId={ProductId}, Qty={Qty}, MergeDuplicates={Merge}",
            _state.Model.ProductId,
            _state.Model.Quantity,
            _state.Model.MergeDuplicateProducts);

        var result = await ExecuteWithErrorHandlingAsync(
            () => DocumentHeaderService.AddDocumentRowAsync(_state.Model),
            operationName: "createDocumentRow",
            successMessageKey: "documents.rowAddedSuccess");

        if (result != null)
        {
            ResetForm();

            await FocusBarcodeField();
        }
    }

    /// <summary>
    /// Resetta il form per una nuova riga
    /// </summary>
    private void ResetForm()
    {
        var preserveMergeDuplicates = _state.Model.MergeDuplicateProducts;

        _state.Model = new CreateDocumentRowDto
        {
            DocumentHeaderId = DocumentHeaderId,
            Quantity = 1,
            MergeDuplicateProducts = preserveMergeDuplicates
        };
        _state.SelectedProduct = null;
        _state.PreviousSelectedProduct = null;
        _state.Barcode.Input = string.Empty;

        // Invalidate cached calculation result
        InvalidateCalculationCache();

        StateHasChanged();
    }

    /// <summary>
    /// Focuses the active product search field after a reset.
    /// In ContinuousScan mode focuses the scanner component;
    /// in Standard mode delegates to UnifiedProductSelector.FocusAsync().
    /// </summary>
    private async Task FocusBarcodeField()
    {
        try
        {
            await Task.Delay(Delays.RenderDelayMs);

            if (_dialogMode == DialogMode.ContinuousScan && _continuousScanRef != null)
            {
                await _continuousScanRef.FocusAsync();
            }
            else if (_productScannerRef != null)
            {
                await _productScannerRef.FocusAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Could not focus barcode field.");
        }
    }

    /// <summary>
    /// Annulla e chiude il dialog
    /// </summary>
    private void Cancel()
    {
        MudDialog.Cancel();
    }

    /// <summary>
    /// Handles product with code found from UnifiedProductSelector.
    /// NOTE: SelectedProductChanged fires first and already calls OnProductSelectedAsync
    /// (which calls PopulateFromProductAsync). This handler only captures barcode-specific
    /// data (ProductUnitId) that is exclusive to the barcode scan path.
    /// </summary>
    private Task HandleProductWithCodeFound(ProductWithCodeDto productWithCode)
    {
        // Handle the unit of measure specific from the barcode if present
        if (productWithCode.Code?.ProductUnitId != null)
        {
            _state.Barcode.ProductUnitId = productWithCode.Code.ProductUnitId;
            Logger.LogInformation("Barcode has specific unit: {ProductUnitId}",
                productWithCode.Code.ProductUnitId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles product updated from UnifiedProductSelector inline editing.
    /// The UnifiedProductSelector now passes the updated product as parameter.
    /// </summary>
    private async Task HandleProductUpdated(ProductDto updatedProduct)
    {
        try
        {
            // Update our local reference with the updated product
            _selectedProduct = updatedProduct;
            _state.SelectedProduct = updatedProduct;
            _state.PreviousSelectedProduct = updatedProduct;

            // Update the description and code fields
            _state.Model.Description = updatedProduct.Name;
            _state.Model.ProductCode = updatedProduct.Code;

            // Sync DefaultPrice after product edit. The barcode/autocomplete selection path uses
            // PriceResolutionService (price lists + promotions) via PopulateFromProductAsync, which
            // is more accurate. Here we only apply the updated DefaultPrice directly because the user
            // just changed it in the edit dialog — this is intentional and avoids a full repopulate.
            _state.Model.UnitPrice = updatedProduct.DefaultPrice ?? _state.Model.UnitPrice;

            // Update VAT if changed
            if (updatedProduct.VatRateId.HasValue)
            {
                _state.SelectedVatRateId = updatedProduct.VatRateId.Value;
                var vatRate = _state.Cache.AllVatRates.FirstOrDefault(v => v.Id == updatedProduct.VatRateId.Value);
                if (vatRate != null)
                {
                    _state.Model.VatRate = vatRate.Percentage;
                    _state.Model.VatDescription = vatRate.Name;
                }
            }

            // Update Unit of Measure if changed
            if (updatedProduct.UnitOfMeasureId.HasValue)
            {
                _state.SelectedUnitOfMeasureId = updatedProduct.UnitOfMeasureId.Value;
            }

            // Invalidate cached calculation result
            InvalidateCalculationCache();

            AppNotification.ShowSuccess(TranslationService.GetTranslation("products.updatedSuccess", "Prodotto aggiornato con successo"));

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in HandleProductUpdated for product {ProductId}.", updatedProduct.Id);
            AppNotification.ShowError(TranslationService.GetTranslation("products.updateApplyError", "Errore durante l'applicazione dell'aggiornamento del prodotto."));
        }
    }

    #endregion

    #region Continuous Scan Mode Methods

    /// <summary>
    /// Sets the dialog mode and initializes mode-specific state
    /// </summary>
    private void SetDialogMode(DialogMode mode)
    {
        if (_state.Mode == mode) return;

        _state.Mode = mode;

        if (mode == DialogMode.ContinuousScan)
        {
            _state.ContinuousScan.ScanCount = 0;
            _state.ContinuousScan.UniqueProductsCount = 0;
            _state.ContinuousScan.RecentScans.Clear();
            _state.ContinuousScan.ScannedProductIds.Clear();
            _state.ContinuousScan.FirstScanTime = DateTime.UtcNow;
            Logger.LogInformation("Switched to Continuous Scan Mode");
        }

        StateHasChanged();
    }

    /// <summary>
    /// Handles product selection in continuous scan mode.
    /// Called by UnifiedProductSelector.SelectedProductChanged when ContinuousReadMode=true.
    /// Uses the same population logic as standard mode to ensure full data completeness.
    /// </summary>
    private async Task HandleContinuousScanProductAsync(ProductDto? product)
    {
        if (product == null || _state.ContinuousScan.IsProcessing)
            return;

        _state.ContinuousScan.IsProcessing = true;
        StateHasChanged();

        try
        {
            // Populate all fields from the selected product (VAT, UoM, price, etc.)
            await SelectProductAndPopulateAsync(product);

            // Continuous scan: force qty=1 and enable row merge
            _state.Model.Quantity = 1;
            _state.Model.MergeDuplicateProducts = true;

            // Apply barcode-specific unit if captured via HandleProductWithCodeFound
            if (_state.Barcode.ProductUnitId.HasValue)
            {
                var specificUnit = _state.Cache.AvailableUnits
                    .FirstOrDefault(u => u.Id == _state.Barcode.ProductUnitId.Value);

                if (specificUnit != null)
                {
                    _state.SelectedUnitOfMeasureId = specificUnit.UnitOfMeasureId;
                    _state.Model.UnitOfMeasureId = specificUnit.UnitOfMeasureId;
                    UpdateModelUnitOfMeasure(_state.SelectedUnitOfMeasureId);

                    Logger.LogInformation("Using specific unit from barcode: {UnitId}",
                        specificUnit.Id);
                }

                _state.Barcode.ProductUnitId = null;
            }

            // Validate before save
            var validationResult = Validator.Validate(_state.Model);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.GetErrorMessages(TranslationService));
                Logger.LogWarning("Validation failed for continuous scan: {Errors}", errors);
                AppNotification.ShowError(
                    $"{TranslationService.GetTranslation("validation.incompleteData", "Dati incompleti")}: {errors}");
                await PlayErrorBeep();
                return;
            }

            // Save the row
            var result = await DocumentHeaderService.AddDocumentRowAsync(_state.Model);
            if (result == null)
                throw new Exception("AddDocumentRowAsync returned null");

            // Update scan statistics
            _state.ContinuousScan.ScanCount++;
            _state.ContinuousScan.LastScannedProduct = product.Name;
            UpdateRecentScans(product, product.Code ?? string.Empty, result);

            if (_state.ContinuousScan.ScannedProductIds.Add(product.Id))
                _state.ContinuousScan.UniqueProductsCount = _state.ContinuousScan.ScannedProductIds.Count;

            Logger.LogInformation(
                "Continuous scan successful: Product={ProductName}, NewQty={Quantity}, VatRate={Vat}%",
                product.Name, result.Quantity, result.VatRate);

            await PlaySuccessBeep();
            // StateHasChanged called in finally block below
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in continuous scan for product: {ProductName}", product?.Name);
            AppNotification.ShowError(
                TranslationService.GetTranslation("documents.errorScanProduct", "Errore durante la scansione del prodotto"));
            await PlayErrorBeep();
        }
        finally
        {
            _state.ContinuousScan.IsProcessing = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Updates recent scans list with merge logic
    /// </summary>
    private void UpdateRecentScans(ProductDto product, string identifier, DocumentRowDto result)
    {
        var existingEntry = _state.ContinuousScan.RecentScans.FirstOrDefault(s =>
            s.ProductId == product.Id && s.Barcode == identifier);

        if (existingEntry != null)
        {
            existingEntry.Quantity = (int)Math.Round(result.Quantity);
            existingEntry.Timestamp = DateTime.UtcNow;
            _state.ContinuousScan.RecentScans.Remove(existingEntry);
            _state.ContinuousScan.RecentScans.Insert(0, existingEntry);
        }
        else
        {
            _state.ContinuousScan.RecentScans.Insert(0, new ContinuousScanEntry
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Barcode = identifier,
                Quantity = (int)Math.Round(result.Quantity),
                Timestamp = DateTime.UtcNow,
                UnitPrice = product.DefaultPrice ?? 0m
            });
        }

        if (_state.ContinuousScan.RecentScans.Count > Limits.MaxRecentScans)
        {
            _state.ContinuousScan.RecentScans = _state.ContinuousScan.RecentScans
                .Take(Limits.MaxRecentScans).ToList();
        }
    }

    /// <summary>
    /// Plays success beep using JavaScript
    /// </summary>
    private async Task PlaySuccessBeep()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("playBeep", "success");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to play success beep");
        }
    }

    /// <summary>
    /// Plays error beep using JavaScript
    /// </summary>
    private async Task PlayErrorBeep()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("playBeep", "error");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to play error beep");
        }
    }

    /// <summary>
    /// Returns relative time string (e.g., "2 sec fa", "5 min fa")
    /// </summary>
    private string GetTimeAgo(DateTime timestamp)
    {
        var elapsed = DateTime.UtcNow - timestamp;

        if (elapsed.TotalSeconds < 60)
            return $"{(int)elapsed.TotalSeconds} {TranslationService.GetTranslation("common.secAgo", "sec fa")}";

        if (elapsed.TotalMinutes < 60)
            return $"{(int)elapsed.TotalMinutes} {TranslationService.GetTranslation("common.minAgo", "min fa")}";

        if (elapsed.TotalHours < 24)
            return $"{(int)elapsed.TotalHours} {TranslationService.GetTranslation("common.hourAgo", "h fa")}";

        return timestamp.ToString("HH:mm");
    }

    /// <summary>
    /// Gets the name of the applied price list for the current row
    /// </summary>
    private string GetAppliedPriceListName()
    {
        if (!_state.Model.AppliedPriceListId.HasValue)
            return string.Empty;

        // Return cached name if available
        return _appliedPriceListName ?? TranslationService.GetTranslation("documents.priceList", "Listino");
    }

    /// <summary>
    /// Ottiene il testo da mostrare per la sorgente del prezzo
    /// </summary>
    private string GetPriceSourceText()
    {
        if (_state.Model.IsPriceManual && _state.Model.AppliedPriceListId.HasValue)
        {
            // Prezzo modificato manualmente, ma c'era un listino originale
            return $"{TranslationService.GetTranslation("documents.originalFromList", "Originale da")}: {GetAppliedPriceListName()}";
        }

        if (_state.Model.AppliedPriceListId.HasValue)
        {
            // Prezzo da listino non modificato
            return $"{TranslationService.GetTranslation("documents.fromPriceList", "Da listino")}: {GetAppliedPriceListName()}";
        }

        return TranslationService.GetTranslation("documents.defaultPrice", "Prezzo predefinito prodotto");
    }

    /// <summary>
    /// Ottiene il colore dell'icona in base alla sorgente del prezzo
    /// </summary>
    private Color GetPriceSourceColor()
    {
        if (_state.Model.IsPriceManual)
        {
            return Color.Warning; // Giallo/arancione per modifiche manuali
        }

        if (_state.Model.AppliedPriceListId.HasValue)
        {
            return Color.Info; // Blu per listini applicati
        }

        return Color.Default; // Grigio per default price
    }

    /// <summary>
    /// Ottiene la classe CSS da applicare al campo prezzo
    /// </summary>
    private string GetPriceFieldClass()
    {
        if (_state.Model.IsPriceManual)
        {
            return "price-field-manual"; // Classe custom per highlight
        }

        if (_state.Model.AppliedPriceListId.HasValue)
        {
            return "price-field-from-list";
        }

        return string.Empty;
    }

    /// <summary>
    /// Genera tooltip esplicativo per la sorgente del prezzo
    /// </summary>
    private string GetPriceSourceTooltip()
    {
        if (_state.Model.IsPriceManual && _state.Model.AppliedPriceListId.HasValue)
        {
            return string.Format(
                TranslationService.GetTranslation(
                    "documents.priceManualTooltip",
                    "Prezzo modificato manualmente. Originale da listino: {0:C2}"
                ),
                _state.Model.OriginalPriceFromPriceList ?? 0m
            );
        }

        if (_state.Model.AppliedPriceListId.HasValue)
        {
            return string.Format(
                TranslationService.GetTranslation(
                    "documents.priceFromListTooltip",
                    "Prezzo applicato automaticamente dal listino: {0}"
                ),
                GetAppliedPriceListName()
            );
        }

        return TranslationService.GetTranslation(
            "documents.defaultPriceTooltip",
            "Prezzo predefinito del prodotto. Nessun listino applicabile."
        );
    }

    /// <summary>
    /// Handles manual price changes by the user
    /// </summary>
    private void OnPriceManuallyChanged(decimal newPrice)
    {
        // Compare with current price to avoid unnecessary triggers
        if (_state.Model.UnitPrice != newPrice)
        {
            _state.Model.UnitPrice = newPrice;
            _state.Model.IsPriceManual = true;

            AppNotification.ShowWarning(
                TranslationService.GetTranslation("documents.priceManuallyModified", "Prezzo modificato manualmente"));

            Logger.LogInformation(
                "Price manually overridden: Original={Original}, New={New}, Product={ProductId}",
                _state.Model.OriginalPriceFromPriceList ?? 0m,
                newPrice,
                _state.Model.ProductId
            );

            // Invalidate cache calculations
            _cachedCalculationResult = null;
            _cachedCalculationKey = string.Empty;

            StateHasChanged();
        }
    }

    /// <summary>
    /// Disposes resources
    /// </summary>
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    #endregion
}
