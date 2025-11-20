using EventForge.DTOs.Common;
using EventForge.DTOs.Warehouse;

namespace EventForge.Client.Services.Domain.Warehouse;

/// <summary>
/// Service for managing stock operations.
/// </summary>
public interface IStockService
{
    /// <summary>
    /// Gets all stock entries with optional pagination and filtering.
    /// </summary>
    Task<PagedResult<StockDto>?> GetStockAsync(
        int page = 1,
        int pageSize = 20,
        Guid? productId = null,
        Guid? locationId = null,
        Guid? lotId = null,
        bool? lowStock = null);

    /// <summary>
    /// Gets a stock entry by ID.
    /// </summary>
    Task<StockDto?> GetStockByIdAsync(Guid id);

    /// <summary>
    /// Gets stock entries by product ID.
    /// </summary>
    Task<IEnumerable<StockDto>> GetStockByProductIdAsync(Guid productId);
}
