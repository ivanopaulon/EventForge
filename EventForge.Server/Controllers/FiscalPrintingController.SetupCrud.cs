using EventForge.Server.Services.FiscalPrinting;
using EventForge.Server.Services.Station;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.FiscalPrinting;
using Prym.DTOs.Station;
using System.Text.Json;


namespace EventForge.Server.Controllers;

public partial class FiscalPrintingController
{
    /// <summary>
    /// Saves the complete fiscal printer configuration produced by the setup wizard.
    /// Creates the printer record, persists fiscal code mapping overrides, and
    /// associates the printer to the specified POS stations as default fiscal printer.
    /// </summary>
    /// <param name="setup">Complete wizard configuration payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("setup")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(PrinterDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PrinterDto>> SaveSetupAsync(
        [FromBody] FiscalPrinterSetupDto setup,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            logger.LogInformation(
                "SaveSetupAsync | Name={Name} ConnectionType={Type} User={User}",
                setup.Name, setup.ConnectionType, GetCurrentUser());

            var connectionType = setup.ConnectionType switch
            {
                "Serial" => PrinterConnectionType.Serial,
                "UsbViaAgent" => PrinterConnectionType.UsbViaAgent,
                "NetworkShare" => PrinterConnectionType.NetworkShare,
                "TcpViaAgent" => PrinterConnectionType.TcpViaAgent,
                _ => PrinterConnectionType.Tcp
            };

            var createDto = new CreatePrinterDto
            {
                Name = setup.Name,
                Type = "Fiscal",
                Location = setup.Location,
                IsFiscalPrinter = true,
                ProtocolType = setup.ProtocolType,
                Status = PrinterConfigurationStatus.Active,
                ConnectionType = connectionType,
                AgentId = setup.AgentId,
                UsbDeviceId = setup.UsbDeviceId,
                Category = setup.Category,
                IsThermal = setup.IsThermal,
                PrinterWidth = setup.PrinterWidth,
                PaperWidth = setup.PaperWidth,
                PrintLanguage = setup.PrintLanguage
            };

            if (setup.ConnectionType == "TCP" || setup.ConnectionType == "TcpViaAgent")
            {
                createDto.Address = setup.IpAddress;
                createDto.Port = setup.TcpPort;
            }
            else if (setup.ConnectionType == "Serial")
            {
                createDto.SerialPortName = setup.SerialPortName;
                createDto.BaudRate = setup.BaudRate;
            }
            else if (setup.ConnectionType == "UsbViaAgent")
            {
                createDto.AgentId = setup.AgentId;
                createDto.UsbDeviceId = setup.UsbDeviceId;
            }

            if (setup.ConnectionType == "TcpViaAgent")
                createDto.AgentId = setup.AgentId;

            var printer = await stationService.CreatePrinterAsync(
                createDto, GetCurrentUser(), cancellationToken);

            // Note: VAT/Payment fiscal code overrides and POS associations
            // are persisted via the existing FiscalMappingService and StationsController.
            // The wizard client handles these in separate calls after receiving the printer ID.
            // This keeps the setup endpoint focused and avoids cross-service transactions.

            logger.LogInformation(
                "Fiscal printer {Name} created via wizard | PrinterId={Id}",
                printer.Name, printer.Id);

            await auditLogService.LogEntityChangeAsync(
                entityName: "Printer",
                entityId: printer.Id,
                propertyName: "Configuration",
                operationType: "Insert",
                oldValue: null,
                newValue: JsonSerializer.Serialize(setup),
                changedBy: GetCurrentUser(),
                entityDisplayName: printer.Name,
                cancellationToken: cancellationToken);

