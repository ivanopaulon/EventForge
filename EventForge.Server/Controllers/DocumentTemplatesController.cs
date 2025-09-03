using EventForge.DTOs.Documents;
using EventForge.Server.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for document template management with multi-tenant support.
/// Provides CRUD operations for document templates within the authenticated user's tenant context.
/// </summary>
/// <remarks>
/// DEPRECATED: This controller is deprecated in favor of the unified DocumentsController.
/// Use the unified API at /api/v1/documents/templates/* instead of /api/v1/DocumentTemplates/*.
/// This controller will be removed in a future version.
/// </remarks>
[Route("api/v1/[controller]")]
[Authorize]
[Obsolete("This controller is deprecated. Use the unified DocumentsController at /api/v1/documents/templates/* instead. This controller will be removed in a future version.")]
public class DocumentTemplatesController : BaseApiController
{
    private readonly IDocumentTemplateService _documentTemplateService;
    private readonly ITenantContext _tenantContext;

    /// <summary>
    /// Initializes a new instance of the DocumentTemplatesController
    /// </summary>
    /// <param name="documentTemplateService">Document template service</param>
    /// <param name="tenantContext">Tenant context service</param>
    public DocumentTemplatesController(IDocumentTemplateService documentTemplateService, ITenantContext tenantContext)
    {
        _documentTemplateService = documentTemplateService ?? throw new ArgumentNullException(nameof(documentTemplateService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    /// <summary>
    /// Gets all document templates
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of document templates</returns>
    /// <response code="200">Returns the list of document templates</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DocumentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DocumentTemplateDto>>> GetDocumentTemplates(CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var templates = await _documentTemplateService.GetAllAsync(cancellationToken);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document templates.", ex);
        }
    }

    /// <summary>
    /// Gets public document templates available to all users
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of public document templates</returns>
    /// <response code="200">Returns the list of public document templates</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("public")]
    [ProducesResponseType(typeof(IEnumerable<DocumentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DocumentTemplateDto>>> GetPublicDocumentTemplates(CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var templates = await _documentTemplateService.GetPublicTemplatesAsync(cancellationToken);
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
    [HttpGet("by-document-type/{documentTypeId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DocumentTemplateDto>>> GetDocumentTemplatesByDocumentType(Guid documentTypeId, CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var templates = await _documentTemplateService.GetByDocumentTypeAsync(documentTypeId, cancellationToken);
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
    [HttpGet("by-category/{category}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DocumentTemplateDto>>> GetDocumentTemplatesByCategory(string category, CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        if (string.IsNullOrWhiteSpace(category))
            return CreateValidationProblemDetails("Category cannot be empty.");

        try
        {
            var templates = await _documentTemplateService.GetByCategoryAsync(category, cancellationToken);
            return Ok(templates);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving document templates by category.", ex);
        }
    }

    /// <summary>
    /// Gets a specific document template by ID
    /// </summary>
    /// <param name="id">Document template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document template details</returns>
    /// <response code="200">Returns the document template</response>
    /// <response code="404">If the document template is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentTemplateDto>> GetDocumentTemplate(Guid id, CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var template = await _documentTemplateService.GetByIdAsync(id, cancellationToken);
            if (template == null)
                return CreateNotFoundProblem($"Document template with ID {id} was not found.");

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
    [HttpPost]
    [ProducesResponseType(typeof(DocumentTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentTemplateDto>> CreateDocumentTemplate([FromBody] CreateDocumentTemplateDto createDto, CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            var template = await _documentTemplateService.CreateAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetDocumentTemplate),
                new { id = template.Id },
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
    /// <param name="id">Document template ID</param>
    /// <param name="updateDto">Document template update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated document template</returns>
    /// <response code="200">Document template updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the document template is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DocumentTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DocumentTemplateDto>> UpdateDocumentTemplate(Guid id, [FromBody] UpdateDocumentTemplateDto updateDto, CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        try
        {
            var currentUser = GetCurrentUser();
            var template = await _documentTemplateService.UpdateAsync(id, updateDto, currentUser, cancellationToken);

            if (template == null)
                return CreateNotFoundProblem($"Document template with ID {id} was not found.");

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
    /// <param name="id">Document template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Document template deleted successfully</response>
    /// <response code="404">If the document template is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteDocumentTemplate(Guid id, CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _documentTemplateService.DeleteAsync(id, currentUser, cancellationToken);

            if (!deleted)
                return CreateNotFoundProblem($"Document template with ID {id} was not found.");

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
    /// <param name="id">Document template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Usage updated successfully</response>
    /// <response code="404">If the document template is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpPatch("{id:guid}/usage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateTemplateUsage(Guid id, CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var updated = await _documentTemplateService.UpdateUsageAsync(id, currentUser, cancellationToken);

            if (!updated)
                return CreateNotFoundProblem($"Document template with ID {id} was not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating template usage.", ex);
        }
    }
}