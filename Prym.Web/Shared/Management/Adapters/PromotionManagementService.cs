using Prym.Web.Services;
using Prym.DTOs.Common;
using Prym.DTOs.Promotions;

namespace Prym.Web.Shared.Management.Adapters;

public class PromotionManagementService : IEntityManagementService<PromotionDto>
{
    private readonly IPromotionClientService _promotionService;

    public PromotionManagementService(IPromotionClientService promotionService)
        => _promotionService = promotionService;

    public async Task<PagedResult<PromotionDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
        => await _promotionService.GetPagedAsync(page, pageSize, ct);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var success = await _promotionService.DeleteAsync(id, ct);
        if (!success)
            throw new InvalidOperationException($"Failed to delete promotion {id}");
    }
}
