using EventForge.Server.Data.Entities.Warehouse;
using Prym.DTOs.Documents;

namespace EventForge.Server.Mappers;

/// <summary>
/// Static mapper for DocumentType entity to Prym.DTOs.
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
            RequiredPartyType = BusinessPartyTypeMapper.ToDto(documentType.RequiredPartyType),
            Notes = documentType.Notes,
            IsInventoryDocument = documentType.IsInventoryDocument,
            CreatesStockMovements = documentType.CreatesStockMovements,
            MovesStockOnRowChange = documentType.MovesStockOnRowChange,
            IsTransferDocument = documentType.IsTransferDocument,
            DefaultMovementReason = documentType.DefaultMovementReason.HasValue
                ? documentType.DefaultMovementReason.Value.ToString()
                : null,
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
            RequiredPartyType = BusinessPartyTypeMapper.ToEntity(dto.RequiredPartyType),
            IsInventoryDocument = dto.IsInventoryDocument,
            CreatesStockMovements = (dto.IsInventoryDocument || dto.MovesStockOnRowChange) ? false : dto.CreatesStockMovements,
            MovesStockOnRowChange = dto.IsInventoryDocument ? false : dto.MovesStockOnRowChange,
            IsTransferDocument = !dto.IsInventoryDocument && dto.IsTransferDocument,
            DefaultMovementReason = ParseMovementReason(dto.DefaultMovementReason)
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
        entity.RequiredPartyType = BusinessPartyTypeMapper.ToEntity(dto.RequiredPartyType);
        entity.Notes = dto.Notes;
        entity.IsInventoryDocument = dto.IsInventoryDocument;
        entity.CreatesStockMovements = (dto.IsInventoryDocument || dto.MovesStockOnRowChange) ? false : dto.CreatesStockMovements;
        entity.MovesStockOnRowChange = dto.IsInventoryDocument ? false : dto.MovesStockOnRowChange;
        entity.IsTransferDocument = !dto.IsInventoryDocument && dto.IsTransferDocument;
        entity.DefaultMovementReason = ParseMovementReason(dto.DefaultMovementReason);
    }

    private static StockMovementReason? ParseMovementReason(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return Enum.TryParse<StockMovementReason>(value, out var reason) ? reason : null;
    }
}