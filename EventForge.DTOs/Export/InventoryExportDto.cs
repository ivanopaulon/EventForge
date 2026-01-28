using System;

namespace EventForge.DTOs.Export;

public class InventoryExportDto
{
    public Guid Id { get; set; }
    public DateTime MovementDate { get; set; }
    public string Product { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public string StorageLocation { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string? DocumentReference { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
