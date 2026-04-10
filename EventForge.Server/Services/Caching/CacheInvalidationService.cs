using Microsoft.AspNetCore.OutputCaching;

namespace EventForge.Server.Services.Caching;

/// <summary>
/// Implementation of cache invalidation service using Output Cache Store
/// </summary>
public class CacheInvalidationService(
    IOutputCacheStore cache,
    ILogger<CacheInvalidationService> logger) : ICacheInvalidationService
{

    public async Task InvalidateStaticEntitiesAsync(CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Invalidating static entities cache");
            await cache.EvictByTagAsync("static", ct);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task InvalidateSemiStaticEntitiesAsync(CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Invalidating semi-static entities cache");
            await cache.EvictByTagAsync("semi-static", ct);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task InvalidateRealTimeEntitiesAsync(CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Invalidating real-time entities cache");
            await cache.EvictByTagAsync("realtime", ct);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task InvalidateByTagAsync(string tag, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Invalidating cache tag: {Tag}", tag);
            await cache.EvictByTagAsync(tag, ct);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

}
