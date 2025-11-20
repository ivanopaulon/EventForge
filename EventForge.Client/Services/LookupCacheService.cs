using EventForge.DTOs.Products;
using EventForge.DTOs.UnitOfMeasures;
using EventForge.DTOs.VatRates;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EventForge.Client.Services;

/// <summary>
/// Centralized cache service for lookup data (brands, models, VAT rates, units of measure)
/// </summary>
public interface ILookupCacheService
{
    Task<IEnumerable<BrandDto>> GetBrandsAsync(bool forceRefresh = false);
    Task<IEnumerable<ModelDto>> GetModelsAsync(Guid? brandId = null, bool forceRefresh = false);
    Task<IEnumerable<VatRateDto>> GetVatRatesAsync(bool forceRefresh = false);
    Task<IEnumerable<UMDto>> GetUnitsOfMeasureAsync(bool forceRefresh = false);
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
    }

    public async Task<IEnumerable<BrandDto>> GetBrandsAsync(bool forceRefresh = false)
    {
        if (forceRefresh)
        {
            _cache.Remove(BrandsCacheKey);
        }

        var result = await _cache.GetOrCreateAsync(BrandsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;
            try
            {
                var apiResult = await _brandService.GetBrandsAsync(1, 100);
                var brands = apiResult?.Items?.ToList() ?? new List<BrandDto>();
                _logger.LogInformation("Loaded {Count} brands from service", brands.Count);
                return brands;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load brands from service");
                return new List<BrandDto>();
            }
        });
        return result ?? new List<BrandDto>();
    }

    public async Task<IEnumerable<ModelDto>> GetModelsAsync(Guid? brandId = null, bool forceRefresh = false)
    {
        var cacheKey = brandId.HasValue 
            ? $"{ModelsByBrandCacheKeyPrefix}{brandId.Value}" 
            : ModelsCacheKey;

        if (forceRefresh)
        {
            _cache.Remove(cacheKey);
        }

        var result = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;
            try
            {
                if (brandId.HasValue)
                {
                    var apiResult = await _modelService.GetModelsByBrandIdAsync(brandId.Value, 1, 100);
                    var models = apiResult?.Items?.ToList() ?? new List<ModelDto>();
                    _logger.LogInformation("Loaded {Count} models from service for brand {BrandId}", models.Count, brandId.Value);
                    return models;
                }
                else
                {
                    var apiResult = await _modelService.GetModelsAsync(1, 100);
                    var models = apiResult?.Items?.ToList() ?? new List<ModelDto>();
                    _logger.LogInformation("Loaded {Count} models from service", models.Count);
                    return models;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load models from service (BrandId: {BrandId})", brandId);
                return new List<ModelDto>();
            }
        });
        return result ?? new List<ModelDto>();
    }

    public async Task<IEnumerable<VatRateDto>> GetVatRatesAsync(bool forceRefresh = false)
    {
        if (forceRefresh)
        {
            _cache.Remove(VatRatesCacheKey);
        }

        var result = await _cache.GetOrCreateAsync(VatRatesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;
            try
            {
                var apiResult = await _financialService.GetVatRatesAsync(1, 100);
                var vatRates = apiResult?.Items?.ToList() ?? new List<VatRateDto>();
                _logger.LogInformation("Loaded {Count} VAT rates from service", vatRates.Count);
                return vatRates;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load VAT rates from service");
                return new List<VatRateDto>();
            }
        });
        return result ?? new List<VatRateDto>();
    }

    public async Task<IEnumerable<UMDto>> GetUnitsOfMeasureAsync(bool forceRefresh = false)
    {
        if (forceRefresh)
        {
            _cache.Remove(UnitsCacheKey);
        }

        var result = await _cache.GetOrCreateAsync(UnitsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = DefaultCacheExpiration;
            try
            {
                var apiResult = await _umService.GetUMsAsync(1, 100);
                var units = apiResult?.Items?.ToList() ?? new List<UMDto>();
                _logger.LogInformation("Loaded {Count} units of measure from service", units.Count);
                return units;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load units of measure from service");
                return new List<UMDto>();
            }
        });
        return result ?? new List<UMDto>();
    }

    public async Task<BrandDto?> GetBrandByIdAsync(Guid brandId)
    {
        try
        {
            // First check if it's in the cached collection
            var brands = await GetBrandsAsync();
            var brand = brands.FirstOrDefault(b => b.Id == brandId);
            if (brand != null) return brand;

            // If not found, fetch directly
            return await _brandService.GetBrandByIdAsync(brandId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get brand by ID {BrandId}", brandId);
            return null;
        }
    }

    public async Task<ModelDto?> GetModelByIdAsync(Guid modelId)
    {
        try
        {
            // First check if it's in the cached collection
            var models = await GetModelsAsync();
            var model = models.FirstOrDefault(m => m.Id == modelId);
            if (model != null) return model;

            // If not found, fetch directly
            return await _modelService.GetModelByIdAsync(modelId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get model by ID {ModelId}", modelId);
            return null;
        }
    }

    public async Task<VatRateDto?> GetVatRateByIdAsync(Guid vatRateId)
    {
        try
        {
            // First check if it's in the cached collection
            var vatRates = await GetVatRatesAsync();
            var vatRate = vatRates.FirstOrDefault(v => v.Id == vatRateId);
            if (vatRate != null) return vatRate;

            // If not found, fetch directly
            return await _financialService.GetVatRateAsync(vatRateId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get VAT rate by ID {VatRateId}", vatRateId);
            return null;
        }
    }

    public async Task<UMDto?> GetUnitOfMeasureByIdAsync(Guid unitId)
    {
        try
        {
            // First check if it's in the cached collection
            var units = await GetUnitsOfMeasureAsync();
            var unit = units.FirstOrDefault(u => u.Id == unitId);
            if (unit != null) return unit;

            // If not found, fetch directly
            return await _umService.GetUMByIdAsync(unitId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get unit of measure by ID {UnitId}", unitId);
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
