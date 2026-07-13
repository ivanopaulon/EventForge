using EventForge.Server.Filters;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Documents;
using Prym.DTOs.Warehouse;


namespace EventForge.Server.Controllers;

public partial class WarehouseManagementController
{

    /// <summary>
    /// Gets all lots with filtering and pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="productId">Optional product ID filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="expiringSoon">Optional filter for lots expiring soon</param>
    /// <param name="recent">Optional filter to return only recently created lots</param>
    /// <param name="searchTerm">Optional search term to filter lots by code or description</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of lots</returns>
    /// <response code="200">Returns the paginated list of lots</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("lots")]
    [ProducesResponseType(typeof(PagedResult<LotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<LotDto>>> GetLots(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        [FromQuery] Guid? productId = null,
        [FromQuery] string? status = null,
        [FromQuery] bool? expiringSoon = null,
        [FromQuery] bool? recent = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.GetLotsAsync(pagination, productId, status, expiringSoon, recent, searchTerm, cancellationToken);

            SetPaginationHeaders(result, pagination);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving lots.", ex);
        }
    }

    /// <summary>
    /// Gets a specific lot by ID.
    /// </summary>
    /// <param name="id">Lot ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lot details</returns>
    /// <response code="200">Returns the lot details</response>
    /// <response code="404">If the lot is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("lots/{id:guid}")]
    [ProducesResponseType(typeof(LotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LotDto>> GetLot(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.GetLotByIdAsync(id, cancellationToken);
            return result is not null ? Ok(result) : NotFound();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the lot.", ex);
        }
    }

    [HttpGet("lots/{id:guid}/history")]
    [ProducesResponseType(typeof(IEnumerable<StockMovementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLotHistory(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.GetLotHistoryAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the lot history.", ex);
        }
    }

    /// <summary>
    /// Gets a specific lot by code.
    /// </summary>
    /// <param name="code">Lot code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lot details</returns>
    /// <response code="200">Returns the lot details</response>
    /// <response code="404">If the lot is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("lots/code/{code}")]
    [ProducesResponseType(typeof(LotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LotDto>> GetLotByCode(string code, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.GetLotByCodeAsync(code, cancellationToken);
            return result is not null ? Ok(result) : NotFound();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the lot.", ex);
        }
    }

    /// <summary>
    /// Gets lots that are expiring within the specified number of days.
    /// </summary>
    /// <param name="daysAhead">Number of days to look ahead (default: 30)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of expiring lots</returns>
    /// <response code="200">Returns the list of expiring lots</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("lots/expiring")]
    [ProducesResponseType(typeof(IEnumerable<LotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<LotDto>>> GetExpiringLots(
        [FromQuery] int daysAhead = 30,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.GetExpiringLotsAsync(daysAhead, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving expiring lots.", ex);
        }
    }

    /// <summary>
    /// Creates a new lot.
    /// </summary>
    /// <param name="createLotDto">Lot creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created lot</returns>
    /// <response code="201">Returns the newly created lot</response>
    /// <response code="400">If the lot data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="409">If a lot with the same code already exists</response>
    [HttpPost("lots")]
    [ProducesResponseType(typeof(LotDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LotDto>> CreateLot(
        [FromBody] CreateLotDto createLotDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.CreateLotAsync(createLotDto, GetCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetLot), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return CreateConflictProblem(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the lot.", ex);
        }
    }

    /// <summary>
    /// Updates an existing lot.
    /// </summary>
    /// <param name="id">Lot ID</param>
    /// <param name="updateLotDto">Lot update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated lot</returns>
    /// <response code="200">Returns the updated lot</response>
    /// <response code="400">If the lot data is invalid</response>
    /// <response code="404">If the lot is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="409">If a lot with the same code already exists</response>
    [HttpPut("lots/{id:guid}")]
    [ProducesResponseType(typeof(LotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LotDto>> UpdateLot(
        Guid id,
        [FromBody] UpdateLotDto updateLotDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.UpdateLotAsync(id, updateLotDto, GetCurrentUser(), cancellationToken);
            return result is not null ? Ok(result) : NotFound();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return CreateConflictProblem(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the lot.", ex);
        }
    }

    /// <summary>
    /// Deletes a lot.
    /// </summary>
    /// <param name="id">Lot ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="204">If the lot was successfully deleted</response>
    /// <response code="404">If the lot is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="409">If the lot cannot be deleted due to dependencies</response>
    [HttpDelete("lots/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteLot(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.DeleteLotAsync(id, GetCurrentUser(), cancellationToken);
            return result ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return CreateConflictProblem(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the lot.", ex);
        }
    }

    /// <summary>
    /// Updates the quality status of a lot.
    /// </summary>
    /// <param name="id">Lot ID</param>
    /// <param name="qualityStatus">New quality status</param>
    /// <param name="notes">Optional notes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="200">If the quality status was successfully updated</response>
    /// <response code="400">If the quality status is invalid</response>
    /// <response code="404">If the lot is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPatch("lots/{id:guid}/quality-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateLotQualityStatus(
        Guid id,
        [FromQuery] string qualityStatus,
        [FromQuery] string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(qualityStatus))
        {
            return CreateValidationProblemDetails();
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.UpdateQualityStatusAsync(id, qualityStatus, GetCurrentUser(), notes, cancellationToken);
            return result ? Ok() : NotFound();
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error occurred during request processing");
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the lot quality status.", ex);
        }
    }

    /// <summary>
    /// Blocks a lot (prevents further operations).
    /// </summary>
    /// <param name="id">Lot ID</param>
    /// <param name="reason">Reason for blocking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="200">If the lot was successfully blocked</response>
    /// <response code="400">If the reason is not provided</response>
    /// <response code="404">If the lot is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("lots/{id:guid}/block")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> BlockLot(
        Guid id,
        [FromQuery] string reason,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return CreateValidationProblemDetails("Reason is required for blocking a lot.");
        }

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.BlockLotAsync(id, reason, GetCurrentUser(), cancellationToken);
            return result ? Ok() : NotFound();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while blocking the lot.", ex);
        }
    }

    /// <summary>
    /// Unblocks a lot (allows further operations).
    /// </summary>
    /// <param name="id">Lot ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="200">If the lot was successfully unblocked</response>
    /// <response code="404">If the lot is not found</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("lots/{id:guid}/unblock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnblockLot(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await warehouseFacade.UnblockLotAsync(id, GetCurrentUser(), cancellationToken);
            return result ? Ok() : NotFound();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while unblocking the lot.", ex);
        }
    }

}
