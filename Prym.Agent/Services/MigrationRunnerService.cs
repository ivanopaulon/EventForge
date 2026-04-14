using Microsoft.Data.SqlClient;

namespace Prym.Agent.Services;

/// <summary>Executes SQL migration scripts from an extracted package directory.</summary>
public class MigrationRunnerService(AgentOptions options, ILogger<MigrationRunnerService> logger)
{
    /// <param name="extractedPath">Root directory of the extracted package ZIP.</param>
    /// <param name="scriptRelativePaths">Script paths relative to <paramref name="extractedPath"/>.</param>
    /// <param name="onBeforeScript">
    /// Optional callback invoked <em>before</em> each script with its relative path.
    /// Used to push live progress to connected clients.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    public async Task RunScriptsAsync(
        string extractedPath,
        IEnumerable<string> scriptRelativePaths,
        Func<string, Task>? onBeforeScript,
        CancellationToken ct)
    {
        var connectionString = options.Components.Server.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("No SQL connection string configured. Skipping migrations.");
            return;
        }

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        foreach (var relativePath in scriptRelativePaths)
        {
            ct.ThrowIfCancellationRequested();

            if (onBeforeScript is not null)
                await onBeforeScript(relativePath);

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
                await using var cmd = conn.CreateCommand();
                // Set timeout once — it is invariant across all GO batches in the same script.
                cmd.CommandTimeout = options.Install.SqlCommandTimeoutSeconds;
                // Support GO batch separators by splitting
                foreach (var batch in sql.Split(["\nGO\n", "\r\nGO\r\n"], StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = batch.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed)) continue;
                    cmd.CommandText = trimmed;
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
