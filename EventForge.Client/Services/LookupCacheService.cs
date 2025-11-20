using EventForge.DTOs.Products;
using EventForge.DTOs.UnitOfMeasures;
using EventForge.DTOs.VatRates;
using Microsoft.Extensions.Caching.Memory;
using Polly;
using Polly.Retry;

namespace EventForge.Client.Services;

/// <summary>
/// Result wrapper for lookup operations with structured error handling
/// </summary>
public record LookupResult<T>(
    bool Success,
    IReadOnlyCollection<T> Items,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    bool IsTransient = false)
{
    public static LookupResult<T> Ok(IEnumerable<T> items) =>
        new(true, items.ToList(), null, null, false);

    public static LookupResult<T> Fail(string? message, string? code = null, bool transient = false) =>
        new(false, Array.Empty<T>(), code, message, transient);
}

/// <summary>
/// Centralized cache service for lookup data (brands, models, VAT rates, units of measure)
/// </summary>
public interface ILookupCacheService
{
    // New methods returning LookupResult<T> for structured error handling
    Task<LookupResult<BrandDto>> GetBrandsAsync(bool forceRefresh = false);
    Task<LookupResult<ModelDto>> GetModelsAsync(Guid? brandId = null, bool forceRefresh = false);
    Task<LookupResult<VatRateDto>> GetVatRatesAsync(bool forceRefresh = false);
    Task<LookupResult<UMDto>> GetUnitsOfMeasureAsync(bool forceRefresh = false);

    // Legacy raw methods for backward compatibility
    Task<IEnumerable<BrandDto>> GetBrandsRawAsync(bool forceRefresh = false);
    Task<IEnumerable<ModelDto>> GetModelsRawAsync(Guid? brandId = null, bool forceRefresh = false);
    Task<IEnumerable<VatRateDto>> GetVatRatesRawAsync(bool forceRefresh = false);
    Task<IEnumerable<UMDto>> GetUnitsOfMeasureRawAsync(bool forceRefresh = false);

    // Direct lookup methods
    Task<BrandDto?> GetBrandByIdAsync(Guid brandId);
    Task<ModelDto?> GetModelByIdAsync(Guid modelId);
    Task<VatRateDto?> GetVatRateByIdAsync(Guid vatRateId);
    Task<UMDto?> GetUnitOfMeasureByIdAsync(Guid unitId);

    void ClearCache();
}

public class LookupCacheService : ILookupCacheService
{
    private readonly IBrandService _brandService;
    private readonly IModelService _modelService;
    private readonly IFinancialService _financialService;
    private readonly IUMService _umService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LookupCacheService> _logger;

    private const string BrandsCacheKey = "lookup_brands";
    private const string ModelsCacheKey = "lookup_models";
    private const string ModelsByBrandCacheKeyPrefix = "lookup_models_brand_";
    private const string VatRatesCacheKey = "lookup_vatrates";
    private const string UnitsCacheKey = "lookup_units";
    private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(10);

    private readonly AsyncRetryPolicy _retryPolicy;

