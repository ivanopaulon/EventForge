using EventForge.Client.Services;
using EventForge.Client.Services.Documents;
using EventForge.Client.Models.Documents;
using EventForge.DTOs.Documents;
using EventForge.DTOs.Products;
using EventForge.DTOs.UnitOfMeasures;
using EventForge.DTOs.VatRates;
using EventForge.DTOs.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;
using Blazored.LocalStorage;

namespace EventForge.Client.Shared.Components.Dialogs.Documents;

/// <summary>
/// Code-behind per AddDocumentRowDialog - Gestisce inserimento/modifica righe documento
/// </summary>
public partial class AddDocumentRowDialog : IDisposable
{
    #region Injected Dependencies
    
    [Inject] private IDocumentHeaderService DocumentHeaderService { get; set; } = null!;
    [Inject] private IProductService ProductService { get; set; } = null!;
    [Inject] private IFinancialService FinancialService { get; set; } = null!;
    [Inject] private IDocumentRowCalculationService CalculationService { get; set; } = null!;
    [Inject] private IDocumentDialogCacheService CacheService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private ITranslationService TranslationService { get; set; } = null!;
    [Inject] private ILogger<AddDocumentRowDialog> Logger { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private ILocalStorageService LocalStorage { get; set; } = null!;
    
    #endregion

    #region Parameters
    
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public Guid DocumentHeaderId { get; set; }
    [Parameter] public Guid? RowId { get; set; }
    
    #endregion

    #region Component References
    
    private MudTextField<string>? _barcodeField;
    private MudNumericField<decimal>? _quantityField;
    
    #endregion

    #region State Variables
    
    private CreateDocumentRowDto _model = new() { Quantity = 1m };
    private ProductDto? _selectedProduct = null;
    private ProductDto? _previousSelectedProduct = null;
    private string _barcodeInput = string.Empty;
    private bool _isProcessing = false;
    private bool _isEditMode => RowId.HasValue;
    private DialogMode _dialogMode = DialogMode.Standard;
    
    private List<ProductUnitDto> _availableUnits = new();
    private List<UMDto> _allUnitsOfMeasure = new();
    private List<VatRateDto> _allVatRates = new();
    
    private Guid? _selectedUnitOfMeasureId = null;
    private Guid? _selectedVatRateId = null;
    private Guid? _barcodeProductUnitId = null;
    
    private string _scannedBarcode = string.Empty;
    private bool _isProcessingBarcode = false;
    
    private DocumentHeaderDto? _documentHeader = null;
    private List<RecentProductTransactionDto> _recentTransactions = new();
    private bool _loadingTransactions = false;
    
    // Quick Add Mode entries tracking
    private List<QuickAddEntry> _recentQuickEntries = new();
    
    // Continuous scan mode state
    private List<ContinuousScanEntry> _recentContinuousScans = new();
    private HashSet<Guid> _scannedProductIds = new();
    private int _continuousScanCount = 0;
    private int _uniqueProductsCount = 0;
    private int _scansPerMinute = 0;
    private string _lastScannedProduct = string.Empty;
    private bool _isProcessingContinuousScan = false;
    private DateTime _firstScanTime = DateTime.UtcNow;
    private System.Timers.Timer? _statsTimer;
    private MudTextField<string>? _continuousScanField;
    private string _continuousScanInput = string.Empty;
    
    // Expansion panel states for accessibility
    private bool _vatPanelExpanded = false;
    private bool _discountsPanelExpanded = false;
    private bool _notesPanelExpanded = false;
    
    // Cached calculation result to avoid redundant calculations
    private Client.Models.Documents.DocumentRowCalculationResult? _cachedCalculationResult = null;
    private string _cachedCalculationKey = string.Empty;
    
    #endregion
    
    #region Quick Add Entry Model
    
    /// <summary>
    /// Model for tracking recent Quick Add entries
    /// </summary>
    private class QuickAddEntry
    {
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    #endregion

    #region Constants
    
    private const int RENDER_DELAY_MS = 100;
    private const int UI_REFOCUS_DELAY_MS = 100;
    private const int MAX_RECENT_SCANS = 20;
    
    private static readonly string[] PurchaseKeywords = 
        { "purchase", "receipt", "return", "acquisto", "carico", "reso" };
    private static readonly string[] SaleKeywords = 
        { "sale", "invoice", "shipment", "delivery", "vendita", "fattura", "scarico", "consegna" };
    
    #endregion

    #region Lifecycle Methods
    
    /// <summary>
    /// Inizializza il componente caricando i dati necessari
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        // Load panel states from LocalStorage
        await LoadPanelStatesAsync();
        
        await LoadDocumentHeaderAsync();
        await LoadUnitsOfMeasureAsync();
        await LoadVatRatesAsync();

        if (_isEditMode && RowId.HasValue)
        {
            await LoadRowForEdit(RowId.Value);
        }
    }

    /// <summary>
    /// Aggiorna il model quando i parametri cambiano
    /// </summary>
    protected override void OnParametersSet()
    {
        _model.DocumentHeaderId = DocumentHeaderId;
        
        // Watch for product selection changes - compare by ID for proper equality check
        if (_selectedProduct?.Id != _previousSelectedProduct?.Id)
        {
            // Capture the current product to avoid race conditions
            var currentProduct = _selectedProduct;
            _previousSelectedProduct = currentProduct;
            
            if (currentProduct != null)
            {
                // Use InvokeAsync to safely execute async operation within component lifecycle
                // Fire-and-forget is safe here as PopulateFromProductAsync handles its own errors
                _ = InvokeAsync(async () => 
                {
                    try
                    {
                        await PopulateFromProductAsync(currentProduct);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error populating product fields in OnParametersSet");
                    }
                });
            }
            else
            {
                ClearProductFields();
            }
        }
    }

    /// <summary>
    /// Imposta il focus sul campo barcode dopo il primo render
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_isEditMode && _barcodeField != null)
        {
            await _barcodeField.FocusAsync();
        }
    }
    
    #endregion
    
    #region Panel State Persistence
    
    private const string PANEL_STATE_KEY = "EventForge.Documents.AddDocumentRowDialog.PanelStates";
    
    /// <summary>
    /// Loads panel states from LocalStorage
    /// </summary>
    private async Task LoadPanelStatesAsync()
    {
        try
        {
            var states = await LocalStorage.GetItemAsync<PanelStates>(PANEL_STATE_KEY);
            if (states != null)
            {
                _vatPanelExpanded = states.VatPanelExpanded;
                _discountsPanelExpanded = states.DiscountsPanelExpanded;
                _notesPanelExpanded = states.NotesPanelExpanded;
                Logger.LogDebug("Loaded panel states from LocalStorage");
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error loading panel states from LocalStorage");
        }
    }
    
    /// <summary>
    /// Saves panel states to LocalStorage
    /// </summary>
    private async Task SavePanelStatesAsync()
    {
        try
        {
            var states = new PanelStates
            {
                VatPanelExpanded = _vatPanelExpanded,
                DiscountsPanelExpanded = _discountsPanelExpanded,
                NotesPanelExpanded = _notesPanelExpanded
            };
            await LocalStorage.SetItemAsync(PANEL_STATE_KEY, states);
            Logger.LogDebug("Saved panel states to LocalStorage");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error saving panel states to LocalStorage");
        }
    }
    
    /// <summary>
    /// Panel states DTO for LocalStorage persistence
    /// </summary>
    private class PanelStates
    {
        public bool VatPanelExpanded { get; set; }
        public bool DiscountsPanelExpanded { get; set; }
        public bool NotesPanelExpanded { get; set; }
    }
    
    
    #endregion

    #region Data Loading Methods
    
    /// <summary>
    /// Carica l'intestazione del documento
    /// </summary>
    private async Task LoadDocumentHeaderAsync()
    {
        try
        {
            _documentHeader = await DocumentHeaderService.GetDocumentHeaderByIdAsync(
                DocumentHeaderId, includeRows: false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading document header");
        }
    }

    /// <summary>
    /// Carica tutte le unità di misura disponibili
    /// </summary>
    private async Task LoadUnitsOfMeasureAsync()
    {
        try
        {
            // ✅ OPTIMIZATION: Use cache service instead of direct API call
            _allUnitsOfMeasure = await CacheService.GetUnitsOfMeasureAsync();
            Logger.LogDebug("Loaded {Count} units of measure from cache service", _allUnitsOfMeasure.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading units of measure");
        }
    }

    /// <summary>
    /// Carica tutte le aliquote IVA attive
    /// </summary>
    private async Task LoadVatRatesAsync()
    {
        try
        {
            // ✅ OPTIMIZATION: Use cache service instead of direct API call
            _allVatRates = await CacheService.GetVatRatesAsync();
            Logger.LogDebug("Loaded {Count} VAT rates from cache service", _allVatRates.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading VAT rates");
        }
    }

    /// <summary>
    /// Carica le transazioni recenti per un prodotto
    /// </summary>
    private async Task LoadRecentTransactions(Guid productId)
    {
        if (_documentHeader == null)
        {
            return;
        }

        _loadingTransactions = true;
        _recentTransactions.Clear();
        
        try
        {
            string transactionType = "purchase";
            
            if (!string.IsNullOrEmpty(_documentHeader.DocumentTypeName))
            {
                var lowerName = _documentHeader.DocumentTypeName.ToLower();
                
                if (SaleKeywords.Any(k => lowerName.Contains(k)))
                {
                    transactionType = "sale";
                }
                else if (PurchaseKeywords.Any(k => lowerName.Contains(k)))
                {
                    transactionType = "purchase";
                }
            }
            
            Guid? partyId = _documentHeader.BusinessPartyId != Guid.Empty ? _documentHeader.BusinessPartyId : null;
            
            var transactions = await ProductService.GetRecentProductTransactionsAsync(
                productId,
                transactionType,
                partyId,
                top: 3
            );
            
            if (transactions != null)
            {
                _recentTransactions = transactions.ToList();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading recent transactions for product {ProductId}", productId);
        }
        finally
        {
            _loadingTransactions = false;
        }
    }

    /// <summary>
    /// Carica una riga esistente per la modifica
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
            Snackbar.Add(
                TranslationService.GetTranslation("documents.errorLoadingRow", 
                    "Errore nel caricamento della riga"), 
                Severity.Error);
        }
    }

    /// <summary>
    /// Popola il model dai dati di una riga esistente
    /// </summary>
    private async Task PopulateModelFromRow(DocumentRowDto row)
    {
        _model.ProductCode = row.ProductCode;
        _model.Description = row.Description;
        _model.Quantity = row.Quantity;
        _model.UnitPrice = row.UnitPrice;
        _model.UnitOfMeasure = row.UnitOfMeasure;
        _model.UnitOfMeasureId = row.UnitOfMeasureId;
        _model.Notes = row.Notes;
        _model.VatRate = row.VatRate;
        _model.VatDescription = row.VatDescription;
        _selectedUnitOfMeasureId = row.UnitOfMeasureId;

        if (_model.VatRate > 0 || !string.IsNullOrEmpty(_model.VatDescription))
        {
            var vatRate = _allVatRates.FirstOrDefault(v => 
                v.Percentage == _model.VatRate && 
                (string.IsNullOrEmpty(_model.VatDescription) || v.Name == _model.VatDescription));
            if (vatRate != null)
            {
                _selectedVatRateId = vatRate.Id;
            }
        }

        if (row.ProductId.HasValue)
        {
            var product = await ProductService.GetProductByIdAsync(row.ProductId.Value);
            if (product != null)
            {
                _selectedProduct = product;
                var units = await ProductService.GetProductUnitsAsync(product.Id);
                _availableUnits = units?.ToList() ?? new List<ProductUnitDto>();
            }
        }

        StateHasChanged();
    }
    
    #endregion

    #region Barcode Handling
    
    /// <summary>
    /// Handles keyboard shortcuts for the dialog
    /// </summary>
    private async Task OnDialogKeyDown(KeyboardEventArgs e)
    {
        // Ctrl+E: Edit Product
        if (e.CtrlKey && e.Key == "e")
        {
            await OpenEditProductDialog();
            return;
        }
        
        // Ctrl+S: Save (alternative to Enter)
        if (e.CtrlKey && e.Key == "s")
        {
            if (IsValid() && !_isProcessing)
            {
                await SaveAndContinue();
            }
            return;
        }
        
        // Escape: Close dialog
        if (e.Key == "Escape")
        {
            Cancel();
            return;
        }
    }
    
    /// <summary>
    /// Gestisce la pressione di tasti nel campo barcode
    /// </summary>
    private async Task OnBarcodeKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            var currentValue = _barcodeField?.Value;
            if (!string.IsNullOrWhiteSpace(currentValue))
            {
                await SearchByBarcode(currentValue);
            }
        }
        else if (e.Key == "Escape")
        {
            Cancel();
        }
    }

    /// <summary>
    /// Cerca un prodotto tramite barcode
    /// </summary>
    private async Task SearchByBarcode(string barcode)
    {
        if (_isProcessingBarcode) return;
        
        try
        {
            _isProcessingBarcode = true;
            _scannedBarcode = barcode;
            
            var productWithCode = await ProductService.GetProductWithCodeByCodeAsync(barcode);
            if (productWithCode?.Product != null)
            {
                _barcodeProductUnitId = productWithCode.Code?.ProductUnitId;
                
                // Set product and populate fields
                await SelectProductAndPopulateAsync(productWithCode.Product);
                
                _barcodeInput = string.Empty;
                _scannedBarcode = string.Empty;
                
                Snackbar.Add(
                    TranslationService.GetTranslation("warehouse.productFound", "Prodotto trovato"),
                    Severity.Success);
            }
            else
            {
                await ShowProductNotFoundDialog(barcode);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error searching product by barcode {Barcode}", barcode);
            Snackbar.Add($"Errore: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isProcessingBarcode = false;
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

        if (!result.Canceled && result.Data != null)
        {
            await HandleProductNotFoundResult(result.Data);
        }
        else
        {
            _barcodeInput = string.Empty;
            _scannedBarcode = string.Empty;
        }
    }

    /// <summary>
    /// Gestisce il risultato del dialog prodotto non trovato
    /// </summary>
    private async Task HandleProductNotFoundResult(object data)
    {
        if (data is ProductDto createdProduct)
        {
            // Set product and populate fields
            await SelectProductAndPopulateAsync(createdProduct);
            
            _barcodeInput = string.Empty;
            _scannedBarcode = string.Empty;
            
            Snackbar.Add(
                TranslationService.GetTranslation("warehouse.productCreatedAndSelected", 
                    "Prodotto creato e selezionato"),
                Severity.Success);
        }
        else if (data is string action && action == "skip")
        {
            _barcodeInput = string.Empty;
            _scannedBarcode = string.Empty;
        }
        else
        {
            try
            {
                dynamic assignResult = data;
                if (assignResult.Action == "assigned" && assignResult.Product != null)
                {
                    ProductDto assignedProduct = assignResult.Product;
                    
                    // Set product and populate fields
                    await SelectProductAndPopulateAsync(assignedProduct);
                    
                    _barcodeInput = string.Empty;
                    _scannedBarcode = string.Empty;
                    
                    Snackbar.Add(
                        TranslationService.GetTranslation("warehouse.codeAssignedAndProductSelected", 
                            "Codice assegnato e prodotto selezionato"),
                        Severity.Success);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Could not extract assignment info from dialog result");
                _barcodeInput = string.Empty;
                _scannedBarcode = string.Empty;
            }
        }
    }
    
    #endregion

    #region Product Selection & Search
    
    /// <summary>
    /// Popola i campi del form dai dati del prodotto selezionato
    /// </summary>
    private async Task PopulateFromProductAsync(ProductDto product)
    {
        try
        {
            // ✅ CRITICAL: Force immediate UI update on render thread BEFORE async operations
            // This ensures MudAutocomplete sees the committed value
            await InvokeAsync(StateHasChanged);
            
            // 1. Popola campi base
            _model.ProductId = product.Id;
            _model.ProductCode = product.Code;
            _model.Description = product.Name;
            
            // 2. Popola prezzo e IVA
            decimal productPrice = product.DefaultPrice ?? 0m;
            decimal vatRate = 0m;

            if (product.VatRateId.HasValue)
            {
                _selectedVatRateId = product.VatRateId;
                var vatRateDto = _allVatRates.FirstOrDefault(v => v.Id == product.VatRateId.Value);
                if (vatRateDto != null)
                {
                    vatRate = vatRateDto.Percentage;
                    _model.VatRate = vatRate;
                    _model.VatDescription = vatRateDto.Name;
                }
            }
            
            // 3. Gestisci IVA inclusa
            if (product.IsVatIncluded && vatRate > 0)
            {
                productPrice = productPrice / (1 + vatRate / 100m);
            }

            // ✅ Update UI after basic fields are set
            await InvokeAsync(StateHasChanged);

            // 4. Carica unità di misura del prodotto (ASYNC operation)
            var units = await ProductService.GetProductUnitsAsync(product.Id);
            _availableUnits = units?.ToList() ?? new List<ProductUnitDto>();

            if (_availableUnits.Any())
            {
                // Seleziona unità di misura base o prima disponibile
                var defaultUnit = _availableUnits.FirstOrDefault(u => u.UnitType == "Base") 
                               ?? _availableUnits.FirstOrDefault();
                
                if (defaultUnit != null)
                {
                    _selectedUnitOfMeasureId = defaultUnit.UnitOfMeasureId;
                    _model.UnitOfMeasureId = defaultUnit.UnitOfMeasureId;
                    UpdateModelUnitOfMeasure(_selectedUnitOfMeasureId);
                }
            }
            else if (product.UnitOfMeasureId.HasValue)
            {
                // Fallback all'unità di misura del prodotto
                _selectedUnitOfMeasureId = product.UnitOfMeasureId;
                _model.UnitOfMeasureId = product.UnitOfMeasureId;
                
                var um = _allUnitsOfMeasure.FirstOrDefault(u => u.Id == product.UnitOfMeasureId.Value);
                if (um != null)
                {
                    _model.UnitOfMeasure = um.Symbol;
                }
            }

            // 5. Imposta prezzo finale
            _model.UnitPrice = productPrice;
            
            // 6. Invalidate cached calculation result
            _cachedCalculationResult = null;
            _cachedCalculationKey = string.Empty;

            // 7. Carica transazioni recenti (ASYNC operation)
            await LoadRecentTransactions(product.Id);

            // ✅ Final UI update after all data is loaded
            await InvokeAsync(StateHasChanged);

            // 8. Auto-focus su campo quantità
            if (_quantityField != null)
            {
                await Task.Delay(RENDER_DELAY_MS); // Delay per rendering
                await _quantityField.FocusAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error populating from product {ProductId}", product.Id);
            Snackbar.Add(
                TranslationService.GetTranslation("error.loadProductData", "Errore caricamento dati prodotto"),
                Severity.Error);
            
            // ✅ Ensure UI update even on error
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Pulisce i campi dipendenti dal prodotto
    /// </summary>
    private void ClearProductFields()
    {
        _model.ProductId = null;
        _model.ProductCode = string.Empty;
        _model.Description = string.Empty;
        _model.UnitPrice = 0m;
        _selectedUnitOfMeasureId = null;
        _model.UnitOfMeasureId = null;
        _model.UnitOfMeasure = string.Empty;
        _availableUnits.Clear();
        _recentTransactions.Clear();
    }

    /// <summary>
    /// Helper method to set a product and populate all related fields
    /// Consolidates the pattern used throughout the component
    /// </summary>
    private async Task SelectProductAndPopulateAsync(ProductDto product)
    {
        _selectedProduct = product;
        _previousSelectedProduct = _selectedProduct;
        await PopulateFromProductAsync(_selectedProduct);
    }

    /// <summary>
    /// Search products for autocomplete
    /// Uses proper cancellation token handling pattern matching working implementations
    /// </summary>
    private async Task<IEnumerable<ProductDto>> SearchProductsAsync(
        string searchTerm, 
        CancellationToken cancellationToken)
    {
        // Early return for empty/short search terms
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
            return Array.Empty<ProductDto>();

        try
        {
            // Add delay to avoid excessive renders (matches working autocompletes)
            // The delay itself can be cancelled via the cancellationToken
            await Task.Delay(50, cancellationToken);
            
            Logger.LogDebug("Searching products with term: {SearchTerm}", searchTerm);
            
            // Service call - Note: ProductService.SearchProductsAsync doesn't accept CancellationToken,
            // so the actual search cannot be cancelled, only the delay above
            var result = await ProductService.SearchProductsAsync(searchTerm, 50);
            
            if (result == null)
            {
                Logger.LogWarning("Product search returned null for term: {SearchTerm}", searchTerm);
                return Array.Empty<ProductDto>();
            }
            
            var products = new List<ProductDto>();
            
            // Add exact match at the top
            if (result.ExactMatch?.Product != null)
            {
                products.Add(result.ExactMatch.Product);
            }
            
            // Add other results (excluding duplicates)
            if (result.SearchResults?.Any() == true)
            {
                var exactMatchId = result.ExactMatch?.Product?.Id;
                products.AddRange(
                    result.SearchResults.Where(p => p.Id != exactMatchId)
                );
            }
            
            Logger.LogDebug("Found {Count} products for term '{SearchTerm}'", products.Count, searchTerm);
            return products;
        }
        catch (OperationCanceledException)
        {
            // Handle cancellation gracefully
            Logger.LogDebug("Product search cancelled for term: {SearchTerm}", searchTerm);
            return Array.Empty<ProductDto>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error searching products with term: {SearchTerm}", searchTerm);
            return Array.Empty<ProductDto>();
        }
    }

    /// <summary>
    /// Handles product selection from autocomplete
    /// CRITICAL: Uses InvokeAsync to ensure UI updates happen synchronously on render thread
    /// This prevents MudAutocomplete from resetting during async PopulateFromProductAsync
    /// </summary>
    private async Task OnProductSelected(ProductDto? product)
    {
        if (product == null)
        {
            // Clear selection - use InvokeAsync to ensure immediate UI update
            await InvokeAsync(() =>
            {
                _selectedProduct = null;
                _previousSelectedProduct = null;
                ClearProductFields();
                StateHasChanged();
            });
            return;
        }

        // Prevent re-processing same product - check against previously selected product
        if (_previousSelectedProduct?.Id == product.Id)
        {
            Logger.LogDebug("Product selection unchanged, skipping");
            return;
        }

        // ✅ CRITICAL FIX: Use InvokeAsync to ensure UI updates synchronously
        // This commits the selection to MudAutocomplete BEFORE async operations
        await InvokeAsync(() =>
        {
            _selectedProduct = product;
            _previousSelectedProduct = product;
            
            // ✅ Update UI IMMEDIATELY - this prevents autocomplete reset
            StateHasChanged();
            
            Logger.LogInformation("Product selected via autocomplete: {ProductId} - {ProductName}", 
                product.Id, product.Name);
        });
        
        // Now perform async operations (UI is already updated)
        await PopulateFromProductAsync(product);
    }

    /// <summary>
    /// Carica le unità di misura del prodotto
    /// </summary>
    private async Task LoadProductUnits(ProductDto product)
    {
        try
        {
            var units = await ProductService.GetProductUnitsAsync(product.Id);
            _availableUnits = units?.ToList() ?? new List<ProductUnitDto>();

            if (_availableUnits.Any())
            {
                if (_barcodeProductUnitId.HasValue)
                {
                    var barcodeUnit = _availableUnits.FirstOrDefault(u => u.Id == _barcodeProductUnitId.Value);
                    if (barcodeUnit != null)
                    {
                        _selectedUnitOfMeasureId = barcodeUnit.UnitOfMeasureId;
                        UpdateModelUnitOfMeasure(_selectedUnitOfMeasureId);
                        _barcodeProductUnitId = null;
                    }
                    else
                    {
                        SelectDefaultUnit();
                    }
                }
                else
                {
                    SelectDefaultUnit();
                }
            }
            else
            {
                await HandleNoUnitsConfigured(product);
            }

            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading product units for product {ProductId}", product.Id);
            Snackbar.Add(
                TranslationService.GetTranslation("documents.errorLoadingUnits", 
                    "Errore nel caricamento delle unità di misura"), 
                Severity.Error);
        }
    }

    /// <summary>
    /// Gestisce il caso in cui non ci siano unità configurate
    /// </summary>
    private async Task HandleNoUnitsConfigured(ProductDto product)
    {
        if (product.UnitOfMeasureId.HasValue)
        {
            _availableUnits.Add(new ProductUnitDto
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                UnitOfMeasureId = product.UnitOfMeasureId.Value,
                ConversionFactor = 1,
                UnitType = "Base",
                Status = EventForge.DTOs.Common.ProductUnitStatus.Active
            });

            _selectedUnitOfMeasureId = product.UnitOfMeasureId;
            UpdateModelUnitOfMeasure(_selectedUnitOfMeasureId);
        }
        else
        {
            Snackbar.Add(
                TranslationService.GetTranslation("documents.noUnitsConfigured", 
                    "Nessuna unità di misura configurata per questo prodotto"), 
                Severity.Warning);
            _selectedUnitOfMeasureId = null;
            _model.UnitOfMeasure = null;
            _model.UnitOfMeasureId = null;
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Seleziona l'unità di misura predefinita
    /// </summary>
    private void SelectDefaultUnit()
    {
        var defaultUnit = _availableUnits.FirstOrDefault(u => u.UnitType == "Base");
        if (defaultUnit != null)
        {
            _selectedUnitOfMeasureId = defaultUnit.UnitOfMeasureId;
        }
        else
        {
            _selectedUnitOfMeasureId = _availableUnits.First().UnitOfMeasureId;
        }

        UpdateModelUnitOfMeasure(_selectedUnitOfMeasureId);
    }
    
    #endregion

    #region Unit & VAT Selection
    
    /// <summary>
    /// Gestisce il cambio dell'unità di misura
    /// </summary>
    private void OnUnitOfMeasureChanged(Guid? unitOfMeasureId)
    {
        _selectedUnitOfMeasureId = unitOfMeasureId;
        UpdateModelUnitOfMeasure(unitOfMeasureId);
    }

    /// <summary>
    /// Gestisce il cambio dell'aliquota IVA
    /// </summary>
    private void OnVatRateChanged(Guid? vatRateId)
    {
        _selectedVatRateId = vatRateId;
        if (vatRateId.HasValue)
        {
            var vatRate = _allVatRates.FirstOrDefault(v => v.Id == vatRateId.Value);
            if (vatRate != null)
            {
                _model.VatRate = vatRate.Percentage;
                _model.VatDescription = vatRate.Name;
            }
        }
        else
        {
            _model.VatRate = 0;
            _model.VatDescription = null;
        }
        
        // Invalidate cached calculation result
        _cachedCalculationResult = null;
        _cachedCalculationKey = string.Empty;
        
        StateHasChanged();
    }

    /// <summary>
    /// Aggiorna l'unità di misura nel model
    /// </summary>
    private void UpdateModelUnitOfMeasure(Guid? unitOfMeasureId)
    {
        if (unitOfMeasureId.HasValue)
        {
            var selectedUom = _allUnitsOfMeasure.FirstOrDefault(u => u.Id == unitOfMeasureId);
            if (selectedUom != null)
            {
                _model.UnitOfMeasure = selectedUom.Symbol;
                _model.UnitOfMeasureId = selectedUom.Id;
            }
        }
        else
        {
            _model.UnitOfMeasure = null;
            _model.UnitOfMeasureId = null;
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
        return $"{_model.Quantity}|{_model.UnitPrice}|{_model.VatRate}|{_model.LineDiscount}|{_model.LineDiscountValue}|{_model.DiscountType}";
    }
    
    /// <summary>
    /// Gets calculation results from centralized service with caching.
    /// ✅ OPTIMIZATION: Caches calculation results to avoid redundant calculations
    /// during UI rendering. Cache is automatically invalidated when any input value changes.
    /// </summary>
    private Client.Models.Documents.DocumentRowCalculationResult GetCalculationResult()
    {
        var currentKey = GetCalculationCacheKey();
        
        // Use cached result if key matches (check string equality first as it's cheaper than null check in common case)
        if (_cachedCalculationKey == currentKey && _cachedCalculationResult != null)
        {
            return _cachedCalculationResult;
        }
        
        // Calculate and cache the result
        var input = new Client.Models.Documents.DocumentRowCalculationInput
        {
            Quantity = _model.Quantity,
            UnitPrice = _model.UnitPrice,
            VatRate = _model.VatRate,
            DiscountPercentage = _model.LineDiscount,
            DiscountValue = _model.LineDiscountValue,
            DiscountType = _model.DiscountType
        };
        
        _cachedCalculationResult = CalculationService.CalculateRowTotals(input);
        _cachedCalculationKey = currentKey;
        
        return _cachedCalculationResult;
    }
    
    private bool IsProductVatIncluded => _selectedProduct?.IsVatIncluded ?? false;
    
    /// <summary>
    /// Gets the original gross price of the product
    /// </summary>
    private decimal GetOriginalGrossPrice()
    {
        if (!IsProductVatIncluded)
            return _model.Quantity * _model.UnitPrice;
            
        return _model.Quantity * _model.UnitPrice * (1 + _model.VatRate / 100m);
    }

    /// <summary>
    /// Gets the subtotal for markup display
    /// </summary>
    private decimal GetSubtotal() => GetCalculationResult().NetAmount;
    
    /// <summary>
    /// Gets the VAT amount for markup display
    /// </summary>
    private decimal GetVatAmount() => GetCalculationResult().VatAmount;
    
    /// <summary>
    /// Gets the line total for markup display
    /// </summary>
    private decimal GetLineTotal() => GetCalculationResult().TotalAmount;
    
    /// <summary>
    /// Gets the total discount for markup display
    /// </summary>
    private decimal GetTotalDiscount() => GetCalculationResult().DiscountAmount;
    
    /// <summary>
    /// Gets the gross unit price for markup display
    /// </summary>
    private decimal GetUnitPriceGross() => GetCalculationResult().UnitPriceGross;
    
    /// <summary>
    /// Gets the total for markup display (alias of GetLineTotal)
    /// </summary>
    private decimal GetTotal() => GetCalculationResult().TotalAmount;
    
    #endregion

    #region Save & Validation
    
    /// <summary>
    /// Valida il form
    /// </summary>
    private bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(_model.Description) && _model.Quantity > 0;
    }

    /// <summary>
    /// Salva la riga e continua
    /// </summary>
    private async Task SaveAndContinue()
    {
        if (_isProcessing)
            return;

        _isProcessing = true;
        try
        {
            if (_selectedUnitOfMeasureId.HasValue && _model.UnitOfMeasureId != _selectedUnitOfMeasureId)
            {
                UpdateModelUnitOfMeasure(_selectedUnitOfMeasureId);
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
            Snackbar.Add(TranslationService.GetTranslation(errorKey, errorMessage), Severity.Error);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    /// <summary>
    /// Aggiorna una riga esistente
    /// </summary>
    private async Task UpdateExistingRow()
    {
        var updateDto = new UpdateDocumentRowDto
        {
            ProductCode = _model.ProductCode,
            Description = _model.Description,
            UnitOfMeasure = _model.UnitOfMeasure,
            UnitOfMeasureId = _model.UnitOfMeasureId,
            UnitPrice = _model.UnitPrice,
            Quantity = _model.Quantity,
            Notes = _model.Notes,
            RowType = _model.RowType,
            LineDiscount = _model.LineDiscount,
            LineDiscountValue = _model.LineDiscountValue,
            DiscountType = _model.DiscountType,
            VatRate = _model.VatRate,
            VatDescription = _model.VatDescription,
            IsGift = _model.IsGift,
            IsManual = _model.IsManual,
            SourceWarehouseId = _model.SourceWarehouseId,
            DestinationWarehouseId = _model.DestinationWarehouseId,
            SortOrder = _model.SortOrder,
            StationId = _model.StationId,
            ParentRowId = _model.ParentRowId
        };

        var result = await DocumentHeaderService.UpdateDocumentRowAsync(RowId!.Value, updateDto);
        if (result != null)
        {
            Snackbar.Add(
                TranslationService.GetTranslation("documents.rowUpdatedSuccess", 
                    "Riga aggiornata con successo"), 
                Severity.Success);
            MudDialog.Close(DialogResult.Ok(result));
        }
        else
        {
            Snackbar.Add(
                TranslationService.GetTranslation("documents.rowUpdatedError", 
                    "Errore durante l'aggiornamento della riga"), 
                Severity.Error);
        }
    }

    /// <summary>
    /// Crea una nuova riga
    /// </summary>
    private async Task CreateNewRow()
    {
        Logger.LogInformation(
            "Adding document row: ProductId={ProductId}, Qty={Qty}, MergeDuplicates={Merge}",
            _model.ProductId,
            _model.Quantity,
            _model.MergeDuplicateProducts);
        
        var result = await DocumentHeaderService.AddDocumentRowAsync(_model);
        if (result != null)
        {
            Snackbar.Add(
                TranslationService.GetTranslation("documents.rowAddedSuccess", 
                    "Riga aggiunta con successo"), 
                Severity.Success);
            
            // Track entry in Quick Add mode
            if (_dialogMode == DialogMode.QuickAdd)
            {
                _recentQuickEntries.Insert(0, new QuickAddEntry
                {
                    Description = _model.Description ?? string.Empty,
                    Quantity = _model.Quantity,
                    Timestamp = DateTime.Now
                });
                
                // Keep only last 10 entries
                if (_recentQuickEntries.Count > 10)
                {
                    _recentQuickEntries = _recentQuickEntries.Take(10).ToList();
                }
            }
            
            ResetForm();
            
            if (_barcodeField != null)
            {
                await Task.Delay(RENDER_DELAY_MS);
                await _barcodeField.FocusAsync();
            }
        }
        else
        {
            Snackbar.Add(
                TranslationService.GetTranslation("documents.rowAddedError", 
                    "Errore durante l'aggiunta della riga"), 
                Severity.Error);
        }
    }

    /// <summary>
    /// Resetta il form per una nuova riga
    /// </summary>
    private void ResetForm()
    {
        var preserveMergeDuplicates = _model.MergeDuplicateProducts;
        
        _model = new CreateDocumentRowDto 
        { 
            DocumentHeaderId = DocumentHeaderId,
            Quantity = 1,
            MergeDuplicateProducts = preserveMergeDuplicates
        };
        _selectedProduct = null;
        _previousSelectedProduct = null;
        _barcodeInput = string.Empty;
        
        // Invalidate cached calculation result
        _cachedCalculationResult = null;
        _cachedCalculationKey = string.Empty;
        
        StateHasChanged();
    }

    /// <summary>
    /// Annulla e chiude il dialog
    /// </summary>
    private void Cancel()
    {
        MudDialog.Cancel();
    }

    /// <summary>
    /// Applica un suggerimento di prezzo dalle transazioni recenti
    /// </summary>
    private void ApplySuggestion(RecentProductTransactionDto suggestion)
    {
        try
        {
            _model.UnitPrice = suggestion.EffectiveUnitPrice;
            
            if (suggestion.BaseUnitPrice.HasValue)
            {
                _model.BaseUnitPrice = suggestion.BaseUnitPrice.Value;
            }
            
            _model.LineDiscount = 0;
            _model.LineDiscountValue = 0;
            _model.DiscountType = EventForge.DTOs.Common.DiscountType.Percentage;
            
            // Invalidate cached calculation result
            _cachedCalculationResult = null;
            _cachedCalculationKey = string.Empty;
            
            Snackbar.Add(
                TranslationService.GetTranslation("documents.priceApplied", "Prezzo applicato: {0:C2}", suggestion.EffectiveUnitPrice),
                Severity.Success);
            
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error applying price suggestion");
            Snackbar.Add(
                TranslationService.GetTranslation("documents.priceApplyError", "Errore nell'applicazione del prezzo"),
                Severity.Error);
        }
    }

    /// <summary>
    /// Apre il dialog per modificare rapidamente il prodotto
    /// </summary>
    private async Task OpenEditProductDialog()
    {
        if (_selectedProduct == null || _selectedProduct.Id == Guid.Empty)
        {
            Snackbar.Add(
                TranslationService.GetTranslation("products.noProductSelected", "Nessun prodotto selezionato"),
                Severity.Warning);
            return;
        }

        var parameters = new DialogParameters
        {
            { "IsEditMode", true },
            { "ProductId", _selectedProduct.Id },
            { "ExistingProduct", _selectedProduct }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        var dialog = await DialogService.ShowAsync<QuickCreateProductDialog>(
            TranslationService.GetTranslation("warehouse.quickEditProduct", "Modifica Rapida Prodotto"),
            parameters,
            options);

        var result = await dialog.Result;

        if (!result.Canceled && result.Data is ProductDto updatedProduct)
        {
            _selectedProduct = updatedProduct;
            
            _model.Description = updatedProduct.Description;
            _model.ProductCode = updatedProduct.Code;
            _model.UnitPrice = updatedProduct.DefaultPrice ?? _model.UnitPrice;
            
            if (updatedProduct.VatRateId.HasValue)
            {
                _selectedVatRateId = updatedProduct.VatRateId.Value;
                var vatRate = _allVatRates.FirstOrDefault(v => v.Id == updatedProduct.VatRateId.Value);
                if (vatRate != null)
                {
                    _model.VatRate = vatRate.Percentage;
                    _model.VatDescription = vatRate.Name;
                }
            }
            
            // Invalidate cached calculation result
            _cachedCalculationResult = null;
            _cachedCalculationKey = string.Empty;
            
            Snackbar.Add(
                TranslationService.GetTranslation("products.updatedSuccess", "Prodotto aggiornato con successo"),
                Severity.Success);
            
            StateHasChanged();
        }
    }
    
    #endregion
    
    #region Continuous Scan Mode Methods
    
    /// <summary>
    /// Sets the dialog mode and initializes mode-specific state
    /// </summary>
    private void SetDialogMode(DialogMode mode)
    {
        if (_dialogMode == mode) return;
        
        _dialogMode = mode;
        
        if (mode == DialogMode.ContinuousScan)
        {
            // Initialize continuous scan mode
            _continuousScanCount = 0;
            _uniqueProductsCount = 0;
            _scansPerMinute = 0;
            _recentContinuousScans.Clear();
            _scannedProductIds.Clear();
            _firstScanTime = DateTime.UtcNow;
            StartStatsTimer();
            
            Logger.LogInformation("Switched to Continuous Scan Mode");
        }
        else
        {
            StopStatsTimer();
        }
        
        StateHasChanged();
    }
    
    /// <summary>
    /// Processes a scanned barcode in continuous scan mode
    /// </summary>
    private async Task ProcessContinuousScan(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode) || _isProcessingContinuousScan)
            return;

        _isProcessingContinuousScan = true;
        StateHasChanged();

        try
        {
            // 1. Search product by barcode
            var productWithCode = await ProductService.GetProductWithCodeByCodeAsync(barcode);
            
            if (productWithCode?.Product == null)
            {
                Logger.LogWarning("Product not found for barcode: {Barcode}", barcode);
                Snackbar.Add($"⚠️ Prodotto non trovato: {barcode}", Severity.Warning);
                await PlayErrorBeep();
                return;
            }
            
            var product = productWithCode.Product;
            
            // 2. Create DTO with MergeDuplicateProducts = true
            var rowDto = new CreateDocumentRowDto
            {
                DocumentHeaderId = DocumentHeaderId,
                ProductId = product.Id,
                ProductCode = product.Code,
                Description = product.Name,
                Quantity = 1,
                UnitPrice = product.DefaultPrice ?? 0m,
                UnitOfMeasureId = productWithCode.Code?.ProductUnitId ?? product.UnitOfMeasureId,
                VatRate = product.VatRatePercentage ?? 0m,
                VatDescription = product.VatRateName,
                MergeDuplicateProducts = true, // Enable auto-merge
                Notes = $"Scansione continua: {DateTime.UtcNow:HH:mm:ss}"
            };
            
            // 3. API call
            var result = await DocumentHeaderService.AddDocumentRowAsync(rowDto);
            
            if (result == null)
            {
                throw new Exception("AddDocumentRowAsync returned null");
            }
            
            // 4. Update stats
            _continuousScanCount++;
            _lastScannedProduct = product.Name;
            
            // 5. Update tracking list and unique products count
            UpdateRecentScans(product, barcode, result);
            
            // Track unique product (using HashSet for O(1) lookup and insert)
            if (_scannedProductIds.Add(product.Id))
            {
                _uniqueProductsCount = _scannedProductIds.Count;
            }
            
            // 6. Audio feedback
            await PlaySuccessBeep();
            
            Logger.LogInformation(
                "Continuous scan successful: Barcode={Barcode}, Product={ProductName}, NewQty={Quantity}",
                barcode,
                product.Name,
                result.Quantity);
            
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in continuous scan for barcode: {Barcode}", barcode);
            Snackbar.Add($"❌ Errore: {ex.Message}", Severity.Error);
            await PlayErrorBeep();
        }
        finally
        {
            _isProcessingContinuousScan = false;
            _continuousScanInput = string.Empty;
            StateHasChanged();
            
            // Auto-refocus scanner field
            await Task.Delay(UI_REFOCUS_DELAY_MS);
            if (_continuousScanField != null)
            {
                await _continuousScanField.FocusAsync();
            }
        }
    }
    
    /// <summary>
    /// Updates recent scans list with merge logic
    /// </summary>
    private void UpdateRecentScans(ProductDto product, string barcode, DocumentRowDto result)
    {
        var existingEntry = _recentContinuousScans.FirstOrDefault(s => 
            s.ProductId == product.Id && s.Barcode == barcode);
        
        if (existingEntry != null)
        {
            // Update existing entry
            existingEntry.Quantity = (int)result.Quantity;
            existingEntry.Timestamp = DateTime.UtcNow;
            
            // Move to top
            _recentContinuousScans.Remove(existingEntry);
            _recentContinuousScans.Insert(0, existingEntry);
        }
        else
        {
            // Create new entry
            _recentContinuousScans.Insert(0, new ContinuousScanEntry
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Barcode = barcode,
                Quantity = (int)result.Quantity,
                Timestamp = DateTime.UtcNow,
                UnitPrice = product.DefaultPrice ?? 0m
            });
        }
        
        // Keep only last MAX_RECENT_SCANS entries
        if (_recentContinuousScans.Count > MAX_RECENT_SCANS)
        {
            _recentContinuousScans = _recentContinuousScans.Take(MAX_RECENT_SCANS).ToList();
        }
    }
    
    /// <summary>
    /// Starts timer for real-time stats updates
    /// </summary>
    private void StartStatsTimer()
    {
        StopStatsTimer();
        
        _statsTimer = new System.Timers.Timer(1000); // Update every second
        _statsTimer.Elapsed += (sender, e) =>
        {
            var elapsed = (DateTime.UtcNow - _firstScanTime).TotalMinutes;
            _scansPerMinute = elapsed > 0 ? (int)(_continuousScanCount / elapsed) : 0;
            InvokeAsync(StateHasChanged);
        };
        _statsTimer.AutoReset = true;
        _statsTimer.Start();
    }
    
    /// <summary>
    /// Stops and disposes stats timer
    /// </summary>
    private void StopStatsTimer()
    {
        if (_statsTimer != null)
        {
            _statsTimer.Stop();
            _statsTimer.Dispose();
            _statsTimer = null;
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
    /// Handles key down events in continuous scan input
    /// </summary>
    private async Task OnContinuousScanKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(_continuousScanInput))
        {
            await ProcessContinuousScan(_continuousScanInput.Trim());
        }
        else if (e.Key == "Escape")
        {
            SetDialogMode(DialogMode.Standard);
        }
    }
    
    /// <summary>
    /// Returns relative time string (e.g., "2 sec fa", "5 min fa")
    /// </summary>
    private string GetTimeAgo(DateTime timestamp)
    {
        var elapsed = DateTime.UtcNow - timestamp;
        
        if (elapsed.TotalSeconds < 60)
            return $"{(int)elapsed.TotalSeconds} sec fa";
        
        if (elapsed.TotalMinutes < 60)
            return $"{(int)elapsed.TotalMinutes} min fa";
        
        if (elapsed.TotalHours < 24)
            return $"{(int)elapsed.TotalHours} h fa";
        
        return timestamp.ToString("HH:mm");
    }
    
    /// <summary>
    /// Disposes resources including stats timer
    /// </summary>
    public void Dispose()
    {
        StopStatsTimer();
    }
    
    #endregion
}
