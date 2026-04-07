namespace EventForge.DTOs.Dashboard;

/// <summary>
/// Performance metrics.
/// </summary>
public class PerformanceMetrics
{
    public DateTime Timestamp { get; set; }
    public double RequestsPerMinute { get; set; }
    public double AvgResponseTimeMs { get; set; }
    public long MemoryUsageMB { get; set; }
    public double CpuUsagePercent { get; set; }
    public List<SlowQueryDto> SlowQueries { get; set; } = new();
}
