using EventForge.Server.Data.Entities.Common;
using EventForge.Server.DTOs.VatRates;

namespace EventForge.Server.Mappers;

/// <summary>
/// Manual mapper for VatRate entities and DTOs
/// </summary>
public static class VatRateMapper
{
    /// <summary>
    /// Maps VatRate entity to VatRateDto
    /// </summary>
    public static VatRateDto ToDto(VatRate entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return new VatRateDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Percentage = entity.Percentage,
            Status = entity.Status,
            ValidFrom = entity.ValidFrom,
            ValidTo = entity.ValidTo,
            Notes = entity.Notes,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            ModifiedAt = entity.ModifiedAt,
            ModifiedBy = entity.ModifiedBy
        };
    }

    /// <summary>
    /// Maps VatRateDto to VatRate entity
    /// </summary>
    public static VatRate ToEntity(VatRateDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new VatRate
        {
            Id = dto.Id,
            Name = dto.Name,
            Percentage = dto.Percentage,
            Status = dto.Status,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            Notes = dto.Notes,
            CreatedAt = dto.CreatedAt,
            CreatedBy = dto.CreatedBy,
            ModifiedAt = dto.ModifiedAt,
            ModifiedBy = dto.ModifiedBy
        };
    }

    /// <summary>
    /// Maps CreateVatRateDto to VatRate entity
    /// </summary>
    public static VatRate ToEntity(CreateVatRateDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new VatRate
        {
            Name = dto.Name,
            Percentage = dto.Percentage,
            Status = dto.Status,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            Notes = dto.Notes
        };
    }

    /// <summary>
    /// Updates VatRate entity with UpdateVatRateDto data
    /// </summary>
    public static void UpdateEntity(VatRate entity, UpdateVatRateDto dto)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        entity.Name = dto.Name;
        entity.Percentage = dto.Percentage;
        entity.Status = dto.Status;
        entity.ValidFrom = dto.ValidFrom;
        entity.ValidTo = dto.ValidTo;
        entity.Notes = dto.Notes;
    }

    /// <summary>
    /// Maps a collection of VatRate entities to VatRateDto collection
    /// </summary>
    public static IEnumerable<VatRateDto> ToDtoCollection(IEnumerable<VatRate> entities)
    {
        if (entities == null)
            return Enumerable.Empty<VatRateDto>();

        return entities.Select(ToDto);
    }
}