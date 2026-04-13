using Prym.DTOs.Common;
using Prym.DTOs.Store;

namespace EventForge.Client.Services.Store;

/// <summary>
/// Client service interface for managing store users.
/// </summary>
public interface IStoreUserService
{
    /// <summary>
    /// Gets all store users.
    /// </summary>
    Task<List<StoreUserDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a store user by ID.
    /// </summary>
    Task<StoreUserDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets a store user by username.
    /// </summary>
    Task<StoreUserDto?> GetByUsernameAsync(string username, CancellationToken ct = default);

    /// <summary>
    /// Creates a new store user.
    /// </summary>
    Task<StoreUserDto?> CreateAsync(CreateStoreUserDto createDto, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing store user.
    /// </summary>
    Task<StoreUserDto?> UpdateAsync(Guid id, UpdateStoreUserDto updateDto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a store user.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets store users with pagination.
    /// </summary>
    Task<PagedResult<StoreUserDto>> GetPagedAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);

    /// <summary>
    /// Gets all store operators that have a date of birth set. Used for birthday tracking.
    /// </summary>
    Task<IEnumerable<StoreUserDto>> GetWithBirthdayAsync(CancellationToken ct = default);
}
