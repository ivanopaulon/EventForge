using EventForge.DTOs.Common;
using EventForge.DTOs.Store;
using EventForge.Server.ModelBinders;
using EventForge.Server.Services.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for fiscal drawer (cassetto fiscale) management.
/// </summary>
[Route("api/v1/fiscal-drawers")]
[Authorize]
public class FiscalDrawersController(
    IFiscalDrawerService fiscalDrawerService,
    ITenantContext tenantContext) : BaseApiController
{
    #region CRUD

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<FiscalDrawerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<FiscalDrawerDto>>> GetFiscalDrawers(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            var result = await fiscalDrawerService.GetFiscalDrawersAsync(pagination.Page, pagination.PageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving fiscal drawers.", ex);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FiscalDrawerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FiscalDrawerDto>> GetFiscalDrawer(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            var result = await fiscalDrawerService.GetFiscalDrawerByIdAsync(id, cancellationToken);
            if (result is null) return CreateNotFoundProblem($"Fiscal drawer {id} not found.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving fiscal drawer.", ex);
        }
    }

    [HttpGet("by-pos/{posId:guid}")]
    [ProducesResponseType(typeof(FiscalDrawerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FiscalDrawerDto>> GetFiscalDrawerByPos(Guid posId, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            var result = await fiscalDrawerService.GetFiscalDrawerByPosIdAsync(posId, cancellationToken);
            if (result is null) return CreateNotFoundProblem($"No fiscal drawer found for POS {posId}.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving fiscal drawer by POS.", ex);
        }
    }

    [HttpGet("by-operator/{operatorId:guid}")]
    [ProducesResponseType(typeof(FiscalDrawerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FiscalDrawerDto>> GetFiscalDrawerByOperator(Guid operatorId, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            var result = await fiscalDrawerService.GetFiscalDrawerByOperatorIdAsync(operatorId, cancellationToken);
            if (result is null) return CreateNotFoundProblem($"No fiscal drawer found for operator {operatorId}.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving fiscal drawer by operator.", ex);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(FiscalDrawerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FiscalDrawerDto>> CreateFiscalDrawer(
        [FromBody] CreateFiscalDrawerDto dto, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();
        try
        {
            var result = await fiscalDrawerService.CreateFiscalDrawerAsync(dto, GetCurrentUser(), cancellationToken);
            return CreatedAtAction(nameof(GetFiscalDrawer), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error creating fiscal drawer.", ex);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FiscalDrawerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FiscalDrawerDto>> UpdateFiscalDrawer(
        Guid id, [FromBody] UpdateFiscalDrawerDto dto, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();
        try
        {
            var result = await fiscalDrawerService.UpdateFiscalDrawerAsync(id, dto, GetCurrentUser(), cancellationToken);
            if (result is null) return CreateNotFoundProblem($"Fiscal drawer {id} not found.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error updating fiscal drawer.", ex);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFiscalDrawer(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            var deleted = await fiscalDrawerService.DeleteFiscalDrawerAsync(id, GetCurrentUser(), cancellationToken);
            if (!deleted) return CreateNotFoundProblem($"Fiscal drawer {id} not found.");
            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error deleting fiscal drawer.", ex);
        }
    }

    #endregion

    #region Sessions

    [HttpGet("{id:guid}/current-session")]
    [ProducesResponseType(typeof(FiscalDrawerSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FiscalDrawerSessionDto>> GetCurrentSession(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            var result = await fiscalDrawerService.GetCurrentSessionAsync(id, cancellationToken);
            if (result is null) return CreateNotFoundProblem("No open session found.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving current session.", ex);
        }
    }

    [HttpGet("{id:guid}/sessions")]
    [ProducesResponseType(typeof(PagedResult<FiscalDrawerSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<FiscalDrawerSessionDto>>> GetSessions(
        Guid id,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            var result = await fiscalDrawerService.GetSessionsAsync(id, pagination.Page, pagination.PageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving sessions.", ex);
        }
    }

    [HttpPost("{id:guid}/open-session")]
    [ProducesResponseType(typeof(FiscalDrawerSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FiscalDrawerSessionDto>> OpenSession(
        Guid id, [FromBody] OpenFiscalDrawerSessionDto dto, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();
        try
        {
            var result = await fiscalDrawerService.OpenSessionAsync(id, dto, GetCurrentUser(), cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return CreateConflictProblem(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error opening session.", ex);
        }
    }

    [HttpPost("{id:guid}/close-session")]
    [ProducesResponseType(typeof(FiscalDrawerSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FiscalDrawerSessionDto>> CloseSession(
        Guid id, [FromBody] CloseFiscalDrawerSessionDto dto, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();
        try
        {
            var result = await fiscalDrawerService.CloseSessionAsync(id, dto, GetCurrentUser(), cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return CreateConflictProblem(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error closing session.", ex);
        }
    }

    #endregion

    #region Transactions

    [HttpGet("{id:guid}/transactions")]
    [ProducesResponseType(typeof(PagedResult<FiscalDrawerTransactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<FiscalDrawerTransactionDto>>> GetTransactions(
        Guid id,
        [FromQuery] Guid? sessionId = null,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination = default!,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            var result = await fiscalDrawerService.GetTransactionsAsync(id, sessionId, pagination?.Page ?? 1, pagination?.PageSize ?? 50, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving transactions.", ex);
        }
    }

    [HttpPost("{id:guid}/transactions")]
    [ProducesResponseType(typeof(FiscalDrawerTransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FiscalDrawerTransactionDto>> CreateTransaction(
        Guid id, [FromBody] CreateFiscalDrawerTransactionDto dto, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();
        try
        {
            var result = await fiscalDrawerService.CreateTransactionAsync(id, dto, GetCurrentUser(), cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (KeyNotFoundException ex)
        {
            return CreateNotFoundProblem(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error creating transaction.", ex);
        }
    }

    #endregion

    #region Cash Denominations

    [HttpGet("{id:guid}/denominations")]
    [ProducesResponseType(typeof(List<CashDenominationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CashDenominationDto>>> GetDenominations(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            var result = await fiscalDrawerService.GetCashDenominationsAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving denominations.", ex);
        }
    }

    [HttpPost("{id:guid}/denominations/initialize")]
    [ProducesResponseType(typeof(List<CashDenominationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CashDenominationDto>>> InitializeDenominations(
        Guid id, [FromQuery] string currencyCode = "EUR", CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            var result = await fiscalDrawerService.InitializeDenominationsAsync(id, currencyCode, GetCurrentUser(), cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error initializing denominations.", ex);
        }
    }

    [HttpPut("denominations/{denominationId:guid}")]
    [ProducesResponseType(typeof(CashDenominationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CashDenominationDto>> UpdateDenomination(
        Guid denominationId, [FromBody] UpdateCashDenominationDto dto, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();
        try
        {
            var result = await fiscalDrawerService.UpdateDenominationQuantityAsync(denominationId, dto, GetCurrentUser(), cancellationToken);
            if (result is null) return CreateNotFoundProblem($"Denomination {denominationId} not found.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error updating denomination.", ex);
        }
    }

    #endregion

    #region Change Calculation & Summary

    [HttpPost("{id:guid}/calculate-change")]
    [ProducesResponseType(typeof(CalculateChangeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CalculateChangeResponseDto>> CalculateChange(
        Guid id, [FromBody] CalculateChangeRequestDto request, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        if (!ModelState.IsValid) return CreateValidationProblemDetails();
        try
        {
            var result = await fiscalDrawerService.CalculateChangeAsync(id, request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error calculating change.", ex);
        }
    }

    [HttpGet("{id:guid}/summary")]
    [ProducesResponseType(typeof(FiscalDrawerSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FiscalDrawerSummaryDto>> GetDrawerSummary(Guid id, CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            var result = await fiscalDrawerService.GetDrawerSummaryAsync(id, cancellationToken);
            if (result is null) return CreateNotFoundProblem($"Fiscal drawer {id} not found.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving fiscal drawer summary.", ex);
        }
    }

    [HttpGet("sales-dashboard")]
    [ProducesResponseType(typeof(SalesDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SalesDashboardDto>> GetSalesDashboard(CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } err) return err;
        try
        {
            var result = await fiscalDrawerService.GetSalesDashboardAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving sales dashboard.", ex);
        }
    }

    #endregion
}
