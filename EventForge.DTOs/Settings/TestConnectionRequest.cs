namespace EventForge.DTOs.Settings;

using System.ComponentModel.DataAnnotations;

public class TestConnectionRequest
{
    [Required]
    public string ServerAddress { get; set; } = string.Empty;
    
    [Required]
    public string DatabaseName { get; set; } = string.Empty;
    
    [Required]
    public string AuthenticationType { get; set; } = "SQL"; // "SQL" or "Windows"
    
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool TrustServerCertificate { get; set; } = true;
}
