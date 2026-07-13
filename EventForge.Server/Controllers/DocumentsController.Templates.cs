using EventForge.Server.Filters;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Documents;


namespace EventForge.Server.Controllers;

public partial class DocumentsController
{
    /// <summary>
    /// Gets all document templates
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document templates</returns>
    /// <response code="200">Returns the list of document templates</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(IEnumerable<DocumentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DocumentTemplateDto>>> GetTemplates(CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var templates = await documentFacade.GetAllTemplatesAsync(cancellationToken);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document templates.", ex);
        }
    }

    /// <summary>
    /// Gets public document templates available to all users.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of public document templates</returns>
    /// <response code="200">Returns the public document templates</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("templates/public")]
    [ProducesResponseType(typeof(IEnumerable<DocumentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<DocumentTemplateDto>>> GetPublicTemplates(
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var templates = await documentFacade.GetPublicTemplatesAsync(cancellationToken);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving public document templates.", ex);
        }
    }

    /// <summary>
    /// Gets document templates by document type
    /// </summary>
    /// <param name="documentTypeId">Document type ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document templates for the specified document type</returns>
    /// <response code="200">Returns the list of document templates</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("templates/by-document-type/{documentTypeId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DocumentTemplateDto>>> GetTemplatesByDocumentType(Guid documentTypeId, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var templates = await documentFacade.GetTemplatesByDocumentTypeAsync(documentTypeId, cancellationToken);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document templates by document type.", ex);
        }
    }

    /// <summary>
    /// Gets document templates by category
    /// </summary>
    /// <param name="category">Template category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document templates for the specified category</returns>
    /// <response code="200">Returns the list of document templates</response>
    /// <response code="400">If the category is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("templates/by-category/{category}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DocumentTemplateDto>>> GetTemplatesByCategory(string category, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        if (string.IsNullOrWhiteSpace(category))
            return CreateValidationProblemDetails("Category cannot be empty.");

        try
        {
            var templates = await documentFacade.GetTemplatesByCategoryAsync(category, cancellationToken);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document templates by category.", ex);
        }
    }

    /// <summary>
    /// Gets a document template by ID.
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document template</returns>
    /// <response code="200">Returns the document template</response>
    /// <response code="404">If the template is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("templates/{templateId:guid}")]
    [ProducesResponseType(typeof(DocumentTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DocumentTemplateDto>> GetTemplate(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var template = await documentFacade.GetTemplateByIdAsync(templateId, cancellationToken);

            if (template == null)
                return CreateNotFoundProblem($"Document template with ID {templateId} not found.");

            return Ok(template);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the document template.", ex);
        }
    }

    /// <summary>
    /// Creates a new document template
    /// </summary>
    /// <param name="createDto">Document template creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created document template</returns>
    /// <response code="201">Document template created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPost("templates")]
    [ProducesResponseType(typeof(DocumentTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentTemplateDto>> CreateTemplate([FromBody] CreateDocumentTemplateDto createDto, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            var template = await documentFacade.CreateTemplateAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetTemplate),
                new { templateId = template.Id },
                template);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the document template.", ex);
        }
    }

    /// <summary>
    /// Updates an existing document template
    /// </summary>
    /// <param name="templateId">Document template ID</param>
    /// <param name="updateDto">Document template update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document template</returns>
    /// <response code="200">Document template updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the document template is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPut("templates/{templateId:guid}")]
    [ProducesResponseType(typeof(DocumentTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentTemplateDto>> UpdateTemplate(Guid templateId, [FromBody] UpdateDocumentTemplateDto updateDto, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            var template = await documentFacade.UpdateTemplateAsync(templateId, updateDto, currentUser, cancellationToken);

            if (template == null)
                return CreateNotFoundProblem($"Document template with ID {templateId} was not found.");

            return Ok(template);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the document template.", ex);
        }
    }

    /// <summary>
    /// Deletes a document template (soft delete)
    /// </summary>
    /// <param name="templateId">Document template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Document template deleted successfully</response>
    /// <response code="404">If the document template is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpDelete("templates/{templateId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteTemplate(Guid templateId, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await documentFacade.DeleteTemplateAsync(templateId, currentUser, cancellationToken);

            if (!deleted)
                return CreateNotFoundProblem($"Document template with ID {templateId} was not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the document template.", ex);
        }
    }

    /// <summary>
    /// Updates template usage statistics
    /// </summary>
    /// <param name="templateId">Document template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Usage updated successfully</response>
    /// <response code="404">If the document template is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPatch("templates/{templateId:guid}/usage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateTemplateUsage(Guid templateId, CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var updated = await documentFacade.UpdateTemplateUsageAsync(templateId, currentUser, cancellationToken);

            if (!updated)
                return CreateNotFoundProblem($"Document template with ID {templateId} was not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating template usage.", ex);
        }
    }

    // Workflow endpoints
}
