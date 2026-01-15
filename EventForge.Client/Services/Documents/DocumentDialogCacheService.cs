using EventForge.DTOs.UnitOfMeasures;
using EventForge.DTOs.VatRates;
using Microsoft.Extensions.Logging;

namespace EventForge.Client.Services.Documents;

/// <summary>
/// Implementation of caching service for document dialog data.
/// Caches units of measure and VAT rates with a 5-minute TTL.
/// </summary>
public class DocumentDialogCacheService : IDocumentDialogCacheService
{
    private readonly IFinancialService _financialService;
    private readonly IProductService _productService;
    private readonly ILogger<DocumentDialogCacheService> _logger;

    // Cache fields
    private List<UMDto>? _cachedUnits;
    private List<VatRateDto>? _cachedVatRates;
    private DateTime? _cacheTime;
    
    // Cache configuration
    private const int CacheMinutes = 5;

    public DocumentDialogCacheService(
        IFinancialService financialService,
        IProductService productService,
        ILogger<DocumentDialogCacheService> logger)
    {
        _financialService = financialService ?? throw new ArgumentNullException(nameof(financialService));
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
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
        
        _cachedUnits = null;
        _cachedVatRates = null;
        _cacheTime = null;
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
