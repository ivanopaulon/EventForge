using EventForge.DTOs.SuperAdmin;
using EventForge.Server.Services.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for SuperAdmin advanced operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
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
    [HttpGet("configuration")]
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
            return StatusCode(500, new { message = "Error retrieving configurations", error = ex.Message });
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
            return StatusCode(500, new { message = "Error retrieving configurations", error = ex.Message });
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
            return StatusCode(500, new { message = "Error retrieving categories", error = ex.Message });
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
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating configuration");
            return StatusCode(500, new { message = "Error creating configuration", error = ex.Message });
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
                return NotFound(new { message = $"Configuration with key '{key}' not found" });
            }
            return Ok(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration {Key}", key);
            return StatusCode(500, new { message = "Error retrieving configuration", error = ex.Message });
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
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration {Key}", key);
            return StatusCode(500, new { message = "Error updating configuration", error = ex.Message });
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
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration {Key}", key);
            return StatusCode(500, new { message = "Error deleting configuration", error = ex.Message });
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
            return StatusCode(500, new { message = "Error testing SMTP", error = ex.Message });
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
            return StatusCode(500, new { message = "Error reloading configuration", error = ex.Message });
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
            return StatusCode(500, new { message = "Error starting backup", error = ex.Message });
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
                return NotFound(new { message = $"Backup operation {backupId} not found" });
            }
            return Ok(backup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving backup status {BackupId}", backupId);
            return StatusCode(500, new { message = "Error retrieving backup status", error = ex.Message });
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
            return StatusCode(500, new { message = "Error retrieving backups", error = ex.Message });
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
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling backup {BackupId}", backupId);
            return StatusCode(500, new { message = "Error cancelling backup", error = ex.Message });
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
                return NotFound(new { message = "Backup file not found or not completed" });
            }

            return File(result.Value.FileStream, "application/zip", result.Value.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading backup {BackupId}", backupId);
            return StatusCode(500, new { message = "Error downloading backup", error = ex.Message });
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
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting backup {BackupId}", backupId);
            return StatusCode(500, new { message = "Error deleting backup", error = ex.Message });
        }
    }

    #endregion
}