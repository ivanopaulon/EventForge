using EventForge.Server.Data.Entities.Configuration;

namespace EventForge.Server.Mappers;

/// <summary>
/// Static mapper for BackupOperation entity to DTOs.
/// </summary>
public static class BackupMapper
{
    /// <summary>
    /// Maps BackupOperation entity to BackupStatusDto.
    /// </summary>
    public static EventForge.DTOs.SuperAdmin.BackupStatusDto ToStatusDto(BackupOperation backup, string startedByUserName)
    {
        return new EventForge.DTOs.SuperAdmin.BackupStatusDto
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
}