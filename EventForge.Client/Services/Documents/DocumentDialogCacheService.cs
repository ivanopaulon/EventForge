using EventForge.DTOs.Common;
using EventForge.DTOs.PriceLists;
using EventForge.DTOs.UnitOfMeasures;
using EventForge.DTOs.VatRates;

namespace EventForge.Client.Services.Documents;

/// <summary>
/// Implementation of caching service for document dialog data.
/// Caches units of measure, VAT rates, and price lists with a 5-minute TTL.
/// </summary>
public class DocumentDialogCacheService : IDocumentDialogCacheService
{
    private readonly IFinancialService _financialService;
    private readonly IProductService _productService;
    private readonly IPriceListService _priceListService;
    private readonly ILogger<DocumentDialogCacheService> _logger;

    // Cache fields for UOM and VAT
    private List<UMDto>? _cachedUnits;
    private List<VatRateDto>? _cachedVatRates;
    private DateTime? _cacheTime;

    // Cache fields for price lists
    private List<PriceListDto>? _activeSalesPriceLists;
    private List<PriceListDto>? _activePurchasePriceLists;
    private Dictionary<Guid, string>? _priceListNamesCache;
    private DateTime? _priceListsCacheTimestamp;

    // Metrics for monitoring
    private int _priceListCacheHits = 0;
    private int _priceListCacheMisses = 0;

    // Cache configuration
    private const int CacheMinutes = 5;
    private static readonly TimeSpan PriceListsCacheDuration = TimeSpan.FromMinutes(5);

