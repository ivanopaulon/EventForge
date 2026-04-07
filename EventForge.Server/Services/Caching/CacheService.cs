using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace EventForge.Server.Services.Caching;

public class CacheService(IMemoryCache cache, ILogger<CacheService> logger) : ICacheService
{

    // Track cache keys for pattern invalidation
    private readonly ConcurrentDictionary<string, byte> _cacheKeys = new();

    // Default expiration times
    private static readonly TimeSpan DefaultAbsoluteExpiration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromMinutes(5);

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Guid tenantId,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // Build tenant-specific cache key
        var cacheKey = BuildCacheKey(key, tenantId);

        // Track key for pattern invalidation
        _cacheKeys.TryAdd(cacheKey, 0);

        // Try get from cache
        if (cache.TryGetValue<T>(cacheKey, out var cachedValue))
        {
            logger.LogDebug("Cache HIT for key: {CacheKey}", cacheKey);
            return cachedValue!;
        }

        logger.LogDebug("Cache MISS for key: {CacheKey}", cacheKey);

        // Cache miss - create new entry using GetOrCreateAsync to prevent race conditions
        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetSize(1);  // Count towards size limit
            entry.SetAbsoluteExpiration(absoluteExpiration ?? DefaultAbsoluteExpiration);

            // Only set sliding expiration if absolute expiration is not specified
            // to avoid confusion about when the entry actually expires
            if (!absoluteExpiration.HasValue)
            {
                entry.SetSlidingExpiration(slidingExpiration ?? DefaultSlidingExpiration);
            }

            entry.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                try
                {
                    logger.LogDebug("Cache entry evicted: {Key}, Reason: {Reason}", key, reason);
                    _cacheKeys.TryRemove(key.ToString()!, out _);
                }
                catch (Exception ex)
                {
                    // Suppress exceptions in eviction callback to prevent crashes
                    logger.LogWarning(ex, "Error in cache eviction callback for key: {Key}", key);
                }
            });

            return await factory(ct);
        }) ?? throw new InvalidOperationException("Factory returned null");
    }

    public void Invalidate(string key, Guid tenantId)
    {
        var cacheKey = BuildCacheKey(key, tenantId);
        cache.Remove(cacheKey);
        _cacheKeys.TryRemove(cacheKey, out _);
        logger.LogInformation("Cache invalidated for key: {CacheKey}", cacheKey);
    }

    public void InvalidateTenant(Guid tenantId)
    {
        var pattern = $"*_{tenantId}";
        InvalidatePattern(pattern);
        logger.LogInformation("Cache invalidated for tenant: {TenantId}", tenantId);
    }

    public void InvalidatePattern(string pattern)
    {
        var keysToRemove = _cacheKeys.Keys
            .Where(k => MatchesPattern(k, pattern))
            .ToList();

        foreach (var key in keysToRemove)
        {
            cache.Remove(key);
            _cacheKeys.TryRemove(key, out _);
        }

        logger.LogInformation("Cache invalidated for pattern: {Pattern}, {Count} entries removed",
            pattern, keysToRemove.Count);
    }

    private static string BuildCacheKey(string key, Guid tenantId)
    {
        // Format: prefix_tenantId
        // Example: VatRates_a1b2c3d4-e5f6-7890-abcd-ef1234567890
        return $"{key}_{tenantId}";
    }

    private static bool MatchesPattern(string key, string pattern)
    {
        // Simple wildcard matching (* = any characters)
        // Note: Only supports wildcards at the beginning or end of the pattern
        // Examples: "*_tenantId" or "prefix_*" 
        // Does not support: "prefix_*_suffix"
        if (pattern.StartsWith("*"))
        {
            return key.EndsWith(pattern.TrimStart('*'));
        }
        if (pattern.EndsWith("*"))
        {
            return key.StartsWith(pattern.TrimEnd('*'));
        }
        return key == pattern;
    }

}
