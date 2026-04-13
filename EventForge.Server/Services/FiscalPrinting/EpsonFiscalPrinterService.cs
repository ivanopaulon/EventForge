using Prym.DTOs.Common;
using Prym.DTOs.FiscalPrinting;
using EventForge.Server.Data;
using EventForge.Server.Services.FiscalPrinting.Communication;
using EventForge.Server.Services.FiscalPrinting.EpsonProtocol;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EventForge.Server.Services.FiscalPrinting;

/// <summary>
/// Implementation of <see cref="IFiscalPrinterService"/> for Epson TM-series network printers
/// using the Epson POS Printer WebAPI (ePOS-Print XML protocol).
/// </summary>
/// <remarks>
/// Communicates with the printer's built-in HTTP server via
/// <c>POST http://{ip}:{port}/api/1/request</c> using SOAP/XML.
/// <para>
/// Specification: Epson POS Printer WebAPI Interface Specification (Rev. A).
/// </para>
/// <para>
/// <b>Connection types supported</b>: <c>Tcp</c> (Ethernet/WiFi, direct from server)
/// and <c>TcpViaAgent</c> (Ethernet/WiFi on the agent's local network, relayed via UpdateAgent HTTP proxy).
/// </para>
/// <para>
/// <b>Device ID</b>: stored in <see cref="EventForge.Server.Data.Entities.Common.Printer.UsbDeviceId"/>.
/// Defaults to <c>"local_printer"</c> when not set.
/// </para>
/// <para>
/// <b>Note on fiscal compliance</b>: Epson TM-series receipt printers do not have
/// built-in fiscal memory (RT). Daily closure and Z-report operations are handled
/// entirely in software by aggregating data from the EventForge database and printing
/// a summary document. For hardware RT fiscal compliance use the Custom protocol.
/// </para>
/// </remarks>
public partial class EpsonFiscalPrinterService(
    EventForgeDbContext context,
    ILoggerFactory loggerFactory,
    ILogger<EpsonFiscalPrinterService> logger,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IFiscalPrinterService
{
    // Payment method codes recognised as "cash" for POS aggregation (shared with Custom)
    private const int CashFiscalCode = 1;
    private static readonly HashSet<string> CashPaymentCodes =
        new(StringComparer.OrdinalIgnoreCase) { "CASH", "CONTANTE" };

    // -------------------------------------------------------------------------
    //  Private helpers – channel creation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Loads the printer from the database and creates the Epson WebAPI HTTP channel.
    /// Supports both direct TCP (<c>Tcp</c>) and agent-proxied (<c>TcpViaAgent</c>) connections.
    /// </summary>
    private async Task<IEpsonChannel> CreateChannelAsync(
        Guid printerId,
        CancellationToken cancellationToken)
    {
        var printer = await context.Printers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == printerId, cancellationToken)
            .ConfigureAwait(false);

        if (printer is null)
            throw new InvalidOperationException($"Fiscal printer with ID {printerId} not found.");

        if (!printer.IsFiscalPrinter)
            throw new InvalidOperationException(
                $"Printer '{printer.Name}' (ID: {printerId}) is not configured as a fiscal printer.");

        if (string.IsNullOrWhiteSpace(printer.Address))
            throw new InvalidOperationException(
                $"Printer '{printer.Name}' (ID: {printerId}) has no IP address configured. " +
                "Set Address for Epson WebAPI printers.");

        int port = printer.Port is > 0 ? printer.Port.Value : EpsonProtocolConstants.DefaultPort;
        string devid = string.IsNullOrWhiteSpace(printer.UsbDeviceId)
            ? EpsonProtocolConstants.DefaultDeviceId
            : printer.UsbDeviceId;

        // TcpViaAgent: the printer is on the agent's local network
        if (printer.ConnectionType == PrinterConnectionType.TcpViaAgent)
        {
            if (printer.AgentId is null)
                throw new InvalidOperationException(
                    $"Printer '{printer.Name}' (ID: {printerId}) is configured as TcpViaAgent but has no AgentId.");

            var agentUrl = GetAgentBaseUrl(printer.AgentId.Value);

            logger.LogDebug(
                "Creating AgentEpsonProxy channel | Printer={Name} {Host}:{Port} devid={DevId} via agent={AgentUrl}",
                printer.Name, printer.Address, port, devid, agentUrl);

            return new AgentEpsonProxyCommunication(
                agentUrl,
                printer.Address,
                port,
                devid,
                httpClientFactory,
                loggerFactory.CreateLogger<AgentEpsonProxyCommunication>());
        }

        // Direct TCP: server connects straight to the printer's IP
        logger.LogDebug(
            "Creating Epson WebAPI channel | Printer={Name} {Host}:{Port} devid={DevId}",
            printer.Name, printer.Address, port, devid);

        return new EpsonWebApiCommunication(
            printer.Address,
            port,
            devid,
            httpClientFactory,
            loggerFactory.CreateLogger<EpsonWebApiCommunication>());
    }

    /// <summary>
    /// Creates a direct channel from raw connection parameters (no DB lookup).
    /// Used by wizard methods (<see cref="TestTcpConnectionAsync"/>,
    /// <see cref="GetPrinterInfoByAddressAsync"/>).
    /// </summary>
    private IEpsonChannel CreateDirectChannel(
        string ipAddress,
        int port,
        string devid = EpsonProtocolConstants.DefaultDeviceId)
        => new EpsonWebApiCommunication(
            ipAddress,
            port,
            devid,
            httpClientFactory,
            loggerFactory.CreateLogger<EpsonWebApiCommunication>());

    /// <summary>
    /// Resolves the base URL of an agent from configuration (<c>AgentProxies:{agentId}</c>).
    /// </summary>
    private string GetAgentBaseUrl(Guid agentId)
    {
        var url = configuration[$"AgentProxies:{agentId}"];
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException(
                $"Agent base URL not configured for agent ID '{agentId}'. " +
                "Add 'AgentProxies:{agentId}' to application configuration.");
        return url.TrimEnd('/');
    }

    // -------------------------------------------------------------------------
    //  Private helpers – request execution
    // -------------------------------------------------------------------------

    /// <summary>
    /// Sends the XML document, parses the response, and converts it to a
    /// <see cref="FiscalPrintResult"/>.
    /// </summary>
    private async Task<FiscalPrintResult> ExecuteXmlAsync(
        IEpsonChannel channel,
        string xml,
        Guid printerId,
        CancellationToken cancellationToken)
    {
        try
        {
            var rawResponse = await channel
                .SendXmlAsync(xml, cancellationToken)
                .ConfigureAwait(false);

            var response = EpsonResponseParser.ParseResponse(rawResponse);

            if (!response.Success)
            {
                logger.LogWarning(
                    "Epson printer returned error | PrinterId={PrinterId} Code={Code} Msg={Msg}",
                    printerId, response.Code, response.ErrorMessage);

                return new FiscalPrintResult
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage ?? "Printer returned an error",
                    PrintDate = DateTime.UtcNow
                };
            }

            logger.LogInformation(
                "Epson operation completed | PrinterId={PrinterId} devid={DevId}",
                printerId, channel.DeviceId);

            return new FiscalPrintResult { Success = true, PrintDate = DateTime.UtcNow };
        }
        catch (FiscalPrinterCommunicationException ex)
        {
            logger.LogError(ex, "Epson communication error for printer {PrinterId}", printerId);
            return new FiscalPrintResult
            {
                Success = false,
                ErrorMessage = $"Communication error: {ex.Message}",
                PrintDate = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Returns the device ID for the given printer (falls back to "local_printer").
    /// </summary>
    private static string GetDevId(string? usbDeviceId)
        => string.IsNullOrWhiteSpace(usbDeviceId)
            ? EpsonProtocolConstants.DefaultDeviceId
            : usbDeviceId;
}
