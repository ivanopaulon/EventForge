using EventForge.DTOs.PriceHistory;

namespace EventForge.Server.Services.PriceHistory;

/// <summary>
/// Service for managing supplier product price history.
/// </summary>
public interface ISupplierProductPriceHistoryService
{
    /// <summary>
    /// Logs a single price change.
    /// </summary>
    /// <param name="request">Price change details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created price history entry ID.</returns>
    Task<Guid> LogPriceChangeAsync(PriceChangeLogRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs multiple price changes in bulk.
    /// </summary>
    /// <param name="requests">List of price change details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of created price history entry IDs.</returns>
    Task<List<Guid>> LogBulkPriceChangesAsync(List<PriceChangeLogRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets price history for a specific supplier product.
    /// </summary>
    /// <param name="supplierId">Supplier identifier.</param>
    /// <param name="productId">Product identifier.</param>
    /// <param name="request">Query parameters with filters and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated price history response.</returns>
    Task<PriceHistoryResponse> GetProductPriceHistoryAsync(
        Guid supplierId,
        Guid productId,
        PriceHistoryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregated price history for all products from a supplier.
    /// </summary>
    /// <param name="supplierId">Supplier identifier.</param>
    /// <param name="request">Query parameters with filters and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated price history response.</returns>
    Task<PriceHistoryResponse> GetSupplierPriceHistoryAsync(
        Guid supplierId,
        PriceHistoryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets price history for a product across all suppliers.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="request">Query parameters with filters and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated price history response.</returns>
    Task<PriceHistoryResponse> GetProductAllSuppliersPriceHistoryAsync(
        Guid productId,
        PriceHistoryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets price history statistics for a supplier, optionally filtered by product.
    /// </summary>
    /// <param name="supplierId">Supplier identifier.</param>
    /// <param name="productId">Optional product identifier to filter statistics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Price history statistics.</returns>
    Task<PriceHistoryStatistics> GetPriceHistoryStatisticsAsync(
        Guid supplierId,
        Guid? productId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets price trend data points for charting.
    /// </summary>
    /// <param name="supplierId">Supplier identifier.</param>
    /// <param name="productId">Product identifier.</param>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of price trend data points.</returns>
    Task<List<PriceTrendDataPoint>> GetPriceTrendDataAsync(
        Guid supplierId,
        Guid productId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);
}
