using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prym.Hub.Services;

/// <summary>
/// Builds <see cref="UpdatePackage"/> records directly from a local publish/deploy folder.
/// Handles component detection, version/commit extraction and ZIP assembly.
/// </summary>
public class PackageBuildService(
    IPackageService packageService,
    UpdateHubOptions hubOptions,
    ILogger<PackageBuildService> logger) : IPackageBuildService
{
    // Files that must not overwrite existing production config (IIS-managed, ops-managed).
    private static readonly string[] DefaultPreserveFiles =
    [
        "web.config"
    ];

    // JSON config files that should be deep-merged (new keys added, existing values kept).
    private static readonly string[] DefaultMergeConfigFiles =
    [
        "appsettings.json",
        "appsettings.Production.json"
    ];

    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // ── Detection ─────────────────────────────────────────────────────────

    public Task<PackageFolderInfo> DetectFromFolderAsync(string folderPath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return Task.FromResult(new PackageFolderInfo(null, null, null,
                $"Cartella non trovata: {folderPath}"));

        var component = DetectComponent(folderPath);
        var (version, gitCommit) = DetectVersionAndCommit(folderPath, component);

        return Task.FromResult(new PackageFolderInfo(component, version, gitCommit));
    }

    private static PackageComponent? DetectComponent(string folderPath)
    {
        if (File.Exists(Path.Combine(folderPath, "Prym.Server.dll")) ||
            File.Exists(Path.Combine(folderPath, "Prym.Server.exe")))
            return PackageComponent.Server;

        if (File.Exists(Path.Combine(folderPath, "Prym.Client.dll")) ||
            Directory.Exists(Path.Combine(folderPath, "wwwroot")))
            return PackageComponent.Client;

        return null;
    }

    private static (string? Version, string? GitCommit) DetectVersionAndCommit(
        string folderPath, PackageComponent? component)
    {
        // 1. version.txt — also written by the build pipeline and read by UpdateAgent.
        var versionTxt = Path.Combine(folderPath, "version.txt");
        if (File.Exists(versionTxt))
        {
            var raw = File.ReadAllText(versionTxt).Trim();
            // May be just "1.2.3" or "1.2.3+gabcdef1" (NBGV format).
            var plusIdx = raw.IndexOf('+');
            var ver = plusIdx >= 0 ? raw[..plusIdx] : raw;
            if (!string.IsNullOrWhiteSpace(ver))
            {
                string? commit = null;
                if (plusIdx >= 0)
                {
                    var suffix = raw[(plusIdx + 1)..].TrimStart('g');
                    commit = TruncateCommit(suffix);
                }
                return (ver, commit);
            }
        }

        // 2. Assembly ProductVersion (contains NBGV InformationalVersion: "1.2.456+gabcdef1").
        string[] candidates = component switch
        {
            PackageComponent.Server => ["Prym.Server.dll", "Prym.Server.exe"],
            PackageComponent.Client => ["Prym.Client.dll"],
            _ => ["Prym.Server.dll", "Prym.Server.exe", "Prym.Client.dll"]
        };

        foreach (var name in candidates)
        {
            var asmPath = Path.Combine(folderPath, name);
            if (!File.Exists(asmPath)) continue;
            try
            {
                var fvi = FileVersionInfo.GetVersionInfo(asmPath);
                var infoVer = fvi.ProductVersion;
                if (string.IsNullOrWhiteSpace(infoVer)) continue;

                var plusIdx = infoVer.IndexOf('+');
                var ver = plusIdx >= 0 ? infoVer[..plusIdx] : infoVer;
                string? commit = null;
                if (plusIdx >= 0)
                {
                    var suffix = infoVer[(plusIdx + 1)..].TrimStart('g');
                    commit = TruncateCommit(suffix);
                }
                if (!string.IsNullOrWhiteSpace(ver))
                    return (ver, commit);
            }
            catch
            {
                // Unreadable assembly (e.g. native or corrupt) — try next candidate.
            }
        }

        return (null, null);
    }

    /// <summary>Returns the first 8 characters of a commit hash, or null if empty.</summary>
    private static string? TruncateCommit(string suffix) =>
        suffix.Length > 8 ? suffix[..8] : (suffix.Length > 0 ? suffix : null);

    // ── Build ─────────────────────────────────────────────────────────────

    public async Task<UpdatePackage> BuildFromFolderAsync(
        string folderPath,
        PackageComponent component,
        string version,
        string? releaseNotes,
        string? gitCommit,
        bool isManualInstall = false,
        CancellationToken ct = default)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Cartella deploy non trovata: {folderPath}");

        var storePath = hubOptions.PackageStorePath;
        Directory.CreateDirectory(storePath);

        var fileName = $"{component.ToString().ToLowerInvariant()}-{version}-{Guid.NewGuid():N}.zip";
        var zipPath = Path.Combine(storePath, fileName);

        logger.LogInformation("Building package {Component} {Version} from {Folder}",
            component, version, folderPath);

        try
        {
            await CreatePackageZipAsync(zipPath, folderPath, component, version,
                releaseNotes, gitCommit, ct);

            string checksum;
            await using (var fs = File.OpenRead(zipPath))
            {
                var hash = await SHA256.HashDataAsync(fs, ct);
                checksum = Convert.ToHexStringLower(hash);
            }

            if (await packageService.ExistsByChecksumAsync(checksum, ct))
            {
                File.Delete(zipPath);
                throw new InvalidOperationException(
                    "Un pacchetto identico (stesso checksum SHA-256) è già presente.");
            }

            var fileSize = new FileInfo(zipPath).Length;
            var package = new UpdatePackage
            {
                Version = version,
                Component = component,
                ReleaseNotes = string.IsNullOrWhiteSpace(releaseNotes) ? null : releaseNotes,
                Checksum = checksum,
                FilePath = fileName,
                FileSizeBytes = fileSize,
                GitCommit = gitCommit,
                Status = PackageStatus.ReadyToDeploy,
                IsManualInstall = isManualInstall,
                UploadedBy = "build"
            };

            var created = await packageService.CreateAsync(package, ct);
            logger.LogInformation(
                "Package built from folder: {Component} {Version} Id={Id} Size={Mb} MB",
                component, version, created.Id, Math.Round(fileSize / 1_048_576.0, 1));

            return created;
        }
        catch
        {
            if (File.Exists(zipPath))
                try { File.Delete(zipPath); } catch { /* best effort cleanup */ }
            throw;
        }
    }

    private static async Task CreatePackageZipAsync(
        string zipPath,
        string folderPath,
        PackageComponent component,
        string version,
        string? releaseNotes,
        string? gitCommit,
        CancellationToken ct)
    {
        var rootLen = folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length + 1;

        // Detect an optional top-level migrations/ folder. Supported sub-structure:
        //   migrations/pre/      → PreMigrationScripts  (ordered by name)
        //   migrations/post/     → PostMigrationScripts (ordered by name)
        //   migrations/rollback/ → RollbackScripts      (ordered by name)
        //   migrations/*.sql     → PreMigrationScripts  (flat layout, all .sql ordered by name)
        var migrationsRoot = Directory.Exists(Path.Combine(folderPath, "migrations"))
            ? Path.Combine(folderPath, "migrations")
            : Directory.Exists(Path.Combine(folderPath, "Migrations"))
                ? Path.Combine(folderPath, "Migrations")
                : null;

        var preMigrationScripts    = new List<string>();
        var postMigrationScripts   = new List<string>();
        var rollbackScripts        = new List<string>();

        if (migrationsRoot is not null)
        {
            // Sub-folder layout takes priority over flat layout.
            var preDir      = Path.Combine(migrationsRoot, "pre");
            var postDir     = Path.Combine(migrationsRoot, "post");
            var rollbackDir = Path.Combine(migrationsRoot, "rollback");

            if (Directory.Exists(preDir) || Directory.Exists(postDir) || Directory.Exists(rollbackDir))
            {
                if (Directory.Exists(preDir))
                    preMigrationScripts.AddRange(
                        Directory.EnumerateFiles(preDir, "*.sql", SearchOption.TopDirectoryOnly)
                                 .OrderBy(f => f)
                                 .Select(f => "migrations/pre/" + Path.GetFileName(f)));

                if (Directory.Exists(postDir))
                    postMigrationScripts.AddRange(
                        Directory.EnumerateFiles(postDir, "*.sql", SearchOption.TopDirectoryOnly)
                                 .OrderBy(f => f)
                                 .Select(f => "migrations/post/" + Path.GetFileName(f)));

                if (Directory.Exists(rollbackDir))
                    rollbackScripts.AddRange(
                        Directory.EnumerateFiles(rollbackDir, "*.sql", SearchOption.TopDirectoryOnly)
                                 .OrderBy(f => f)
                                 .Select(f => "migrations/rollback/" + Path.GetFileName(f)));
            }
            else
            {
                // Flat layout: all .sql files in migrations/ root → pre-migration scripts.
                preMigrationScripts.AddRange(
                    Directory.EnumerateFiles(migrationsRoot, "*.sql", SearchOption.TopDirectoryOnly)
                             .OrderBy(f => f)
                             .Select(f => "migrations/" + Path.GetFileName(f)));
            }
        }

        await using var fs = File.Create(zipPath);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false);

        // ── binaries/ — publish folder contents (migrations/ excluded from binaries) ──
        foreach (var file in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();

            var relative = file[rootLen..].Replace('\\', '/');

            // Skip the migrations folder from binaries — it is included separately at the zip root.
            if (migrationsRoot is not null &&
                (relative.StartsWith("migrations/", StringComparison.OrdinalIgnoreCase) ||
                 relative.StartsWith("Migrations/", StringComparison.OrdinalIgnoreCase)))
                continue;

            var entry = zip.CreateEntry($"binaries/{relative}", CompressionLevel.Optimal);
            await using var entryStream = entry.Open();
            await using var fileStream = File.OpenRead(file);
            await fileStream.CopyToAsync(entryStream, ct);
        }

        // ── migrations/ — placed at zip root so Agent can find them via manifest paths ──
        if (migrationsRoot is not null)
        {
            foreach (var file in Directory.EnumerateFiles(migrationsRoot, "*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();
                var migRootLen = migrationsRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length + 1;
                var relative = file[migRootLen..].Replace('\\', '/');
                var entry = zip.CreateEntry($"migrations/{relative}", CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await using var fileStream = File.OpenRead(file);
                await fileStream.CopyToAsync(entryStream, ct);
            }
        }

        // ── manifest.json ─────────────────────────────────────────────────────────────
        var manifest = new PackageManifest
        {
            Version = version,
            Component = component.ToString(),
            Checksum = string.Empty,
            PreMigrationScripts = [.. preMigrationScripts],
            PostMigrationScripts = [.. postMigrationScripts],
            RollbackScripts = [.. rollbackScripts],
            PreserveFiles = DefaultPreserveFiles,
            MergeConfigFiles = DefaultMergeConfigFiles,
            ReleaseNotes = releaseNotes,
            BuiltAt = DateTime.UtcNow,
            GitCommit = gitCommit
        };

        var manifestEntry = zip.CreateEntry("manifest.json", CompressionLevel.Optimal);
        await using var manifestStream = manifestEntry.Open();
        await JsonSerializer.SerializeAsync(manifestStream, manifest, ManifestJsonOptions, ct);
    }

    private sealed class PackageManifest
    {
        public string Version { get; init; } = string.Empty;
        public string Component { get; init; } = string.Empty;
        public string Checksum { get; init; } = string.Empty;
        public string[] PreMigrationScripts { get; init; } = [];
        public string[] PostMigrationScripts { get; init; } = [];
        public string[] RollbackScripts { get; init; } = [];
        public string[] PreserveFiles { get; init; } = [];
        public string[] MergeConfigFiles { get; init; } = [];
        public string? ReleaseNotes { get; init; }
        public DateTime BuiltAt { get; init; }
        public string? GitCommit { get; init; }
    }
}
