using Microsoft.Extensions.Caching.Memory;

namespace EventForge.Client.Helpers;

/// <summary>
/// Centralized cache keys and durations for client-side caching.
/// Provides consistent cache key naming and expiration policies.
/// </summary>
public static class CacheHelper
{
    #region Cache Keys for Lookup Tables
    
    /// <summary>
    /// Cache key for active units of measure
    /// </summary>
    public const string ACTIVE_UNITS_OF_MEASURE = "cache:lookup:um:active";
    
    /// <summary>
    /// Cache key for active brands
    /// </summary>
    public const string ACTIVE_BRANDS = "cache:lookup:brands:active";
    
    /// <summary>
    /// Cache key pattern for active models by brand ID
    /// Usage: string.Format(ACTIVE_MODELS_BY_BRAND, brandId)
    /// </summary>
    public const string ACTIVE_MODELS_BY_BRAND = "cache:lookup:models:active:{0}";
    
    /// <summary>
    /// Cache key for active payment methods
    /// </summary>
    public const string ACTIVE_PAYMENT_METHODS = "cache:lookup:payment_methods:active";
    
    /// <summary>
    /// Cache key for active note flags
    /// </summary>
    public const string ACTIVE_NOTE_FLAGS = "cache:lookup:note_flags:active";
    
    /// <summary>
    /// Cache key for VAT rates (all)
    /// </summary>
    public const string VAT_RATES = "cache:lookup:vat_rates:all";
    
    /// <summary>
    /// Cache key pattern for single entity by type and ID
    /// Usage: string.Format(ENTITY_BY_ID, entityType, id)
    /// Example: "cache:entity:um:12345678-1234-1234-1234-123456789012"
    /// </summary>
    public const string ENTITY_BY_ID = "cache:entity:{0}:{1}";
    
    #endregion
    
    #region Cache Durations
    
    /// <summary>
    /// Short cache duration: 15 minutes
    /// Recommended for: Brands, Models (frequently updated by users)
    /// </summary>
    public static readonly TimeSpan ShortCache = TimeSpan.FromMinutes(15);
    
    /// <summary>
    /// Medium cache duration: 30 minutes
    /// Recommended for: Units of Measure (occasionally updated)
    /// </summary>
    public static readonly TimeSpan MediumCache = TimeSpan.FromMinutes(30);
    
    /// <summary>
    /// Long cache duration: 60 minutes
    /// Recommended for: Payment Methods, Note Flags (rarely updated)
    /// </summary>
    public static readonly TimeSpan LongCache = TimeSpan.FromMinutes(60);
    
    /// <summary>
    /// Extra long cache duration: 24 hours
    /// Recommended for: VAT Rates (very rarely updated, tax compliance data)
    /// </summary>
    public static readonly TimeSpan ExtraLongCache = TimeSpan.FromHours(24);
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Creates a cache key for active models filtered by brand ID
    /// </summary>
    /// <param name="brandId">Brand ID to filter models</param>
    /// <returns>Formatted cache key</returns>
    public static string GetModelsByBrandKey(Guid brandId)
    {
        return string.Format(ACTIVE_MODELS_BY_BRAND, brandId);
    }
    
    /// <summary>
    /// Creates a cache key for a single entity by type and ID
    /// </summary>
    /// <param name="entityType">Entity type (e.g., "um", "brand", "product")</param>
    /// <param name="id">Entity ID</param>
    /// <returns>Formatted cache key</returns>
    public static string GetEntityByIdKey(string entityType, Guid id)
    {
        return string.Format(ENTITY_BY_ID, entityType.ToLowerInvariant(), id);
    }
    
    /// <summary>
    /// Creates MemoryCacheEntryOptions for short duration cache
    /// </summary>
    /// <returns>Cache options with 15-minute expiration</returns>
    public static MemoryCacheEntryOptions GetShortCacheOptions()
    {
        return new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ShortCache,
            Size = 1 // Each entry counts as size 1 towards SizeLimit
        };
    }
    
    /// <summary>
    /// Creates MemoryCacheEntryOptions for medium duration cache
    /// </summary>
    /// <returns>Cache options with 30-minute expiration</returns>
    public static MemoryCacheEntryOptions GetMediumCacheOptions()
    {
        return new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = MediumCache,
            Size = 1
        };
    }
    
    /// <summary>
    /// Creates MemoryCacheEntryOptions for long duration cache
    /// </summary>
    /// <returns>Cache options with 60-minute expiration</returns>
    public static MemoryCacheEntryOptions GetLongCacheOptions()
    {
        return new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = LongCache,
            Size = 1
        };
    }
    
    /// <summary>
    /// Creates MemoryCacheEntryOptions for extra long duration cache
    /// </summary>
    /// <returns>Cache options with 24-hour expiration</returns>
    public static MemoryCacheEntryOptions GetExtraLongCacheOptions()
    {
        return new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ExtraLongCache,
            Size = 1
        };
    }
    
    #endregion
}
