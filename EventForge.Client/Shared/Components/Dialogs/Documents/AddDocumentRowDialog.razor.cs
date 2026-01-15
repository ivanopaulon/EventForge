using EventForge.Client.Services;
using EventForge.Client.Services.Documents;
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
public partial class AddDocumentRowDialog
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
    private string _barcodeInput = string.Empty;
    private bool _isProcessing = false;
    private bool _isEditMode => RowId.HasValue;
    private bool _quickAddMode = false;
    
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
                
                await OnProductSelected(productWithCode.Product);
                _barcodeInput = string.Empty;
                _scannedBarcode = string.Empty;
                
                Snackbar.Add(
                    TranslationService.GetTranslation("warehouse.productFound", "Prodotto trovato"),
                    Severity.Success);
                
                if (_quantityField != null)
                {
                    await Task.Delay(100);
                    await _quantityField.FocusAsync();
                }
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
            _selectedProduct = createdProduct;
            await OnProductSelected(createdProduct);
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
                    
                    _selectedProduct = assignedProduct;
                    await OnProductSelected(assignedProduct);
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
    /// Gestisce la selezione di un prodotto
    /// </summary>
    private async Task OnProductSelected(ProductDto? product)
    {
        _selectedProduct = product;
        if (product != null)
        {
            await PopulateFromProduct(product);
            await LoadRecentTransactions(product.Id);
            StateHasChanged();
        }
    }

    /// <summary>
    /// Cerca prodotti per autocomplete
    /// </summary>
    private async Task<IEnumerable<ProductDto>> SearchProductsAsync(
        string searchTerm, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Array.Empty<ProductDto>();

        try
        {
            var result = await ProductService.SearchProductsAsync(searchTerm, 50);
            
            if (result == null)
                return Array.Empty<ProductDto>();
            
            var exactMatchProduct = result.ExactMatch?.Product;
            if (exactMatchProduct != null)
            {
                var exactMatchList = new List<ProductDto> { exactMatchProduct };
                
                var searchResults = result.SearchResults ?? Enumerable.Empty<ProductDto>();
                if (searchResults.Any())
                {
                    exactMatchList.AddRange(searchResults.Where(p => p.Id != exactMatchProduct.Id));
                }
                
                return exactMatchList;
            }
            
            return result.SearchResults ?? Enumerable.Empty<ProductDto>();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error searching products");
            return Array.Empty<ProductDto>();
        }
    }

    /// <summary>
    /// Popola il model dai dati del prodotto selezionato
    /// </summary>
    private async Task PopulateFromProduct(ProductDto product)
    {
        _model.ProductId = product.Id;
        _model.ProductCode = product.Code;
        _model.Description = product.Name;
        
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
        
        if (product.IsVatIncluded && vatRate > 0)
        {
            productPrice = productPrice / (1 + vatRate / 100m);
        }
        
        _model.UnitPrice = productPrice;
        
        // Invalidate cached calculation result
        _cachedCalculationResult = null;
        _cachedCalculationKey = string.Empty;

        await LoadProductUnits(product);
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
            if (_quickAddMode)
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
                await Task.Delay(100);
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
}
