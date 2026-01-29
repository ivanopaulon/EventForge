using EventForge.DTOs.Setup;
using EventForge.Server.Services.Setup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers.Api;

/// <summary>
/// API Controller for Setup Wizard operations.
/// </summary>
[ApiController]
[Route("api/v1/setup")]
[AllowAnonymous]
public class SetupApiController : ControllerBase
{
    private readonly IFirstRunDetectionService _firstRunDetection;
    private readonly ISqlServerDiscoveryService _sqlServerDiscovery;
    private readonly ISetupWizardService _setupWizard;
    private readonly ILogger<SetupApiController> _logger;

    public SetupApiController(
        IFirstRunDetectionService firstRunDetection,
        ISqlServerDiscoveryService sqlServerDiscovery,
        ISetupWizardService setupWizard,
        ILogger<SetupApiController> logger)
    {
        _firstRunDetection = firstRunDetection;
        _sqlServerDiscovery = sqlServerDiscovery;
        _setupWizard = setupWizard;
        _logger = logger;
    }

    /// <summary>
    /// Detects if this is the first run (setup not completed).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if first run, false if setup is complete</returns>
    /// <response code="200">Returns first run status</response>
    [HttpGet("detect-first-run")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> DetectFirstRun(CancellationToken cancellationToken = default)
    {
        try
        {
            var isSetupComplete = await _firstRunDetection.IsSetupCompleteAsync(cancellationToken);
            return Ok(!isSetupComplete);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting first run");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error checking setup status");
        }
    }

    /// <summary>
    /// Discovers local SQL Server instances.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of discovered SQL Server instances</returns>
    /// <response code="200">Returns list of SQL Server instances</response>
    [HttpGet("discover-sql-servers")]
    [ProducesResponseType(typeof(List<SqlServerInstance>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SqlServerInstance>>> DiscoverSqlServers(CancellationToken cancellationToken = default)
    {
        try
        {
            var instances = await _sqlServerDiscovery.DiscoverLocalInstancesAsync(cancellationToken);
            return Ok(instances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering SQL Server instances");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error discovering SQL Server instances");
        }
    }

    /// <summary>
    /// Tests connection to a SQL Server with given credentials.
    /// </summary>
    /// <param name="request">Test connection request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection successful</returns>
    /// <response code="200">Returns connection test result</response>
    [HttpPost("test-connection")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> TestConnection(
        [FromBody] TestConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ServerAddress))
            {
                return BadRequest("Server address is required");
            }

            var success = await _sqlServerDiscovery.TestConnectionAsync(
                request.ServerAddress,
                request.Credentials,
                cancellationToken);

            return Ok(success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing SQL Server connection");
            return Ok(false);
        }
    }

    /// <summary>
    /// Lists available databases on a SQL Server instance.
    /// </summary>
    /// <param name="request">List databases request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of database names</returns>
    /// <response code="200">Returns list of databases</response>
    [HttpPost("list-databases")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> ListDatabases(
        [FromBody] TestConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ServerAddress))
            {
                return BadRequest("Server address is required");
            }

            var databases = await _sqlServerDiscovery.ListDatabasesAsync(
                request.ServerAddress,
                request.Credentials,
                cancellationToken);

            return Ok(databases);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing databases");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error listing databases");
        }
    }

    /// <summary>
    /// Completes the setup wizard with the provided configuration.
    /// </summary>
    /// <param name="config">Setup configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Setup result</returns>
    /// <response code="200">Returns setup result</response>
    [HttpPost("complete")]
    [ProducesResponseType(typeof(SetupResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<SetupResult>> CompleteSetup(
        [FromBody] SetupConfiguration config,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting setup wizard completion...");

            var result = await _setupWizard.CompleteSetupAsync(config, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Setup wizard completed successfully");
            }
            else
            {
                _logger.LogWarning("Setup wizard completed with errors: {Errors}", string.Join(", ", result.Errors));
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing setup wizard");
            return Ok(new SetupResult
            {
                Success = false,
                Errors = new List<string> { $"Setup failed: {ex.Message}" }
            });
        }
    }
}

/// <summary>
/// Request model for testing SQL Server connection.
/// </summary>
public class TestConnectionRequest
{
    public string ServerAddress { get; set; } = string.Empty;
    public SqlCredentials Credentials { get; set; } = new();
}
