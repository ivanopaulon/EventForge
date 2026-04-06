namespace EventForge.Server.Services;

/// <summary>
/// Singleton BackgroundService that continuously probes the co-located UpdateAgent,
/// tracks unreachability duration, and automatically restarts the Windows Service
/// once the configured threshold is exceeded.
///
/// Configuration (Server appsettings.json, section "Agent"):
///   LocalUrl                — http://localhost:{agentPort}  (probe target)
///   PollIntervalSeconds     — how often to probe (default 30 s)
///   AutoRestartAfterMinutes — minutes of unreachability before auto-restart (0 = disabled)
/// </summary>
public sealed class AgentMonitorService(
    IHttpClientFactory httpFactory,
    IConfiguration config,
    ILogger<AgentMonitorService> logger) : BackgroundService
{
    // ── Mutable state protected by _lock ─────────────────────────────────────
    private readonly object _lock = new();
    private bool _reachable;
    private DateTime? _unreachableSince;
    private DateTime? _lastSeenAt;
    private DateTime? _lastRestartAttemptAt;
    private string? _lastStatusJson;

    // ── Thread-safe readers (snapshot under lock) ─────────────────────────────
    public bool Reachable { get { lock (_lock) return _reachable; } }
    public DateTime? UnreachableSince { get { lock (_lock) return _unreachableSince; } }
    public DateTime? LastSeenAt { get { lock (_lock) return _lastSeenAt; } }
    public string? LastStatusJson { get { lock (_lock) return _lastStatusJson; } }

    /// <summary>Minutes of unreachability before auto-restart. 0 = disabled.</summary>
    public int AutoRestartAfterMinutes =>
        config.GetValue<int>("Agent:AutoRestartAfterMinutes", 0);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var agentUrl = (config["Agent:LocalUrl"] ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(agentUrl))
        {
            logger.LogInformation("Agent:LocalUrl not configured — AgentMonitorService idle");
            return;
        }

        var pollSeconds = config.GetValue<int>("Agent:PollIntervalSeconds", 30);
        logger.LogInformation(
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
        bool reached;
        string? json = null;

        try
        {
            using var http = httpFactory.CreateClient("AgentClient");
            http.Timeout = TimeSpan.FromSeconds(3);
            var response = await http.GetAsync($"{agentUrl}/api/agent/health", ct);

            if (response.IsSuccessStatusCode)
            {
                json = await response.Content.ReadAsStringAsync(ct);
                reached = true;
            }
            else
            {
                reached = false;
            }
        }
        catch (OperationCanceledException) { return; }
        catch { reached = false; }

        // Update shared state atomically
        lock (_lock)
        {
            if (reached)
            {
                _reachable = true;
                _unreachableSince = null;
                _lastSeenAt = DateTime.UtcNow;
                _lastStatusJson = json;
            }
            else
            {
                if (_reachable || !_unreachableSince.HasValue)
                {
                    _unreachableSince = DateTime.UtcNow;
                    logger.LogWarning("UpdateAgent is no longer reachable at {Url}", config["Agent:LocalUrl"]);
                }
                _reachable = false;
            }
        }

        // Auto-restart check (reads are consistent via local copies)
        if (!reached)
        {
            DateTime? unreachableSince;
            DateTime? lastAttempt;
            lock (_lock) { unreachableSince = _unreachableSince; lastAttempt = _lastRestartAttemptAt; }

            var threshold = AutoRestartAfterMinutes;
            if (threshold > 0 && unreachableSince.HasValue)
            {
                var elapsed = DateTime.UtcNow - unreachableSince.Value;
                var sinceLastAttempt = lastAttempt.HasValue
                    ? (DateTime.UtcNow - lastAttempt.Value).TotalMinutes
                    : double.MaxValue;

                if (elapsed.TotalMinutes >= threshold && sinceLastAttempt >= threshold)
                {
                    logger.LogWarning(
                        "Agent unreachable for {Min:F1} min (threshold {Threshold} min) — triggering auto-restart",
                        elapsed.TotalMinutes, threshold);
                    var result = TryRestartService();
                    logger.LogInformation("Auto-restart result: {Success} — {Message}", result.Success, result.Message);
                }
            }
        }
    }

    // ── Restart logic (shared by auto-restart and manual restart endpoint) ──

    /// <summary>
    /// Attempts to restart the Windows Service and records the attempt timestamp.
    /// Returns a structured result with an explicit success flag.
    /// </summary>
    public AgentRestartResult TryRestartService()
    {
        const string ServiceName = "EventForge Update Agent";

        lock (_lock) { _lastRestartAttemptAt = DateTime.UtcNow; }

        if (!OperatingSystem.IsWindows())
        {
            logger.LogWarning("Agent restart requested but not running on Windows");
            return new AgentRestartResult(false, "Il riavvio automatico è supportato solo su Windows.");
        }

        try
        {
            using var sc = new System.ServiceProcess.ServiceController(ServiceName);
            var status = sc.Status;

            if (status is System.ServiceProcess.ServiceControllerStatus.Running
                      or System.ServiceProcess.ServiceControllerStatus.Paused)
            {
                sc.Stop();
                sc.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Stopped,
                    TimeSpan.FromSeconds(15));
            }

            sc.Start();
            logger.LogInformation("Agent service '{Name}' start command issued", ServiceName);
            return new AgentRestartResult(true, $"Servizio '{ServiceName}' avviato con successo.");
        }
        catch (System.ServiceProcess.TimeoutException)
        {
            logger.LogWarning("Agent service stop timed out; attempting start anyway");
            try
            {
                using var sc2 = new System.ServiceProcess.ServiceController(ServiceName);
                sc2.Start();
                return new AgentRestartResult(true,
                    "Avvio inviato (stop timeout). Verificare lo stato del servizio.");
            }
            catch (Exception ex2)
            {
                logger.LogError(ex2, "Agent restart fallback failed");
                return new AgentRestartResult(false, $"Riavvio fallito: {ex2.Message}");
            }
        }
        catch (InvalidOperationException ex)
            when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(ex, "Agent service '{Name}' not found", ServiceName);
            return new AgentRestartResult(false, $"Servizio '{ServiceName}' non trovato su questo host.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Agent restart failed");
            return new AgentRestartResult(false, $"Errore: {ex.Message}");
        }
    }

    public record AgentRestartResult(bool Success, string Message);
}
