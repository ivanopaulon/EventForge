namespace Prym.DTOs.Reports;

/// <summary>
/// Full report definition DTO including the RDLC content.
/// Used by the designer/viewer and for detail views.
/// </summary>
public class ReportDefinitionDto
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Tenant owning this report.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Display name of the report.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Category for grouping (e.g. Sales, Warehouse, Fiscal).</summary>
    public string? Category { get; set; }

    /// <summary>Full RDLC XML content. May be null for un-designed reports.</summary>
    public string? ReportContent { get; set; }

    /// <summary>Whether this report is visible to all roles.</summary>
    public bool IsPublic { get; set; }

    /// <summary>Whether this report is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>User who created the report.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>UTC last modification timestamp.</summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>User who last modified the report.</summary>
    public string? ModifiedBy { get; set; }

    /// <summary>Data sources declared for this report.</summary>
    public IReadOnlyList<ReportDataSourceDto> DataSources { get; set; } = [];
}
