using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventForge.DTOs.SuperAdmin
{
    /// <summary>
    /// DTO for configuration settings.
    /// </summary>
    public class ConfigurationDto
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsEncrypted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for creating configuration settings.
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

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        public bool IsEncrypted { get; set; }
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

        public bool IsEncrypted { get; set; }
    }

    /// <summary>
    /// DTO for testing SMTP configuration.
    /// </summary>
    public class TestSmtpConfigurationDto
    {
        [Required]
        [EmailAddress]
        public string ToEmail { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Subject { get; set; } = "Test Email";

        [MaxLength(1000)]
        public string Body { get; set; } = "This is a test email to verify SMTP configuration.";
    }
}