using EventForge.DTOs.Products;
using Microsoft.AspNetCore.Components;

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

    #endregion

    #region Private Fields

    private ProductDto? _previousProduct;

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// Detects product changes and triggers SelectedProductChanged event.
    /// Uses ID comparison to avoid reference equality issues.
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        // Check if SelectedProduct changed by comparing IDs (not references)
        var currentProductId = SelectedProduct?.Id;
        var previousProductId = _previousProduct?.Id;

        if (currentProductId != previousProductId)
        {
            _previousProduct = SelectedProduct;

            // Notify parent of change
            if (SelectedProductChanged.HasDelegate)
            {
                await SelectedProductChanged.InvokeAsync(SelectedProduct);
            }
        }
    }

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

    #endregion
}
