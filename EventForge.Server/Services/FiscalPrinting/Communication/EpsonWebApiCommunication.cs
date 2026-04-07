using System.Text;
using EventForge.Server.Services.FiscalPrinting.Communication;
using EventForge.Server.Services.FiscalPrinting.EpsonProtocol;

namespace EventForge.Server.Services.FiscalPrinting.Communication;

/// <summary>
/// HTTP communication channel for the Epson POS Printer WebAPI (ePOS-Print XML).
/// Sends SOAP/XML requests to <c>http://{host}:{port}/api/1/request</c> on the
/// printer's embedded HTTP server and returns the raw XML response body.
/// </summary>
/// <remarks>
/// Specification: Epson POS Printer WebAPI Interface Specification (Rev. A).
/// Default port is 80 (the printer's built-in web server).
/// Each instance holds an <see cref="HttpClient"/> created via
/// <see cref="IHttpClientFactory"/> and must be disposed after use.
/// </remarks>
public sealed class EpsonWebApiCommunication : IAsyncDisposable
{
    // -------------------------------------------------------------------------
    //  Fields
    // -------------------------------------------------------------------------

    private readonly string _host;
    private readonly int _port;
    private readonly string _devid;
    private readonly int _requestTimeoutMs;
    private readonly ILogger<EpsonWebApiCommunication> _logger;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    // -------------------------------------------------------------------------
    //  Constructor
    // -------------------------------------------------------------------------

    /// <summary>
    /// Initialises a new Epson WebAPI communication channel.
    /// </summary>
    /// <param name="host">Printer IP address or hostname.</param>
    /// <param name="port">
    /// HTTP port of the printer's embedded web server.
    /// Default: 80 (<see cref="EpsonProtocolConstants.DefaultPort"/>).
    /// </param>
    /// <param name="devid">
    /// Epson device identifier configured on the printer's web server.
    /// Default: "local_printer" (<see cref="EpsonProtocolConstants.DefaultDeviceId"/>).
    /// </param>
    /// <param name="httpClientFactory">Factory used to create the <see cref="HttpClient"/>.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="requestTimeoutMs">
    /// Overall HTTP request timeout in milliseconds (default: 30 000).
    /// The ePOS <c>timeout</c> attribute is set to <see cref="EpsonProtocolConstants.DefaultTimeoutMs"/>.
    /// </param>
    public EpsonWebApiCommunication(
        string host,
        int port,
        string devid,
        IHttpClientFactory httpClientFactory,
        ILogger<EpsonWebApiCommunication> logger,
        int requestTimeoutMs = 30_000)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        ArgumentException.ThrowIfNullOrWhiteSpace(devid);
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        _host = host;
        _port = port;
        _devid = devid;
        _requestTimeoutMs = requestTimeoutMs;
        _logger = logger;

        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromMilliseconds(requestTimeoutMs);
    }

    // -------------------------------------------------------------------------
    //  Public API
    // -------------------------------------------------------------------------

    /// <summary>The device ID used in all requests to this printer.</summary>
    public string DeviceId => _devid;

    /// <summary>
    /// Posts the given SOAP/XML document to the Epson WebAPI endpoint and returns
    /// the raw XML response body.
    /// </summary>
    /// <param name="xmlBody">Complete SOAP/XML string built by <see cref="EpsonXmlBuilder"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Raw XML response string.</returns>
    /// <exception cref="FiscalPrinterCommunicationException">
    /// Thrown when the HTTP request fails, times out, or returns a non-success HTTP status.
    /// </exception>
    public async Task<string> SendXmlAsync(string xmlBody, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(xmlBody);

        var uri = BuildRequestUri();
        _logger.LogDebug(
            "Epson → {Uri} | devid={DevId} | {Bytes} bytes",
            uri, _devid, Encoding.UTF8.GetByteCount(xmlBody));

        using var content = new StringContent(xmlBody, Encoding.UTF8, EpsonProtocolConstants.ContentType);

        try
        {
            using var response = await _httpClient
                .PostAsync(uri, content, cancellationToken)
                .ConfigureAwait(false);

            var responseBody = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            _logger.LogDebug(
                "Epson ← {Uri} | HTTP {Status} | {Bytes} bytes",
                uri, (int)response.StatusCode, Encoding.UTF8.GetByteCount(responseBody));

            if (!response.IsSuccessStatusCode)
            {
                throw new FiscalPrinterCommunicationException(
                    $"Epson printer at {_host}:{_port} returned HTTP {(int)response.StatusCode} " +
                    $"{response.ReasonPhrase}. Body: {Truncate(responseBody, 200)}");
            }

            return responseBody;
        }
        catch (FiscalPrinterCommunicationException)
        {
            throw; // already wrapped
        }
        catch (OperationCanceledException ex)
        {
            throw new FiscalPrinterCommunicationException(
                $"Request to Epson printer at {_host}:{_port} timed out after {_requestTimeoutMs} ms.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new FiscalPrinterCommunicationException(
                $"HTTP request to Epson printer at {_host}:{_port} failed: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new FiscalPrinterCommunicationException(
                $"Unexpected error communicating with Epson printer at {_host}:{_port}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Tests connectivity by sending a status query (empty ePOS-Print body)
    /// and verifying the printer responds with success.
    /// </summary>
    /// <exception cref="FiscalPrinterCommunicationException">Thrown when the printer is unreachable.</exception>
    public async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var xml = EpsonXmlBuilder.BuildStatusQuery(_devid, EpsonProtocolConstants.DefaultTimeoutMs);
        var responseXml = await SendXmlAsync(xml, cancellationToken).ConfigureAwait(false);

        var parsed = EpsonResponseParser.ParseResponse(responseXml);
        if (!parsed.Success)
        {
            throw new FiscalPrinterCommunicationException(
                $"Epson printer at {_host}:{_port} (devid={_devid}) reported an error during " +
                $"connection test: {parsed.ErrorMessage}");
        }

        _logger.LogInformation(
            "Epson connection test successful | {Host}:{Port} devid={DevId}",
            _host, _port, _devid);
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

    private string BuildRequestUri()
        => $"http://{_host}:{_port}{EpsonProtocolConstants.RequestEndpointPath}";

    private static string Truncate(string text, int maxLength)
        => text.Length > maxLength ? text[..maxLength] + "..." : text;
}
