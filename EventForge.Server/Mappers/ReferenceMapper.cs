using EventForge.Server.Data.Entities.Common;
using EventForge.Server.DTOs.Common;

namespace EventForge.Server.Mappers;

/// <summary>
/// Manual mapper for Reference entities and DTOs
/// </summary>
public static class ReferenceMapper
{
    /// <summary>
    /// Maps Reference entity to ReferenceDto
    /// </summary>
    public static ReferenceDto ToDto(Reference entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return new ReferenceDto
        {
            Id = entity.Id,
            OwnerId = entity.OwnerId,
            OwnerType = entity.OwnerType,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Department = entity.Department,
            Notes = entity.Notes,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            ModifiedAt = entity.ModifiedAt,
            ModifiedBy = entity.ModifiedBy
        };
    }

    /// <summary>
    /// Maps ReferenceDto to Reference entity
    /// </summary>
    public static Reference ToEntity(ReferenceDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new Reference
        {
            Id = dto.Id,
            OwnerId = dto.OwnerId,
            OwnerType = dto.OwnerType,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Department = dto.Department,
            Notes = dto.Notes,
            CreatedAt = dto.CreatedAt,
            CreatedBy = dto.CreatedBy,
            ModifiedAt = dto.ModifiedAt,
            ModifiedBy = dto.ModifiedBy
        };
    }

    /// <summary>
    /// Maps CreateReferenceDto to Reference entity
    /// </summary>
    public static Reference ToEntity(CreateReferenceDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new Reference
        {
            OwnerId = dto.OwnerId,
            OwnerType = dto.OwnerType,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Department = dto.Department,
            Notes = dto.Notes
        };
    }

    /// <summary>
    /// Updates Reference entity with UpdateReferenceDto data
    /// </summary>
    public static void UpdateEntity(Reference entity, UpdateReferenceDto dto)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        entity.FirstName = dto.FirstName;
        entity.LastName = dto.LastName;
        entity.Department = dto.Department;
        entity.Notes = dto.Notes;
    }

    /// <summary>
    /// Maps a collection of Reference entities to ReferenceDto collection
    /// </summary>
    public static IEnumerable<ReferenceDto> ToDtoCollection(IEnumerable<Reference> entities)
    {
        if (entities == null)
            return Enumerable.Empty<ReferenceDto>();

        return entities.Select(ToDto);
    }

    /// <summary>
    /// Maps a List of Reference entities to List of ReferenceDto
    /// </summary>
    public static List<ReferenceDto> ToDtoList(List<Reference> entities)
    {
        if (entities == null)
            return new List<ReferenceDto>();

        return entities.Select(ToDto).ToList();
    }
}