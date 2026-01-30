using EventForge.DTOs.Common;
using EventForge.DTOs.UnitOfMeasures;
using System.Net;
using Microsoft.Extensions.Caching.Memory;
using EventForge.Client.Helpers;

namespace EventForge.Client.Services;

/// <summary>
/// Service implementation for managing units of measure.
/// </summary>
public class UMService : IUMService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<UMService> _logger;
    private readonly IMemoryCache _cache;
    private const string BaseUrl = "api/v1/product-management/units";

    public UMService(IHttpClientService httpClientService, ILogger<UMService> logger, IMemoryCache cache)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<PagedResult<UMDto>> GetUMsAsync(int page = 1, int pageSize = 100, CancellationToken ct = default)
    {
        var result = await _httpClientService.GetAsync<PagedResult<UMDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}", ct);
        return result ?? new PagedResult<UMDto> { Items = new List<UMDto>(), TotalCount = 0, Page = page, PageSize = pageSize };
    }

    public async Task<IEnumerable<UMDto>> GetUnitsOfMeasureAsync(CancellationToken ct = default)
    {
        // Try cache first
        if (_cache.TryGetValue(CacheHelper.ACTIVE_UNITS_OF_MEASURE, out IEnumerable<UMDto>? cached) && cached != null)
        {
            _logger.LogDebug("Cache HIT: Active units of measure ({Count} items)", cached.Count());
            return cached;
        }
        
        // Cache miss - API call
        _logger.LogDebug("Cache MISS: Loading active units of measure from API");
        
        try
        {
            var result = await GetUMsAsync(1, 100, ct);
            var activeUnits = result?.Items?.Where(um => um.IsActive) ?? Enumerable.Empty<UMDto>();
            
            // Store in cache (30 minutes)
            _cache.Set(
                CacheHelper.ACTIVE_UNITS_OF_MEASURE, 
                activeUnits, 
                CacheHelper.GetMediumCacheOptions()
            );
            
            _logger.LogInformation(
                "Cached {Count} active units of measure for {Minutes} minutes", 
                activeUnits.Count(), 
                CacheHelper.MediumCache.TotalMinutes
            );
            
            return activeUnits;
        }
        catch (HttpRequestException)
        {
            // Fallback empty (NO LOG - already logged by HttpClientService)
            return Enumerable.Empty<UMDto>();
        }
    }

    public async Task<UMDto?> GetUMByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _httpClientService.GetAsync<UMDto>($"{BaseUrl}/{id}", ct);
    }

    public async Task<UMDto> CreateUMAsync(CreateUMDto createUMDto, CancellationToken ct = default)
    {
        var result = await _httpClientService.PostAsync<CreateUMDto, UMDto>(BaseUrl, createUMDto, ct);
        
        // Invalidate cache
        _cache.Remove(CacheHelper.ACTIVE_UNITS_OF_MEASURE);
        _logger.LogDebug("Invalidated active units cache after create");
        
        return result ?? throw new InvalidOperationException("Failed to create unit of measure");
    }

    public async Task<UMDto?> UpdateUMAsync(Guid id, UpdateUMDto updateUMDto, CancellationToken ct = default)
    {
        var result = await _httpClientService.PutAsync<UpdateUMDto, UMDto>($"{BaseUrl}/{id}", updateUMDto, ct);
        
        // Invalidate cache
        _cache.Remove(CacheHelper.ACTIVE_UNITS_OF_MEASURE);
        _logger.LogDebug("Invalidated active units cache after update (ID: {Id})", id);
        
        return result;
    }

    public async Task<bool> DeleteUMAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{id}", ct);
            
            // Invalidate cache
            _cache.Remove(CacheHelper.ACTIVE_UNITS_OF_MEASURE);
            _logger.LogDebug("Invalidated active units cache after delete (ID: {Id})", id);
            
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Business logic: return false if not found (no logging)
            return false;
        }
    }
}
