using EventForge.Client.Services;
using EventForge.DTOs.Common;
using EventForge.DTOs.Products;

namespace EventForge.Client.Shared.Management.Adapters;

public class BrandManagementService : IEntityManagementService<BrandDto>
{
    private readonly IBrandService _brandService;

    public BrandManagementService(IBrandService brandService)
        => _brandService = brandService;

    public async Task<PagedResult<BrandDto>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
        => await _brandService.GetBrandsAsync(page, pageSize);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await _brandService.DeleteBrandAsync(id);
}
