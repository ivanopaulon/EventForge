using EventForge.Server.Filters;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Documents;


namespace EventForge.Server.Controllers;

public partial class DocumentsController
{

    /// <summary>
    /// Exports documents to various formats (PDF, Excel, HTML, CSV, JSON).
    /// Supports filtering by date range, document type, and status.
    /// </summary>
    /// <param name="request">Export request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export operation result with download information</returns>
    /// <response code="200">Export operation initiated successfully</response>
    /// <response code="400">Invalid export parameters</response>
    /// <response code="403">User doesn't have permission to export documents</response>
    [HttpPost("export")]
    [ProducesResponseType(typeof(DocumentExportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentExportResultDto>> ExportDocumentsAsync(
        [FromBody] DocumentExportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var exportService = HttpContext.RequestServices.GetRequiredService<IDocumentExportService>();

            var result = await exportService.ExportDocumentsAsync(request, currentUser, cancellationToken);

            logger.LogInformation(
                "Document export {ExportId} initiated by {User} with format {Format}",
                result.ExportId, currentUser, request.Format);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid export parameters");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while exporting documents.", ex);
        }
    }

    /// <summary>
    /// Gets the status of a document export operation.
    /// </summary>
    /// <param name="exportId">Export operation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export operation status</returns>
    /// <response code="200">Export status retrieved successfully</response>
    /// <response code="404">Export operation not found</response>
    [HttpGet("export/{exportId:guid}/status")]
    [ProducesResponseType(typeof(DocumentExportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentExportResultDto>> GetExportStatusAsync(
        [FromRoute] Guid exportId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var exportService = HttpContext.RequestServices.GetRequiredService<IDocumentExportService>();
            var result = await exportService.GetExportStatusAsync(exportId, cancellationToken);

            if (result == null)
            {
                return CreateNotFoundProblem($"Export operation {exportId} not found.");
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving export status.", ex);
        }
    }

}
