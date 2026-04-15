using System.ComponentModel.DataAnnotations;

namespace Prym.DTOs.Reports;

/// <summary>
/// Request payload for exporting a report to a specific file format.
/// </summary>
public class ReportExportRequestDto
{
    /// <summary>
    /// Target export format.
    /// </summary>
    [Required]
    public ReportExportFormat Format { get; set; } = ReportExportFormat.Pdf;

    /// <summary>
    /// Optional report parameters as key-value pairs (e.g. date range filters).
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = [];
}

/// <summary>
/// Supported export formats for Bold Reports.
/// </summary>
public enum ReportExportFormat
{
    Pdf   = 0,
    Excel = 1,
    Word  = 2,
    Html  = 3,
    Csv   = 4,
}
