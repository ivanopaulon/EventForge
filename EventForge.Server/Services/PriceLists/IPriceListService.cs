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
    /// Includes priority, validity dates, default price list handling, and BusinessParty-specific discounts.
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <param name="eventId">Event identifier</param>
    /// <param name="businessPartyId">BusinessParty identifier (optional, for partner-specific pricing)</param>
    /// <param name="evaluationDate">Date to evaluate price validity (default: current UTC)</param>
    /// <param name="quantity">Quantity for price tier evaluation (default: 1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Applied price information or null if no price found</returns>
    Task<AppliedPriceDto?> GetAppliedPriceAsync(
        Guid productId,
        Guid eventId,
        Guid? businessPartyId = null,
        DateTime? evaluationDate = null,
        int quantity = 1,
        CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Confronta i prezzi di acquisto per un prodotto da tutti i fornitori nei listini attivi.
    /// Ritorna lista ordinata per prezzo (migliore prima).
    /// </summary>
    /// <param name="productId">ID prodotto</param>
    /// <param name="quantity">Quantità per calcolo scaglioni</param>
    /// <param name="evaluationDate">Data valutazione (default: oggi)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lista confronto prezzi ordinata per prezzo crescente</returns>
    Task<List<PurchasePriceComparisonDto>> GetPurchasePriceComparisonAsync(
        Guid productId,
        int quantity = 1,
        DateTime? evaluationDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calcola il prezzo di un prodotto secondo la modalità specificata.
    /// </summary>
    Task<ProductPriceResultDto> GetProductPriceAsync(
        GetProductPriceRequestDto request,
        CancellationToken cancellationToken = default);

    // Phase 2A/2B - BusinessParty assignment methods
    Task<PriceListBusinessPartyDto> AssignBusinessPartyAsync(Guid priceListId, AssignBusinessPartyToPriceListDto dto, string currentUser, CancellationToken cancellationToken = default);
    Task<bool> RemoveBusinessPartyAsync(Guid priceListId, Guid businessPartyId, string currentUser, CancellationToken cancellationToken = default);
    Task<IEnumerable<PriceListBusinessPartyDto>> GetBusinessPartiesForPriceListAsync(Guid priceListId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PriceListDto>> GetPriceListsByTypeAsync(PriceListType type, CancellationToken cancellationToken = default);
    Task<IEnumerable<PriceListDto>> GetPriceListsByBusinessPartyAsync(Guid businessPartyId, PriceListType? type, CancellationToken cancellationToken = default);

    // Phase 2C - Price list duplication
    /// <summary>
    /// Duplica un listino esistente con opzioni di copia e trasformazione.
    /// </summary>
    /// <param name="sourcePriceListId">ID del listino da duplicare</param>
    /// <param name="dto">Opzioni di duplicazione</param>
    /// <param name="currentUser">Utente corrente</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dettagli del listino duplicato</returns>
    Task<DuplicatePriceListResultDto> DuplicatePriceListAsync(
        Guid sourcePriceListId,
        DuplicatePriceListDto dto,
        string currentUser,
        CancellationToken cancellationToken = default);

    // Bulk price update methods
    /// <summary>
    /// Anteprima aggiornamento massivo prezzi
    /// </summary>
    /// <param name="priceListId">ID del listino prezzi</param>
    /// <param name="dto">Parametri di aggiornamento massivo</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preview delle modifiche senza salvare</returns>
    Task<BulkUpdatePreviewDto> PreviewBulkUpdateAsync(
        Guid priceListId,
        BulkPriceUpdateDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Esegue aggiornamento massivo prezzi
    /// </summary>
    /// <param name="priceListId">ID del listino prezzi</param>
    /// <param name="dto">Parametri di aggiornamento massivo</param>
    /// <param name="currentUser">Utente corrente</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Risultato dell'aggiornamento con conteggi e errori</returns>
    Task<BulkUpdateResultDto> BulkUpdatePricesAsync(
        Guid priceListId,
        BulkPriceUpdateDto dto,
        string currentUser,
        CancellationToken cancellationToken = default);

    // Phase 2C - PR #4: Price list generation from purchase documents
    /// <summary>
    /// Preview generazione listino da documenti (senza salvataggio)
    /// </summary>
    Task<GeneratePriceListPreviewDto> PreviewGenerateFromPurchasesAsync(
        GeneratePriceListFromPurchasesDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Genera nuovo listino da documenti di acquisto
    /// </summary>
    Task<Guid> GenerateFromPurchasesAsync(
        GeneratePriceListFromPurchasesDto dto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Preview aggiornamento listino esistente
    /// </summary>
    Task<GeneratePriceListPreviewDto> PreviewUpdateFromPurchasesAsync(
        UpdatePriceListFromPurchasesDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggiorna listino esistente con prezzi da documenti
    /// </summary>
    Task<UpdatePriceListResultDto> UpdateFromPurchasesAsync(
        UpdatePriceListFromPurchasesDto dto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Genera nuovo listino dai prezzi DefaultPrice dei prodotti
    /// </summary>
    Task<Guid> GenerateFromProductPricesAsync(
        GeneratePriceListFromProductsDto dto,
        string currentUser,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applica i prezzi di un listino ai Product.DefaultPrice
    /// </summary>
    Task<ApplyPriceListResultDto> ApplyPriceListToProductsAsync(
        ApplyPriceListToProductsDto dto,
        string currentUser,
        CancellationToken cancellationToken = default);
}