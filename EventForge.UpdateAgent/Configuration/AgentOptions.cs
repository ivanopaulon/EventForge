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

    public ComponentsOptions Components { get; set; } = new();
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
