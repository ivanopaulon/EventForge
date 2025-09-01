using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{

/// <summary>
/// DTO for updating an existing serial number.
/// </summary>
public class UpdateSerialDto
{
    [StringLength(100, ErrorMessage = "Serial number cannot exceed 100 characters.")]
    public string? SerialNumber { get; set; }

    public Guid? LotId { get; set; }

    public Guid? CurrentLocationId { get; set; }

    public DateTime? ManufacturingDate { get; set; }

    public DateTime? WarrantyExpiry { get; set; }

    public Guid? OwnerId { get; set; }

    public DateTime? SaleDate { get; set; }

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }

    [StringLength(50, ErrorMessage = "Barcode cannot exceed 50 characters.")]
    public string? Barcode { get; set; }

    [StringLength(50, ErrorMessage = "RFID tag cannot exceed 50 characters.")]
    public string? RfidTag { get; set; }

    public bool? IsActive { get; set; }
}
}