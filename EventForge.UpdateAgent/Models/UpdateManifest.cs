namespace EventForge.UpdateAgent.Models;

/// <summary>
/// Describes the contents and apply-order of an update package zip.
/// Stored as manifest.json inside the zip root.
/// </summary>
public class UpdateManifest
{
    public string Version { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;

    /// <summary>SQL scripts to run BEFORE copying binaries (in order).</summary>
    public List<string> PreMigrationScripts { get; set; } = [];

    /// <summary>SQL scripts to run AFTER restarting the service (in order).</summary>
    public List<string> PostMigrationScripts { get; set; } = [];

    /// <summary>Optional rollback SQL scripts, run if post-deploy health check fails.</summary>
    public List<string> RollbackScripts { get; set; } = [];
}

/// <summary>Represents the phases an update goes through, reported to the hub.</summary>
public enum UpdatePhase
{
    Downloading,
    VerifyingChecksum,
    BackingUp,
    RunningPreMigrations,
    StoppingService,
    DeployingBinaries,
    StartingService,
    RunningPostMigrations,
    VerifyingHealth,
    Rollback,
    Completed
}
