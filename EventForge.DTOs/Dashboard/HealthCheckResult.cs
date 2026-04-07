namespace EventForge.DTOs.Dashboard;

/// <summary>
/// Health check result.
/// </summary>
public class HealthCheckResult
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Healthy"; // Healthy, Degraded, Unhealthy
    public string? Description { get; set; }
    public double DurationMs { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
