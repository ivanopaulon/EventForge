using EventForge.Server.DTOs.Common;

namespace EventForge.Server.Services.Common;

/// <summary>
/// Service interface for managing references.
/// </summary>
public interface IReferenceService
{
    /// <summary>
    /// Gets all references with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of references</returns>
    Task<PagedResult<ReferenceDto>> GetReferencesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets references by owner ID.
    /// </summary>
    /// <param name="ownerId">Owner ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of references for the owner</returns>
    Task<IEnumerable<ReferenceDto>> GetReferencesByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a reference by ID.
    /// </summary>
    /// <param name="id">Reference ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reference DTO or null if not found</returns>
    Task<ReferenceDto?> GetReferenceByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new reference.
    /// </summary>
    /// <param name="createReferenceDto">Reference creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created reference DTO</returns>
    Task<ReferenceDto> CreateReferenceAsync(CreateReferenceDto createReferenceDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing reference.
    /// </summary>
    /// <param name="id">Reference ID</param>
    /// <param name="updateReferenceDto">Reference update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated reference DTO or null if not found</returns>
    Task<ReferenceDto?> UpdateReferenceAsync(Guid id, UpdateReferenceDto updateReferenceDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a reference (soft delete).
    /// </summary>
    /// <param name="id">Reference ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteReferenceAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a reference exists.
    /// </summary>
    /// <param name="referenceId">Reference ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ReferenceExistsAsync(Guid referenceId, CancellationToken cancellationToken = default);
}