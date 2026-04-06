using EventForge.DTOs.FiscalPrinting;
using EventForge.Server.Services.FiscalPrinting.Communication;
using EventForge.Server.Services.FiscalPrinting.CustomProtocol;
using System.Net.Sockets;

namespace EventForge.Server.Services.FiscalPrinting;

public partial class CustomFiscalPrinterService
{
    // -------------------------------------------------------------------------
    //  Ad-hoc connection tests (used by wizard – no DB record required)
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<FiscalPrintResult> TestTcpConnectionAsync(
        string ipAddress,
        int port,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var comm = new CustomTcpCommunication(
                ipAddress, port, loggerFactory.CreateLogger<CustomTcpCommunication>());

            await comm.TestConnectionAsync(cancellationToken);
            return new FiscalPrintResult { Success = true, PrintDate = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            return new FiscalPrintResult { Success = false, ErrorMessage = ex.Message, PrintDate = DateTime.UtcNow };
        }
    }

    /// <inheritdoc />
    public async Task<FiscalPrintResult> TestSerialConnectionAsync(
        string serialPortName,
        int baudRate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var comm = new CustomSerialCommunication(
                serialPortName,
                loggerFactory.CreateLogger<CustomSerialCommunication>(),
                baudRate);

            await comm.TestConnectionAsync(cancellationToken);
            return new FiscalPrintResult { Success = true, PrintDate = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            return new FiscalPrintResult { Success = false, ErrorMessage = ex.Message, PrintDate = DateTime.UtcNow };
        }
    }

    // -------------------------------------------------------------------------
    //  Printer info by address (wizard Step 3)
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<FiscalPrinterInfoDto> GetPrinterInfoByAddressAsync(
        string ipAddress,
        int port,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var comm = new CustomTcpCommunication(
                ipAddress, port, loggerFactory.CreateLogger<CustomTcpCommunication>());

            // Send CMD_READ_STATUS ("10") and parse response
            var statusCmd = CustomCommandBuilder.StatusRequest();
            var rawResponse = await comm.SendCommandAsync(statusCmd, cancellationToken);
            await comm.DisconnectAsync();

            if (rawResponse.Length == 0)
                return new FiscalPrinterInfoDto { IsOnline = false, ErrorMessage = "No response from printer" };

            var parsed = CustomResponseParser.ParseResponse(rawResponse);
            if (parsed.Type == CustomResponseType.Nak || parsed.Type == CustomResponseType.Unknown)
                return new FiscalPrinterInfoDto { IsOnline = false, ErrorMessage = "Printer returned NAK or unknown response" };

            // Best-effort: parse status bytes if a Data frame was returned
            var isOnline = parsed.Type == CustomResponseType.Ack || parsed.Type == CustomResponseType.Data;
            bool memoryAlmostFull = false, memoryFull = false;

            if (parsed.Type == CustomResponseType.Data && !string.IsNullOrEmpty(parsed.Data))
            {
                var statusBytes = System.Text.Encoding.ASCII.GetBytes(parsed.Data);
                if (statusBytes.Length >= 3)
                {
                    var status = CustomStatusParser.Parse(statusBytes);
                    memoryAlmostFull = status.IsFiscalMemoryAlmostFull;
                    memoryFull = status.IsFiscalMemoryFull;
                }
            }

            return new FiscalPrinterInfoDto
            {
                IsOnline = isOnline,
                FiscalMemoryUsedPercent = memoryFull ? 100m : memoryAlmostFull ? 92m : null,
                PrinterDateTime = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GetPrinterInfoByAddressAsync failed for {IpAddress}:{Port}", ipAddress, port);
            return new FiscalPrinterInfoDto { IsOnline = false, ErrorMessage = ex.Message };
        }
    }

    // -------------------------------------------------------------------------
    //  Network scan (wizard Step 2A)
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<List<NetworkScanResultDto>> ScanNetworkAsync(
        string subnetPrefix,
        int port = 9100,
        int timeoutMs = 300,
        CancellationToken cancellationToken = default)
    {
        var results = new System.Collections.Concurrent.ConcurrentBag<NetworkScanResultDto>();

        // Probe .1 to .254 in parallel, capped at 50 concurrent connections
        using var semaphore = new SemaphoreSlim(50);
        var tasks = Enumerable.Range(1, 254).Select(async i =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var ip = $"{subnetPrefix}.{i}";
                var sw = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    using var tcp = new TcpClient();
                    var connectTask = tcp.ConnectAsync(ip, port, cancellationToken).AsTask();
                    if (await Task.WhenAny(connectTask, Task.Delay(timeoutMs, cancellationToken)) == connectTask
                        && connectTask.IsCompletedSuccessfully)
                    {
                        sw.Stop();
                        results.Add(new NetworkScanResultDto
                        {
                            IpAddress = ip,
                            Port = port,
                            RoundTripMs = (int)sw.ElapsedMilliseconds,
                            RespondedToProtocol = false
                        });
                    }
                }
                catch { /* host not reachable – expected */ }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        var list = results.OrderBy(r => r.IpAddress, StringComparer.OrdinalIgnoreCase).ToList();
        logger.LogInformation("Network scan {Subnet}.x:{Port} found {Count} devices", subnetPrefix, port, list.Count);
        return list;
    }
}
