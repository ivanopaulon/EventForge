using EventForge.DTOs.Sales;
using EventForge.Server.Filters;
using EventForge.Server.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for managing tables and reservations.
/// </summary>
[ApiController]
[Route("api/v1/tables")]
[Authorize]
[RequireLicenseFeature("SalesManagement")]
public class TableManagementController : BaseApiController
{
    private readonly ITableManagementService _tableService;
    private readonly ILogger<TableManagementController> _logger;

    public TableManagementController(
        ITableManagementService tableService,
        ILogger<TableManagementController> logger)
    {
        _tableService = tableService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all tables.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TableSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TableSessionDto>>> GetAllTables(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all tables");

        try
        {
            var tables = await _tableService.GetAllTablesAsync(cancellationToken);
            return Ok(tables);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all tables");
            return StatusCode(500, new { error = "An error occurred while getting tables." });
        }
    }

    /// <summary>
    /// Gets a specific table by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TableSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TableSessionDto>> GetTable(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting table {TableId}", id);

        try
        {
            var table = await _tableService.GetTableAsync(id, cancellationToken);
            if (table == null)
            {
                return NotFound(new { error = "Table not found." });
            }

            return Ok(table);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting table {TableId}", id);
            return StatusCode(500, new { error = "An error occurred while getting the table." });
        }
    }

