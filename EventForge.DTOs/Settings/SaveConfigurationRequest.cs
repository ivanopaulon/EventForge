namespace EventForge.DTOs.Settings;

using System.ComponentModel.DataAnnotations;

public class SaveConfigurationRequest
{
    // Database
    [Required]
    public string ServerAddress { get; set; } = string.Empty;
    
    [Required]
    public string DatabaseName { get; set; } = string.Empty;
    
    [Required]
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
