using EventForge.DTOs.Documents;

namespace EventForge.Server.Mappers;

/// <summary>
/// Static mapper for DocumentType entity to DTOs.
/// </summary>
public static class DocumentTypeMapper
{
    /// <summary>
    /// Maps DocumentType entity to DocumentTypeDto.
    /// </summary>
    public static DocumentTypeDto ToDto(DocumentType documentType)
    {
        return new DocumentTypeDto
        {
            Id = documentType.Id,
            Name = documentType.Name,
            Code = documentType.Code,
            IsStockIncrease = documentType.IsStockIncrease,
            DefaultWarehouseId = documentType.DefaultWarehouseId,
            DefaultWarehouseName = documentType.DefaultWarehouse?.Name,
            IsFiscal = documentType.IsFiscal,
            RequiredPartyType = (EventForge.DTOs.Common.BusinessPartyType)documentType.RequiredPartyType,
            Notes = documentType.Notes,
            IsInventoryDocument = documentType.IsInventoryDocument,
            CreatedAt = documentType.CreatedAt,
            CreatedBy = documentType.CreatedBy,
            ModifiedAt = documentType.ModifiedAt,
            ModifiedBy = documentType.ModifiedBy
        };
    }

    /// <summary>
    /// Maps collection of DocumentType entities to DocumentTypeDto collection.
    /// </summary>
    public static IEnumerable<DocumentTypeDto> ToDtoCollection(IEnumerable<DocumentType> documentTypes)
    {
        return documentTypes.Select(ToDto);
    }

    /// <summary>
    /// Maps CreateDocumentTypeDto to DocumentType entity.
    /// </summary>
    public static DocumentType ToEntity(CreateDocumentTypeDto dto)
    {
        return new DocumentType
        {
            Name = dto.Name,
            Code = dto.Code,
            IsStockIncrease = dto.IsStockIncrease,
            DefaultWarehouseId = dto.DefaultWarehouseId,
            IsFiscal = dto.IsFiscal,
            RequiredPartyType = (EventForge.Server.Data.Entities.Business.BusinessPartyType)dto.RequiredPartyType,
            Notes = dto.Notes
        };
    }

    /// <summary>
    /// Updates DocumentType entity from UpdateDocumentTypeDto.
    /// </summary>
    public static void UpdateEntity(DocumentType entity, UpdateDocumentTypeDto dto)
    {
        entity.Name = dto.Name;
        entity.Code = dto.Code;
        entity.IsStockIncrease = dto.IsStockIncrease;
        entity.DefaultWarehouseId = dto.DefaultWarehouseId;
        entity.IsFiscal = dto.IsFiscal;
        entity.RequiredPartyType = (EventForge.Server.Data.Entities.Business.BusinessPartyType)dto.RequiredPartyType;
        entity.Notes = dto.Notes;
    }
}