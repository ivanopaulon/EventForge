using Prym.Hardware.Exceptions;
using System.IO.Ports;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

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
    public Task TestConnectionAsync(string deviceId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        var path = BuildDevicePath(deviceId);

        // Attempt to open the device; if it throws, the device is not accessible.
        // OpenDeviceStream is synchronous — dispose immediately after the check.
        OpenDeviceStream(path).Dispose();

        logger.LogInformation("[AgentPrinterService] USB connection test OK | {Path}", path);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ListDevices()
    {
        var found = new System.Collections.Concurrent.ConcurrentBag<(int Index, string Suffix)>();

        Parallel.For(1, 100, i =>
        {
            var suffix = $"USB{i:D3}";
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

                found.Add((i, suffix));

                logger.LogDebug("[AgentPrinterService] USB device discovered: {Path}", path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Device not present or not accessible — skip silently.
            }
        });

        return found.OrderBy(x => x.Index).Select(x => x.Suffix).ToList().AsReadOnly();
    }

    // ── IAgentPrinterService – OS-level device enumeration ────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListSystemPrintersAsync(CancellationToken ct = default)
    {
        try
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? await GetWindowsPrintersAsync(ct).ConfigureAwait(false)
                : await GetLinuxPrintersAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[AgentPrinterService] Failed to enumerate system printers");
            return [];
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListSerialPortsAsync(CancellationToken ct = default)
    {
        IReadOnlyList<string> result;
        try
        {
            result = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? SerialPort.GetPortNames().OrderBy(p => p).ToArray()
                : GetLinuxSerialPorts();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[AgentPrinterService] Failed to enumerate serial ports");
            result = [];
        }
        return Task.FromResult(result);
    }

    // ── IAgentPrinterService – Test print ─────────────────────────────────────

    /// <inheritdoc />
    public async Task<bool> SendTestPrintAsync(string printerName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(printerName);

        var receipt = BuildTestReceiptText(printerName);

        try
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? await SendWindowsTestPrintAsync(printerName, receipt, ct).ConfigureAwait(false)
                : await SendLinuxTestPrintAsync(printerName, receipt, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[AgentPrinterService] Test print failed for '{Printer}'", printerName);
            return false;
        }
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

    // ── Private helpers – OS printer & serial enumeration ─────────────────────

    private static async Task<IReadOnlyList<string>> GetWindowsPrintersAsync(CancellationToken ct)
    {
        var printers = new List<string>();

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-NoProfile -NonInteractive -Command " +
                        "\"Get-Printer | Select-Object -ExpandProperty Name\"",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        using var proc = System.Diagnostics.Process.Start(psi);
        if (proc is not null)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var stdoutTask = proc.StandardOutput.ReadToEndAsync(cts.Token);
            var stderrTask = proc.StandardError.ReadToEndAsync(cts.Token);
            await proc.WaitForExitAsync(cts.Token);
            var output = await stdoutTask;
            await stderrTask;

            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var name = line.Trim();
                if (!string.IsNullOrEmpty(name))
                    printers.Add(name);
            }
        }

        return printers;
    }

    private static async Task<IReadOnlyList<string>> GetLinuxPrintersAsync(CancellationToken ct)
    {
        var printers = new List<string>();

        var psi = new System.Diagnostics.ProcessStartInfo("lpstat", "-a")
        {
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        using var proc = System.Diagnostics.Process.Start(psi);
        if (proc is not null)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            var stdoutTask = proc.StandardOutput.ReadToEndAsync(cts.Token);
            var stderrTask = proc.StandardError.ReadToEndAsync(cts.Token);
            await proc.WaitForExitAsync(cts.Token);
            var output = await stdoutTask;
            await stderrTask;

            // lpstat -a format: "PrinterName accepting requests since ..."
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                    printers.Add(parts[0]);
            }
        }

        return printers;
    }

    private static IReadOnlyList<string> GetLinuxSerialPorts()
    {
        // Enumerate well-known serial device patterns under /dev/:
        // ttyS=standard serial, ttyUSB=USB-serial adapters, ttyACM=USB CDC ACM, ttyAMA=ARM UART.
        var patterns = new[] { "ttyS", "ttyUSB", "ttyACM", "ttyAMA" };
        var found = new List<string>();

        foreach (var pattern in patterns)
        {
            var dir = new DirectoryInfo("/dev");
            if (!dir.Exists) break;

            foreach (var fi in dir.EnumerateFiles($"{pattern}*"))
                found.Add(fi.FullName);
        }

        found.Sort(StringComparer.OrdinalIgnoreCase);
        return found;
    }

    // ── Private helpers – Test print ──────────────────────────────────────────

    private static string BuildTestReceiptText(string printerName)
    {
        var now = DateTime.Now;
        var sb  = new StringBuilder();

        sb.AppendLine("================================");
        sb.AppendLine("       STAMPA DI PROVA");
        sb.AppendLine("    EventForge Update Agent");
        sb.AppendLine("================================");
        sb.AppendLine();
        sb.AppendLine($"Data:  {now:dd/MM/yyyy}");
        sb.AppendLine($"Ora:   {now:HH:mm:ss}");
        sb.AppendLine($"PC:    {Environment.MachineName}");
        sb.AppendLine();
        sb.AppendLine("--------------------------------");
        sb.AppendLine("1x Prodotto Test A      € 10,00");
        sb.AppendLine("1x Prodotto Test B      €  5,50");
        sb.AppendLine("1x Prodotto Test C      €  3,00");
        sb.AppendLine("--------------------------------");
        sb.AppendLine("Subtotale:              € 18,50");
        sb.AppendLine("  di cui IVA (22%):     €  3,34");
        sb.AppendLine("================================");
        sb.AppendLine("TOTALE:                 € 18,50");
        sb.AppendLine("================================");
        sb.AppendLine("Pagamento: CONTANTI     € 20,00");
        sb.AppendLine("Resto:                  €  1,50");
        sb.AppendLine("================================");
        sb.AppendLine();
        sb.AppendLine("Stampante:");
        sb.AppendLine(printerName.Length > 32 ? printerName[..32] : printerName);
        sb.AppendLine();
        sb.AppendLine("  Stampa di prova riuscita!");
        sb.AppendLine("================================");
        sb.AppendLine();

        return sb.ToString();
    }

    private async Task<bool> SendWindowsTestPrintAsync(
        string printerName, string receipt, CancellationToken ct)
    {
        // Write the receipt to a unique temp file; pass both paths via environment variables
        // so that neither the printer name nor the file path can cause shell injection.
        var tempFile = Path.Combine(Path.GetTempPath(), $"ef-testprint-{Guid.NewGuid():N}.txt");
        try
        {
            await File.WriteAllTextAsync(tempFile, receipt, Encoding.UTF8, ct).ConfigureAwait(false);

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName  = "powershell.exe",
                Arguments = "-NoProfile -NonInteractive -Command " +
                            "\"Get-Content -Path $env:EF_PRINT_FILE -Encoding UTF8 | " +
                            "Out-Printer -Name $env:EF_PRINTER_NAME\"",
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            psi.Environment["EF_PRINT_FILE"]    = tempFile;
            psi.Environment["EF_PRINTER_NAME"]  = printerName;

            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc is null) return false;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));

            var stderrTask = proc.StandardError.ReadToEndAsync(cts.Token);
            var stdoutTask = proc.StandardOutput.ReadToEndAsync(cts.Token);
            await proc.WaitForExitAsync(cts.Token).ConfigureAwait(false);
            var stderr = await stderrTask;
            await stdoutTask;

            if (proc.ExitCode != 0)
            {
                logger.LogWarning(
                    "[AgentPrinterService] Test print failed for '{Printer}' (exit {Code}): {Stderr}",
                    printerName, proc.ExitCode, stderr);
                return false;
            }

            logger.LogInformation(
                "[AgentPrinterService] Test print sent to '{Printer}'", printerName);
            return true;
        }
        finally
        {
            try { File.Delete(tempFile); } catch { /* best-effort temp cleanup */ }
        }
    }

    private async Task<bool> SendLinuxTestPrintAsync(
        string printerName, string receipt, CancellationToken ct)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"ef-testprint-{Guid.NewGuid():N}.txt");
        try
        {
            await File.WriteAllTextAsync(tempFile, receipt, Encoding.UTF8, ct).ConfigureAwait(false);

            var psi = new System.Diagnostics.ProcessStartInfo("lp")
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            psi.ArgumentList.Add("-d");
            psi.ArgumentList.Add(printerName);
            psi.ArgumentList.Add(tempFile);

            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc is null) return false;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var stderrTask = proc.StandardError.ReadToEndAsync(cts.Token);
            var stdoutTask = proc.StandardOutput.ReadToEndAsync(cts.Token);
            await proc.WaitForExitAsync(cts.Token).ConfigureAwait(false);
            var stderr = await stderrTask;
            await stdoutTask;

            if (proc.ExitCode != 0)
            {
                logger.LogWarning(
                    "[AgentPrinterService] Test print (Linux) failed for '{Printer}' (exit {Code}): {Stderr}",
                    printerName, proc.ExitCode, stderr);
                return false;
            }

            logger.LogInformation(
                "[AgentPrinterService] Test print sent to '{Printer}'", printerName);
            return true;
        }
        finally
        {
            try { File.Delete(tempFile); } catch { /* best-effort temp cleanup */ }
        }
    }
}
