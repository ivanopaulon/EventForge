namespace EventForge.DTOs.Export;

/// <summary>
/// DTO for exporting business parties
/// </summary>
public class BusinessPartyExportDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PartyType { get; set; } = string.Empty;
    public string? VatNumber { get; set; }
    public string? FiscalCode { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
