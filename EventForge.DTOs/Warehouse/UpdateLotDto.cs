using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{

/// <summary>
/// DTO for updating lot information.
/// </summary>
public class UpdateLotDto
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;
    
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    
    public Guid? SupplierId { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal AvailableQuantity { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    [StringLength(50)]
    public string? Barcode { get; set; }
    
    [StringLength(50)]
    public string? CountryOfOrigin { get; set; }
    
    public bool IsActive { get; set; } = true;
}
}