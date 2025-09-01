namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service interface for antivirus scanning of uploaded files
/// </summary>
public interface IAntivirusScanService
{
    /// <summary>
    /// Scans a file stream for viruses and malware
    /// </summary>
    /// <param name="fileStream">File content to scan</param>
    /// <param name="fileName">Original filename for context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Scan result with threat detection information</returns>
    Task<AntivirusScanResult> ScanFileAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if antivirus scanning is enabled in configuration
    /// </summary>
    /// <returns>True if antivirus scanning is enabled</returns>
    bool IsEnabled { get; }
}

/// <summary>
/// Result of antivirus scan operation
/// </summary>
public class AntivirusScanResult
{
    /// <summary>
    /// True if file is clean, false if threats detected
    /// </summary>
    public bool IsClean { get; set; }

    /// <summary>
    /// List of detected threats (empty if clean)
    /// </summary>
    public List<string> DetectedThreats { get; set; } = new();

    /// <summary>
    /// Scan engine information
    /// </summary>
    public string? ScanEngineInfo { get; set; }

    /// <summary>
    /// Timestamp when scan was performed
    /// </summary>
    public DateTime ScannedAt { get; set; }

    /// <summary>
    /// Additional scan metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}