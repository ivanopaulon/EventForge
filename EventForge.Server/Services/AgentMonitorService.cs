namespace EventForge.Server.Services;

/// <summary>
/// Singleton that continuously probes the co-located UpdateAgent, tracks unreachability
/// duration, and automatically restarts the Windows Service once the configured threshold
/// is exceeded.
///
/// Configuration (Server appsettings.json, section "Agent"):
///   LocalUrl                — http://localhost:{agentPort}  (probe target)
///   PollIntervalSeconds     — how often to probe (default 30 s)
///   AutoRestartAfterMinutes — minutes of unreachability before auto-restart (0 = disabled)
/// </summary>
public sealed class AgentMonitorService : BackgroundService
{
    // ── Injected deps ────────────────────────────────────────────────────────
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<AgentMonitorService> _logger;

    // ── Current state (read by AgentStatusController) ────────────────────────
    private volatile bool _reachable = false;
    private DateTime? _unreachableSince;
    private DateTime? _lastSeenAt;
    private DateTime? _lastRestartAttemptAt;
    private string? _lastStatusJson;          // raw JSON from /api/agent/health

    public bool Reachable => _reachable;
    public DateTime? UnreachableSince => _unreachableSince;
    public DateTime? LastSeenAt => _lastSeenAt;
    public DateTime? LastRestartAttemptAt => _lastRestartAttemptAt;

    /// <summary>
    /// Minutes of unreachability before auto-restart is attempted.
    /// 0 means disabled.
    /// </summary>
    public int AutoRestartAfterMinutes =>
        _config.GetValue<int>("Agent:AutoRestartAfterMinutes", 0);

    /// <summary>Parsed snapshot of the last successful probe payload, or null.</summary>
    public string? LastStatusJson => _lastStatusJson;

    public AgentMonitorService(
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<AgentMonitorService> logger)
    {
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var agentUrl = (_config["Agent:LocalUrl"] ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(agentUrl))
        {
            _logger.LogInformation("Agent:LocalUrl not configured — AgentMonitorService idle");
            return;
        }

        var pollSeconds = _config.GetValue<int>("Agent:PollIntervalSeconds", 30);
        _logger.LogInformation(
            "AgentMonitorService started — probing {Url} every {Poll}s, auto-restart after {Threshold}min",
            agentUrl, pollSeconds, AutoRestartAfterMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProbeAsync(agentUrl, stoppingToken);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(pollSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProbeAsync(string agentUrl, CancellationToken ct)
    {
        try
        {
            using var http = _httpFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(3);
            var response = await http.GetAsync($"{agentUrl}/api/agent/health", ct);

            if (response.IsSuccessStatusCode)
            {
                _lastStatusJson = await response.Content.ReadAsStringAsync(ct);
                _reachable = true;
                _unreachableSince = null;
                _lastSeenAt = DateTime.UtcNow;
            }
            else
            {
                MarkUnreachable();
            }
        }
        catch (OperationCanceledException) { /* shutdown */ }
        catch
        {
            MarkUnreachable();
        }

        // Auto-restart check
        if (!_reachable && AutoRestartAfterMinutes > 0 && _unreachableSince.HasValue)
        {
            var elapsed = DateTime.UtcNow - _unreachableSince.Value;
            if (elapsed.TotalMinutes >= AutoRestartAfterMinutes)
            {
                // Avoid retrying too frequently — wait at least one full threshold interval
                var sinceLastAttempt = _lastRestartAttemptAt.HasValue
                    ? (DateTime.UtcNow - _lastRestartAttemptAt.Value).TotalMinutes
                    : double.MaxValue;

                if (sinceLastAttempt >= AutoRestartAfterMinutes)
                {
                    _logger.LogWarning(
                        "Agent unreachable for {Min:F1} min (threshold {Threshold} min) — triggering auto-restart",
                        elapsed.TotalMinutes, AutoRestartAfterMinutes);
                    var result = TryRestartService();
                    _logger.LogInformation("Auto-restart result: {Success} — {Message}", result.Success, result.Message);
                }
            }
        }
    }

    private void MarkUnreachable()
    {
        if (_reachable || !_unreachableSince.HasValue)
        {
            _unreachableSince = DateTime.UtcNow;
            _logger.LogWarning("UpdateAgent is no longer reachable at {Url}", _config["Agent:LocalUrl"]);
        }
        _reachable = false;
    }

    // ── Restart logic (shared by auto-restart and manual restart endpoint) ──

    /// <summary>
    /// Attempts to restart the Windows Service and records the attempt timestamp.
    /// Returns a structured result with an explicit success flag.
    /// </summary>
    public AgentRestartResult TryRestartService()
    {
        const string ServiceName = "EventForge Update Agent";

        _lastRestartAttemptAt = DateTime.UtcNow;

        if (!OperatingSystem.IsWindows())
        {
            _logger.LogWarning("Agent restart requested but not running on Windows");
            return new AgentRestartResult(false, "Il riavvio automatico è supportato solo su Windows.");
        }

        try
        {
            using var sc = new System.ServiceProcess.ServiceController(ServiceName);
            var status = sc.Status;

            if (status == System.ServiceProcess.ServiceControllerStatus.Running ||
                status == System.ServiceProcess.ServiceControllerStatus.Paused)
            {
                sc.Stop();
                sc.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Stopped,
                    TimeSpan.FromSeconds(15));
            }

            sc.Start();
            _logger.LogInformation("Agent service '{Name}' start command issued", ServiceName);
            return new AgentRestartResult(true, $"Servizio '{ServiceName}' avviato con successo.");
        }
        catch (System.ServiceProcess.TimeoutException)
        {
            _logger.LogWarning("Agent service stop timed out; attempting start anyway");
            try
            {
                using var sc2 = new System.ServiceProcess.ServiceController(ServiceName);
                sc2.Start();
                return new AgentRestartResult(true, $"Avvio inviato (stop timeout). Verificare lo stato del servizio.");
            }
            catch (Exception ex2)
            {
                _logger.LogError(ex2, "Agent restart fallback failed");
                return new AgentRestartResult(false, $"Riavvio fallito: {ex2.Message}");
            }
        }
        catch (InvalidOperationException ex)
            when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(ex, "Agent service '{Name}' not found", ServiceName);
            return new AgentRestartResult(false, $"Servizio '{ServiceName}' non trovato su questo host.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent restart failed");
            return new AgentRestartResult(false, $"Errore: {ex.Message}");
        }
    }

    public record AgentRestartResult(bool Success, string Message);
}
