namespace EventForge.DTOs.Dashboard;

/// <summary>
/// Server status information.
/// </summary>
public class ServerStatus
{
    public string Status { get; set; } = "Running"; // Running, Maintenance
    public TimeSpan Uptime { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string RuntimeVersion { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public int CpuCores { get; set; }
    public long TotalMemoryMB { get; set; }
    public long UsedMemoryMB { get; set; }
    public double CpuUsagePercent { get; set; }
    public bool DatabaseConnected { get; set; }
    public string CacheType { get; set; } = "Memory";
    public int ActiveUsers { get; set; }
    public double RequestsPerMinute { get; set; }
}
