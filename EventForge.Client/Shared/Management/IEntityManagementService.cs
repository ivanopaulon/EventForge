using EventForge.DTOs.Common;

namespace EventForge.Client.Shared.Management;

public interface IEntityManagementService<TEntity> where TEntity : class
{
    Task<PagedResult<TEntity>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task ToggleStatusAsync(Guid id, bool newStatus, CancellationToken ct = default)
        => throw new NotSupportedException();
}
