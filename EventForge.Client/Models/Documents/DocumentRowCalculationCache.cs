namespace EventForge.Client.Models.Documents;

/// <summary>
/// Cache for document row financial calculations
/// Prevents redundant recalculations during rendering
/// </summary>
public class DocumentRowCalculationCache
{
    private readonly Dictionary<string, object> _cache = new();

    /// <summary>
    /// Gets or calculates a value with automatic caching
    /// </summary>
    /// <typeparam name="TKey">Type of cache key (must be value type for hashing)</typeparam>
    /// <typeparam name="TValue">Type of cached value</typeparam>
    /// <param name="key">Cache key based on input parameters</param>
    /// <param name="calculator">Function to calculate value if not cached</param>
    /// <returns>Cached or newly calculated value</returns>
    public TValue GetOrCalculate<TKey, TValue>(TKey key, Func<TValue> calculator) 
        where TKey : notnull
    {
        var cacheKey = $"{typeof(TValue).Name}_{key.GetHashCode()}";

        if (_cache.TryGetValue(cacheKey, out var cachedValue))
        {
            return (TValue)cachedValue;
        }

        var value = calculator();
        _cache[cacheKey] = value!;
        return value;
    }

    /// <summary>
    /// Invalidates all cached calculations
    /// Call when any input value changes
    /// </summary>
    public void Invalidate()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Invalidates calculations for a specific key pattern
    /// </summary>
    public void InvalidatePattern(string pattern)
    {
        var keysToRemove = _cache.Keys.Where(k => k.Contains(pattern)).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }
    }
}
