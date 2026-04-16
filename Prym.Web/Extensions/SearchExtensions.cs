using System.Collections.Concurrent;
using System.Reflection;

namespace Prym.Web.Extensions;

/// <summary>
/// Extension methods for search functionality in EFTable
/// </summary>
public static class SearchExtensions
{
    // Cache of PropertyInfo objects to avoid repeated reflection calls per type+property combination
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> _propertyCache = new();

    /// <summary>
    /// Checks if an item matches the search term in any of the specified searchable properties.
    /// </summary>
    /// <typeparam name="T">The type of item to search</typeparam>
    /// <param name="item">The item to check</param>
    /// <param name="searchTerm">The search term to look for</param>
    /// <param name="searchablePropertyNames">Collection of property names to search in</param>
    /// <returns>True if the search term is found in any searchable property, false otherwise</returns>
    public static bool MatchesSearchInColumns<T>(
        this T item,
        string? searchTerm,
        IEnumerable<string> searchablePropertyNames)
    {
        // If no search term, item matches (show all items)
        if (string.IsNullOrWhiteSpace(searchTerm))
            return true;

        var searchLower = searchTerm.ToLowerInvariant();
        var type = typeof(T);

        // Check each searchable property using cached PropertyInfo
        foreach (var propName in searchablePropertyNames)
        {
            var prop = _propertyCache.GetOrAdd((type, propName), key => key.Item1.GetProperty(key.Item2));

            var propValue = prop?.GetValue(item)?.ToString()?.ToLowerInvariant();

            if (!string.IsNullOrEmpty(propValue) && propValue.Contains(searchLower))
                return true;
        }

        return false;
    }
}
