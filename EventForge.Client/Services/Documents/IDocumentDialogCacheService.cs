using EventForge.DTOs.Common;
using EventForge.DTOs.PriceLists;
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
    /// Ottiene i listini vendita attivi (cached)
    /// </summary>
    /// <returns>Lista dei listini vendita attivi</returns>
    Task<List<PriceListDto>> GetActiveSalesPriceListsAsync();

    /// <summary>
    /// Ottiene i listini acquisto attivi (cached)
    /// </summary>
    /// <returns>Lista dei listini acquisto attivi</returns>
    Task<List<PriceListDto>> GetActivePurchasePriceListsAsync();

    /// <summary>
    /// Ottiene tutti i listini attivi (vendita + acquisto) cached
    /// </summary>
    /// <returns>Lista combinata di tutti i listini attivi</returns>
    Task<List<PriceListDto>> GetAllActivePriceListsAsync();

    /// <summary>
    /// Ottiene il nome di un listino specifico dal cache (O(1) lookup)
    /// </summary>
    /// <param name="priceListId">ID del listino</param>
    /// <returns>Nome del listino o null se non trovato</returns>
    Task<string?> GetPriceListNameAsync(Guid priceListId);

    /// <summary>
    /// Invalidates the cache, forcing fresh data to be loaded on next request.
    /// </summary>
    void InvalidateCache();
}
