namespace EventForge.DTOs.ServerInfo;

/// <summary>
/// Server version information DTO.
/// </summary>
public class ServerVersionDto
{
    /// <summary>
    /// Assembly version (e.g., "1.0.0").
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Informational version with additional metadata (e.g., "1.0.0-beta+abc123").
    /// </summary>
    public string InformationalVersion { get; set; } = string.Empty;

    /// <summary>
    /// Environment name (Development, Staging, Production).
    /// </summary>
    public string Environment { get; set; } = string.Empty;
}
