using Prym.DTOs.Common;
using Prym.DTOs.Reports;

namespace EventForge.Server.Services.Reports;

/// <summary>
/// Service for managing Bold Reports report definitions (CRUD, data sources, export).
/// All operations are tenant-scoped.
/// </summary>
public interface IReportDefinitionService
{
    /// <summary>
    /// Returns a paginated list of reports for the current tenant.
    /// </summary>
    /// <param name="category">Optional category filter.</param>
    /// <param name="searchTerm">Optional free-text filter on name/description.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page (max 100).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PagedResult<ReportListItemDto>> GetReportsAsync(
        string? category = null,
        string? searchTerm = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the full report definition including RDLC content.
    /// </summary>
    Task<ReportDefinitionDto?> GetReportAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new report definition.
    /// </summary>
    Task<ReportDefinitionDto> CreateReportAsync(CreateReportDto dto, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing report definition (name, description, RDLC content, etc.).
    /// </summary>
    Task<ReportDefinitionDto?> UpdateReportAsync(Guid id, UpdateReportDto dto, CancellationToken ct = default);

    /// <summary>
    /// Persists raw RDLC content for an existing report definition.
    /// Used by the Bold Reports designer service (SetData) to store the design bytes.
    /// </summary>
    Task<bool> SaveReportContentAsync(Guid id, string rdlcContent, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a report definition.
    /// </summary>
    Task<bool> DeleteReportAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns the distinct categories in use by the current tenant's reports.
    /// </summary>
    Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns JSON data for the named data source, scoped to the current tenant.
    /// Used by the Bold Reports designer/viewer when fetching live data.
    /// </summary>
    /// <param name="entityType">Entity type key (see <see cref="ReportDataSourceEntityTypes"/>).</param>
    /// <param name="dateFrom">Optional start date filter for time-series data sources.</param>
    /// <param name="dateTo">Optional end date filter for time-series data sources.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<object> GetDataSourceDataAsync(
        string entityType,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken ct = default);
}
