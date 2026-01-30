using EventForge.DTOs.Common;
using EventForge.DTOs.Products;
using System.Net;
using Microsoft.Extensions.Caching.Memory;
using EventForge.Client.Helpers;

namespace EventForge.Client.Services;

/// <summary>
/// Service implementation for managing brands.
/// </summary>
public class BrandService : IBrandService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<BrandService> _logger;
    private readonly IMemoryCache _cache;
    private const string BaseUrl = "api/v1/product-management/brands";

    public BrandService(IHttpClientService httpClientService, ILogger<BrandService> logger, IMemoryCache cache)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<PagedResult<BrandDto>> GetBrandsAsync(int page = 1, int pageSize = 100, CancellationToken ct = default)
    {
        var result = await _httpClientService.GetAsync<PagedResult<BrandDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}", ct);
        return result ?? new PagedResult<BrandDto> { Items = new List<BrandDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
    }

    public async Task<IEnumerable<BrandDto>> GetActiveBrandsAsync(CancellationToken ct = default)
    {
        // Note: Method name kept as "GetActiveBrandsAsync" for consistency with other services,
        // but returns all brands since BrandDto doesn't have an IsActive property
        if (_cache.TryGetValue(CacheHelper.ACTIVE_BRANDS, out IEnumerable<BrandDto>? cached) && cached != null)
        {
            _logger.LogDebug("Cache HIT: Brands ({Count} items)", cached.Count());
            return cached;
        }
        
        _logger.LogDebug("Cache MISS: Loading brands from API");
        
        try
        {
            var result = await GetBrandsAsync(1, 100, ct);
            var brands = result?.Items ?? Enumerable.Empty<BrandDto>();
            
            _cache.Set(
                CacheHelper.ACTIVE_BRANDS, 
                brands, 
                CacheHelper.GetShortCacheOptions()
            );
            
            _logger.LogInformation(
                "Cached {Count} brands for {Minutes} minutes", 
                brands.Count(), 
                CacheHelper.ShortCache.TotalMinutes
            );
            
            return brands;
        }
        catch (HttpRequestException)
        {
            return Enumerable.Empty<BrandDto>();
        }
    }

    public async Task<BrandDto?> GetBrandByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _httpClientService.GetAsync<BrandDto>($"{BaseUrl}/{id}", ct);
    }

    public async Task<BrandDto> CreateBrandAsync(CreateBrandDto createBrandDto, CancellationToken ct = default)
    {
        var result = await _httpClientService.PostAsync<CreateBrandDto, BrandDto>(BaseUrl, createBrandDto, ct);
        
        // Invalidate cache
        _cache.Remove(CacheHelper.ACTIVE_BRANDS);
        _logger.LogDebug("Invalidated brands cache after create");
        
        return result ?? throw new InvalidOperationException("Failed to create brand");
    }

    public async Task<BrandDto?> UpdateBrandAsync(Guid id, UpdateBrandDto updateBrandDto, CancellationToken ct = default)
    {
        var result = await _httpClientService.PutAsync<UpdateBrandDto, BrandDto>($"{BaseUrl}/{id}", updateBrandDto, ct);
        
        // Invalidate cache
        _cache.Remove(CacheHelper.ACTIVE_BRANDS);
        _logger.LogDebug("Invalidated brands cache after update (ID: {Id})", id);
        
        return result;
    }

    public async Task<bool> DeleteBrandAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{id}", ct);
            
            // Invalidate cache
            _cache.Remove(CacheHelper.ACTIVE_BRANDS);
            _logger.LogDebug("Invalidated brands cache after delete (ID: {Id})", id);
            
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Business logic: return false if not found (no logging)
            return false;
        }
    }
}
