using Prym.DTOs.External;

namespace Prym.Web.Services.External;

/// <summary>
/// Client service for product barcode lookup.
/// </summary>
public interface IProductBarcodeLookupService
{
    /// <summary>
    /// Looks up product data by barcode/product code.
    /// </summary>
    /// <param name="code">Barcode or product code</param>
    /// <returns>Lookup result with product information</returns>
    Task<ProductBarcodeLookupResultDto?> LookupAsync(string code, CancellationToken ct = default);
}
