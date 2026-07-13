using EventForge.Server.Filters;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Documents;


namespace EventForge.Server.Controllers;

public partial class DocumentsController
{

    /// <summary>
    /// Change document status with validation
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(DocumentHeaderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeDocumentStatusAsync(
        Guid id,
        [FromBody] ChangeDocumentStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var result = await documentFacade.ChangeStatusAsync(
                id,
                dto.NewStatus,
                dto.Reason,
                cancellationToken);

            if (result == null)
            {
                return CreateNotFoundProblem($"Document with ID {id} was not found.");
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while changing document status.", ex);
        }
    }

    /// <summary>
    /// Get document status history
    /// </summary>
    [HttpGet("{id:guid}/status/history")]
    [ProducesResponseType(typeof(List<DocumentStatusHistoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDocumentStatusHistoryAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var history = await documentFacade.GetStatusHistoryAsync(id, cancellationToken);
            return Ok(history);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document status history.", ex);
        }
    }

    /// <summary>
    /// Get available status transitions
    /// </summary>
    [HttpGet("{id:guid}/status/available-transitions")]
    [ProducesResponseType(typeof(List<DocumentStatus>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableTransitionsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var transitions = await documentFacade.GetAvailableTransitionsAsync(id, cancellationToken);
            return Ok(transitions);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving available transitions.", ex);
        }
    }

}
