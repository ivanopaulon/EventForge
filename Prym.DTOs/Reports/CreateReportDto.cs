using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Reports;

/// <summary>
/// Payload for creating a new report definition.
/// The RDLC content is saved separately via the designer (PUT endpoint).
/// </summary>
public class CreateReportDto
{
    /// <summary>Display name of the report.</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>Category for grouping (e.g. Sales, Warehouse, Fiscal).</summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>Whether this report is publicly visible to all roles.</summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>Initial data sources to declare (optional).</summary>
    public IList<CreateReportDataSourceDto> DataSources { get; set; } = [];
}

/// <summary>
/// Declares a data source when creating a report.
/// </summary>
public class CreateReportDataSourceDto
{
    /// <summary>Name referenced in the RDLC XML.</summary>
    [Required]
    [MaxLength(100)]
    public string DataSourceName { get; set; } = string.Empty;

    /// <summary>EF entity type backing this data source (e.g. "DocumentHeaders", "Products").</summary>
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    [MaxLength(500)]
    public string? Description { get; set; }
}
