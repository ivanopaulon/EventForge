using DtoEnum = Prym.DTOs.Common.BusinessPartyType;
using EntityEnum = EventForge.Server.Data.Entities.Business.BusinessPartyType;

namespace EventForge.Server.Mappers;

/// <summary>
/// Provides explicit conversions between the entity <see cref="EntityEnum"/> and the DTO
/// <see cref="DtoEnum"/> enumerations.
///
/// A direct integer cast is WRONG because the two enums have different numeric layouts:
///   Entity : Cliente=0  Fornitore=1        ClienteFornitore=2
///   DTO    : Cliente=0  Customer=1  Supplier=2  Both=3
///
/// Correct mappings:
///   Cliente(0)         → Cliente(0)
///   Fornitore(1)       → Supplier(2)
///   ClienteFornitore(2)→ Both(3)
/// </summary>
public static class BusinessPartyTypeMapper
{
    /// <summary>Converts an entity <see cref="EntityEnum"/> to the corresponding DTO value.</summary>
    public static DtoEnum ToDto(EntityEnum entity) => entity switch
    {
        EntityEnum.Cliente          => DtoEnum.Cliente,
        EntityEnum.Fornitore        => DtoEnum.Supplier,
        EntityEnum.ClienteFornitore => DtoEnum.Both,
        _                           => DtoEnum.Cliente
    };

    /// <summary>Converts a DTO <see cref="DtoEnum"/> to the corresponding entity value.</summary>
    public static EntityEnum ToEntity(DtoEnum dto) => dto switch
    {
        DtoEnum.Cliente   => EntityEnum.Cliente,
        DtoEnum.Customer  => EntityEnum.Cliente,
        DtoEnum.Supplier  => EntityEnum.Fornitore,
        DtoEnum.Both      => EntityEnum.ClienteFornitore,
        _                 => EntityEnum.Cliente
    };
}
