using Prym.DTOs.Products;
using EventForge.Client.Services;
using EventForge.Client.Shared.Components.Dialogs;
using EventForge.Client.Shared.Components;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace EventForge.Client.Shared.Components.Dialogs.Documents;

/// <summary>
/// Product autocomplete component for document row dialogs.
/// Recreated using the EXACT pattern from GenericDocumentProcedure (BusinessParty autocomplete).
///
/// CRITICAL FIX: Previous version had broken autocomplete due to:
/// - Manual Value + ValueChanged instead of @bind-Value
/// - CoerceText/CoerceValue conflicts
/// - Incorrect state management
///
/// This version uses the proven working pattern from GenericDocumentProcedure.
/// </summary>
public partial class DocumentRowProductSelector : ComponentBase
{
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private IAppNotificationService AppNotification { get; set; } = default!;
    [Inject] private ILogger<DocumentRowProductSelector> Logger { get; set; } = default!;

    #region Parameters

    /// <summary>
    /// Currently selected product (two-way binding)
    /// </summary>
    [Parameter]
    public ProductDto? SelectedProduct { get; set; }

    /// <summary>
    /// Event triggered when selected product changes
    /// </summary>
    [Parameter]
    public EventCallback<ProductDto?> SelectedProductChanged { get; set; }

    /// <summary>
    /// Search function for product autocomplete.
    /// CRITICAL: Must match signature: Func&lt;string, CancellationToken, Task&lt;IEnumerable&lt;ProductDto&gt;&gt;&gt;
    /// </summary>
    [Parameter, EditorRequired]
    public Func<string, CancellationToken, Task<IEnumerable<ProductDto>>>? SearchFunc { get; set; }

    /// <summary>
    /// Show ProductQuickInfo card when product selected
    /// </summary>
    [Parameter]
    public bool ShowQuickInfo { get; set; } = true;

    /// <summary>
    /// Show Quick Edit button in ProductQuickInfo card
    /// </summary>
    [Parameter]
    public bool AllowQuickEdit { get; set; } = true;

    /// <summary>
    /// Event triggered when Quick Edit button clicked
    /// </summary>
    [Parameter]
    public EventCallback OnQuickEdit { get; set; }

    /// <summary>
    /// Allow clearing selection via X button
    /// </summary>
    [Parameter]
    public bool AllowClear { get; set; } = true;

    /// <summary>
    /// Disable autocomplete
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; } = false;

    /// <summary>
    /// When true shows a camera icon button to the right of the search field
    /// that opens the barcode scanner dialog.
    /// </summary>
    [Parameter]
    public bool ShowBarcodeScanner { get; set; } = false;

    #endregion

    #region Methods

    /// <summary>
    /// Wrapper for search function - delegates to parent.
    /// CRITICAL: This matches the GenericDocumentProcedure pattern exactly.
    /// </summary>
    private async Task<IEnumerable<ProductDto>> SearchProductsAsync(
        string searchTerm,
        CancellationToken cancellationToken)
    {
        if (SearchFunc == null)
        {
            return Array.Empty<ProductDto>();
        }

        return await SearchFunc(searchTerm, cancellationToken);
    }

    /// <summary>
    /// Handles Quick Edit button click
    /// </summary>
    private async Task HandleQuickEdit()
    {
        if (OnQuickEdit.HasDelegate)
        {
            await OnQuickEdit.InvokeAsync();
        }
    }

    /// <summary>
    /// Opens the camera barcode scanner dialog and handles the detected barcode.
    /// </summary>
    private async Task OpenBarcodeScannerAsync()
    {
        var parameters = new DialogParameters<CameraBarcodeScannerDialog>
        {
            { x => x.OnBarcodeDetected, EventCallback.Factory.Create<string>(this, OnBarcodeScannedAsync) }
        };
        await DialogService.ShowAsync<CameraBarcodeScannerDialog>(string.Empty, parameters, EFDialogDefaults.Options);
    }

    /// <summary>
    /// Processes a barcode scanned by the camera.
    /// If exactly one product matches the barcode, it is auto-selected.
    /// </summary>
    private async Task OnBarcodeScannedAsync(string barcode)
    {
        if (SearchFunc == null || string.IsNullOrWhiteSpace(barcode))
            return;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var results = (await SearchFunc(barcode, cts.Token)).ToList();

            if (results.Count == 1)
            {
                SelectedProduct = results[0];
                await SelectedProductChanged.InvokeAsync(SelectedProduct);
                StateHasChanged();
            }
            else if (results.Count == 0)
            {
                AppNotification.ShowWarning(TranslationService.GetTranslation(
                    "camera.noProductFound", "Nessun prodotto trovato per il codice: {0}", barcode));
            }
            else
            {
                AppNotification.ShowInfo(TranslationService.GetTranslation(
                    "camera.multipleProductsFound", "Trovati {0} prodotti per il codice: {1}", results.Count, barcode));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing scanned barcode {Barcode}", barcode);
            AppNotification.ShowError(TranslationService.GetTranslation(
                "camera.scanError", "Errore durante la lettura del codice a barre"));
        }
    }

    #endregion
}
