using Prym.DTOs.PriceHistory;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service for retrieving supplier purchase price history derived from stock movements.
/// Replaces the dedicated SupplierProductPriceHistory table: every inbound movement
/// with a UnitCost is the source of truth for purchase price history.
/// </summary>
public interface IStockMovementPriceService
{
    /// <summary>
    /// Gets purchase price history for a specific supplier product, computed from inbound stock movements.
    /// </summary>
    Task<PriceHistoryResponse> GetProductPriceHistoryAsync(
        Guid supplierId,
        Guid productId,
        PriceHistoryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregated purchase price history for all products from a supplier.
    /// </summary>
    Task<PriceHistoryResponse> GetSupplierPriceHistoryAsync(
        Guid supplierId,
        PriceHistoryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets purchase price history for a product across all suppliers.
    /// </summary>
    Task<PriceHistoryResponse> GetProductAllSuppliersPriceHistoryAsync(
        Guid productId,
        PriceHistoryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets price history statistics for a supplier, optionally filtered by product.
    /// </summary>
    Task<PriceHistoryStatistics> GetPriceHistoryStatisticsAsync(
        Guid supplierId,
        Guid? productId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets price trend data points for charting (purchase price over time).
    /// </summary>
    Task<List<PriceTrendDataPoint>> GetPriceTrendDataAsync(
        Guid supplierId,
        Guid productId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);
}
