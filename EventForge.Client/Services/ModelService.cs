using EventForge.DTOs.Common;
using EventForge.DTOs.Products;
using Microsoft.Extensions.Caching.Memory;
using EventForge.Client.Helpers;
using System.Net;

namespace EventForge.Client.Services;

/// <summary>
/// Service implementation for managing models.
/// </summary>
public class ModelService : IModelService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<ModelService> _logger;
    private readonly IMemoryCache _cache;
    private const string BaseUrl = "api/v1/product-management/models";

    public ModelService(IHttpClientService httpClientService, ILogger<ModelService> logger, IMemoryCache cache)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<PagedResult<ModelDto>> GetModelsAsync(int page = 1, int pageSize = 100)
    {
        try
        {
            var result = await _httpClientService.GetAsync<PagedResult<ModelDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}");
            return result ?? new PagedResult<ModelDto> { Items = new List<ModelDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving models");
            throw;
        }
    }

    public async Task<PagedResult<ModelDto>> GetModelsByBrandIdAsync(Guid brandId, int page = 1, int pageSize = 100)
    {
        try
        {
            var result = await _httpClientService.GetAsync<PagedResult<ModelDto>>($"{BaseUrl}?brandId={brandId}&page={page}&pageSize={pageSize}");
            return result ?? new PagedResult<ModelDto> { Items = new List<ModelDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving models for brand {BrandId}", brandId);
            throw;
        }
    }

    public async Task<IEnumerable<ModelDto>> GetActiveModelsByBrandAsync(Guid brandId, CancellationToken ct = default)
    {
        // Note: Method name kept as "GetActiveModelsByBrandAsync" for consistency,
        // but returns all models since ModelDto doesn't have an IsActive property
        var cacheKey = CacheHelper.GetModelsByBrandKey(brandId);
        
        // Try cache first
        if (_cache.TryGetValue(cacheKey, out IEnumerable<ModelDto>? cached) && cached != null)
        {
            _logger.LogDebug("Cache HIT: Models for brand {BrandId} ({Count} items)", brandId, cached.Count());
            return cached;
        }
        
        // Cache miss - API call
        _logger.LogDebug("Cache MISS: Loading models for brand {BrandId} from API", brandId);
        
        try
        {
            var result = await GetModelsByBrandIdAsync(brandId, 1, 100);
            var models = result?.Items ?? Enumerable.Empty<ModelDto>();
            
            // Store in cache (15 minutes)
            _cache.Set(
                cacheKey, 
                models, 
                CacheHelper.GetShortCacheOptions()
            );
            
            _logger.LogInformation(
                "Cached {Count} models for brand {BrandId} for {Minutes} minutes", 
                models.Count(), 
                brandId,
                CacheHelper.ShortCache.TotalMinutes
            );
            
            return models;
        }
        catch (HttpRequestException)
        {
            return Enumerable.Empty<ModelDto>();
        }
    }

    public async Task<ModelDto?> GetModelByIdAsync(Guid id)
    {
        try
        {
            return await _httpClientService.GetAsync<ModelDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving model with ID {Id}", id);
            throw;
        }
    }

    public async Task<ModelDto> CreateModelAsync(CreateModelDto createModelDto)
    {
        try
        {
            var result = await _httpClientService.PostAsync<CreateModelDto, ModelDto>(BaseUrl, createModelDto);
            
            // Invalidate cache per Brand specifico
            if (result != null)
            {
                var cacheKey = CacheHelper.GetModelsByBrandKey(result.BrandId);
                _cache.Remove(cacheKey);
                _logger.LogDebug("Invalidated models cache for brand {BrandId} after create", result.BrandId);
            }
            
            return result ?? throw new InvalidOperationException("Failed to create model");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating model");
            throw;
        }
    }

    public async Task<ModelDto?> UpdateModelAsync(Guid id, UpdateModelDto updateModelDto)
    {
        try
        {
            var result = await _httpClientService.PutAsync<UpdateModelDto, ModelDto>($"{BaseUrl}/{id}", updateModelDto);
            
            // Invalidate cache per Brand
            if (result != null)
            {
                var cacheKey = CacheHelper.GetModelsByBrandKey(result.BrandId);
                _cache.Remove(cacheKey);
                _logger.LogDebug("Invalidated models cache for brand {BrandId} after update (model {ModelId})", 
                    result.BrandId, id);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating model with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteModelAsync(Guid id)
    {
        // Problem: Non conosciamo BrandId prima del delete
        // Solution: GET model first (+1 API call, acceptable trade-off)
        var model = await GetModelByIdAsync(id);
        
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{id}");
            
            // Invalidate se model esisteva
            if (model != null)
            {
                var cacheKey = CacheHelper.GetModelsByBrandKey(model.BrandId);
                _cache.Remove(cacheKey);
                _logger.LogDebug("Invalidated models cache for brand {BrandId} after delete (model {ModelId})", 
                    model.BrandId, id);
            }
            
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting model with ID {Id}", id);
            throw;
        }
    }
}
