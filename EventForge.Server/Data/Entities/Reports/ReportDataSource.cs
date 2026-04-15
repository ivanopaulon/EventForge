using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.Data.Entities.Reports;

/// <summary>
/// Declares a named data source available to a report definition.
/// The Bold Reports designer binds data-set fields to these data sources.
/// The server exposes each data source via GET /api/v1/reports/{id}/data/{name}.
/// </summary>
public class ReportDataSource : AuditableEntity
{
    /// <summary>
    /// Foreign key to the owning <see cref="ReportDefinition"/>.
    /// </summary>
    [Required]
    [Display(Name = "Report Definition", Description = "Owning report definition.")]
    public Guid ReportDefinitionId { get; set; }

    /// <summary>
    /// Navigation: owning report definition.
    /// </summary>
    public ReportDefinition? ReportDefinition { get; set; }

    /// <summary>
    /// Name of the data source as referenced in the RDLC XML (e.g. "Sales", "Products").
    /// Must be unique within the same report definition.
    /// </summary>
    [Required]
    [MaxLength(100, ErrorMessage = "The data source name cannot exceed 100 characters.")]
    [Display(Name = "Data Source Name", Description = "Name referenced in the RDLC XML.")]
    public string DataSourceName { get; set; } = string.Empty;

    /// <summary>
    /// Entity or query type that backs this data source (e.g. "DocumentHeaders", "Products").
    /// Used by the server to resolve the correct EF query.
    /// </summary>
    [Required]
    [MaxLength(100, ErrorMessage = "The entity type cannot exceed 100 characters.")]
    [Display(Name = "Entity Type", Description = "EF entity type backing this data source.")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Optional human-readable description for the data source.
    /// </summary>
    [MaxLength(500)]
    [Display(Name = "Description", Description = "Optional description of the data source.")]
    public string? Description { get; set; }
}
