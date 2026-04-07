namespace EventForge.DTOs.Setup;

/// <summary>
/// Represents a discovered SQL Server instance.
/// </summary>
public class SqlServerInstance
{
    /// <summary>
    /// Server name or instance.
    /// </summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>
    /// Instance name (if applicable).
    /// </summary>
    public string? InstanceName { get; set; }

    /// <summary>
    /// Full server address (ServerName\InstanceName or just ServerName).
    /// </summary>
    public string FullAddress { get; set; } = string.Empty;

    /// <summary>
    /// Whether this instance is available/responsive.
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// SQL Server version information.
    /// </summary>
    public string? Version { get; set; }
}
