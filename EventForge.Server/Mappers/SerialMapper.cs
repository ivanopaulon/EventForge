using EventForge.DTOs.Warehouse;
using EventForge.Server.Data.Entities.Warehouse;

namespace EventForge.Server.Mappers;

/// <summary>
/// Static mapper for Serial entities and DTOs.
/// </summary>
public static class SerialMapper
{
    /// <summary>
    /// Maps a Serial entity to a SerialDto.
    /// </summary>
    public static SerialDto ToSerialDto(this Serial serial)
    {
        return new SerialDto
        {
            Id = serial.Id,
            TenantId = serial.TenantId,
            SerialNumber = serial.SerialNumber,
            ProductId = serial.ProductId,
            ProductName = serial.Product?.Name,
            ProductCode = serial.Product?.Code,
            LotId = serial.LotId,
            LotCode = serial.Lot?.Code,
            CurrentLocationId = serial.CurrentLocationId,
            CurrentLocationCode = serial.CurrentLocation?.Code,
            WarehouseName = serial.CurrentLocation?.Warehouse?.Name,
            Status = serial.Status.ToString(),
            ManufacturingDate = serial.ManufacturingDate,
            WarrantyExpiry = serial.WarrantyExpiry,
            OwnerId = serial.OwnerId,
            OwnerName = serial.Owner?.Name,
            SaleDate = serial.SaleDate,
            Notes = serial.Notes,
            Barcode = serial.Barcode,
            RfidTag = serial.RfidTag,
            CreatedAt = serial.CreatedAt,
            CreatedBy = serial.CreatedBy,
            ModifiedAt = serial.ModifiedAt,
            ModifiedBy = serial.ModifiedBy,
            IsActive = serial.IsActive
        };
    }

    /// <summary>
    /// Maps a CreateSerialDto to a Serial entity.
    /// </summary>
    public static Serial ToEntity(this CreateSerialDto createDto, Guid tenantId, string createdBy)
    {
        return new Serial
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SerialNumber = createDto.SerialNumber,
            ProductId = createDto.ProductId,
            LotId = createDto.LotId,
            CurrentLocationId = createDto.CurrentLocationId,
            Status = SerialStatus.Available,
            ManufacturingDate = createDto.ManufacturingDate,
            WarrantyExpiry = createDto.WarrantyExpiry,
            Notes = createDto.Notes,
            Barcode = createDto.Barcode,
            RfidTag = createDto.RfidTag,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    /// <summary>
    /// Updates a Serial entity from an UpdateSerialDto.
    /// </summary>
    public static void UpdateFromDto(this Serial serial, UpdateSerialDto updateDto, string modifiedBy)
    {
        if (!string.IsNullOrEmpty(updateDto.SerialNumber))
            serial.SerialNumber = updateDto.SerialNumber;
        if (updateDto.LotId.HasValue)
            serial.LotId = updateDto.LotId.Value;
        if (updateDto.CurrentLocationId.HasValue)
            serial.CurrentLocationId = updateDto.CurrentLocationId.Value;
        if (updateDto.ManufacturingDate.HasValue)
            serial.ManufacturingDate = updateDto.ManufacturingDate.Value;
        if (updateDto.WarrantyExpiry.HasValue)
            serial.WarrantyExpiry = updateDto.WarrantyExpiry.Value;
        if (updateDto.OwnerId.HasValue)
            serial.OwnerId = updateDto.OwnerId.Value;
        if (updateDto.SaleDate.HasValue)
            serial.SaleDate = updateDto.SaleDate.Value;
        if (!string.IsNullOrEmpty(updateDto.Notes))
            serial.Notes = updateDto.Notes;
        if (!string.IsNullOrEmpty(updateDto.Barcode))
            serial.Barcode = updateDto.Barcode;
        if (!string.IsNullOrEmpty(updateDto.RfidTag))
            serial.RfidTag = updateDto.RfidTag;
        if (updateDto.IsActive.HasValue)
            serial.IsActive = updateDto.IsActive.Value;

        serial.ModifiedBy = modifiedBy;
        serial.ModifiedAt = DateTime.UtcNow;
    }
}