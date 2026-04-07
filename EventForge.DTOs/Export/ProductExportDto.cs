namespace EventForge.DTOs.Export;

public class ProductExportDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public decimal StockQuantity { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
