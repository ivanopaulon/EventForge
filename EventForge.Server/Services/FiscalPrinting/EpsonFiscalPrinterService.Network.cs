using EventForge.DTOs.FiscalPrinting;
using EventForge.Server.Services.FiscalPrinting.Communication;
using EventForge.Server.Services.FiscalPrinting.EpsonProtocol;
using System.Net.Sockets;

namespace EventForge.Server.Services.FiscalPrinting;

public partial class EpsonFiscalPrinterService
{
    // -------------------------------------------------------------------------
    //  Ad-hoc connection tests (wizard – no DB record required)
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<FiscalPrintResult> TestTcpConnectionAsync(
        string ipAddress,
        int port,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var comm = CreateDirectChannel(ipAddress, port);
            await comm.TestConnectionAsync(cancellationToken).ConfigureAwait(false);
            return new FiscalPrintResult { Success = true, PrintDate = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            return new FiscalPrintResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                PrintDate = DateTime.UtcNow
            };
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Serial connections are not supported for Epson WebAPI printers.
    /// This method always returns a failure result.
    /// </remarks>
    public Task<FiscalPrintResult> TestSerialConnectionAsync(
        string serialPortName,
        int baudRate,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new FiscalPrintResult
        {
            Success = false,
            ErrorMessage = "Serial connections are not supported for Epson POS Printer WebAPI. Use TCP (Ethernet/WiFi).",
            PrintDate = DateTime.UtcNow
        });
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
            await using var comm = CreateDirectChannel(ipAddress, port);

            var xml = EpsonXmlBuilder.BuildStatusQuery(
                EpsonProtocolConstants.DefaultDeviceId,
                EpsonProtocolConstants.DefaultTimeoutMs);

            var rawResponse = await comm.SendXmlAsync(xml, cancellationToken).ConfigureAwait(false);
            var status = EpsonResponseParser.ParseStatusResponse(rawResponse);

            if (!status.IsOnline)
            {
                return new FiscalPrinterInfoDto
                {
                    IsOnline = false,
                    ErrorMessage = status.LastError ?? "Printer reported offline status"
                };
            }

            return new FiscalPrinterInfoDto
            {
                IsOnline = true,
                FiscalMemoryUsedPercent = null, // Epson TM series don't have fiscal memory
                PrinterDateTime = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Epson GetPrinterInfoByAddressAsync failed for {IpAddress}:{Port}", ipAddress, port);
            return new FiscalPrinterInfoDto { IsOnline = false, ErrorMessage = ex.Message };
        }
    }

    // -------------------------------------------------------------------------
    //  Network scan (wizard Step 2A)
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public async Task<List<NetworkScanResultDto>> ScanNetworkAsync(
        string subnetPrefix,
        int port = EpsonProtocolConstants.DefaultPort,
        int timeoutMs = 300,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var results = new System.Collections.Concurrent.ConcurrentBag<NetworkScanResultDto>();

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
            logger.LogInformation(
                "Epson network scan {Subnet}.x:{Port} found {Count} devices",
                subnetPrefix, port, list.Count);
            return list;
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
