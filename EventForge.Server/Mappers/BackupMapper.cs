using ServerSuperAdminDtos = EventForge.DTOs.SuperAdmin;
using SharedSuperAdminDtos = EventForge.DTOs.SuperAdmin;

namespace EventForge.Server.Mappers;

/// <summary>
/// Static mapper for BackupOperation entity to DTOs.
/// </summary>
public static class BackupMapper
{
    /// <summary>
    /// Maps BackupOperation entity to BackupStatusDto.
    /// </summary>
    public static SharedSuperAdminDtos.BackupStatusDto ToStatusDto(BackupOperation backup, string startedByUserName)
    {
        return new SharedSuperAdminDtos.BackupStatusDto
        {
            Id = backup.Id,
            Name = backup.Description ?? $"Backup_{backup.StartedAt:yyyyMMdd_HHmmss}",
            Status = backup.Status,
            ProgressPercentage = backup.ProgressPercentage,
            StartedAt = backup.StartedAt,
            CompletedAt = backup.CompletedAt,
            ErrorMessage = backup.ErrorMessage,
            FileSizeBytes = backup.FileSizeBytes,
            StartedByUserName = startedByUserName
        };
    }

    /// <summary>
    /// Maps BackupOperation entity to Server BackupStatusDto.
    /// </summary>
    public static ServerSuperAdminDtos.BackupStatusDto ToServerStatusDto(BackupOperation backup, string startedByUserName)
    {
        return new ServerSuperAdminDtos.BackupStatusDto
        {
            Id = backup.Id,
            Status = backup.Status,
            ProgressPercentage = backup.ProgressPercentage,
            CurrentOperation = backup.CurrentOperation,
            StartedAt = backup.StartedAt,
            CompletedAt = backup.CompletedAt,
            FilePath = backup.FilePath,
            FileSizeBytes = backup.FileSizeBytes,
            ErrorMessage = backup.ErrorMessage,
            StartedByUserId = backup.StartedByUserId,
            StartedByUserName = startedByUserName
        };
    }
}