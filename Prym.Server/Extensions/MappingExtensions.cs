using Prym.DTOs.Documents;

namespace Prym.Server.Extensions;

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
            IsDocumentTypeStockIncrease = entity.DocumentType?.IsStockIncrease ?? false,
            PriceApplicationModeOverride = entity.PriceApplicationModeOverride.HasValue
                ? (Prym.DTOs.Common.PriceApplicationMode)entity.PriceApplicationModeOverride.Value
                : null,
            PriceListId = entity.PriceListId,
            PriceListName = entity.PriceList?.Name,
            Series = entity.Series,
            Number = entity.Number,
            Date = entity.Date,
            BusinessPartyId = entity.BusinessPartyId,
            BusinessPartyName = entity.BusinessParty?.Name,
            CustomerName = entity.CustomerName,
            Notes = entity.Notes,
            Status = (Prym.DTOs.Common.DocumentStatus)entity.Status,
            PaymentStatus = (Prym.DTOs.Common.PaymentStatus)entity.PaymentStatus,
            ApprovalStatus = MapApprovalStatus(entity.ApprovalStatus),
            TotalNetAmount = entity.TotalNetAmount,
            VatAmount = entity.VatAmount,
            TotalGrossAmount = entity.TotalGrossAmount,
            TotalDiscount = entity.TotalDiscount,
            TotalDiscountAmount = entity.TotalDiscountAmount,
            TeamId = entity.TeamId,
            EventId = entity.EventId,
            SourceWarehouseId = entity.SourceWarehouseId,
            SourceWarehouseName = entity.SourceWarehouse?.Name,
            DestinationWarehouseId = entity.DestinationWarehouseId,
            DestinationWarehouseName = entity.DestinationWarehouse?.Name,
            IsFiscal = entity.IsFiscal,
            IsProforma = entity.IsProforma,
            ApprovedBy = entity.ApprovedBy,
            ApprovedAt = entity.ApprovedAt,
            ClosedAt = entity.ClosedAt,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            ModifiedAt = entity.ModifiedAt,
            ModifiedBy = entity.ModifiedBy,
            Rows = entity.Rows?.Where(r => !r.IsDeleted).Select(r => r.ToDto()).ToList() ?? new List<DocumentRowDto>()
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
            Number = dto.Number ?? string.Empty,
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
            RowType = (Prym.DTOs.Common.DocumentRowType)entity.RowType,
            ParentRowId = entity.ParentRowId,
            ProductCode = entity.ProductCode,
            ProductId = entity.ProductId,
            LocationId = entity.LocationId,
            Description = entity.Description,
            UnitOfMeasure = entity.UnitOfMeasure,
            UnitOfMeasureId = entity.UnitOfMeasureId,
            UnitPrice = entity.UnitPrice,
            Quantity = entity.Quantity,
            LineDiscount = entity.LineDiscount,
            LineDiscountValue = entity.LineDiscountValue,
            DiscountType = (Prym.DTOs.Common.DiscountType)entity.DiscountType,
            VatRate = entity.VatRate,
            VatDescription = entity.VatDescription,
            IsGift = entity.IsGift,
            IsManual = entity.IsManual,
            SourceWarehouseId = entity.SourceWarehouseId,
            SourceWarehouseName = entity.SourceWarehouse?.Name,
            DestinationWarehouseId = entity.DestinationWarehouseId,
            DestinationWarehouseName = entity.DestinationWarehouse?.Name,
            Notes = entity.Notes,
            SortOrder = entity.SortOrder,
            StationId = entity.StationId,
            StationName = entity.Station?.Name,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            ModifiedAt = entity.ModifiedAt,
            ModifiedBy = entity.ModifiedBy,
            LineTotal = entity.LineTotal,
            VatTotal = entity.VatTotal,
            DiscountTotal = entity.DiscountTotal,
            BaseQuantity = entity.BaseQuantity,
            BaseUnitPrice = entity.BaseUnitPrice,
            BaseUnitOfMeasureId = entity.BaseUnitOfMeasureId,
            IsPriceManual = entity.IsPriceManual,
            AppliedPriceListId = entity.AppliedPriceListId,
            OriginalPriceFromPriceList = entity.OriginalPriceFromPriceList,
            PriceNotes = entity.PriceNotes,
            AppliedPromotionsJSON = entity.AppliedPromotionsJSON
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
            RowType = (Prym.Server.Data.Entities.Documents.DocumentRowType)dto.RowType,
            ParentRowId = dto.ParentRowId,
            ProductCode = dto.ProductCode,
            ProductId = dto.ProductId,
            LocationId = dto.LocationId,
            Description = dto.Description,
            UnitOfMeasure = dto.UnitOfMeasure,
            UnitOfMeasureId = dto.UnitOfMeasureId,
            UnitPrice = dto.UnitPrice,
            Quantity = dto.Quantity,
            LineDiscount = dto.LineDiscount,
            LineDiscountValue = dto.LineDiscountValue,
            DiscountType = (Prym.DTOs.Common.DiscountType)dto.DiscountType,
            VatRate = dto.VatRate,
            VatDescription = dto.VatDescription,
            IsGift = dto.IsGift,
            IsManual = dto.IsManual,
            SourceWarehouseId = dto.SourceWarehouseId,
            DestinationWarehouseId = dto.DestinationWarehouseId,
            Notes = dto.Notes,
            SortOrder = dto.SortOrder,
            StationId = dto.StationId,
            BaseQuantity = dto.BaseQuantity,
            BaseUnitPrice = dto.BaseUnitPrice,
            BaseUnitOfMeasureId = dto.BaseUnitOfMeasureId,
            AppliedPriceListId = dto.AppliedPriceListId,
            OriginalPriceFromPriceList = dto.OriginalPriceFromPriceList,
            IsPriceManual = dto.IsPriceManual,
            PriceNotes = dto.PriceNotes,
            AppliedPromotionsJSON = dto.AppliedPromotionsJSON
        };
    }

    /// <summary>
    /// Maps entity ApprovalStatus to DTO ApprovalStatus.
    /// Entity has: None(0), Pending(1), Approved(2), Rejected(3)
    /// DTO has: Pending(0), Approved(1), Rejected(2)
    /// </summary>
    private static Prym.DTOs.Common.ApprovalStatus MapApprovalStatus(
        Prym.Server.Data.Entities.Documents.ApprovalStatus entityStatus)
    {
        return entityStatus switch
        {
            Prym.Server.Data.Entities.Documents.ApprovalStatus.None => Prym.DTOs.Common.ApprovalStatus.Pending,
            Prym.Server.Data.Entities.Documents.ApprovalStatus.Pending => Prym.DTOs.Common.ApprovalStatus.Pending,
            Prym.Server.Data.Entities.Documents.ApprovalStatus.Approved => Prym.DTOs.Common.ApprovalStatus.Approved,
            Prym.Server.Data.Entities.Documents.ApprovalStatus.Rejected => Prym.DTOs.Common.ApprovalStatus.Rejected,
            _ => Prym.DTOs.Common.ApprovalStatus.Pending
        };
    }
}