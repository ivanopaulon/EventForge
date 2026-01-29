namespace EventForge.DTOs.Setup;

/// <summary>
/// Complete setup wizard configuration.
/// </summary>
public class SetupConfiguration
{
    // SQL Server Configuration
    public string ServerAddress { get; set; } = string.Empty;
    public SqlCredentials Credentials { get; set; } = new();
    public string DatabaseName { get; set; } = "EventForge";
    public bool CreateDatabase { get; set; } = true;

    // Network Configuration
    public string Environment { get; set; } = "Kestrel"; // Kestrel, IIS
    public int? HttpPort { get; set; }
    public int? HttpsPort { get; set; }

    // Security Settings
    public string JwtSecretKey { get; set; } = string.Empty;
    public int TokenExpirationMinutes { get; set; } = 60;
    public bool RateLimitingEnabled { get; set; } = true;
    public int LoginAttemptsLimit { get; set; } = 5;
    public int ApiCallsLimit { get; set; } = 100;
    public bool EnforceHttps { get; set; } = true;
    public bool EnableHsts { get; set; } = true;

    // SuperAdmin Account
    public string SuperAdminUsername { get; set; } = "superadmin";
    public string SuperAdminEmail { get; set; } = string.Empty;
    public string SuperAdminPassword { get; set; } = string.Empty;

    // Log Settings
    public int LogRetentionDays { get; set; } = 30;

    // Seed Data
    public bool SeedDefaultData { get; set; } = true;
}
