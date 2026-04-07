using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Prym.Server.Controllers;

/// <summary>
/// Controller for SuperAdmin advanced operations.
/// </summary>
[ApiController]
[Route("api/v1/super-admin")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController(
    IConfigurationService configurationService,
    IBackupService backupService) : BaseApiController
{

    #region Configuration Management

    /// <summary>
    /// Gets all configuration settings.
    /// </summary>
    /// <response code="200">Returns all configuration settings</response>
    /// <response code="500">If an error occurred while retrieving configurations</response>
    [HttpGet("configuration")]
    [ProducesResponseType(typeof(IEnumerable<ConfigurationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ConfigurationDto>>> GetConfigurations(CancellationToken cancellationToken = default)
    {
        try
        {
            var configurations = await configurationService.GetAllConfigurationsAsync(cancellationToken);
            return Ok(configurations);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving configurations", ex);
        }
    }

    /// <summary>
    /// Gets configuration settings by category.
    /// </summary>
    [HttpGet("configuration/category/{category}")]
    [ProducesResponseType(typeof(IEnumerable<ConfigurationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ConfigurationDto>>> GetConfigurationsByCategory(string category, CancellationToken cancellationToken = default)
    {
        try
        {
            var configurations = await configurationService.GetConfigurationsByCategoryAsync(category, cancellationToken);
            return Ok(configurations);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving configurations", ex);
        }
    }

    /// <summary>
    /// Gets all available configuration categories.
    /// </summary>
    [HttpGet("configuration/categories")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<string>>> GetConfigurationCategories(CancellationToken cancellationToken = default)
    {
        try
        {
            var categories = await configurationService.GetCategoriesAsync(cancellationToken);
            return Ok(categories);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving categories", ex);
        }
    }

    /// <summary>
    /// Creates a new configuration setting.
    /// </summary>
    [HttpPost("configuration")]
    [ProducesResponseType(typeof(ConfigurationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ConfigurationDto>> CreateConfiguration([FromBody] CreateConfigurationDto createDto, CancellationToken cancellationToken = default)
    {
        try
        {
            var configuration = await configurationService.CreateConfigurationAsync(createDto, cancellationToken);
            return CreatedAtAction(nameof(GetConfiguration), new { key = configuration.Key }, configuration);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error creating configuration", ex);
        }
    }

    /// <summary>
    /// Gets a specific configuration setting.
    /// </summary>
    [HttpGet("configuration/{key}")]
    [ProducesResponseType(typeof(ConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ConfigurationDto>> GetConfiguration(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var configuration = await configurationService.GetConfigurationAsync(key, cancellationToken);
            if (configuration is null)
            {
                return CreateNotFoundProblem($"Configuration with key '{key}' not found");
            }
            return Ok(configuration);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving configuration", ex);
        }
    }

    /// <summary>
    /// Updates a configuration setting.
    /// </summary>
    [HttpPut("configuration/{key}")]
    [ProducesResponseType(typeof(ConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ConfigurationDto>> UpdateConfiguration(string key, [FromBody] UpdateConfigurationDto updateDto, CancellationToken cancellationToken = default)
    {
        try
        {
            var configuration = await configurationService.UpdateConfigurationAsync(key, updateDto, cancellationToken);
            return Ok(configuration);
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error updating configuration", ex);
        }
    }

    /// <summary>
    /// Deletes a configuration setting.
    /// </summary>
    [HttpDelete("configuration/{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteConfiguration(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await configurationService.DeleteConfigurationAsync(key, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error deleting configuration", ex);
        }
    }

    /// <summary>
    /// Tests SMTP configuration.
    /// </summary>
    [HttpPost("configuration/test-smtp")]
    [ProducesResponseType(typeof(SmtpTestResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SmtpTestResultDto>> TestSmtp([FromBody] SmtpTestDto testDto, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await configurationService.TestSmtpAsync(testDto, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error testing SMTP", ex);
        }
    }

    /// <summary>
    /// Reloads configuration from database.
    /// </summary>
    [HttpPost("configuration/reload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReloadConfiguration(CancellationToken cancellationToken = default)
    {
        try
        {
            await configurationService.ReloadConfigurationAsync(cancellationToken);
            return Ok(new { message = "Configuration reloaded successfully" });
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error reloading configuration", ex);
        }
    }

    #endregion

    #region Backup Management

    /// <summary>
    /// Starts a manual backup operation.
    /// </summary>
    [HttpPost("backup")]
    [ProducesResponseType(typeof(BackupStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BackupStatusDto>> StartBackup([FromBody] BackupRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var backup = await backupService.StartBackupAsync(request, cancellationToken);
            return Ok(backup);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error starting backup", ex);
        }
    }

    /// <summary>
    /// Gets the status of a backup operation.
    /// </summary>
    [HttpGet("backup/{backupId}")]
    [ProducesResponseType(typeof(BackupStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BackupStatusDto>> GetBackupStatus(Guid backupId, CancellationToken cancellationToken = default)
    {
        try
        {
            var backup = await backupService.GetBackupStatusAsync(backupId, cancellationToken);
            if (backup is null)
            {
                return CreateNotFoundProblem($"Backup operation {backupId} not found");
            }
            return Ok(backup);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving backup status", ex);
        }
    }

    /// <summary>
    /// Gets all backup operations.
    /// </summary>
    [HttpGet("backup")]
    [ProducesResponseType(typeof(IEnumerable<BackupStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<BackupStatusDto>>> GetBackups([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var backups = await backupService.GetBackupsAsync(limit, cancellationToken);
            return Ok(backups);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error retrieving backups", ex);
        }
    }

    /// <summary>
    /// Cancels a running backup operation.
    /// </summary>
    [HttpPost("backup/{backupId}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelBackup(Guid backupId, CancellationToken cancellationToken = default)
    {
        try
        {
            await backupService.CancelBackupAsync(backupId, cancellationToken);
            return Ok(new { message = "Backup cancelled successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error cancelling backup", ex);
        }
    }

    /// <summary>
    /// Downloads a completed backup file.
    /// </summary>
    [HttpGet("backup/{backupId}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadBackup(Guid backupId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await backupService.DownloadBackupAsync(backupId, cancellationToken);
            if (result is null)
            {
                return CreateNotFoundProblem("Backup file not found or not completed");
            }

            return File(result.Value.FileStream, "application/zip", result.Value.FileName);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error downloading backup", ex);
        }
    }

    /// <summary>
    /// Deletes a backup operation and file.
    /// </summary>
    [HttpDelete("backup/{backupId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteBackup(Guid backupId, CancellationToken cancellationToken = default)
    {
        try
        {
            await backupService.DeleteBackupAsync(backupId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return CreateValidationProblemDetails(ex.Message);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("Error deleting backup", ex);
        }
    }

    #endregion
}