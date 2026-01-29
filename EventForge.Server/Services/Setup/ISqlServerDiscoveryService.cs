using EventForge.DTOs.Setup;

namespace EventForge.Server.Services.Setup;

/// <summary>
/// Service for discovering and testing SQL Server instances.
/// </summary>
public interface ISqlServerDiscoveryService
{
    /// <summary>
    /// Discovers local SQL Server instances.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of discovered SQL Server instances</returns>
    Task<List<SqlServerInstance>> DiscoverLocalInstancesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests connection to a SQL Server with given credentials.
    /// </summary>
    /// <param name="serverAddress">Server address (e.g., localhost\\SQLEXPRESS)</param>
    /// <param name="credentials">SQL credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection successful</returns>
    Task<bool> TestConnectionAsync(string serverAddress, SqlCredentials credentials, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists available databases on a SQL Server instance.
    /// </summary>
    /// <param name="serverAddress">Server address</param>
    /// <param name="credentials">SQL credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of database names</returns>
    Task<List<string>> ListDatabasesAsync(string serverAddress, SqlCredentials credentials, CancellationToken cancellationToken = default);
}
