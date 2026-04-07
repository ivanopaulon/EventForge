namespace Prym.Agent.Services;

/// <summary>
/// Communicates with a USB-attached printer on the local Windows machine by opening
/// the raw device path (<c>\\.\{deviceId}</c>) as a <see cref="FileStream"/> and
/// writing/reading raw bytes directly.
/// </summary>
/// <remarks>
/// USB thermal printers on Windows expose a kernel device object such as
/// <c>\\.\USB001</c>. Opening it with <c>FileAccess.ReadWrite</c> and
/// <c>FileShare.ReadWrite</c> allows sending ESC/POS or Custom protocol frames
/// without a printer driver.
/// </remarks>
public sealed class AgentPrinterService(ILogger<AgentPrinterService> logger) : IAgentPrinterService
{
    // -------------------------------------------------------------------------
    //  Constants
    // -------------------------------------------------------------------------

    private const string DevicePathPrefix = @"\\.\";
    private const int ReadBufferSize = 4096;
    private const int DefaultReadTimeoutMs = 5_000;

    // -------------------------------------------------------------------------
    //  IAgentPrinterService
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<byte[]> SendCommandAsync(
        string deviceId,
        byte[] command,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ArgumentNullException.ThrowIfNull(command);

        var path = BuildDevicePath(deviceId);

        logger.LogDebug(
            "AgentPrinterService → {Path} | {Bytes} bytes",
            path, command.Length);

        await using var stream = OpenDeviceStream(path);

        await stream.WriteAsync(command, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);

        var response = await ReadResponseAsync(stream, path, ct).ConfigureAwait(false);

        logger.LogDebug(
            "AgentPrinterService ← {Path} | {Bytes} bytes",
            path, response.Length);

        return response;
    }

    /// <inheritdoc />
    public async Task TestConnectionAsync(string deviceId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        var path = BuildDevicePath(deviceId);

        // Attempt to open the device; if it throws, the device is not accessible.
        await using var stream = OpenDeviceStream(path);

        logger.LogInformation(
            "AgentPrinterService: connection test successful for {Path}", path);

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ListDevices()
    {
        var found = new List<string>();

        for (int i = 1; i <= 9; i++)
        {
            var suffix = $"USB00{i}";
            var path = BuildDevicePath(suffix);

            try
            {
                // Try opening; if it succeeds the device is present.
                using var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Write,
                    FileShare.ReadWrite,
                    bufferSize: 1,
                    FileOptions.None);

                found.Add(suffix);

                logger.LogDebug("AgentPrinterService: discovered device {Path}", path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Device not present or not accessible — skip silently.
            }
        }

        return found.AsReadOnly();
    }

    // -------------------------------------------------------------------------
    //  Private helpers
    // -------------------------------------------------------------------------

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
            throw new InvalidOperationException(
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
            // Timeout or cancellation — return empty response (printer sent nothing).
            return [];
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                $"Error reading response from USB device '{path}': {ex.Message}", ex);
        }

        return bytesRead == 0 ? [] : buffer[..bytesRead];
    }
}
