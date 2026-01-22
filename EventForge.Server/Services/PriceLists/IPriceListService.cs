using EventForge.DTOs.PriceLists;

namespace EventForge.Server.Services.PriceLists;

/// <summary>
/// Service interface for managing price lists and price list entries.
/// Enhanced with Issue #245 optimizations for precedence, unit conversion, and performance.
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

    // Enhanced price calculation methods (Issue #245)
    /// <summary>
    /// Gets the effective price for a product considering all applicable price lists with precedence logic.
    /// Includes priority, validity dates, and default price list handling.
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <param name="eventId">Event identifier</param>
    /// <param name="evaluationDate">Date to evaluate price validity (default: current UTC)</param>
    /// <param name="quantity">Quantity for price tier evaluation (default: 1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Applied price information or null if no price found</returns>
    Task<AppliedPriceDto?> GetAppliedPriceAsync(Guid productId, Guid eventId, DateTime? evaluationDate = null, int quantity = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the effective price for a product in a specific unit of measure with automatic conversion.
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <param name="eventId">Event identifier</param>
    /// <param name="targetUnitId">Target unit of measure identifier</param>
    /// <param name="evaluationDate">Date to evaluate price validity (default: current UTC)</param>
    /// <param name="quantity">Quantity for price tier evaluation (default: 1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Applied price information with unit conversion or null if no price found</returns>
    Task<AppliedPriceDto?> GetAppliedPriceWithUnitConversionAsync(Guid productId, Guid eventId, Guid targetUnitId, DateTime? evaluationDate = null, int quantity = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets price history for a product across all applicable price lists.
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <param name="eventId">Event identifier</param>
    /// <param name="fromDate">Start date for history (optional)</param>
    /// <param name="toDate">End date for history (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Price history entries ordered by date</returns>
    Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAsync(Guid productId, Guid eventId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk imports price list entries from a collection with validation.
    /// </summary>
    /// <param name="priceListId">Target price list identifier</param>
    /// <param name="entries">Collection of price list entries to import</param>
    /// <param name="currentUser">Current user performing the import</param>
    /// <param name="replaceExisting">Whether to replace existing entries for the same products</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk import result with success/failure counts and validation errors</returns>
    Task<BulkImportResultDto> BulkImportPriceListEntriesAsync(Guid priceListId, IEnumerable<CreatePriceListEntryDto> entries, string currentUser, bool replaceExisting = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports price list entries to a structured format for bulk operations.
    /// </summary>
    /// <param name="priceListId">Price list identifier</param>
    /// <param name="includeInactiveEntries">Whether to include inactive entries</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exportable price list entries</returns>
    Task<IEnumerable<ExportablePriceListEntryDto>> ExportPriceListEntriesAsync(Guid priceListId, bool includeInactiveEntries = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates price list precedence rules and identifies any conflicts.
    /// </summary>
    /// <param name="eventId">Event identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with any identified precedence issues</returns>
    Task<PrecedenceValidationResultDto> ValidatePriceListPrecedenceAsync(Guid eventId, CancellationToken cancellationToken = default);

    // ===== GESTIONE BUSINESSPARTY =====

    /// <summary>
    /// Assegna un BusinessParty a un PriceList con configurazioni specifiche.
    /// </summary>
    /// <param name="priceListId">ID del listino</param>
    /// <param name="dto">Dati assegnazione</param>
    /// <param name="currentUser">Utente corrente</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Relazione creata</returns>
    Task<PriceListBusinessPartyDto> AssignBusinessPartyAsync(
        Guid priceListId,
        AssignBusinessPartyToPriceListDto dto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rimuove (soft delete) un BusinessParty da un PriceList.
    /// </summary>
    /// <param name="priceListId">ID del listino</param>
    /// <param name="businessPartyId">ID del BusinessParty</param>
    /// <param name="currentUser">Utente corrente</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True se rimosso con successo</returns>
    Task<bool> RemoveBusinessPartyAsync(
        Guid priceListId,
        Guid businessPartyId,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene tutti i BusinessParty assegnati a un PriceList.
    /// </summary>
    /// <param name="priceListId">ID del listino</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lista BusinessParty assegnati</returns>
    Task<IEnumerable<PriceListBusinessPartyDto>> GetBusinessPartiesForPriceListAsync(
        Guid priceListId,
        CancellationToken cancellationToken = default);

    // ===== QUERY AVANZATE =====

    /// <summary>
    /// Ottiene i listini filtrati per tipo (Sales/Purchase).
    /// </summary>
    /// <param name="type">Tipo listino</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Listini del tipo specificato</returns>
    Task<IEnumerable<PriceListDto>> GetPriceListsByTypeAsync(
        PriceListType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene tutti i listini assegnati a un BusinessParty.
    /// </summary>
    /// <param name="businessPartyId">ID del BusinessParty</param>
    /// <param name="type">Tipo listino (opzionale, null = tutti)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Listini assegnati al BusinessParty</returns>
    Task<IEnumerable<PriceListDto>> GetPriceListsByBusinessPartyAsync(
        Guid businessPartyId,
        PriceListType? type = null,
        CancellationToken cancellationToken = default);
}