namespace EventForge.DTOs.Dashboard;

/// <summary>
/// Slow query information.
/// </summary>
public class SlowQueryDto
{
    public string QueryPreview { get; set; } = string.Empty;
    public double AvgDurationMs { get; set; }
    public int ExecutionCount { get; set; }
    public DateTime LastSeen { get; set; }
    public string? Context { get; set; }
}
