using System.ComponentModel.DataAnnotations;

namespace EventForge.UpdateHub.Data.Entities;

/// <summary>
/// Represents a registered installation of EventForge (server, client, or both).
/// </summary>
public class Installation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Location { get; set; }

    /// <summary>Server, Client, or Both</summary>
    public InstallationComponents Components { get; set; }

    [MaxLength(50)]
    public string? InstalledVersionServer { get; set; }

    [MaxLength(50)]
    public string? InstalledVersionClient { get; set; }

    public InstallationStatus Status { get; set; } = InstallationStatus.Unknown;

    public DateTime? LastSeen { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>API key used by the agent to authenticate with the hub.</summary>
    [Required, MaxLength(200)]
    public string ApiKey { get; set; } = string.Empty;

    public ICollection<UpdateHistory> UpdateHistory { get; set; } = [];
}

public enum InstallationComponents { Server = 1, Client = 2, Both = 3 }

public enum InstallationStatus { Unknown, Online, Offline, Updating, Error }
