using Prym.Web.Helpers;
using Prym.DTOs.Common;
using Prym.DTOs.UnitOfMeasures;
using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace Prym.Web.Services;

/// <summary>
/// Service implementation for managing units of measure.
/// </summary>
public class UMService(
    IHttpClientService httpClientService,
    ILogger<UMService> logger,
    IMemoryCache cache) : IUMService
{
    private const string BaseUrl = "api/v1/product-management/units";

    public async Task<PagedResult<UMDto>> GetUMsAsync(int page = 1, int pageSize = 100, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.GetAsync<PagedResult<UMDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}", ct);
            return result ?? new PagedResult<UMDto> { Items = [], TotalCount = 0, Page = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving units of measure (page={Page}, pageSize={PageSize})", page, pageSize);
            throw;
        }
    }

    public async Task<IEnumerable<UMDto>> GetUnitsOfMeasureAsync(CancellationToken ct = default)
    {
        // Try cache first
        if (cache.TryGetValue(CacheHelper.ACTIVE_UNITS_OF_MEASURE, out IEnumerable<UMDto>? cached) && cached is not null)
        {
            logger.LogDebug("Cache HIT: Active units of measure ({Count} items)", cached.Count());
            return cached;
        }

        // Cache miss - API call
        logger.LogDebug("Cache MISS: Loading active units of measure from API");

        try
        {
            var result = await GetUMsAsync(1, 100, ct);
            var activeUnits = result?.Items?.Where(um => um.IsActive) ?? Enumerable.Empty<UMDto>();

            // Store in cache (30 minutes)
            cache.Set(
                CacheHelper.ACTIVE_UNITS_OF_MEASURE,
                activeUnits,
                CacheHelper.GetMediumCacheOptions()
            );

            logger.LogInformation(
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
        try
        {
            return await httpClientService.GetAsync<UMDto>($"{BaseUrl}/{id}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving unit of measure {Id}", id);
            throw;
        }
    }

    public async Task<UMDto> CreateUMAsync(CreateUMDto createUMDto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<CreateUMDto, UMDto>(BaseUrl, createUMDto, ct);

            // Invalidate cache
            cache.Remove(CacheHelper.ACTIVE_UNITS_OF_MEASURE);
            logger.LogDebug("Invalidated active units cache after create");

            return result ?? throw new InvalidOperationException("Failed to create unit of measure");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating unit of measure");
            throw;
        }
    }

    public async Task<UMDto?> UpdateUMAsync(Guid id, UpdateUMDto updateUMDto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PutAsync<UpdateUMDto, UMDto>($"{BaseUrl}/{id}", updateUMDto, ct);

            // Invalidate cache
            cache.Remove(CacheHelper.ACTIVE_UNITS_OF_MEASURE);
            logger.LogDebug("Invalidated active units cache after update (ID: {Id})", id);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating unit of measure {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteUMAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{id}", ct);

            // Invalidate cache
            cache.Remove(CacheHelper.ACTIVE_UNITS_OF_MEASURE);
            logger.LogDebug("Invalidated active units cache after delete (ID: {Id})", id);

            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Business logic: return false if not found (no logging)
            return false;
        }
    }
}
