namespace EventForge.DTOs.Warehouse;

public class InventoryAutoRepairOptionsDto
{
    public bool MergeDuplicates { get; set; } = true;
    public bool FixMissingData { get; set; } = true;
    public bool RemoveInvalidReferences { get; set; } = false;
    public bool ConvertNegativeQuantities { get; set; } = true;
    public Guid? DefaultLocationId { get; set; }
}
