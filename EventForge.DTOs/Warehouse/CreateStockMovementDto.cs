using System;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Warehouse
{

/// <summary>
/// DTO for creating a stock movement.
/// </summary>
public class CreateStockMovementDto
{
    [Required]
    public string MovementType { get; set; } = string.Empty;
    
    [Required]
    public Guid ProductId { get; set; }
    
    public Guid? LotId { get; set; }
    public Guid? SerialId { get; set; }
    
    public Guid? FromLocationId { get; set; }
    public Guid? ToLocationId { get; set; }
    
    [Required]
    public decimal Quantity { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal? UnitCost { get; set; }
    
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    
    public Guid? DocumentHeaderId { get; set; }
    public Guid? DocumentRowId { get; set; }
    
    [Required]
    public string Reason { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    [StringLength(50)]
    public string? Reference { get; set; }
    
    [StringLength(100)]
    public string? UserId { get; set; }
    
    public Guid? MovementPlanId { get; set; }
}
}