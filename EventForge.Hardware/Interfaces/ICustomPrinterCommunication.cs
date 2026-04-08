using EventForge.Hardware.Exceptions;

namespace EventForge.Hardware.Interfaces;

/// <summary>
/// Abstraction for the low-level communication channel with a Custom fiscal printer.
/// Implementations live in <c>EventForge.Server</c>:
/// <list type="bullet">
///   <item><c>CustomTcpCommunication</c> – direct TCP/IP from server</item>
///   <item><c>CustomSerialCommunication</c> – RS-232 serial from server</item>
///   <item><c>AgentProxyCommunication</c> – USB via UpdateAgent HTTP proxy</item>
///   <item><c>AgentTcpProxyCommunication</c> – TCP via UpdateAgent HTTP proxy</item>
/// </list>
/// </summary>
public interface ICustomPrinterCommunication : IAsyncDisposable
{
    /// <summary>
    /// Sends a binary command frame to the printer and waits for the response.
    /// </summary>
    /// <param name="command">Raw binary command frame.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The raw binary response received from the printer (ACK, NAK, or data frame).</returns>
    /// <exception cref="FiscalPrinterCommunicationException">
    /// Thrown when the connection fails or times out after all retry attempts.
    /// </exception>
    Task<byte[]> SendCommandAsync(byte[] command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to the printer by sending an ENQ enquiry frame.
    /// </summary>
    /// <exception cref="FiscalPrinterCommunicationException">
    /// Thrown when the printer is unreachable or does not respond.
    /// </exception>
    Task TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>Disconnects from the printer and releases underlying resources.</summary>
    Task DisconnectAsync();

    /// <summary>Gets a value indicating whether the channel is currently connected.</summary>
    bool IsConnected { get; }
}
