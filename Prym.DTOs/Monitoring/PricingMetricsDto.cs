namespace Prym.DTOs.Monitoring;

/// <summary>
/// Performance and operational metrics for the pricing subsystem.
/// </summary>
public class PricingMetricsDto
{
    /// <summary>
    /// Total number of pricing operations executed since the server started.
    /// </summary>
    public long TotalPricingOperations { get; set; }

    /// <summary>
    /// Number of pricing operations that completed successfully.
    /// </summary>
    public long SuccessfulPricingOperations { get; set; }

    /// <summary>
    /// Number of pricing operations that resulted in an error.
    /// </summary>
    public long FailedPricingOperations { get; set; }

    /// <summary>
    /// Success rate as a percentage (0–100).
    /// </summary>
    public double SuccessRatePercent => TotalPricingOperations > 0
        ? Math.Round((double)SuccessfulPricingOperations / TotalPricingOperations * 100, 1)
        : 0;

    /// <summary>
    /// Average time (in milliseconds) for a pricing resolution operation.
    /// </summary>
    public double AveragePricingResolutionMs { get; set; }

    /// <summary>
    /// Total number of cache lookups for promotions and price lists.
    /// </summary>
    public long TotalCacheLookups { get; set; }

    /// <summary>
    /// Number of cache lookups that hit an existing entry.
    /// </summary>
    public long CacheHits { get; set; }

    /// <summary>
    /// Cache hit rate as a percentage (0–100).
    /// </summary>
    public double CacheHitRatePercent => TotalCacheLookups > 0
        ? Math.Round((double)CacheHits / TotalCacheLookups * 100, 1)
        : 0;

    /// <summary>
    /// Total number of queries executed by the database layer.
    /// </summary>
    public long TotalDbQueries { get; set; }

    /// <summary>
    /// Number of slow queries detected.
    /// </summary>
    public long SlowDbQueries { get; set; }

    /// <summary>
    /// Average database query duration in milliseconds.
    /// </summary>
    public double AverageDbQueryMs { get; set; }
}
