using EventForge.DTOs.Setup;

namespace EventForge.Server.Services.Setup;

/// <summary>
/// Service for orchestrating the setup wizard process.
/// </summary>
public interface ISetupWizardService
{
    /// <summary>
    /// Completes the setup wizard with the provided configuration.
    /// </summary>
    /// <param name="config">Setup configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Setup result</returns>
    Task<SetupResult> CompleteSetupAsync(SetupConfiguration config, CancellationToken cancellationToken = default);
}
