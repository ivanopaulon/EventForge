namespace Prym.DTOs.Reports;

/// <summary>
/// Represents a data source declared for a report definition.
/// </summary>
public class ReportDataSourceDto
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Name referenced in the RDLC XML.</summary>
    public string DataSourceName { get; set; } = string.Empty;

    /// <summary>EF entity type backing this data source.</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }
}

/// <summary>
/// Enumerates the built-in data source types the server exposes for report binding.
/// </summary>
public static class ReportDataSourceEntityTypes
{
    public const string DocumentHeaders = "DocumentHeaders";
    public const string DocumentRows    = "DocumentRows";
    public const string Products        = "Products";
    public const string BusinessParties = "BusinessParties";
    public const string Sales           = "Sales";
    public const string Warehouse       = "Warehouse";
    public const string Fiscal          = "Fiscal";
}
