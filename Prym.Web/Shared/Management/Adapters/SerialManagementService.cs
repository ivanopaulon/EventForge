using Prym.DTOs.Common;
using Prym.DTOs.Warehouse;
using Prym.Web.Services;

namespace Prym.Web.Shared.Management.Adapters;

/// <summary>
/// Adapter that wraps <see cref="ISerialService"/> for use with
/// <see cref="EntityManagementPage{TEntity}"/>.
/// </summary>
public class SerialManagementService : IEntityManagementService<SerialDto>
{
    private readonly ISerialService _serialService;

    public SerialManagementService(ISerialService serialService)
        => _serialService = serialService;

    public async Task<PagedResult<SerialDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
    {
        var status = filters != null && filters.TryGetValue("status", out var statusValue) ? statusValue as string : null;
        var result = await _serialService.GetSerialsAsync(page, pageSize, status: status, searchTerm: searchTerm, ct: ct);
        return result ?? new PagedResult<SerialDto> { Items = [], TotalCount = 0, Page = page, PageSize = pageSize };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await _serialService.DeleteSerialAsync(id, ct);
}
