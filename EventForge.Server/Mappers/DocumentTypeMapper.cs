using EventForge.Server.Data.Entities.Documents;
using EventForge.Server.DTOs.Documents;

namespace EventForge.Server.Mappers;

/// <summary>
/// Manual mapper for DocumentType entities and DTOs
/// </summary>
public static class DocumentTypeMapper
{
    /// <summary>
    /// Maps DocumentType entity to DocumentTypeDto
    /// </summary>
    public static DocumentTypeDto ToDto(DocumentType entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        return new DocumentTypeDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Code = entity.Code,
            IsStockIncrease = entity.IsStockIncrease,
            DefaultWarehouseId = entity.DefaultWarehouseId,
            DefaultWarehouseName = entity.DefaultWarehouse?.Name, // Handle navigation property
            IsFiscal = entity.IsFiscal,
            Notes = entity.Notes,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            ModifiedAt = entity.ModifiedAt,
            ModifiedBy = entity.ModifiedBy
        };
    }

    /// <summary>
    /// Maps DocumentTypeDto to DocumentType entity
    /// </summary>
    public static DocumentType ToEntity(DocumentTypeDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new DocumentType
        {
            Id = dto.Id,
            Name = dto.Name,
            Code = dto.Code,
            IsStockIncrease = dto.IsStockIncrease,
            DefaultWarehouseId = dto.DefaultWarehouseId,
            IsFiscal = dto.IsFiscal,
            Notes = dto.Notes,
            CreatedAt = dto.CreatedAt,
            CreatedBy = dto.CreatedBy,
            ModifiedAt = dto.ModifiedAt,
            ModifiedBy = dto.ModifiedBy
            // DefaultWarehouse navigation property not mapped in reverse
        };
    }

    /// <summary>
    /// Maps CreateDocumentTypeDto to DocumentType entity
    /// </summary>
    public static DocumentType ToEntity(CreateDocumentTypeDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new DocumentType
        {
            Name = dto.Name,
            Code = dto.Code,
            IsStockIncrease = dto.IsStockIncrease,
            DefaultWarehouseId = dto.DefaultWarehouseId,
            IsFiscal = dto.IsFiscal,
            Notes = dto.Notes
        };
    }

    /// <summary>
    /// Updates DocumentType entity with UpdateDocumentTypeDto data
    /// </summary>
    public static void UpdateEntity(DocumentType entity, UpdateDocumentTypeDto dto)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        entity.Name = dto.Name;
        entity.Code = dto.Code;
        entity.IsStockIncrease = dto.IsStockIncrease;
        entity.DefaultWarehouseId = dto.DefaultWarehouseId;
        entity.IsFiscal = dto.IsFiscal;
        entity.Notes = dto.Notes;
    }

    /// <summary>
    /// Maps a collection of DocumentType entities to DocumentTypeDto collection
    /// </summary>
    public static IEnumerable<DocumentTypeDto> ToDtoCollection(IEnumerable<DocumentType> entities)
    {
        if (entities == null)
            return Enumerable.Empty<DocumentTypeDto>();

        return entities.Select(ToDto);
    }
}