using EventForge.Server.Mappers;
using DtoEnum = Prym.DTOs.Common.BusinessPartyType;
using EntityEnum = EventForge.Server.Data.Entities.Business.BusinessPartyType;

namespace EventForge.Tests.Mappers;

/// <summary>
/// Verifies that BusinessPartyTypeMapper correctly translates between the entity enum
/// (Cliente=0, Fornitore=1, ClienteFornitore=2) and the DTO enum
/// (Cliente=0, Customer=1, Supplier=2, Both=3).
///
/// A naive integer cast is wrong because the numeric values do not align:
///   Entity Fornitore(1)        → DTO Customer(1)   instead of Supplier(2)
///   Entity ClienteFornitore(2) → DTO Supplier(2)   instead of Both(3)
/// </summary>
[Trait("Category", "Unit")]
public class BusinessPartyTypeMapperTests
{
    #region Entity → DTO

    [Fact]
    public void ToDto_Cliente_ReturnsDtoCliente()
        => Assert.Equal(DtoEnum.Cliente, BusinessPartyTypeMapper.ToDto(EntityEnum.Cliente));

    [Fact]
    public void ToDto_Fornitore_ReturnsDtoSupplier()
        => Assert.Equal(DtoEnum.Supplier, BusinessPartyTypeMapper.ToDto(EntityEnum.Fornitore));

    [Fact]
    public void ToDto_ClienteFornitore_ReturnsDtoBoth()
        => Assert.Equal(DtoEnum.Both, BusinessPartyTypeMapper.ToDto(EntityEnum.ClienteFornitore));

    [Fact]
    public void ToDto_UnknownLegacyValue_ReturnsDtoBothInsteadOfThrowing()
        => Assert.Equal(DtoEnum.Both, BusinessPartyTypeMapper.ToDto((EntityEnum)3));

    #endregion

    #region DTO → Entity

    [Fact]
    public void ToEntity_DtoCliente_ReturnsEntityCliente()
        => Assert.Equal(EntityEnum.Cliente, BusinessPartyTypeMapper.ToEntity(DtoEnum.Cliente));

    [Fact]
    public void ToEntity_DtoCustomer_ReturnsEntityCliente()
        => Assert.Equal(EntityEnum.Cliente, BusinessPartyTypeMapper.ToEntity(DtoEnum.Customer));

    [Fact]
    public void ToEntity_DtoSupplier_ReturnsEntityFornitore()
        => Assert.Equal(EntityEnum.Fornitore, BusinessPartyTypeMapper.ToEntity(DtoEnum.Supplier));

    [Fact]
    public void ToEntity_DtoBoth_ReturnsEntityClienteFornitore()
        => Assert.Equal(EntityEnum.ClienteFornitore, BusinessPartyTypeMapper.ToEntity(DtoEnum.Both));

    #endregion

    #region Round-trip

    [Theory]
    [InlineData(EntityEnum.Cliente)]
    [InlineData(EntityEnum.Fornitore)]
    [InlineData(EntityEnum.ClienteFornitore)]
    public void RoundTrip_EntityToDtoToEntity_Preserves(EntityEnum entity)
        => Assert.Equal(entity, BusinessPartyTypeMapper.ToEntity(BusinessPartyTypeMapper.ToDto(entity)));

    #endregion
}