    public LookupCacheService(
        IBrandService brandService,
        IModelService modelService,
        IFinancialService financialService,
        IUMService umService,
        IMemoryCache cache,
        ILogger<LookupCacheService> logger)
    {
        _brandService = brandService;
        _modelService = modelService;
        _financialService = financialService;
        _umService = umService;
        _cache = cache;
        _logger = logger;

        // Configure Polly retry policy for transient errors with exponential backoff
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                new[] { TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1) },
                (ex, delay, attempt, ctx) =>
                    _logger.LogWarning(ex, "Transient lookup failure on attempt {Attempt} after {Delay}ms",
                        attempt, delay.TotalMilliseconds));
    }

    public async Task<LookupResult<BrandDto>> GetBrandsAsync(bool forceRefresh = false)
    {
        if (forceRefresh)
        {
            _cache.Remove(BrandsCacheKey);
            _logger.LogDebug("forceRefresh invalidated brands cache");
        }

        // Check if we have a cached successful result
        if (_cache.TryGetValue(BrandsCacheKey, out LookupResult<BrandDto>? cached) && cached != null && cached.Success)
        {
            return cached;
        }

        try
        {
            var result = await _retryPolicy.ExecuteAsync(async () =>
            {
                var api = await _brandService.GetBrandsAsync(1, 100);
                if (api == null)
                {
                    return LookupResult<BrandDto>.Fail("Null API response", "NULL_RESPONSE", true);
                }

                var items = api.Items?.ToList() ?? new List<BrandDto>();
                _logger.LogInformation("Loaded {Count} brands (Total={Total})", items.Count, api.TotalCount);

                return LookupResult<BrandDto>.Ok(items);
            });

            // Only cache successful results
            if (result.Success)
            {
                _cache.Set(BrandsCacheKey, result, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = DefaultCacheExpiration
                });
            }
            else
            {
                _logger.LogWarning("Brands failure: {Msg} (Transient={Transient})",
                    result.ErrorMessage, result.IsTransient);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unrecoverable brands error");
            return LookupResult<BrandDto>.Fail(ex.Message, "UNHANDLED_EXCEPTION");
        }
    }

    public async Task<LookupResult<ModelDto>> GetModelsAsync(Guid? brandId = null, bool forceRefresh = false)
    {
        var key = brandId.HasValue ? $"{ModelsByBrandCacheKeyPrefix}{brandId}" : ModelsCacheKey;

        if (forceRefresh)
        {
            _cache.Remove(key);
            _logger.LogDebug("forceRefresh invalidated models cache {Key}", key);
        }

        // Check if we have a cached successful result
        if (_cache.TryGetValue(key, out LookupResult<ModelDto>? cached) && cached != null && cached.Success)
        {
            return cached;
        }

        try
        {
            var result = await _retryPolicy.ExecuteAsync(async () =>
            {
                var api = brandId.HasValue
                    ? await _modelService.GetModelsByBrandIdAsync(brandId.Value, 1, 100)
                    : await _modelService.GetModelsAsync(1, 100);

                if (api == null)
                {
                    return LookupResult<ModelDto>.Fail("Null API response models", "NULL_RESPONSE", true);
                }

                var items = api.Items?.ToList() ?? new List<ModelDto>();
                _logger.LogInformation("Loaded {Count} models (Brand={Brand} Total={Total})",
                    items.Count, brandId?.ToString() ?? "ALL", api.TotalCount);

                return LookupResult<ModelDto>.Ok(items);
            });

            // Only cache successful results
            if (result.Success)
            {
                _cache.Set(key, result, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = DefaultCacheExpiration
                });
            }
            else
            {
                _logger.LogWarning("Models failure: {Msg} (Transient={Transient})",
                    result.ErrorMessage, result.IsTransient);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unrecoverable models error (BrandId={BrandId})", brandId);
            return LookupResult<ModelDto>.Fail(ex.Message, "UNHANDLED_EXCEPTION");
        }
    }

    public async Task<LookupResult<VatRateDto>> GetVatRatesAsync(bool forceRefresh = false)
    {
        if (forceRefresh)
        {
            _cache.Remove(VatRatesCacheKey);
            _logger.LogDebug("forceRefresh invalidated VAT rates cache");
        }

        // Check if we have a cached successful result
        if (_cache.TryGetValue(VatRatesCacheKey, out LookupResult<VatRateDto>? cached) && cached != null && cached.Success)
        {
            return cached;
        }

        try
        {
            var result = await _retryPolicy.ExecuteAsync(async () =>
            {
                var api = await _financialService.GetVatRatesAsync(1, 100);
                if (api == null)
                {
                    return LookupResult<VatRateDto>.Fail("Null API response VAT", "NULL_RESPONSE", true);
                }

                var items = api.Items?.ToList() ?? new List<VatRateDto>();
                _logger.LogInformation("Loaded {Count} VAT rates", items.Count);

                return LookupResult<VatRateDto>.Ok(items);
            });

            // Only cache successful results
            if (result.Success)
            {
                _cache.Set(VatRatesCacheKey, result, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = DefaultCacheExpiration
                });
            }
            else
            {
                _logger.LogWarning("VAT rates failure: {Msg} (Transient={Transient})",
                    result.ErrorMessage, result.IsTransient);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unrecoverable VAT rates error");
            return LookupResult<VatRateDto>.Fail(ex.Message, "UNHANDLED_EXCEPTION");
        }
    }

    public async Task<LookupResult<UMDto>> GetUnitsOfMeasureAsync(bool forceRefresh = false)
    {
        if (forceRefresh)
        {
            _cache.Remove(UnitsCacheKey);
            _logger.LogDebug("forceRefresh invalidated units cache");
        }

        // Check if we have a cached successful result
        if (_cache.TryGetValue(UnitsCacheKey, out LookupResult<UMDto>? cached) && cached != null && cached.Success)
        {
            return cached;
        }

        try
        {
            var result = await _retryPolicy.ExecuteAsync(async () =>
            {
                var api = await _umService.GetUMsAsync(1, 100);
                if (api == null)
                {
                    return LookupResult<UMDto>.Fail("Null API response units", "NULL_RESPONSE", true);
                }

                var items = api.Items?.ToList() ?? new List<UMDto>();
                _logger.LogInformation("Loaded {Count} units of measure", items.Count);

                return LookupResult<UMDto>.Ok(items);
            });

            // Only cache successful results
            if (result.Success)
            {
                _cache.Set(UnitsCacheKey, result, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = DefaultCacheExpiration
                });
            }
            else
            {
                _logger.LogWarning("Units failure: {Msg} (Transient={Transient})",
                    result.ErrorMessage, result.IsTransient);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unrecoverable units error");
            return LookupResult<UMDto>.Fail(ex.Message, "UNHANDLED_EXCEPTION");
        }
    }

    // Legacy raw methods for backward compatibility - unwrap Items from LookupResult
    public async Task<IEnumerable<BrandDto>> GetBrandsRawAsync(bool forceRefresh = false) =>
        (await GetBrandsAsync(forceRefresh)).Items;

    public async Task<IEnumerable<ModelDto>> GetModelsRawAsync(Guid? brandId = null, bool forceRefresh = false) =>
        (await GetModelsAsync(brandId, forceRefresh)).Items;

    public async Task<IEnumerable<VatRateDto>> GetVatRatesRawAsync(bool forceRefresh = false) =>
        (await GetVatRatesAsync(forceRefresh)).Items;

    public async Task<IEnumerable<UMDto>> GetUnitsOfMeasureRawAsync(bool forceRefresh = false) =>
        (await GetUnitsOfMeasureAsync(forceRefresh)).Items;

    public async Task<BrandDto?> GetBrandByIdAsync(Guid brandId)
    {
        try
        {
            // First check if it's in the cached collection
            var all = await GetBrandsRawAsync();
            var brand = all.FirstOrDefault(b => b.Id == brandId);
            if (brand != null) return brand;

            // If not found, fetch directly
            return await _brandService.GetBrandByIdAsync(brandId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetBrandById failed {BrandId}", brandId);
            return null;
        }
    }

    public async Task<ModelDto?> GetModelByIdAsync(Guid modelId)
    {
        try
        {
            // First check if it's in the cached collection
            var all = await GetModelsRawAsync();
            var model = all.FirstOrDefault(m => m.Id == modelId);
            if (model != null) return model;

            // If not found, fetch directly
            return await _modelService.GetModelByIdAsync(modelId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetModelById failed {ModelId}", modelId);
            return null;
        }
    }

    public async Task<VatRateDto?> GetVatRateByIdAsync(Guid vatRateId)
    {
        try
        {
            // First check if it's in the cached collection
            var all = await GetVatRatesRawAsync();
            var vatRate = all.FirstOrDefault(v => v.Id == vatRateId);
            if (vatRate != null) return vatRate;

            // If not found, fetch directly
            return await _financialService.GetVatRateAsync(vatRateId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetVatRateById failed {VatRateId}", vatRateId);
            return null;
        }
    }

    public async Task<UMDto?> GetUnitOfMeasureByIdAsync(Guid unitId)
    {
        try
        {
            // First check if it's in the cached collection
            var all = await GetUnitsOfMeasureRawAsync();
            var unit = all.FirstOrDefault(u => u.Id == unitId);
            if (unit != null) return unit;

            // If not found, fetch directly
            return await _umService.GetUMByIdAsync(unitId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetUnitOfMeasureById failed {UnitId}", unitId);
            return null;
        }
    }

    public void ClearCache()
    {
        _cache.Remove(BrandsCacheKey);
        _cache.Remove(ModelsCacheKey);
        _cache.Remove(VatRatesCacheKey);
        _cache.Remove(UnitsCacheKey);
        _logger.LogInformation("Lookup cache cleared");
    }
}
