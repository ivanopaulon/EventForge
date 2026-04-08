using EventForge.Hardware.PrinterProxy;
using EventForge.Server.Services.FiscalPrinting.EpsonProtocol;

namespace EventForge.Server.Services.FiscalPrinting.Communication;

/// <summary>
/// HTTP communication channel for Epson POS Printer WebAPI (ePOS-Print XML) printers
/// that are reachable from an <c>EventForge.UpdateAgent</c> instance but not directly
/// from the server (<c>ConnectionType = TcpViaAgent</c>).
/// </summary>
/// <remarks>
/// <para>
/// When the Epson printer is on the agent's local network, the server cannot POST SOAP/XML
/// directly to <c>http://{printerIp}/api/1/request</c>. Instead this class forwards the
/// request to the agent's <c>POST /api/printer-proxy/http-forward</c> endpoint, which
/// relays it locally and returns the raw XML response.
/// </para>
/// <para>
/// This is functionally equivalent to <see cref="EpsonWebApiCommunication"/> but adds the
/// agent-proxy hop. All ePOS-Print XML building and response parsing remain in the existing
/// <see cref="EpsonXmlBuilder"/> / <see cref="EpsonResponseParser"/> classes.
/// </para>
/// </remarks>
public sealed class AgentEpsonProxyCommunication : IEpsonChannel
{
    // -------------------------------------------------------------------------
    //  Fields
    // -------------------------------------------------------------------------

    private readonly string _agentBaseUrl;
    private readonly string _printerHost;
    private readonly int _printerPort;
    private readonly string _devid;
    private readonly int _requestTimeoutMs;
    private readonly ILogger<AgentEpsonProxyCommunication> _logger;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    // -------------------------------------------------------------------------
    //  Constructor
    // -------------------------------------------------------------------------

