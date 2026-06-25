using Prym.DTOs.Common;
using Prym.DTOs.Products;
using Prym.Web.Services;

namespace Prym.Web.Shared.Management.Adapters;

public class ProductManagementService : IEntityManagementService<ProductDto>
{
    private readonly IProductService _productService;

    public ProductManagementService(IProductService productService)
        => _productService = productService;

    public async Task<PagedResult<ProductDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, Dictionary<string, object?>? filters = null, CancellationToken ct = default)
    {
        Guid? classificationNodeId = null;
        bool includeInactive = true; // Management page always shows all products (including inactive)
        if (filters != null)
        {
            if (filters.TryGetValue("ClassificationNodeId", out var rawId) && rawId is Guid guid)
                classificationNodeId = guid;
            if (filters.TryGetValue("IncludeInactive", out var rawInactive) && rawInactive is bool inactive)
                includeInactive = inactive;
        }

        return await _productService.GetProductsAsync(page, pageSize, searchTerm, classificationNodeId, includeInactive, ct) ?? new PagedResult<ProductDto>();
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await _productService.DeleteProductAsync(id, ct);
}
