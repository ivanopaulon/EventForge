using Prym.DTOs.Common;
using Prym.DTOs.Warehouse;
using Prym.Web.Services;

namespace Prym.Web.Shared.Management.Adapters;

/// <summary>
/// Adapter that wraps <see cref="ISerialService"/> for use with
/// <see cref="EntityManagementPage{TEntity}"/>.
/// All serials are loaded client-side so that filtering, search and quick-filters
/// operate without additional server round-trips.
/// </summary>
public class SerialManagementService : IEntityManagementService<SerialDto>
{
    private readonly ISerialService _serialService;

    public SerialManagementService(ISerialService serialService)
        => _serialService = serialService;

    public async Task<PagedResult<SerialDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
    {
        var result = await _serialService.GetSerialsAsync(1, int.MaxValue, ct: ct);
        var list = result?.Items?.ToList() ?? new List<SerialDto>();
        return new PagedResult<SerialDto> { Items = list, TotalCount = list.Count, Page = 1, PageSize = list.Count };
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await _serialService.DeleteSerialAsync(id, ct);
}
