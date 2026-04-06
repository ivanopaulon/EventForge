namespace EventForge.UpdateAgent.Configuration;

public class AgentOptions
{
    public const string SectionName = "UpdateAgent";

    public string HubUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string InstallationId { get; set; } = string.Empty;
    public string InstallationName { get; set; } = string.Empty;
    public int HeartbeatIntervalSeconds { get; set; } = 60;

    /// <summary>Base URL of the UpdateHub for downloading packages.</summary>
    public string HubBaseUrl { get; set; } = string.Empty;

    /// <summary>Per-request download timeout in minutes.</summary>
    public int DownloadTimeoutMinutes { get; set; } = 10;

    /// <summary>Maximum number of retry attempts after the initial download attempt fails.</summary>
    public int DownloadMaxRetries { get; set; } = 5;

    /// <summary>
    /// Maintenance windows during which pending updates may be installed.
    /// If empty, installation is allowed at any time (default behaviour).
    /// </summary>
    public List<MaintenanceWindowOptions> MaintenanceWindows { get; set; } = [];

    public ComponentsOptions Components { get; set; } = new();

    /// <summary>Local web UI settings (Razor Pages served on localhost).</summary>
    public UiOptions UI { get; set; } = new();
}

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

public class MaintenanceWindowOptions
{
    /// <summary>Days of the week this window applies to.</summary>
    public List<DayOfWeek> DaysOfWeek { get; set; } = [];

    /// <summary>Start time in HH:mm format (local time).</summary>
    public string StartTime { get; set; } = "00:00";

    /// <summary>End time in HH:mm format (local time). May be earlier than StartTime for overnight windows.</summary>
    public string EndTime { get; set; } = "23:59";
}

public class UiOptions
{
    /// <summary>Port the local web UI listens on.</summary>
    public int Port { get; set; } = 5780;

    /// <summary>HTTP Basic auth username. Leave empty to disable the UI entirely.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>HTTP Basic auth password.</summary>
    public string Password { get; set; } = string.Empty;
}
