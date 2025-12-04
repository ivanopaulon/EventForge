using EventForge.DTOs.Sales;
using EventForge.Server.Filters;
using EventForge.Server.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for sales session management with multi-tenant support.
/// Provides endpoints for creating and managing sales sessions, items, and payments.
/// </summary>
[Route("api/v1/sales")]
[Authorize]
[RequireLicenseFeature("SalesManagement")]
public class SalesController : BaseApiController
{
    private readonly ISaleSessionService _saleSessionService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<SalesController> _logger;

    public SalesController(
        ISaleSessionService saleSessionService,
        ITenantContext tenantContext,
        ILogger<SalesController> logger)
    {
        _saleSessionService = saleSessionService ?? throw new ArgumentNullException(nameof(saleSessionService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
            return BadRequest(ModelState);

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var session = await _saleSessionService.CreateSessionAsync(createDto, currentUser, cancellationToken);

            return CreatedAtAction(
                nameof(GetSession),
                new { sessionId = session.Id },
                session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating sale session.");
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var session = await _saleSessionService.GetSessionAsync(sessionId, cancellationToken);

            if (session == null)
                return NotFound(new { message = $"Sale session {sessionId} not found." });

            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving sale session {SessionId}.", sessionId);
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
            return BadRequest(ModelState);

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var session = await _saleSessionService.UpdateSessionAsync(sessionId, updateDto, currentUser, cancellationToken);

            if (session == null)
                return NotFound(new { message = $"Sale session {sessionId} not found." });

            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating sale session {SessionId}.", sessionId);
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var deleted = await _saleSessionService.DeleteSessionAsync(sessionId, currentUser, cancellationToken);

            if (!deleted)
                return NotFound(new { message = $"Sale session {sessionId} not found." });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting sale session {SessionId}.", sessionId);
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
    public async Task<ActionResult<List<SaleSessionDto>>> GetActiveSessions(
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var sessions = await _saleSessionService.GetActiveSessionsAsync(cancellationToken);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving active sale sessions.");
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
    public async Task<ActionResult<List<SaleSessionDto>>> GetOperatorSessions(
        Guid operatorId,
        CancellationToken cancellationToken = default)
    {
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var sessions = await _saleSessionService.GetOperatorSessionsAsync(operatorId, cancellationToken);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving sale sessions for operator {OperatorId}.", operatorId);
            return CreateInternalServerErrorProblem("An error occurred while retrieving operator sale sessions.", ex);
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
            return BadRequest(ModelState);

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var session = await _saleSessionService.AddItemAsync(sessionId, addItemDto, currentUser, cancellationToken);

            if (session == null)
                return NotFound(new { message = $"Sale session {sessionId} not found." });

            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while adding item to session {SessionId}.", sessionId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while adding item to sale session {SessionId}.", sessionId);
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
            return BadRequest(ModelState);

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var session = await _saleSessionService.UpdateItemAsync(sessionId, itemId, updateItemDto, currentUser, cancellationToken);

            if (session == null)
                return NotFound(new { message = $"Sale session {sessionId} not found." });

            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while updating item {ItemId} in session {SessionId}.", itemId, sessionId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating item {ItemId} in sale session {SessionId}.", itemId, sessionId);
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var session = await _saleSessionService.RemoveItemAsync(sessionId, itemId, currentUser, cancellationToken);

            if (session == null)
                return NotFound(new { message = $"Sale session {sessionId} not found." });

            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while removing item {ItemId} from session {SessionId}.", itemId, sessionId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while removing item {ItemId} from sale session {SessionId}.", itemId, sessionId);
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
            return BadRequest(ModelState);

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var session = await _saleSessionService.AddPaymentAsync(sessionId, addPaymentDto, currentUser, cancellationToken);

            if (session == null)
                return NotFound(new { message = $"Sale session {sessionId} not found." });

            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while adding payment to sale session {SessionId}.", sessionId);
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var session = await _saleSessionService.RemovePaymentAsync(sessionId, paymentId, currentUser, cancellationToken);

            if (session == null)
                return NotFound(new { message = $"Sale session {sessionId} not found." });

            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while removing payment {PaymentId} from session {SessionId}.", paymentId, sessionId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while removing payment {PaymentId} from sale session {SessionId}.", paymentId, sessionId);
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
            return BadRequest(ModelState);

        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var session = await _saleSessionService.AddNoteAsync(sessionId, addNoteDto, currentUser, cancellationToken);

            if (session == null)
                return NotFound(new { message = $"Sale session {sessionId} not found." });

            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while adding note to sale session {SessionId}.", sessionId);
            return CreateInternalServerErrorProblem("An error occurred while adding the note.", ex);
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var session = await _saleSessionService.CalculateTotalsAsync(sessionId, cancellationToken);

            if (session == null)
                return NotFound(new { message = $"Sale session {sessionId} not found." });

            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while calculating totals for sale session {SessionId}.", sessionId);
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            var session = await _saleSessionService.CloseSessionAsync(sessionId, currentUser, cancellationToken);

            if (session == null)
                return NotFound(new { message = $"Sale session {sessionId} not found." });

            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot close sale session {SessionId}: {Message}", sessionId, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while closing sale session {SessionId}.", sessionId);
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
        var tenantError = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantError != null) return tenantError;

        try
        {
            var currentUser = User.Identity?.Name ?? "Unknown";
            
            var voidedSession = await _saleSessionService.VoidSessionAsync(sessionId, currentUser, cancellationToken);

            if (voidedSession == null)
                return NotFound(new { message = $"Sale session {sessionId} not found." });

            _logger.LogInformation("Voided sale session {SessionId} by {User}", sessionId, currentUser);

            return Ok(voidedSession);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot void sale session {SessionId}: {Message}", sessionId, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while voiding sale session {SessionId}.", sessionId);
            return CreateInternalServerErrorProblem("An error occurred while voiding the sale session.", ex);
        }
    }
}
