namespace EventForge.DTOs.Configuration;

/// <summary>
/// Public client-facing configuration returned by the server at startup.
/// Contains only values that are safe to expose to unauthenticated clients.
/// </summary>
public class ClientPublicConfigDto
{
    /// <summary>
    /// Syncfusion license key to be registered in the Blazor WASM client
    /// before AddSyncfusionBlazor() is called.
    /// </summary>
    public string? SyncfusionLicenseKey { get; set; }
}
