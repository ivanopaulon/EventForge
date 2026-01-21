using EventForge.Client.Services;
using EventForge.DTOs.Products;
using EventForge.DTOs.UnitOfMeasures;
using EventForge.DTOs.VatRates;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventForge.Client.Shared.Components
{
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
        [Inject] private ISnackbar Snackbar { get; set; } = null!;
        [Inject] private ILogger<UnifiedProductScanner> Logger { get; set; } = null!;
        [Inject] private IDialogService DialogService { get; set; } = null!;

        #region Parameters - Appearance

        [Parameter] public string Title { get; set; } = "Cerca Prodotto";
        [Parameter] public bool ShowTitle { get; set; } = true;
        [Parameter] public string Placeholder { get; set; } = "Scansiona barcode o cerca per nome...";
        [Parameter] public string? SearchHelperText { get; set; }
        [Parameter] public int Elevation { get; set; } = 1;
        [Parameter] public bool Dense { get; set; } = true;
        [Parameter] public string? Class { get; set; }
        [Parameter] public string? Style { get; set; }
        [Parameter] public bool Disabled { get; set; } = false;

        #endregion

        #region Parameters - Behavior

        [Parameter] public int MinSearchCharacters { get; set; } = 2;
        [Parameter] public int DebounceMs { get; set; } = 300;
        [Parameter] public int MaxResults { get; set; } = 50;
        [Parameter] public bool AllowEdit { get; set; } = true;
        [Parameter] public bool AllowClear { get; set; } = true;
        [Parameter] public bool AutoFocus { get; set; } = true;
        [Parameter] public bool ShowCurrentStock { get; set; } = false;
        [Parameter] public decimal? CurrentStockQuantity { get; set; }

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
        [Parameter] public EventCallback OnProductUpdated { get; set; }

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

        #endregion

        #region Lifecycle Methods

        protected override async Task OnInitializedAsync()
        {
            await LoadReferenceData();
            UpdateDisplayValues();
        }

        protected override void OnParametersSet()
        {
            UpdateDisplayValues();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && AutoFocus && _autocomplete != null && SelectedProduct == null)
            {
                await Task.Delay(100); // Small delay to ensure rendering is complete
                await _autocomplete.FocusAsync();
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
                var result = await ProductService.SearchProductsAsync(searchTerm, MaxResults);

                if (result == null)
                {
                    Logger.LogWarning("Product search returned null for term: {SearchTerm}", searchTerm);
                    return Array.Empty<ProductDto>();
                }

                return result.SearchResults ?? new List<ProductDto>();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error searching products");
                return Array.Empty<ProductDto>();
            }
        }

        /// <summary>
        /// Handle ENTER key in search field - search by barcode.
        /// </summary>
        private async Task HandleSearchKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(_searchText))
            {
                await SearchByBarcode(_searchText);
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
                    // Product found - update binding
                    SelectedProduct = productWithCode.Product;
                    await SelectedProductChanged.InvokeAsync(SelectedProduct);

                    // Notify parent with code context
                    if (OnProductWithCodeFound.HasDelegate)
                    {
                        await OnProductWithCodeFound.InvokeAsync(productWithCode);
                    }

                    // Update display values
                    UpdateDisplayValues();

                    Logger.LogInformation("Product found by barcode {Barcode}: {ProductName}", 
                        barcode, productWithCode.Product.Name);
                }
                else
                {
                    // Product not found - notify parent to show ProductNotFoundDialog
                    Logger.LogWarning("Product not found for barcode: {Barcode}", barcode);

                    if (OnProductNotFound.HasDelegate)
                    {
                        await OnProductNotFound.InvokeAsync(barcode);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error searching product by barcode: {Barcode}", barcode);
                Snackbar.Add(
                    TranslationService.GetTranslation("errors.barcodeSearch", "Errore nella ricerca del codice a barre"),
                    Severity.Error
                );
            }
        }

        #endregion

        #region View Mode Methods

        private async Task LoadReferenceData()
        {
            try
            {
                // Load units and VAT rates in parallel
                var unitsTask = ProductService.GetUnitsOfMeasureAsync();
                var vatRatesTask = FinancialService.GetVatRatesAsync(1, 100);

                await Task.WhenAll(unitsTask, vatRatesTask);

                _availableUnits = await unitsTask;
                var vatRatesResult = await vatRatesTask;
                _availableVatRates = vatRatesResult?.Items ?? Enumerable.Empty<VatRateDto>();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading reference data for UnifiedProductScanner");
                Snackbar.Add(
                    TranslationService.GetTranslation("errors.loadingData", "Errore nel caricamento dei dati"),
                    Severity.Error
                );
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
        /// Opens the QuickCreateProductDialog in edit mode
        /// </summary>
        private async Task OpenEditProductDialog()
        {
            if (SelectedProduct == null) return;

            var parameters = new DialogParameters
            {
                { "ProductId", SelectedProduct.Id },
                { "IsEditMode", true }
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

            if (!result.Canceled && result.Data is ProductDto updatedProduct)
            {
                // Update the selected product with the edited data
                SelectedProduct = updatedProduct;
                await SelectedProductChanged.InvokeAsync(SelectedProduct);
                
                // Update display values
                UpdateDisplayValues();

                // Notify parent that product was updated
                if (OnProductUpdated.HasDelegate)
                {
                    await OnProductUpdated.InvokeAsync();
                }

                Snackbar.Add(
                    TranslationService.GetTranslation("products.updateSuccess", "Prodotto aggiornato con successo"),
                    Severity.Success
                );
            }
        }

        #endregion

        #region Action Methods

        private async Task ClearSelection()
        {
            SelectedProduct = null;
            await SelectedProductChanged.InvokeAsync(null);
            _searchText = string.Empty;
            
            if (_autocomplete != null)
            {
                await _autocomplete.FocusAsync();
            }
        }

        #endregion

        #region Helper Methods

        private string GetCssClass()
        {
            var baseClass = "pa-3";
            return string.IsNullOrEmpty(Class) ? baseClass : $"{baseClass} {Class}";
        }

        #endregion
    }
}
