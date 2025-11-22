using EventForge.DTOs.Warehouse;
using EventForge.Server.Filters;
using EventForge.Server.Services.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for transfer order management.
/// Provides endpoints for creating, shipping, receiving, and managing stock transfer orders between warehouses.
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
[Authorize]
[RequireLicenseFeature("ProductManagement")]
public class TransferOrderController : BaseApiController
{
    private readonly ITransferOrderService _transferOrderService;
    private readonly ILogger<TransferOrderController> _logger;

    public TransferOrderController(
        ITransferOrderService transferOrderService,
        ILogger<TransferOrderController> logger)
    {
        _transferOrderService = transferOrderService ?? throw new ArgumentNullException(nameof(transferOrderService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a paginated list of transfer orders with optional filters.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="sourceWarehouseId">Filter by source warehouse.</param>
    /// <param name="destinationWarehouseId">Filter by destination warehouse.</param>
    /// <param name="status">Filter by status.</param>
    /// <param name="searchTerm">Search term for number or shipping reference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of transfer orders.</returns>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TransferOrderDto>>> GetTransferOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? sourceWarehouseId = null,
        [FromQuery] Guid? destinationWarehouseId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _transferOrderService.GetTransferOrdersAsync(
                page, pageSize, sourceWarehouseId, destinationWarehouseId, status, searchTerm, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transfer orders.");
            return StatusCode(500, new { message = "An error occurred while retrieving transfer orders." });
        }
    }

    /// <summary>
    /// Gets a transfer order by ID.
    /// </summary>
    /// <param name="id">Transfer order ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Transfer order details.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<TransferOrderDto>> GetTransferOrderById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var transferOrder = await _transferOrderService.GetTransferOrderByIdAsync(id, cancellationToken);
            if (transferOrder == null)
            {
                return NotFound(new { message = $"Transfer order with ID {id} not found." });
            }

            return Ok(transferOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transfer order {TransferOrderId}.", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the transfer order." });
        }
    }

    /// <summary>
    /// Creates a new transfer order.
    /// </summary>
    /// <param name="createDto">Transfer order creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created transfer order.</returns>
    [HttpPost]
    public async Task<ActionResult<TransferOrderDto>> CreateTransferOrder(
        [FromBody] CreateTransferOrderDto createDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var transferOrder = await _transferOrderService.CreateTransferOrderAsync(createDto, currentUser, cancellationToken);
            return CreatedAtAction(nameof(GetTransferOrderById), new { id = transferOrder.Id }, transferOrder);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating transfer order.");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transfer order.");
            return StatusCode(500, new { message = "An error occurred while creating the transfer order." });
        }
    }

    /// <summary>
    /// Ships a transfer order - creates stock movements OUT and reduces stock.
    /// </summary>
    /// <param name="id">Transfer order ID.</param>
    /// <param name="shipDto">Shipping data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated transfer order.</returns>
    [HttpPost("{id}/ship")]
    public async Task<ActionResult<TransferOrderDto>> ShipTransferOrder(
        Guid id,
        [FromBody] ShipTransferOrderDto shipDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var transferOrder = await _transferOrderService.ShipTransferOrderAsync(id, shipDto, currentUser, cancellationToken);
            return Ok(transferOrder);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while shipping transfer order {TransferOrderId}.", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shipping transfer order {TransferOrderId}.", id);
            return StatusCode(500, new { message = "An error occurred while shipping the transfer order." });
        }
    }

    /// <summary>
    /// Receives a transfer order - creates stock movements IN and increases stock.
    /// </summary>
    /// <param name="id">Transfer order ID.</param>
    /// <param name="receiveDto">Receiving data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated transfer order.</returns>
    [HttpPost("{id}/receive")]
    public async Task<ActionResult<TransferOrderDto>> ReceiveTransferOrder(
        Guid id,
        [FromBody] ReceiveTransferOrderDto receiveDto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var transferOrder = await _transferOrderService.ReceiveTransferOrderAsync(id, receiveDto, currentUser, cancellationToken);
            return Ok(transferOrder);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while receiving transfer order {TransferOrderId}.", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving transfer order {TransferOrderId}.", id);
            return StatusCode(500, new { message = "An error occurred while receiving the transfer order." });
        }
    }

    /// <summary>
    /// Cancels a transfer order.
    /// </summary>
    /// <param name="id">Transfer order ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    [HttpDelete("{id}/cancel")]
    public async Task<ActionResult> CancelTransferOrder(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUser = GetCurrentUser();
            var success = await _transferOrderService.CancelTransferOrderAsync(id, currentUser, cancellationToken);
            if (!success)
            {
                return NotFound(new { message = $"Transfer order with ID {id} not found." });
            }

            return Ok(new { message = "Transfer order cancelled successfully." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while cancelling transfer order {TransferOrderId}.", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling transfer order {TransferOrderId}.", id);
            return StatusCode(500, new { message = "An error occurred while cancelling the transfer order." });
        }
    }
}
