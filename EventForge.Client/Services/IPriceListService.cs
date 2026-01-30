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
    /// Gets price lists with pagination and optional filtering by direction and status.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="direction">Optional filter by price list direction (Input/Output)</param>
    /// <param name="status">Optional filter by price list status (Active/Suspended/Deleted)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of filtered price lists</returns>
    Task<PagedResult<PriceListDto>> GetPagedAsync(int page, int pageSize, PriceListDirection? direction = null, PriceListStatus? status = null, CancellationToken ct = default);

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

    /// <summary>
    /// Previews price list generation from product default prices without saving.
    /// </summary>
    /// <param name="dto">Generation parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Preview of the price list to be generated</returns>
    Task<GeneratePriceListPreviewDto> PreviewGenerateFromDefaultPricesAsync(GenerateFromDefaultPricesDto dto, CancellationToken ct);

    /// <summary>
    /// Generates and saves a price list from product default prices.
    /// </summary>
    /// <param name="dto">Generation parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>ID of the generated price list</returns>
    Task<Guid> GenerateFromDefaultPricesAsync(GenerateFromDefaultPricesDto dto, CancellationToken ct);

    /// <summary>
    /// Adds a single entry to a price list.
    /// </summary>
    Task<PriceListEntryDto> AddEntryAsync(CreatePriceListEntryDto dto, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing price list entry.
    /// </summary>
    Task<PriceListEntryDto> UpdateEntryAsync(Guid id, UpdatePriceListEntryDto dto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a price list entry.
    /// </summary>
    Task<bool> DeleteEntryAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Bulk add entries (for multi-select dialog - future PR).
    /// </summary>
    Task<int> AddEntriesBulkAsync(List<CreatePriceListEntryDto> entries, CancellationToken ct = default);

    /// <summary>
    /// Assegna un BusinessParty al listino con configurazione specifica.
    /// </summary>
    /// <param name="priceListId">ID del listino prezzi</param>
    /// <param name="dto">Dati di assegnazione del BusinessParty</param>
    /// <param name="ct">Cancellation token</param>
    Task AssignBusinessPartyAsync(Guid priceListId, AssignBusinessPartyToPriceListDto dto, CancellationToken ct = default);

    /// <summary>
    /// Rimuove l'assegnazione di un BusinessParty dal listino.
    /// </summary>
    /// <param name="priceListId">ID del listino prezzi</param>
    /// <param name="businessPartyId">ID del BusinessParty da rimuovere</param>
    /// <param name="ct">Cancellation token</param>
    Task UnassignBusinessPartyAsync(Guid priceListId, Guid businessPartyId, CancellationToken ct = default);

    /// <summary>
    /// Ottiene tutti i BusinessParty assegnati a un listino.
    /// </summary>
    /// <param name="priceListId">ID del listino prezzi</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Elenco dei BusinessParty assegnati</returns>
    Task<IEnumerable<PriceListBusinessPartyDto>> GetAssignedBusinessPartiesAsync(Guid priceListId, CancellationToken ct = default);

    /// <summary>
    /// Aggiorna la configurazione di un BusinessParty assegnato.
    /// </summary>
    /// <param name="priceListId">ID del listino prezzi</param>
    /// <param name="businessPartyId">ID del BusinessParty</param>
    /// <param name="dto">Dati di aggiornamento</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>BusinessParty aggiornato</returns>
    Task<PriceListBusinessPartyDto> UpdateBusinessPartyAssignmentAsync(Guid priceListId, Guid businessPartyId, UpdateBusinessPartyAssignmentDto dto, CancellationToken ct = default);

    /// <summary>
    /// Preview bulk price update operation without applying changes.
    /// </summary>
    /// <param name="priceListId">Price list ID</param>
    /// <param name="dto">Bulk update parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Preview of the changes that would be applied</returns>
    Task<BulkUpdatePreviewDto> PreviewBulkUpdateAsync(Guid priceListId, BulkPriceUpdateDto dto, CancellationToken ct = default);

    /// <summary>
    /// Apply bulk price update operation.
    /// </summary>
    /// <param name="priceListId">Price list ID</param>
    /// <param name="dto">Bulk update parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result of the bulk update operation</returns>
    Task<BulkUpdateResultDto> BulkUpdatePricesAsync(Guid priceListId, BulkPriceUpdateDto dto, CancellationToken ct = default);

    /// <summary>
    /// Bulk import price list entries from a list.
    /// </summary>
    /// <param name="priceListId">Price list ID</param>
    /// <param name="entries">List of entries to import</param>
    /// <param name="replaceExisting">Whether to replace existing entries</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result of the import operation</returns>
    Task<BulkImportResultDto> BulkImportEntriesAsync(Guid priceListId, List<CreatePriceListEntryDto> entries, bool replaceExisting, CancellationToken ct = default);

    /// <summary>
    /// Export price list entries to a list suitable for Excel export.
    /// </summary>
    /// <param name="priceListId">Price list ID</param>
    /// <param name="includeInactive">Whether to include inactive entries</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of exportable entries</returns>
    Task<List<ExportablePriceListEntryDto>> ExportEntriesAsync(Guid priceListId, bool includeInactive, CancellationToken ct = default);

    /// <summary>
    /// Duplicate an existing price list with optional transformations.
    /// </summary>
    /// <param name="priceListId">Source price list ID</param>
    /// <param name="dto">Duplication parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result of the duplication operation</returns>
    Task<DuplicatePriceListResultDto> DuplicatePriceListAsync(Guid priceListId, DuplicatePriceListDto dto, CancellationToken ct = default);

    /// <summary>
    /// Gets active price lists filtered by direction
    /// </summary>
    /// <param name="direction">Price list direction (Input for purchases, Output for sales)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of active price lists for the specified direction</returns>
    Task<List<PriceListDto>> GetActivePriceListsAsync(PriceListDirection direction, CancellationToken ct = default);

    /// <summary>
    /// Gets all price lists assigned to a specific business party.
    /// </summary>
    /// <param name="businessPartyId">Business party ID</param>
    /// <param name="type">Optional filter by price list type (Sales/Purchase)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of assigned price lists</returns>
    Task<IEnumerable<PriceListDto>> GetPriceListsByBusinessPartyAsync(
        Guid businessPartyId,
        PriceListType? type = null,
        CancellationToken ct = default);
}
