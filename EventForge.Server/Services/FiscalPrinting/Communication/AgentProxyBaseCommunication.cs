using Prym.Hardware.PrinterProxy;
using Polly;
using Polly.Retry;
using System.Net.Http.Json;

namespace EventForge.Server.Services.FiscalPrinting.Communication;

/// <summary>
/// Shared base for HTTP proxy communication channels that relay commands
/// to an <c>EventForge.UpdateAgent</c> instance.
/// Provides the retry pipeline, HTTP execution helpers, and validation utilities
/// used by both <see cref="AgentProxyCommunication"/> (USB) and
/// <see cref="AgentTcpProxyCommunication"/> (TCP network).
/// </summary>
public abstract class AgentProxyBaseCommunication : ICustomPrinterCommunication
{
    // ── Constants ──────────────────────────────────────────────────────────────

    protected const int MaxRetryAttempts = 3;
    protected const int RetryBaseDelayMs = 500;

    // ── Fields ─────────────────────────────────────────────────────────────────

    protected readonly string AgentBaseUrl;
    protected readonly int TimeoutMs;
    protected readonly IHttpClientFactory HttpClientFactory;

    private readonly ResiliencePipeline _retryPipeline;
    private readonly ILogger _logger;

    // ── Constructor ────────────────────────────────────────────────────────────

    protected AgentProxyBaseCommunication(
        string agentBaseUrl,
        int timeoutMs,
        ILogger logger,
        IHttpClientFactory httpClientFactory)
    {
        AgentBaseUrl  = ValidateAndNormalizeUrl(agentBaseUrl);
        TimeoutMs     = ValidateTimeout(timeoutMs);
        _logger        = logger;
        HttpClientFactory = httpClientFactory;

        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = MaxRetryAttempts,
                Delay            = TimeSpan.FromMilliseconds(RetryBaseDelayMs),
                BackoffType      = DelayBackoffType.Exponential,
                UseJitter        = true,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<FiscalPrinterCommunicationException>(),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "[{Type}] Retry {Attempt}/{Max} – {Reason}",
                        GetType().Name,
                        args.AttemptNumber + 1, MaxRetryAttempts,
                        args.Outcome.Exception?.Message ?? "unknown error");
                    return default;
                }
            })
            .Build();
    }

    // ── ICustomPrinterCommunication ────────────────────────────────────────────

    /// <inheritdoc />
    /// <remarks>Always <see langword="true"/>; HTTP is stateless, checked per-request.</remarks>
    public bool IsConnected => true;

    /// <inheritdoc />
    public abstract Task<byte[]> SendCommandAsync(byte[] command, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc />
    /// <remarks>No-op — HTTP is stateless; there is no persistent connection to close.</remarks>
    public Task DisconnectAsync() => Task.CompletedTask;

    // ── IAsyncDisposable ───────────────────────────────────────────────────────

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // ── Protected helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Executes <paramref name="operation"/> inside the shared retry pipeline.
    /// </summary>
    protected Task<byte[]> ExecuteWithRetryAsync(
        Func<CancellationToken, ValueTask<byte[]>> operation,
        CancellationToken cancellationToken)
        => _retryPipeline.ExecuteAsync(operation, cancellationToken).AsTask();

    /// <summary>
    /// POSTs <paramref name="request"/> as JSON to <paramref name="url"/> and returns the
    /// deserialised <see cref="PrinterProxySendResponse"/>, throwing
    /// <see cref="FiscalPrinterCommunicationException"/> on HTTP error or empty response.
    /// </summary>
    protected async Task<PrinterProxySendResponse> PostJsonAsync<TReq>(
        string url,
        TReq request,
        string contextDescription,
        CancellationToken ct)
    {
        using var client = HttpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMilliseconds(TimeoutMs);

        using var response = await client
            .PostAsJsonAsync(url, request, ct)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            throw new FiscalPrinterCommunicationException(
                $"Agent returned HTTP {(int)response.StatusCode} for {contextDescription}: {body}");
        }

        var result = await response.Content
            .ReadFromJsonAsync<PrinterProxySendResponse>(ct)
            .ConfigureAwait(false);

        if (result is null || string.IsNullOrEmpty(result.Response))
            throw new FiscalPrinterCommunicationException(
                $"Agent returned an empty response for {contextDescription}.");

        return result;
    }

    /// <summary>
    /// Issues a GET to <paramref name="url"/>, throwing
    /// <see cref="FiscalPrinterCommunicationException"/> when the printer is unreachable.
    /// </summary>
    protected async Task GetTestAsync(
        string url,
        string contextDescription,
        CancellationToken ct)
    {
        using var client = HttpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMilliseconds(TimeoutMs);

        _logger.LogDebug("[{Type}] TestConnection GET {Url}", GetType().Name, url);

        using var response = await client.GetAsync(url, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            throw new FiscalPrinterCommunicationException(
                $"Agent connection test failed for {contextDescription} " +
                $"(HTTP {(int)response.StatusCode}): {body}");
        }
    }

    // ── Private validation helpers ─────────────────────────────────────────────

    protected static string ValidateAndNormalizeUrl(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        return url.TrimEnd('/');
    }

    protected static int ValidateTimeout(int ms)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ms);
        return ms;
    }
}
