using EventForge.Server.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Common;
using Prym.DTOs.Reports;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for Bold Reports report definitions.
/// Provides CRUD operations, data source endpoints, and server-side export.
/// </summary>
[Route("api/v1/reports")]
[Authorize]
[ApiController]
public class ReportsController(
    IReportDefinitionService reportService,
    ITenantContext tenantContext) : BaseApiController
{
    // ── List / Get ───────────────────────────────────────────────────────────

    /// <summary>Returns a paginated list of report definitions for the current tenant.</summary>
    /// <param name="category">Optional category filter.</param>
    /// <param name="search">Optional free-text search on name/description.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page (1–100).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReportListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ReportListItemDto>>> GetReports(
        [FromQuery] string? category = null,
        [FromQuery] string? search   = null,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 25,
        CancellationToken ct = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError is not null) return tenantError;

        var paginationError = ValidatePaginationParameters(page, pageSize, maxPageSize: 10_000);
        if (paginationError is not null) return paginationError;

        try
        {
            var result = await reportService.GetReportsAsync(category, search, page, pageSize, ct);
            SetPaginationHeaders(result, new PaginationParameters { Page = page, PageSize = pageSize });
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving reports.", ex);
        }
    }

    /// <summary>Returns the full report definition (including RDLC content) for the given ID.</summary>
    /// <param name="id">Report definition GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReportDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReportDefinitionDto>> GetReport(Guid id, CancellationToken ct = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError is not null) return tenantError;

        try
        {
            var report = await reportService.GetReportAsync(id, ct);
            return report is null ? CreateNotFoundProblem($"Report {id} not found.") : Ok(report);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem($"An error occurred while retrieving report {id}.", ex);
        }
    }

    /// <summary>Returns distinct categories in use by the current tenant's reports.</summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetCategories(CancellationToken ct = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError is not null) return tenantError;

        try
        {
            return Ok(await reportService.GetCategoriesAsync(ct));
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving report categories.", ex);
        }
    }

    // ── Create / Update / Delete ─────────────────────────────────────────────

    /// <summary>Creates a new report definition.</summary>
    /// <param name="dto">Create payload.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ReportDefinitionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReportDefinitionDto>> CreateReport(
        [FromBody] CreateReportDto dto,
        CancellationToken ct = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError is not null) return tenantError;

        if (!ModelState.IsValid) return CreateValidationProblemDetails();

        try
        {
            var created = await reportService.CreateReportAsync(dto, ct);
            return CreatedAtAction(nameof(GetReport), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the report.", ex);
        }
    }

    /// <summary>
    /// Updates an existing report definition.
    /// The designer sends the full RDLC XML in <c>ReportContent</c> when saving the design.
    /// </summary>
    /// <param name="id">Report definition GUID.</param>
    /// <param name="dto">Update payload.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ReportDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReportDefinitionDto>> UpdateReport(
        Guid id,
        [FromBody] UpdateReportDto dto,
        CancellationToken ct = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError is not null) return tenantError;

        if (!ModelState.IsValid) return CreateValidationProblemDetails();

        try
        {
            var updated = await reportService.UpdateReportAsync(id, dto, ct);
            return updated is null ? CreateNotFoundProblem($"Report {id} not found.") : Ok(updated);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem($"An error occurred while updating report {id}.", ex);
        }
    }

    /// <summary>Soft-deletes a report definition.</summary>
    /// <param name="id">Report definition GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReport(Guid id, CancellationToken ct = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError is not null) return tenantError;

        try
        {
            var deleted = await reportService.DeleteReportAsync(id, ct);
            return deleted ? NoContent() : CreateNotFoundProblem($"Report {id} not found.");
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem($"An error occurred while deleting report {id}.", ex);
        }
    }

    // ── Data source endpoints ────────────────────────────────────────────────

    /// <summary>
    /// Returns JSON data for the named entity type, to be consumed by the Bold Reports designer/viewer.
    /// </summary>
    /// <param name="entityType">Entity type key (e.g. "DocumentHeaders", "Products").</param>
    /// <param name="from">Optional start date for time-series data sources.</param>
    /// <param name="to">Optional end date for time-series data sources.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("datasources/{entityType}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDataSource(
        string entityType,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to   = null,
        CancellationToken ct = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError is not null) return tenantError;

        try
        {
            var data = await reportService.GetDataSourceDataAsync(entityType, from, to, ct);
            return Ok(data);
        }
        catch (ArgumentException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem($"An error occurred while retrieving data source '{entityType}'.", ex);
        }
    }
}
