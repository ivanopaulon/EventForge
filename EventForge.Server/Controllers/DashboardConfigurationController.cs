using EventForge.DTOs.Dashboard;
using EventForge.Server.Services.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for managing dashboard configurations.
/// </summary>
[Authorize]
[Route("api/v1/[controller]")]
public class DashboardConfigurationController : BaseApiController
{
    private readonly IDashboardConfigurationService _service;
    private readonly ILogger<DashboardConfigurationController> _logger;

    public DashboardConfigurationController(
        IDashboardConfigurationService service,
        ILogger<DashboardConfigurationController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Gets all dashboard configurations for the current user and entity type.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<DashboardConfigurationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DashboardConfigurationDto>>> GetConfigurations([FromQuery] string entityType)
    {
        try
        {
            var configurations = await _service.GetConfigurationsAsync(entityType);
            return Ok(configurations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard configurations for entity type {EntityType}", entityType);
            return StatusCode(500, "An error occurred while retrieving dashboard configurations");
        }
    }

    /// <summary>
    /// Gets a specific dashboard configuration by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DashboardConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DashboardConfigurationDto>> GetConfigurationById(Guid id)
    {
        try
        {
            var configuration = await _service.GetConfigurationByIdAsync(id);
            if (configuration == null)
            {
                return NotFound();
            }
            return Ok(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard configuration {ConfigurationId}", id);
            return StatusCode(500, "An error occurred while retrieving the dashboard configuration");
        }
    }

    /// <summary>
    /// Gets the default dashboard configuration for an entity type.
    /// </summary>
    [HttpGet("default/{entityType}")]
    [ProducesResponseType(typeof(DashboardConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DashboardConfigurationDto>> GetDefaultConfiguration(string entityType)
    {
        try
        {
            var configuration = await _service.GetDefaultConfigurationAsync(entityType);
            if (configuration == null)
            {
                return NotFound();
            }
            return Ok(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default dashboard configuration for entity type {EntityType}", entityType);
            return StatusCode(500, "An error occurred while retrieving the default dashboard configuration");
        }
    }

    /// <summary>
    /// Creates a new dashboard configuration.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DashboardConfigurationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DashboardConfigurationDto>> CreateConfiguration([FromBody] CreateDashboardConfigurationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var configuration = await _service.CreateConfigurationAsync(dto);
            return CreatedAtAction(nameof(GetConfigurationById), new { id = configuration.Id }, configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating dashboard configuration");
            return StatusCode(500, "An error occurred while creating the dashboard configuration");
        }
    }

    /// <summary>
    /// Updates an existing dashboard configuration.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(DashboardConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DashboardConfigurationDto>> UpdateConfiguration(Guid id, [FromBody] UpdateDashboardConfigurationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var configuration = await _service.UpdateConfigurationAsync(id, dto);
            return Ok(configuration);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dashboard configuration {ConfigurationId}", id);
            return StatusCode(500, "An error occurred while updating the dashboard configuration");
        }
    }

    /// <summary>
    /// Deletes a dashboard configuration.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteConfiguration(Guid id)
    {
        try
        {
            await _service.DeleteConfigurationAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting dashboard configuration {ConfigurationId}", id);
            return StatusCode(500, "An error occurred while deleting the dashboard configuration");
        }
    }

    /// <summary>
    /// Sets a configuration as default for its entity type.
    /// </summary>
    [HttpPost("{id}/set-default")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SetAsDefault(Guid id)
    {
        try
        {
            await _service.SetAsDefaultAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting dashboard configuration as default {ConfigurationId}", id);
            return StatusCode(500, "An error occurred while setting the dashboard configuration as default");
        }
    }
}
