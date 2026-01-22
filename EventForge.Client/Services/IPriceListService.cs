using EventForge.DTOs.Common;
using EventForge.DTOs.PriceLists;

namespace EventForge.Client.Services;

/// <summary>
/// Service interface for managing price lists.
/// </summary>
public interface IPriceListService
{
    /// <summary>
    /// Gets price lists with pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of price lists</returns>
    Task<PagedResult<PriceListDto>> GetPagedAsync(int page, int pageSize, CancellationToken ct);

    /// <summary>
    /// Gets a price list by ID.
    /// </summary>
    /// <param name="id">Price list ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Price list DTO or null if not found</returns>
    Task<PriceListDto?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Gets detailed information about a price list including entries.
    /// </summary>
    /// <param name="id">Price list ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Price list detail DTO or null if not found</returns>
    Task<PriceListDetailDto?> GetDetailAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Creates a new price list.
    /// </summary>
    /// <param name="dto">Price list creation data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created price list DTO</returns>
    Task<PriceListDto> CreateAsync(CreatePriceListDto dto, CancellationToken ct);

    /// <summary>
    /// Updates an existing price list.
    /// </summary>
    /// <param name="id">Price list ID</param>
    /// <param name="dto">Price list update data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated price list DTO or null if not found</returns>
    Task<PriceListDto?> UpdateAsync(Guid id, UpdatePriceListDto dto, CancellationToken ct);

    /// <summary>
    /// Deletes a price list.
    /// </summary>
    /// <param name="id">Price list ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Gets all price lists for a specific event.
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of price lists</returns>
    Task<IEnumerable<PriceListDto>> GetByEventAsync(Guid eventId, CancellationToken ct);

    /// <summary>
    /// Previews price list generation from purchase documents without saving.
    /// </summary>
    /// <param name="dto">Generation parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Preview of the price list to be generated</returns>
    Task<GeneratePriceListPreviewDto> PreviewGenerateFromPurchasesAsync(GeneratePriceListFromPurchasesDto dto, CancellationToken ct);

    /// <summary>
    /// Generates and saves a price list from purchase documents.
    /// </summary>
    /// <param name="dto">Generation parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>ID of the generated price list</returns>
    Task<Guid> GenerateFromPurchasesAsync(GeneratePriceListFromPurchasesDto dto, CancellationToken ct);
}
