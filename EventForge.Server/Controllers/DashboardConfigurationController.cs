using Prym.DTOs.Dashboard;
using EventForge.Server.Services.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventForge.Server.Controllers;

/// <summary>
/// Controller for managing dashboard configurations.
/// </summary>
[Authorize]
[Route("api/v1/[controller]")]
public class DashboardConfigurationController(
    IDashboardConfigurationService service) : BaseApiController
{

    /// <summary>
    /// Gets all dashboard configurations for the current user and entity type.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<DashboardConfigurationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<DashboardConfigurationDto>>> GetConfigurations([FromQuery] string entityType, CancellationToken cancellationToken = default)
    {
        try
        {
            var configurations = await service.GetConfigurationsAsync(entityType, cancellationToken);
            return Ok(configurations);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving dashboard configurations.", ex);
        }
    }

    /// <summary>
    /// Gets a specific dashboard configuration by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DashboardConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DashboardConfigurationDto>> GetConfigurationById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var configuration = await service.GetConfigurationByIdAsync(id, cancellationToken);
            if (configuration is null)
            {
                return CreateNotFoundProblem($"Dashboard configuration with id {id} not found.");
            }
            return Ok(configuration);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the dashboard configuration.", ex);
        }
    }

    /// <summary>
    /// Gets the default dashboard configuration for an entity type.
    /// </summary>
    [HttpGet("default/{entityType}")]
    [ProducesResponseType(typeof(DashboardConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DashboardConfigurationDto>> GetDefaultConfiguration(string entityType, CancellationToken cancellationToken = default)
    {
        try
        {
            var configuration = await service.GetDefaultConfigurationAsync(entityType, cancellationToken);
            if (configuration is null)
            {
                return CreateNotFoundProblem($"Default dashboard configuration for entity type '{entityType}' not found.");
            }
            return Ok(configuration);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while retrieving the default dashboard configuration.", ex);
        }
    }

    /// <summary>
    /// Creates a new dashboard configuration.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DashboardConfigurationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DashboardConfigurationDto>> CreateConfiguration([FromBody] CreateDashboardConfigurationDto dto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var configuration = await service.CreateConfigurationAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetConfigurationById), new { id = configuration.Id }, configuration);
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while creating the dashboard configuration.", ex);
        }
    }

    /// <summary>
    /// Updates an existing dashboard configuration.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(DashboardConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DashboardConfigurationDto>> UpdateConfiguration(Guid id, [FromBody] UpdateDashboardConfigurationDto dto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return CreateValidationProblemDetails();
        }

        try
        {
            var configuration = await service.UpdateConfigurationAsync(id, dto, cancellationToken);
            return Ok(configuration);
        }
        catch (InvalidOperationException)
        {
            return CreateNotFoundProblem($"Dashboard configuration with id {id} not found.");
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while updating the dashboard configuration.", ex);
        }
    }

    /// <summary>
    /// Deletes a dashboard configuration.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteConfiguration(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await service.DeleteConfigurationAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return CreateNotFoundProblem($"Dashboard configuration with id {id} not found.");
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while deleting the dashboard configuration.", ex);
        }
    }

    /// <summary>
    /// Sets a configuration as default for its entity type.
    /// </summary>
    [HttpPost("{id}/set-default")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> SetAsDefault(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await service.SetAsDefaultAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return CreateNotFoundProblem($"Dashboard configuration with id {id} not found.");
        }
        catch (Exception ex)
        {
            return CreateInternalServerErrorProblem("An error occurred while setting the dashboard configuration as default.", ex);
        }
    }
}
