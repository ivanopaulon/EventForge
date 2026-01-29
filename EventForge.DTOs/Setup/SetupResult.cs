namespace EventForge.DTOs.Setup;

/// <summary>
/// Result of setup wizard execution.
/// </summary>
public class SetupResult
{
    /// <summary>
    /// Whether the setup was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Status message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// List of errors if any.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of warnings if any.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Redirect URL after successful setup.
    /// </summary>
    public string? RedirectUrl { get; set; }
}
