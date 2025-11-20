using EventForge.DTOs.Common;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Diagnostics;
using System.Text.Json;

namespace EventForge.Server.Services.Logging;

/// <summary>
/// Background service that processes client logs from the ingestion queue.
/// Uses Polly for retry (exponential backoff) and circuit breaker patterns.
/// Falls back to file logging when the circuit breaker opens.
/// </summary>
public class LogIngestionBackgroundService : BackgroundService
{
    private const int BatchSize = 200;
    private const string FallbackLogDirectory = "logs";
    
    private readonly LogIngestionService _ingestionService;
    private readonly ILogger<LogIngestionBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ResiliencePipeline _resiliencePipeline;
    private int _consecutiveFailures;

    public LogIngestionBackgroundService(
        LogIngestionService ingestionService,
        ILogger<LogIngestionBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _ingestionService = ingestionService;
        _logger = logger;
        _serviceProvider = serviceProvider;

        // Create resilience pipeline with Polly v8 API
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning(args.Outcome.Exception,
                        "Retry {RetryCount} after {Delay}s due to error",
                        args.AttemptNumber, args.RetryDelay.TotalSeconds);
                    return default;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 1.0, // Break after all attempts fail
                MinimumThroughput = 5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(30),
                OnOpened = args =>
                {
                    _logger.LogError(args.Outcome.Exception,
                        "Circuit breaker opened. Will retry after 30s");
                    _ingestionService.UpdateCircuitBreakerState("Open");
                    _consecutiveFailures++;
                    return default;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Circuit breaker closed - resuming normal operations");
                    _ingestionService.UpdateCircuitBreakerState("Closed");
                    _consecutiveFailures = 0;
                    return default;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation("Circuit breaker half-open - testing if service recovered");
                    _ingestionService.UpdateCircuitBreakerState("HalfOpen");
                    return default;
                }
            })
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Log Ingestion Background Service starting");

        // Ensure fallback directory exists
        Directory.CreateDirectory(FallbackLogDirectory);

        try
        {
            await ProcessLogsAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Log Ingestion Background Service stopping");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Log Ingestion Background Service failed with unhandled exception");
            throw; // Let the host handle the failure
        }
    }

    private async Task ProcessLogsAsync(CancellationToken stoppingToken)
    {
        var channelReader = _ingestionService.GetChannelReader();
        var batch = new List<ClientLogDto>(BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for logs to be available
                await channelReader.WaitToReadAsync(stoppingToken);

                // Read a batch of logs
                while (batch.Count < BatchSize && channelReader.TryRead(out var logEntry))
                {
                    batch.Add(logEntry);
                }

                if (batch.Count > 0)
                {
                    await ProcessBatchAsync(batch, stoppingToken);
                    batch.Clear();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in log processing loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Back off before retrying
            }
        }

        _logger.LogInformation("Log Ingestion Background Service stopped");
    }

    private async Task ProcessBatchAsync(List<ClientLogDto> batch, CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Try to process with resilience policies
            await _resiliencePipeline.ExecuteAsync(async (ct) =>
            {
                await WriteBatchToLoggerAsync(batch, ct);
            }, stoppingToken);

            stopwatch.Stop();
            _ingestionService.UpdateMetrics(stopwatch.Elapsed.TotalMilliseconds / batch.Count);
            _consecutiveFailures = 0;
        }
        catch (BrokenCircuitException ex)
        {
            // Circuit breaker is open - fallback to file
            _logger.LogWarning(ex, "Circuit breaker open - writing {Count} logs to fallback file", batch.Count);
            await WriteBatchToFallbackFileAsync(batch, "CircuitBreakerOpen", stoppingToken);
        }
        catch (Exception ex)
        {
            // All retries exhausted - fallback to file
            _logger.LogError(ex, "Failed to process log batch after retries - writing {Count} logs to fallback file", batch.Count);
            await WriteBatchToFallbackFileAsync(batch, ex.Message, stoppingToken);
            _consecutiveFailures++;
        }
    }

    private async Task WriteBatchToLoggerAsync(List<ClientLogDto> batch, CancellationToken stoppingToken)
    {
        // Create a new scope to get a scoped ILogger for writing client logs
        using var scope = _serviceProvider.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        
        foreach (var clientLog in batch)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            // Use a category-specific logger to write the actual client log
            var categoryLogger = loggerFactory.CreateLogger(clientLog.Category ?? "ClientLog");

            // Create enriched log context with client-specific properties
            var properties = new Dictionary<string, object>
            {
                ["Source"] = "Client",
                ["Page"] = clientLog.Page ?? "Unknown",
                ["UserAgent"] = clientLog.UserAgent ?? "Unknown",
                ["ClientTimestamp"] = clientLog.Timestamp,
                ["CorrelationId"] = clientLog.CorrelationId ?? Guid.NewGuid().ToString()
            };

            if (clientLog.UserId.HasValue)
            {
                properties["UserId"] = clientLog.UserId.Value;
            }

            if (!string.IsNullOrEmpty(clientLog.Properties))
            {
                properties["ClientProperties"] = clientLog.Properties;
            }

            // Log based on level with structured properties
            using (categoryLogger.BeginScope(properties))
            {
                switch (clientLog.Level.ToUpperInvariant())
                {
                    case "DEBUG":
                        categoryLogger.LogDebug("{Message}", clientLog.Message);
                        break;

                    case "INFORMATION":
                    case "INFO":
                        categoryLogger.LogInformation("{Message}", clientLog.Message);
                        break;

                    case "WARNING":
                    case "WARN":
                        if (!string.IsNullOrEmpty(clientLog.Exception))
                        {
                            categoryLogger.LogWarning("{Message} | Exception: {Exception}", 
                                clientLog.Message, clientLog.Exception);
                        }
                        else
                        {
                            categoryLogger.LogWarning("{Message}", clientLog.Message);
                        }
                        break;

                    case "ERROR":
                        if (!string.IsNullOrEmpty(clientLog.Exception))
                        {
                            categoryLogger.LogError("{Message} | Exception: {Exception}", 
                                clientLog.Message, clientLog.Exception);
                        }
                        else
                        {
                            categoryLogger.LogError("{Message}", clientLog.Message);
                        }
                        break;

                    case "CRITICAL":
                        if (!string.IsNullOrEmpty(clientLog.Exception))
                        {
                            categoryLogger.LogCritical("{Message} | Exception: {Exception}", 
                                clientLog.Message, clientLog.Exception);
                        }
                        else
                        {
                            categoryLogger.LogCritical("{Message}", clientLog.Message);
                        }
                        break;

                    default:
                        categoryLogger.LogInformation("{Message}", clientLog.Message);
                        break;
                }
            }
        }

        // Simulate potential transient errors for testing resilience
        // In production, this would be real I/O errors, database connectivity issues, etc.
        await Task.CompletedTask;
    }

    private async Task WriteBatchToFallbackFileAsync(
        List<ClientLogDto> batch, 
        string reason, 
        CancellationToken stoppingToken)
    {
        try
        {
            var fileName = $"client-fallback-{DateTime.UtcNow:yyyyMMdd}.log";
            var filePath = Path.Combine(FallbackLogDirectory, fileName);

            var fallbackEntries = batch.Select(log => new
            {
                Timestamp = DateTime.UtcNow,
                Reason = reason,
                ClientLog = log
            });

            var jsonLines = fallbackEntries.Select(entry => JsonSerializer.Serialize(entry));
            await File.AppendAllLinesAsync(filePath, jsonLines, stoppingToken);

            _logger.LogInformation(
                "Wrote {Count} client logs to fallback file: {FilePath}",
                batch.Count, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write logs to fallback file - logs may be lost");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Log Ingestion Background Service stopping gracefully");
        await base.StopAsync(cancellationToken);
    }
}
