namespace EventForge.DTOs.Monitoring;

/// <summary>
/// Represents a recent system log entry for the health section of the dashboard.
/// </summary>
public class SystemHealthEntryDto
{
    /// <summary>
    /// Log entry identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Timestamp of the log entry (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Severity level (e.g., Error, Warning, Critical).
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Operation type or category.
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Short description of the operation or error.
    /// </summary>
    public string? Operation { get; set; }

    /// <summary>
    /// Detailed message or stack trace excerpt.
    /// </summary>
    public string? Details { get; set; }
}
