using EventForge.Client.Helpers;
using EventForge.DTOs.Common;
using EventForge.DTOs.Products;
using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace EventForge.Client.Services;

/// <summary>
/// Service implementation for managing models.
/// </summary>
public class ModelService(
    IHttpClientService httpClientService,
    ILogger<ModelService> logger,
    IMemoryCache cache) : IModelService
{
    private const string BaseUrl = "api/v1/product-management/models";

    public async Task<PagedResult<ModelDto>> GetModelsAsync(int page = 1, int pageSize = 100, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.GetAsync<PagedResult<ModelDto>>($"{BaseUrl}?page={page}&pageSize={pageSize}", ct);
            return result ?? new PagedResult<ModelDto> { Items = [], TotalCount = 0, Page = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving models");
            throw;
        }
    }

    public async Task<PagedResult<ModelDto>> GetModelsByBrandIdAsync(Guid brandId, int page = 1, int pageSize = 100, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.GetAsync<PagedResult<ModelDto>>($"{BaseUrl}?brandId={brandId}&page={page}&pageSize={pageSize}", ct);
            return result ?? new PagedResult<ModelDto> { Items = [], TotalCount = 0, Page = page, PageSize = pageSize };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving models for brand {BrandId}", brandId);
            throw;
        }
    }

    public async Task<IEnumerable<ModelDto>> GetActiveModelsByBrandAsync(Guid brandId, CancellationToken ct = default)
    {

        var cacheKey = CacheHelper.GetModelsByBrandKey(brandId);

        // Try cache first
        if (cache.TryGetValue(cacheKey, out IEnumerable<ModelDto>? cached) && cached is not null)
        {
            logger.LogDebug("Cache HIT: Models for brand {BrandId} ({Count} items)", brandId, cached.Count());
            return cached;
        }

        // Cache miss - API call
        logger.LogDebug("Cache MISS: Loading models for brand {BrandId} from API", brandId);

        try
        {
            var result = await GetModelsByBrandIdAsync(brandId, 1, 100, ct);
            var models = result?.Items ?? Enumerable.Empty<ModelDto>();

            // Store in cache (15 minutes)
            cache.Set(
                cacheKey,
                models,
                CacheHelper.GetShortCacheOptions()
            );

            logger.LogInformation(
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

    public async Task<ModelDto?> GetModelByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await httpClientService.GetAsync<ModelDto>($"{BaseUrl}/{id}", ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving model with ID {Id}", id);
            throw;
        }
    }

    public async Task<ModelDto> CreateModelAsync(CreateModelDto createModelDto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PostAsync<CreateModelDto, ModelDto>(BaseUrl, createModelDto, ct);

            // Invalidate cache for specific brand
            if (result is not null)
            {
                var cacheKey = CacheHelper.GetModelsByBrandKey(result.BrandId);
                cache.Remove(cacheKey);
                logger.LogDebug("Invalidated models cache for brand {BrandId} after create", result.BrandId);
            }

            return result ?? throw new InvalidOperationException("Failed to create model");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating model");
            throw;
        }
    }

    public async Task<ModelDto?> UpdateModelAsync(Guid id, UpdateModelDto updateModelDto, CancellationToken ct = default)
    {
        try
        {
            var result = await httpClientService.PutAsync<UpdateModelDto, ModelDto>($"{BaseUrl}/{id}", updateModelDto, ct);

            // Invalidate cache for brand
            if (result is not null)
            {
                var cacheKey = CacheHelper.GetModelsByBrandKey(result.BrandId);
                cache.Remove(cacheKey);
                logger.LogDebug("Invalidated models cache for brand {BrandId} after update (model {ModelId})",
                    result.BrandId, id);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating model with ID {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteModelAsync(Guid id, CancellationToken ct = default)
    {
        // Problem: We don't know BrandId before delete
        // Solution: GET model first (+1 API call, acceptable trade-off)
        ModelDto? model = null;
        try
        {
            model = await GetModelByIdAsync(id, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch model {ModelId} before delete - cache invalidation may be skipped", id);
        }

        try
        {
            await httpClientService.DeleteAsync($"{BaseUrl}/{id}", ct);

            // Invalidate if model existed
            if (model is not null)
            {
                var cacheKey = CacheHelper.GetModelsByBrandKey(model.BrandId);
                cache.Remove(cacheKey);
                logger.LogDebug("Invalidated models cache for brand {BrandId} after delete (model {ModelId})",
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
            logger.LogError(ex, "Error deleting model with ID {Id}", id);
            throw;
        }
    }
}
