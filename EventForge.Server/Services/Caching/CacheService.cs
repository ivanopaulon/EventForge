using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace EventForge.Server.Services.Caching;

public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;
    
    // Track cache keys for pattern invalidation
    private readonly ConcurrentDictionary<string, byte> _cacheKeys = new();
    
    // Default expiration times
    private static readonly TimeSpan DefaultAbsoluteExpiration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromMinutes(5);

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Guid tenantId,
        Func<Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null)
    {
        // Build tenant-specific cache key
        var cacheKey = BuildCacheKey(key, tenantId);
        
        // Track key for pattern invalidation
        _cacheKeys.TryAdd(cacheKey, 0);

        // Try get from cache
        if (_cache.TryGetValue(cacheKey, out T? cachedValue) && cachedValue != null)
        {
            _logger.LogDebug("Cache HIT for key: {CacheKey}", cacheKey);
            return cachedValue;
        }

        _logger.LogDebug("Cache MISS for key: {CacheKey}", cacheKey);

        // Cache miss - create new entry
        var value = await factory();

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSize(1)  // Count towards size limit
            .SetAbsoluteExpiration(absoluteExpiration ?? DefaultAbsoluteExpiration)
            .SetSlidingExpiration(slidingExpiration ?? DefaultSlidingExpiration)
            .RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                _logger.LogDebug("Cache entry evicted: {Key}, Reason: {Reason}", key, reason);
                _cacheKeys.TryRemove(key.ToString()!, out _);
            });

        _cache.Set(cacheKey, value, cacheEntryOptions);
        
        return value;
    }

    public void Invalidate(string key, Guid tenantId)
    {
        var cacheKey = BuildCacheKey(key, tenantId);
        _cache.Remove(cacheKey);
        _cacheKeys.TryRemove(cacheKey, out _);
        _logger.LogInformation("Cache invalidated for key: {CacheKey}", cacheKey);
    }

    public void InvalidateTenant(Guid tenantId)
    {
        var pattern = $"*_{tenantId}";
        InvalidatePattern(pattern);
        _logger.LogInformation("Cache invalidated for tenant: {TenantId}", tenantId);
    }

    public void InvalidatePattern(string pattern)
    {
        var keysToRemove = _cacheKeys.Keys
            .Where(k => MatchesPattern(k, pattern))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
            _cacheKeys.TryRemove(key, out _);
        }

        _logger.LogInformation("Cache invalidated for pattern: {Pattern}, {Count} entries removed", 
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
