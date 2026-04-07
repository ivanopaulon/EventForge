namespace Prym.Hub.Configuration;

/// <summary>
/// Strongly-typed configuration for Prym UpdateHub.
/// Bound from the "UpdateHub" section of appsettings.json.
/// </summary>
public class UpdateHubOptions
{
    public const string SectionName = "UpdateHub";

    // ── Package storage ───────────────────────────────────────────────────
    /// <summary>Directory where validated packages are stored and served from.</summary>
    public string PackageStorePath { get; set; } = "packages";

    /// <summary>Directory monitored for new incoming packages (.zip) to be auto-ingested.</summary>
    public string IncomingPackagesPath { get; set; } = "packages/incoming";

    // ── Security ──────────────────────────────────────────────────────────
    /// <summary>
    /// API key required by the admin REST endpoints (X-Admin-Key header).
    /// Leave empty to disable admin API access.
    /// </summary>
    public string AdminApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Shared secret that an Agent must present to request a new API key automatically.
    /// Empty = auto-enrollment disabled (agents must be registered manually by an admin).
    /// </summary>
    public string EnrollmentToken { get; set; } = string.Empty;

    /// <summary>
    /// When true, a new Agent may self-register by presenting the correct EnrollmentToken.
    /// Requires <see cref="EnrollmentToken"/> to be non-empty.
    /// </summary>
    public bool AllowAutoEnrollment { get; set; } = false;

    // ── Hub identity ──────────────────────────────────────────────────────
    /// <summary>
    /// Public base URL of the Hub (used to build package download URLs sent to agents).
    /// E.g. "https://updatehub.example.com". If empty, the Hub derives it from the request.
    /// </summary>
    public string? BaseUrl { get; set; }

    // ── Agent monitoring ──────────────────────────────────────────────────
    /// <summary>
    /// Seconds after the last heartbeat before an installation is considered offline.
    /// </summary>
    public int HeartbeatTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Seconds between background checks that mark stale installations as Offline.
    /// </summary>
    public int AgentStatusCheckIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum number of concurrent active update operations across all installations.
    /// Additional requests are queued. 0 = unlimited.
    /// </summary>
    public int MaxConcurrentUpdates { get; set; } = 1;

    // ── Build from folder ─────────────────────────────────────────────────
    /// <summary>
    /// Default local path to the Server publish/deploy folder shown in the
    /// "Crea da cartella" UI form. Leave empty to show no default.
    /// </summary>
    public string? DefaultServerDeployPath { get; set; }

    /// <summary>
    /// Default local path to the Client publish/deploy folder shown in the
    /// "Crea da cartella" UI form. Leave empty to show no default.
    /// </summary>
    public string? DefaultClientDeployPath { get; set; }

    // ── Package management ────────────────────────────────────────────────
    /// <summary>Maximum allowed upload size for a single package file, in megabytes.</summary>
    public int MaxUploadSizeMb { get; set; } = 500;

    /// <summary>
    /// Number of days to retain archived/deployed packages on disk before automatic cleanup.
    /// 0 = keep forever.
    /// </summary>
    public int PackageRetentionDays { get; set; } = 90;

    /// <summary>How often (in hours) the background cleanup job runs. 0 = disabled.</summary>
    public int PackageCleanupIntervalHours { get; set; } = 24;

    // ── Nested option groups ──────────────────────────────────────────────
    /// <summary>Logging options for the Hub process itself.</summary>
    public HubLoggingOptions Logging { get; set; } = new();

    /// <summary>Local web UI (Razor Pages).</summary>
    public HubUiOptions UI { get; set; } = new();
}

// ── Logging ───────────────────────────────────────────────────────────────

public class HubLoggingOptions
{
    /// <summary>Number of daily log files to retain (Serilog rolling retention).</summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>
    /// Directory for Hub log files.
    /// Null/empty = {ContentRootPath}/logs (default).
    /// </summary>
    public string? DirectoryPath { get; set; }
}

// ── Local web UI ──────────────────────────────────────────────────────────

public class HubUiOptions
{
    /// <summary>
    /// HTTPS port Kestrel listens on when running standalone (not behind IIS/reverse-proxy).
    /// 0 = disabled. Default: 7244.
    /// When deployed under IIS the value is ignored — use the IIS site binding instead.
    /// </summary>
    public int HttpsPort { get; set; } = 7244;

    /// <summary>
    /// HTTP port Kestrel listens on when running standalone.
    /// 0 = disabled. Default: 7243.
    /// For standalone HTTPS-only deployments set this to 0.
    /// </summary>
    public int HttpPort { get; set; } = 7243;

    /// <summary>HTTP Basic auth username for the admin web UI. Change before deploying.</summary>
    public string Username { get; set; } = "admin";

    /// <summary>HTTP Basic auth password for the admin web UI. Change before deploying.</summary>
    public string Password { get; set; } = "Admin#123!";
}
