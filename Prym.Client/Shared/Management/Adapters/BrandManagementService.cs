using Prym.Client.Services;
using Prym.DTOs.Common;
using Prym.DTOs.Products;

namespace Prym.Client.Shared.Management.Adapters;

public class BrandManagementService : IEntityManagementService<BrandDto>
{
    private readonly IBrandService _brandService;

    public BrandManagementService(IBrandService brandService)
        => _brandService = brandService;

    public async Task<PagedResult<BrandDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
        => await _brandService.GetBrandsAsync(page, pageSize, ct);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await _brandService.DeleteBrandAsync(id, ct);
}
