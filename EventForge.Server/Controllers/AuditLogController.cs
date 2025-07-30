using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EventForge.DTOs.Audit;
using EventForge.DTOs.Common;
using EventForge.DTOs.SuperAdmin;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for audit log consultation.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class AuditLogController : BaseApiController
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
    [ProducesResponseType(typeof(PagedResult<EntityChangeLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<EntityChangeLogDto>>> GetAuditLogs(
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
            var dtoResult = new PagedResult<EntityChangeLogDto>
            {
                Items = result.Items.ToDto(),
                TotalCount = result.TotalCount,
                PageSize = result.PageSize,
                Page = result.Page
            };
            return Ok(dtoResult);
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
    [ProducesResponseType(typeof(EntityChangeLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EntityChangeLogDto>> GetAuditLog(
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

            return Ok(auditLog.ToDto());
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
    [ProducesResponseType(typeof(IEnumerable<EntityChangeLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EntityChangeLogDto>>> GetEntityAuditLogs(
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLogs = await _auditLogService.GetEntityLogsAsync(entityId, cancellationToken);
            return Ok(auditLogs.ToDto());
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
    [ProducesResponseType(typeof(IEnumerable<EntityChangeLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<EntityChangeLogDto>>> GetEntityTypeAuditLogs(
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
            return Ok(auditLogs.ToDto());
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
    [ProducesResponseType(typeof(IEnumerable<EntityChangeLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<EntityChangeLogDto>>> GetUserAuditLogs(
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
            return Ok(auditLogs.ToDto());
        }
        catch (Exception ex)
        {
            // Log the exception (you might want to use ILogger here)
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving user audit logs.", error = ex.Message });
        }
    }

    /// <summary>
    /// Exports audit logs in the specified format (SuperAdmin only).
    /// </summary>
    /// <param name="exportDto">Export parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exported audit logs file</returns>
    /// <response code="200">Returns the exported file</response>
    /// <response code="400">If export parameters are invalid</response>
    [HttpPost("export")]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportAuditLogs(
        [FromBody] AuditLogExportDto exportDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!new[] { "JSON", "CSV", "TXT" }.Contains(exportDto.Format.ToUpper()))
            {
                return BadRequest(new { message = "Invalid format. Supported formats: JSON, CSV, TXT" });
            }

            // Build query parameters for filtering
            var queryParameters = new AuditLogQueryParameters
            {
                Page = 1,
                PageSize = int.MaxValue // Export all matching records
            };

            // Apply filters
            if (exportDto.FromDate.HasValue)
            {
                queryParameters.FromDate = exportDto.FromDate.Value;
            }

            if (exportDto.ToDate.HasValue)
            {
                queryParameters.ToDate = exportDto.ToDate.Value;
            }

            if (!string.IsNullOrEmpty(exportDto.UserId?.ToString()))
            {
                queryParameters.ChangedBy = exportDto.UserId.ToString();
            }

            // Get the filtered audit logs
            var result = await _auditLogService.GetPagedLogsAsync(queryParameters, cancellationToken);
            var auditLogs = result.Items.ToDto();

            // Generate file content based on format
            byte[] fileContent;
            string fileName;
            string contentType;

            switch (exportDto.Format.ToUpper())
            {
                case "JSON":
                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(auditLogs, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    fileContent = System.Text.Encoding.UTF8.GetBytes(jsonContent);
                    fileName = $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                    contentType = "application/json";
                    break;

                case "CSV":
                    var csvContent = GenerateCsvContent(auditLogs);
                    fileContent = System.Text.Encoding.UTF8.GetBytes(csvContent);
                    fileName = $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                    contentType = "text/csv";
                    break;

                case "TXT":
                    var txtContent = GenerateTxtContent(auditLogs);
                    fileContent = System.Text.Encoding.UTF8.GetBytes(txtContent);
                    fileName = $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";
                    contentType = "text/plain";
                    break;

                default:
                    return BadRequest(new { message = "Unsupported format" });
            }

            return File(fileContent, contentType, fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while exporting audit logs.", error = ex.Message });
        }
    }

    private string GenerateCsvContent(IEnumerable<EntityChangeLogDto> auditLogs)
    {
        var csv = new System.Text.StringBuilder();

        // Add header
        csv.AppendLine("Id,EntityName,EntityId,PropertyName,OperationType,OldValue,NewValue,ChangedBy,ChangedAt,EntityDisplayName");

        // Add data rows
        foreach (var log in auditLogs)
        {
            csv.AppendLine($"{log.Id}," +
                          $"\"{log.EntityName}\"," +
                          $"{log.EntityId}," +
                          $"\"{log.PropertyName}\"," +
                          $"\"{log.OperationType}\"," +
                          $"\"{EscapeCsvValue(log.OldValue)}\"," +
                          $"\"{EscapeCsvValue(log.NewValue)}\"," +
                          $"\"{log.ChangedBy}\"," +
                          $"{log.ChangedAt:yyyy-MM-dd HH:mm:ss}," +
                          $"\"{log.EntityDisplayName}\"");
        }

        return csv.ToString();
    }

    private string GenerateTxtContent(IEnumerable<EntityChangeLogDto> auditLogs)
    {
        var txt = new System.Text.StringBuilder();

        txt.AppendLine("AUDIT LOG EXPORT");
        txt.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        txt.AppendLine(new string('=', 50));
        txt.AppendLine();

        foreach (var log in auditLogs)
        {
            txt.AppendLine($"ID: {log.Id}");
            txt.AppendLine($"Entity: {log.EntityName} ({log.EntityId})");
            txt.AppendLine($"Property: {log.PropertyName}");
            txt.AppendLine($"Operation: {log.OperationType}");
            txt.AppendLine($"Old Value: {log.OldValue ?? "null"}");
            txt.AppendLine($"New Value: {log.NewValue ?? "null"}");
            txt.AppendLine($"Changed By: {log.ChangedBy}");
            txt.AppendLine($"Changed At: {log.ChangedAt:yyyy-MM-dd HH:mm:ss} UTC");
            txt.AppendLine($"Display Name: {log.EntityDisplayName ?? "N/A"}");
            txt.AppendLine(new string('-', 30));
        }

        return txt.ToString();
    }

    private string EscapeCsvValue(string? value)
    {
        if (value == null) return "";
        return value.Replace("\"", "\"\"");
    }

    /// <summary>
    /// Searches audit trail entries with advanced filtering (SuperAdmin only).
    /// </summary>
    /// <param name="searchDto">Search criteria</param>
    /// <returns>Paginated audit trail results</returns>
    [HttpPost("audit-trail/search")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<PagedResult<AuditTrailResponseDto>>> SearchAuditTrail([FromBody] AuditTrailSearchDto searchDto)
    {
        try
        {
            var results = await _auditLogService.SearchAuditTrailAsync(searchDto);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error searching audit trail", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets audit trail statistics (SuperAdmin only).
    /// </summary>
    /// <returns>Audit trail statistics</returns>
    [HttpGet("audit-trail/statistics")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<AuditTrailStatisticsDto>> GetAuditTrailStatistics()
    {
        try
        {
            var statistics = await _auditLogService.GetAuditTrailStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error retrieving audit trail statistics", error = ex.Message });
        }
    }

    /// <summary>
    /// Exports data in various formats (SuperAdmin only).
    /// </summary>
    /// <param name="exportDto">Export request parameters</param>
    /// <returns>Export result with download information</returns>
    [HttpPost("export-advanced")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ExportResultDto>> ExportAdvanced([FromBody] ExportRequestDto exportDto)
    {
        try
        {
            var exportResult = await _auditLogService.ExportAdvancedAsync(exportDto);
            return Ok(exportResult);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error starting export", error = ex.Message });
        }
    }

    /// <summary>
    /// Gets the status of an export operation (SuperAdmin only).
    /// </summary>
    /// <param name="exportId">Export operation ID</param>
    /// <returns>Export status information</returns>
    [HttpGet("export/{exportId}/status")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ExportResultDto>> GetExportStatus(Guid exportId)
    {
        try
        {
            var exportResult = await _auditLogService.GetExportStatusAsync(exportId);
            
            if (exportResult == null)
            {
                return NotFound(new { message = "Export not found" });
            }

            return Ok(exportResult);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error retrieving export status", error = ex.Message });
        }
    }

    /// <summary>
    /// Downloads a completed export file (SuperAdmin only).
    /// </summary>
    /// <param name="exportId">Export operation ID</param>
    /// <returns>Export file for download</returns>
    [HttpGet("export/{exportId}/download")]
    [Authorize(Roles = "SuperAdmin")]
    public IActionResult DownloadExport(Guid exportId)
    {
        try
        {
            // In a real implementation, this would:
            // 1. Verify the export exists and is completed
            // 2. Check user permissions
            // 3. Return the actual file

            // For now, return a placeholder
            var content = "Export file content would be here";
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            var fileName = $"export_{exportId:N}.json";

            return File(bytes, "application/json", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error downloading export", error = ex.Message });
        }
    }
}