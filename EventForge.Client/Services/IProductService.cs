using EventForge.DTOs.Products;

namespace EventForge.Client.Services;

/// <summary>
/// Service for managing products.
/// </summary>
public interface IProductService
{
    Task<ProductDto?> GetProductByCodeAsync(string code);
    Task<ProductDto?> GetProductByIdAsync(Guid id);
}
