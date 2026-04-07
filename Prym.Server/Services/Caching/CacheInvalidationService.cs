using Microsoft.AspNetCore.OutputCaching;

namespace Prym.Server.Services.Caching;

/// <summary>
/// Implementation of cache invalidation service using Output Cache Store
/// </summary>
public class CacheInvalidationService(
    IOutputCacheStore cache,
    ILogger<CacheInvalidationService> logger) : ICacheInvalidationService
{

    public async Task InvalidateStaticEntitiesAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Invalidating static entities cache");
        await cache.EvictByTagAsync("static", ct);
    }

    public async Task InvalidateSemiStaticEntitiesAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Invalidating semi-static entities cache");
        await cache.EvictByTagAsync("semi-static", ct);
    }

    public async Task InvalidateRealTimeEntitiesAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Invalidating real-time entities cache");
        await cache.EvictByTagAsync("realtime", ct);
    }

    public async Task InvalidateByTagAsync(string tag, CancellationToken ct = default)
    {
        logger.LogInformation("Invalidating cache tag: {Tag}", tag);
        await cache.EvictByTagAsync(tag, ct);
    }

}
