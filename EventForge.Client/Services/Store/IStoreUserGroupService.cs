using EventForge.DTOs.Common;
using EventForge.DTOs.Store;

namespace EventForge.Client.Services.Store;

/// <summary>
/// Client service interface for managing store user groups.
/// </summary>
public interface IStoreUserGroupService
{
    /// <summary>
    /// Gets all store user groups.
    /// </summary>
    Task<List<StoreUserGroupDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a store user group by ID.
    /// </summary>
    Task<StoreUserGroupDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new store user group.
    /// </summary>
    Task<StoreUserGroupDto?> CreateAsync(CreateStoreUserGroupDto createDto, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing store user group.
    /// </summary>
    Task<StoreUserGroupDto?> UpdateAsync(Guid id, UpdateStoreUserGroupDto updateDto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a store user group.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets store user groups with pagination.
    /// </summary>
    Task<PagedResult<StoreUserGroupDto>> GetPagedAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
}
