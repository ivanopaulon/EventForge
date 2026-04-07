using EventForge.Server.ModelBinders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for audit log operations with standardized pagination.
/// </summary>
[ApiController]
[Route("api/v1/audit-logs")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AuditLogsController(
    IAuditLogService service,
    ITenantContext tenantContext) : BaseApiController
{

    /// <summary>
    /// Retrieves all audit logs with pagination (Admin/SuperAdmin only)
    /// </summary>
    /// <param name="pagination">Pagination parameters. Max pageSize based on role: User=1000, Admin=5000, SuperAdmin=10000</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of audit logs</returns>
    /// <response code="200">Successfully retrieved audit logs with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="403">User not authorized to view audit logs</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EntityChangeLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<EntityChangeLogDto>>> GetAuditLogs(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError is not null) return tenantError;

        try
        {
            var result = await service.GetAuditLogsAsync(pagination, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving audit logs.", ex);
        }
    }

    /// <summary>
    /// Retrieves audit logs for specific entity type (e.g., "Product", "Invoice")
    /// </summary>
    /// <param name="entityType">Entity type name</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of audit logs for the specified entity type</returns>
    /// <response code="200">Successfully retrieved audit logs with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="403">User not authorized to view audit logs</response>
    [HttpGet("entity/{entityType}")]
    [ProducesResponseType(typeof(PagedResult<EntityChangeLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<EntityChangeLogDto>>> GetLogsByEntity(
        string entityType,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError is not null) return tenantError;

        try
        {
            var result = await service.GetLogsByEntityAsync(entityType, pagination, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem($"An error occurred while retrieving audit logs for entity type {entityType}.", ex);
        }
    }

    /// <summary>
    /// Retrieves audit logs for specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of audit logs for the specified user</returns>
    /// <response code="200">Successfully retrieved audit logs with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="403">User not authorized to view audit logs</response>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(PagedResult<EntityChangeLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<EntityChangeLogDto>>> GetLogsByUser(
        Guid userId,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError is not null) return tenantError;

        try
        {
            var result = await service.GetLogsByUserAsync(userId, pagination, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem($"An error occurred while retrieving audit logs for user {userId}.", ex);
        }
    }

    /// <summary>
    /// Retrieves audit logs within a date range
    /// </summary>
    /// <param name="startDate">Start date (inclusive)</param>
    /// <param name="endDate">End date (inclusive, defaults to current UTC time if not provided)</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of audit logs within the date range</returns>
    /// <response code="200">Successfully retrieved audit logs with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters or date range</response>
    /// <response code="403">User not authorized to view audit logs</response>
    [HttpGet("date-range")]
    [ProducesResponseType(typeof(PagedResult<EntityChangeLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<EntityChangeLogDto>>> GetLogsByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError is not null) return tenantError;

        try
        {
            var result = await service.GetLogsByDateRangeAsync(startDate, endDate, pagination, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem($"An error occurred while retrieving audit logs for the specified date range.", ex);
        }
    }
}
