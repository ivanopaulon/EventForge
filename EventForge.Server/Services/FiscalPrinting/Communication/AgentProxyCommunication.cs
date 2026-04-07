using EventForge.Server.Services.FiscalPrinting.Communication;
using Polly;
using Polly.Retry;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace EventForge.Server.Services.FiscalPrinting.Communication;

/// <summary>
/// HTTP proxy communication channel that forwards fiscal printer commands to an
/// <c>EventForge.UpdateAgent</c> instance, which in turn communicates with a
/// locally-attached USB printer on the agent machine.
/// </summary>
/// <remarks>
/// Because HTTP is stateless, <see cref="IsConnected"/> always returns <see langword="true"/>
/// and <see cref="DisconnectAsync"/> is a no-op. Connectivity is validated per-request.
/// Retries on <see cref="HttpRequestException"/> with exponential back-off (3 attempts).
/// </remarks>
public sealed class AgentProxyCommunication(
    string agentBaseUrl,
    string printerDeviceId,
    int timeoutMs,
    ILogger<AgentProxyCommunication> logger,
    IHttpClientFactory httpClientFactory) : ICustomPrinterCommunication
{
    // -------------------------------------------------------------------------
    //  Constants
    // -------------------------------------------------------------------------

    private const int MaxRetryAttempts = 3;

    // -------------------------------------------------------------------------
    //  Fields
    // -------------------------------------------------------------------------

    private readonly string _agentBaseUrl = ValidateAndNormalizeUrl(agentBaseUrl);
    private readonly string _printerDeviceId = ValidateDeviceId(printerDeviceId);
    private readonly int _timeoutMs = ValidateTimeout(timeoutMs);

    private readonly ResiliencePipeline _retryPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = MaxRetryAttempts,
            Delay = TimeSpan.FromMilliseconds(500),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
                                                 .Handle<FiscalPrinterCommunicationException>(),
            OnRetry = args =>
            {
                logger.LogWarning(
                    args.Outcome.Exception,
                    "Agent proxy retry {Attempt}/{Max} for device {DeviceId} at {Url} – {Reason}",
                    args.AttemptNumber + 1, MaxRetryAttempts, printerDeviceId, agentBaseUrl,
                    args.Outcome.Exception?.Message ?? "unknown error");
                return default;
            }
        })
        .Build();

    // -------------------------------------------------------------------------
    //  ICustomPrinterCommunication
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    /// <remarks>Always <see langword="true"/>; HTTP is stateless and checked per-request.</remarks>
    public bool IsConnected => true;

    /// <inheritdoc />
    public async Task<byte[]> SendCommandAsync(byte[] command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await _retryPipeline.ExecuteAsync(async ct =>
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMilliseconds(_timeoutMs);

            var requestBody = new PrinterProxySendRequest(
                _printerDeviceId,
                Convert.ToBase64String(command));

            logger.LogDebug(
                "Agent proxy → {Url}/api/printer-proxy/send | device={DeviceId} | {Bytes} bytes",
                _agentBaseUrl, _printerDeviceId, command.Length);

            using var response = await client
                .PostAsJsonAsync($"{_agentBaseUrl}/api/printer-proxy/send", requestBody, ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                throw new FiscalPrinterCommunicationException(
                    $"Agent proxy returned HTTP {(int)response.StatusCode} for device '{_printerDeviceId}': {body}");
            }

            var result = await response.Content
                .ReadFromJsonAsync<PrinterProxySendResponse>(ct)
                .ConfigureAwait(false);

            if (result is null || string.IsNullOrEmpty(result.Response))
                throw new FiscalPrinterCommunicationException(
                    $"Agent proxy returned an empty response for device '{_printerDeviceId}'.");

            var responseBytes = Convert.FromBase64String(result.Response);

            logger.LogDebug(
                "Agent proxy ← {Url} | device={DeviceId} | {Bytes} bytes",
                _agentBaseUrl, _printerDeviceId, responseBytes.Length);

            return responseBytes;

        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        using var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMilliseconds(_timeoutMs);

        var url = $"{_agentBaseUrl}/api/printer-proxy/test?deviceId={Uri.EscapeDataString(_printerDeviceId)}";

        logger.LogDebug("Agent proxy test connection GET {Url}", url);

        using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new FiscalPrinterCommunicationException(
                $"Agent proxy connection test failed for device '{_printerDeviceId}' " +
                $"(HTTP {(int)response.StatusCode}): {body}");
        }

        logger.LogInformation(
            "Agent proxy connection test successful for device '{DeviceId}' at {Url}",
            _printerDeviceId, _agentBaseUrl);
    }

    /// <inheritdoc />
    /// <remarks>No-op — HTTP is stateless; there is no persistent connection to close.</remarks>
    public Task DisconnectAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    //  IAsyncDisposable
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // -------------------------------------------------------------------------
    //  Private validation helpers
    // -------------------------------------------------------------------------

    private static string ValidateAndNormalizeUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        return url.TrimEnd('/');
    }

    private static string ValidateDeviceId(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        return deviceId;
    }

    private static int ValidateTimeout(int ms)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ms);
        return ms;
    }

    // -------------------------------------------------------------------------
    //  Private DTOs
    // -------------------------------------------------------------------------

    private sealed record PrinterProxySendRequest(
        [property: JsonPropertyName("deviceId")] string DeviceId,
        [property: JsonPropertyName("command")] string Command);

    private sealed record PrinterProxySendResponse(
        [property: JsonPropertyName("response")] string Response);
}
