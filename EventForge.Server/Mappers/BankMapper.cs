using EventForge.Server.Data.Entities.Common;
using EventForge.Server.DTOs.Banks;

namespace EventForge.Server.Mappers;

/// <summary>
/// Manual mapper for Bank entities and DTOs
/// </summary>
public static class BankMapper
{
    /// <summary>
    /// Maps Bank entity to BankDto
    /// </summary>
    public static BankDto ToDto(Bank entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return new BankDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Code = entity.Code,
            SwiftBic = entity.SwiftBic,
            Branch = entity.Branch,
            Address = entity.Address,
            Country = entity.Country,
            Phone = entity.Phone,
            Email = entity.Email,
            Notes = entity.Notes,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            ModifiedAt = entity.ModifiedAt,
            ModifiedBy = entity.ModifiedBy
        };
    }

    /// <summary>
    /// Maps BankDto to Bank entity
    /// </summary>
    public static Bank ToEntity(BankDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new Bank
        {
            Id = dto.Id,
            Name = dto.Name,
            Code = dto.Code,
            SwiftBic = dto.SwiftBic,
            Branch = dto.Branch,
            Address = dto.Address,
            Country = dto.Country,
            Phone = dto.Phone,
            Email = dto.Email,
            Notes = dto.Notes,
            CreatedAt = dto.CreatedAt,
            CreatedBy = dto.CreatedBy,
            ModifiedAt = dto.ModifiedAt,
            ModifiedBy = dto.ModifiedBy
        };
    }

    /// <summary>
    /// Maps CreateBankDto to Bank entity
    /// </summary>
    public static Bank ToEntity(CreateBankDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new Bank
        {
            Name = dto.Name,
            Code = dto.Code,
            SwiftBic = dto.SwiftBic,
            Branch = dto.Branch,
            Address = dto.Address,
            Country = dto.Country,
            Phone = dto.Phone,
            Email = dto.Email,
            Notes = dto.Notes
        };
    }

    /// <summary>
    /// Updates Bank entity with UpdateBankDto data
    /// </summary>
    public static void UpdateEntity(Bank entity, UpdateBankDto dto)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        entity.Name = dto.Name;
        entity.Code = dto.Code;
        entity.SwiftBic = dto.SwiftBic;
        entity.Branch = dto.Branch;
        entity.Address = dto.Address;
        entity.Country = dto.Country;
        entity.Phone = dto.Phone;
        entity.Email = dto.Email;
        entity.Notes = dto.Notes;
    }

    /// <summary>
    /// Maps a collection of Bank entities to BankDto collection
    /// </summary>
    public static IEnumerable<BankDto> ToDtoCollection(IEnumerable<Bank> entities)
    {
        if (entities == null)
            return Enumerable.Empty<BankDto>();

        return entities.Select(ToDto);
    }
}