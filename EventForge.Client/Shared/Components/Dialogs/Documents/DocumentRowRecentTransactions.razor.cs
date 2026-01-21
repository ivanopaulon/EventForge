using EventForge.DTOs.Products;
using EventForge.Client.Services;
using Microsoft.AspNetCore.Components;

namespace EventForge.Client.Shared.Components.Dialogs.Documents;

/// <summary>
/// Component for displaying recent product transactions and allowing price suggestions.
/// Shows last 3 transactions with date, price, unit of measure, document, and party.
/// </summary>
public partial class DocumentRowRecentTransactions : ComponentBase
{
    #region Injected Services

    [Inject] private IProductService ProductService { get; set; } = null!;
    [Inject] private ILogger<DocumentRowRecentTransactions> Logger { get; set; } = null!;

    #endregion

    #region Parameters

    /// <summary>
    /// Product ID to load transactions for
    /// </summary>
    [Parameter]
    public Guid? ProductId { get; set; }

    /// <summary>
    /// Document type filter for transactions (e.g., "purchase", "sale")
    /// </summary>
    [Parameter]
    public string? TransactionType { get; set; }

    /// <summary>
    /// Business party ID filter (optional)
    /// </summary>
    [Parameter]
    public Guid? PartyId { get; set; }

    /// <summary>
    /// Event triggered when user clicks "Applica" button to apply a price
    /// </summary>
    [Parameter]
    public EventCallback<decimal> OnPriceApplied { get; set; }

    /// <summary>
    /// Number of recent transactions to display (default: 3)
    /// </summary>
    [Parameter]
    public int MaxTransactions { get; set; } = 3;

    #endregion

    #region Private Fields

    private bool _isLoading = false;
    private List<RecentProductTransactionDto> RecentTransactions { get; set; } = new();
    private Guid? _previousProductId;

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// Loads recent transactions when ProductId changes
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        // Check if ProductId changed
        if (_previousProductId != ProductId)
        {
            _previousProductId = ProductId;

            // Load transactions if ProductId is set
            if (ProductId.HasValue)
            {
                await LoadRecentTransactionsAsync();
            }
            else
            {
                // Clear transactions if ProductId is null
                RecentTransactions.Clear();
            }
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Loads recent transactions for the selected product
    /// </summary>
    private async Task LoadRecentTransactionsAsync()
    {
        if (!ProductId.HasValue)
        {
            return;
        }

        _isLoading = true;
        RecentTransactions.Clear();
        StateHasChanged();

        try
        {
            var transactions = await ProductService.GetRecentProductTransactionsAsync(
                ProductId.Value,
                TransactionType ?? "purchase",
                PartyId,
                top: MaxTransactions
            );

            if (transactions != null)
            {
                RecentTransactions = transactions.ToList();
            }

            Logger.LogDebug("Loaded {Count} recent transactions for product {ProductId}",
                RecentTransactions.Count, ProductId.Value);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading recent transactions for product {ProductId}", ProductId.Value);
            RecentTransactions.Clear();
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Handles applying a price from recent transactions
    /// </summary>
    private async Task ApplyPrice(decimal price)
    {
        if (OnPriceApplied.HasDelegate)
        {
            await OnPriceApplied.InvokeAsync(price);
        }
    }

    #endregion
}
