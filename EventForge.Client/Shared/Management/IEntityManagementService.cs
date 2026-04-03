using EventForge.DTOs.Common;

namespace EventForge.Client.Shared.Management;

public interface IEntityManagementService<TEntity> where TEntity : class
{
    Task<PagedResult<TEntity>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Toggles the active status of the entity with the given <paramref name="id"/>.
    /// The default implementation throws <see cref="NotSupportedException"/>.
    /// Override this method in adapters that support status toggling.
    /// </summary>
    Task ToggleStatusAsync(Guid id, bool newStatus, CancellationToken ct = default)
        => throw new NotSupportedException();
}