    /// <summary>
    /// Initialises the agent-proxied Epson WebAPI channel.
    /// </summary>
    /// <param name="agentBaseUrl">Base URL of the agent (e.g. <c>http://localhost:5780</c>).</param>
    /// <param name="printerHost">Printer IP address or hostname on the agent's local network.</param>
    /// <param name="printerPort">Printer HTTP port (default: 80).</param>
    /// <param name="devid">Epson device ID (default: <c>local_printer</c>).</param>
    /// <param name="httpClientFactory">Factory used to create the <see cref="HttpClient"/>.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="requestTimeoutMs">Request timeout in milliseconds (default: 30 000).</param>
    public AgentEpsonProxyCommunication(
        string agentBaseUrl,
        string printerHost,
        int printerPort,
        string devid,
        IHttpClientFactory httpClientFactory,
        ILogger<AgentEpsonProxyCommunication> logger,
        int requestTimeoutMs = 30_000)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentBaseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(printerHost);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(printerPort);
        ArgumentException.ThrowIfNullOrWhiteSpace(devid);
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        _agentBaseUrl = agentBaseUrl.TrimEnd('/');
        _printerHost = printerHost;
        _printerPort = printerPort;
        _devid = devid;
        _requestTimeoutMs = requestTimeoutMs;
        _logger = logger;

        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromMilliseconds(requestTimeoutMs);
    }

    // -------------------------------------------------------------------------
    //  Public API
    // -------------------------------------------------------------------------

    /// <summary>The Epson device ID used in all requests.</summary>
    public string DeviceId => _devid;

    /// <summary>
    /// Forwards the SOAP/XML body to the agent's http-forward endpoint, which relays it
    /// to the Epson printer's embedded WebAPI on the agent's local network.
    /// </summary>
    /// <param name="xmlBody">Complete SOAP/XML string built by <see cref="EpsonXmlBuilder"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Raw XML response string from the printer.</returns>
    /// <exception cref="FiscalPrinterCommunicationException">
    /// Thrown when the agent or printer returns an error.
    /// </exception>
    public async Task<string> SendXmlAsync(string xmlBody, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(xmlBody);

        var printerUrl = $"http://{_printerHost}:{_printerPort}{EpsonProtocolConstants.RequestEndpointPath}";
        var forwardUrl = $"{_agentBaseUrl}/api/printer-proxy/http-forward";

        _logger.LogDebug(
            "[AgentEpsonProxyCommunication] → {AgentUrl} → {PrinterUrl} | devid={DevId}",
            forwardUrl, printerUrl, _devid);

        var forwardRequest = new PrinterProxyHttpForwardRequest(printerUrl, EpsonProtocolConstants.ContentType, xmlBody);

        try
        {
            using var response = await _httpClient
                .PostAsJsonAsync(forwardUrl, forwardRequest, cancellationToken)
                .ConfigureAwait(false);

            var responseBody = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            _logger.LogDebug(
                "[AgentEpsonProxyCommunication] ← {AgentUrl} | HTTP {Status} | {Chars} chars",
                forwardUrl, (int)response.StatusCode, responseBody.Length);

            if (!response.IsSuccessStatusCode)
            {
                throw new FiscalPrinterCommunicationException(
                    $"Agent at '{_agentBaseUrl}' returned HTTP {(int)response.StatusCode} " +
                    $"while trying to forward to Epson printer at '{_printerHost}:{_printerPort}'. " +
                    $"Agent error body: {Truncate(responseBody, 200)}");
            }

            // Agent wraps the printer's XML in a JSON envelope — deserialise from string.
            var forwardResponse = System.Text.Json.JsonSerializer.Deserialize<PrinterProxyHttpForwardResponse>(
                responseBody,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (forwardResponse is null || string.IsNullOrWhiteSpace(forwardResponse.ResponseBody))
                throw new FiscalPrinterCommunicationException(
                    $"Agent returned an empty forwarded response from Epson printer " +
                    $"at '{_printerHost}:{_printerPort}'.");

            return forwardResponse.ResponseBody;
        }
        catch (FiscalPrinterCommunicationException)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            throw new FiscalPrinterCommunicationException(
                $"Request to Epson printer via agent at '{_agentBaseUrl}' timed out after {_requestTimeoutMs} ms.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new FiscalPrinterCommunicationException(
                $"HTTP request to agent at '{_agentBaseUrl}' failed: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new FiscalPrinterCommunicationException(
                $"Unexpected error forwarding Epson request via agent at '{_agentBaseUrl}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Tests connectivity through the agent to the Epson printer.
    /// </summary>
    public async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var xml = EpsonXmlBuilder.BuildStatusQuery(_devid, EpsonProtocolConstants.DefaultTimeoutMs);
        var responseXml = await SendXmlAsync(xml, cancellationToken).ConfigureAwait(false);

        var parsed = EpsonResponseParser.ParseResponse(responseXml);
        if (!parsed.Success)
        {
            throw new FiscalPrinterCommunicationException(
                $"Epson printer at {_printerHost}:{_printerPort} via agent '{_agentBaseUrl}' " +
                $"reported an error during connection test: {parsed.ErrorMessage}");
        }

        _logger.LogInformation(
            "[AgentEpsonProxyCommunication] Connection test OK | {Host}:{Port} devid={DevId} agent={AgentUrl}",
            _printerHost, _printerPort, _devid, _agentBaseUrl);
    }

    // -------------------------------------------------------------------------
    //  IAsyncDisposable
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            _httpClient.Dispose();
        }
        return ValueTask.CompletedTask;
    }

    // -------------------------------------------------------------------------
    //  Private helpers
    // -------------------------------------------------------------------------

    private static string Truncate(string text, int maxLength)
        => text.Length > maxLength ? text[..maxLength] + "..." : text;

    // ── DTOs live in EventForge.Hardware.PrinterProxy (PrinterProxyHttpForwardRequest / PrinterProxyHttpForwardResponse)
}
