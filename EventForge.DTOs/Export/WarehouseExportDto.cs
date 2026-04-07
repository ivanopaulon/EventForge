namespace EventForge.DTOs.Export;

public class WarehouseExportDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public bool IsActive { get; set; }
    public int TotalStorageLocations { get; set; }
    public DateTime CreatedAt { get; set; }
}
