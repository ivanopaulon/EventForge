namespace EventForge.DTOs.Warehouse;

public class InventoryRowRepairDto
{
    public Guid? NewProductId { get; set; }
    public Guid? NewLocationId { get; set; }
    public decimal? NewQuantity { get; set; }
    public string? NewNotes { get; set; }
}
