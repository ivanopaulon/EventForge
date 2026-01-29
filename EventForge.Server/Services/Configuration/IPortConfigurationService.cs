namespace EventForge.Server.Services.Configuration;

/// <summary>
/// Service for managing port configuration.
/// </summary>
public interface IPortConfigurationService
{
    /// <summary>
    /// Detects if running on Kestrel or IIS.
    /// </summary>
    /// <returns>Environment type (Kestrel or IIS)</returns>
    string DetectEnvironment();

    /// <summary>
    /// Reads current port configuration.
    /// </summary>
    /// <returns>Dictionary of protocol to port mappings</returns>
    Dictionary<string, int?> ReadPortConfiguration();

    /// <summary>
    /// Writes port configuration to appsettings.
    /// </summary>
    /// <param name="httpPort">HTTP port</param>
    /// <param name="httpsPort">HTTPS port</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task WritePortConfigurationAsync(int? httpPort, int? httpsPort, CancellationToken cancellationToken = default);
}
