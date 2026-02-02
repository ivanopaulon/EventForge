namespace EventForge.DTOs.Configuration;

/// <summary>
/// DTO for branding configuration, including global settings and tenant overrides.
/// </summary>
public class BrandingConfigurationDto
{
    /// <summary>
    /// Logo URL for the application.
    /// </summary>
    public string LogoUrl { get; set; } = "/eventforgetitle.svg";

    /// <summary>
    /// Logo height in pixels.
    /// </summary>
    public int LogoHeight { get; set; } = 40;

    /// <summary>
    /// Application name displayed in UI.
    /// </summary>
    public string ApplicationName { get; set; } = "EventForge";

    /// <summary>
    /// Favicon URL for the application.
    /// </summary>
    public string FaviconUrl { get; set; } = "/trace.svg";

    /// <summary>
    /// Indicates if this configuration is a tenant override.
    /// </summary>
    public bool IsTenantOverride { get; set; }

    /// <summary>
    /// Tenant ID if this is a tenant override, null for global configuration.
    /// </summary>
    public Guid? TenantId { get; set; }
}

/// <summary>
/// DTO for updating branding configuration.
/// </summary>
public class UpdateBrandingDto
{
    /// <summary>
    /// Logo URL for the application (optional).
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Logo height in pixels (optional).
    /// </summary>
    public int? LogoHeight { get; set; }

    /// <summary>
    /// Application name displayed in UI (optional).
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Favicon URL for the application (optional).
    /// </summary>
    public string? FaviconUrl { get; set; }
}
