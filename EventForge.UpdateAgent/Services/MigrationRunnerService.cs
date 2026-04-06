using Microsoft.Data.SqlClient;

namespace EventForge.UpdateAgent.Services;

/// <summary>Executes SQL migration scripts from an extracted package directory.</summary>
public class MigrationRunnerService(AgentOptions options, ILogger<MigrationRunnerService> logger)
{
    public async Task RunScriptsAsync(string extractedPath, IEnumerable<string> scriptRelativePaths, CancellationToken ct)
    {
        var connectionString = options.Components.Server.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("No SQL connection string configured. Skipping migrations.");
            return;
        }

        foreach (var relativePath in scriptRelativePaths)
        {
            ct.ThrowIfCancellationRequested();
            var fullPath = Path.Combine(extractedPath, relativePath);
            if (!File.Exists(fullPath))
            {
                logger.LogWarning("Migration script not found: {Path}", fullPath);
                continue;
            }

            var sql = await File.ReadAllTextAsync(fullPath, ct);
            logger.LogInformation("Executing migration: {Script}", relativePath);

            try
            {
                await using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync(ct);
                await using var cmd = conn.CreateCommand();
                // Support GO batch separators by splitting
                foreach (var batch in sql.Split(["\nGO\n", "\r\nGO\r\n"], StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = batch.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed)) continue;
                    cmd.CommandText = trimmed;
                    cmd.CommandTimeout = options.Install.SqlCommandTimeoutSeconds;
                    await cmd.ExecuteNonQueryAsync(ct);
                }
                logger.LogInformation("Migration completed: {Script}", relativePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Migration failed: {Script}", relativePath);
                throw;
            }
        }
    }
}
