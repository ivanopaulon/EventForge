namespace Prym.DTOs.Reports;

/// <summary>
/// Lightweight report summary for list views — excludes the heavy RDLC content.
/// </summary>
public class ReportListItemDto
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Category for grouping.</summary>
    public string? Category { get; set; }

    /// <summary>Whether a RDLC design has been saved (ReportContent is not null/empty).</summary>
    public bool HasDesign { get; set; }

    /// <summary>Whether the report is publicly visible to all roles.</summary>
    public bool IsPublic { get; set; }

    /// <summary>Whether the report is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>User who created the report.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>UTC last modification timestamp.</summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>Number of declared data sources.</summary>
    public int DataSourceCount { get; set; }
}