    /// <summary>
    /// Gets all available tables.
    /// </summary>
    [HttpGet("available")]
    [ProducesResponseType(typeof(List<TableSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TableSessionDto>>> GetAvailableTables(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting available tables");

        try
        {
            var tables = await _tableService.GetAvailableTablesAsync(cancellationToken);
            return Ok(tables);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available tables");
            return StatusCode(500, new { error = "An error occurred while getting available tables." });
        }
    }

    /// <summary>
    /// Creates a new table.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TableSessionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TableSessionDto>> CreateTable([FromBody] CreateTableSessionDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating table {TableNumber}", dto.TableNumber);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var table = await _tableService.CreateTableAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetTable), new { id = table.Id }, table);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation creating table");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating table");
            return StatusCode(500, new { error = "An error occurred while creating the table." });
        }
    }

    /// <summary>
    /// Updates a table.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TableSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TableSessionDto>> UpdateTable(Guid id, [FromBody] UpdateTableSessionDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating table {TableId}", id);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var table = await _tableService.UpdateTableAsync(id, dto, cancellationToken);
            if (table == null)
            {
                return NotFound(new { error = "Table not found." });
            }

            return Ok(table);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation updating table");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating table {TableId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the table." });
        }
    }

    /// <summary>
    /// Updates table status.
    /// </summary>
    [HttpPut("{id}/status")]
    [ProducesResponseType(typeof(TableSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TableSessionDto>> UpdateTableStatus(Guid id, [FromBody] UpdateTableStatusDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating status for table {TableId} to {Status}", id, dto.Status);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var table = await _tableService.UpdateTableStatusAsync(id, dto, cancellationToken);
            if (table == null)
            {
                return NotFound(new { error = "Table not found." });
            }

            return Ok(table);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid status value");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating table status {TableId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the table status." });
        }
    }

    /// <summary>
    /// Deletes a table.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTable(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting table {TableId}", id);

        try
        {
            var success = await _tableService.DeleteTableAsync(id, cancellationToken);
            if (!success)
            {
                return NotFound(new { error = "Table not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting table {TableId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the table." });
        }
    }

    // Reservation endpoints

    /// <summary>
    /// Gets reservations for a specific date.
    /// </summary>
    [HttpGet("reservations")]
    [ProducesResponseType(typeof(List<TableReservationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TableReservationDto>>> GetReservations([FromQuery] DateTime date, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting reservations for date {Date}", date.Date);

        try
        {
            var reservations = await _tableService.GetReservationsByDateAsync(date, cancellationToken);
            return Ok(reservations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reservations");
            return StatusCode(500, new { error = "An error occurred while getting reservations." });
        }
    }

    /// <summary>
    /// Gets a specific reservation by ID.
    /// </summary>
    [HttpGet("reservations/{id}")]
    [ProducesResponseType(typeof(TableReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TableReservationDto>> GetReservation(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting reservation {ReservationId}", id);

        try
        {
            var reservation = await _tableService.GetReservationAsync(id, cancellationToken);
            if (reservation == null)
            {
                return NotFound(new { error = "Reservation not found." });
            }

            return Ok(reservation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reservation {ReservationId}", id);
            return StatusCode(500, new { error = "An error occurred while getting the reservation." });
        }
    }

    /// <summary>
    /// Creates a new reservation.
    /// </summary>
    [HttpPost("reservations")]
    [ProducesResponseType(typeof(TableReservationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TableReservationDto>> CreateReservation([FromBody] CreateTableReservationDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating reservation for table {TableId}", dto.TableId);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var reservation = await _tableService.CreateReservationAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, reservation);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation creating reservation");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reservation");
            return StatusCode(500, new { error = "An error occurred while creating the reservation." });
        }
    }

    /// <summary>
    /// Updates a reservation.
    /// </summary>
    [HttpPut("reservations/{id}")]
    [ProducesResponseType(typeof(TableReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TableReservationDto>> UpdateReservation(Guid id, [FromBody] UpdateTableReservationDto dto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating reservation {ReservationId}", id);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var reservation = await _tableService.UpdateReservationAsync(id, dto, cancellationToken);
            if (reservation == null)
            {
                return NotFound(new { error = "Reservation not found." });
            }

            return Ok(reservation);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation updating reservation");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reservation {ReservationId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the reservation." });
        }
    }

    /// <summary>
    /// Confirms a reservation.
    /// </summary>
    [HttpPut("reservations/{id}/confirm")]
    [ProducesResponseType(typeof(TableReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TableReservationDto>> ConfirmReservation(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Confirming reservation {ReservationId}", id);

        try
        {
            var reservation = await _tableService.ConfirmReservationAsync(id, cancellationToken);
            if (reservation == null)
            {
                return NotFound(new { error = "Reservation not found." });
            }

            return Ok(reservation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming reservation {ReservationId}", id);
            return StatusCode(500, new { error = "An error occurred while confirming the reservation." });
        }
    }

    /// <summary>
    /// Marks a reservation as arrived.
    /// </summary>
    [HttpPut("reservations/{id}/arrived")]
    [ProducesResponseType(typeof(TableReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TableReservationDto>> MarkArrived(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Marking reservation {ReservationId} as arrived", id);

        try
        {
            var reservation = await _tableService.MarkArrivedAsync(id, cancellationToken);
            if (reservation == null)
            {
                return NotFound(new { error = "Reservation not found." });
            }

            return Ok(reservation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking reservation {ReservationId} as arrived", id);
            return StatusCode(500, new { error = "An error occurred while marking the reservation as arrived." });
        }
    }

    /// <summary>
    /// Cancels a reservation.
    /// </summary>
    [HttpDelete("reservations/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelReservation(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling reservation {ReservationId}", id);

        try
        {
            var success = await _tableService.CancelReservationAsync(id, cancellationToken);
            if (!success)
            {
                return NotFound(new { error = "Reservation not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling reservation {ReservationId}", id);
            return StatusCode(500, new { error = "An error occurred while cancelling the reservation." });
        }
    }

    /// <summary>
    /// Marks a reservation as no-show.
    /// </summary>
    [HttpPut("reservations/{id}/no-show")]
    [ProducesResponseType(typeof(TableReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TableReservationDto>> MarkNoShow(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Marking reservation {ReservationId} as no-show", id);

        try
        {
            var reservation = await _tableService.MarkNoShowAsync(id, cancellationToken);
            if (reservation == null)
            {
                return NotFound(new { error = "Reservation not found." });
            }

            return Ok(reservation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking reservation {ReservationId} as no-show", id);
            return StatusCode(500, new { error = "An error occurred while marking the reservation as no-show." });
        }
    }
}
