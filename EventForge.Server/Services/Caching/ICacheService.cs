namespace EventForge.Server.Services.Caching;

/// <summary>
/// Generic caching service with multi-tenant isolation
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get or create cached data for a specific tenant
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Guid tenantId,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken ct = default);

    /// <summary>
    /// Invalidate cache for a specific key and tenant
    /// </summary>
    void Invalidate(string key, Guid tenantId);

    /// <summary>
    /// Invalidate all cache entries for a tenant
    /// </summary>
    void InvalidateTenant(Guid tenantId);

    /// <summary>
    /// Invalidate all cache entries matching a pattern
    /// </summary>
    void InvalidatePattern(string pattern);
}
