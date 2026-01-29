namespace EventForge.Server.Services.Configuration;

/// <summary>
/// Service interface for managing backup operations.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Starts a manual backup operation.
    /// </summary>
    /// <param name="request">Backup request parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Backup operation details</returns>
    Task<BackupStatusDto> StartBackupAsync(BackupRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Gets the status of a backup operation.
    /// </summary>
    /// <param name="backupId">Backup operation ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Backup status or null if not found</returns>
    Task<BackupStatusDto?> GetBackupStatusAsync(Guid backupId, CancellationToken ct = default);

    /// <summary>
    /// Gets all backup operations (recent first).
    /// </summary>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of backup operations</returns>
    Task<IEnumerable<BackupStatusDto>> GetBackupsAsync(int limit = 50, CancellationToken ct = default);

    /// <summary>
    /// Cancels a running backup operation.
    /// </summary>
    /// <param name="backupId">Backup operation ID</param>
    /// <param name="ct">Cancellation token</param>
    Task CancelBackupAsync(Guid backupId, CancellationToken ct = default);

    /// <summary>
    /// Downloads a completed backup file.
    /// </summary>
    /// <param name="backupId">Backup operation ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>File stream and filename</returns>
    Task<(Stream FileStream, string FileName)?> DownloadBackupAsync(Guid backupId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a backup file and operation record.
    /// </summary>
    /// <param name="backupId">Backup operation ID</param>
    /// <param name="ct">Cancellation token</param>
    Task DeleteBackupAsync(Guid backupId, CancellationToken ct = default);
}