using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{

/// <summary>
/// DTO for creating a new lot.
/// </summary>
public class CreateLotDto
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;
    
    [Required]
    public Guid ProductId { get; set; }
    
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    
    public Guid? SupplierId { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal OriginalQuantity { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    [StringLength(50)]
    public string? Barcode { get; set; }
    
    [StringLength(50)]
    public string? CountryOfOrigin { get; set; }
}
}