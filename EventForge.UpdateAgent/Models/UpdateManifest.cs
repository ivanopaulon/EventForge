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

    /// <summary>
    /// Files inside binaries/ that must NOT be overwritten if they already exist in the deploy path.
    /// Use for files that are entirely managed by ops (e.g. web.config, which IIS owns).
    /// </summary>
    public List<string> PreserveFiles { get; set; } = [];

    /// <summary>
    /// JSON config files inside binaries/ that should be deep-merged with the existing file.
    /// New keys from the package template are added; existing keys/values in the deploy path are kept.
    /// This ensures production secrets survive upgrades while new config keys are picked up.
    /// Typical entries: appsettings.json, appsettings.Production.json.
    /// </summary>
    public List<string> MergeConfigFiles { get; set; } = [];

    /// <summary>Release notes for this package.</summary>
    public string? ReleaseNotes { get; set; }

    /// <summary>UTC timestamp when the package was built.</summary>
    public DateTime BuiltAt { get; set; }

    /// <summary>Short git commit SHA at build time.</summary>
    public string? GitCommit { get; set; }
}

/// <summary>Represents the phases an update goes through, reported to the hub.</summary>
public enum UpdatePhase
{
    Downloading,
    VerifyingChecksum,
    /// <summary>Download completed; waiting for an allowed maintenance window before installing.</summary>
    AwaitingMaintenanceWindow,
    BackingUp,
    RunningPreMigrations,
    StoppingService,
    DeployingBinaries,
    StartingService,
    RunningPostMigrations,
    VerifyingHealth,
    /// <summary>Post-deploy check: version.txt and key files verified on disk.</summary>
    VerifyingDeploy,
    Rollback,
    Completed
}
