namespace Prym.Agent.Services;

/// <summary>Creates and restores file-system backups before deployment.</summary>
public class BackupService(AgentOptions options, ILogger<BackupService> logger)
{
    private readonly string _backupRoot = !string.IsNullOrWhiteSpace(options.Backup.RootPath)
        ? options.Backup.RootPath
        : Path.Combine(AppContext.BaseDirectory, "backups");

    public async Task<string> CreateBackupAsync(string deployPath, string component, string version, CancellationToken ct)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var backupPath = Path.Combine(_backupRoot, $"{component}-{version}-{timestamp}");
        Directory.CreateDirectory(backupPath);

        logger.LogInformation("Creating backup of {DeployPath} -> {BackupPath}", deployPath, backupPath);

        if (!Directory.Exists(deployPath))
        {
            logger.LogWarning("Deploy path {DeployPath} does not exist, skipping backup", deployPath);
            return backupPath;
        }

        await CopyDirectoryAsync(deployPath, backupPath, ct);
        logger.LogInformation("Backup completed: {BackupPath}", backupPath);

        await PruneOldBackupsAsync(component, ct);

        return backupPath;
    }

    public async Task RestoreBackupAsync(string backupPath, string deployPath, CancellationToken ct)
    {
        if (!Directory.Exists(backupPath))
        {
            logger.LogError("Backup path {BackupPath} does not exist, cannot restore", backupPath);
            return;
        }

        logger.LogWarning("Restoring backup from {BackupPath} -> {DeployPath}", backupPath, deployPath);

        if (Directory.Exists(deployPath))
            Directory.Delete(deployPath, recursive: true);

        Directory.CreateDirectory(deployPath);
        await CopyDirectoryAsync(backupPath, deployPath, ct);
        logger.LogInformation("Restore completed");
    }

    /// <summary>
    /// Removes the oldest backup copies that exceed <see cref="BackupAgentOptions.MaxBackupsToKeep"/>
    /// for the given component. No-op when limit is 0 (unlimited).
    /// </summary>
    private async Task PruneOldBackupsAsync(string component, CancellationToken ct)
    {
        var max = options.Backup.MaxBackupsToKeep;
        if (max <= 0) return;

        try
        {
            var prefix = $"{component}-";
            var existing = Directory.GetDirectories(_backupRoot, $"{prefix}*")
                .OrderBy(d => Directory.GetCreationTimeUtc(d))
                .ToList();

            var toDelete = existing.Count - max;
            for (var i = 0; i < toDelete; i++)
            {
                ct.ThrowIfCancellationRequested();
                var dirToDelete = existing[i];
                logger.LogInformation("Pruning old backup: {Path}", dirToDelete);
                await Task.Run(() => Directory.Delete(dirToDelete, recursive: true), ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to prune old backups for component {Component}", component);
        }
    }

    private static async Task CopyDirectoryAsync(string source, string destination, CancellationToken ct)
    {
        var files = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories);

        await Parallel.ForEachAsync(files,
            new ParallelOptions { MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 8), CancellationToken = ct },
            async (file, token) =>
            {
                var relative = Path.GetRelativePath(source, file);
                var destFile = Path.Combine(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                await using var src = File.OpenRead(file);
                await using var dst = File.Create(destFile);
                await src.CopyToAsync(dst, token);
            });
    }
}
