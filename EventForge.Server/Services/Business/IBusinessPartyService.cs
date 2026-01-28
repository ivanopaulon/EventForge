using EventForge.DTOs.Business;

namespace EventForge.Server.Services.Business;

/// <summary>
/// Service interface for managing business parties and their accounting data.
/// </summary>
public interface IBusinessPartyService
{
    // BusinessParty CRUD operations

    /// <summary>
    /// Gets all business parties with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of business parties</returns>
    Task<PagedResult<BusinessPartyDto>> GetBusinessPartiesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a business party by ID.
    /// </summary>
    /// <param name="id">Business party ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Business party DTO or null if not found</returns>
    Task<BusinessPartyDto?> GetBusinessPartyByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets business parties by type.
    /// </summary>
    /// <param name="partyType">Business party type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of business parties of the specified type</returns>
    Task<IEnumerable<BusinessPartyDto>> GetBusinessPartiesByTypeAsync(DTOs.Common.BusinessPartyType partyType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches business parties by name or tax code.
    /// </summary>
    /// <param name="searchTerm">Search term to match against name or tax code</param>
    /// <param name="partyType">Optional filter by business party type</param>
    /// <param name="pageSize">Maximum number of results to return (default 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of matching business parties</returns>
    Task<IEnumerable<BusinessPartyDto>> SearchBusinessPartiesAsync(string searchTerm, DTOs.Common.BusinessPartyType? partyType = null, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new business party.
    /// </summary>
    /// <param name="createBusinessPartyDto">Business party creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created business party DTO</returns>
    Task<BusinessPartyDto> CreateBusinessPartyAsync(CreateBusinessPartyDto createBusinessPartyDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing business party.
    /// </summary>
    /// <param name="id">Business party ID</param>
    /// <param name="updateBusinessPartyDto">Business party update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated business party DTO or null if not found</returns>
    Task<BusinessPartyDto?> UpdateBusinessPartyAsync(Guid id, UpdateBusinessPartyDto updateBusinessPartyDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a business party (soft delete).
    /// </summary>
    /// <param name="id">Business party ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteBusinessPartyAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupera tutti i dettagli completi di un BusinessParty in una singola query ottimizzata.
    /// Include: dati base, contatti, indirizzi, listini, statistiche aggregate.
    /// Ottimizzazione FASE 5 per ridurre N+1 queries.
    /// </summary>
    /// <param name="id">BusinessParty ID</param>
    /// <param name="includeInactive">Se true, include anche contatti/indirizzi con IsDeleted=false ma inattivi</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>DTO aggregato con tutti i dati, null se non trovato</returns>
    Task<BusinessPartyFullDetailDto?> GetFullDetailAsync(
        Guid id, 
        bool includeInactive = false, 
        CancellationToken cancellationToken = default);

    // BusinessPartyAccounting CRUD operations

    /// <summary>
    /// Gets all business party accounting records with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of business party accounting records</returns>
    Task<PagedResult<BusinessPartyAccountingDto>> GetBusinessPartyAccountingAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a business party accounting record by ID.
    /// </summary>
    /// <param name="id">Business party accounting ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Business party accounting DTO or null if not found</returns>
    Task<BusinessPartyAccountingDto?> GetBusinessPartyAccountingByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets business party accounting by business party ID.
    /// </summary>
    /// <param name="businessPartyId">Business party ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Business party accounting DTO or null if not found</returns>
    Task<BusinessPartyAccountingDto?> GetBusinessPartyAccountingByBusinessPartyIdAsync(Guid businessPartyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new business party accounting record.
    /// </summary>
    /// <param name="createBusinessPartyAccountingDto">Business party accounting creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created business party accounting DTO</returns>
    Task<BusinessPartyAccountingDto> CreateBusinessPartyAccountingAsync(CreateBusinessPartyAccountingDto createBusinessPartyAccountingDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing business party accounting record.
    /// </summary>
    /// <param name="id">Business party accounting ID</param>
    /// <param name="updateBusinessPartyAccountingDto">Business party accounting update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated business party accounting DTO or null if not found</returns>
    Task<BusinessPartyAccountingDto?> UpdateBusinessPartyAccountingAsync(Guid id, UpdateBusinessPartyAccountingDto updateBusinessPartyAccountingDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a business party accounting record (soft delete).
    /// </summary>
    /// <param name="id">Business party accounting ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteBusinessPartyAccountingAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a business party exists.
    /// </summary>
    /// <param name="businessPartyId">Business party ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> BusinessPartyExistsAsync(Guid businessPartyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents for a business party with optional filters and pagination.
    /// </summary>
    /// <param name="businessPartyId">Business party ID</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="documentTypeId">Optional document type filter</param>
    /// <param name="searchNumber">Optional number/series search</param>
    /// <param name="approvalStatus">Optional approval status filter</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of document headers</returns>
    Task<PagedResult<EventForge.DTOs.Documents.DocumentHeaderDto>> GetBusinessPartyDocumentsAsync(
        Guid businessPartyId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Guid? documentTypeId = null,
        string? searchNumber = null,
        DTOs.Common.ApprovalStatus? approvalStatus = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets product analysis aggregated data for a business party.
    /// </summary>
    /// <param name="businessPartyId">Business party ID</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="type">Filter by transaction type: 'purchase', 'sale', or null for both</param>
    /// <param name="topN">Limit results to top N by value</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="sortBy">Sort field (default: ValuePurchased)</param>
    /// <param name="sortDescending">Sort direction</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of product analysis data</returns>
    Task<PagedResult<BusinessPartyProductAnalysisDto>> GetBusinessPartyProductAnalysisAsync(
        Guid businessPartyId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? type = null,
        int? topN = null,
        int page = 1,
        int pageSize = 20,
        string? sortBy = null,
        bool sortDescending = true,
        CancellationToken cancellationToken = default);
}