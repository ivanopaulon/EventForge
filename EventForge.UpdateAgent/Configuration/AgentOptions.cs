namespace EventForge.UpdateAgent.Configuration;

public class AgentOptions
{
    public const string SectionName = "UpdateAgent";

    // ── Identity ─────────────────────────────────────────────────────────
    public string HubUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string InstallationId { get; set; } = string.Empty;
    public string InstallationName { get; set; } = string.Empty;

    /// <summary>Free-text physical location (e.g. "Magazzino Nord – PC-03").</summary>
    public string? Location { get; set; }

    /// <summary>Optional classification tags (e.g. ["production", "milan"]).</summary>
    public List<string> Tags { get; set; } = [];

    // ── Auto-enrollment ───────────────────────────────────────────────────
    /// <summary>
    /// Shared secret used to request a new API key from the Hub automatically.
    /// Must match the EnrollmentToken configured on the Hub.
    /// If ApiKey is empty and this token is set, the Agent will call
    /// POST {HubBaseUrl}/api/v1/enrollments on startup and save the received key.
    /// </summary>
    public string EnrollmentToken { get; set; } = string.Empty;

    /// <summary>
    /// Unique code identifying this installation, generated on first startup.
    /// Format: EF-{hostname8}-{yyyyMMddHHmmss}-{32hexrandom}
    /// Once generated it is stable and persisted to appsettings.json.
    /// </summary>
    public string InstallationCode { get; set; } = string.Empty;

    // ── Hub connection ────────────────────────────────────────────────────
    /// <summary>Base URL used to build package download URLs.</summary>
    public string HubBaseUrl { get; set; } = string.Empty;

    /// <summary>Seconds between heartbeat messages sent to the Hub.</summary>
    public int HeartbeatIntervalSeconds { get; set; } = 60;

    /// <summary>Seconds to wait before retrying the Hub connection after an error.</summary>
    public int ReconnectDelaySeconds { get; set; } = 30;

    // ── Download ──────────────────────────────────────────────────────────
    /// <summary>Per-request download timeout in minutes.</summary>
    public int DownloadTimeoutMinutes { get; set; } = 10;

    /// <summary>Maximum number of retry attempts after the initial download fails.</summary>
    public int DownloadMaxRetries { get; set; } = 5;

    // ── Maintenance windows ───────────────────────────────────────────────
    /// <summary>
    /// Time windows during which pending updates may be installed automatically.
    /// Empty = installation is allowed at any time (no restriction).
    /// </summary>
    public List<MaintenanceWindowOptions> MaintenanceWindows { get; set; } = [];

    // ── Nested option groups ──────────────────────────────────────────────
    public ComponentsOptions Components { get; set; } = new();
    public InstallOptions Install { get; set; } = new();
    public BackupAgentOptions Backup { get; set; } = new();
    public LoggingAgentOptions Logging { get; set; } = new();

    /// <summary>Local web UI (Razor Pages served on localhost).</summary>
    public UiOptions UI { get; set; } = new();
}

// ── Components ────────────────────────────────────────────────────────────

public class ComponentsOptions
{
    public ServerComponentOptions Server { get; set; } = new();
    public ClientComponentOptions Client { get; set; } = new();
}

public class ServerComponentOptions
{
    public bool Enabled { get; set; }
    public string IISSiteName { get; set; } = string.Empty;
    public string AppPoolName { get; set; } = string.Empty;
    public string DeployPath { get; set; } = string.Empty;
    public string HealthCheckUrl { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}

public class ClientComponentOptions
{
    public bool Enabled { get; set; }
    public string DeployPath { get; set; } = string.Empty;
}

// ── Install behaviour ─────────────────────────────────────────────────────

public class InstallOptions
{
    /// <summary>Maximum attempts for the post-deploy health check.</summary>
    public int HealthCheckMaxAttempts { get; set; } = 5;

    /// <summary>Seconds between consecutive health check attempts.</summary>
    public int HealthCheckDelaySeconds { get; set; } = 5;

    /// <summary>Seconds to wait after IIS start before considering the service warmed up.</summary>
    public int IisWarmupDelaySeconds { get; set; } = 5;

    /// <summary>SQL command timeout in seconds for migration scripts.</summary>
    public int SqlCommandTimeoutSeconds { get; set; } = 300;

    /// <summary>How often (in seconds) the ScheduledInstallWorker checks the pending queue.</summary>
    public int ScheduledCheckIntervalSeconds { get; set; } = 60;
}

// ── Backup ────────────────────────────────────────────────────────────────

public class BackupAgentOptions
{
    /// <summary>
    /// Maximum number of backup copies to retain per component.
    /// Older backups are deleted automatically when this limit is exceeded. 0 = unlimited.
    /// </summary>
    public int MaxBackupsToKeep { get; set; } = 10;

    /// <summary>
    /// Absolute or relative path for the backup root directory.
    /// Null/empty = {AppBaseDirectory}/backups (default).
    /// </summary>
    public string? RootPath { get; set; }
}

// ── Logging ───────────────────────────────────────────────────────────────

public class LoggingAgentOptions
{
    /// <summary>Number of daily log files to retain (Serilog rolling retention).</summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>
    /// Directory for agent log files.
    /// Null/empty = {AppBaseDirectory}/logs (default).
    /// </summary>
    public string? DirectoryPath { get; set; }
}

// ── Maintenance windows ───────────────────────────────────────────────────

public class MaintenanceWindowOptions
{
    /// <summary>Days of the week this window applies to. Empty = every day.</summary>
    public List<DayOfWeek> DaysOfWeek { get; set; } = [];

    /// <summary>Window start time in HH:mm (local time).</summary>
    public string StartTime { get; set; } = "00:00";

    /// <summary>
    /// Window end time in HH:mm (local time).
    /// May be earlier than StartTime for overnight windows (e.g. 23:00 -> 01:00).
    /// </summary>
    public string EndTime { get; set; } = "23:59";
}

// ── Local web UI ──────────────────────────────────────────────────────────

public class UiOptions
{
    /// <summary>TCP port the local web UI listens on (localhost only).</summary>
    public int Port { get; set; } = 5780;

    /// <summary>HTTP Basic auth username. Empty = UI disabled (returns 503).</summary>
    public string Username { get; set; } = "admin";

    /// <summary>HTTP Basic auth password. Change this before deploying.</summary>
    public string Password { get; set; } = "Admin#123!";
}
