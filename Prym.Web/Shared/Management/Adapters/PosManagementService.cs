using Prym.Web.Services.Store;
using Prym.DTOs.Common;
using Prym.DTOs.Store;

namespace Prym.Web.Shared.Management.Adapters;

public class PosManagementService : IEntityManagementService<StorePosDto>
{
    private readonly IStorePosService _storePosService;

    public PosManagementService(IStorePosService storePosService)
        => _storePosService = storePosService;

    public async Task<PagedResult<StorePosDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
        => await _storePosService.GetPagedAsync(page, pageSize);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var success = await _storePosService.DeleteAsync(id);
        if (!success)
            throw new InvalidOperationException($"Failed to delete POS {id}");
    }
}
