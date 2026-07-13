using EventForge.Server.Filters;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Documents;


namespace EventForge.Server.Controllers;

public partial class DocumentsController
{

    /// <summary>
    /// Adds a row to a document.
    /// </summary>
    /// <param name="createRowDto">Document row creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document row</returns>
    /// <response code="201">Returns the created document row</response>
    /// <response code="400">If the creation data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("rows")]
    [ProducesResponseType(typeof(DocumentRowDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentRowDto>> AddDocumentRow(
        [FromBody] CreateDocumentRowDto createRowDto,
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
            var currentUser = GetCurrentUser();
            var documentRow = await documentFacade.AddDocumentRowAsync(createRowDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(AddDocumentRow), new { id = documentRow.Id }, documentRow);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while adding document row.", ex);
        }
    }

    /// <summary>
    /// Updates an existing document row.
    /// </summary>
    /// <param name="rowId">Document row ID</param>
    /// <param name="updateRowDto">Document row update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document row</returns>
    /// <response code="200">Returns the updated document row</response>
    /// <response code="400">If the update data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the document row is not found</response>
    [HttpPut("rows/{rowId:guid}")]
    [ProducesResponseType(typeof(DocumentRowDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentRowDto>> UpdateDocumentRow(
        Guid rowId,
        [FromBody] UpdateDocumentRowDto updateRowDto,
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
            var currentUser = GetCurrentUser();
            var documentRow = await documentFacade.UpdateDocumentRowAsync(rowId, updateRowDto, currentUser, cancellationToken);

            if (documentRow == null)
            {
                return CreateNotFoundProblem($"Document row with ID {rowId} was not found.");
            }

            return Ok(documentRow);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem($"An error occurred while updating document row {rowId}.", ex);
        }
    }

    /// <summary>
    /// Deletes a document row.
    /// </summary>
    /// <param name="rowId">Document row ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">If the document row was successfully deleted</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the document row is not found</response>
    [HttpDelete("rows/{rowId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDocumentRow(
        Guid rowId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var success = await documentFacade.DeleteDocumentRowAsync(rowId, currentUser, cancellationToken);

            if (!success)
            {
                return CreateNotFoundProblem($"Document row with ID {rowId} was not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem($"An error occurred while deleting document row {rowId}.", ex);
        }
    }

}
