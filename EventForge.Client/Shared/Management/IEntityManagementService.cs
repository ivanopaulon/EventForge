using EventForge.DTOs.Common;

namespace EventForge.Client.Shared.Management;

public interface IEntityManagementService<TEntity> where TEntity : class
{
    /// <summary>
    /// Retrieves a paged result. When <paramref name="searchTerm"/> or <paramref name="filters"/> are provided
    /// and <see cref="EntityManagementConfig{TEntity}.UseServerSidePaging"/> is true, implementations should
    /// forward them to the server. Default implementations may ignore them (client-side filtering).
    /// </summary>
    Task<PagedResult<TEntity>> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        Dictionary<string, object?>? filters = null,
        CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Toggles the active status of the entity with the given <paramref name="id"/>.
    /// The default implementation throws <see cref="NotSupportedException"/>.
    /// Override this method in adapters that support status toggling.
    /// </summary>
    Task ToggleStatusAsync(Guid id, bool newStatus, CancellationToken ct = default)
        => throw new NotSupportedException();
}
