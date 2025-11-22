using System;

namespace EventForge.DTOs.Alerts;

/// <summary>
/// Request for filtering alerts.
/// </summary>
public class AlertFilterRequest
{
    public string? Status { get; set; }
    public string? Severity { get; set; }
    public string? AlertType { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? SupplierId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    
    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    
    // Sorting
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; } = "desc";
}
