namespace Prym.Server.Services.Monitoring;

/// <summary>
/// Singleton service for accumulating in-memory metrics about pricing operations and cache usage.
/// This service is thread-safe and is intended to be registered as a singleton.
/// </summary>
public interface IMonitoringMetricsService
{
    /// <summary>
    /// Records a completed pricing resolution operation.
    /// </summary>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="elapsedMs">Duration of the operation in milliseconds.</param>
    void RecordPricingOperation(bool success, double elapsedMs);

    /// <summary>
    /// Records a cache lookup result (promotion or price list cache).
    /// </summary>
    /// <param name="hit">Whether the lookup was a cache hit.</param>
    void RecordCacheLookup(bool hit);

    /// <summary>
    /// Returns a snapshot of the current accumulated metrics.
    /// </summary>
    PricingMetricsSnapshot GetSnapshot();
}

/// <summary>
/// Snapshot of the current in-memory pricing metrics.
/// </summary>
public class PricingMetricsSnapshot
{
    public long TotalPricingOperations { get; init; }
    public long SuccessfulPricingOperations { get; init; }
    public long FailedPricingOperations { get; init; }
    public double AveragePricingResolutionMs { get; init; }
    public long TotalCacheLookups { get; init; }
    public long CacheHits { get; init; }
}
