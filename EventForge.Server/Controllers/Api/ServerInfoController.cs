using EventForge.DTOs.ServerInfo;
using EventForge.Server.Services.Setup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection;

namespace EventForge.Server.Controllers.Api;

/// <summary>
/// REST API controller for public server information.
/// </summary>
[Route("api/v1/server")]
[ApiController]
[Produces("application/json")]
public class ServerInfoController : ControllerBase
{
    private readonly IFirstRunDetectionService _firstRunService;
    private readonly ILogger<ServerInfoController> _logger;
    private readonly IConfiguration _configuration;

    public ServerInfoController(
        IFirstRunDetectionService firstRunService,
        ILogger<ServerInfoController> logger,
        IConfiguration configuration)
    {
        _firstRunService = firstRunService ?? throw new ArgumentNullException(nameof(firstRunService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Gets public server information (version, uptime, environment).
    /// </summary>
    /// <returns>Server information</returns>
    /// <response code="200">Returns server information</response>
    [HttpGet("info")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServerInfoDto), StatusCodes.Status200OK)]
    public ActionResult<ServerInfoDto> GetServerInfo()
    {
        var uptime = (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalSeconds;
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        return Ok(new ServerInfoDto
        {
            Version = version,
            Uptime = (int)uptime,
            Environment = environment,
            ServerTime = DateTime.UtcNow
        });
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
            InformationalVersion = informationalVersion ?? version?.ToString() ?? "1.0.0",
            Environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production"
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
/// DTO for server information.
/// </summary>
public class ServerInfoDto
{
    /// <summary>
    /// Server version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Server uptime in seconds.
    /// </summary>
    public int Uptime { get; set; }

    /// <summary>
    /// Server environment (Development/Production/Staging).
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Current server time (UTC).
    /// </summary>
    public DateTime ServerTime { get; set; }
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
