using System.ComponentModel.DataAnnotations;

namespace EventForge.Server.DTOs.SuperAdmin;

/// <summary>
/// DTO for system configuration settings.
/// </summary>
public class ConfigurationDto
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;
    
    [Required]
    public string Value { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public string Category { get; set; } = "General";
    
    public bool IsEncrypted { get; set; } = false;
    
    public bool RequiresRestart { get; set; } = false;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? ModifiedAt { get; set; }
    
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// DTO for updating configuration settings.
/// </summary>
public class UpdateConfigurationDto
{
    [Required]
    public string Value { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public bool RequiresRestart { get; set; } = false;
}

/// <summary>
/// DTO for creating new configuration settings.
/// </summary>
public class CreateConfigurationDto
{
    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;
    
    [Required]
    public string Value { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    public string Category { get; set; } = "General";
    
    public bool IsEncrypted { get; set; } = false;
    
    public bool RequiresRestart { get; set; } = false;
}

/// <summary>
/// DTO for SMTP test configuration.
/// </summary>
public class SmtpTestDto
{
    [Required]
    [EmailAddress]
    public string ToEmail { get; set; } = string.Empty;
    
    [Required]
    public string Subject { get; set; } = string.Empty;
    
    [Required]
    public string Body { get; set; } = string.Empty;
}

/// <summary>
/// DTO for SMTP test result.
/// </summary>
public class SmtpTestResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
    public double DurationMs { get; set; }
}