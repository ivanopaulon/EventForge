using System.Data;
using System.Data.SqlClient;
using EventForge.DTOs.Setup;

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

        try
        {
            // Common local SQL Server instances
            var commonInstances = new[]
            {
                "(localdb)\\MSSQLLocalDB",
                ".\\SQLEXPRESS",
                "localhost\\SQLEXPRESS",
                ".",
                "localhost"
            };

            foreach (var instanceName in commonInstances)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var connectionString = BuildConnectionString(instanceName, new SqlCredentials { AuthenticationType = "Windows" });
                    using var connection = new SqlConnection(connectionString);
                    
                    await connection.OpenAsync(cancellationToken);
                    
                    var version = connection.ServerVersion;
                    await connection.CloseAsync();

                    instances.Add(new SqlServerInstance
                    {
                        ServerName = instanceName,
                        FullAddress = instanceName,
                        IsAvailable = true,
                        Version = version
                    });

                    _logger.LogDebug("Discovered SQL Server instance: {Instance} (Version: {Version})", instanceName, version);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "SQL Server instance not available: {Instance}", instanceName);
                    instances.Add(new SqlServerInstance
                    {
                        ServerName = instanceName,
                        FullAddress = instanceName,
                        IsAvailable = false
                    });
                }
            }

            _logger.LogInformation("Discovered {Count} SQL Server instances ({Available} available)", 
                instances.Count, instances.Count(i => i.IsAvailable));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering SQL Server instances");
        }

        return instances;
    }

    public async Task<bool> TestConnectionAsync(string serverAddress, SqlCredentials credentials, CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = BuildConnectionString(serverAddress, credentials);
            using var connection = new SqlConnection(connectionString);
            
            await connection.OpenAsync(cancellationToken);
            await connection.CloseAsync();

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
            using var connection = new SqlConnection(connectionString);
            
            await connection.OpenAsync(cancellationToken);

            // System databases (master=1, tempdb=2, model=3, msdb=4) are excluded
            using var command = new SqlCommand("SELECT name FROM sys.databases WHERE database_id > 4 ORDER BY name", connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                databases.Add(reader.GetString(0));
            }

            await connection.CloseAsync();

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
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = serverAddress,
            ConnectTimeout = 5,
            Encrypt = false,
            TrustServerCertificate = true
        };

        if (credentials.AuthenticationType == "Windows")
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.IntegratedSecurity = false;
            builder.UserID = credentials.Username;
            builder.Password = credentials.Password;
        }

        return builder.ConnectionString;
    }
}
