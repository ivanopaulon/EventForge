namespace EventForge.Server.Services.Setup;

/// <summary>
/// Service for detecting if the application is running for the first time.
/// </summary>
public interface IFirstRunDetectionService
{
    /// <summary>
    /// Checks if the setup wizard has been completed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if setup is complete, false if first run</returns>
    Task<bool> IsSetupCompleteAsync(CancellationToken cancellationToken = default);
}
