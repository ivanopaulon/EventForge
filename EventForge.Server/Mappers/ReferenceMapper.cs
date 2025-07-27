using EventForge.Server.Data.Entities.Common;
using EventForge.Server.DTOs.Common;

namespace EventForge.Server.Mappers;

/// <summary>
/// Static mapper for Reference entity to DTOs.
/// </summary>
public static class ReferenceMapper
{
    /// <summary>
    /// Maps Reference entity to ReferenceDto.
    /// </summary>
    public static ReferenceDto ToDto(Reference reference)
    {
        return new ReferenceDto
        {
            Id = reference.Id,
            OwnerId = reference.OwnerId,
            OwnerType = reference.OwnerType,
            FirstName = reference.FirstName,
            LastName = reference.LastName,
            Department = reference.Department,
            Notes = reference.Notes,
            CreatedAt = reference.CreatedAt,
            CreatedBy = reference.CreatedBy,
            ModifiedAt = reference.ModifiedAt,
            ModifiedBy = reference.ModifiedBy
        };
    }

    /// <summary>
    /// Maps collection of Reference entities to ReferenceDto collection.
    /// </summary>
    public static IEnumerable<ReferenceDto> ToDtoCollection(IEnumerable<Reference> references)
    {
        return references.Select(ToDto);
    }

    /// <summary>
    /// Maps collection of Reference entities to ReferenceDto list.
    /// </summary>
    public static List<ReferenceDto> ToDtoList(IEnumerable<Reference> references)
    {
        return references.Select(ToDto).ToList();
    }

    /// <summary>
    /// Maps CreateReferenceDto to Reference entity.
    /// </summary>
    public static Reference ToEntity(CreateReferenceDto dto)
    {
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
    /// Updates Reference entity from UpdateReferenceDto.
    /// </summary>
    public static void UpdateEntity(Reference entity, UpdateReferenceDto dto)
    {
        entity.FirstName = dto.FirstName;
        entity.LastName = dto.LastName;
        entity.Department = dto.Department;
        entity.Notes = dto.Notes;
    }
}