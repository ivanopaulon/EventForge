using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Store;


namespace EventForge.Server.Controllers;

public partial class StoreUsersController
{

    /// <summary>
    /// Gets all store POS terminals with optional pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page and pageSize are automatically capped to configured limits)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of store POS terminals</returns>
    /// <response code="200">Returns the paginated list of store POS terminals</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("pos")]
    [ProducesResponseType(typeof(PagedResult<StorePosDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<StorePosDto>>> GetStorePoses(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await storeUserService.GetStorePosesAsync(pagination.Page, pagination.PageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving store POS terminals.", ex);
        }
    }

    /// <summary>
    /// Gets a store POS by ID.
    /// </summary>
    /// <param name="id">Store POS ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Store POS details</returns>
    /// <response code="200">Returns the store POS</response>
    /// <response code="404">If the store POS is not found</response>
    [HttpGet("pos/{id:guid}")]
    [ProducesResponseType(typeof(StorePosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorePosDto>> GetStorePos(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var storePos = await storeUserService.GetStorePosByIdAsync(id, cancellationToken);

            if (storePos is null)
            {
                return CreateNotFoundProblem($"Store POS with ID {id} not found.");
            }

            return Ok(storePos);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the store POS.", ex);
        }
    }

    /// <summary>
    /// Creates a new store POS.
    /// </summary>
    /// <param name="createStorePosDto">Store POS creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created store POS</returns>
    /// <response code="201">Returns the newly created store POS</response>
    /// <response code="400">If the store POS data is invalid</response>
    [HttpPost("pos")]
    [Authorize(Policy = "RequireStoreConfig")]
    [ProducesResponseType(typeof(StorePosDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StorePosDto>> CreateStorePos(CreateStorePosDto createStorePosDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var currentUser = GetCurrentUser();
            var storePos = await storeUserService.CreateStorePosAsync(createStorePosDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetStorePos), new { id = storePos.Id }, storePos);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the store POS.", ex);
        }
    }

    /// <summary>
    /// Updates an existing store POS.
    /// </summary>
    /// <param name="id">Store POS ID</param>
    /// <param name="updateStorePosDto">Store POS update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated store POS</returns>
    /// <response code="200">Returns the updated store POS</response>
    /// <response code="400">If the store POS data is invalid</response>
    /// <response code="404">If the store POS is not found</response>
    [HttpPut("pos/{id:guid}")]
    [Authorize(Policy = "RequireStoreConfig")]
    [ProducesResponseType(typeof(StorePosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StorePosDto>> UpdateStorePos(Guid id, UpdateStorePosDto updateStorePosDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var currentUser = GetCurrentUser();
            var storePos = await storeUserService.UpdateStorePosAsync(id, updateStorePosDto, currentUser, cancellationToken);

            if (storePos is null)
            {
                return CreateNotFoundProblem($"Store POS with ID {id} not found.");
            }

            return Ok(storePos);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the store POS.", ex);
        }
    }

    /// <summary>
    /// Deletes a store POS (soft delete).
    /// </summary>
    /// <param name="id">Store POS ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Store POS deleted successfully</response>
    /// <response code="404">If the store POS is not found</response>
    [HttpDelete("pos/{id:guid}")]
    [Authorize(Policy = "RequireStoreConfig")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStorePos(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await storeUserService.DeleteStorePosAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Store POS with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the store POS.", ex);
        }
    }

}
