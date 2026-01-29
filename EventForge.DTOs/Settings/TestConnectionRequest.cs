namespace EventForge.DTOs.Settings;

public class TestConnectionRequest
{
    public string ServerAddress { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string AuthenticationType { get; set; } = "SQL"; // "SQL" or "Windows"
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool TrustServerCertificate { get; set; } = true;
}
