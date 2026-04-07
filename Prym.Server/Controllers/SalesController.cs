using Prym.DTOs.Sales;
using Prym.Server.Filters;
using Prym.Server.ModelBinders;
using Prym.Server.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Prym.Server.Controllers;

/// <summary>
/// REST API controller for sales session management with multi-tenant support.
/// Provides endpoints for creating and managing sales sessions, items, and payments.
/// </summary>
[Route("api/v1/sales")]
[Authorize]
[RequireLicenseFeature("SalesManagement")]
public class SalesController(
    ISaleSessionService saleSessionService,
    ITenantContext tenantContext,
    ILogger<SalesController> logger) : BaseApiController
{

    /// <summary>
    /// Creates a new sale session.
    /// </summary>
    /// <param name="createDto">Sale session creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created sale session</returns>
    /// <response code="201">Returns the newly created sale session</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(SaleSessionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SaleSessionDto>> CreateSession(
        [FromBody] CreateSaleSessionDto createDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var session = await saleSessionService.CreateSessionAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetSession),
                new { sessionId = session.Id },
                session);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the sale session.", ex);
        }
    }

    /// <summary>
    /// Gets a specific sale session by ID.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sale session details</returns>
    /// <response code="200">Returns the sale session</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the session is not found</response>
    [HttpGet("sessions/{sessionId:guid}")]
    [ProducesResponseType(typeof(SaleSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleSessionDto>> GetSession(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var session = await saleSessionService.GetSessionAsync(sessionId, cancellationToken);

            if (session is null)
                return CreateNotFoundProblem($"Sale session {sessionId} not found.");

            return Ok(session);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem($"An error occurred while retrieving the sale session.", ex);
        }
    }

    /// <summary>
    /// Updates a sale session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="updateDto">Sale session update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated sale session</returns>
    /// <response code="200">Returns the updated sale session</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the session is not found</response>
    [HttpPut("sessions/{sessionId:guid}")]
    [ProducesResponseType(typeof(SaleSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleSessionDto>> UpdateSession(
        Guid sessionId,
        [FromBody] UpdateSaleSessionDto updateDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var session = await saleSessionService.UpdateSessionAsync(sessionId, updateDto, currentUser, cancellationToken);

            if (session is null)
                return CreateNotFoundProblem($"Sale session {sessionId} not found.");

            return Ok(session);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the sale session.", ex);
        }
    }

    /// <summary>
    /// Deletes a sale session (soft delete).
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    /// <response code="204">If the session was successfully deleted</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the session is not found</response>
    [HttpDelete("sessions/{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSession(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var deleted = await saleSessionService.DeleteSessionAsync(sessionId, currentUser, cancellationToken);

            if (!deleted)
                return CreateNotFoundProblem($"Sale session {sessionId} not found.");

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the sale session.", ex);
        }
    }

    /// <summary>
    /// Gets all active sale sessions for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active sale sessions</returns>
    /// <response code="200">Returns the list of active sessions</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(List<SaleSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Obsolete("Use GetOpenSessions or GetPOSSessions with pagination instead")]
    public async Task<ActionResult<List<SaleSessionDto>>> GetActiveSessions(
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var sessions = await saleSessionService.GetActiveSessionsAsync(cancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving active sale sessions.", ex);
        }
    }

    /// <summary>
    /// Gets all sale sessions for a specific operator.
    /// </summary>
    /// <param name="operatorId">Operator ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of operator's sale sessions</returns>
    /// <response code="200">Returns the list of operator's sessions</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("sessions/operator/{operatorId:guid}")]
    [ProducesResponseType(typeof(List<SaleSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Obsolete("Use GetSessionsByOperator with pagination instead")]
    public async Task<ActionResult<List<SaleSessionDto>>> GetOperatorSessions(
        Guid operatorId,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var sessions = await saleSessionService.GetOperatorSessionsAsync(operatorId, cancellationToken);
#pragma warning restore CS0618 // Type or member is obsolete
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving operator sale sessions.", ex);
        }
    }

    /// <summary>
    /// Retrieves all POS sessions with pagination
    /// </summary>
    /// <param name="pagination">Pagination parameters. Max pageSize based on role: User=1000, Admin=5000, SuperAdmin=10000</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of POS sessions</returns>
    /// <response code="200">Successfully retrieved POS sessions with pagination metadata in headers</response>
    /// <response code="400">Invalid pagination parameters</response>
    [HttpGet("pos-sessions")]
    [ProducesResponseType(typeof(PagedResult<SaleSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<SaleSessionDto>>> GetPOSSessions(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await saleSessionService.GetPOSSessionsAsync(pagination, cancellationToken);
            SetPaginationHeaders(result, pagination);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving POS sessions.", ex);
        }
    }

    /// <summary>
    /// Retrieves POS sessions for specific operator
    /// </summary>
    /// <param name="operatorId">Operator ID</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("pos-sessions/operator/{operatorId}")]
    [ProducesResponseType(typeof(PagedResult<SaleSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SaleSessionDto>>> GetSessionsByOperator(
        Guid operatorId,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await saleSessionService.GetSessionsByOperatorAsync(operatorId, pagination, cancellationToken);
            SetPaginationHeaders(result, pagination);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving operator sale sessions.", ex);
        }
    }

    /// <summary>
    /// Retrieves POS sessions within date range
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date (optional, defaults to now)</param>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("pos-sessions/date-range")]
    [ProducesResponseType(typeof(PagedResult<SaleSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SaleSessionDto>>> GetSessionsByDate(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await saleSessionService.GetSessionsByDateAsync(startDate, endDate, pagination, cancellationToken);
            SetPaginationHeaders(result, pagination);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving sale sessions by date.", ex);
        }
    }

    /// <summary>
    /// Retrieves currently open POS sessions (real-time data)
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <response code="200">Successfully retrieved open sessions</response>
    [OutputCache(PolicyName = "RealTimeShortCache")]
    [HttpGet("pos-sessions/open")]
    [ProducesResponseType(typeof(PagedResult<SaleSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SaleSessionDto>>> GetOpenSessions(
        [FromQuery, ModelBinder(typeof(PaginationModelBinder))] PaginationParameters pagination,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var result = await saleSessionService.GetOpenSessionsAsync(pagination, cancellationToken);
            SetPaginationHeaders(result, pagination);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving open sessions.", ex);
        }
    }

    /// <summary>
    /// Adds an item to a sale session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="addItemDto">Item data to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated sale session</returns>
    /// <response code="200">Returns the updated sale session with the new item</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the session is not found</response>
    [HttpPost("sessions/{sessionId:guid}/items")]
    [ProducesResponseType(typeof(SaleSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleSessionDto>> AddItem(
        Guid sessionId,
        [FromBody] AddSaleItemDto addItemDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var session = await saleSessionService.AddItemAsync(sessionId, addItemDto, currentUser, cancellationToken);

            if (session is null)
                return CreateNotFoundProblem($"Sale session {sessionId} not found.");

            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while adding item to session {SessionId}. ProductId: {ProductId}",
                sessionId, addItemDto.ProductId);
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while adding the item.", ex);
        }
    }

    /// <summary>
    /// Updates an item in a sale session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="itemId">Item ID</param>
    /// <param name="updateItemDto">Item update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated sale session</returns>
    /// <response code="200">Returns the updated sale session</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the session or item is not found</response>
    [HttpPut("sessions/{sessionId:guid}/items/{itemId:guid}")]
    [ProducesResponseType(typeof(SaleSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleSessionDto>> UpdateItem(
        Guid sessionId,
        Guid itemId,
        [FromBody] UpdateSaleItemDto updateItemDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var session = await saleSessionService.UpdateItemAsync(sessionId, itemId, updateItemDto, currentUser, cancellationToken);

            if (session is null)
                return CreateNotFoundProblem($"Sale session {sessionId} not found.");

            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while updating item {ItemId} in session {SessionId}.", itemId, sessionId);
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the item.", ex);
        }
    }

    /// <summary>
    /// Removes an item from a sale session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="itemId">Item ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated sale session</returns>
    /// <response code="200">Returns the updated sale session without the removed item</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the session or item is not found</response>
    [HttpDelete("sessions/{sessionId:guid}/items/{itemId:guid}")]
    [ProducesResponseType(typeof(SaleSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleSessionDto>> RemoveItem(
        Guid sessionId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var session = await saleSessionService.RemoveItemAsync(sessionId, itemId, currentUser, cancellationToken);

            if (session is null)
                return CreateNotFoundProblem($"Sale session {sessionId} not found.");

            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while removing item {ItemId} from session {SessionId}.", itemId, sessionId);
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while removing the item.", ex);
        }
    }

    /// <summary>
    /// Adds a payment to a sale session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="addPaymentDto">Payment data to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated sale session</returns>
    /// <response code="200">Returns the updated sale session with the new payment</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the session is not found</response>
    [HttpPost("sessions/{sessionId:guid}/payments")]
    [ProducesResponseType(typeof(SaleSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleSessionDto>> AddPayment(
        Guid sessionId,
        [FromBody] AddSalePaymentDto addPaymentDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var session = await saleSessionService.AddPaymentAsync(sessionId, addPaymentDto, currentUser, cancellationToken);

            if (session is null)
                return CreateNotFoundProblem($"Sale session {sessionId} not found.");

            return Ok(session);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while adding the payment.", ex);
        }
    }

    /// <summary>
    /// Removes a payment from a sale session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="paymentId">Payment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated sale session</returns>
    /// <response code="200">Returns the updated sale session without the removed payment</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the session or payment is not found</response>
    [HttpDelete("sessions/{sessionId:guid}/payments/{paymentId:guid}")]
    [ProducesResponseType(typeof(SaleSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleSessionDto>> RemovePayment(
        Guid sessionId,
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var session = await saleSessionService.RemovePaymentAsync(sessionId, paymentId, currentUser, cancellationToken);

            if (session is null)
                return CreateNotFoundProblem($"Sale session {sessionId} not found.");

            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while removing payment {PaymentId} from session {SessionId}.", paymentId, sessionId);
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while removing the payment.", ex);
        }
    }

    /// <summary>
    /// Adds a note to a sale session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="addNoteDto">Note data to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated sale session</returns>
    /// <response code="200">Returns the updated sale session with the new note</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the session is not found</response>
    [HttpPost("sessions/{sessionId:guid}/notes")]
    [ProducesResponseType(typeof(SaleSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleSessionDto>> AddNote(
        Guid sessionId,
        [FromBody] AddSessionNoteDto addNoteDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var session = await saleSessionService.AddNoteAsync(sessionId, addNoteDto, currentUser, cancellationToken);

            if (session is null)
                return CreateNotFoundProblem($"Sale session {sessionId} not found.");

            return Ok(session);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while adding the note.", ex);
        }
    }

    /// <summary>
    /// Applies a global discount percentage to all session items.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="discountDto">Discount data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated sale session with applied discount</returns>
    /// <response code="200">Returns the updated sale session</response>
    /// <response code="400">If discount percentage is invalid</response>
    /// <response code="403">If user doesn't have access to current tenant</response>
    /// <response code="404">If session not found</response>
    [HttpPost("sessions/{sessionId:guid}/discount")]
    [ProducesResponseType(typeof(SaleSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleSessionDto>> ApplyGlobalDiscount(
        Guid sessionId,
        [FromBody] ApplyGlobalDiscountDto discountDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var session = await saleSessionService.ApplyGlobalDiscountAsync(sessionId, discountDto, currentUser, cancellationToken);

            if (session is null)
                return CreateNotFoundProblem($"Sale session {sessionId} not found.");

            return Ok(session);
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
            return CreateInternalServerErrorProblem("An error occurred while applying the discount.", ex);
        }
    }

    /// <summary>
    /// Calculates totals for a sale session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Sale session with recalculated totals</returns>
    /// <response code="200">Returns the sale session with updated totals</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the session is not found</response>
    [HttpPost("sessions/{sessionId:guid}/totals")]
    [ProducesResponseType(typeof(SaleSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleSessionDto>> CalculateTotals(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var session = await saleSessionService.CalculateTotalsAsync(sessionId, cancellationToken);

            if (session is null)
                return CreateNotFoundProblem($"Sale session {sessionId} not found.");

            return Ok(session);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while calculating totals.", ex);
        }
    }

    /// <summary>
    /// Closes a sale session and generates a document (if fully paid).
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Closed sale session</returns>
    /// <response code="200">Returns the closed sale session</response>
    /// <response code="400">If the session cannot be closed (e.g., not fully paid)</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the session is not found</response>
    [HttpPost("sessions/{sessionId:guid}/close")]
    [ProducesResponseType(typeof(SaleSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleSessionDto>> CloseSession(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var session = await saleSessionService.CloseSessionAsync(sessionId, currentUser, cancellationToken);

            if (session is null)
                return CreateNotFoundProblem($"Sale session {sessionId} not found.");

            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Cannot close sale session {SessionId}: {Message}", sessionId, ex.Message);
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while closing the sale session.", ex);
        }
    }

    /// <summary>
    /// Voids a closed receipt/sale session.
    /// </summary>
    /// <param name="sessionId">Sale session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated sale session</returns>
    /// <response code="200">Session voided successfully</response>
    /// <response code="400">If session cannot be voided</response>
    /// <response code="404">If session not found</response>
    [HttpPost("sessions/{sessionId:guid}/void")]
    [ProducesResponseType(typeof(SaleSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleSessionDto>> VoidSession(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";

            var voidedSession = await saleSessionService.VoidSessionAsync(sessionId, currentUser, cancellationToken);

            if (voidedSession is null)
                return CreateNotFoundProblem($"Sale session {sessionId} not found.");

            logger.LogInformation("Voided sale session {SessionId} by {User}", sessionId, currentUser);

            return Ok(voidedSession);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Cannot void sale session {SessionId}: {Message}", sessionId, ex.Message);
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while voiding the sale session.", ex);
        }
    }

    /// <summary>
    /// Splits a sale session into multiple child sessions.
    /// </summary>
    /// <param name="splitDto">Split session data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Split result with child sessions</returns>
    /// <response code="200">Session split successfully</response>
    /// <response code="400">If session cannot be split</response>
    /// <response code="404">If session not found</response>
    [HttpPost("sessions/split")]
    [ProducesResponseType(typeof(SplitResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SplitSession(
        [FromBody] SplitSessionDto splitDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var result = await saleSessionService.SplitSessionAsync(splitDto, currentUser, cancellationToken);

            if (result is null)
                return CreateNotFoundProblem("Sessione non trovata");

            logger.LogInformation("Split session {SessionId} into {Count} child sessions", splitDto.SessionId, result.ChildSessions.Count);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Cannot split session {SessionId}: {Message}", splitDto.SessionId, ex.Message);
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while splitting the sale session.", ex);
        }
    }

    /// <summary>
    /// Merges multiple sale sessions into one.
    /// </summary>
    /// <param name="mergeDto">Merge sessions data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Merged session</returns>
    /// <response code="200">Sessions merged successfully</response>
    /// <response code="400">If sessions cannot be merged</response>
    /// <response code="404">If one or more sessions not found</response>
    [HttpPost("sessions/merge")]
    [ProducesResponseType(typeof(SaleSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MergeSessions(
        [FromBody] MergeSessionsDto mergeDto,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var result = await saleSessionService.MergeSessionsAsync(mergeDto, currentUser, cancellationToken);

            if (result is null)
                return CreateNotFoundProblem("Una o più sessioni non trovate");

            logger.LogInformation("Merged {Count} sessions into session {SessionId}", mergeDto.SessionIds.Count, result.Id);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Cannot merge sessions: {Message}", ex.Message);
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while merging the sale sessions.", ex);
        }
    }

    /// <summary>
    /// Gets all child sessions of a parent session.
    /// </summary>
    /// <param name="parentSessionId">Parent session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of child sessions</returns>
    /// <response code="200">Returns the child sessions</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("sessions/{parentSessionId:guid}/children")]
    [ProducesResponseType(typeof(List<SaleSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetChildSessions(
        Guid parentSessionId,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var children = await saleSessionService.GetChildSessionsAsync(parentSessionId, cancellationToken);
            return Ok(children);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving child sessions.", ex);
        }
    }

    /// <summary>
    /// Checks if a session can be split.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Object indicating if session can be split</returns>
    /// <response code="200">Returns the result</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("sessions/{sessionId:guid}/can-split")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CanSplitSession(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var canSplit = await saleSessionService.CanSplitSessionAsync(sessionId, cancellationToken);
            return Ok(new { canSplit });
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while checking split capability.", ex);
        }
    }

    /// <summary>
    /// Checks if sessions can be merged.
    /// </summary>
    /// <param name="sessionIds">List of session IDs to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Object indicating if sessions can be merged</returns>
    /// <response code="200">Returns the result</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("sessions/can-merge")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CanMergeSessions(
        [FromQuery] List<Guid> sessionIds,
        CancellationToken cancellationToken = default)
    {
        if (await ValidateTenantAccessAsync(tenantContext) is { } tenantError) return tenantError;

        try
        {
            var canMerge = await saleSessionService.CanMergeSessionsAsync(sessionIds, cancellationToken);
            return Ok(new { canMerge });
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while checking merge capability.", ex);
        }
    }
}
