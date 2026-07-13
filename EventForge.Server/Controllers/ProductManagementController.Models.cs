using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Common;
using EventForge.Server.Services.Documents;
using EventForge.Server.Services.Export;
using EventForge.Server.Services.PriceLists;
using EventForge.Server.Services.Products;
using EventForge.Server.Services.Promotions;
using EventForge.Server.Services.UnitOfMeasures;
using EventForge.Server.Services.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.PriceLists;
using Prym.DTOs.Products;
using Prym.DTOs.Promotions;
using Prym.DTOs.UnitOfMeasures;
using Prym.DTOs.Warehouse;


namespace EventForge.Server.Controllers;

public partial class ProductManagementController
{

    /// <summary>
    /// Gets all models with pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page, pageSize)</param>
    /// <param name="brandId">Optional brand ID to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of models</returns>
    /// <response code="200">Returns the paginated list of models</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("models")]
    [ProducesResponseType(typeof(PagedResult<ModelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ModelDto>>> GetModels(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        [FromQuery] Guid? brandId = null,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = brandId.HasValue
                ? await modelService.GetModelsByBrandIdAsync(brandId.Value, pagination, cancellationToken)
                : await modelService.GetModelsAsync(pagination, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving models.", ex);
        }
    }

    /// <summary>
    /// Gets a model by ID.
    /// </summary>
    /// <param name="id">Model ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Model information</returns>
    /// <response code="200">Returns the model</response>
    /// <response code="404">If the model is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("models/{id:guid}")]
    [ProducesResponseType(typeof(ModelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ModelDto>> GetModel(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var model = await modelService.GetModelByIdAsync(id, cancellationToken);
            if (model is null)
                return CreateNotFoundProblem($"Model with ID {id} not found.");

            return Ok(model);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the model.", ex);
        }
    }

    /// <summary>
    /// Creates a new model.
    /// </summary>
    /// <param name="createModelDto">Model creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created model information</returns>
    /// <response code="201">Model created successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("models")]
    [ProducesResponseType(typeof(ModelDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ModelDto>> CreateModel(
        [FromBody] CreateModelDto createModelDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var model = await modelService.CreateModelAsync(createModelDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetModel), new { id = model.Id }, model);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the model.", ex);
        }
    }

    /// <summary>
    /// Updates an existing model.
    /// </summary>
    /// <param name="id">Model ID</param>
    /// <param name="updateModelDto">Model update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated model information</returns>
    /// <response code="200">Model updated successfully</response>
    /// <response code="400">If the input data is invalid</response>
    /// <response code="404">If the model is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPut("models/{id:guid}")]
    [ProducesResponseType(typeof(ModelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ModelDto>> UpdateModel(
        Guid id,
        [FromBody] UpdateModelDto updateModelDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var model = await modelService.UpdateModelAsync(id, updateModelDto, currentUser, cancellationToken);
            if (model is null)
                return CreateNotFoundProblem($"Model with ID {id} not found.");

            return Ok(model);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the model.", ex);
        }
    }

    /// <summary>
    /// Deletes a model.
    /// </summary>
    /// <param name="id">Model ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">Model deleted successfully</response>
    /// <response code="404">If the model is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpDelete("models/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteModel(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = GetCurrentUser();
            var success = await modelService.DeleteModelAsync(id, currentUser, cancellationToken);
            if (!success)
                return CreateNotFoundProblem($"Model with ID {id} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the model.", ex);
        }
    }

}
