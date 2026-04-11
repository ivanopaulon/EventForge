using EventForge.Hardware.Exceptions;
using System.Net.Sockets;

namespace Prym.Agent.Services;

/// <summary>
/// Communicates with printers on the local machine or local network on behalf of the server.
/// Supports:
/// <list type="bullet">
///   <item>USB-attached printers via raw device I/O (<c>\\.\USB00x</c>).</item>
///   <item>TCP/IP network printers via raw socket (<c>TcpViaAgent</c> connection type).</item>
///   <item>HTTP forwarding for WebAPI-based printers (Epson ePOS-Print, <c>TcpViaAgent</c>).</item>
/// </list>
/// </summary>
public sealed class AgentPrinterService(
    ILogger<AgentPrinterService> logger,
    IHttpClientFactory httpClientFactory) : IAgentPrinterService
{
    // ── Constants ──────────────────────────────────────────────────────────────

    private const string DevicePathPrefix   = @"\\.\";
    private const int ReadBufferSize        = 4096;
    private const int DefaultReadTimeoutMs  = 5_000;
    private const int TcpConnectTimeoutMs   = 10_000;

    // ── IAgentPrinterService – USB ─────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<byte[]> SendCommandAsync(
        string deviceId,
        byte[] command,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ArgumentNullException.ThrowIfNull(command);

        var path = BuildDevicePath(deviceId);

        logger.LogDebug("[AgentPrinterService] USB → {Path} | {Bytes} bytes", path, command.Length);

        await using var stream = OpenDeviceStream(path);

        await stream.WriteAsync(command, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);

        var response = await ReadResponseAsync(stream, path, ct).ConfigureAwait(false);

        logger.LogDebug("[AgentPrinterService] USB ← {Path} | {Bytes} bytes", path, response.Length);

        return response;
    }

    /// <inheritdoc />
    public async Task TestConnectionAsync(string deviceId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        var path = BuildDevicePath(deviceId);

        // Attempt to open the device; if it throws, the device is not accessible.
        await using var stream = OpenDeviceStream(path);

        logger.LogInformation("[AgentPrinterService] USB connection test OK | {Path}", path);

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ListDevices()
    {
        var found = new List<string>();

        for (var i = 1; i <= 9; i++)
        {
            var suffix = $"USB00{i}";
            var path   = BuildDevicePath(suffix);

            try
            {
                using var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Write,
                    FileShare.ReadWrite,
                    bufferSize: 1,
                    FileOptions.None);

                found.Add(suffix);

                logger.LogDebug("[AgentPrinterService] USB device discovered: {Path}", path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Device not present or not accessible — skip silently.
            }
        }

        return found.AsReadOnly();
    }

    // ── IAgentPrinterService – TCP (TcpViaAgent) ───────────────────────────────

    /// <inheritdoc />
    public async Task<byte[]> SendTcpCommandAsync(
        string host,
        int port,
        byte[] command,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        ArgumentNullException.ThrowIfNull(command);

        logger.LogDebug(
            "[AgentPrinterService] TCP → {Host}:{Port} | {Bytes} bytes",
            host, port, command.Length);

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TcpConnectTimeoutMs);

            using var tcpClient = new TcpClient();
            tcpClient.ReceiveTimeout = DefaultReadTimeoutMs;
            tcpClient.SendTimeout    = TcpConnectTimeoutMs;

            await tcpClient.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);

            await using var stream = tcpClient.GetStream();
            await stream.WriteAsync(command, ct).ConfigureAwait(false);
            await stream.FlushAsync(ct).ConfigureAwait(false);

            var response = await ReadTcpResponseAsync(stream, host, port, ct).ConfigureAwait(false);

            logger.LogDebug(
                "[AgentPrinterService] TCP ← {Host}:{Port} | {Bytes} bytes",
                host, port, response.Length);

            return response;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new FiscalPrinterCommunicationException(
                $"TCP connection to printer at '{host}:{port}' timed out after {TcpConnectTimeoutMs} ms.");
        }
        catch (SocketException ex)
        {
            throw new FiscalPrinterCommunicationException(
                $"Cannot connect to TCP printer at '{host}:{port}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task TestTcpConnectionAsync(string host, int port, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TcpConnectTimeoutMs);

            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);

            logger.LogInformation(
                "[AgentPrinterService] TCP connection test OK | {Host}:{Port}", host, port);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new FiscalPrinterCommunicationException(
                $"TCP connection test to '{host}:{port}' timed out after {TcpConnectTimeoutMs} ms.");
        }
        catch (SocketException ex)
        {
            throw new FiscalPrinterCommunicationException(
                $"TCP connection test to '{host}:{port}' failed: {ex.Message}", ex);
        }
    }

    // ── IAgentPrinterService – HTTP forward (Epson WebAPI, TcpViaAgent) ────────

    /// <inheritdoc />
    public async Task<string> ForwardHttpAsync(
        string targetUrl,
        string contentType,
        string body,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentNullException.ThrowIfNull(body);

        logger.LogDebug(
            "[AgentPrinterService] HTTP forward → {Url} | contentType={ContentType} | {Chars} chars",
            targetUrl, contentType, body.Length);

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            // Build content with raw UTF-8 bytes and set the full Content-Type value
            // (which may include "; charset=utf-8") without StringContent appending
            // a duplicate charset parameter.
            var bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);
            using var content = new ByteArrayContent(bodyBytes);
            content.Headers.ContentType =
                System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);

            using var response = await client
                .PostAsync(targetUrl, content, ct)
                .ConfigureAwait(false);

            var responseBody = await response.Content
                .ReadAsStringAsync(ct)
                .ConfigureAwait(false);

            logger.LogDebug(
                "[AgentPrinterService] HTTP forward ← {Url} | HTTP {Status} | {Chars} chars",
                targetUrl, (int)response.StatusCode, responseBody.Length);

            if (!response.IsSuccessStatusCode)
            {
                throw new FiscalPrinterCommunicationException(
                    $"HTTP forward to '{targetUrl}' returned HTTP {(int)response.StatusCode}: " +
                    $"{responseBody[..Math.Min(200, responseBody.Length)]}");
            }

            return responseBody;
        }
        catch (FiscalPrinterCommunicationException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new FiscalPrinterCommunicationException(
                $"HTTP forward to '{targetUrl}' failed: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new FiscalPrinterCommunicationException(
                $"HTTP forward to '{targetUrl}' timed out.", ex);
        }
    }

    // ── Private helpers – USB ──────────────────────────────────────────────────

    private static string BuildDevicePath(string deviceId) =>
        deviceId.StartsWith(DevicePathPrefix, StringComparison.OrdinalIgnoreCase)
            ? deviceId
            : $"{DevicePathPrefix}{deviceId}";

    private static FileStream OpenDeviceStream(string path)
    {
        try
        {
            return new FileStream(
                path,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.ReadWrite,
                bufferSize: ReadBufferSize,
                FileOptions.None);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new FiscalPrinterCommunicationException(
                $"Cannot open USB printer device at '{path}': {ex.Message}", ex);
        }
    }

    private static async Task<byte[]> ReadResponseAsync(
        FileStream stream,
        string path,
        CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(DefaultReadTimeoutMs);

        var buffer = new byte[ReadBufferSize];
        int bytesRead;

        try
        {
            bytesRead = await stream.ReadAsync(buffer, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Timeout or cancellation — printer sent nothing; return empty response.
            return [];
        }
        catch (IOException ex)
        {
            throw new FiscalPrinterCommunicationException(
                $"Error reading response from USB device '{path}': {ex.Message}", ex);
        }

        return bytesRead == 0 ? [] : buffer[..bytesRead];
    }

    // ── Private helpers – TCP ──────────────────────────────────────────────────

    private static async Task<byte[]> ReadTcpResponseAsync(
        NetworkStream stream,
        string host,
        int port,
        CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(DefaultReadTimeoutMs);

        var buffer = new byte[ReadBufferSize];
        int bytesRead;

        try
        {
            bytesRead = await stream.ReadAsync(buffer, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Timeout — printer sent nothing; return empty response.
            return [];
        }
        catch (IOException ex)
        {
            throw new FiscalPrinterCommunicationException(
                $"Error reading response from TCP printer at '{host}:{port}': {ex.Message}", ex);
        }

        return bytesRead == 0 ? [] : buffer[..bytesRead];
    }
}
