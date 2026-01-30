using EventForge.DTOs.ServerInfo;
using EventForge.Server.Services.Setup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection;

namespace EventForge.Server.Controllers.Api;

/// <summary>
/// REST API controller for public server information.
/// Provides version info and first-run detection for setup wizard.
/// For comprehensive health/status info, use /api/v1/health/detailed
/// </summary>
[Route("api/v1/server")]
[ApiController]
[Produces("application/json")]
public class ServerInfoController : ControllerBase
{
    private readonly IFirstRunDetectionService _firstRunService;
    private readonly ILogger<ServerInfoController> _logger;

    public ServerInfoController(
        IFirstRunDetectionService firstRunService,
        ILogger<ServerInfoController> logger)
    {
        _firstRunService = firstRunService ?? throw new ArgumentNullException(nameof(firstRunService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets server version information from assembly.
    /// </summary>
    /// <returns>Server version details</returns>
    /// <response code="200">Returns server version information</response>
    [HttpGet("version")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServerVersionDto), StatusCodes.Status200OK)]
    public ActionResult<ServerVersionDto> GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        
        return Ok(new ServerVersionDto
        {
            Version = version?.ToString() ?? "1.0.0",
            InformationalVersion = informationalVersion ?? version?.ToString() ?? "1.0.0"
        });
    }

    /// <summary>
    /// Checks if the server is in first-run mode (requires setup).
    /// </summary>
    /// <returns>First run status</returns>
    /// <response code="200">Returns first run status</response>
    [HttpGet("first-run")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FirstRunDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FirstRunDto>> CheckFirstRun()
    {
        var isSetupComplete = await _firstRunService.IsSetupCompleteAsync();

        return Ok(new FirstRunDto
        {
            IsFirstRun = !isSetupComplete
        });
    }
}

/// <summary>
/// DTO for server version.
/// </summary>
public class ServerVersionDto
{
    /// <summary>
    /// Server version from assembly.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Informational version (includes pre-release info).
    /// </summary>
    public string InformationalVersion { get; set; } = string.Empty;
}

/// <summary>
/// DTO for first run check.
/// </summary>
public class FirstRunDto
{
    /// <summary>
    /// Indicates if the server is in first-run mode.
    /// </summary>
    public bool IsFirstRun { get; set; }
}
