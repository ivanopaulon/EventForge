using Prym.Agent.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace Prym.Agent.Controllers;

/// <summary>
/// Exposes USB printer proxy endpoints consumed by the Prym Server when a printer
/// is configured with <c>ConnectionType = UsbViaAgent</c>.
/// The Server posts raw command bytes (base64); this controller forwards them to the
/// locally-attached USB printer via <see cref="IAgentPrinterService"/>.
/// </summary>
[ApiController]
[Route("api/printer-proxy")]
public sealed class PrinterProxyController(
    IAgentPrinterService printerService,
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
    /// 200 OK with <see cref="PrinterProxyResponse"/> on success;
    /// 400 Bad Request for invalid input;
    /// 500 Internal Server Error on device I/O failure.
    /// </returns>
    [HttpPost("send")]
    public async Task<IActionResult> SendAsync(
        [FromBody] PrinterProxyRequest request,
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

            return Ok(new PrinterProxyResponse(Convert.ToBase64String(responseBytes)));
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
}
