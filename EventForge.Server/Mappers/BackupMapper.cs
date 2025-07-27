using EventForge.Server.Data.Entities.Configuration;
using EventForge.Server.DTOs.SuperAdmin;

namespace EventForge.Server.Mappers;

/// <summary>
/// Manual mapper for BackupOperation entities and DTOs
/// </summary>
public static class BackupMapper
{
    /// <summary>
    /// Maps BackupOperation entity to BackupStatusDto
    /// </summary>
    public static BackupStatusDto ToStatusDto(BackupOperation entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return new BackupStatusDto
        {
            Id = entity.Id,
            Status = entity.Status,
            ProgressPercentage = entity.ProgressPercentage,
            CurrentOperation = entity.CurrentOperation,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            FilePath = entity.FilePath,
            ErrorMessage = entity.ErrorMessage,
            FileSizeBytes = entity.FileSizeBytes,
            StartedBy = string.Empty // Will be filled manually in service
        };
    }

    /// <summary>
    /// Maps BackupOperation entity to BackupStatusDto with StartedBy
    /// </summary>
    public static BackupStatusDto ToStatusDto(BackupOperation entity, string startedBy)
    {
        var dto = ToStatusDto(entity);
        dto.StartedBy = startedBy ?? string.Empty;
        return dto;
    }

    /// <summary>
    /// Maps a collection of BackupOperation entities to BackupStatusDto collection
    /// </summary>
    public static IEnumerable<BackupStatusDto> ToStatusDtoCollection(IEnumerable<BackupOperation> entities)
    {
        if (entities == null)
            return Enumerable.Empty<BackupStatusDto>();

        return entities.Select(ToStatusDto);
    }
}