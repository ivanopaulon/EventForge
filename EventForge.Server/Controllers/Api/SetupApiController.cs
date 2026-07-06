using EventForge.Server.Services.Setup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prym.DTOs.Setup;

namespace EventForge.Server.Controllers.Api;

/// <summary>
/// API Controller for Setup Wizard operations.
/// </summary>
[ApiController]
[Route("api/v1/setup")]
[AllowAnonymous]
public class SetupApiController(
    IFirstRunDetectionService firstRunDetection,
    ISqlServerDiscoveryService sqlServerDiscovery,
    ISetupWizardService setupWizard,
    ILogger<SetupApiController> logger) : BaseApiController
{

    /// <summary>
    /// Detects if this is the first run (setup not completed).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if first run, false if setup is complete</returns>
    /// <response code="200">Returns first run status</response>
    /// <response code="500">An unexpected error occurred</response>
    [HttpGet("detect-first-run")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<bool>> DetectFirstRun(CancellationToken cancellationToken = default)
    {
        try
        {
            var isSetupComplete = await firstRunDetection.IsSetupCompleteAsync(cancellationToken);

            if (isSetupComplete)
            {
                logger.LogWarning("Attempt to access setup endpoints after setup completion");
                return Forbid();
            }

            return Ok(!isSetupComplete);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error detecting first run");
            return CreateInternalServerErrorProblem("Error checking setup status", ex);
        }
    }

    /// <summary>
    /// Discovers local SQL Server instances.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of discovered SQL Server instances</returns>
    /// <response code="200">Returns list of SQL Server instances</response>
    /// <response code="500">An unexpected error occurred</response>
    [HttpGet("discover-sql-servers")]
    [ProducesResponseType(typeof(List<SqlServerInstance>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<SqlServerInstance>>> DiscoverSqlServers(CancellationToken cancellationToken = default)
    {
        try
        {
            var isSetupComplete = await firstRunDetection.IsSetupCompleteAsync(cancellationToken);
            if (isSetupComplete)
            {
                logger.LogWarning("Attempt to access setup endpoints after setup completion");
                return Forbid();
            }

            var instances = await sqlServerDiscovery.DiscoverLocalInstancesAsync(cancellationToken);
            return Ok(instances);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error discovering SQL Server instances");
            return CreateInternalServerErrorProblem("Error discovering SQL Server instances", ex);
        }
    }

    /// <summary>
    /// Tests connection to a SQL Server with given credentials.
    /// </summary>
    /// <param name="request">Test connection request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection successful</returns>
    /// <response code="200">Returns connection test result</response>
    /// <response code="400">Server address is missing</response>
    [HttpPost("test-connection")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<bool>> TestConnection(
        [FromBody] TestConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ServerAddress))
            {
                return CreateValidationProblemDetails("Server address is required.");
            }

            var success = await sqlServerDiscovery.TestConnectionAsync(
                request.ServerAddress,
                request.Credentials,
                cancellationToken);

            return Ok(success);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error testing SQL Server connection");
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
    /// <response code="400">Server address is missing</response>
    /// <response code="500">An unexpected error occurred</response>
    [HttpPost("list-databases")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<string>>> ListDatabases(
        [FromBody] TestConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ServerAddress))
            {
                return CreateValidationProblemDetails("Server address is required.");
            }

            var databases = await sqlServerDiscovery.ListDatabasesAsync(
                request.ServerAddress,
                request.Credentials,
                cancellationToken);

            return Ok(databases);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing databases");
            return CreateInternalServerErrorProblem("Error listing databases", ex);
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
            var isSetupComplete = await firstRunDetection.IsSetupCompleteAsync(cancellationToken);
            if (isSetupComplete)
            {
                logger.LogWarning("Attempt to run setup after it's already complete");
                return Ok(new SetupResult
                {
                    Success = false,
                    Errors = new List<string> { "Setup has already been completed. This operation is not allowed." }
                });
            }


            var result = await setupWizard.CompleteSetupAsync(config, cancellationToken);

            if (result.Success)
            {
                logger.LogInformation("Setup wizard completed successfully");
            }
            else
            {
                logger.LogWarning("Setup wizard completed with errors: {Errors}", string.Join(", ", result.Errors));
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error completing setup wizard");
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
