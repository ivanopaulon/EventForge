using Prym.DTOs.Sales;

namespace EventForge.Client.Services.Sales;

/// <summary>
/// Client service for managing note flags.
/// </summary>
public interface INoteFlagService
{
    /// <summary>
    /// Gets all note flags.
    /// </summary>
    Task<List<NoteFlagDto>?> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets only active note flags.
    /// </summary>
    Task<List<NoteFlagDto>?> GetActiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a note flag by ID.
    /// </summary>
    Task<NoteFlagDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new note flag.
    /// </summary>
    Task<NoteFlagDto?> CreateAsync(CreateNoteFlagDto createDto, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing note flag.
    /// </summary>
    Task<NoteFlagDto?> UpdateAsync(Guid id, UpdateNoteFlagDto updateDto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a note flag.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
