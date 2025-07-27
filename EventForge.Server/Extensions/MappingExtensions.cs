using EventForge.Server.DTOs.Documents;

namespace EventForge.Server.Extensions;

/// <summary>
/// Extension methods for manual entity-to-DTO mapping.
/// </summary>
public static class MappingExtensions
{
    /// <summary>
    /// Maps SystemConfiguration entity to ConfigurationDto.
    /// </summary>
    public static ConfigurationDto ToDto(this SystemConfiguration entity)
    {
        return new ConfigurationDto
        {
            Id = entity.Id,
            Key = entity.Key,
            Value = entity.Value,
            Description = entity.Description,
            Category = entity.Category,
            IsEncrypted = entity.IsEncrypted,
            RequiresRestart = entity.RequiresRestart,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt,
            ModifiedBy = entity.ModifiedBy
        };
    }

    /// <summary>
    /// Maps a collection of SystemConfiguration entities to ConfigurationDto.
    /// </summary>
    public static IEnumerable<ConfigurationDto> ToDto(this IEnumerable<SystemConfiguration> entities)
    {
        return entities.Select(entity => entity.ToDto());
    }

    /// <summary>
    /// Maps DocumentHeader entity to DocumentHeaderDto.
    /// </summary>
    public static DocumentHeaderDto ToDto(this DocumentHeader entity)
    {
        return new DocumentHeaderDto
        {
            Id = entity.Id,
            DocumentTypeId = entity.DocumentTypeId,
            DocumentTypeName = entity.DocumentType?.Name,
            Series = entity.Series,
            Number = entity.Number,
            Date = entity.Date,
            BusinessPartyId = entity.BusinessPartyId,
            CustomerName = entity.CustomerName,
            Notes = entity.Notes,
            Status = entity.Status,
            PaymentStatus = entity.PaymentStatus,
            ApprovalStatus = entity.ApprovalStatus,
            TotalNetAmount = entity.TotalNetAmount,
            VatAmount = entity.VatAmount,
            TotalGrossAmount = entity.TotalGrossAmount,
            TotalDiscount = entity.TotalDiscount,
            TotalDiscountAmount = entity.TotalDiscountAmount,
            TeamId = entity.TeamId,
            EventId = entity.EventId,
            SourceWarehouseId = entity.SourceWarehouseId,
            DestinationWarehouseId = entity.DestinationWarehouseId,
            IsFiscal = entity.IsFiscal,
            IsProforma = entity.IsProforma,
            ApprovedBy = entity.ApprovedBy,
            ApprovedAt = entity.ApprovedAt,
            ClosedAt = entity.ClosedAt,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            ModifiedAt = entity.ModifiedAt,
            ModifiedBy = entity.ModifiedBy
        };
    }

    /// <summary>
    /// Maps CreateDocumentHeaderDto to DocumentHeader entity.
    /// </summary>
    public static DocumentHeader ToEntity(this CreateDocumentHeaderDto dto)
    {
        return new DocumentHeader
        {
            Id = Guid.NewGuid(),
            DocumentTypeId = dto.DocumentTypeId,
            Series = dto.Series,
            Number = dto.Number,
            Date = dto.Date,
            BusinessPartyId = dto.BusinessPartyId,
            CustomerName = dto.CustomerName,
            Notes = dto.Notes,
            TeamId = dto.TeamId,
            EventId = dto.EventId,
            SourceWarehouseId = dto.SourceWarehouseId,
            DestinationWarehouseId = dto.DestinationWarehouseId,
            IsFiscal = dto.IsFiscal,
            IsProforma = dto.IsProforma
        };
    }

    /// <summary>
    /// Updates DocumentHeader entity from UpdateDocumentHeaderDto.
    /// </summary>
    public static void UpdateFromDto(this DocumentHeader entity, UpdateDocumentHeaderDto dto)
    {
        entity.DocumentTypeId = dto.DocumentTypeId;
        entity.Series = dto.Series;
        entity.Number = dto.Number;
        entity.Date = dto.Date;
        entity.BusinessPartyId = dto.BusinessPartyId;
        entity.CustomerName = dto.CustomerName;
        entity.Notes = dto.Notes;
        entity.TeamId = dto.TeamId;
        entity.EventId = dto.EventId;
        entity.SourceWarehouseId = dto.SourceWarehouseId;
        entity.DestinationWarehouseId = dto.DestinationWarehouseId;
        entity.IsFiscal = dto.IsFiscal;
        entity.IsProforma = dto.IsProforma;
    }

    /// <summary>
    /// Maps DocumentRow entity to DocumentRowDto.
    /// </summary>
    public static DocumentRowDto ToDto(this DocumentRow entity)
    {
        return new DocumentRowDto
        {
            Id = entity.Id,
            DocumentHeaderId = entity.DocumentHeaderId,
            RowType = entity.RowType,
            ParentRowId = entity.ParentRowId,
            ProductCode = entity.ProductCode,
            Description = entity.Description,
            UnitOfMeasure = entity.UnitOfMeasure,
            UnitPrice = entity.UnitPrice,
            Quantity = entity.Quantity,
            LineDiscount = entity.LineDiscount,
            VatRate = entity.VatRate,
            Notes = entity.Notes,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            ModifiedAt = entity.ModifiedAt,
            ModifiedBy = entity.ModifiedBy
        };
    }

    /// <summary>
    /// Maps CreateDocumentRowDto to DocumentRow entity.
    /// </summary>
    public static DocumentRow ToEntity(this CreateDocumentRowDto dto)
    {
        return new DocumentRow
        {
            Id = Guid.NewGuid(),
            DocumentHeaderId = dto.DocumentHeaderId,
            RowType = dto.RowType,
            ParentRowId = dto.ParentRowId,
            ProductCode = dto.ProductCode,
            Description = dto.Description,
            UnitOfMeasure = dto.UnitOfMeasure,
            UnitPrice = dto.UnitPrice,
            Quantity = dto.Quantity,
            LineDiscount = dto.LineDiscount,
            VatRate = dto.VatRate,
            Notes = dto.Notes
        };
    }
}