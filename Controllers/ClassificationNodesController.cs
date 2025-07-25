using EventForge.DTOs.Common;
using EventForge.Filters;
using EventForge.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Controllers;

/// <summary>
/// REST API controller for classification node management.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class ClassificationNodesController : BaseApiController
{
    private readonly IClassificationNodeService _classificationNodeService;

    public ClassificationNodesController(IClassificationNodeService classificationNodeService)
    {
        _classificationNodeService = classificationNodeService ?? throw new ArgumentNullException(nameof(classificationNodeService));
    }

    /// <summary>
    /// Gets all classification nodes with optional pagination and parent filtering.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="parentId">Optional parent ID to filter children</param>
    /// <param name="deleted">Filter for soft-deleted items: 'false' (default), 'true', or 'all'</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of classification nodes</returns>
    /// <response code="200">Returns the paginated list of classification nodes</response>
    /// <response code="400">If the query parameters are invalid</response>
    [HttpGet]
    [SoftDeleteFilter]
    [ProducesResponseType(typeof(PagedResult<ClassificationNodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ClassificationNodeDto>>> GetClassificationNodes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? parentId = null,
        [FromQuery] string deleted = "false",
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        if (page < 1)
        {
            ModelState.AddModelError(nameof(page), "Page number must be greater than 0.");
            return CreateValidationProblemDetails();
        }

        if (pageSize < 1 || pageSize > 100)
        {
            ModelState.AddModelError(nameof(pageSize), "Page size must be between 1 and 100.");
            return CreateValidationProblemDetails();
        }

        var result = await _classificationNodeService.GetClassificationNodesAsync(page, pageSize, parentId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets root classification nodes (nodes without parent).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of root classification nodes</returns>
    /// <response code="200">Returns the list of root classification nodes</response>
    [HttpGet("root")]
    [ProducesResponseType(typeof(IEnumerable<ClassificationNodeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ClassificationNodeDto>>> GetRootClassificationNodes(
        CancellationToken cancellationToken = default)
    {
        var result = await _classificationNodeService.GetRootClassificationNodesAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets children of a specific classification node.
    /// </summary>
    /// <param name="parentId">Parent classification node ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of child classification nodes</returns>
    /// <response code="200">Returns the list of child classification nodes</response>
    [HttpGet("{parentId:guid}/children")]
    [ProducesResponseType(typeof(IEnumerable<ClassificationNodeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ClassificationNodeDto>>> GetChildren(
        Guid parentId,
        CancellationToken cancellationToken = default)
    {
        var result = await _classificationNodeService.GetChildrenAsync(parentId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets a classification node by ID.
    /// </summary>
    /// <param name="id">Classification node ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Classification node information</returns>
    /// <response code="200">Returns the classification node</response>
    /// <response code="404">If the classification node is not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClassificationNodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassificationNodeDto>> GetClassificationNode(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var node = await _classificationNodeService.GetClassificationNodeByIdAsync(id, cancellationToken);

        if (node == null)
        {
            return CreateNotFoundProblem($"Classification node with ID {id} not found.");
        }

        return Ok(node);
    }

    /// <summary>
    /// Creates a new classification node.
    /// </summary>
    /// <param name="createDto">Classification node creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created classification node</returns>
    /// <response code="201">Returns the newly created classification node</response>
    /// <response code="400">If the classification node data is invalid</response>
    [HttpPost]
    [ProducesResponseType(typeof(ClassificationNodeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClassificationNodeDto>> CreateClassificationNode(
        [FromBody] CreateClassificationNodeDto createDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var currentUser = GetCurrentUser();
        var node = await _classificationNodeService.CreateClassificationNodeAsync(createDto, currentUser, cancellationToken);

        return CreatedAtAction(
            nameof(GetClassificationNode),
            new { id = node.Id },
            node);
    }

    /// <summary>
    /// Updates an existing classification node.
    /// </summary>
    /// <param name="id">Classification node ID</param>
    /// <param name="updateDto">Classification node update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated classification node</returns>
    /// <response code="200">Returns the updated classification node</response>
    /// <response code="400">If the classification node data is invalid</response>
    /// <response code="404">If the classification node is not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ClassificationNodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassificationNodeDto>> UpdateClassificationNode(
        Guid id,
        [FromBody] UpdateClassificationNodeDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        var currentUser = GetCurrentUser();
        var node = await _classificationNodeService.UpdateClassificationNodeAsync(id, updateDto, currentUser, cancellationToken);

        if (node == null)
        {
            return CreateNotFoundProblem($"Classification node with ID {id} not found.");
        }

        return Ok(node);
    }

    /// <summary>
    /// Deletes a classification node.
    /// </summary>
    /// <param name="id">Classification node ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">If the classification node was successfully deleted</response>
    /// <response code="404">If the classification node is not found</response>
    /// <response code="400">If the classification node cannot be deleted (has children)</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteClassificationNode(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var currentUser = GetCurrentUser();
        var result = await _classificationNodeService.DeleteClassificationNodeAsync(id, currentUser, Array.Empty<byte>(), cancellationToken);

        if (!result)
        {
            return CreateNotFoundProblem($"Classification node with ID {id} not found.");
        }

        return NoContent();
    }
}