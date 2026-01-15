using EventForge.DTOs.UnitOfMeasures;
using EventForge.DTOs.VatRates;

namespace EventForge.Client.Services.Documents;

/// <summary>
/// Service for caching frequently accessed data used in document dialogs.
/// Reduces redundant API calls when opening dialogs multiple times in succession.
/// </summary>
public interface IDocumentDialogCacheService
{
    /// <summary>
    /// Gets all units of measure from cache or loads them if cache is expired.
    /// </summary>
    /// <returns>List of all units of measure</returns>
    Task<List<UMDto>> GetUnitsOfMeasureAsync();

    /// <summary>
    /// Gets all active VAT rates from cache or loads them if cache is expired.
    /// </summary>
    /// <returns>List of active VAT rates</returns>
    Task<List<VatRateDto>> GetVatRatesAsync();

    /// <summary>
    /// Invalidates the cache, forcing fresh data to be loaded on next request.
    /// </summary>
    void InvalidateCache();
}
