using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace EventForge.Client.Services.Performance;

/// <summary>
/// Service for performance optimizations including caching, debouncing, and data management.
/// Provides centralized performance monitoring and optimization strategies.
/// </summary>
public interface IPerformanceOptimizationService
{
    Task<T?> GetCachedDataAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    void InvalidateCache(string key);
    void InvalidateCachePattern(string pattern);
    Task<T> DebounceAsync<T>(string key, Func<Task<T>> operation, TimeSpan delay);
    void PreloadData<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    void SetCacheExpiration(string key, TimeSpan expiration);
    bool IsCached(string key);
    void ClearExpiredCache();
    IEnumerable<string> GetCacheKeys();
}

/// <summary>
/// Implementation of performance optimization service with intelligent caching and debouncing.
/// </summary>
public class PerformanceOptimizationService : IPerformanceOptimizationService, IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<PerformanceOptimizationService> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _lockSemaphores;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _debounceCancellations;
    private readonly Timer _cacheCleanupTimer;

    private readonly MemoryCacheEntryOptions _defaultCacheOptions;

    public PerformanceOptimizationService(
        IMemoryCache cache,
        ILogger<PerformanceOptimizationService> logger)
    {
        _cache = cache;
        _logger = logger;
        _lockSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
        _debounceCancellations = new ConcurrentDictionary<string, CancellationTokenSource>();

        // Set up default cache options optimized for mobile and high load scenarios
        _defaultCacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(15), // Longer expiration for mobile
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2),
            Priority = CacheItemPriority.Normal,
            Size = 1
        };

        // Setup cache cleanup timer to run every 5 minutes
        _cacheCleanupTimer = new Timer(async _ => await CleanupExpiredCacheAsync(),
            null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

        _logger.LogInformation("Performance optimization service initialized with mobile-optimized caching");
    }

    /// <summary>
    /// Gets cached data or executes factory function with intelligent cache management.
    /// </summary>
    public async Task<T?> GetCachedDataAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        // Try to get from cache first
        if (_cache.TryGetValue(key, out T? cachedValue))
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return cachedValue;
        }

        // Use semaphore to prevent multiple concurrent requests for same data
        var semaphore = _lockSemaphores.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_cache.TryGetValue(key, out cachedValue))
            {
                return cachedValue;
            }

            _logger.LogDebug("Cache miss for key: {Key}, executing factory", key);

            // Execute factory and cache result
            var result = await factory();

            var cacheOptions = _defaultCacheOptions;
            if (expiration.HasValue)
            {
                cacheOptions = new MemoryCacheEntryOptions
                {
                    SlidingExpiration = _defaultCacheOptions.SlidingExpiration,
                    Priority = _defaultCacheOptions.Priority,
                    Size = _defaultCacheOptions.Size,
                    AbsoluteExpirationRelativeToNow = expiration.Value
                };
            }

            _ = _cache.Set(key, result, cacheOptions);
            _logger.LogDebug("Cached result for key: {Key}", key);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing factory for cache key: {Key}", key);
            throw;
        }
        finally
        {
            _ = semaphore.Release();
        }
    }

    /// <summary>
    /// Invalidates specific cache entry.
    /// </summary>
    public void InvalidateCache(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        _cache.Remove(key);
        _logger.LogDebug("Cache invalidated for key: {Key}", key);
    }

    /// <summary>
    /// Invalidates cache entries matching a pattern (prefix-based).
    /// </summary>
    public void InvalidateCachePattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return;

        // For MemoryCache, we need to track keys ourselves for pattern invalidation
        // This is a limitation of IMemoryCache interface
        var keysToRemove = GetCacheKeys().Where(key => key.StartsWith(pattern)).ToList();

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }

        _logger.LogDebug("Cache invalidated for pattern: {Pattern}, removed {Count} entries",
            pattern, keysToRemove.Count);
    }

    /// <summary>
    /// Debounces an operation to reduce excessive API calls.
    /// </summary>
    public async Task<T> DebounceAsync<T>(string key, Func<Task<T>> operation, TimeSpan delay)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Debounce key cannot be null or empty", nameof(key));

        // Cancel previous operation if it exists
        if (_debounceCancellations.TryGetValue(key, out var existingCts))
        {
            existingCts.Cancel();
        }

        // Create new cancellation token
        var newCts = new CancellationTokenSource();
        _debounceCancellations[key] = newCts;

        try
        {
            // Wait for debounce delay
            await Task.Delay(delay, newCts.Token);

            // Execute operation if not cancelled
            _logger.LogDebug("Executing debounced operation for key: {Key}", key);
            return await operation();
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Debounced operation cancelled for key: {Key}", key);
            throw;
        }
        finally
        {
            // Clean up
            _ = _debounceCancellations.TryRemove(key, out _);
            newCts.Dispose();
        }
    }

    /// <summary>
    /// Preloads data into cache for improved perceived performance.
    /// </summary>
    public async void PreloadData<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        try
        {
            _ = await GetCachedDataAsync(key, factory, expiration);
            _logger.LogDebug("Preloaded data for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to preload data for key: {Key}", key);
        }
    }

    /// <summary>
    /// Sets custom expiration for a cached item.
    /// </summary>
    public void SetCacheExpiration(string key, TimeSpan expiration)
    {
        if (_cache.TryGetValue(key, out var value))
        {
            var options = new MemoryCacheEntryOptions
            {
                SlidingExpiration = _defaultCacheOptions.SlidingExpiration,
                Priority = _defaultCacheOptions.Priority,
                Size = _defaultCacheOptions.Size,
                AbsoluteExpirationRelativeToNow = expiration
            };
            _ = _cache.Set(key, value, options);
        }
    }

    /// <summary>
    /// Checks if a key is currently cached.
    /// </summary>
    public bool IsCached(string key)
    {
        return !string.IsNullOrWhiteSpace(key) && _cache.TryGetValue(key, out _);
    }

    /// <summary>
    /// Manually clears expired cache entries.
    /// </summary>
    public void ClearExpiredCache()
    {
        // MemoryCache automatically handles expiration, but we can trigger compaction
        if (_cache is MemoryCache memCache)
        {
            memCache.Compact(0.25); // Remove 25% of cache entries
            _logger.LogDebug("Cache compaction triggered");
        }
    }

    /// <summary>
    /// Gets all current cache keys (requires reflection for MemoryCache).
    /// </summary>
    public IEnumerable<string> GetCacheKeys()
    {
        // Note: This is a limitation of IMemoryCache interface
        // In a production environment, consider using a cache implementation that tracks keys
        // For now, returning empty enumerable as MemoryCache doesn't expose keys
        return Enumerable.Empty<string>();
    }

    /// <summary>
    /// Asynchronously cleans up expired cache entries.
    /// </summary>
    private async Task CleanupExpiredCacheAsync()
    {
        try
        {
            await Task.Run(() => ClearExpiredCache());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during cache cleanup");
        }
    }

    public void Dispose()
    {
        _cacheCleanupTimer?.Dispose();

        // Cancel all pending debounce operations
        foreach (var cts in _debounceCancellations.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _debounceCancellations.Clear();

        // Dispose semaphores
        foreach (var semaphore in _lockSemaphores.Values)
        {
            semaphore.Dispose();
        }
        _lockSemaphores.Clear();

        _logger.LogInformation("Performance optimization service disposed");
    }
}

/// <summary>
/// Cache key constants for consistent cache management.
/// </summary>
public static class CacheKeys
{
    public const string CHAT_LIST = "chat_list";
    public const string CHAT_MESSAGES_PREFIX = "chat_messages_";
    public const string NOTIFICATION_LIST = "notification_list";
    public const string USER_PROFILE = "user_profile";
    public const string TENANT_INFO = "tenant_info";
    public const string CONFIG_SETTINGS = "config_settings";

    public static string ChatMessages(Guid chatId) => $"{CHAT_MESSAGES_PREFIX}{chatId}";
    public static string UserData(Guid userId) => $"user_data_{userId}";
    public static string TenantData(Guid tenantId) => $"tenant_data_{tenantId}";
}

/// <summary>
/// Debounce key constants for consistent debouncing.
/// </summary>
public static class DebounceKeys
{
    public const string CHAT_SEARCH = "chat_search";
    public const string NOTIFICATION_SEARCH = "notification_search";
    public const string USER_SEARCH = "user_search";
    public const string TYPING_INDICATOR = "typing_indicator";
}