using Prym.Client.Helpers;
using Prym.DTOs.Common;
using Prym.DTOs.Sales;
using Microsoft.Extensions.Caching.Memory;

namespace Prym.Client.Services.Sales;

/// <summary>
/// Client service implementation for note flags.
/// </summary>
public class NoteFlagService(
    IHttpClientService httpClientService,
    ILogger<NoteFlagService> logger,
    IMemoryCache cache) : INoteFlagService
{
    private const string BaseUrl = "api/v1/note-flags";

    public async Task<List<NoteFlagDto>?> GetAllAsync()
    {
        try
        {
            return await httpClientService.GetAsync<List<NoteFlagDto>>(BaseUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all note flags");
            return null;
        }
    }

    public async Task<List<NoteFlagDto>?> GetActiveAsync()
    {
        // Try cache first
        if (cache.TryGetValue(CacheHelper.ACTIVE_NOTE_FLAGS, out List<NoteFlagDto>? cached) && cached != null)
        {
            logger.LogDebug("Cache HIT: Active note flags ({Count} items)", cached.Count);
            return cached;
        }

        // Cache miss - API call
        logger.LogDebug("Cache MISS: Loading active note flags from API");

        try
        {
            var pagedResult = await httpClientService.GetAsync<PagedResult<NoteFlagDto>>($"{BaseUrl}/active");
            var activeNoteFlags = pagedResult?.Items?.ToList() ?? [];

            // Store in cache (60 minutes - LongCache)
            cache.Set(
                CacheHelper.ACTIVE_NOTE_FLAGS,
                activeNoteFlags,
                CacheHelper.GetLongCacheOptions()
            );

            logger.LogInformation(
                "Cached {Count} active note flags for {Minutes} minutes",
                activeNoteFlags.Count,
                CacheHelper.LongCache.TotalMinutes
            );

            return activeNoteFlags;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving active note flags");
            return null;
        }
    }

    public async Task<NoteFlagDto?> GetByIdAsync(Guid id)
    {
        try
        {
            return await httpClientService.GetAsync<NoteFlagDto>($"{BaseUrl}/{id}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving note flag {Id}", id);
            return null;
        }
    }

    public async Task<NoteFlagDto?> CreateAsync(CreateNoteFlagDto createDto)
    {
        try
        {
            var result = await httpClientService.PostAsync<CreateNoteFlagDto, NoteFlagDto>(BaseUrl, createDto);

            // Invalidate cache
            cache.Remove(CacheHelper.ACTIVE_NOTE_FLAGS);
            logger.LogDebug("Invalidated active note flags cache after create");

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating note flag");
            return null;
        }
    }

    public async Task<NoteFlagDto?> UpdateAsync(Guid id, UpdateNoteFlagDto updateDto)
    {
        try
        {
            var result = await httpClientService.PutAsync<UpdateNoteFlagDto, NoteFlagDto>($"{BaseUrl}/{id}", updateDto);

            // Invalidate cache
            cache.Remove(CacheHelper.ACTIVE_NOTE_FLAGS);
            logger.LogDebug("Invalidated active note flags cache after update (ID: {Id})", id);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating note flag {Id}", id);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{id}");

            // Invalidate cache
            cache.Remove(CacheHelper.ACTIVE_NOTE_FLAGS);
            logger.LogDebug("Invalidated active note flags cache after delete (ID: {Id})", id);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting note flag {Id}", id);
            return false;
        }
    }
}
