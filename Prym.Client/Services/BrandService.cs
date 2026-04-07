using Prym.Client.Helpers;
using Prym.DTOs.Common;
using Prym.DTOs.Products;
using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace Prym.Client.Services;

/// <summary>
/// Service implementation for managing brands.
/// </summary>
public class BrandService(
    IHttpClientService httpClientService,
    ILogger<BrandService> logger,
    IMemoryCache cache) : IBrandService
{
    private const string BaseUrl = "api/v1/product-management/brands";

    public async Task<PagedResult<BrandDto>> GetBrandsAsync(int page = 1, int pageSize = 100, CancellationToken ct = default)
    {
        var result = await httpClientService.GetAsync<PagedResult<BrandDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}", ct);
        return result ?? new PagedResult<BrandDto> { Items = [], TotalCount = 0, Page = page, PageSize = pageSize };
    }

    public async Task<IEnumerable<BrandDto>> GetActiveBrandsAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheHelper.ACTIVE_BRANDS, out IEnumerable<BrandDto>? cached) && cached != null)
        {
            logger.LogDebug("Cache HIT: Brands ({Count} items)", cached.Count());
            return cached;
        }

        logger.LogDebug("Cache MISS: Loading brands from API");

        try
        {
            var result = await GetBrandsAsync(1, 100, ct);
            var brands = result?.Items ?? Enumerable.Empty<BrandDto>();

            cache.Set(
                CacheHelper.ACTIVE_BRANDS,
                brands,
                CacheHelper.GetShortCacheOptions()
            );

            logger.LogInformation(
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
        return await httpClientService.GetAsync<BrandDto>($"{BaseUrl}/{id}", ct);
    }

    public async Task<BrandDto> CreateBrandAsync(CreateBrandDto createBrandDto, CancellationToken ct = default)
    {
        var result = await httpClientService.PostAsync<CreateBrandDto, BrandDto>(BaseUrl, createBrandDto, ct);

        // Invalidate cache
        cache.Remove(CacheHelper.ACTIVE_BRANDS);
        logger.LogDebug("Invalidated brands cache after create");

        return result ?? throw new InvalidOperationException("Failed to create brand");
    }

    public async Task<BrandDto?> UpdateBrandAsync(Guid id, UpdateBrandDto updateBrandDto, CancellationToken ct = default)
    {
        var result = await httpClientService.PutAsync<UpdateBrandDto, BrandDto>($"{BaseUrl}/{id}", updateBrandDto, ct);

        // Invalidate cache
        cache.Remove(CacheHelper.ACTIVE_BRANDS);
        logger.LogDebug("Invalidated brands cache after update (ID: {Id})", id);

        return result;
    }

    public async Task<bool> DeleteBrandAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{id}", ct);

            // Invalidate cache
            cache.Remove(CacheHelper.ACTIVE_BRANDS);
            logger.LogDebug("Invalidated brands cache after delete (ID: {Id})", id);

            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Business logic: return false if not found (no logging)
            return false;
        }
    }
}
