using Prym.Hardware.Exceptions;

namespace Prym.Hardware.Interfaces;

/// <summary>
/// Abstraction for an Epson POS Printer WebAPI (ePOS-Print XML) communication channel.
/// Implementations live in <c>EventForge.Server</c>:
/// <list type="bullet">
///   <item><c>EpsonWebApiCommunication</c> – direct HTTP from server</item>
///   <item><c>AgentEpsonProxyCommunication</c> – HTTP forwarded via UpdateAgent proxy</item>
/// </list>
/// </summary>
public interface IEpsonChannel : IAsyncDisposable
{
    /// <summary>The Epson device ID used in all requests to this printer (e.g. <c>local_printer</c>).</summary>
    string DeviceId { get; }

    /// <summary>
    /// Posts the given SOAP/XML document to the Epson WebAPI endpoint and returns the raw XML response.
    /// </summary>
    /// <param name="xmlBody">Complete SOAP/XML string built by <c>EpsonXmlBuilder</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Raw XML response string from the printer.</returns>
    /// <exception cref="FiscalPrinterCommunicationException">
    /// Thrown when the request fails, times out, or returns a non-success status.
    /// </exception>
    Task<string> SendXmlAsync(string xmlBody, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests connectivity by sending a status query and verifying the printer responds.
    /// </summary>
    /// <exception cref="FiscalPrinterCommunicationException">Thrown when the printer is unreachable.</exception>
    Task TestConnectionAsync(CancellationToken cancellationToken = default);
}
