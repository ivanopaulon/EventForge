using EventForge.DTOs.Sales;

namespace EventForge.Server.Services.Sales;

/// <summary>
/// Service interface for managing note flags.
/// </summary>
public interface INoteFlagService
{
    /// <summary>
    /// Gets all note flags.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of note flags</returns>
    Task<List<NoteFlagDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets only active note flags.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active note flags</returns>
    Task<List<NoteFlagDto>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a note flag by ID.
    /// </summary>
    /// <param name="id">Note flag ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Note flag DTO or null if not found</returns>
    Task<NoteFlagDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new note flag.
    /// </summary>
    /// <param name="createDto">Note flag creation data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created note flag DTO</returns>
    Task<NoteFlagDto> CreateAsync(CreateNoteFlagDto createDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a note flag.
    /// </summary>
    /// <param name="id">Note flag ID</param>
    /// <param name="updateDto">Note flag update data</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated note flag DTO or null if not found</returns>
    Task<NoteFlagDto?> UpdateAsync(Guid id, UpdateNoteFlagDto updateDto, string currentUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a note flag (soft delete).
    /// </summary>
    /// <param name="id">Note flag ID</param>
    /// <param name="currentUser">Current user name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, string currentUser, CancellationToken cancellationToken = default);
}
