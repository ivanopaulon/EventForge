using EventForge.Services.Audit;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Controllers;

/// <summary>
/// REST API controller for audit log consultation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
    }

    /// <summary>
    /// Gets paginated audit logs with optional filtering.
    /// </summary>
    /// <param name="queryParameters">Query parameters for filtering, sorting and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated audit logs</returns>
    /// <response code="200">Returns the paginated audit logs</response>
    /// <response code="400">If the query parameters are invalid</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EntityChangeLog>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<EntityChangeLog>>> GetAuditLogs(
        [FromQuery] AuditLogQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _auditLogService.GetPagedLogsAsync(queryParameters, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Log the exception (you might want to use ILogger here)
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving audit logs.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a specific audit log by ID.
    /// </summary>
    /// <param name="id">The audit log ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The audit log entry</returns>
    /// <response code="200">Returns the audit log entry</response>
    /// <response code="404">If the audit log is not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EntityChangeLog), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EntityChangeLog>> GetAuditLog(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = await _auditLogService.GetLogByIdAsync(id, cancellationToken);

            if (auditLog == null)
            {
                return NotFound(new { message = $"Audit log with ID {id} not found." });
            }

            return Ok(auditLog);
        }
        catch (Exception ex)
        {
            // Log the exception (you might want to use ILogger here)
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the audit log.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets audit logs for a specific entity.
    /// </summary>
    /// <param name="entityId">The entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of audit logs for the entity</returns>
    /// <response code="200">Returns the audit logs for the entity</response>
    [HttpGet("entity/{entityId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<EntityChangeLog>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EntityChangeLog>>> GetEntityAuditLogs(
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLogs = await _auditLogService.GetEntityLogsAsync(entityId, cancellationToken);
            return Ok(auditLogs);
        }
        catch (Exception ex)
        {
            // Log the exception (you might want to use ILogger here)
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving entity audit logs.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets audit logs for a specific entity type.
    /// </summary>
    /// <param name="entityName">The entity type name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of audit logs for the entity type</returns>
    /// <response code="200">Returns the audit logs for the entity type</response>
    /// <response code="400">If the entity name is invalid</response>
    [HttpGet("entity-type/{entityName}")]
    [ProducesResponseType(typeof(IEnumerable<EntityChangeLog>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<EntityChangeLog>>> GetEntityTypeAuditLogs(
        string entityName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityName))
        {
            return BadRequest(new { message = "Entity name cannot be empty." });
        }

        try
        {
            var auditLogs = await _auditLogService.GetEntityTypeLogsAsync(entityName, cancellationToken);
            return Ok(auditLogs);
        }
        catch (Exception ex)
        {
            // Log the exception (you might want to use ILogger here)
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving entity type audit logs.", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets audit logs for a specific user.
    /// </summary>
    /// <param name="username">The username</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of audit logs for the user</returns>
    /// <response code="200">Returns the audit logs for the user</response>
    /// <response code="400">If the username is invalid</response>
    [HttpGet("user/{username}")]
    [ProducesResponseType(typeof(IEnumerable<EntityChangeLog>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<EntityChangeLog>>> GetUserAuditLogs(
        string username,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest(new { message = "Username cannot be empty." });
        }

        try
        {
            var auditLogs = await _auditLogService.GetUserLogsAsync(username, cancellationToken);
            return Ok(auditLogs);
        }
        catch (Exception ex)
        {
            // Log the exception (you might want to use ILogger here)
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving user audit logs.", error = ex.Message });
        }
    }
}