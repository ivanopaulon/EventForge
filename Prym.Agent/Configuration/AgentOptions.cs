namespace Prym.Agent.Configuration;

public class AgentOptions
{
    public const string SectionName = "PrymAgent";

    // ── Identity ─────────────────────────────────────────────────────────
    /// <summary>SignalR endpoint URL of the UpdateHub (e.g. "https://updatehub.example.com/hubs/update").</summary>
    public string HubUrl { get; set; } = string.Empty;

    /// <summary>API key issued by the Hub to authenticate this agent. Set automatically after successful enrollment.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Stable GUID assigned by the Hub when this installation was enrolled. Persisted after first enrollment.</summary>
    public string InstallationId { get; set; } = string.Empty;

    /// <summary>Human-readable display name for this installation (e.g. "Magazzino Nord – Server").</summary>
    public string InstallationName { get; set; } = string.Empty;

    /// <summary>Free-text physical location (e.g. "Magazzino Nord – PC-03").</summary>
    public string? Location { get; set; }

    /// <summary>Optional classification tags (e.g. ["production", "milan"]).</summary>
    public List<string> Tags { get; set; } = [];

    // ── Standalone / printer-proxy-only mode ─────────────────────────────
    /// <summary>
    /// When <see langword="true"/>, this agent runs in standalone (printer-proxy-only) mode:
    /// it does <b>not</b> connect to the UpdateHub, does not manage Server/Client component
    /// updates, and serves exclusively as a printer proxy for fiscal printers on its local network.
    /// <para>
    /// Use this mode on dedicated POS terminals or print servers that have no EventForge
    /// Server/Client installed locally but need to expose USB or TCP printers to the
    /// EventForge Server over HTTP.
    /// </para>
    /// </summary>
    public bool StandaloneMode { get; set; }

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
    /// <summary>
    /// Base URL of the UpdateHub used to build package download URLs (e.g. "https://updatehub.example.com").
    /// If empty, the Hub URL is derived from <see cref="HubUrl"/> by stripping the SignalR path.
    /// </summary>
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

    /// <summary>
    /// Directory (relative to the service exe or absolute) where package ZIP files are downloaded
    /// and temporarily extracted during installation.
    /// Defaults to <c>work</c> → resolves to <c>{ServiceExeDir}\work\</c>.
    /// </summary>
    public string WorkPath { get; set; } = "work";

    /// <summary>
    /// Directory (relative to the service exe or absolute) where successfully installed package
    /// ZIP files are moved after installation, so they can be inspected or rolled back manually.
    /// Defaults to <c>processed</c> → resolves to <c>{ServiceExeDir}\processed\</c>.
    /// Set to empty string to skip archiving and delete immediately after install.
    /// </summary>
    public string ProcessedPackagesPath { get; set; } = "processed";

    // ── Maintenance windows ───────────────────────────────────────────────
    /// <summary>
    /// Time windows during which pending updates may be installed automatically.
    /// Empty = installation is allowed at any time (no restriction).
    /// </summary>
    public List<MaintenanceWindowOptions> MaintenanceWindows { get; set; } = [];

    // ── Nested option groups ──────────────────────────────────────────────
    /// <summary>Per-component deployment configuration (Server and Client).</summary>
    public ComponentsOptions Components { get; set; } = new();

    /// <summary>Install-time behaviour (health check retries, IIS warm-up, SQL timeout, etc.).</summary>
    public InstallOptions Install { get; set; } = new();
    public BackupAgentOptions Backup { get; set; } = new();
    public LoggingAgentOptions Logging { get; set; } = new();

    /// <summary>Local web UI (Razor Pages served on localhost).</summary>
    public UiOptions UI { get; set; } = new();
}

// ── Components ────────────────────────────────────────────────────────────

/// <summary>Groups the Server and Client component configuration sections.</summary>
public class ComponentsOptions
{
    /// <summary>Configuration for the EventForge Server component.</summary>
    public ServerComponentOptions Server { get; set; } = new();

    /// <summary>Configuration for the EventForge Client component.</summary>
    public ClientComponentOptions Client { get; set; } = new();
}

/// <summary>Deployment settings for the EventForge Server component.</summary>
public class ServerComponentOptions
{
    /// <summary>When <see langword="true"/>, the Server component is managed by this agent.</summary>
    public bool Enabled { get; set; }

    /// <summary>Name of the IIS site to stop/start during deployment.</summary>
    public string IISSiteName { get; set; } = string.Empty;

    /// <summary>Name of the IIS application pool to stop/start during deployment.</summary>
    public string AppPoolName { get; set; } = string.Empty;

    /// <summary>Absolute path to the Server deployment directory on disk.</summary>
    public string DeployPath { get; set; } = string.Empty;

    /// <summary>URL polled by the post-deploy health check (e.g. "http://localhost/api/v1/health").</summary>
    public string HealthCheckUrl { get; set; } = string.Empty;

    /// <summary>SQL Server connection string used to execute pre/post migration scripts.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Base URL of the running EventForge.Server (e.g. "http://localhost:5000") used to send
    /// maintenance notifications before/after IIS stop/start.
    /// Leave empty to skip notifications.
    /// </summary>
    public string NotificationBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Shared secret that must match <c>UpdateHub:MaintenanceSecret</c> on EventForge.Server.
    /// Sent as the <c>X-Maintenance-Secret</c> request header.
    /// </summary>
    public string MaintenanceSecret { get; set; } = string.Empty;
}

/// <summary>Deployment settings for the EventForge Client (Blazor WebAssembly) component.</summary>
public class ClientComponentOptions
{
    /// <summary>When <see langword="true"/>, the Client component is managed by this agent.</summary>
    public bool Enabled { get; set; }

    /// <summary>Absolute path to the Client deployment directory on disk.</summary>
    public string DeployPath { get; set; } = string.Empty;

    /// <summary>
    /// Base URL of the running EventForge.Server (e.g. "http://localhost:5000") used to send
    /// a client-deployed notification after the new static files have been deployed.
    /// Leave empty to skip notifications.
    /// </summary>
    public string NotificationBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Shared secret that must match <c>UpdateHub:MaintenanceSecret</c> on EventForge.Server.
    /// </summary>
    public string MaintenanceSecret { get; set; } = string.Empty;
}

// ── Install behaviour ─────────────────────────────────────────────────────

/// <summary>Controls the behaviour of the installation and post-deploy verification steps.</summary>
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

    /// <summary>
    /// Number of consecutive installation failures after which an automatic package is
    /// downgraded to manual-install mode, requiring explicit operator approval to retry.
    /// Set to 0 to disable auto-downgrade (not recommended).
    /// </summary>
    public int MaxAutoRetries { get; set; } = 3;
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

    /// <summary>
    /// When <see langword="true"/>, the agent forwards log batches to the EventForge Server
    /// at <see cref="ServerIngestUrl"/> using the <c>X-Maintenance-Secret</c> header.
    /// If the Server is unreachable the log is still available in the local rolling file —
    /// no data is lost. Defaults to <see langword="true"/>.
    /// </summary>
    public bool ServerIngestEnabled { get; set; } = true;

    /// <summary>
    /// Base URL of the EventForge Server to which agent logs are forwarded
    /// (e.g. "https://localhost:7242").  The endpoint
    /// <c>/api/v1/agent-logs/batch</c> is appended automatically.
    /// When null or empty the value is derived from
    /// <see cref="ServerComponentOptions.NotificationBaseUrl"/> at runtime.
    /// </summary>
    public string? ServerIngestUrl { get; set; }
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
