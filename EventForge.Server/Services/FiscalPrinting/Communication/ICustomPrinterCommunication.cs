namespace EventForge.Server.Services.FiscalPrinting.Communication;

/// <summary>
/// Abstraction for the low-level communication channel with a Custom fiscal printer.
/// Implementations: <see cref="CustomTcpCommunication"/> (TCP/IP) and
/// <see cref="CustomSerialCommunication"/> (RS-232 serial port).
/// </summary>
public interface ICustomPrinterCommunication : IAsyncDisposable
{
    /// <summary>
    /// Sends a binary command frame to the printer and waits for the response.
    /// </summary>
    /// <param name="command">
    /// The raw binary command frame produced by
    /// <see cref="CustomProtocol.CustomCommandBuilder"/>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The raw binary response received from the printer (ACK, NAK, or data frame).</returns>
    /// <exception cref="FiscalPrinterCommunicationException">
    /// Thrown when the connection fails or times out after all retry attempts.
    /// </exception>
    Task<byte[]> SendCommandAsync(byte[] command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to the printer by sending an ENQ enquiry frame
    /// (<see cref="CustomProtocol.CustomProtocolCommands.ENQ"/>).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="FiscalPrinterCommunicationException">
    /// Thrown when the printer is unreachable or does not respond.
    /// </exception>
    Task TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the printer and releases underlying resources.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>Gets a value indicating whether the channel is currently connected.</summary>
    bool IsConnected { get; }
}
