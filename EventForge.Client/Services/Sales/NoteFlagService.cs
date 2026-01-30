using EventForge.DTOs.Sales;
using Microsoft.Extensions.Caching.Memory;
using EventForge.Client.Helpers;

namespace EventForge.Client.Services.Sales;

/// <summary>
/// Client service implementation for note flags.
/// </summary>
public class NoteFlagService : INoteFlagService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILogger<NoteFlagService> _logger;
    private readonly IMemoryCache _cache;
    private const string BaseUrl = "api/v1/note-flags";

    public NoteFlagService(IHttpClientService httpClientService, ILogger<NoteFlagService> logger, IMemoryCache cache)
    {
        _httpClientService = httpClientService ?? throw new ArgumentNullException(nameof(httpClientService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<List<NoteFlagDto>?> GetAllAsync()
    {
        try
        {
            return await _httpClientService.GetAsync<List<NoteFlagDto>>(BaseUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all note flags");
            return null;
        }
    }

    public async Task<List<NoteFlagDto>?> GetActiveAsync()
    {
        // Try cache first
        if (_cache.TryGetValue(CacheHelper.ACTIVE_NOTE_FLAGS, out List<NoteFlagDto>? cached) && cached != null)
        {
            _logger.LogDebug("Cache HIT: Active note flags ({Count} items)", cached.Count);
            return cached;
        }
        
        // Cache miss - API call
        _logger.LogDebug("Cache MISS: Loading active note flags from API");
        
        try
        {
            var result = await _httpClientService.GetAsync<List<NoteFlagDto>>($"{BaseUrl}/active");
            
            if (result != null)
            {
                // Store in cache (60 minutes - LongCache)
                _cache.Set(
                    CacheHelper.ACTIVE_NOTE_FLAGS, 
                    result, 
                    CacheHelper.GetLongCacheOptions()
                );
                
                _logger.LogInformation(
                    "Cached {Count} active note flags for {Minutes} minutes", 
                    result.Count, 
                    CacheHelper.LongCache.TotalMinutes
                );
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active note flags");
            return null;
        }
    }

    public async Task<NoteFlagDto?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _httpClientService.GetAsync<NoteFlagDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving note flag {Id}", id);
            return null;
        }
    }

    public async Task<NoteFlagDto?> CreateAsync(CreateNoteFlagDto createDto)
    {
        try
        {
            var result = await _httpClientService.PostAsync<CreateNoteFlagDto, NoteFlagDto>(BaseUrl, createDto);
            
            // Invalidate cache
            _cache.Remove(CacheHelper.ACTIVE_NOTE_FLAGS);
            _logger.LogDebug("Invalidated active note flags cache after create");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating note flag");
            return null;
        }
    }

    public async Task<NoteFlagDto?> UpdateAsync(Guid id, UpdateNoteFlagDto updateDto)
    {
        try
        {
            var result = await _httpClientService.PutAsync<UpdateNoteFlagDto, NoteFlagDto>($"{BaseUrl}/{id}", updateDto);
            
            // Invalidate cache
            _cache.Remove(CacheHelper.ACTIVE_NOTE_FLAGS);
            _logger.LogDebug("Invalidated active note flags cache after update (ID: {Id})", id);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating note flag {Id}", id);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _httpClientService.DeleteAsync($"{BaseUrl}/{id}");
            
            // Invalidate cache
            _cache.Remove(CacheHelper.ACTIVE_NOTE_FLAGS);
            _logger.LogDebug("Invalidated active note flags cache after delete (ID: {Id})", id);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting note flag {Id}", id);
            return false;
        }
    }
}
