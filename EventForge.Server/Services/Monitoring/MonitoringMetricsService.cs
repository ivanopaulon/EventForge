namespace EventForge.Server.Services.Monitoring;

/// <inheritdoc />
public class MonitoringMetricsService : IMonitoringMetricsService
{
    private long _totalPricingOps;
    private long _successfulPricingOps;
    private long _failedPricingOps;
    private double _totalPricingMs;
    private long _totalCacheLookups;
    private long _cacheHits;
    private readonly Lock _lock = new();

    /// <inheritdoc />
    public void RecordPricingOperation(bool success, double elapsedMs)
    {
        lock (_lock)
        {
            _totalPricingOps++;
            _totalPricingMs += elapsedMs;
            if (success)
                _successfulPricingOps++;
            else
                _failedPricingOps++;
        }
    }

    /// <inheritdoc />
    public void RecordCacheLookup(bool hit)
    {
        lock (_lock)
        {
            _totalCacheLookups++;
            if (hit) _cacheHits++;
        }
    }

    /// <inheritdoc />
    public PricingMetricsSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            var avg = _totalPricingOps > 0 ? _totalPricingMs / _totalPricingOps : 0d;
            return new PricingMetricsSnapshot
            {
                TotalPricingOperations = _totalPricingOps,
                SuccessfulPricingOperations = _successfulPricingOps,
                FailedPricingOperations = _failedPricingOps,
                AveragePricingResolutionMs = Math.Round(avg, 2),
                TotalCacheLookups = _totalCacheLookups,
                CacheHits = _cacheHits
            };
        }
    }
}
