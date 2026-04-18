using Prym.DTOs.Store;
using EventForge.Server.Services.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for cashier shift management.
/// Read operations are available to all authenticated users.
/// Write operations require StoreConfig role (Admin, Manager, StoreManager, SuperAdmin).
/// </summary>
[Route("api/v1/shifts")]
[Authorize]
public class ShiftsController(IShiftService shiftService, ITenantContext tenantContext) : BaseApiController
{
    /// <summary>
    /// Gets all shifts within the specified date range for the current tenant.
    /// </summary>
    /// <param name="from">Start date (inclusive), format YYYY-MM-DD</param>
    /// <param name="to">End date (inclusive), format YYYY-MM-DD</param>
    /// <param name="ct">Cancellation token</param>
    [HttpGet]
    [ProducesResponseType(typeof(List<CashierShiftDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<CashierShiftDto>>> GetShifts(
        [FromQuery] string from,
        [FromQuery] string to,
        CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        if (!DateOnly.TryParse(from, out var fromDate) || !DateOnly.TryParse(to, out var toDate))
            return CreateValidationProblemDetails("Parameters 'from' and 'to' must be valid dates in YYYY-MM-DD format.");

        if (toDate < fromDate)
            return CreateValidationProblemDetails("'to' must be greater than or equal to 'from'.");

        try
        {
            var result = await shiftService.GetShiftsAsync(fromDate, toDate, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving shifts.", ex);
        }
    }

    /// <summary>
    /// Gets a single shift by ID.
    /// </summary>
    /// <param name="id">Shift ID</param>
    /// <param name="ct">Cancellation token</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CashierShiftDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CashierShiftDto>> GetShift(Guid id, CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;
        try
        {
            var result = await shiftService.GetShiftByIdAsync(id, ct);
            if (result is null) return CreateNotFoundProblem($"Shift {id} not found.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the shift.", ex);
        }
    }

    /// <summary>
    /// Creates a new cashier shift.
    /// </summary>
    /// <param name="dto">Shift creation data</param>
    /// <param name="ct">Cancellation token</param>
    [HttpPost]
    [Authorize(Policy = "RequireStoreConfig")]
    [ProducesResponseType(typeof(CashierShiftDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CashierShiftDto>> CreateShift([FromBody] CreateCashierShiftDto dto, CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();

        try
        {
            var result = await shiftService.CreateShiftAsync(dto, GetCurrentUser(), ct);
            return CreatedAtAction(nameof(GetShift), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the shift.", ex);
        }
    }

    /// <summary>
    /// Updates an existing cashier shift.
    /// </summary>
    /// <param name="id">Shift ID</param>
    /// <param name="dto">Shift update data</param>
    /// <param name="ct">Cancellation token</param>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireStoreConfig")]
    [ProducesResponseType(typeof(CashierShiftDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CashierShiftDto>> UpdateShift(Guid id, [FromBody] UpdateCashierShiftDto dto, CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();

        try
        {
            var result = await shiftService.UpdateShiftAsync(id, dto, GetCurrentUser(), ct);
            if (result is null) return CreateNotFoundProblem($"Shift {id} not found.");
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the shift.", ex);
        }
    }

    /// <summary>
    /// Soft-deletes a cashier shift.
    /// </summary>
    /// <param name="id">Shift ID</param>
    /// <param name="ct">Cancellation token</param>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireStoreConfig")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteShift(Guid id, CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;
        try
        {
            var deleted = await shiftService.DeleteShiftAsync(id, GetCurrentUser(), ct);
            if (!deleted) return CreateNotFoundProblem($"Shift {id} not found.");
            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the shift.", ex);
        }
    }

    /// <summary>
    /// Gets all shifts for a specific operator within the specified date range.
    /// </summary>
    /// <param name="storeUserId">Store user (operator) ID</param>
    /// <param name="from">Start date (inclusive), format YYYY-MM-DD</param>
    /// <param name="to">End date (inclusive), format YYYY-MM-DD</param>
    /// <param name="ct">Cancellation token</param>
    [HttpGet("operator/{storeUserId:guid}")]
    [ProducesResponseType(typeof(List<CashierShiftDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<CashierShiftDto>>> GetShiftsByOperator(
        Guid storeUserId,
        [FromQuery] string from,
        [FromQuery] string to,
        CancellationToken ct = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        if (!DateOnly.TryParse(from, out var fromDate) || !DateOnly.TryParse(to, out var toDate))
            return CreateValidationProblemDetails("Parameters 'from' and 'to' must be valid dates in YYYY-MM-DD format.");

        try
        {
            var result = await shiftService.GetShiftsByOperatorAsync(storeUserId, fromDate, toDate, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving shifts for the operator.", ex);
        }
    }
}
