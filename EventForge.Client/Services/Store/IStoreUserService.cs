using EventForge.DTOs.Store;

namespace EventForge.Client.Services.Store;

/// <summary>
/// Client service interface for managing store users.
/// </summary>
public interface IStoreUserService
{
    /// <summary>
    /// Gets all store users.
    /// </summary>
    Task<List<StoreUserDto>> GetAllAsync();

    /// <summary>
    /// Gets a store user by ID.
    /// </summary>
    Task<StoreUserDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a store user by username.
    /// </summary>
    Task<StoreUserDto?> GetByUsernameAsync(string username);

    /// <summary>
    /// Creates a new store user.
    /// </summary>
    Task<StoreUserDto?> CreateAsync(CreateStoreUserDto createDto);

    /// <summary>
    /// Updates an existing store user.
    /// </summary>
    Task<StoreUserDto?> UpdateAsync(Guid id, UpdateStoreUserDto updateDto);

    /// <summary>
    /// Deletes a store user.
    /// </summary>
    Task<bool> DeleteAsync(Guid id);
}
