using EventForge.Client.Services;
using Prym.DTOs.Products;
using Prym.DTOs.UnitOfMeasures;
using Prym.DTOs.VatRates;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace EventForge.Client.Shared.Components
{
    /// <summary>
    /// Defines how product search should behave
    /// </summary>
    [Flags]
    public enum ProductSearchMode
    {
        None = 0,
        Barcode = 1,          // ENTER searches as barcode
        Description = 2,       // Autocomplete for description
        Both = Barcode | Description
    }

    /// <summary>
    /// Defines how product editing should be handled
    /// </summary>
    public enum ProductEditMode
    {
        None,       // No editing allowed, Edit button hidden
        Dialog,     // Opens QuickCreateProductDialog
        Inline,     // Inline form in component (to be implemented)
        Delegate    // Notifies parent via OnEditRequested event
    }

    /// <summary>
    /// Defines how product creation should be handled when not found
    /// </summary>
    public enum ProductCreateMode
    {
        None,       // Does not handle creation
        Dialog,     // Opens QuickCreateProductDialog automatically
        Prompt,     // Shows inline prompt "Do you want to create a new product?"
        Delegate    // Notifies parent via OnProductNotFound (current behavior)
    }

    /// <summary>
    /// Unified component that combines product search (barcode + description) 
    /// with product info display.
    /// Replaces the separate DocumentRowBarcodeScanner, MudAutocomplete, and ProductQuickInfo components.
    /// Product editing is done via the QuickCreateProductDialog.
    /// </summary>
    public partial class UnifiedProductScanner : ComponentBase
    {
        [Inject] private IProductService ProductService { get; set; } = null!;
        [Inject] private IFinancialService FinancialService { get; set; } = null!;
        [Inject] private ITranslationService TranslationService { get; set; } = null!;
        [Inject] private IAppNotificationService AppNotification { get; set; } = null!;
        [Inject] private ILogger<UnifiedProductScanner> Logger { get; set; } = null!;
        [Inject] private IDialogService DialogService { get; set; } = null!;

        #region Parameters - Appearance

        /// <summary>
        /// Title to display at the top of the component.
        /// Set to null to hide the title section entirely.
        /// Default: "Cerca Prodotto"
        /// </summary>
        [Parameter] public string? Title { get; set; } = "Cerca Prodotto";
        [Parameter] public string Placeholder { get; set; } = "Scansiona barcode o cerca...";
        [Parameter] public string? SearchHelperText { get; set; }
        [Parameter] public bool Dense { get; set; } = true;
        [Parameter] public string? Class { get; set; }
        [Parameter] public string? Style { get; set; }

        #endregion

        #region Parameters - Sections

        [Parameter] public bool ShowProductInfo { get; set; } = true;
        [Parameter] public bool ShowCurrentStock { get; set; } = false;
        [Parameter] public decimal? CurrentStockQuantity { get; set; }

        #endregion

        #region Parameters - Search

        [Parameter] public ProductSearchMode SearchMode { get; set; } = ProductSearchMode.Both;
        [Parameter] public int MinSearchCharacters { get; set; } = 2;
        [Parameter] public int DebounceMs { get; set; } = 300;
        [Parameter] public int MaxResults { get; set; } = 50;
        [Parameter] public bool AutoFocus { get; set; } = true;
        [Parameter] public bool Disabled { get; set; } = false;
        [Parameter] public bool ShowBarcodeScanner { get; set; } = true;

        #endregion

        #region Parameters - Actions

        [Parameter] public bool AllowClear { get; set; } = true;
        [Parameter] public ProductEditMode EditMode { get; set; } = ProductEditMode.Dialog;
        [Parameter] public ProductCreateMode CreateMode { get; set; } = ProductCreateMode.Delegate;

        /// <summary>
        /// When true (default), each product selection automatically notifies the parent via
        /// <see cref="SelectedProductChanged"/> and then immediately resets the component
        /// (clears text, clears product info, re-focuses the search field) so the operator
        /// can scan the next item without any manual interaction.
        /// Set to false when the parent needs the selection to stay visible (e.g. an edit form
        /// that populates fields and waits for the user to explicitly move on).
        /// </summary>
        [Parameter] public bool ContinuousReadMode { get; set; } = true;

        #endregion

        #region Parameters - Reference Data (optional pre-loaded)

        /// <summary>
        /// Optional pre-loaded units of measure. If provided, the component skips loading
        /// units from the service (avoids a duplicate HTTP call when the parent already has them).
        /// </summary>
        [Parameter] public IEnumerable<UMDto>? AvailableUnits { get; set; }

        /// <summary>
        /// Optional pre-loaded VAT rates. If provided, the component skips loading VAT rates
        /// from the service (avoids a duplicate HTTP call when the parent already has them).
        /// </summary>
        [Parameter] public IEnumerable<VatRateDto>? AvailableVatRates { get; set; }

        #endregion

        #region Parameters - Two-Way Binding

        /// <summary>
        /// Selected product. Uses simple pattern like BusinessParty autocomplete.
        /// NO backing field, NO complex setter, NO StateHasChanged in setter.
        /// </summary>
        [Parameter] public ProductDto? SelectedProduct { get; set; }

        [Parameter] public EventCallback<ProductDto?> SelectedProductChanged { get; set; }

        #endregion

        #region Parameters - Events

        [Parameter] public EventCallback<ProductWithCodeDto> OnProductWithCodeFound { get; set; }
        [Parameter] public EventCallback<string> OnProductNotFound { get; set; }
        [Parameter] public EventCallback<ProductDto> OnEditRequested { get; set; }
        [Parameter] public EventCallback<ProductDto> OnProductUpdated { get; set; }
        [Parameter] public EventCallback<ProductDto> OnProductCreated { get; set; }

        #endregion

        #region Private Fields

        private MudAutocomplete<ProductDto>? _autocomplete;
        private string _searchText = string.Empty;

        // Available options for display
        private IEnumerable<UMDto>? _availableUnits;
        private IEnumerable<VatRateDto>? _availableVatRates;

        // Current display values
        private string? _currentUnitName;
        private string? _currentUnitSymbol;
        private string? _currentVatName;

        // Prompt mode tracking
        private bool _showNotFoundPrompt = false;
        private string _notFoundBarcode = string.Empty;

        // Reset tracking: detect when parent clears SelectedProduct (e.g. after adding to POS cart)
        private ProductDto? _previousSelectedProduct;
        private bool _shouldFocusAfterProductAdded;

        #endregion

        #region Lifecycle Methods

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await LoadReferenceData();
                UpdateDisplayValues();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error initializing UnifiedProductScanner.");
            }
        }

        protected override void OnParametersSet()
        {
            // Keep reference-data fields in sync when parent provides updated collections
            if (AvailableUnits != null)
                _availableUnits = AvailableUnits;
            if (AvailableVatRates != null)
                _availableVatRates = AvailableVatRates;

            // When parent resets SelectedProduct to null (e.g. product added to POS cart)
            // clear the search text and schedule autocomplete focus for next render
            if (_previousSelectedProduct != null && SelectedProduct == null)
            {
                _searchText = string.Empty;
                _shouldFocusAfterProductAdded = true;
            }
            _previousSelectedProduct = SelectedProduct;
            UpdateDisplayValues();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            try
            {
                if (firstRender && AutoFocus && _autocomplete != null && SelectedProduct == null)
                {
                    await Task.Delay(100); // Small delay to ensure rendering is complete
                    await _autocomplete.FocusAsync();
                }

                if (_shouldFocusAfterProductAdded && _autocomplete != null && SelectedProduct == null)
                {
                    _shouldFocusAfterProductAdded = false;
                    await Task.Delay(50);
                    await _autocomplete.FocusAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in OnAfterRenderAsync for UnifiedProductScanner.");
            }
        }

        #endregion

        #region Search Methods

        /// <summary>
        /// Search products by description.
        /// IMPORTANT: Uses the EXACT same pattern as SearchBusinessPartiesAsync in GenericDocumentProcedure.
        /// Simple, clean, NO StateHasChanged during search.
        /// </summary>
        private async Task<IEnumerable<ProductDto>> SearchProductsAsync(
            string searchTerm,
            CancellationToken cancellationToken)
        {
            // Early return for empty/short search terms
            if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < MinSearchCharacters)
                return Array.Empty<ProductDto>();

            try
            {
                var result = await ProductService.SearchProductsAsync(searchTerm, MaxResults, cancellationToken);

                if (result == null)
                {
                    Logger.LogWarning("Product search returned null for term: {SearchTerm}", searchTerm);
                    return Array.Empty<ProductDto>();
                }

                // When the server finds an exact code match it returns only ExactMatch (SearchResults is empty).
                // Include the exact match product in the autocomplete list so it is visible to the user.
                if (result.IsExactCodeMatch && result.ExactMatch?.Product != null)
                {
                    var combined = new List<ProductDto> { result.ExactMatch.Product };
                    combined.AddRange(result.SearchResults);
                    return combined;
                }

                return result.SearchResults;
            }
            catch (OperationCanceledException)
            {
                // Request was cancelled (e.g. user typed another character) — return empty silently
                return Array.Empty<ProductDto>();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error searching products");
                return Array.Empty<ProductDto>();
            }
        }

        /// <summary>
        /// Handle ENTER key in search field - search by barcode if enabled.
        /// </summary>
        private async Task HandleSearchKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && SearchMode.HasFlag(ProductSearchMode.Barcode))
            {
                var currentText = _searchText;
                if (!string.IsNullOrWhiteSpace(currentText))
                    await SearchByBarcode(currentText);
            }
        }

        /// <summary>
        /// Called when a product is selected from the autocomplete dropdown.
        /// CRITICAL: This is the missing piece that propagates selection to parent components.
        /// Pattern: Same as OnBusinessPartySelectedAsync in GenericDocumentProcedure.
        /// </summary>
        private async Task OnProductSelectionChangedAsync(ProductDto? product)
        {
            Logger.LogDebug("OnProductSelectionChangedAsync called. Product: {ProductId} - {ProductName}",
                product?.Id, product?.Name ?? "NULL");

            if (product == null) return;

            try
            {
                _previousSelectedProduct = product;
                SelectedProduct = product;

                if (SelectedProductChanged.HasDelegate)
                    await SelectedProductChanged.InvokeAsync(product);

                if (ContinuousReadMode)
                {
                    // Auto-reset: clear selection so component is immediately ready for next scan
                    SelectedProduct = null;
                    _previousSelectedProduct = null;
                    _searchText = string.Empty;
                    _shouldFocusAfterProductAdded = true;
                    StateHasChanged();
                }
                else
                {
                    UpdateDisplayValues();
                }

                Logger.LogDebug("Product selection propagated to parent. SelectedProduct: {ProductId}", product?.Id);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in OnProductSelectionChangedAsync for product {ProductId}.", product?.Id);
                AppNotification.ShowError(TranslationService.GetTranslation("errors.selectingProduct", "Errore durante la selezione del prodotto."));
            }
        }

        /// <summary>
        /// Search product by barcode/code.
        /// </summary>
        private async Task SearchByBarcode(string barcode)
        {
            try
            {
                var productWithCode = await ProductService.GetProductWithCodeByCodeAsync(barcode);

                if (productWithCode?.Product != null)
                {
                    _previousSelectedProduct = productWithCode.Product;
                    SelectedProduct = productWithCode.Product;
                    await SelectedProductChanged.InvokeAsync(SelectedProduct);

                    if (OnProductWithCodeFound.HasDelegate)
                        await OnProductWithCodeFound.InvokeAsync(productWithCode);

                    if (ContinuousReadMode)
                    {
                        SelectedProduct = null;
                        _previousSelectedProduct = null;
                        _searchText = string.Empty;
                        _shouldFocusAfterProductAdded = true;
                        StateHasChanged();
                    }
                    else
                    {
                        UpdateDisplayValues();
                    }

                    Logger.LogDebug("Product found by barcode {Barcode}: {ProductName}",
                        barcode, productWithCode.Product.Name);
                }
                else
                {
                    // Product NOT FOUND - handle based on CreateMode
                    Logger.LogWarning("Product not found for barcode: {Barcode}", barcode);

                    switch (CreateMode)
                    {
                        case ProductCreateMode.Dialog:
                            await OpenQuickCreateDialog(barcode);
                            break;

                        case ProductCreateMode.Prompt:
                            _showNotFoundPrompt = true;
                            _notFoundBarcode = barcode;
                            StateHasChanged();
                            break;

                        case ProductCreateMode.Delegate:
                            if (OnProductNotFound.HasDelegate)
                                await OnProductNotFound.InvokeAsync(barcode);
                            break;

                        case ProductCreateMode.None:
                        default:
                            // Do nothing
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error searching product by barcode: {Barcode}", barcode);
                AppNotification.ShowError(TranslationService.GetTranslation("errors.barcodeSearch", "Errore nella ricerca del codice a barre"));
            }
        }

        #endregion

        #region View Mode Methods

        private async Task LoadReferenceData()
        {
            try
            {
                var needUnits = AvailableUnits == null;
                var needVatRates = AvailableVatRates == null;

                // Use parent-provided data when available to avoid duplicate HTTP calls
                if (!needUnits)
                    _availableUnits = AvailableUnits;
                if (!needVatRates)
                    _availableVatRates = AvailableVatRates;

                // Only fetch what the parent hasn't provided
                if (needUnits && needVatRates)
                {
                    var unitsTask = ProductService.GetUnitsOfMeasureAsync();
                    var vatRatesTask = FinancialService.GetVatRatesAsync(1, 100);
                    await Task.WhenAll(unitsTask, vatRatesTask);
                    _availableUnits = unitsTask.Result;
                    _availableVatRates = vatRatesTask.Result?.Items ?? Enumerable.Empty<VatRateDto>();
                }
                else if (needUnits)
                {
                    _availableUnits = await ProductService.GetUnitsOfMeasureAsync();
                }
                else if (needVatRates)
                {
                    var vatRatesResult = await FinancialService.GetVatRatesAsync(1, 100);
                    _availableVatRates = vatRatesResult?.Items ?? Enumerable.Empty<VatRateDto>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading reference data for UnifiedProductScanner");
                AppNotification.ShowError(TranslationService.GetTranslation("errors.loadingData", "Errore nel caricamento dei dati"));
            }
        }

        private void UpdateDisplayValues()
        {
            if (SelectedProduct == null) return;

            // Update unit display
            if (SelectedProduct.UnitOfMeasureId.HasValue && _availableUnits != null)
            {
                var unit = _availableUnits.FirstOrDefault(u => u.Id == SelectedProduct.UnitOfMeasureId.Value);
                if (unit != null)
                {
                    _currentUnitName = unit.Name;
                    _currentUnitSymbol = unit.Symbol;
                }
            }
            else
            {
                _currentUnitName = null;
                _currentUnitSymbol = null;
            }

            // Update VAT display
            if (SelectedProduct.VatRateId.HasValue && _availableVatRates != null)
            {
                var vat = _availableVatRates.FirstOrDefault(v => v.Id == SelectedProduct.VatRateId.Value);
                if (vat != null)
                {
                    _currentVatName = $"{vat.Name} ({vat.Percentage}%)";
                }
            }
            else
            {
                _currentVatName = null;
            }
        }

        #endregion

        #region Edit Methods

        /// <summary>
        /// Handles edit button click based on EditMode
        /// </summary>
        private async Task HandleEditClick()
        {
            if (SelectedProduct == null) return;

            switch (EditMode)
            {
                case ProductEditMode.Dialog:
                    await OpenEditProductDialog();
                    break;

                case ProductEditMode.Inline:
                    StateHasChanged();
                    break;

                case ProductEditMode.Delegate:
                    await OnEditRequested.InvokeAsync(SelectedProduct);
                    break;

                case ProductEditMode.None:
                default:
                    break;
            }
        }

        /// <summary>
        /// Opens the QuickCreateProductDialog in edit mode
        /// </summary>
        private async Task OpenEditProductDialog()
        {
            if (SelectedProduct == null) return;

            try
            {
                var parameters = new DialogParameters
                {
                    { "ProductId", SelectedProduct.Id },
                    { "IsEditMode", true },
                    { "ExistingProduct", SelectedProduct }
                };

                var options = new DialogOptions
                {
                    MaxWidth = MaxWidth.Medium,
                    FullWidth = true,
                    CloseOnEscapeKey = true
                };

                var dialog = await DialogService.ShowAsync<Dialogs.QuickCreateProductDialog>(
                    TranslationService.GetTranslation("warehouse.quickEditProduct", "Modifica Rapida Prodotto"),
                    parameters,
                    options);

                var result = await dialog.Result;

                if (result is { Canceled: false } && result.Data is ProductDto updatedProduct)
                {
                    // Update the selected product with the edited data
                    SelectedProduct = updatedProduct;
                    await SelectedProductChanged.InvokeAsync(SelectedProduct);

                    // Update display values
                    UpdateDisplayValues();

                    // Notify parent that product was updated
                    if (OnProductUpdated.HasDelegate)
                    {
                        await OnProductUpdated.InvokeAsync(updatedProduct);
                    }

                    AppNotification.ShowSuccess(TranslationService.GetTranslation("products.updateSuccess", "Prodotto aggiornato con successo"));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in OpenEditProductDialog for product {ProductId}.", SelectedProduct?.Id);
                AppNotification.ShowError(TranslationService.GetTranslation("errors.editProduct", "Errore durante la modifica del prodotto."));
            }
        }

        /// <summary>
        /// Opens the QuickCreateProductDialog for creating a new product
        /// </summary>
        private async Task OpenQuickCreateDialog(string barcode)
        {
            try
            {
                var parameters = new DialogParameters
                {
                    { "PrefilledCode", barcode },
                    { "AutoAssignCode", true }
                };

                var options = new DialogOptions
                {
                    MaxWidth = MaxWidth.Medium,
                    FullWidth = true,
                    CloseOnEscapeKey = true
                };

                var dialog = await DialogService.ShowAsync<Dialogs.QuickCreateProductDialog>(
                    TranslationService.GetTranslation("warehouse.createNewProduct", "Crea Nuovo Prodotto"),
                    parameters,
                    options);

                var result = await dialog.Result;

                if (result is { Canceled: false } && result.Data is ProductDto createdProduct)
                {
                    // Set the created product as selected
                    SelectedProduct = createdProduct;
                    await SelectedProductChanged.InvokeAsync(SelectedProduct);

                    // Update display values
                    UpdateDisplayValues();

                    // Notify parent that product was created
                    if (OnProductCreated.HasDelegate)
                    {
                        await OnProductCreated.InvokeAsync(createdProduct);
                    }

                    // Hide prompt if it was showing
                    _showNotFoundPrompt = false;

                    AppNotification.ShowSuccess(TranslationService.GetTranslation("products.createSuccess", "Prodotto creato con successo"));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in OpenQuickCreateDialog for barcode {Barcode}.", barcode);
                AppNotification.ShowError(TranslationService.GetTranslation("errors.createProduct", "Errore durante la creazione del prodotto."));
            }
        }

        #endregion

        #region Action Methods

        private async Task ClearSelection()
        {
            try
            {
                SelectedProduct = null;
                await SelectedProductChanged.InvokeAsync(null);
                _searchText = string.Empty;

                if (_autocomplete != null)
                {
                    await _autocomplete.FocusAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in ClearSelection for UnifiedProductScanner.");
            }
        }

        /// <summary>
        /// Opens the camera barcode scanner dialog in continuous mode so the scanner
        /// stays open after each detection, allowing multiple consecutive scans.
        /// Each detected barcode is delegated to SearchByBarcode for an exact product-code lookup.
        /// </summary>
        private async Task OpenCameraScannerAsync()
        {
            var parameters = new DialogParameters<CameraBarcodeScannerDialog>
            {
                { x => x.OnBarcodeDetected, EventCallback.Factory.Create<string>(this, SearchByBarcode) },
                { x => x.ContinuousMode, true }
            };
            await DialogService.ShowAsync<CameraBarcodeScannerDialog>(
                string.Empty, parameters, Dialogs.EFDialogDefaults.Options);
        }

        /// <summary>
        /// Exposes focus control to parent components so they can programmatically move
        /// the keyboard focus into the product search field (e.g. after a dialog opens).
        /// </summary>
        public async Task FocusAsync()
        {
            if (_autocomplete != null)
            {
                await _autocomplete.FocusAsync();
            }
        }

        /// <summary>
        /// Triggers the product edit flow programmatically (e.g. from a parent keyboard shortcut).
        /// Behaves identically to the scanner's own Edit button.
        /// </summary>
        public async Task TriggerEditAsync()
        {
            await HandleEditClick();
        }

        #endregion
    }
}
