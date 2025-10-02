using EventForge.DTOs.Sales;

namespace EventForge.Client.Services.Sales;

/// <summary>
/// Client service for managing note flags.
/// </summary>
public interface INoteFlagService
{
    /// <summary>
    /// Gets all note flags.
    /// </summary>
    Task<List<NoteFlagDto>?> GetAllAsync();

    /// <summary>
    /// Gets only active note flags.
    /// </summary>
    Task<List<NoteFlagDto>?> GetActiveAsync();

    /// <summary>
    /// Gets a note flag by ID.
    /// </summary>
    Task<NoteFlagDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new note flag.
    /// </summary>
    Task<NoteFlagDto?> CreateAsync(CreateNoteFlagDto createDto);

    /// <summary>
    /// Updates an existing note flag.
    /// </summary>
    Task<NoteFlagDto?> UpdateAsync(Guid id, UpdateNoteFlagDto updateDto);

    /// <summary>
    /// Deletes a note flag.
    /// </summary>
    Task<bool> DeleteAsync(Guid id);
}
