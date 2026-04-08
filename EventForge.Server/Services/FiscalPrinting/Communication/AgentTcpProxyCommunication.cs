using EventForge.Hardware.PrinterProxy;

namespace EventForge.Server.Services.FiscalPrinting.Communication;

/// <summary>
/// HTTP proxy communication channel for <b>TCP/IP network printers</b> reachable from an
/// <c>EventForge.UpdateAgent</c> instance but not directly from the server
/// (<c>ConnectionType = TcpViaAgent</c>).
/// </summary>
/// <remarks>
/// Extends <see cref="AgentProxyBaseCommunication"/> with TCP-specific endpoint paths and
/// request DTOs. The agent opens the TCP socket on its side and relays the command bytes.
/// Retries on transient HTTP errors (3 attempts, exponential back-off).
/// </remarks>
public sealed class AgentTcpProxyCommunication(
    string agentBaseUrl,
    string printerHost,
    int printerPort,
    int timeoutMs,
    ILogger<AgentTcpProxyCommunication> logger,
    IHttpClientFactory httpClientFactory)
    : AgentProxyBaseCommunication(agentBaseUrl, timeoutMs, logger, httpClientFactory)
{
    private readonly string _printerHost = ValidateHost(printerHost);
    private readonly int _printerPort    = ValidatePort(printerPort);

    // ── ICustomPrinterCommunication ────────────────────────────────────────────

    /// <inheritdoc />
    public override async Task<byte[]> SendCommandAsync(
        byte[] command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await ExecuteWithRetryAsync(async ct =>
        {
            logger.LogDebug(
                "[AgentTcpProxyCommunication] → {Url}/tcp-send | printer={Host}:{Port} | {Bytes} bytes",
                AgentBaseUrl, _printerHost, _printerPort, command.Length);

            var result = await PostJsonAsync(
                $"{AgentBaseUrl}/api/printer-proxy/tcp-send",
                new PrinterProxyTcpSendRequest(_printerHost, _printerPort, Convert.ToBase64String(command)),
                $"TCP printer '{_printerHost}:{_printerPort}'",
                ct).ConfigureAwait(false);

            var responseBytes = Convert.FromBase64String(result.Response);

            logger.LogDebug(
                "[AgentTcpProxyCommunication] ← {Url} | printer={Host}:{Port} | {Bytes} bytes",
                AgentBaseUrl, _printerHost, _printerPort, responseBytes.Length);

            return responseBytes;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        await GetTestAsync(
            $"{AgentBaseUrl}/api/printer-proxy/tcp-test" +
            $"?host={Uri.EscapeDataString(_printerHost)}&port={_printerPort}",
            $"TCP printer '{_printerHost}:{_printerPort}'",
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "[AgentTcpProxyCommunication] Connection test OK | printer={Host}:{Port} agent={Url}",
            _printerHost, _printerPort, AgentBaseUrl);
    }

    // ── Validation ─────────────────────────────────────────────────────────────

    private static string ValidateHost(string host)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        return host;
    }

    private static int ValidatePort(int port)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        return port;
    }
}
