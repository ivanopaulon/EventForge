namespace EventForge.DTOs.Export;

public class DocumentExportDto
{
    public Guid Id { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public string BusinessParty { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal TotalVat { get; set; }
    public decimal NetAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
