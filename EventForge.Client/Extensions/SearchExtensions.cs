namespace EventForge.Client.Extensions;

/// <summary>
/// Extension methods for search functionality in EFTable
/// </summary>
public static class SearchExtensions
{
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

        // Check each searchable property
        foreach (var propName in searchablePropertyNames)
        {
            var propValue = item?.GetType()
                .GetProperty(propName)?
                .GetValue(item)?
                .ToString()?
                .ToLowerInvariant();

            if (!string.IsNullOrEmpty(propValue) && propValue.Contains(searchLower))
                return true;
        }

        return false;
    }
}
