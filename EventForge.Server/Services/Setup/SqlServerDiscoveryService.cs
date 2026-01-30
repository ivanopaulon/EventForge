using EventForge.DTOs.Setup;
using MicrosoftSqlClient = Microsoft.Data.SqlClient;  // For actual connections

namespace EventForge.Server.Services.Setup;

/// <summary>
/// Implementation of SQL Server discovery service.
/// </summary>
public class SqlServerDiscoveryService : ISqlServerDiscoveryService
{
    private readonly ILogger<SqlServerDiscoveryService> _logger;

    public SqlServerDiscoveryService(ILogger<SqlServerDiscoveryService> logger)
    {
        _logger = logger;
    }

    public async Task<List<SqlServerInstance>> DiscoverLocalInstancesAsync(CancellationToken cancellationToken = default)
    {
        var instances = new List<SqlServerInstance>();
        var discovered = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // Prevent duplicates

        try
        {
            // Test common local instances directly
            // Note: SQL Server Browser enumeration is not available with System.Data.SqlClient in .NET Core/5+
            var commonInstances = new[]
            {
                "localhost",
                ".",
                "(local)",
                ".\\SQLEXPRESS",
                "localhost\\SQLEXPRESS",
                "(localdb)\\MSSQLLocalDB",
                "(localdb)\\v11.0",
                "(localdb)\\v12.0",
                "(localdb)\\v13.0",
                "(localdb)\\ProjectsV13",
                Environment.MachineName,
                $"{Environment.MachineName}\\SQLEXPRESS"
            };

            foreach (var instanceName in commonInstances)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (discovered.Contains(instanceName))
                    continue; // Skip duplicates

                try
                {
                    // Test connection with Windows Auth
                    var connectionString = BuildConnectionString(instanceName, new SqlCredentials { AuthenticationType = "Windows" });
                    using var connection = new MicrosoftSqlClient.SqlConnection(connectionString);

                    await connection.OpenAsync(cancellationToken);

                    var version = connection.ServerVersion;

                    if (discovered.Add(instanceName))
                    {
                        instances.Add(new SqlServerInstance
                        {
                            ServerName = instanceName,
                            FullAddress = instanceName,
                            IsAvailable = true,
                            Version = version
                        });

                        _logger.LogInformation("Found SQL Server instance: {Instance} (Version: {Version})", instanceName, version);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not connect to {Instance}", instanceName);
                }
            }

            _logger.LogInformation("Total SQL Server instances discovered: {Count}", instances.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SQL Server discovery");
        }

        return instances;
    }

    public async Task<bool> TestConnectionAsync(string serverAddress, SqlCredentials credentials, CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = BuildConnectionString(serverAddress, credentials);
            using var connection = new MicrosoftSqlClient.SqlConnection(connectionString);

            await connection.OpenAsync(cancellationToken);

            _logger.LogInformation("Successfully connected to SQL Server: {Server}", serverAddress);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SQL Server: {Server}", serverAddress);
            return false;
        }
    }

    public async Task<List<string>> ListDatabasesAsync(string serverAddress, SqlCredentials credentials, CancellationToken cancellationToken = default)
    {
        var databases = new List<string>();

        try
        {
            var connectionString = BuildConnectionString(serverAddress, credentials);
            using var connection = new MicrosoftSqlClient.SqlConnection(connectionString);

            await connection.OpenAsync(cancellationToken);

            // System databases (master=1, tempdb=2, model=3, msdb=4) are excluded
            // Note: This assumes a standard SQL Server installation
            using var command = new MicrosoftSqlClient.SqlCommand("SELECT name FROM sys.databases WHERE database_id > 4 ORDER BY name", connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                databases.Add(reader.GetString(0));
            }

            _logger.LogInformation("Found {Count} user databases on {Server}", databases.Count, serverAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing databases on {Server}", serverAddress);
        }

        return databases;
    }

    private string BuildConnectionString(string serverAddress, SqlCredentials credentials)
    {
        // Build connection string matching appsettings.json format EXACTLY

        if (credentials.AuthenticationType == "Windows")
        {
            return $"Server={serverAddress};Integrated Security=True;TrustServerCertificate=True;";
        }
        else
        {
            return $"Server={serverAddress};User Id={credentials.Username};Password={credentials.Password};TrustServerCertificate=True;";
        }
    }
}
