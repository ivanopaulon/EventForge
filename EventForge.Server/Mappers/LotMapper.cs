using EventForge.DTOs.Warehouse;
using EventForge.Server.Data.Entities.Warehouse;

namespace EventForge.Server.Mappers;

/// <summary>
/// Static mapper for Lot entities and DTOs.
/// </summary>
public static class LotMapper
{
    /// <summary>
    /// Maps a Lot entity to a LotDto.
    /// </summary>
    public static LotDto ToDto(Lot lot)
    {
        return new LotDto
        {
            Id = lot.Id,
            TenantId = lot.TenantId,
            Code = lot.Code,
            ProductId = lot.ProductId,
            ProductName = lot.Product?.Name,
            ProductCode = lot.Product?.Code,
            ProductionDate = lot.ProductionDate,
            ExpiryDate = lot.ExpiryDate,
            SupplierId = lot.SupplierId,
            SupplierName = lot.Supplier?.Name,
            OriginalQuantity = lot.OriginalQuantity,
            AvailableQuantity = lot.AvailableQuantity,
            Status = lot.Status.ToString(),
            QualityStatus = lot.QualityStatus.ToString(),
            Notes = lot.Notes,
            Barcode = lot.Barcode,
            CountryOfOrigin = lot.CountryOfOrigin,
            CreatedAt = lot.CreatedAt,
            CreatedBy = lot.CreatedBy,
            ModifiedAt = lot.ModifiedAt,
            ModifiedBy = lot.ModifiedBy,
            IsActive = lot.IsActive
        };
    }

    /// <summary>
    /// Maps a CreateLotDto to a Lot entity.
    /// </summary>
    public static Lot ToEntity(CreateLotDto createDto, Guid tenantId, string createdBy)
    {
        return new Lot
        {
            TenantId = tenantId,
            Code = createDto.Code,
            ProductId = createDto.ProductId,
            ProductionDate = createDto.ProductionDate,
            ExpiryDate = createDto.ExpiryDate,
            SupplierId = createDto.SupplierId,
            OriginalQuantity = createDto.OriginalQuantity,
            AvailableQuantity = createDto.OriginalQuantity, // Initial available = original
            Notes = createDto.Notes,
            Barcode = createDto.Barcode,
            CountryOfOrigin = createDto.CountryOfOrigin,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    /// <summary>
    /// Updates a Lot entity from an UpdateLotDto.
    /// </summary>
    public static void UpdateEntity(Lot lot, UpdateLotDto updateDto, string modifiedBy)
    {
        lot.Code = updateDto.Code;
        lot.ProductionDate = updateDto.ProductionDate;
        lot.ExpiryDate = updateDto.ExpiryDate;
        lot.SupplierId = updateDto.SupplierId;
        lot.AvailableQuantity = updateDto.AvailableQuantity;
        lot.Notes = updateDto.Notes;
        lot.Barcode = updateDto.Barcode;
        lot.CountryOfOrigin = updateDto.CountryOfOrigin;
        lot.IsActive = updateDto.IsActive;
        lot.ModifiedBy = modifiedBy;
        lot.ModifiedAt = DateTime.UtcNow;
    }
}