using Prym.DTOs.FiscalPrinting;
using Prym.Server.Data;
using Prym.Server.Services.FiscalPrinting.Communication;
using Prym.Server.Services.FiscalPrinting.CustomProtocol;
using Microsoft.EntityFrameworkCore;

namespace Prym.Server.Services.FiscalPrinting;

/// <summary>
/// Implementation of <see cref="IFiscalPrinterService"/> for Custom fiscal printers.
/// Resolves the printer configuration from the database, builds the appropriate
/// communication channel (TCP, serial, or USB-via-Agent proxy), executes the command
/// sequence, and parses the printer responses.
/// </summary>
/// <remarks>
/// Protocol type "Custom" is the only protocol currently supported.
/// The service selects the channel based on <see cref="Printer.ConnectionType"/>:
/// <list type="bullet">
///   <item><term>TCP</term><description>Uses <see cref="CustomTcpCommunication"/> (Address + Port).</description></item>
///   <item><term>Serial</term><description>Uses <see cref="CustomSerialCommunication"/> (SerialPortName).</description></item>
///   <item><term>UsbViaAgent</term><description>Uses <see cref="AgentProxyCommunication"/> (AgentId → URL from configuration, UsbDeviceId).</description></item>
/// </list>
/// Agent base URLs are resolved from <c>AgentProxies:{agentId}</c> in application configuration.
/// </remarks>
public partial class CustomFiscalPrinterService(
    PrymDbContext context,
    ILoggerFactory loggerFactory,
    ILogger<CustomFiscalPrinterService> logger,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory) : IFiscalPrinterService
{
    private readonly FiscalReceiptBuilder _builder = new();

    // Payment method codes recognised as "cash" for POS aggregation
    private const int CashFiscalCode = 1;
    private static readonly HashSet<string> CashPaymentCodes =
        new(StringComparer.OrdinalIgnoreCase) { "CASH", "CONTANTE" };

    // -------------------------------------------------------------------------
    //  Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Loads the printer from the database and creates the appropriate communication channel.
    /// </summary>
    private async Task<ICustomPrinterCommunication> CreateChannelAsync(
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

        // USB-via-Agent channel: requires AgentId + UsbDeviceId
        if (printer.ConnectionType == PrinterConnectionType.UsbViaAgent)
        {
            if (printer.AgentId is null)
                throw new InvalidOperationException(
                    $"Printer '{printer.Name}' (ID: {printerId}) is configured as UsbViaAgent but has no AgentId.");

            if (string.IsNullOrWhiteSpace(printer.UsbDeviceId))
                throw new InvalidOperationException(
                    $"Printer '{printer.Name}' (ID: {printerId}) is configured as UsbViaAgent but has no UsbDeviceId.");

            var agentUrl = GetAgentBaseUrl(printer.AgentId.Value);

            logger.LogDebug(
                "Creating agent-proxy channel for printer {Name} (agent={AgentId} device={DeviceId} url={Url})",
                printer.Name, printer.AgentId, printer.UsbDeviceId, agentUrl);

            return new AgentProxyCommunication(
                agentUrl,
                printer.UsbDeviceId,
                timeoutMs: 30_000,
                loggerFactory.CreateLogger<AgentProxyCommunication>(),
                httpClientFactory);
        }

        // TCP channel: requires Address + Port
        if (!string.IsNullOrWhiteSpace(printer.Address) && printer.Port > 0)
        {
            logger.LogDebug(
                "Creating TCP channel for printer {Name} ({Host}:{Port})",
                printer.Name, printer.Address, printer.Port);

            return new CustomTcpCommunication(
                printer.Address,
                printer.Port!.Value,
                loggerFactory.CreateLogger<CustomTcpCommunication>());
        }

        // Serial channel: requires SerialPortName
        if (!string.IsNullOrWhiteSpace(printer.SerialPortName))
        {
            logger.LogDebug(
                "Creating serial channel for printer {Name} ({Port} @ {BaudRate} baud)",
                printer.Name, printer.SerialPortName, printer.BaudRate ?? 9600);

            return new CustomSerialCommunication(
                printer.SerialPortName,
                loggerFactory.CreateLogger<CustomSerialCommunication>(),
                baudRate: printer.BaudRate ?? 9600);
        }

        throw new InvalidOperationException(
            $"Printer '{printer.Name}' (ID: {printerId}) has no valid connection configuration. " +
            "Set Address+Port for TCP, SerialPortName for serial, or AgentId+UsbDeviceId for USB-via-Agent.");
    }

    /// <summary>
    /// Resolves the base URL of an agent from configuration key <c>AgentProxies:{agentId}</c>.
    /// </summary>
    /// <param name="agentId">The agent's stable GUID.</param>
    /// <returns>The base URL (e.g. <c>http://localhost:5780</c>) of the agent's HTTP endpoint.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no URL is configured for the given <paramref name="agentId"/>.
    /// </exception>
    /// <remarks>
    /// Configure agent URLs in <c>appsettings.json</c> (or environment variables) using the pattern:
    /// <code>
    /// "AgentProxies": {
    ///   "11111111-2222-3333-4444-555555555555": "http://localhost:5780",
    ///   "aaaabbbb-cccc-dddd-eeee-ffffffffffff": "http://192.168.1.50:5780"
    /// }
    /// </code>
    /// Each key is a Guid matching <see cref="Printer.AgentId"/>; the value is the agent's HTTP base URL.
    /// </remarks>
    private string GetAgentBaseUrl(Guid agentId)
    {
        var url = configuration[$"AgentProxies:{agentId}"];

        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException(
                $"No base URL configured for agent '{agentId}'. " +
                $"Add 'AgentProxies:{agentId}' to application configuration.");

        return url.TrimEnd('/');
    }

    /// <summary>
    /// Sends all frames in <paramref name="sequence"/> sequentially and verifies each response.
    /// Returns a failure result immediately if any frame receives a NAK.
    /// </summary>
    private async Task<FiscalPrintResult> ExecuteSequenceAsync(
        ICustomPrinterCommunication channel,
        IReadOnlyList<byte[]> sequence,
        Guid printerId,
        CancellationToken cancellationToken)
    {
        for (int i = 0; i < sequence.Count; i++)
        {
            try
            {
                byte[] responseBytes = await channel
                    .SendCommandAsync(sequence[i], cancellationToken)
                    .ConfigureAwait(false);

                var response = CustomResponseParser.ParseResponse(responseBytes);

                if (response.Type == CustomResponseType.Nak)
                {
                    string err = $"Printer returned NAK for command {i + 1}/{sequence.Count}";
                    logger.LogWarning("{Error} | PrinterId={PrinterId}", err, printerId);
                    return new FiscalPrintResult
                    {
                        Success = false,
                        ErrorMessage = err,
                        PrintDate = DateTime.UtcNow
                    };
                }

                if (response.Type == CustomResponseType.Unknown)
                {
                    logger.LogWarning(
                        "Unexpected response type for command {Idx}/{Total} | PrinterId={PrinterId}",
                        i + 1, sequence.Count, printerId);
                }
            }
            catch (FiscalPrinterCommunicationException ex)
            {
                logger.LogError(
                    ex,
                    "Communication error on command {Idx}/{Total} for printer {PrinterId}",
                    i + 1, sequence.Count, printerId);

                return new FiscalPrintResult
                {
                    Success = false,
                    ErrorMessage = $"Communication error: {ex.Message}",
                    PrintDate = DateTime.UtcNow
                };
            }
        }

        logger.LogInformation(
            "Sequence completed successfully | PrinterId={PrinterId} Commands={Count}",
            printerId, sequence.Count);

        return new FiscalPrintResult { Success = true, PrintDate = DateTime.UtcNow };
    }

}
