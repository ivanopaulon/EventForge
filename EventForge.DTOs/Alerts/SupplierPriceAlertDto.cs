using System;

namespace EventForge.DTOs.Alerts;

/// <summary>
/// DTO for supplier price alert display.
/// </summary>
public class SupplierPriceAlertDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    // References
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductCode { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }

    // Alert Data
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    // Price Info
    public decimal? OldPrice { get; set; }
    public decimal? NewPrice { get; set; }
    public decimal? PriceChangePercentage { get; set; }
    public string Currency { get; set; } = "EUR";
    public decimal? PotentialSavings { get; set; }

    // Content
    public string AlertTitle { get; set; } = string.Empty;
    public string AlertMessage { get; set; } = string.Empty;
    public string? RecommendedAction { get; set; }

    // Links
    public Guid? BetterSupplierSuggestionId { get; set; }

    // Tracking
    public DateTime CreatedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgedByUserId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedByUserId { get; set; }
    public string? ResolutionNotes { get; set; }

    // Notifications
    public bool EmailSent { get; set; }
    public DateTime? EmailSentAt { get; set; }
}
