using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.Alerts;

/// <summary>
/// Request to create a new alert.
/// </summary>
public class CreateAlertRequest
{
    public Guid? ProductId { get; set; }
    public Guid? SupplierId { get; set; }
    
    [Required]
    public string AlertType { get; set; } = string.Empty;
    
    [Required]
    public string Severity { get; set; } = string.Empty;
    
    public decimal? OldPrice { get; set; }
    public decimal? NewPrice { get; set; }
    public decimal? PriceChangePercentage { get; set; }
    public string Currency { get; set; } = "EUR";
    public decimal? PotentialSavings { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string AlertTitle { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(1000)]
    public string AlertMessage { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? RecommendedAction { get; set; }
    
    public Guid? BetterSupplierSuggestionId { get; set; }
}
