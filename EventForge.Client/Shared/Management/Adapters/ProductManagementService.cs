using EventForge.Client.Services;
using Prym.DTOs.Common;
using Prym.DTOs.Products;

namespace EventForge.Client.Shared.Management.Adapters;

public class ProductManagementService : IEntityManagementService<ProductDto>
{
    private readonly IProductService _productService;

    public ProductManagementService(IProductService productService)
        => _productService = productService;

    public async Task<PagedResult<ProductDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
        => await _productService.GetProductsAsync(page, pageSize, null, ct) ?? new PagedResult<ProductDto>();

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await _productService.DeleteProductAsync(id, ct);
}
