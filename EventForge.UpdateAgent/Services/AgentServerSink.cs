using EventForge.DTOs.Logging;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using System.Net.Http.Json;
using System.Text.Json;

namespace EventForge.UpdateAgent.Services;

/// <summary>
/// Serilog sink that batches log events and forwards them to the EventForge Server
/// via <c>POST /api/v1/agent-logs/batch</c> authenticated with the
/// <c>X-Maintenance-Secret</c> header.
/// <para>
/// This mirrors the pattern used by the Blazor WASM Client (<c>ClientLogService</c>)
/// so that Agent logs appear alongside Client logs in the Server's centralised
/// log viewer (<c>/dashboard/logs</c>).
/// </para>
/// <para>
/// Forwarding is best-effort: if the Server is unreachable the event is still
/// written to the local rolling file — no data is lost.
/// </para>
/// </summary>
public sealed class AgentServerSink : ILogEventSink, IDisposable, IAsyncDisposable
{
    private const int BatchSize       = 20;
    private const int MaxQueueDepth   = 500;
    private static readonly TimeSpan  FlushInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan  HttpTimeout   = TimeSpan.FromSeconds(4);

    private readonly AgentOptions       _options;
    private readonly HttpClient         _http;
    private readonly string             _ingestUrl;
    private readonly string             _maintenanceSecret;
    private readonly Queue<AgentLogEntryDto> _queue = new();
    private readonly SemaphoreSlim      _flushSemaphore = new(1, 1);
    private readonly Timer              _timer;

    // Internal Serilog logger used ONLY to report sink-level problems without
    // causing infinite recursion.  SelfLog writes to the standard Serilog diagnostics
    // channel (enabled via Serilog.Debugging.SelfLog.Enable(...) in the host if desired).

    public AgentServerSink(AgentOptions options)
    {
        _options = options;
        // Create a dedicated HttpClient rather than using IHttpClientFactory.
        // The factory resolves ILoggerFactory internally, which causes a circular
        // dependency when this sink is instantiated inside the UseSerilog callback
        // (Serilog is not yet configured at that point).
        _http = new HttpClient();

        // Resolve the ingest URL: explicit override > Server NotificationBaseUrl > empty (disabled)
        var baseUrl = !string.IsNullOrWhiteSpace(options.Logging.ServerIngestUrl)
            ? options.Logging.ServerIngestUrl.TrimEnd('/')
            : options.Components.Server.NotificationBaseUrl?.TrimEnd('/') ?? string.Empty;

        _ingestUrl         = string.IsNullOrWhiteSpace(baseUrl) ? string.Empty : $"{baseUrl}/api/v1/agent-logs/batch";
        _maintenanceSecret = options.Components.Server.MaintenanceSecret ?? string.Empty;

        _timer = new Timer(_ => _ = FlushAsync(CancellationToken.None), null, FlushInterval, FlushInterval);
    }

    /// <inheritdoc/>
    public void Emit(LogEvent logEvent)
    {
        if (string.IsNullOrWhiteSpace(_ingestUrl)) return;

        var entry = new AgentLogEntryDto
        {
            Timestamp     = logEvent.Timestamp.UtcDateTime,
            Level         = MapLevel(logEvent.Level),
            Message       = logEvent.RenderMessage(),
            Exception     = logEvent.Exception?.ToString(),
            SourceContext = logEvent.Properties.TryGetValue("SourceContext", out var sc)
                            ? sc.ToString().Trim('"') : null
        };

        lock (_queue)
        {
            if (_queue.Count >= MaxQueueDepth)
                _queue.Dequeue(); // drop oldest when full (same strategy as Client)
            _queue.Enqueue(entry);
        }

        // Flush immediately when we reach the batch size threshold
        if (_queue.Count >= BatchSize)
            _ = FlushAsync(CancellationToken.None);
    }

    /// <summary>Flushes all queued entries to the Server (best-effort).</summary>
    public async Task FlushAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_ingestUrl)) return;

        if (!await _flushSemaphore.WaitAsync(0, ct)) return; // already flushing
        try
        {
            while (true)
            {
                List<AgentLogEntryDto> batch;
                lock (_queue)
                {
                    if (_queue.Count == 0) break;
                    var take = Math.Min(BatchSize, _queue.Count);
                    batch = [];
                    for (var i = 0; i < take; i++) batch.Add(_queue.Dequeue());
                }

                await SendBatchAsync(batch, ct);
            }
        }
        finally
        {
            _flushSemaphore.Release();
        }
    }

    private async Task SendBatchAsync(List<AgentLogEntryDto> batch, CancellationToken ct)
    {
        try
        {
            var payload = new AgentLogBatchDto
            {
                InstallationId   = _options.InstallationId,
                InstallationName = _options.InstallationName,
                Logs             = batch
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _ingestUrl)
            {
                Content = JsonContent.Create(payload, options: new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })
            };

            if (!string.IsNullOrWhiteSpace(_maintenanceSecret))
                request.Headers.Add("X-Maintenance-Secret", _maintenanceSecret);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(HttpTimeout);

            var response = await _http.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode)
                SelfLog.WriteLine("AgentServerSink: Server returned {0} for {1}", (int)response.StatusCode, _ingestUrl);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or TaskCanceledException)
        {
            // Server unreachable — silently ignore; logs are already in the local file.
        }
        catch (Exception ex)
        {
            SelfLog.WriteLine("AgentServerSink: Unexpected error sending batch to {0}: {1}", _ingestUrl, ex.Message);
        }
    }

    private static string MapLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose     => "Verbose",
        LogEventLevel.Debug       => "Debug",
        LogEventLevel.Information => "Information",
        LogEventLevel.Warning     => "Warning",
        LogEventLevel.Error       => "Error",
        LogEventLevel.Fatal       => "Fatal",
        _                         => "Information"
    };

    public async ValueTask DisposeAsync()
    {
        await _timer.DisposeAsync();
        await FlushAsync(CancellationToken.None);
        _flushSemaphore.Dispose();
        _http.Dispose();
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();
}
