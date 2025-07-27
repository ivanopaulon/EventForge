using EventForge.DTOs.Audit;
using EventForge.Server.Data.Entities.Audit;

namespace EventForge.Server.Extensions;

/// <summary>
/// Extension methods for mapping between EntityChangeLog entity and DTOs.
/// </summary>
public static class AuditMappingExtensions
{
    /// <summary>
    /// Converts EntityChangeLog entity to DTO.
    /// </summary>
    public static EntityChangeLogDto ToDto(this EntityChangeLog entity)
    {
        return new EntityChangeLogDto
        {
            Id = entity.Id,
            EntityName = entity.EntityName,
            EntityDisplayName = entity.EntityDisplayName,
            EntityId = entity.EntityId,
            PropertyName = entity.PropertyName,
            OperationType = entity.OperationType,
            OldValue = entity.OldValue,
            NewValue = entity.NewValue,
            ChangedBy = entity.ChangedBy,
            ChangedAt = entity.ChangedAt
        };
    }

    /// <summary>
    /// Converts a collection of EntityChangeLog entities to DTOs.
    /// </summary>
    public static IEnumerable<EntityChangeLogDto> ToDto(this IEnumerable<EntityChangeLog> entities)
    {
        return entities.Select(entity => entity.ToDto());
    }

    /// <summary>
    /// Converts EntityChangeLogDto to entity (for create operations).
    /// </summary>
    public static EntityChangeLog ToEntity(this EntityChangeLogDto dto)
    {
        return new EntityChangeLog
        {
            Id = dto.Id,
            EntityName = dto.EntityName,
            EntityDisplayName = dto.EntityDisplayName,
            EntityId = dto.EntityId,
            PropertyName = dto.PropertyName,
            OperationType = dto.OperationType,
            OldValue = dto.OldValue,
            NewValue = dto.NewValue,
            ChangedBy = dto.ChangedBy,
            ChangedAt = dto.ChangedAt
        };
    }
}