using System.Net.Sockets;
using EventForge.Server.Services.FiscalPrinting.CustomProtocol;
using Polly;
using Polly.Retry;

namespace EventForge.Server.Services.FiscalPrinting.Communication;

/// <summary>
/// TCP/IP communication channel for Custom fiscal printers.
/// Manages a <see cref="TcpClient"/> connection with a configurable timeout,
/// automatic retry (Polly exponential back-off, 3 attempts), and safe disposal.
/// </summary>
/// <remarks>
/// Each <see cref="CustomTcpCommunication"/> instance owns a single TCP connection.
/// Create a new instance per print operation or reuse across operations within the
/// same service call, then dispose when done.
/// </remarks>
public sealed class CustomTcpCommunication : ICustomPrinterCommunication
{
    // -------------------------------------------------------------------------
    //  Constants
    // -------------------------------------------------------------------------

    private const int DefaultTimeoutMs = 30_000;
    private const int ReadBufferSize = 4096;
    private const int MaxRetryAttempts = 3;

    // -------------------------------------------------------------------------
    //  Fields
    // -------------------------------------------------------------------------

    private readonly string _host;
    private readonly int _port;
    private readonly int _timeoutMs;
    private readonly ILogger<CustomTcpCommunication> _logger;
    private readonly ResiliencePipeline _retryPipeline;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private bool _disposed;

    // -------------------------------------------------------------------------
    //  Constructor
    // -------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new TCP communication channel.
    /// </summary>
    /// <param name="host">Printer IP address or hostname.</param>
    /// <param name="port">TCP port (default for Custom printers: 9100).</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="timeoutMs">Connection and read/write timeout in milliseconds (default: 30 000).</param>
    public CustomTcpCommunication(
        string host,
        int port,
        ILogger<CustomTcpCommunication> logger,
        int timeoutMs = DefaultTimeoutMs)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);

        _host = host;
        _port = port;
        _timeoutMs = timeoutMs;
        _logger = logger;

        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = MaxRetryAttempts,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<SocketException>()
                                                     .Handle<FiscalPrinterCommunicationException>()
                                                     .Handle<IOException>(),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "TCP retry {Attempt}/{Max} for {Host}:{Port} – {Reason}",
                        args.AttemptNumber + 1, MaxRetryAttempts, _host, _port,
                        args.Outcome.Exception?.Message ?? "unknown error");
                    // Re-create client for next attempt
                    DisposeClientResources();
                    return default;
                }
            })
            .Build();
    }

    // -------------------------------------------------------------------------
    //  ICustomPrinterCommunication
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public bool IsConnected => _client?.Connected == true && !_disposed;

    /// <inheritdoc />
    public async Task<byte[]> SendCommandAsync(byte[] command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ObjectDisposedException.ThrowIf(_disposed, this);

        return await _retryPipeline.ExecuteAsync(async ct =>
        {
            await EnsureConnectedAsync(ct).ConfigureAwait(false);

            _logger.LogDebug(
                "TCP → {Host}:{Port} | {Bytes} bytes | {Hex}",
                _host, _port, command.Length, ToHex(command));

            await _stream!.WriteAsync(command, ct).ConfigureAwait(false);
            await _stream.FlushAsync(ct).ConfigureAwait(false);

            return await ReadResponseAsync(ct).ConfigureAwait(false);

        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        byte[] enq = [CustomProtocolCommands.ENQ];
        _ = await SendCommandAsync(enq, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("TCP connection test successful for {Host}:{Port}", _host, _port);
    }

    /// <inheritdoc />
    public Task DisconnectAsync()
    {
        DisposeClientResources();
        _logger.LogDebug("TCP disconnected from {Host}:{Port}", _host, _port);
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
            DisposeClientResources();
            _disposed = true;
        }
        return ValueTask.CompletedTask;
    }

    // -------------------------------------------------------------------------
    //  Private helpers
    // -------------------------------------------------------------------------

    private async Task EnsureConnectedAsync(CancellationToken ct)
    {
        if (_client?.Connected == true) return;

        DisposeClientResources();
        _client = new TcpClient { ReceiveTimeout = _timeoutMs, SendTimeout = _timeoutMs };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_timeoutMs);

        try
        {
            await _client.ConnectAsync(_host, _port, cts.Token).ConfigureAwait(false);
            _stream = _client.GetStream();
            _stream.ReadTimeout = _timeoutMs;
            _stream.WriteTimeout = _timeoutMs;

            _logger.LogDebug("TCP connected to {Host}:{Port}", _host, _port);
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException or IOException)
        {
            DisposeClientResources();
            throw new FiscalPrinterCommunicationException(
                $"Cannot connect to fiscal printer at {_host}:{_port}: {ex.Message}", ex);
        }
    }

    private async Task<byte[]> ReadResponseAsync(CancellationToken ct)
    {
        var buffer = new byte[ReadBufferSize];

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_timeoutMs);

        int bytesRead;
        try
        {
            bytesRead = await _stream!.ReadAsync(buffer, cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or OperationCanceledException)
        {
            throw new FiscalPrinterCommunicationException(
                $"Timeout or error reading response from {_host}:{_port}: {ex.Message}", ex);
        }

        if (bytesRead == 0)
        {
            throw new FiscalPrinterCommunicationException(
                $"Printer at {_host}:{_port} closed the connection without sending a response.");
        }

        var response = buffer[..bytesRead];
        _logger.LogDebug(
            "TCP ← {Host}:{Port} | {Bytes} bytes | {Hex}",
            _host, _port, bytesRead, ToHex(response));

        return response;
    }

    private void DisposeClientResources()
    {
        try { _stream?.Dispose(); } catch { /* intentionally swallowed */ }
        try { _client?.Dispose(); } catch { /* intentionally swallowed */ }
        _stream = null;
        _client = null;
    }

    private static string ToHex(byte[] data)
    {
        if (data.Length > 32) return $"{BitConverter.ToString(data[..32]).Replace("-", " ")} ...";
        return BitConverter.ToString(data).Replace("-", " ");
    }
}
