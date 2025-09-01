using EventForge.DTOs.Warehouse;

namespace EventForge.Server.Services.Warehouse;

/// <summary>
/// Service interface for managing stock levels and inventory operations.
/// </summary>
public interface IStockService
{
    /// <summary>
    /// Gets all stock entries with optional pagination and filtering.
    /// </summary>
    Task<PagedResult<StockDto>> GetStockAsync(
        int page = 1,
        int pageSize = 20,
        Guid? productId = null,
        Guid? locationId = null,
        Guid? lotId = null,
        bool? lowStock = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a stock entry by ID.
    /// </summary>
    Task<StockDto?> GetStockByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stock entries by product ID.
    /// </summary>
    Task<IEnumerable<StockDto>> GetStockByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stock entries by location ID.
    /// </summary>
    Task<IEnumerable<StockDto>> GetStockByLocationIdAsync(Guid locationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available quantity for a product across all locations.
    /// </summary>
    Task<decimal> GetAvailableQuantityAsync(Guid productId, Guid? lotId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available quantity for a product at a specific location.
    /// </summary>
    Task<decimal> GetAvailableQuantityAtLocationAsync(Guid productId, Guid locationId, Guid? lotId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates stock entry.
    /// </summary>
    Task<StockDto> CreateOrUpdateStockAsync(CreateStockDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates stock levels (for inventory adjustments).
    /// </summary>
    Task<StockDto?> UpdateStockLevelsAsync(Guid id, UpdateStockDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reserves stock for a specific quantity.
    /// </summary>
    Task<bool> ReserveStockAsync(Guid productId, Guid locationId, decimal quantity, Guid? lotId = null, string? currentUser = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases reserved stock.
    /// </summary>
    Task<bool> ReleaseReservedStockAsync(Guid productId, Guid locationId, decimal quantity, Guid? lotId = null, string? currentUser = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stock entries that are below minimum levels.
    /// </summary>
    Task<IEnumerable<StockDto>> GetLowStockAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stock entries that are above maximum levels.
    /// </summary>
    Task<IEnumerable<StockDto>> GetOverstockAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a stock entry.
    /// </summary>
    Task<bool> DeleteStockAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);
}