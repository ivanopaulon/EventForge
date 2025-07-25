using EventForge.Server.DTOs.PriceLists;

namespace EventForge.Server.Services.PriceLists;

/// <summary>
/// Service interface for managing price lists and price list entries.
/// </summary>
public interface IPriceListService
{
    // PriceList CRUD operations
    Task<PagedResult<PriceListDto>> GetPriceListsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<IEnumerable<PriceListDto>> GetPriceListsByEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<PriceListDto?> GetPriceListByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PriceListDetailDto?> GetPriceListDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PriceListDto> CreatePriceListAsync(CreatePriceListDto createPriceListDto, string currentUser, CancellationToken cancellationToken = default);
    Task<PriceListDto?> UpdatePriceListAsync(Guid id, UpdatePriceListDto updatePriceListDto, string currentUser, CancellationToken cancellationToken = default);
    Task<bool> DeletePriceListAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    // PriceListEntry management operations
    Task<IEnumerable<PriceListEntryDto>> GetPriceListEntriesAsync(Guid priceListId, CancellationToken cancellationToken = default);
    Task<PriceListEntryDto?> GetPriceListEntryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PriceListEntryDto> AddPriceListEntryAsync(CreatePriceListEntryDto createPriceListEntryDto, string currentUser, CancellationToken cancellationToken = default);
    Task<PriceListEntryDto?> UpdatePriceListEntryAsync(Guid id, UpdatePriceListEntryDto updatePriceListEntryDto, string currentUser, CancellationToken cancellationToken = default);
    Task<bool> RemovePriceListEntryAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    // Helper methods
    Task<bool> PriceListExistsAsync(Guid priceListId, CancellationToken cancellationToken = default);
    Task<bool> EventExistsAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> ProductExistsAsync(Guid productId, CancellationToken cancellationToken = default);
}