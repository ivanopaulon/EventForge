using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Reports;

/// <summary>
/// Payload for updating an existing report definition.
/// The designer sends the full RDLC content via <see cref="ReportContent"/>.
/// </summary>
public class UpdateReportDto
{
    /// <summary>Display name of the report.</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>Category for grouping.</summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// Full RDLC XML saved by the Bold Reports designer.
    /// Null means "do not change the existing content".
    /// </summary>
    public string? ReportContent { get; set; }

    /// <summary>Whether this report is publicly visible to all roles.</summary>
    public bool IsPublic { get; set; }

    /// <summary>Whether this report is active.</summary>
    public bool IsActive { get; set; }
}
