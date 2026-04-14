using Prym.Hardware.Exceptions;
using Prym.Hardware.PrinterProxy;
using Prym.Agent.Services;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Prym.Agent.Controllers;

/// <summary>
/// Exposes printer proxy endpoints consumed by the EventForge Server when a printer
/// is configured with <c>ConnectionType = UsbViaAgent</c> or <c>ConnectionType = TcpViaAgent</c>.
/// <list type="bullet">
///   <item>USB printers: Server posts raw command bytes (base64); forwarded to the locally-attached USB device.</item>
///   <item>TCP network printers: Server posts raw command bytes (base64) with host/port; forwarded via TCP socket.</item>
///   <item>HTTP/WebAPI printers (e.g. Epson ePOS-Print): Server posts XML body; forwarded via HTTP to printer's URL on the agent's local network.</item>
/// </list>
/// </summary>
[ApiController]
[Route("api/printer-proxy")]
public sealed class PrinterProxyController(
    IAgentPrinterService printerService,
    AgentOptions agentOptions,
    ILogger<PrinterProxyController> logger) : ControllerBase
{
    // Allows only Windows USB device names such as USB001-USB009, or paths like \\.\USB001.
    private static readonly Regex DeviceIdPattern =
        new(@"^(\\\\\.\\)?USB0*[1-9][0-9]{0,2}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Sends a raw command to a locally-attached USB printer and returns the response.
    /// </summary>
    /// <param name="request">
    /// JSON body containing the device identifier and base64-encoded command bytes.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 200 OK with <see cref="PrinterProxySendResponse"/> on success;
    /// 400 Bad Request for invalid input;
    /// 500 Internal Server Error on device I/O failure.
    /// </returns>
    [HttpPost("send")]
    public async Task<IActionResult> SendAsync(
        [FromBody] PrinterProxySendRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
            return BadRequest("deviceId is required.");

        if (!DeviceIdPattern.IsMatch(request.DeviceId))
            return BadRequest("deviceId must be a valid USB device name (e.g. USB001).");

        if (string.IsNullOrWhiteSpace(request.Command))
            return BadRequest("command is required.");

        byte[] commandBytes;
        try
        {
            commandBytes = Convert.FromBase64String(request.Command);
        }
        catch (FormatException ex)
        {
            logger.LogWarning(ex, "Invalid base64 command received for device {DeviceId}", request.DeviceId);
            return BadRequest("command must be a valid base64 string.");
        }

        logger.LogDebug(
            "PrinterProxy send: device={DeviceId} bytes={Bytes}",
            request.DeviceId, commandBytes.Length);

        try
        {
            var responseBytes = await printerService
                .SendCommandAsync(request.DeviceId, commandBytes, ct)
                .ConfigureAwait(false);

            return Ok(new PrinterProxySendResponse(Convert.ToBase64String(responseBytes)));
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "PrinterProxy send failed for device {DeviceId}", request.DeviceId);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Tests connectivity to a locally-attached USB printer by attempting to open the device.
    /// </summary>
    /// <param name="deviceId">The USB device path suffix (e.g. <c>USB001</c>).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK when the device is accessible; 503 Service Unavailable otherwise.</returns>
    [HttpGet("test")]
    public async Task<IActionResult> TestConnectionAsync(
        [FromQuery] string deviceId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            return BadRequest("deviceId query parameter is required.");

        if (!DeviceIdPattern.IsMatch(deviceId))
            return BadRequest("deviceId must be a valid USB device name (e.g. USB001).");

        logger.LogDebug("PrinterProxy test: device={DeviceId}", deviceId);

        try
        {
            await printerService.TestConnectionAsync(deviceId, ct).ConfigureAwait(false);
            return Ok(new { deviceId, status = "ok" });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "PrinterProxy test failed for device {DeviceId}", deviceId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message);
        }
    }

    /// <summary>
    /// Lists USB printer devices available on the local machine
    /// (probes <c>\\.\USB001</c> through <c>\\.\USB009</c>).
    /// </summary>
    /// <returns>200 OK with an array of accessible device identifiers.</returns>
    [HttpGet("devices")]
    public Task<IActionResult> ListDevicesAsync()
    {
        var devices = printerService.ListDevices();

        logger.LogDebug("PrinterProxy list devices: found {Count}", devices.Count);

        return Task.FromResult<IActionResult>(Ok(new { devices }));
    }

    /// <summary>
    /// Returns all printers installed at OS level on the machine running this agent.
    /// On Windows this queries installed printer queues via PowerShell <c>Get-Printer</c>.
    /// On Linux/macOS it uses <c>lpstat -a</c> (CUPS). Falls back to an empty list when
    /// neither is available.
    /// </summary>
    /// <returns>200 OK with a <c>printers</c> array of display-name strings.</returns>
    [HttpGet("system-printers")]
    public async Task<IActionResult> ListSystemPrintersAsync()
    {
        var printerNames = new List<string>();

        try
        {
            printerNames = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? await GetWindowsPrintersAsync()
                : await GetLinuxPrintersAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PrinterProxy: failed to enumerate system printers");
        }

        logger.LogDebug("PrinterProxy system-printers: found {Count}", printerNames.Count);
        return Ok(new { printers = printerNames });
    }

    // ── TCP proxy (TcpViaAgent) ───────────────────────────────────────────────

    /// <summary>
    /// Sends a raw command to a TCP/IP network printer on the agent's local network
    /// and returns the response bytes. The server cannot reach this printer directly;
    /// the agent opens the TCP connection on behalf of the server.
    /// </summary>
    /// <param name="request">JSON body with host, port, and base64-encoded command bytes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 200 OK with <see cref="PrinterProxySendResponse"/> on success;
    /// 400 Bad Request for invalid input;
    /// 500 Internal Server Error on TCP I/O failure.
    /// </returns>
    [HttpPost("tcp-send")]
    public async Task<IActionResult> TcpSendAsync(
        [FromBody] PrinterProxyTcpSendRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Host))
            return BadRequest("host is required.");

        if (request.Port is < 1 or > 65535)
            return BadRequest("port must be between 1 and 65535.");

        // SSRF protection: enforce host allowlist when configured.
        var allowedPatternsTcp = agentOptions.PrinterProxy.AllowedHostPatterns;
        if (allowedPatternsTcp.Count > 0 && !PrinterProxyHostValidator.IsHostAllowed(request.Host, allowedPatternsTcp))
        {
            logger.LogWarning(
                "PrinterProxy tcp-send blocked: host '{Host}' is not in AllowedHostPatterns.", request.Host);
            return StatusCode(StatusCodes.Status403Forbidden,
                $"Host '{request.Host}' is not permitted by the agent's printer-proxy allowlist.");
        }

        if (string.IsNullOrWhiteSpace(request.Command))
            return BadRequest("command is required.");

        byte[] commandBytes;
        try
        {
            commandBytes = Convert.FromBase64String(request.Command);
        }
        catch (FormatException ex)
        {
            logger.LogWarning(ex, "Invalid base64 command for TCP proxy {Host}:{Port}", request.Host, request.Port);
            return BadRequest("command must be a valid base64 string.");
        }

        logger.LogDebug(
            "PrinterProxy tcp-send: {Host}:{Port} bytes={Bytes}",
            request.Host, request.Port, commandBytes.Length);

        try
        {
            var responseBytes = await printerService
                .SendTcpCommandAsync(request.Host, request.Port, commandBytes, ct)
                .ConfigureAwait(false);

            return Ok(new PrinterProxySendResponse(Convert.ToBase64String(responseBytes)));
        }
        catch (FiscalPrinterCommunicationException ex)
        {
            logger.LogError(ex, "PrinterProxy tcp-send failed for {Host}:{Port}", request.Host, request.Port);
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Tests TCP connectivity to a network printer on the agent's local network.
    /// </summary>
    /// <param name="host">Printer IP address or hostname.</param>
    /// <param name="port">TCP port on the printer.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK when reachable; 503 Service Unavailable when not reachable.</returns>
    [HttpGet("tcp-test")]
    public async Task<IActionResult> TcpTestAsync(
        [FromQuery] string host,
        [FromQuery] int port,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(host))
            return BadRequest("host query parameter is required.");

        if (port is < 1 or > 65535)
            return BadRequest("port must be between 1 and 65535.");

        // SSRF protection: enforce host allowlist when configured.
        var allowedPatternsTest = agentOptions.PrinterProxy.AllowedHostPatterns;
        if (allowedPatternsTest.Count > 0 && !PrinterProxyHostValidator.IsHostAllowed(host, allowedPatternsTest))
        {
            logger.LogWarning(
                "PrinterProxy tcp-test blocked: host '{Host}' is not in AllowedHostPatterns.", host);
            return StatusCode(StatusCodes.Status403Forbidden,
                $"Host '{host}' is not permitted by the agent's printer-proxy allowlist.");
        }

        logger.LogDebug("PrinterProxy tcp-test: {Host}:{Port}", host, port);

        try
        {
            await printerService.TestTcpConnectionAsync(host, port, ct).ConfigureAwait(false);
            return Ok(new { host, port, status = "ok" });
        }
        catch (FiscalPrinterCommunicationException ex)
        {
            logger.LogWarning(ex, "PrinterProxy tcp-test failed for {Host}:{Port}", host, port);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message);
        }
    }

    // ── HTTP forward (TcpViaAgent – Epson WebAPI, etc.) ──────────────────────

    /// <summary>
    /// Forwards an HTTP POST request to a printer's WebAPI endpoint on the agent's local network
    /// (e.g. Epson ePOS-Print XML at <c>http://192.168.1.100/api/1/request</c>) and returns
    /// the raw response body. Used when the printer is reachable from the agent but not the server.
    /// </summary>
    /// <param name="request">JSON body with target URL, content type, and request body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// 200 OK with <see cref="PrinterProxyHttpForwardResponse"/> on success;
    /// 400 Bad Request for invalid input;
    /// 502 Bad Gateway when the printer returns a non-success HTTP status;
    /// 500 Internal Server Error on network failure.
    /// </returns>
    [HttpPost("http-forward")]
    public async Task<IActionResult> HttpForwardAsync(
        [FromBody] PrinterProxyHttpForwardRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.TargetUrl))
            return BadRequest("targetUrl is required.");

        if (!Uri.TryCreate(request.TargetUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return BadRequest("targetUrl must be a valid absolute HTTP/HTTPS URL.");

        // SSRF protection: enforce host allowlist when configured.
        var allowedPatterns = agentOptions.PrinterProxy.AllowedHostPatterns;
        if (allowedPatterns.Count > 0 && !PrinterProxyHostValidator.IsHostAllowed(uri.Host, allowedPatterns))
        {
            logger.LogWarning(
                "PrinterProxy http-forward blocked: host '{Host}' is not in AllowedHostPatterns.", uri.Host);
            return StatusCode(StatusCodes.Status403Forbidden,
                $"Host '{uri.Host}' is not permitted by the agent's printer-proxy allowlist.");
        }

        if (string.IsNullOrWhiteSpace(request.ContentType))
            return BadRequest("contentType is required.");

        logger.LogDebug(
            "PrinterProxy http-forward → {Url} contentType={ContentType} chars={Chars}",
            request.TargetUrl, request.ContentType, request.Body?.Length ?? 0);

        try
        {
            var responseBody = await printerService
                .ForwardHttpAsync(request.TargetUrl, request.ContentType, request.Body ?? string.Empty, ct)
                .ConfigureAwait(false);

            return Ok(new PrinterProxyHttpForwardResponse(responseBody));
        }
        catch (FiscalPrinterCommunicationException ex)
        {
            logger.LogError(ex, "PrinterProxy http-forward failed for {Url}", request.TargetUrl);
            return StatusCode(StatusCodes.Status502BadGateway, ex.Message);
        }
    }

    private static async Task<List<string>> GetWindowsPrintersAsync()
    {
        var printers = new List<string>();

        // Use PowerShell to enumerate installed printer queues.
        // Get-Printer returns rich objects; we only need the Name field.
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-NoProfile -NonInteractive -Command " +
                        "\"Get-Printer | Select-Object -ExpandProperty Name\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = System.Diagnostics.Process.Start(psi);
        if (proc is not null)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var output = await proc.StandardOutput.ReadToEndAsync(cts.Token);
            await proc.WaitForExitAsync(cts.Token);

            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var name = line.Trim();
                if (!string.IsNullOrEmpty(name))
                    printers.Add(name);
            }
        }

        return printers;
    }

    private static async Task<List<string>> GetLinuxPrintersAsync()
    {
        var printers = new List<string>();

        var psi = new System.Diagnostics.ProcessStartInfo("lpstat", "-a")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = System.Diagnostics.Process.Start(psi);
        if (proc is not null)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var output = await proc.StandardOutput.ReadToEndAsync(cts.Token);
            await proc.WaitForExitAsync(cts.Token);

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
}
