namespace EventForge.DTOs.Settings;

public class SaveConfigurationRequest
{
    // Database
    public string ServerAddress { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string AuthenticationType { get; set; } = "SQL";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool TrustServerCertificate { get; set; } = true;
    
    // JWT (optional)
    public string? JwtSecretKey { get; set; }
    public int JwtExpirationMinutes { get; set; } = 60;
    
    // Security
    public bool EnforceHttps { get; set; } = true;
    public bool EnableHsts { get; set; } = true;
}
