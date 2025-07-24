using EventForge.Models.Business;

namespace EventForge.Services.Business;

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
    Task<IEnumerable<BusinessPartyDto>> GetBusinessPartiesByTypeAsync(BusinessPartyType partyType, CancellationToken cancellationToken = default);

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
}