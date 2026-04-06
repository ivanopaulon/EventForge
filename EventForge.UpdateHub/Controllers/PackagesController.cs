using System.Security.Cryptography;
using EventForge.UpdateHub.Configuration;
using EventForge.UpdateHub.Models;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.UpdateHub.Controllers;

/// <summary>Package management API.</summary>
[ApiController]
[Route("api/v1/packages")]
public class PackagesController(
    IPackageService packageService,
    UpdateHubOptions hubOptions,
    ILogger<PackagesController> logger) : ControllerBase
{
    private string PackageStorePath => hubOptions.PackageStorePath;

    private bool IsAdminAuthorized()
    {
        Request.Headers.TryGetValue("X-Admin-Key", out var key);
        return !string.IsNullOrWhiteSpace(hubOptions.AdminApiKey) && key == hubOptions.AdminApiKey;
    }

    /// <summary>Get all packages.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!IsAdminAuthorized()) return Unauthorized();
        var packages = await packageService.GetAllAsync();
        return Ok(packages.Select(p => new
        {
            p.Id, p.Version, p.Component, p.ReleaseNotes,
            p.Checksum, p.FileSizeBytes, p.UploadedAt, p.Status, p.IsManualInstall
        }));
    }

    /// <summary>Get latest package for a component (server or client).</summary>
    [HttpGet("latest/{component}")]
    public async Task<IActionResult> GetLatest(string component)
    {
        if (!IsAdminAuthorized()) return Unauthorized();
        if (!Enum.TryParse<PackageComponent>(component, true, out var comp))
            return BadRequest($"Invalid component. Use 'Server' or 'Client'.");

        var pkg = await packageService.GetLatestAsync(comp);
        if (pkg is null) return NotFound();
        return Ok(new { pkg.Id, pkg.Version, pkg.Component, pkg.Checksum, pkg.UploadedAt });
    }

    /// <summary>Upload a new update package.</summary>
    [HttpPost]
    [RequestSizeLimit(500_000_000)] // 500 MB
    public async Task<IActionResult> Upload(
        [FromQuery] string version,
        [FromQuery] string component,
        [FromQuery] string? releaseNotes,
        IFormFile file)
    {
        if (!IsAdminAuthorized()) return Unauthorized();
        if (!Enum.TryParse<PackageComponent>(component, true, out var comp))
            return BadRequest("Invalid component.");
        if (string.IsNullOrWhiteSpace(version)) return BadRequest("Version is required.");
        if (file is null || file.Length == 0) return BadRequest("File is required.");

        Directory.CreateDirectory(PackageStorePath);
        var fileName = $"{comp.ToString().ToLowerInvariant()}-{version}-{Guid.NewGuid():N}.zip";
        var filePath = Path.Combine(PackageStorePath, fileName);

        // Write the file and compute SHA-256 in a single stream pass for efficiency.
        string checksum;
        using (var fileStream = System.IO.File.Create(filePath))
        using (var hashAlg = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
        {
            var buffer = new byte[81920];
            using var source = file.OpenReadStream();
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer)) > 0)
            {
                hashAlg.AppendData(buffer, 0, bytesRead);
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            }
            checksum = Convert.ToHexStringLower(hashAlg.GetCurrentHash());
        }

        var package = new UpdatePackage
        {
            Version = version,
            Component = comp,
            ReleaseNotes = releaseNotes,
            Checksum = checksum,
            FilePath = fileName,
            FileSizeBytes = file.Length,
            UploadedBy = Request.Headers["X-Uploaded-By"].FirstOrDefault()
        };

        var created = await packageService.CreateAsync(package);
        logger.LogInformation("Package uploaded: {Version} {Component} {Checksum}", version, component, checksum);

        return CreatedAtAction(nameof(GetLatest), new { component }, new { created.Id, created.Version, created.Checksum });
    }

    /// <summary>Download a package file (agent-accessible with API key).</summary>
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        // Allowed for both admin and agents
        var isAdmin = IsAdminAuthorized();
        var isAgent = HttpContext.Items.ContainsKey("InstallationId");
        if (!isAdmin && !isAgent) return Unauthorized();

        var pkg = await packageService.GetByIdAsync(id);
        if (pkg is null) return NotFound();

        var fullPath = Path.Combine(PackageStorePath, pkg.FilePath);
        if (!System.IO.File.Exists(fullPath)) return NotFound("Package file not found on disk.");

        return PhysicalFile(Path.GetFullPath(fullPath), "application/zip", $"{pkg.Component}-{pkg.Version}.zip");
    }

    /// <summary>Update the status of a package (admin only).</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromQuery] string status)
    {
        if (!IsAdminAuthorized()) return Unauthorized();
        if (!Enum.TryParse<PackageStatus>(status, true, out var newStatus))
            return BadRequest($"Invalid status. Valid values: {string.Join(", ", Enum.GetNames<PackageStatus>())}");

        var pkg = await packageService.GetByIdAsync(id);
        if (pkg is null) return NotFound();

        await packageService.SetStatusAsync(id, newStatus);
        logger.LogInformation("Package {Id} status changed to {Status}", id, newStatus);
        return NoContent();
    }

    /// <summary>Get packages ready to deploy (admin only).</summary>
    [HttpGet("ready")]
    public async Task<IActionResult> GetReady()
    {
        if (!IsAdminAuthorized()) return Unauthorized();
        var packages = await packageService.GetByStatusAsync(PackageStatus.ReadyToDeploy);
        return Ok(packages.Select(p => new
        {
            p.Id, p.Version, p.Component, p.ReleaseNotes,
            p.Checksum, p.FileSizeBytes, p.UploadedAt, p.Status, p.GitCommit
        }));
    }
}
