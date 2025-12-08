using System;
using System.Threading.Tasks;
using EventForge.DTOs.Products;

namespace EventForge.Client.Services
{
    /// <summary>
    /// Extension helper to expose DebugUpdateProductAsync on IProductService when the concrete implementation is ProductService.
    /// This is a minimal, non-invasive fix to resolve the compile error caused by calls to DebugUpdateProductAsync on the injected IProductService.
    /// </summary>
    public static class ProductServiceExtensions
    {
        public static Task<ProductDto?> DebugUpdateProductAsync(this IProductService service, Guid id, UpdateProductDto updateDto)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            // If the concrete implementation provides the debug method, call it.
            if (service is ProductService concrete)
            {
                return concrete.DebugUpdateProductAsync(id, updateDto);
            }

            // Fallback: method not supported by this implementation.
            throw new NotSupportedException("DebugUpdateProductAsync is not supported by the current IProductService implementation.");
        }
    }
}