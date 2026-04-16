using Prym.DTOs.Common;
using Prym.DTOs.Reports;

namespace Prym.Web.Services;

/// <summary>
/// Client service for managing Bold Reports report definitions.
/// </summary>
public interface IReportDefinitionService
{
    /// <summary>Returns a paginated list of reports.</summary>
    Task<PagedResult<ReportListItemDto>?> GetReportsAsync(
        string? category    = null,
        string? searchTerm  = null,
        int     page        = 1,
        int     pageSize    = 25,
        CancellationToken ct = default);

    /// <summary>Returns the full report definition (including RDLC content).</summary>
    Task<ReportDefinitionDto?> GetReportAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns distinct categories used by the tenant's reports.</summary>
    Task<IReadOnlyList<string>?> GetCategoriesAsync(CancellationToken ct = default);

    /// <summary>Creates a new report definition.</summary>
    Task<ReportDefinitionDto?> CreateReportAsync(CreateReportDto dto, CancellationToken ct = default);

    /// <summary>Updates an existing report definition.</summary>
    Task<ReportDefinitionDto?> UpdateReportAsync(Guid id, UpdateReportDto dto, CancellationToken ct = default);

    /// <summary>Deletes a report definition.</summary>
    Task DeleteReportAsync(Guid id, CancellationToken ct = default);
}
