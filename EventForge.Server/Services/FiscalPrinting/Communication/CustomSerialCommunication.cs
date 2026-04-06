using System.IO.Ports;
using EventForge.Server.Services.FiscalPrinting.CustomProtocol;

namespace EventForge.Server.Services.FiscalPrinting.Communication;

/// <summary>
/// RS-232 serial port communication channel for Custom fiscal printers.
/// Wraps <see cref="SerialPort"/> with configurable baud rate, parity, stop bits,
/// and data bits. Supports synchronous send/receive on a dedicated serial line.
/// </summary>
/// <remarks>
/// Dispose the instance after use to release the serial port.
/// Do not share one instance across concurrent callers – serial ports are inherently single-threaded.
/// </remarks>
public sealed class CustomSerialCommunication : ICustomPrinterCommunication
{
    // -------------------------------------------------------------------------
    //  Constants
    // -------------------------------------------------------------------------

    private const int DefaultBaudRate = 9600;
    private const int DefaultReadTimeoutMs = 5_000;
    private const int DefaultWriteTimeoutMs = 5_000;
    private const int ReadBufferSize = 4096;

    // -------------------------------------------------------------------------
    //  Fields
    // -------------------------------------------------------------------------

    private readonly ILogger<CustomSerialCommunication> _logger;
    private readonly SerialPort _serialPort;
    private bool _disposed;

    // -------------------------------------------------------------------------
    //  Constructor
    // -------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new serial communication channel.
    /// </summary>
    /// <param name="portName">Serial port identifier (e.g., "COM1", "/dev/ttyUSB0").</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="baudRate">Baud rate (default: 9600). Supported: 9600, 19200, 38400, 115200.</param>
    /// <param name="parity">Parity check mode (default: <see cref="Parity.None"/>).</param>
    /// <param name="dataBits">Number of data bits per byte (default: 8).</param>
    /// <param name="stopBits">Stop bits configuration (default: <see cref="StopBits.One"/>).</param>
    /// <param name="readTimeoutMs">Read timeout in milliseconds (default: 5 000).</param>
    /// <param name="writeTimeoutMs">Write timeout in milliseconds (default: 5 000).</param>
    public CustomSerialCommunication(
        string portName,
        ILogger<CustomSerialCommunication> logger,
        int baudRate = DefaultBaudRate,
        Parity parity = Parity.None,
        int dataBits = 8,
        StopBits stopBits = StopBits.One,
        int readTimeoutMs = DefaultReadTimeoutMs,
        int writeTimeoutMs = DefaultWriteTimeoutMs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portName);

        _logger = logger;

        _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
        {
            ReadTimeout = readTimeoutMs,
            WriteTimeout = writeTimeoutMs,
            Encoding = System.Text.Encoding.Latin1,
            DtrEnable = false,
            RtsEnable = false
        };
    }

    // -------------------------------------------------------------------------
    //  ICustomPrinterCommunication
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public bool IsConnected => _serialPort.IsOpen && !_disposed;

    /// <inheritdoc />
    public async Task<byte[]> SendCommandAsync(byte[] command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ObjectDisposedException.ThrowIf(_disposed, this);

        EnsureOpen();

        _logger.LogDebug(
            "Serial → {Port} | {Bytes} bytes | {Hex}",
            _serialPort.PortName, command.Length, ToHex(command));

        try
        {
            _serialPort.Write(command, 0, command.Length);
        }
        catch (Exception ex) when (ex is InvalidOperationException or TimeoutException or IOException)
        {
            throw new FiscalPrinterCommunicationException(
                $"Write error on serial port {_serialPort.PortName}: {ex.Message}", ex);
        }

        return await ReadResponseAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        byte[] enq = [CustomProtocolCommands.ENQ];
        _ = await SendCommandAsync(enq, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Serial connection test successful on port {Port}", _serialPort.PortName);
    }

    /// <inheritdoc />
    public Task DisconnectAsync()
    {
        if (_serialPort.IsOpen)
        {
            _serialPort.Close();
            _logger.LogDebug("Serial port {Port} closed", _serialPort.PortName);
        }
        return Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    //  IAsyncDisposable
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            try
            {
                if (_serialPort.IsOpen) _serialPort.Close();
            }
            catch { /* intentionally swallowed */ }
            _serialPort.Dispose();
        }
        return ValueTask.CompletedTask;
    }

    // -------------------------------------------------------------------------
    //  Private helpers
    // -------------------------------------------------------------------------

    private void EnsureOpen()
    {
        if (_serialPort.IsOpen) return;

        try
        {
            _serialPort.Open();
            _logger.LogDebug(
                "Serial port {Port} opened at {BaudRate} baud",
                _serialPort.PortName, _serialPort.BaudRate);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or InvalidOperationException)
        {
            throw new FiscalPrinterCommunicationException(
                $"Cannot open serial port {_serialPort.PortName}: {ex.Message}", ex);
        }
    }

    private async Task<byte[]> ReadResponseAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[ReadBufferSize];
        int totalRead = 0;

        try
        {
            // First byte: must be ACK, NAK, or STX
            await Task.Run(() =>
            {
                int b = _serialPort.ReadByte();
                if (b < 0)
                {
                    throw new FiscalPrinterCommunicationException(
                        $"Serial port {_serialPort.PortName} returned no data.");
                }
                buffer[totalRead] = (byte)b;
            }, cancellationToken).ConfigureAwait(false);

            totalRead = 1;
            byte first = buffer[0];

            // ACK (0x06) and NAK (0x15) are single-byte responses
            if (first == CustomProtocolCommands.ACK || first == CustomProtocolCommands.NAK)
            {
                var singleByteResp = buffer[..1];
                _logger.LogDebug(
                    "Serial ← {Port} | 1 byte | {Hex}", _serialPort.PortName, ToHex(singleByteResp));
                return singleByteResp;
            }

            // STX frame: read until ETX + checksum
            if (first == CustomProtocolCommands.STX)
            {
                await Task.Run(() =>
                {
                    while (totalRead < ReadBufferSize)
                    {
                        int b = _serialPort.ReadByte();
                        if (b < 0) break;
                        buffer[totalRead++] = (byte)b;
                        // ETX followed by checksum = frame complete (ETX at [^2])
                        if (buffer[totalRead - 2] == CustomProtocolCommands.ETX) break;
                    }
                }, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (TimeoutException ex)
        {
            throw new FiscalPrinterCommunicationException(
                $"Timeout reading response from serial port {_serialPort.PortName}.", ex);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            throw new FiscalPrinterCommunicationException(
                $"Read error on serial port {_serialPort.PortName}: {ex.Message}", ex);
        }

        var response = buffer[..totalRead];
        _logger.LogDebug(
            "Serial ← {Port} | {Bytes} bytes | {Hex}",
            _serialPort.PortName, totalRead, ToHex(response));

        return response;
    }

    private static string ToHex(byte[] data)
    {
        if (data.Length > 32) return $"{BitConverter.ToString(data[..32]).Replace("-", " ")} ...";
        return BitConverter.ToString(data).Replace("-", " ");
    }
}
