using Microsoft.AspNetCore.OutputCaching;

namespace EventForge.Server.Services.Caching;

/// <summary>
/// Implementation of cache invalidation service using Output Cache Store
/// </summary>
public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly IOutputCacheStore _cache;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(
        IOutputCacheStore cache,
        ILogger<CacheInvalidationService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task InvalidateStaticEntitiesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Invalidating static entities cache");
        await _cache.EvictByTagAsync("static", ct);
    }

    public async Task InvalidateSemiStaticEntitiesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Invalidating semi-static entities cache");
        await _cache.EvictByTagAsync("semi-static", ct);
    }

    public async Task InvalidateRealTimeEntitiesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Invalidating real-time entities cache");
        await _cache.EvictByTagAsync("realtime", ct);
    }

    public async Task InvalidateByTagAsync(string tag, CancellationToken ct = default)
    {
        _logger.LogInformation("Invalidating cache tag: {Tag}", tag);
        await _cache.EvictByTagAsync(tag, ct);
    }
}
