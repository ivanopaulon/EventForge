using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Reports;

/// <summary>
/// Represents a saved report definition (RDLC layout + metadata) for the Bold Reports designer/viewer.
/// One record per report per tenant; the RDLC XML is stored in <see cref="ReportContent"/>.
/// </summary>
public class ReportDefinition : AuditableEntity
{
    /// <summary>
    /// Display name of the report.
    /// </summary>
    [Required(ErrorMessage = "The report name is required.")]
    [MaxLength(200, ErrorMessage = "The report name cannot exceed 200 characters.")]
    [Display(Name = "Name", Description = "Display name of the report.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the report's purpose or contents.
    /// </summary>
    [MaxLength(1000, ErrorMessage = "The description cannot exceed 1000 characters.")]
    [Display(Name = "Description", Description = "Optional description of the report.")]
    public string? Description { get; set; }

    /// <summary>
    /// Category used to group reports in the list view (e.g. "Sales", "Warehouse", "Fiscal").
    /// </summary>
    [MaxLength(100, ErrorMessage = "The category cannot exceed 100 characters.")]
    [Display(Name = "Category", Description = "Category for grouping (e.g. Sales, Warehouse).")]
    public string? Category { get; set; }

    /// <summary>
    /// Full RDLC XML content created/edited by the Bold Reports designer.
    /// May be null for newly created, un-designed reports.
    /// </summary>
    [Display(Name = "Report Content", Description = "RDLC XML content of the report.")]
    public string? ReportContent { get; set; }

    /// <summary>
    /// Whether this report is publicly visible to all roles (not just admins).
    /// </summary>
    [Display(Name = "Is Public", Description = "Whether this report is visible to all roles.")]
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// Navigation: data sources declared for this report definition.
    /// </summary>
    [Display(Name = "Data Sources", Description = "Data sources declared for this report.")]
    public ICollection<ReportDataSource> DataSources { get; set; } = new List<ReportDataSource>();
}
