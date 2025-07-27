using EventForge.Server.Data.Entities.Configuration;
using EventForge.DTOs.SuperAdmin;

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
            Name = entity.Description ?? $"Backup_{entity.Id.ToString()[..8]}", // Generate name from description or ID
            Status = entity.Status,
            ProgressPercentage = entity.ProgressPercentage,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            ErrorMessage = entity.ErrorMessage,
            FileSizeBytes = entity.FileSizeBytes
        };
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