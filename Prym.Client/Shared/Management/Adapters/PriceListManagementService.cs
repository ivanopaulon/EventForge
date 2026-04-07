using Prym.Client.Services;
using Prym.DTOs.Common;
using Prym.DTOs.PriceLists;

namespace Prym.Client.Shared.Management.Adapters;

public class PriceListManagementService : IEntityManagementService<PriceListDto>
{
    private readonly IPriceListService _priceListService;

    public PriceListManagementService(IPriceListService priceListService)
        => _priceListService = priceListService;

    public async Task<PagedResult<PriceListDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
        => await _priceListService.GetPagedAsync(page, pageSize, ct);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var success = await _priceListService.DeleteAsync(id, ct);
        if (!success)
            throw new InvalidOperationException($"Failed to delete price list {id}");
    }
}
