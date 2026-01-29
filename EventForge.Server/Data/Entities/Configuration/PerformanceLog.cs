using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Configuration;

/// <summary>
/// Represents a performance metrics log entry.
/// </summary>
public class PerformanceLog : AuditableEntity
{
    /// <summary>
    /// Timestamp when the metrics were collected.
    /// </summary>
    [Required]
    [Display(Name = "Timestamp", Description = "Metrics collection timestamp.")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Number of requests per minute.
    /// </summary>
    [Display(Name = "Requests Per Minute", Description = "Request rate.")]
    public double RequestsPerMinute { get; set; }

    /// <summary>
    /// Average response time in milliseconds.
    /// </summary>
    [Display(Name = "Avg Response Time (ms)", Description = "Average response time.")]
    public double AvgResponseTimeMs { get; set; }

    /// <summary>
    /// Memory usage in megabytes.
    /// </summary>
    [Display(Name = "Memory Usage (MB)", Description = "Memory usage.")]
    public long MemoryUsageMB { get; set; }

    /// <summary>
    /// CPU usage percentage.
    /// </summary>
    [Display(Name = "CPU Usage (%)", Description = "CPU usage percentage.")]
    public double CpuUsagePercent { get; set; }
}
