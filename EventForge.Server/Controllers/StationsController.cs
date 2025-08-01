using EventForge.DTOs.Station;
using EventForge.Server.Services.Station;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// REST API controller for station and printer management with multi-tenant support.
/// Provides CRUD operations for stations within the authenticated user's tenant context.
/// </summary>
[Route("api/v1/[controller]")]
[Authorize]
public class StationsController : BaseApiController
{
    private readonly IStationService _stationService;
    private readonly ITenantContext _tenantContext;

    public StationsController(IStationService stationService, ITenantContext tenantContext)
    {
        _stationService = stationService ?? throw new ArgumentNullException(nameof(stationService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    #region Station Endpoints

    /// <summary>
    /// Gets all stations with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of stations</returns>
    /// <response code="200">Returns the paginated list of stations</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<StationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<StationDto>>> GetStations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        var validationResult = ValidatePaginationParameters(page, pageSize);
        if (validationResult != null)
            return validationResult;

        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var result = await _stationService.GetStationsAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving stations.", ex);
        }
    }

    /// <summary>
    /// Gets a station by ID.
    /// </summary>
    /// <param name="id">Station ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Station details</returns>
    /// <response code="200">Returns the station</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the station is not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StationDto>> GetStation(Guid id, CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var station = await _stationService.GetStationByIdAsync(id, cancellationToken);

            if (station == null)
            {
                return CreateNotFoundProblem($"Station with ID {id} not found.");
            }

            return Ok(station);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the station.", ex);
        }
    }

    /// <summary>
    /// Creates a new station.
    /// </summary>
    /// <param name="createStationDto">Station creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created station</returns>
    /// <response code="201">Returns the newly created station</response>
    /// <response code="400">If the station data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost]
    [ProducesResponseType(typeof(StationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StationDto>> CreateStation(CreateStationDto createStationDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var station = await _stationService.CreateStationAsync(createStationDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetStation), new { id = station.Id }, station);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the station.", ex);
        }
    }

    /// <summary>
    /// Updates an existing station.
    /// </summary>
    /// <param name="id">Station ID</param>
    /// <param name="updateStationDto">Station update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated station</returns>
    /// <response code="200">Returns the updated station</response>
    /// <response code="400">If the station data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the station is not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(StationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StationDto>> UpdateStation(Guid id, UpdateStationDto updateStationDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var station = await _stationService.UpdateStationAsync(id, updateStationDto, currentUser, cancellationToken);

            if (station == null)
            {
                return CreateNotFoundProblem($"Station with ID {id} not found.");
            }

            return Ok(station);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the station.", ex);
        }
    }

    /// <summary>
    /// Deletes a station (soft delete).
    /// </summary>
    /// <param name="id">Station ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Station deleted successfully</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the station is not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStation(Guid id, CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _stationService.DeleteStationAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Station with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the station.", ex);
        }
    }

    #endregion

    #region Printer Endpoints

    /// <summary>
    /// Gets all printers with optional pagination.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of printers</returns>
    /// <response code="200">Returns the paginated list of printers</response>
    /// <response code="400">If the query parameters are invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("printers")]
    [ProducesResponseType(typeof(PagedResult<PrinterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<PrinterDto>>> GetPrinters(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        var validationResult = ValidatePaginationParameters(page, pageSize);
        if (validationResult != null)
            return validationResult;

        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var result = await _stationService.GetPrintersAsync(page, pageSize, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving printers.", ex);
        }
    }

    /// <summary>
    /// Gets a printer by ID.
    /// </summary>
    /// <param name="id">Printer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Printer details</returns>
    /// <response code="200">Returns the printer</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the printer is not found</response>
    [HttpGet("printers/{id:guid}")]
    [ProducesResponseType(typeof(PrinterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrinterDto>> GetPrinter(Guid id, CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var printer = await _stationService.GetPrinterByIdAsync(id, cancellationToken);

            if (printer == null)
            {
                return CreateNotFoundProblem($"Printer with ID {id} not found.");
            }

            return Ok(printer);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the printer.", ex);
        }
    }

    /// <summary>
    /// Gets printers by station.
    /// </summary>
    /// <param name="stationId">Station ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of printers for the station</returns>
    /// <response code="200">Returns the list of printers</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpGet("{stationId:guid}/printers")]
    [ProducesResponseType(typeof(IEnumerable<PrinterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<PrinterDto>>> GetPrintersByStation(Guid stationId, CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var printers = await _stationService.GetPrintersByStationAsync(stationId, cancellationToken);
            return Ok(printers);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving printers by station.", ex);
        }
    }

    /// <summary>
    /// Creates a new printer.
    /// </summary>
    /// <param name="createPrinterDto">Printer creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created printer</returns>
    /// <response code="201">Returns the newly created printer</response>
    /// <response code="400">If the printer data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    [HttpPost("printers")]
    [ProducesResponseType(typeof(PrinterDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PrinterDto>> CreatePrinter(CreatePrinterDto createPrinterDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var printer = await _stationService.CreatePrinterAsync(createPrinterDto, currentUser, cancellationToken);

            return CreatedAtAction(nameof(GetPrinter), new { id = printer.Id }, printer);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the printer.", ex);
        }
    }

    /// <summary>
    /// Updates an existing printer.
    /// </summary>
    /// <param name="id">Printer ID</param>
    /// <param name="updatePrinterDto">Printer update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated printer</returns>
    /// <response code="200">Returns the updated printer</response>
    /// <response code="400">If the printer data is invalid</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the printer is not found</response>
    [HttpPut("printers/{id:guid}")]
    [ProducesResponseType(typeof(PrinterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrinterDto>> UpdatePrinter(Guid id, UpdatePrinterDto updatePrinterDto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var printer = await _stationService.UpdatePrinterAsync(id, updatePrinterDto, currentUser, cancellationToken);

            if (printer == null)
            {
                return CreateNotFoundProblem($"Printer with ID {id} not found.");
            }

            return Ok(printer);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the printer.", ex);
        }
    }

    /// <summary>
    /// Deletes a printer (soft delete).
    /// </summary>
    /// <param name="id">Printer ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Printer deleted successfully</response>
    /// <response code="403">If the user doesn't have access to the current tenant</response>
    /// <response code="404">If the printer is not found</response>
    [HttpDelete("printers/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePrinter(Guid id, CancellationToken cancellationToken = default)
    {
        // Validate tenant access
        var tenantValidation = await ValidateTenantAccessAsync(_tenantContext);
        if (tenantValidation != null)
            return tenantValidation;

        try
        {
            var currentUser = GetCurrentUser();
            var deleted = await _stationService.DeletePrinterAsync(id, currentUser, cancellationToken);

            if (!deleted)
            {
                return CreateNotFoundProblem($"Printer with ID {id} not found.");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the printer.", ex);
        }
    }

    #endregion
}