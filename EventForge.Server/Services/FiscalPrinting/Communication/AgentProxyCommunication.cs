using EventForge.Hardware.PrinterProxy;
using System.Net.Http.Json;

namespace EventForge.Server.Services.FiscalPrinting.Communication;

/// <summary>
/// HTTP proxy communication channel that forwards fiscal printer commands to an
/// <c>EventForge.UpdateAgent</c> instance, which in turn communicates with a
/// USB-attached printer on the agent machine (<c>ConnectionType = UsbViaAgent</c>).
/// </summary>
/// <remarks>
/// Extends <see cref="AgentProxyBaseCommunication"/> with the USB-specific endpoint paths
/// and request/response DTOs. Retries on transient HTTP errors (3 attempts, exponential back-off).
/// </remarks>
public sealed class AgentProxyCommunication(
    string agentBaseUrl,
    string printerDeviceId,
    int timeoutMs,
    ILogger<AgentProxyCommunication> logger,
    IHttpClientFactory httpClientFactory)
    : AgentProxyBaseCommunication(agentBaseUrl, timeoutMs, logger, httpClientFactory)
{
    private readonly string _printerDeviceId = ValidateDeviceId(printerDeviceId);

    // ── ICustomPrinterCommunication ────────────────────────────────────────────

    /// <inheritdoc />
    public override async Task<byte[]> SendCommandAsync(
        byte[] command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await ExecuteWithRetryAsync(async ct =>
        {
            logger.LogDebug(
                "[AgentProxyCommunication] → {Url}/send | device={DeviceId} | {Bytes} bytes",
                AgentBaseUrl, _printerDeviceId, command.Length);

            var result = await PostJsonAsync(
                $"{AgentBaseUrl}/api/printer-proxy/send",
                new PrinterProxySendRequest(_printerDeviceId, Convert.ToBase64String(command)),
                $"USB device '{_printerDeviceId}'",
                ct).ConfigureAwait(false);

            var responseBytes = Convert.FromBase64String(result.Response);

            logger.LogDebug(
                "[AgentProxyCommunication] ← {Url} | device={DeviceId} | {Bytes} bytes",
                AgentBaseUrl, _printerDeviceId, responseBytes.Length);

            return responseBytes;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        await GetTestAsync(
            $"{AgentBaseUrl}/api/printer-proxy/test?deviceId={Uri.EscapeDataString(_printerDeviceId)}",
            $"USB device '{_printerDeviceId}'",
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "[AgentProxyCommunication] Connection test OK | device={DeviceId} agent={Url}",
            _printerDeviceId, AgentBaseUrl);
    }

    // ── Validation ─────────────────────────────────────────────────────────────

    private static string ValidateDeviceId(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        return deviceId;
    }
}
