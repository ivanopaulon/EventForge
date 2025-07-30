using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for SuperAdmin advanced operations.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController : BaseApiController
{
    private readonly IConfigurationService _configurationService;
    private readonly IBackupService _backupService;
    private readonly ITenantService _tenantService;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IHubContext<AuditLogHub> _hubContext;
    private readonly ILogger<SuperAdminController> _logger;

    public SuperAdminController(
        IConfigurationService configurationService,
        IBackupService backupService,
        ITenantService tenantService,
        ITenantContext tenantContext,
        IAuditLogService auditLogService,
        IHubContext<AuditLogHub> hubContext,
        ILogger<SuperAdminController> logger)
    {
        _configurationService = configurationService;
        _backupService = backupService;
        _tenantService = tenantService;
        _tenantContext = tenantContext;
        _auditLogService = auditLogService;
        _hubContext = hubContext;
        _logger = logger;
    }

    #region Configuration Management

    /// <summary>
    /// Gets all configuration settings.
    /// </summary>
    /// <response code="200">Returns all configuration settings</response>
    /// <response code="500">If an error occurred while retrieving configurations</response>
    [HttpGet("configuration")]
    [ProducesResponseType(typeof(IEnumerable<ConfigurationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ConfigurationDto>>> GetConfigurations()
    {
        try
        {
            var configurations = await _configurationService.GetAllConfigurationsAsync();
            return Ok(configurations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configurations");
            return CreateInternalServerErrorProblem("Error retrieving configurations", ex);
        }
    }

    /// <summary>
    /// Gets configuration settings by category.
    /// </summary>
    [HttpGet("configuration/category/{category}")]
    public async Task<ActionResult<IEnumerable<ConfigurationDto>>> GetConfigurationsByCategory(string category)
    {
        try
        {
            var configurations = await _configurationService.GetConfigurationsByCategoryAsync(category);
            return Ok(configurations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configurations for category {Category}", category);
            return CreateInternalServerErrorProblem("Error retrieving configurations", ex);
        }
    }

    /// <summary>
    /// Gets all available configuration categories.
    /// </summary>
    [HttpGet("configuration/categories")]
    public async Task<ActionResult<IEnumerable<string>>> GetConfigurationCategories()
    {
        try
        {
            var categories = await _configurationService.GetCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration categories");
            return CreateInternalServerErrorProblem("Error retrieving categories", ex);
        }
    }

    /// <summary>
    /// Creates a new configuration setting.
    /// </summary>
    [HttpPost("configuration")]
    public async Task<ActionResult<ConfigurationDto>> CreateConfiguration([FromBody] CreateConfigurationDto createDto)
    {
        try
        {
            var configuration = await _configurationService.CreateConfigurationAsync(createDto);
            return CreatedAtAction(nameof(GetConfiguration), new { key = configuration.Key }, configuration);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating configuration");
            return CreateInternalServerErrorProblem("Error creating configuration", ex);
        }
    }

    /// <summary>
    /// Gets a specific configuration setting.
    /// </summary>
    [HttpGet("configuration/{key}")]
    public async Task<ActionResult<ConfigurationDto>> GetConfiguration(string key)
    {
        try
        {
            var configuration = await _configurationService.GetConfigurationAsync(key);
            if (configuration == null)
            {
                return CreateNotFoundProblem($"Configuration with key '{key}' not found");
            }
            return Ok(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration {Key}", key);
            return CreateInternalServerErrorProblem("Error retrieving configuration", ex);
        }
    }

    /// <summary>
    /// Updates a configuration setting.
    /// </summary>
    [HttpPut("configuration/{key}")]
    public async Task<ActionResult<ConfigurationDto>> UpdateConfiguration(string key, [FromBody] UpdateConfigurationDto updateDto)
    {
        try
        {
            var configuration = await _configurationService.UpdateConfigurationAsync(key, updateDto);
            return Ok(configuration);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration {Key}", key);
            return CreateInternalServerErrorProblem("Error updating configuration", ex);
        }
    }

    /// <summary>
    /// Deletes a configuration setting.
    /// </summary>
    [HttpDelete("configuration/{key}")]
    public async Task<IActionResult> DeleteConfiguration(string key)
    {
        try
        {
            await _configurationService.DeleteConfigurationAsync(key);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration {Key}", key);
            return CreateInternalServerErrorProblem("Error deleting configuration", ex);
        }
    }

    /// <summary>
    /// Tests SMTP configuration.
    /// </summary>
    [HttpPost("configuration/test-smtp")]
    public async Task<ActionResult<SmtpTestResultDto>> TestSmtp([FromBody] SmtpTestDto testDto)
    {
        try
        {
            var result = await _configurationService.TestSmtpAsync(testDto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing SMTP configuration");
            return CreateInternalServerErrorProblem("Error testing SMTP", ex);
        }
    }

    /// <summary>
    /// Reloads configuration from database.
    /// </summary>
    [HttpPost("configuration/reload")]
    public async Task<IActionResult> ReloadConfiguration()
    {
        try
        {
            await _configurationService.ReloadConfigurationAsync();
            return Ok(new { message = "Configuration reloaded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading configuration");
            return CreateInternalServerErrorProblem("Error reloading configuration", ex);
        }
    }

    #endregion

    #region Backup Management

    /// <summary>
    /// Starts a manual backup operation.
    /// </summary>
    [HttpPost("backup")]
    public async Task<ActionResult<BackupStatusDto>> StartBackup([FromBody] BackupRequestDto request)
    {
        try
        {
            var backup = await _backupService.StartBackupAsync(request);
            return Ok(backup);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting backup");
            return CreateInternalServerErrorProblem("Error starting backup", ex);
        }
    }

    /// <summary>
    /// Gets the status of a backup operation.
    /// </summary>
    [HttpGet("backup/{backupId}")]
    public async Task<ActionResult<BackupStatusDto>> GetBackupStatus(Guid backupId)
    {
        try
        {
            var backup = await _backupService.GetBackupStatusAsync(backupId);
            if (backup == null)
            {
                return CreateNotFoundProblem($"Backup operation {backupId} not found");
            }
            return Ok(backup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving backup status {BackupId}", backupId);
            return CreateInternalServerErrorProblem("Error retrieving backup status", ex);
        }
    }

    /// <summary>
    /// Gets all backup operations.
    /// </summary>
    [HttpGet("backup")]
    public async Task<ActionResult<IEnumerable<BackupStatusDto>>> GetBackups([FromQuery] int limit = 50)
    {
        try
        {
            var backups = await _backupService.GetBackupsAsync(limit);
            return Ok(backups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving backups");
            return CreateInternalServerErrorProblem("Error retrieving backups", ex);
        }
    }

    /// <summary>
    /// Cancels a running backup operation.
    /// </summary>
    [HttpPost("backup/{backupId}/cancel")]
    public async Task<IActionResult> CancelBackup(Guid backupId)
    {
        try
        {
            await _backupService.CancelBackupAsync(backupId);
            return Ok(new { message = "Backup cancelled successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling backup {BackupId}", backupId);
            return CreateInternalServerErrorProblem("Error cancelling backup", ex);
        }
    }

    /// <summary>
    /// Downloads a completed backup file.
    /// </summary>
    [HttpGet("backup/{backupId}/download")]
    public async Task<IActionResult> DownloadBackup(Guid backupId)
    {
        try
        {
            var result = await _backupService.DownloadBackupAsync(backupId);
            if (result == null)
            {
                return CreateNotFoundProblem("Backup file not found or not completed");
            }

            return File(result.Value.FileStream, "application/zip", result.Value.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading backup {BackupId}", backupId);
            return CreateInternalServerErrorProblem("Error downloading backup", ex);
        }
    }

    /// <summary>
    /// Deletes a backup operation and file.
    /// </summary>
    [HttpDelete("backup/{backupId}")]
    public async Task<IActionResult> DeleteBackup(Guid backupId)
    {
        try
        {
            await _backupService.DeleteBackupAsync(backupId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting backup {BackupId}", backupId);
            return CreateInternalServerErrorProblem("Error deleting backup", ex);
        }
    }

    #endregion
}