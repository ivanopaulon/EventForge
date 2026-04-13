using Prym.DTOs.Documents;
using Microsoft.EntityFrameworkCore;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// Service implementation for managing document rows.
/// </summary>
public class DocumentRowService(
    EventForgeDbContext context,
    IAuditLogService auditLogService,
    ITenantContext tenantContext,
    ILogger<DocumentRowService> logger) : IDocumentRowService
{
    /// <inheritdoc />
    public async Task<IEnumerable<DocumentRowDto>> GetRowsByDocumentHeaderIdAsync(
        Guid documentHeaderId,
        CancellationToken cancellationToken = default)
    {
        var rows = await context.DocumentRows
            .AsNoTracking()
            .Where(r => r.DocumentHeaderId == documentHeaderId && !r.IsDeleted)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(MapToDto);
    }

    /// <inheritdoc />
    public async Task<DocumentRowDto?> GetDocumentRowByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var row = await context.DocumentRows
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

        return row is not null ? MapToDto(row) : null;
    }

    /// <inheritdoc />
    public async Task<DocumentRowDto> CreateDocumentRowAsync(
        CreateDocumentRowDto createDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        // Handle MergeDuplicateProducts: if a row for the same product exists, add quantity instead
        if (createDto.MergeDuplicateProducts && createDto.ProductId.HasValue)
        {
            var existing = await context.DocumentRows
                .FirstOrDefaultAsync(r =>
                    r.DocumentHeaderId == createDto.DocumentHeaderId &&
                    r.ProductId == createDto.ProductId &&
                    !r.IsDeleted, cancellationToken);

            if (existing is not null)
            {
                existing.Quantity += createDto.Quantity;
                existing.ModifiedAt = DateTime.UtcNow;
                existing.ModifiedBy = currentUser;
                _ = await context.SaveChangesAsync(cancellationToken);

                _ = await auditLogService.LogEntityChangeAsync(
                    "DocumentRow", existing.Id, "UPDATE", "MERGE_QUANTITY",
                    null, $"Merged quantity +{createDto.Quantity} into row for product {createDto.ProductId}", currentUser);

                logger.LogInformation("Merged quantity into DocumentRow {RowId} for user {User}", existing.Id, currentUser);
                return MapToDto(existing);
            }
        }

        var row = new DocumentRow
        {
            Id = Guid.NewGuid(),
            DocumentHeaderId = createDto.DocumentHeaderId,
            RowType = (Data.Entities.Documents.DocumentRowType)createDto.RowType,
            ParentRowId = createDto.ParentRowId,
            ProductCode = createDto.ProductCode,
            ProductId = createDto.ProductId,
            LocationId = createDto.LocationId,
            Description = createDto.Description,
            UnitOfMeasure = createDto.UnitOfMeasure,
            UnitOfMeasureId = createDto.UnitOfMeasureId,
            UnitPrice = createDto.UnitPrice,
            Quantity = createDto.Quantity,
            LineDiscount = createDto.LineDiscount,
            LineDiscountValue = createDto.LineDiscountValue,
            DiscountType = createDto.DiscountType,
            VatRate = createDto.VatRate,
            VatDescription = createDto.VatDescription,
            IsGift = createDto.IsGift,
            IsManual = createDto.IsManual,
            SourceWarehouseId = createDto.SourceWarehouseId,
            DestinationWarehouseId = createDto.DestinationWarehouseId,
            Notes = createDto.Notes,
            SortOrder = createDto.SortOrder,
            StationId = createDto.StationId,
            BaseQuantity = createDto.BaseQuantity,
            BaseUnitPrice = createDto.BaseUnitPrice,
            BaseUnitOfMeasureId = createDto.BaseUnitOfMeasureId,
            IsPriceManual = createDto.IsPriceManual,
            AppliedPriceListId = createDto.AppliedPriceListId,
            OriginalPriceFromPriceList = createDto.OriginalPriceFromPriceList,
            PriceNotes = createDto.PriceNotes,
            AppliedPromotionsJSON = createDto.AppliedPromotionsJSON,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser,
            TenantId = tenantContext.CurrentTenantId ?? Guid.Empty
        };

        _ = context.DocumentRows.Add(row);
        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.LogEntityChangeAsync(
            "DocumentRow", row.Id, "CREATE", "CREATE",
            null, $"Created row '{row.Description}' for document {row.DocumentHeaderId}", currentUser);

        logger.LogInformation("Created DocumentRow {RowId} for user {User}", row.Id, currentUser);
        return MapToDto(row);
    }

    /// <inheritdoc />
    public async Task<DocumentRowDto?> UpdateDocumentRowAsync(
        Guid id,
        UpdateDocumentRowDto updateDto,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var row = await context.DocumentRows
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

        if (row is null)
            return null;

        row.RowType = (Data.Entities.Documents.DocumentRowType)updateDto.RowType;
        row.ParentRowId = updateDto.ParentRowId;
        row.ProductCode = updateDto.ProductCode;
        row.Description = updateDto.Description;
        row.UnitOfMeasure = updateDto.UnitOfMeasure;
        row.UnitOfMeasureId = updateDto.UnitOfMeasureId;
        row.UnitPrice = updateDto.UnitPrice;
        row.Quantity = updateDto.Quantity;
        row.LineDiscount = updateDto.LineDiscount;
        row.LineDiscountValue = updateDto.LineDiscountValue;
        row.DiscountType = updateDto.DiscountType;
        row.VatRate = updateDto.VatRate;
        row.VatDescription = updateDto.VatDescription;
        row.IsGift = updateDto.IsGift;
        row.IsManual = updateDto.IsManual;
        row.SourceWarehouseId = updateDto.SourceWarehouseId;
        row.DestinationWarehouseId = updateDto.DestinationWarehouseId;
        row.Notes = updateDto.Notes;
        row.SortOrder = updateDto.SortOrder;
        row.StationId = updateDto.StationId;
        row.BaseQuantity = updateDto.BaseQuantity;
        row.BaseUnitPrice = updateDto.BaseUnitPrice;
        row.BaseUnitOfMeasureId = updateDto.BaseUnitOfMeasureId;
        row.IsPriceManual = updateDto.IsPriceManual;
        row.AppliedPriceListId = updateDto.AppliedPriceListId;
        row.OriginalPriceFromPriceList = updateDto.OriginalPriceFromPriceList;
        row.PriceNotes = updateDto.PriceNotes;
        row.AppliedPromotionsJSON = updateDto.AppliedPromotionsJSON;
        row.ModifiedAt = DateTime.UtcNow;
        row.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.LogEntityChangeAsync(
            "DocumentRow", row.Id, "UPDATE", "UPDATE",
            null, $"Updated row '{row.Description}'", currentUser);

        logger.LogInformation("Updated DocumentRow {RowId} for user {User}", row.Id, currentUser);
        return MapToDto(row);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteDocumentRowAsync(
        Guid id,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var row = await context.DocumentRows
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

        if (row is null)
            return false;

        row.IsDeleted = true;
        row.ModifiedAt = DateTime.UtcNow;
        row.ModifiedBy = currentUser;

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.LogEntityChangeAsync(
            "DocumentRow", row.Id, "DELETE", "DELETE",
            null, $"Deleted row '{row.Description}'", currentUser);

        logger.LogInformation("Deleted DocumentRow {RowId} for user {User}", row.Id, currentUser);
        return true;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DocumentRowDto>> BulkCreateDocumentRowsAsync(
        Guid documentHeaderId,
        IEnumerable<CreateDocumentRowDto> createDtos,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var dtoList = createDtos.ToList();
        var rows = dtoList.Select(dto => new DocumentRow
        {
            Id = Guid.NewGuid(),
            DocumentHeaderId = documentHeaderId,
            RowType = (Data.Entities.Documents.DocumentRowType)dto.RowType,
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
            DiscountType = dto.DiscountType,
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
            IsPriceManual = dto.IsPriceManual,
            AppliedPriceListId = dto.AppliedPriceListId,
            OriginalPriceFromPriceList = dto.OriginalPriceFromPriceList,
            PriceNotes = dto.PriceNotes,
            AppliedPromotionsJSON = dto.AppliedPromotionsJSON,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser,
            TenantId = tenantContext.CurrentTenantId ?? Guid.Empty
        }).ToList();

        context.DocumentRows.AddRange(rows);
        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.LogEntityChangeAsync(
            "DocumentRow", documentHeaderId, "BULK_CREATE", "BULK_CREATE",
            null, $"Bulk created {rows.Count} rows for document {documentHeaderId}", currentUser);

        logger.LogInformation("Bulk created {Count} DocumentRows for document {DocumentId}", rows.Count, documentHeaderId);
        return rows.Select(MapToDto);
    }

    /// <inheritdoc />
    public async Task<bool> ReorderDocumentRowsAsync(
        Guid documentHeaderId,
        Dictionary<Guid, int> rowOrderUpdates,
        string currentUser,
        CancellationToken cancellationToken = default)
    {
        var rowIds = rowOrderUpdates.Keys.ToList();
        var rows = await context.DocumentRows
            .Where(r => r.DocumentHeaderId == documentHeaderId && rowIds.Contains(r.Id) && !r.IsDeleted)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return false;

        foreach (var row in rows)
        {
            if (rowOrderUpdates.TryGetValue(row.Id, out var newOrder))
            {
                row.SortOrder = newOrder;
                row.ModifiedAt = DateTime.UtcNow;
                row.ModifiedBy = currentUser;
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        _ = await auditLogService.LogEntityChangeAsync(
            "DocumentRow", documentHeaderId, "REORDER", "REORDER",
            null, $"Reordered {rows.Count} rows for document {documentHeaderId}", currentUser);

        logger.LogInformation("Reordered {Count} rows for document {DocumentId}", rows.Count, documentHeaderId);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DocumentRowExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await context.DocumentRows
            .AsNoTracking()
            .AnyAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);
    }

    private static DocumentRowDto MapToDto(DocumentRow row)
    {
        return new DocumentRowDto
        {
            Id = row.Id,
            DocumentHeaderId = row.DocumentHeaderId,
            RowType = (Prym.DTOs.Common.DocumentRowType)row.RowType,
            ParentRowId = row.ParentRowId,
            ProductCode = row.ProductCode,
            ProductId = row.ProductId,
            LocationId = row.LocationId,
            Description = row.Description,
            UnitOfMeasure = row.UnitOfMeasure,
            UnitOfMeasureId = row.UnitOfMeasureId,
            UnitPrice = row.UnitPrice,
            Quantity = row.Quantity,
            LineDiscount = row.LineDiscount,
            LineDiscountValue = row.LineDiscountValue,
            DiscountType = row.DiscountType,
            VatRate = row.VatRate,
            VatDescription = row.VatDescription,
            IsGift = row.IsGift,
            IsManual = row.IsManual,
            SourceWarehouseId = row.SourceWarehouseId,
            DestinationWarehouseId = row.DestinationWarehouseId,
            Notes = row.Notes,
            SortOrder = row.SortOrder,
            StationId = row.StationId,
            BaseQuantity = row.BaseQuantity,
            BaseUnitPrice = row.BaseUnitPrice,
            BaseUnitOfMeasureId = row.BaseUnitOfMeasureId,
            IsPriceManual = row.IsPriceManual,
            AppliedPriceListId = row.AppliedPriceListId,
            OriginalPriceFromPriceList = row.OriginalPriceFromPriceList,
            PriceNotes = row.PriceNotes,
            AppliedPromotionsJSON = row.AppliedPromotionsJSON,
            LineTotal = row.LineTotal,
            VatTotal = row.VatTotal,
            DiscountTotal = row.DiscountTotal,
            CreatedAt = row.CreatedAt,
            CreatedBy = row.CreatedBy,
            ModifiedAt = row.ModifiedAt,
            ModifiedBy = row.ModifiedBy
        };
    }
}
