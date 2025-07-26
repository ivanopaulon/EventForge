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
    /// <returns>Backup operation details</returns>
    Task<BackupStatusDto> StartBackupAsync(BackupRequestDto request);

    /// <summary>
    /// Gets the status of a backup operation.
    /// </summary>
    /// <param name="backupId">Backup operation ID</param>
    /// <returns>Backup status or null if not found</returns>
    Task<BackupStatusDto?> GetBackupStatusAsync(Guid backupId);

    /// <summary>
    /// Gets all backup operations (recent first).
    /// </summary>
    /// <param name="limit">Maximum number of results to return</param>
    /// <returns>List of backup operations</returns>
    Task<IEnumerable<BackupStatusDto>> GetBackupsAsync(int limit = 50);

    /// <summary>
    /// Cancels a running backup operation.
    /// </summary>
    /// <param name="backupId">Backup operation ID</param>
    Task CancelBackupAsync(Guid backupId);

    /// <summary>
    /// Downloads a completed backup file.
    /// </summary>
    /// <param name="backupId">Backup operation ID</param>
    /// <returns>File stream and filename</returns>
    Task<(Stream FileStream, string FileName)?> DownloadBackupAsync(Guid backupId);

    /// <summary>
    /// Deletes a backup file and operation record.
    /// </summary>
    /// <param name="backupId">Backup operation ID</param>
    Task DeleteBackupAsync(Guid backupId);
}