    public DocumentDialogCacheService(
        IFinancialService financialService,
        IProductService productService,
        IPriceListService priceListService,
        ILogger<DocumentDialogCacheService> logger)
    {
        _financialService = financialService ?? throw new ArgumentNullException(nameof(financialService));
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _priceListService = priceListService ?? throw new ArgumentNullException(nameof(priceListService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<List<UMDto>> GetUnitsOfMeasureAsync()
    {
        // Return cached data if valid
        if (_cachedUnits != null && IsCacheValid())
        {
            _logger.LogDebug("Returning cached units of measure ({Count} items)", _cachedUnits.Count);
            return _cachedUnits;
        }

        // Load from service
        _logger.LogInformation("Loading units of measure from service (cache expired or empty)");

        try
        {
            var units = await _productService.GetUnitsOfMeasureAsync();
            _cachedUnits = units?.ToList() ?? new List<UMDto>();

            // Update cache timestamp
            _cacheTime = DateTime.UtcNow;

            _logger.LogInformation("Cached {Count} units of measure", _cachedUnits.Count);
            return _cachedUnits;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading units of measure");

            // Return cached data if available, even if expired, as fallback
            if (_cachedUnits != null)
            {
                _logger.LogWarning("Returning stale cached units of measure as fallback");
                return _cachedUnits;
            }

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<VatRateDto>> GetVatRatesAsync()
    {
        // Return cached data if valid
        if (_cachedVatRates != null && IsCacheValid())
        {
            _logger.LogDebug("Returning cached VAT rates ({Count} items)", _cachedVatRates.Count);
            return _cachedVatRates;
        }

        // Load from service
        _logger.LogInformation("Loading VAT rates from service (cache expired or empty)");

        try
        {
            var vatRatesResult = await _financialService.GetVatRatesAsync(1, 100);
            _cachedVatRates = vatRatesResult?.Items?.Where(v => v.IsActive).ToList()
                ?? new List<VatRateDto>();

            // Update cache timestamp
            _cacheTime = DateTime.UtcNow;

            _logger.LogInformation("Cached {Count} active VAT rates", _cachedVatRates.Count);
            return _cachedVatRates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading VAT rates");

            // Return cached data if available, even if expired, as fallback
            if (_cachedVatRates != null)
            {
                _logger.LogWarning("Returning stale cached VAT rates as fallback");
                return _cachedVatRates;
            }

            throw;
        }
    }

    /// <inheritdoc />
    public void InvalidateCache()
    {
        _logger.LogInformation("Invalidating document dialog cache");

        // Existing cache invalidation
        _cachedUnits = null;
        _cachedVatRates = null;
        _cacheTime = null;

        // Price lists cache invalidation
        _activeSalesPriceLists = null;
        _activePurchasePriceLists = null;
        _priceListNamesCache = null;
        _priceListsCacheTimestamp = null;

        // Reset metrics
        _priceListCacheHits = 0;
        _priceListCacheMisses = 0;

        _logger.LogInformation("All caches invalidated (including price lists)");
    }

    /// <inheritdoc />
    public async Task<List<PriceListDto>> GetActiveSalesPriceListsAsync()
    {
        if (_activeSalesPriceLists == null || IsPriceListsCacheExpired())
        {
            try
            {
                _logger.LogDebug("Cache MISS: Loading active sales price lists from API");
                _priceListCacheMisses++;
                
                _activeSalesPriceLists = await _priceListService.GetActivePriceListsAsync(
                    PriceListDirection.Output
                );
                
                // Only update timestamp if not already set or expired
                if (_priceListsCacheTimestamp == null || IsPriceListsCacheExpired())
                {
                    _priceListsCacheTimestamp = DateTime.UtcNow;
                }
                
                _logger.LogInformation(
                    "Loaded {Count} active sales price lists into cache (expires at {ExpiryTime})",
                    _activeSalesPriceLists.Count,
                    _priceListsCacheTimestamp.Value.Add(PriceListsCacheDuration)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading active sales price lists");
                _activeSalesPriceLists = new List<PriceListDto>();
            }
        }
        else
        {
            _priceListCacheHits++;
            _logger.LogDebug("Cache HIT: Returning {Count} sales price lists from cache", 
                _activeSalesPriceLists.Count);
        }
        
        return _activeSalesPriceLists;
    }

    /// <inheritdoc />
    public async Task<List<PriceListDto>> GetActivePurchasePriceListsAsync()
    {
        if (_activePurchasePriceLists == null || IsPriceListsCacheExpired())
        {
            try
            {
                _logger.LogDebug("Cache MISS: Loading active purchase price lists from API");
                _priceListCacheMisses++;
                
                _activePurchasePriceLists = await _priceListService.GetActivePriceListsAsync(
                    PriceListDirection.Input
                );
                
                // Only update timestamp if not already set or expired
                if (_priceListsCacheTimestamp == null || IsPriceListsCacheExpired())
                {
                    _priceListsCacheTimestamp = DateTime.UtcNow;
                }
                
                _logger.LogInformation(
                    "Loaded {Count} active purchase price lists into cache (expires at {ExpiryTime})",
                    _activePurchasePriceLists.Count,
                    _priceListsCacheTimestamp.Value.Add(PriceListsCacheDuration)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading active purchase price lists");
                _activePurchasePriceLists = new List<PriceListDto>();
            }
        }
        else
        {
            _priceListCacheHits++;
            _logger.LogDebug("Cache HIT: Returning {Count} purchase price lists from cache", 
                _activePurchasePriceLists.Count);
        }
        
        return _activePurchasePriceLists;
    }

    /// <inheritdoc />
    public async Task<List<PriceListDto>> GetAllActivePriceListsAsync()
    {
        // Carica in parallelo per performance
        var salesTask = GetActiveSalesPriceListsAsync();
        var purchaseTask = GetActivePurchasePriceListsAsync();
        
        await Task.WhenAll(salesTask, purchaseTask);
        
        // Combina e rimuovi duplicati (se esistono)
        return salesTask.Result
            .Concat(purchaseTask.Result)
            .DistinctBy(pl => pl.Id)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<string?> GetPriceListNameAsync(Guid priceListId)
    {
        // Lazy-load cache dictionary se non esiste o scaduto
        if (_priceListNamesCache == null || IsPriceListsCacheExpired())
        {
            await BuildPriceListNamesCacheAsync();
        }
        
        string? name = null;
        var found = _priceListNamesCache?.TryGetValue(priceListId, out name) == true;
        
        if (found)
        {
            _priceListCacheHits++;
        }
        else
        {
            _priceListCacheMisses++;
            _logger.LogWarning(
                "Price list {PriceListId} not found in cache. Cache contains {Count} entries",
                priceListId,
                _priceListNamesCache?.Count ?? 0
            );
        }
        
        // Log metrics periodicamente
        LogCacheMetricsIfNeeded();
        
        return name;
    }

    private async Task BuildPriceListNamesCacheAsync()
    {
        _logger.LogDebug("Building price list names cache dictionary");
        
        var allLists = await GetAllActivePriceListsAsync();
        
        _priceListNamesCache = allLists.ToDictionary(
            pl => pl.Id,
            pl => pl.Name
        );
        
        _logger.LogInformation(
            "Built price list names cache with {Count} entries",
            _priceListNamesCache.Count
        );
    }

    private bool IsPriceListsCacheExpired()
    {
        if (_priceListsCacheTimestamp == null)
        {
            return true;
        }
        
        var age = DateTime.UtcNow - _priceListsCacheTimestamp.Value;
        var expired = age > PriceListsCacheDuration;
        
        if (expired)
        {
            _logger.LogDebug(
                "Price lists cache expired (age: {Age}, max: {MaxAge})",
                age,
                PriceListsCacheDuration
            );
        }
        
        return expired;
    }

    private void LogCacheMetricsIfNeeded()
    {
        var totalRequests = _priceListCacheHits + _priceListCacheMisses;
        
        // Log ogni 20 richieste
        if (totalRequests > 0 && totalRequests % 20 == 0)
        {
            var hitRate = _priceListCacheHits / (double)totalRequests;
            
            _logger.LogInformation(
                "Price list cache metrics: {Hits} hits, {Misses} misses, {HitRate:P1} hit rate",
                _priceListCacheHits,
                _priceListCacheMisses,
                hitRate
            );
        }
    }

    /// <summary>
    /// Checks if the cache is still valid based on the configured TTL.
    /// </summary>
    /// <returns>True if cache is valid, false if expired or not initialized</returns>
    private bool IsCacheValid()
    {
        if (!_cacheTime.HasValue)
        {
            return false;
        }

        var cacheAge = DateTime.UtcNow - _cacheTime.Value;
        var isValid = cacheAge.TotalMinutes < CacheMinutes;

        if (!isValid)
        {
            _logger.LogDebug("Cache expired (age: {Age:F1} minutes, TTL: {TTL} minutes)",
                cacheAge.TotalMinutes, CacheMinutes);
        }

        return isValid;
    }
}