            return CreatedAtAction(
                actionName: null,
                routeValues: new { },
                value: printer);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                "Unexpected error saving fiscal printer setup.", ex);
        }
    }

    /// <summary>
    /// Returns the wizard setup payload for an existing printer (used to populate edit mode).
    /// </summary>
    /// <param name="printerId">Printer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("setup/{printerId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(FiscalPrinterSetupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FiscalPrinterSetupDto>> GetSetupAsync(
        Guid printerId,
        CancellationToken cancellationToken = default)
    {
        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            var printer = await stationService.GetPrinterByIdAsync(printerId, cancellationToken);
            if (printer is null)
                return NotFound(new ProblemDetails { Title = "Printer not found.", Detail = $"No printer with ID {printerId}." });

            var connectionType = printer.ConnectionType switch
            {
                PrinterConnectionType.Serial => "Serial",
                PrinterConnectionType.UsbViaAgent => "UsbViaAgent",
                PrinterConnectionType.NetworkShare => "NetworkShare",
                PrinterConnectionType.TcpViaAgent => "TcpViaAgent",
                _ => "TCP"
            };

            var setupDto = new FiscalPrinterSetupDto
            {
                ConnectionType = connectionType,
                IpAddress = printer.Address,
                TcpPort = printer.Port,
                SerialPortName = printer.SerialPortName,
                BaudRate = printer.BaudRate,
                AgentId = printer.AgentId,
                UsbDeviceId = printer.UsbDeviceId,
                Name = printer.Name,
                Location = printer.Location,
                ProtocolType = printer.ProtocolType ?? "Custom",
                Category = printer.Category,
                IsThermal = printer.IsThermal,
                PrinterWidth = printer.PrinterWidth,
                PaperWidth = printer.PaperWidth,
                PrintLanguage = printer.PrintLanguage
            };

            // Populate stations already associated with this printer
            var allStations = await stationService.GetStationsAsync(1, 200, cancellationToken);
            setupDto.AssociatedStationIds = allStations.Items?
                .Where(s => s.AssignedPrinterId == printerId)
                .Select(s => s.Id)
                .ToList() ?? new List<Guid>();

            return Ok(setupDto);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error loading setup for printer {printerId}.", ex);
        }
    }

    /// <summary>
    /// Updates an existing printer's configuration from the wizard (edit mode).
    /// </summary>
    /// <param name="printerId">Printer identifier.</param>
    /// <param name="setup">Updated wizard configuration payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("setup/{printerId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(PrinterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PrinterDto>> UpdateSetupAsync(
        Guid printerId,
        [FromBody] FiscalPrinterSetupDto setup,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return CreateValidationProblemDetails();

        var tenantValidation = await ValidateTenantAccessAsync(tenantContext);
        if (tenantValidation is not null)
            return tenantValidation;

        try
        {
            logger.LogInformation(
                "UpdateSetupAsync | PrinterId={PrinterId} Name={Name} ConnectionType={Type} User={User}",
                printerId, setup.Name, setup.ConnectionType, GetCurrentUser());

            var existing = await stationService.GetPrinterByIdAsync(printerId, cancellationToken);
            if (existing is null)
                return NotFound(new ProblemDetails { Title = "Printer not found.", Detail = $"No printer with ID {printerId}." });

            var connectionType = setup.ConnectionType switch
            {
                "Serial" => PrinterConnectionType.Serial,
                "UsbViaAgent" => PrinterConnectionType.UsbViaAgent,
                "NetworkShare" => PrinterConnectionType.NetworkShare,
                "TcpViaAgent" => PrinterConnectionType.TcpViaAgent,
                _ => PrinterConnectionType.Tcp
            };

            var updateDto = new UpdatePrinterDto
            {
                Name = setup.Name,
                Type = existing.Type,
                Model = existing.Model,
                Location = setup.Location,
                IsFiscalPrinter = true,
                ProtocolType = setup.ProtocolType,
                Status = existing.Status,
                ConnectionType = connectionType,
                AgentId = setup.AgentId,
                UsbDeviceId = setup.UsbDeviceId,
                Category = setup.Category,
                IsThermal = setup.IsThermal,
                PrinterWidth = setup.PrinterWidth,
                PaperWidth = setup.PaperWidth,
                PrintLanguage = setup.PrintLanguage
            };

            if (setup.ConnectionType == "TCP" || setup.ConnectionType == "TcpViaAgent")
            {
                updateDto.Address = setup.IpAddress;
                updateDto.Port = setup.TcpPort;
            }
            else if (setup.ConnectionType == "Serial")
            {
                updateDto.SerialPortName = setup.SerialPortName;
                updateDto.BaudRate = setup.BaudRate;
            }

            var printer = await stationService.UpdatePrinterAsync(
                printerId, updateDto, GetCurrentUser(), cancellationToken);

            if (printer is null)
                return NotFound(new ProblemDetails { Title = "Printer not found.", Detail = $"Printer {printerId} was not found during update." });

            logger.LogInformation(
                "Fiscal printer {Name} updated via wizard | PrinterId={Id}",
                printer.Name, printer.Id);

            await auditLogService.LogEntityChangeAsync(
                entityName: "Printer",
                entityId: printer.Id,
                propertyName: "Configuration",
                operationType: "Update",
                oldValue: JsonSerializer.Serialize(existing),
                newValue: JsonSerializer.Serialize(setup),
                changedBy: GetCurrentUser(),
                entityDisplayName: printer.Name,
                cancellationToken: cancellationToken);

            return Ok(printer);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem(
                $"Unexpected error updating fiscal printer setup for {printerId}.", ex);
        }
    }

    // -------------------------------------------------------------------------
    //  Daily closure – pre-check (5B.4)
    // -------------------------------------------------------------------------

}
